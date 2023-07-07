﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia.Threading;
using OpenUtau.Core;
using OpenUtau.Core.Format;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SharpCompress;
using YamlDotNet.Core.Tokens;

namespace OpenUtau.App.ViewModels {
    public class NotePropertiesViewModel : ViewModelBase, ICmdSubscriber {
        [Reactive] public string Lyric { get; set; } = "a";
        [Reactive] public float PortamentoLength { get; set; }
        [Reactive] public float PortamentoStart { get; set; }
        [Reactive] public bool VibratoEnable { get; set; }
        [Reactive] public float VibratoLength { get; set; }
        [Reactive] public float VibratoPeriod { get; set; }
        [Reactive] public float VibratoDepth { get; set; }
        [Reactive] public float VibratoIn { get; set; }
        [Reactive] public float VibratoOut { get; set; }
        [Reactive] public float VibratoShift { get; set; }
        [Reactive] public float AutoVibratoNoteLength { get; set; }
        [Reactive] public bool AutoVibratoToggle { get; set; }
        [Reactive] public bool IsNoteSelected { get; set; } = false;

        public List<NotePresets.PortamentoPreset>? PortamentoPresets { get; }
        public NotePresets.PortamentoPreset? ApplyPortamentoPreset {
            get => appliedPortamentoPreset;
            set => this.RaiseAndSetIfChanged(ref appliedPortamentoPreset, value);
        }
        public List<NotePresets.VibratoPreset>? VibratoPresets { get; }
        public NotePresets.VibratoPreset? ApplyVibratoPreset {
            get => appliedVibratoPreset;
            set => this.RaiseAndSetIfChanged(ref appliedVibratoPreset, value);
        }
        private NotePresets.PortamentoPreset? appliedPortamentoPreset = NotePresets.Default.DefaultPortamento;
        private NotePresets.VibratoPreset? appliedVibratoPreset = NotePresets.Default.DefaultVibrato;

        public UVoicePart? Part;
        private HashSet<UNote> selectedNotes = new HashSet<UNote>();
        public List<NotePropertyExpViewModel> Expressions = new List<NotePropertyExpViewModel>();
        public static bool PanelControlPressed { get; set; } = false;
        public static bool NoteLoading { get; set; } = false;
        private static bool AllowNoteEdit { get => PanelControlPressed && !NoteLoading; }

        public NotePropertiesViewModel() {
            PortamentoPresets = NotePresets.Default.PortamentoPresets;
            VibratoPresets = NotePresets.Default.VibratoPresets;

            SetValueChanges();

            MessageBus.Current.Listen<NotesSelectionEvent>()
                .Subscribe(e => {
                    if (PanelControlPressed) {
                        PanelControlPressed = false;
                        DocManager.Inst.EndUndoGroup();
                    }
                    NoteLoading = true;

                    selectedNotes.Clear();
                    selectedNotes.UnionWith(e.selectedNotes);
                    selectedNotes.UnionWith(e.tempSelectedNotes);
                    OnSelectNotes();

                    NoteLoading = false;
                });

            DocManager.Inst.AddSubscriber(this);
        }

        // note -> panel
        private void OnSelectNotes() {
            if (selectedNotes.Count > 0) {
                IsNoteSelected = true;
                var note = selectedNotes.First();

                Lyric = note.lyric;
                if(note.pitch.data.Count == 2) {
                    PortamentoLength = note.pitch.data[1].X - note.pitch.data[0].X;
                    PortamentoStart = note.pitch.data[0].X;
                } else {
                    PortamentoLength = NotePresets.Default.DefaultPortamento.PortamentoLength;
                    PortamentoStart = NotePresets.Default.DefaultPortamento.PortamentoStart;
                }
                VibratoEnable = note.vibrato.length == 0 ? false : true;
                VibratoLength = note.vibrato.length;
                VibratoPeriod = note.vibrato.period;
                VibratoDepth = note.vibrato.depth;
                VibratoIn = note.vibrato.@in;
                VibratoOut = note.vibrato.@out;
                VibratoShift = note.vibrato.shift;
            } else {
                IsNoteSelected = false;
                Lyric = NotePresets.Default.DefaultLyric;
                PortamentoLength = NotePresets.Default.DefaultPortamento.PortamentoLength;
                PortamentoStart = NotePresets.Default.DefaultPortamento.PortamentoStart;
                VibratoLength = NotePresets.Default.DefaultVibrato.VibratoLength;
                VibratoPeriod = NotePresets.Default.DefaultVibrato.VibratoPeriod;
                VibratoDepth = NotePresets.Default.DefaultVibrato.VibratoDepth;
                VibratoIn = NotePresets.Default.DefaultVibrato.VibratoIn;
                VibratoOut = NotePresets.Default.DefaultVibrato.VibratoOut;
                VibratoShift = NotePresets.Default.DefaultVibrato.VibratoShift;
            }
            AutoVibratoNoteLength = NotePresets.Default.AutoVibratoNoteDuration;
            AutoVibratoToggle = NotePresets.Default.AutoVibratoToggle;

            AttachExpressions();
        }

        public void LoadPart(UPart? part) {
            Expressions.Clear();
            if (part != null && part is UVoicePart) {
                this.Part = part as UVoicePart;

                foreach (KeyValuePair<string, UExpressionDescriptor> pair in DocManager.Inst.Project.expressions) {
                    if (pair.Value.type != UExpressionType.Curve) {
                        var viewModel = new NotePropertyExpViewModel(pair.Value, this);
                        if (pair.Value.abbr == Ustx.CLR) {
                            var track = DocManager.Inst.Project.tracks[part.trackNo];
                            if (track.VoiceColorExp != null && track.VoiceColorExp.options.Length > 0) {
                                track.VoiceColorExp.options.ForEach(opt => viewModel.Options.Add(opt));
                            }
                        }
                        Expressions.Add(viewModel);
                    }
                }
                AttachExpressions();
            } else {
                this.Part = null;
            }
        }

        private void AttachExpressions() {
            if (Expressions.Count > 0) {
                if (selectedNotes.Count > 0) {
                    var note = selectedNotes.First();

                    foreach (NotePropertyExpViewModel exp in Expressions) {
                        exp.IsNoteSelected = true;
                        var phonemeExpression = note.phonemeExpressions.FirstOrDefault(e => e.abbr == exp.abbr);
                        if (phonemeExpression != null) {
                            if (exp.IsNumerical) {
                                exp.Value = phonemeExpression.value;
                            } else if (exp.IsOptions) {
                                exp.SelectedOption = (int)phonemeExpression.value;
                            }
                        } else {
                            if (exp.IsNumerical) {
                                exp.Value = exp.defaultValue;
                            } else if (exp.IsOptions) {
                                exp.SelectedOption = (int)exp.defaultValue;
                            }
                        }
                    }
                } else {
                    foreach (NotePropertyExpViewModel exp in Expressions) {
                        exp.IsNoteSelected = false;
                        if (exp.IsNumerical) {
                            exp.Value = exp.defaultValue;
                        } else if (exp.IsOptions) {
                            exp.SelectedOption = (int)exp.defaultValue;
                        }
                    }
                }
            }
        }

        // panel -> note
        private void SetValueChanges() {
            this.WhenAnyValue(vm => vm.Lyric)
                .Subscribe(lyric => {
                    if (AllowNoteEdit && Part != null && selectedNotes.Count > 0) {
                        if (!string.IsNullOrEmpty(lyric)) {
                            foreach (UNote note in selectedNotes) {
                                if (note.lyric != lyric) {
                                    DocManager.Inst.ExecuteCmd(new ChangeNoteLyricCommand(Part, note, lyric));
                                }
                            }
                        }
                    }
                });
            this.WhenAnyValue(vm => vm.PortamentoLength)
                .Subscribe(value => {
                    if (value >= 2 && value <= 320) {
                        if (AllowNoteEdit && Part != null && selectedNotes.Count > 0) {
                            var pitch = new UPitch() { snapFirst = true };
                            pitch.AddPoint(new PitchPoint(PortamentoStart, 0));
                            pitch.AddPoint(new PitchPoint(PortamentoStart + PortamentoLength, 0));
                            foreach (UNote note in selectedNotes) {
                                DocManager.Inst.ExecuteCmd(new SetPitchPointsCommand(Part, note, pitch));
                            }
                        }
                    }
                });
            this.WhenAnyValue(vm => vm.PortamentoStart)
                .Subscribe(value => {
                    if (value >= -200 && value <= 200) {
                        if (AllowNoteEdit && Part != null && selectedNotes.Count > 0) {
                            var pitch = new UPitch() { snapFirst = true };
                            pitch.AddPoint(new PitchPoint(PortamentoStart, 0));
                            pitch.AddPoint(new PitchPoint(PortamentoStart + PortamentoLength, 0));
                            foreach (UNote note in selectedNotes) {
                                DocManager.Inst.ExecuteCmd(new SetPitchPointsCommand(Part, note, pitch));
                            }
                        }
                    }
                });
            this.WhenAnyValue(vm => vm.VibratoLength)
                .Subscribe(value => {
                    if (AllowNoteEdit && Part != null && selectedNotes.Count > 0) {
                        if(value >= 0 && value <= 100) {
                            UNote first = selectedNotes.First();
                            foreach (UNote note in selectedNotes) {
                                if (note != first && AutoVibratoToggle && note.duration < AutoVibratoNoteLength) {
                                    if (note.vibrato.length != 0) {
                                        DocManager.Inst.ExecuteCmd(new VibratoLengthCommand(Part, note, 0));
                                    }
                                } else {
                                    if (note.vibrato.length != value) {
                                        DocManager.Inst.ExecuteCmd(new VibratoLengthCommand(Part, note, value));
                                    }
                                }
                            }
                            if (first.vibrato.length > 0) {
                                VibratoEnable = true;
                            } else {
                                VibratoEnable = false;
                            }
                        }
                    }
                });
            this.WhenAnyValue(vm => vm.VibratoPeriod)
                .Subscribe(value => {
                    if (value >= 5 && value <= 500) {
                        if (AllowNoteEdit && Part != null && selectedNotes.Count > 0) {
                            foreach (UNote note in selectedNotes) {
                                if (note.vibrato.period != value) {
                                    DocManager.Inst.ExecuteCmd(new VibratoPeriodCommand(Part, note, value));
                                }
                            }
                        }
                    }
                });
            this.WhenAnyValue(vm => vm.VibratoDepth)
                .Subscribe(value => {
                    if (value >= 5 && value <= 200) {
                        if (AllowNoteEdit && Part != null && selectedNotes.Count > 0) {
                            foreach (UNote note in selectedNotes) {
                                if (note.vibrato.depth != value) {
                                    DocManager.Inst.ExecuteCmd(new VibratoDepthCommand(Part, note, value));
                                }
                            }
                        }
                    }
                });
            this.WhenAnyValue(vm => vm.VibratoIn)
                .Subscribe(value => {
                    if (value >= 0 && value <= 100) {
                        if (AllowNoteEdit && Part != null && selectedNotes.Count > 0) {
                            foreach (UNote note in selectedNotes) {
                                if (note.vibrato.@in != value) {
                                    DocManager.Inst.ExecuteCmd(new VibratoFadeInCommand(Part, note, value));
                                }
                            }
                        }
                    }
                });
            this.WhenAnyValue(vm => vm.VibratoOut)
                .Subscribe(value => {
                    if (value >= 0 && value <= 100) {
                        if (AllowNoteEdit && Part != null && selectedNotes.Count > 0) {
                            foreach (UNote note in selectedNotes) {
                                if (note.vibrato.@out != value) {
                                    DocManager.Inst.ExecuteCmd(new VibratoFadeOutCommand(Part, note, value));
                                }
                            }
                        }
                    }
                });
            this.WhenAnyValue(vm => vm.VibratoShift)
                .Subscribe(value => {
                    if (value >= 0 && value <= 100) {
                        if (AllowNoteEdit && Part != null && selectedNotes.Count > 0) {
                            foreach (UNote note in selectedNotes) {
                                if (note.vibrato.shift != value) {
                                    DocManager.Inst.ExecuteCmd(new VibratoShiftCommand(Part, note, value));
                                }
                            }
                        }
                    }
                });

            this.WhenAnyValue(vm => vm.ApplyPortamentoPreset)
                .WhereNotNull()
                .Subscribe(portamentoPreset => {
                    if (portamentoPreset != null && Part != null && selectedNotes.Count > 0) {
                        DocManager.Inst.StartUndoGroup();
                        PanelControlPressed = true;
                        PortamentoLength = portamentoPreset.PortamentoLength;
                        PortamentoStart = portamentoPreset.PortamentoStart;
                        PanelControlPressed = false;
                        DocManager.Inst.EndUndoGroup();
                    }
                });
            this.WhenAnyValue(vm => vm.ApplyVibratoPreset)
                .WhereNotNull()
                .Subscribe(vibratoPreset => {
                    if (vibratoPreset != null && Part != null && selectedNotes.Count > 0) {
                        DocManager.Inst.StartUndoGroup();
                        PanelControlPressed = true;
                        VibratoLength = Math.Max(0, Math.Min(100, vibratoPreset.VibratoLength));
                        VibratoPeriod = Math.Max(5, Math.Min(500, vibratoPreset.VibratoPeriod));
                        VibratoDepth = Math.Max(5, Math.Min(200, vibratoPreset.VibratoDepth));
                        VibratoIn = Math.Max(0, Math.Min(100, vibratoPreset.VibratoIn));
                        VibratoOut = Math.Max(0, Math.Min(100, vibratoPreset.VibratoOut));
                        VibratoShift = Math.Max(0, Math.Min(100, vibratoPreset.VibratoShift));
                        PanelControlPressed = false;
                        DocManager.Inst.EndUndoGroup();
                    }
                });
        }
        public void SetVibratoEnable() {
            if (Part != null && selectedNotes.Count > 0) {
                DocManager.Inst.StartUndoGroup();
                bool enable = VibratoEnable;
                UNote first = selectedNotes.First();

                foreach (UNote note in selectedNotes) {
                    if (enable) {
                        if (note.vibrato.length != 0) {
                            DocManager.Inst.ExecuteCmd(new VibratoLengthCommand(Part, note, 0));
                        }
                    } else {
                        if (note != first && AutoVibratoToggle && note.duration < AutoVibratoNoteLength) {
                            if (note.vibrato.length != 0) {
                                DocManager.Inst.ExecuteCmd(new VibratoLengthCommand(Part, note, 0));
                            }
                        } else {
                            if (note.vibrato.length != NotePresets.Default.DefaultVibrato.VibratoLength) {
                                DocManager.Inst.ExecuteCmd(new VibratoLengthCommand(Part, note, NotePresets.Default.DefaultVibrato.VibratoLength));
                            }
                        }
                    }
                }
                DocManager.Inst.EndUndoGroup();
            }
        }
        public void SetNumericalExpressionsChanges(string abbr, float value) {
            if (AllowNoteEdit && Part != null && selectedNotes.Count > 0) {
                var track = DocManager.Inst.Project.tracks[Part.trackNo];

                foreach (UNote note in selectedNotes) {
                    foreach (UPhoneme phoneme in Part.phonemes) {
                        if (phoneme.Parent == note) {
                            DocManager.Inst.ExecuteCmd(new SetPhonemeExpressionCommand(DocManager.Inst.Project, track, Part, phoneme, abbr, value));
                        }
                    }
                }
            }
        }
        public void SetOptionalExpressionsChanges(string abbr, int value) {
            if (!NoteLoading && Part != null && selectedNotes.Count > 0) {
                var track = DocManager.Inst.Project.tracks[Part.trackNo];
                DocManager.Inst.StartUndoGroup();

                foreach (UNote note in selectedNotes) {
                    foreach (UPhoneme phoneme in Part.phonemes) {
                        if (phoneme.Parent == note) {
                            DocManager.Inst.ExecuteCmd(new SetPhonemeExpressionCommand(DocManager.Inst.Project, track, Part, phoneme, abbr, value));
                        }
                    }
                }
                DocManager.Inst.EndUndoGroup();
            }
        }

        // presets
        public void SavePortamentoPreset(string name) {
            if (string.IsNullOrEmpty(name)) {
                return;
            }
            NotePresets.Default.PortamentoPresets.Add(new NotePresets.PortamentoPreset(name, (int)PortamentoLength, (int)PortamentoStart));
            NotePresets.Save();
        }
        public void RemoveAppliedPortamentoPreset() {
            if (appliedPortamentoPreset == null) {
                return;
            }
            NotePresets.Default.PortamentoPresets.Remove(appliedPortamentoPreset);
            NotePresets.Save();
        }
        public void SaveVibratoPreset(string name) {
            if (string.IsNullOrEmpty(name)) {
                return;
            }
            NotePresets.Default.VibratoPresets.Add(new NotePresets.VibratoPreset(name, VibratoLength, VibratoPeriod, VibratoDepth, VibratoIn, VibratoOut, VibratoShift));
            NotePresets.Save();
        }
        public void RemoveAppliedVibratoPreset() {
            if (appliedVibratoPreset == null) {
                return;
            }
            NotePresets.Default.VibratoPresets.Remove(appliedVibratoPreset);
            NotePresets.Save();
        }

        #region ICmdSubscriber
        public void OnNext(UCommand cmd, bool isUndo) {
            var note = selectedNotes.FirstOrDefault();
            if (note == null || AllowNoteEdit) { return; }

            if (cmd is NoteCommand noteCommand) {
                if (cmd is ChangeNoteLyricCommand) {
                    if (noteCommand.Notes.Contains(note)) {
                        Lyric = note.lyric;
                    }
                } else if (cmd is VibratoLengthCommand) {
                    if (noteCommand.Notes.Contains(note)) {
                        if (note.vibrato.length > 0) {
                            VibratoEnable = true;
                        } else {
                            VibratoEnable = false;
                        }
                        VibratoLength = note.vibrato.length;
                    }
                } else if (cmd is VibratoFadeInCommand) {
                    if (noteCommand.Notes.Contains(note)) {
                        VibratoIn = note.vibrato.@in;
                    }
                } else if (cmd is VibratoFadeOutCommand) {
                    if (noteCommand.Notes.Contains(note)) {
                        VibratoOut = note.vibrato.@out;
                    }
                } else if (cmd is VibratoDepthCommand) {
                    if (noteCommand.Notes.Contains(note)) {
                        VibratoDepth = note.vibrato.depth;
                    }
                } else if (cmd is VibratoPeriodCommand) {
                    if (noteCommand.Notes.Contains(note)) {
                        VibratoPeriod = note.vibrato.period;
                    }
                } else if (cmd is VibratoShiftCommand) {
                    if (noteCommand.Notes.Contains(note)) {
                        VibratoShift = note.vibrato.shift;
                    }
                }
            } else if (cmd is ExpCommand) {
                if (cmd is PitchExpCommand pitchExpCommand) {
                    if (pitchExpCommand.Note == null || pitchExpCommand.Note == note) {
                        if (note.pitch.data.Count == 2) {
                            PortamentoLength = note.pitch.data[1].X - note.pitch.data[0].X;
                            PortamentoStart = note.pitch.data[0].X;
                        } else {
                            PortamentoLength = NotePresets.Default.DefaultPortamento.PortamentoLength;
                            PortamentoStart = NotePresets.Default.DefaultPortamento.PortamentoStart;
                        }
                    }
                } else if (cmd is SetPhonemeExpressionCommand || cmd is ResetExpressionsCommand) {
                    AttachExpressions();
                }
            }
        }
        #endregion

        /*public void Finish() {
            if (notesViewModel.Part != null) {
                UVoicePart part = notesViewModel.Part;
                List<UNote> selectedNotes = notesViewModel.Selection.ToList();

                DocManager.Inst.StartUndoGroup();

                if (SetLyric) {
                    foreach (UNote note in selectedNotes) {
                        if (note.lyric != Lyric) {
                            DocManager.Inst.ExecuteCmd(new ChangeNoteLyricCommand(part, note, Lyric));
                        }
                    }
                }
                if (SetPortamento) {
                    foreach (UNote note in selectedNotes) {
                        var pitch = new UPitch();
                        pitch.AddPoint(new PitchPoint(PortamentoStart, 0));
                        pitch.AddPoint(new PitchPoint(PortamentoStart + PortamentoLength, 0));
                        DocManager.Inst.ExecuteCmd(new SetPitchPointsCommand(part, note, pitch));
                    }
                }
                if (SetVibrato) {
                    foreach (UNote note in selectedNotes) {
                        if(VibratoEnable && VibratoLength != 0) {
                            if (!AutoVibratoToggle || (AutoVibratoToggle && note.duration >= AutoVibratoNoteLength)) {
                                DocManager.Inst.ExecuteCmd(new VibratoLengthCommand(part, note, VibratoLength));
                                DocManager.Inst.ExecuteCmd(new VibratoFadeInCommand(part, note, VibratoIn));
                                DocManager.Inst.ExecuteCmd(new VibratoFadeOutCommand(part, note, VibratoOut));
                                DocManager.Inst.ExecuteCmd(new VibratoDepthCommand(part, note, VibratoDepth));
                                DocManager.Inst.ExecuteCmd(new VibratoPeriodCommand(part, note, VibratoPeriod));
                                DocManager.Inst.ExecuteCmd(new VibratoShiftCommand(part, note, VibratoShift));
                            } else {
                                DocManager.Inst.ExecuteCmd(new VibratoLengthCommand(part, note, 0));
                            }
                        } else if (note.vibrato.length != 0) {
                            DocManager.Inst.ExecuteCmd(new VibratoLengthCommand(part, note, 0));
                        }
                    }
                }
                
                DocManager.Inst.EndUndoGroup();
            }
        }*/
    }

    public class NotePropertyExpViewModel : ViewModelBase {
        public string Name { get; set; }
        public bool IsNumerical { get; set; } = false;
        public bool IsOptions { get; set; } = false;
        public float Min { get; set; }
        public float Max { get; set; }
        public ObservableCollection<string> Options { get; set; } = new ObservableCollection<string>();
        public string abbr;
        public float defaultValue;

        [Reactive] public bool IsNoteSelected { get; set; } = false;
        [Reactive] public float Value { get; set; }
        [Reactive] public int SelectedOption { get; set; }
        [Reactive] public bool DropDownOpen { get; set; }

        private NotePropertiesViewModel parentViewmodel;

        public NotePropertyExpViewModel(UExpressionDescriptor descriptor, NotePropertiesViewModel parent) {
            Name = descriptor.name;
            defaultValue = descriptor.defaultValue;
            abbr = descriptor.abbr;
            if (descriptor.type == UExpressionType.Numerical) {
                IsNumerical = true;
                Max = descriptor.max;
                Min = descriptor.min;
                Value = defaultValue;
            } else if (descriptor.type == UExpressionType.Options) {
                IsOptions = true;
                descriptor.options.ForEach(opt => Options.Add(opt));
                SelectedOption = (int)defaultValue;
            }

            parentViewmodel = parent;

            if (IsNumerical) {
                this.WhenAnyValue(vm => vm.Value)
                    .Subscribe(value => {
                        if (value >= Min && value <= Max) {
                            parentViewmodel.SetNumericalExpressionsChanges(abbr, value);
                        }
                    });
            }
            if (IsOptions) {
                this.WhenAnyValue(vm => vm.SelectedOption)
                    .Subscribe(value => {
                        if (value >= 0 && DropDownOpen) {
                            parentViewmodel.SetOptionalExpressionsChanges(abbr, value);
                        }
                    });
            }
        }
        public override string ToString() {
            return Name;
        }
    }
}
