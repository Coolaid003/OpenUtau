﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenUtau.Core.Format;
using OpenUtau.Core.Render;
using OpenUtau.Core.Ustx;
using Serilog;
using static OpenUtau.Api.Phonemizer;

namespace OpenUtau.Core.Voicevox {
    public class VoicevoxRenderer : IRenderer {
        const string VOLC = VoicevoxUtils.VOLC;
        const string KEYS = VoicevoxUtils.KEYS;
        const string PITD = Format.Ustx.PITD;

        static readonly HashSet<string> supportedExp = new HashSet<string>(){
            Format.Ustx.DYN,
            PITD,
            Format.Ustx.CLR,
            VOLC,
            KEYS,
            Format.Ustx.SHFC,
            Format.Ustx.SHFT
        };

        static readonly object lockObj = new object();

        public USingerType SingerType => USingerType.Voicevox;

        public bool SupportsRenderPitch => true;

        public bool SupportsExpression(UExpressionDescriptor descriptor) {
            return supportedExp.Contains(descriptor.abbr);
        }

        public RenderResult Layout(RenderPhrase phrase) {
            return new RenderResult() {
                leadingMs = phrase.leadingMs,
                positionMs = phrase.positionMs - phrase.timeAxis.TickPosToMsPos((VoicevoxUtils.headS * 1000d)),
                estimatedLengthMs = phrase.durationMs + phrase.leadingMs,
            };
        }

        public Task<RenderResult> Render(RenderPhrase phrase, Progress progress, int trackNo, CancellationTokenSource cancellation, bool isPreRender) {
            var task = Task.Run(() => {
                lock (lockObj) {
                    if (cancellation.IsCancellationRequested) {
                        return new RenderResult();
                    }
                    string progressInfo = $"Track {trackNo + 1}: {this} \"{string.Join(" ", phrase.phones.Select(p => p.phoneme))}\"";
                    progress.Complete(0, progressInfo);
                     var wavPath = Path.Join(PathManager.Inst.CachePath, $"vv-{phrase.hash:x16}-{phrase.preEffectHash:x8}.wav");
                    var result = Layout(phrase);
                    if (!File.Exists(wavPath)) {
                        var singer = phrase.singer as VoicevoxSinger;
                        if (singer != null) {
                            Log.Information($"Starting Voicevox synthesis");
                            VoicevoxNote vvNotes = new VoicevoxNote();
                            string singerID = VoicevoxUtils.defaultID;
                            if (!singer.voicevoxConfig.Tag.Equals("VOICEVOX JA")) {
                                Note[][] notes = new Note[phrase.notes.Length][];

                                for (int i = 0; i < phrase.notes.Length; i++) {
                                    notes[i] = new Note[1];
                                    notes[i][0] = new Note() {
                                        lyric = phrase.notes[i].lyric,
                                        position = phrase.notes[i].position,
                                        duration = phrase.notes[i].duration,
                                        tone = (int)(phrase.notes[i].tone + phrase.phones[0].toneShift)
                                    };
                                }

                                var qNotes = VoicevoxUtils.NoteGroupsToVoicevox(notes, phrase.timeAxis, singer);

                                if (singer.voicevoxConfig.base_singer_style != null) {
                                    foreach (var s in singer.voicevoxConfig.base_singer_style) {
                                        if (s.name.Equals(singer.voicevoxConfig.base_singer_name)) {
                                            if (s.styles.name.Equals(singer.voicevoxConfig.base_singer_style_name)) {
                                                vvNotes = VoicevoxUtils.VoicevoxVoiceBase(qNotes, s.styles.id.ToString());
                                                break;
                                            }
                                        }
                                    }
                                }
                                if(vvNotes.phonemes.Count() == 0) {
                                    vvNotes = VoicevoxUtils.VoicevoxVoiceBase(qNotes, singerID);
                                }
                            } else {
                                vvNotes = PhraseToVoicevoxNotes(phrase);
                            }
                            if (vvNotes.phonemes.Count() > 0) {
                                result.positionMs = phrase.positionMs - phrase.timeAxis.TickPosToMsPos((vvNotes.phonemes.First().frame_length / VoicevoxUtils.fps) * 1000d);
                            }

                            int speaker = 0;
                            singer.voicevoxConfig.styles.ForEach(style => {
                                if (style.name.Equals(phrase.singer.Subbanks[1].Color)) {
                                    speaker = style.id;
                                }
                                if (style.name.Equals(phrase.phones.FirstOrDefault().suffix)) {
                                    speaker = style.id;
                                }
                            });
                            try {
                                var queryurl = new VoicevoxURL() { method = "POST", path = "/frame_synthesis", query = new Dictionary<string, string> { { "speaker", speaker.ToString() } }, body = JsonConvert.SerializeObject(vvNotes), accept = "audio/wav" };
                                var response = VoicevoxClient.Inst.SendRequest(queryurl);
                                byte[] bytes = null;
                                if (!response.Item2.Equals(null)) {
                                    bytes = response.Item2;
                                } else if (!string.IsNullOrEmpty(response.Item1)) {
                                    var jObj = JObject.Parse(response.Item1);
                                    if (jObj.ContainsKey("detail")) {
                                        Log.Error($"Failed to create a voice base. : {jObj}");
                                    }
                                }
                                if (bytes != null) {
                                    File.WriteAllBytes(wavPath, bytes);
                                }
                            } catch (Exception e) {
                                Log.Error($"Failed to create a voice base.");
                            }
                            if (cancellation.IsCancellationRequested) {
                                return new RenderResult();
                            }
                        }
                    }
                    progress.Complete(phrase.phones.Length, progressInfo);
                    try {
                        if (File.Exists(wavPath)) {
                            using (var waveStream = new WaveFileReader(wavPath)) {

                                result.samples = Wave.GetSamples(waveStream.ToSampleProvider().ToMono(1, 0));
                            }
                            if (result.samples != null) {
                                Renderers.ApplyDynamics(phrase, result);
                            }
                        }
                    } catch(Exception e) {
                        Log.Error(e.Message);
                        result.samples = new float[0];
                    }
                    return result;
                }
            });
            return task;
        }

        static VoicevoxNote PhraseToVoicevoxNotes(RenderPhrase phrase) {
            VoicevoxNote notes = new VoicevoxNote();

            int headFrames = (int)(VoicevoxUtils.headS * VoicevoxUtils.fps);
            int tailFrames = (int)(VoicevoxUtils.tailS * VoicevoxUtils.fps);

            notes.phonemes.Add(new Phonemes {
                phoneme = "pau",
                frame_length = headFrames
            });
            foreach (var phone in phrase.phones) {
                notes.phonemes.Add(new Phonemes {
                    phoneme = phone.phoneme,
                    frame_length = (int)(phone.durationMs / 1000d * VoicevoxUtils.fps),
                });
            }
            notes.phonemes.Add(new Phonemes {
                phoneme = "pau",
                frame_length = tailFrames
            });

            int vvTotalFrames = -(headFrames + tailFrames);
            notes.phonemes.ForEach(x => vvTotalFrames += x.frame_length);
            double frameMs = 1 / 1000d * VoicevoxUtils.fps;
            int totalFrames = (int)(vvTotalFrames / VoicevoxUtils.fps * 1000d);
            int frameRatio = vvTotalFrames / totalFrames;
            const int pitchInterval = 5;


            //var curve = phrase.pitches.SelectMany(item => Enumerable.Repeat(item, 5)).ToArray();
            notes.f0 = VoicevoxUtils.SampleCurve(phrase, phrase.pitches, 0, frameMs, vvTotalFrames, 0, 0, x => MusicMath.ToneToFreq(x * 0.01)).ToList();
            //notes.f0 = f0.Where((x, i) => i % frameRatio == 0).ToList();
            float[] f0Shifted = notes.f0.Select(f => (float)f).ToArray();
            if (phrase.toneShift != null) {
                for (int i = 0; i < notes.f0.Count; i++) {
                    double posMs = phrase.positionMs - phrase.leadingMs + i * frameMs;
                    int ticks = phrase.timeAxis.MsPosToTickPos(posMs) - (phrase.position - phrase.leading);
                    int index = Math.Max(0, (int)((double)ticks / pitchInterval));
                    if (index < phrase.pitches.Length) {
                        f0Shifted[i] = (float)MusicMath.ToneToFreq((phrase.pitches[index] + phrase.toneShift[index]) * 0.01);
                    }
                }
            }


            var volumeCurve = phrase.curves.FirstOrDefault(c => c.Item1 == VOLC);
            if (volumeCurve != null) {
                notes.volume = VoicevoxUtils.SampleCurve(phrase, volumeCurve.Item2, 0, frameMs, vvTotalFrames, 0, 0, x => MusicMath.DecibelToLinear(x)).ToList();
                //notes.volume = volume.Where((x, i) => i % frameRatio == 0).ToList();
            } else {
                notes.volume = Enumerable.Repeat(1d, vvTotalFrames).ToList();
            }

            notes.outputStereo = false;
            notes.outputSamplingRate = 44100;
            notes.volumeScale = 1;
            return notes;
        }


        public UExpressionDescriptor[] GetSuggestedExpressions(USinger singer, URenderSettings renderSettings) {
            var result = new List<UExpressionDescriptor> {
                new UExpressionDescriptor{
                    name="volume (curve)",
                    abbr=VOLC,
                    type=UExpressionType.Curve,
                    min=-20,
                    max=20,
                    defaultValue=0,
                    isFlag=false,
                },
                //new UExpressionDescriptor{
                //    name="key shift (curve)",
                //    abbr=KEYS,
                //    type=UExpressionType.Curve,
                //    min=-36,
                //    max=36,
                //    defaultValue=0,
                //    isFlag=false,
                //},
            };

            return result.ToArray();
        }

        public override string ToString() => Renderers.VOICEVOX;

        RenderPitchResult IRenderer.LoadRenderedPitch(RenderPhrase phrase) {
            try {
                var singer = phrase.singer as VoicevoxSinger;
                VoicevoxNote vvNotes = new VoicevoxNote();
                if (singer != null) {
                    string singerID = VoicevoxUtils.defaultID;
                    if (!singer.voicevoxConfig.Tag.Equals("VOICEVOX")) {
                        Note[][] notes = new Note[phrase.notes.Length][];

                        for (int i = 0; i < phrase.notes.Length; i++) {
                            notes[i] = new Note[1];
                            notes[i][0] = new Note() {
                                lyric = phrase.notes[i].lyric,
                                position = phrase.notes[i].position,
                                duration = phrase.notes[i].duration,
                                tone = phrase.notes[i].tone
                            };
                        }

                        var qNotes = VoicevoxUtils.NoteGroupsToVoicevox(notes, phrase.timeAxis, singer);

                        if (singer.voicevoxConfig.base_singer_style != null) {
                            foreach (var s in singer.voicevoxConfig.base_singer_style) {
                                if (s.name.Equals(singer.voicevoxConfig.base_singer_name)) {
                                    if (s.styles.name.Equals(singer.voicevoxConfig.base_singer_style_name)) {
                                        vvNotes = VoicevoxUtils.VoicevoxVoiceBase(qNotes, s.styles.id.ToString());
                                        break;
                                    }
                                }
                            }
                        }
                        if (vvNotes.phonemes.Count() <= 0) {
                            vvNotes = VoicevoxUtils.VoicevoxVoiceBase(qNotes, singerID);
                        }
                    } else {
                        vvNotes = PhraseToVoicevoxNotes(phrase);
                    }
                }
                float[] f0 = Array.ConvertAll(vvNotes.f0.ToArray(), item => (float)item);
                return new RenderPitchResult { tones = f0,ticks = new float[f0.Length] };
            }catch {
                return null;
            }
        }
    }
}
