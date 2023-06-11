using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using OpenUtau.Api;

namespace OpenUtau.Core.G2p {
    public class HindiG2p : G2pPack {
        private static readonly string[] graphemes = new string[] {
            "", "", "", "", "(", ")", "+", "-", "ँ", "ं", "ः", "अ", "आ", "इ", "ई", "उ", "ऊ", "ऋ",
            "ए", "ऐ", "ऑ", "ओ", "औ", "क", "ख", "ग", "घ", "ङ", "च", "छ", "ज", "झ", "ञ", "ट", "ठ",
            "ड", "ढ", "ण", "त", "थ", "द", "ध", "न", "प", "फ", "ब", "भ", "म", "य", "र", "ल", "ळ", "व",
            "श", "ष", "स", "ह", "़", "ा", "ि", "ी", "ु", "ू", "ृ", "े", "ै", "ॉ", "ो", "ौ", "्", "क़",
            "ज़", "ड़", "ढ़", "फ़",
        };

        private static readonly string[] phonemes = new string[] {
            "", "", "", "", "E", "E~", "Gh", "O", "O~", "a", "aa", "aa~", "a~", "b", "bh", "ch", "chh",
            "d", "dd", "ddh", "dh", "e", "e~", "f", "g", "gh", "h", "i", "ii", "ii~", "i~", "j", "jh",
            "k", "kh", "l", "m", "n", "ng", "nn", "o", "o~", "p", "ph", "q", "r", "rr", "rrh", "s", "sh",
            "ssh", "t", "th", "tt", "tth", "u", "uu", "uu~", "u~", "v", "x", "y", "z", "~n",
        };

        private static object lockObj = new object();
        private static Dictionary<string, int> graphemeIndexes;
        private static IG2p dict;
        private static InferenceSession session;
        private static Dictionary<string, string[]> predCache = new Dictionary<string, string[]>();

        public HindiG2p() {
            lock (lockObj) {
                if (graphemeIndexes == null) {
                    graphemeIndexes = graphemes
                        .Skip(4)
                        .Select((g, i) => Tuple.Create(g, i))
                        .ToDictionary(t => t.Item1, t => t.Item2 + 4);
                    var tuple = LoadPack(Data.Resources.g2p_hi);
                    dict = tuple.Item1;
                    session = tuple.Item2;
                }
            }
            GraphemeIndexes = graphemeIndexes;
            Phonemes = phonemes;
            Dict = dict;
            Session = session;
            PredCache = predCache;
        }
    }
}
