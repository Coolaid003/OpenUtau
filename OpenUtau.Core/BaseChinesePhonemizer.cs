﻿using System.Collections.Generic;
using System.Linq;
using G2p;
using OpenUtau.Api;

namespace OpenUtau.Core {
    public abstract class BaseChinesePhonemizer : Phonemizer {
        public static Note[] ChangeLyric(Note[] group, string lyric) {
            var oldNote = group[0];
            group[0] = new Note {
                lyric = lyric,
                phoneticHint = oldNote.phoneticHint,
                tone = oldNote.tone,
                position = oldNote.position,
                duration = oldNote.duration,
                phonemeAttributes = oldNote.phonemeAttributes,
            };
            return group;
        }

        public static string[] Romanize(IEnumerable<string> lyrics) {
            var zhG2p = new ZhG2p("mandarin");
            var pinyinRes = zhG2p.Convert(lyrics.ToList(), false, false).Split(" ");
            return pinyinRes;
        }

        public static void RomanizeNotes(Note[][] groups) {
            var ResultLyrics = Romanize(groups.Select(group => group[0].lyric));
            Enumerable.Zip(groups, ResultLyrics, ChangeLyric).Last();
        }

        public override void SetUp(Note[][] groups) {
            RomanizeNotes(groups);
        }
    }
}
