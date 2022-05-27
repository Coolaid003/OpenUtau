﻿using System.Collections.Generic;
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("Vietnamese VCV Phonemizer", "VIE VCV", "Jani Tran")]
    public class VietnameseVCVPhonemizer : Phonemizer {
        /// <summary>
        /// The lookup table to convert a hiragana to its tail vowel.
        /// </summary>
        static readonly string[] vowels = new string[] {
            "a=a,à,á,ả,ã,ạ,ă,ằ,ắ,ẳ,ẵ,ặ,A,À,Á,Ả,Ã,Ạ,Ă,Ằ,Ắ,Ẳ,Ẵ,Ặ",
            "A=â,ầ,ấ,ẩ,ẫ,ậ,Â,Ầ,Ấ,Ẩ,Ẫ,Ậ",
            "@=ơ,ờ,ớ,ở,ỡ,ợ,Ơ,Ờ,Ớ,Ở,Ỡ,Ợ",
            "i=i,y,ì,í,ỉ,ĩ,ị,ỳ,ý,ỷ,ỹ,ỵ,I,Y,Ì,Í,Ỉ,Ĩ,Ị,Ỳ,Ý,Ỷ,Ỹ,Ỵ",
            "e=e,è,é,ẻ,ẽ,ẹ,E,È,É,Ẻ,Ẽ,Ẹ",
            "E=ê,ề,ế,ể,ễ,ệ,Ê,Ề,Ế,Ể,Ễ,Ệ",
            "o=o,ò,ó,ỏ,õ,ọ,O,Ò,Ó,Ỏ,Õ,Ọ",
            "O=ô,ồ,ố,ổ,ỗ,ộ,Ô,Ồ,Ố,Ổ,Ỗ,Ộ",
            "u=u,ù,ú,ủ,ũ,ụ,U,Ù,Ú,Ủ,Ũ,Ụ",
            "U=ư,ừ,ứ,ử,ữ,ự,Ư,Ừ,Ứ,Ử,Ữ,Ự",
            "m=m,M",
            "n=n,N",
            "ng=g,G",
            "nh=h,H",
            "-=c,C,t,T,-,p,P,R",
        };

        static readonly Dictionary<string, string> vowelLookup;

        static VietnameseVCVPhonemizer() {
            vowelLookup = vowels.ToList()
                .SelectMany(line => {
                    var parts = line.Split('=');
                    return parts[1].Split(',').Select(cv => (cv, parts[0]));
                })
                .ToDictionary(t => t.Item1, t => t.Item2);
        }

        private USinger singer;

        public override void SetSinger(USinger singer) => this.singer = singer;

        public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevNeighbours) {
            var note = notes[0];
            if (!string.IsNullOrEmpty(note.phoneticHint)) {
                return new Result {
                    phonemes = new Phoneme[] {
                        new Phoneme {
                            phoneme = note.phoneticHint,
                        }
                    },
                };
            }
            int Short = note.duration * 4 / 7;
            int Long = note.duration / 6;
            int Medium = note.duration / 3;
            int ViTri = Short;
            bool a;
            if (note.lyric != "R") {
                note.lyric = note.lyric.ToLower();
            }
            if (note.lyric == "gi") {
                note.lyric = "zi";
            }
            note.lyric = note.lyric.Replace('à', 'a').Replace('á', 'a').Replace('ả', 'a').Replace('ã', 'a').Replace('ạ', 'a');
            note.lyric = note.lyric.Replace('ằ', 'ă').Replace('ắ', 'ă').Replace('ẳ', 'ă').Replace('ẵ', 'ă').Replace('ặ', 'ă');
            note.lyric = note.lyric.Replace('ầ', 'â').Replace('ấ', 'â').Replace('ẩ', 'â').Replace('ẫ', 'â').Replace('ậ', 'â');
            note.lyric = note.lyric.Replace('ờ', 'ơ').Replace('ớ', 'ơ').Replace('ở', 'ơ').Replace('ỡ', 'ơ').Replace('ợ', 'ơ');
            note.lyric = note.lyric.Replace('ì', 'i').Replace('í', 'i').Replace('ỉ', 'i').Replace('ĩ', 'i').Replace('ị', 'i');
            note.lyric = note.lyric.Replace('ỳ', 'y').Replace('ý', 'y').Replace('ỷ', 'y').Replace('ỹ', 'y').Replace('ỵ', 'y');
            note.lyric = note.lyric.Replace('è', 'e').Replace('é', 'e').Replace('ẻ', 'e').Replace('ẽ', 'e').Replace('ẹ', 'e');
            note.lyric = note.lyric.Replace('ề', 'ê').Replace('ế', 'ê').Replace('ể', 'ê').Replace('ễ', 'ê').Replace('ệ', 'ê');
            note.lyric = note.lyric.Replace('ò', 'o').Replace('ó', 'o').Replace('ỏ', 'o').Replace('õ', 'o').Replace('ọ', 'o');
            note.lyric = note.lyric.Replace('ồ', 'ô').Replace('ố', 'ô').Replace('ổ', 'ô').Replace('ỗ', 'ô').Replace('ộ', 'ô');
            note.lyric = note.lyric.Replace('ù', 'u').Replace('ú', 'u').Replace('ủ', 'u').Replace('ũ', 'u').Replace('ụ', 'u');
            note.lyric = note.lyric.Replace('ừ', 'ư').Replace('ứ', 'ư').Replace('ử', 'ư').Replace('ữ', 'ư').Replace('ự', 'ư');
            note.lyric = note.lyric.Replace("ch", "C").Replace("d", "z").Replace("đ", "d").Replace("ph", "f").Replace("ch", "C")
                .Replace("gi", "z").Replace("gh", "g").Replace("c", "k").Replace("kh", "K").Replace("ng", "N")
                .Replace("ngh", "N").Replace("nh", "J").Replace("x", "s").Replace("tr", "Z").Replace("th", "T")
                .Replace("qu", "w");

            bool tontaiVVC = (note.lyric.EndsWith("iên") || note.lyric.EndsWith("iêN") || note.lyric.EndsWith("iêm") || note.lyric.EndsWith("iêt") || note.lyric.EndsWith("iêk") || note.lyric.EndsWith("iêp") || note.lyric.EndsWith("iêu")
                           || note.lyric.EndsWith("yên") || note.lyric.EndsWith("yêN") || note.lyric.EndsWith("yêm") || note.lyric.EndsWith("yêt") || note.lyric.EndsWith("yêk") || note.lyric.EndsWith("yêp") || note.lyric.EndsWith("yêu")
                           || note.lyric.EndsWith("uôn") || note.lyric.EndsWith("uôN") || note.lyric.EndsWith("uôm") || note.lyric.EndsWith("uôt") || note.lyric.EndsWith("uôk") || note.lyric.EndsWith("uôi")
                           || note.lyric.EndsWith("ươn") || note.lyric.EndsWith("ươN") || note.lyric.EndsWith("ươm") || note.lyric.EndsWith("ươt") || note.lyric.EndsWith("ươk") || note.lyric.EndsWith("ươp") || note.lyric.EndsWith("ươi"));
            bool koVVCchia;
            if (tontaiVVC == true) {
                koVVCchia = false;
            } else
                koVVCchia = true;
            bool tontaiCcuoi = (note.lyric.EndsWith("k") || note.lyric.EndsWith("t") || note.lyric.EndsWith("C") || note.lyric.EndsWith("p"));
            bool kocoCcuoi;
            if (tontaiCcuoi == true) {
                kocoCcuoi = false;
            } else
                kocoCcuoi = true;
            bool tontaiC = (note.lyric.StartsWith("b") || note.lyric.StartsWith("C") || note.lyric.StartsWith("d") || note.lyric.StartsWith("f")
                         || note.lyric.StartsWith("g") || note.lyric.StartsWith("h") || note.lyric.StartsWith("k") || note.lyric.StartsWith("K")
                         || note.lyric.StartsWith("l") || note.lyric.StartsWith("m") || note.lyric.StartsWith("n") || note.lyric.StartsWith("N")
                         || note.lyric.StartsWith("J") || note.lyric.StartsWith("r") || note.lyric.StartsWith("s") || note.lyric.StartsWith("t")
                         || note.lyric.StartsWith("T") || note.lyric.StartsWith("Z") || note.lyric.StartsWith("v") || note.lyric.StartsWith("w")
                         || note.lyric.StartsWith("z"));
            bool kocoC;
            if (tontaiC == true) {
                kocoC = false;
            } else
                kocoC = true;
            bool tontaiVV = (note.lyric.EndsWith("ai") || note.lyric.EndsWith("ơi") || note.lyric.EndsWith("oi") || note.lyric.EndsWith("ôi") || note.lyric.EndsWith("ui") || note.lyric.EndsWith("ưi")
                          || note.lyric.EndsWith("ao") || note.lyric.EndsWith("eo") || note.lyric.EndsWith("êu") || note.lyric.EndsWith("iu")
                          || note.lyric.EndsWith("an") || note.lyric.EndsWith("ơn") || note.lyric.EndsWith("in") || note.lyric.EndsWith("en") || note.lyric.EndsWith("ên") || note.lyric.EndsWith("on") || note.lyric.EndsWith("ôn") || note.lyric.EndsWith("un") || note.lyric.EndsWith("ưn")
                          || note.lyric.EndsWith("am") || note.lyric.EndsWith("ơm") || note.lyric.EndsWith("im") || note.lyric.EndsWith("em") || note.lyric.EndsWith("êm") || note.lyric.EndsWith("om") || note.lyric.EndsWith("ôm") || note.lyric.EndsWith("um") || note.lyric.EndsWith("ưm")
                          || note.lyric.EndsWith("aN") || note.lyric.EndsWith("ơN") || note.lyric.EndsWith("iN") || note.lyric.EndsWith("eN") || note.lyric.EndsWith("êN") || note.lyric.EndsWith("ưN")
                          || note.lyric.EndsWith("aJ") || note.lyric.EndsWith("iJ") || note.lyric.EndsWith("êJ")
                          || note.lyric.EndsWith("at") || note.lyric.EndsWith("ơt") || note.lyric.EndsWith("it") || note.lyric.EndsWith("et") || note.lyric.EndsWith("êt") || note.lyric.EndsWith("ot") || note.lyric.EndsWith("ôt") || note.lyric.EndsWith("ut") || note.lyric.EndsWith("ưt")
                          || note.lyric.EndsWith("aC") || note.lyric.EndsWith("iC") || note.lyric.EndsWith("êC")
                          || note.lyric.EndsWith("ak") || note.lyric.EndsWith("ơk") || note.lyric.EndsWith("ik") || note.lyric.EndsWith("ek") || note.lyric.EndsWith("êk") || note.lyric.EndsWith("ok") || note.lyric.EndsWith("ôk") || note.lyric.EndsWith("uk") || note.lyric.EndsWith("ưk")
                          || note.lyric.EndsWith("ap") || note.lyric.EndsWith("ơp") || note.lyric.EndsWith("ip") || note.lyric.EndsWith("ep") || note.lyric.EndsWith("êp") || note.lyric.EndsWith("op") || note.lyric.EndsWith("ôp") || note.lyric.EndsWith("up") || note.lyric.EndsWith("ưp")
                          || note.lyric.EndsWith("ia") || note.lyric.EndsWith("ua") || note.lyric.EndsWith("ưa")
                          || note.lyric.EndsWith("ay") || note.lyric.EndsWith("ây") || note.lyric.EndsWith("uy")
                          || note.lyric.EndsWith("au") || note.lyric.EndsWith("âu")
                          || note.lyric.EndsWith("oa") || note.lyric.EndsWith("oe") || note.lyric.EndsWith("uê"));
            bool ViTriNgan = (note.lyric.EndsWith("ai") || note.lyric.EndsWith("ơi") || note.lyric.EndsWith("oi") || note.lyric.EndsWith("ôi") || note.lyric.EndsWith("ui") || note.lyric.EndsWith("ưi")
                  || note.lyric.EndsWith("ao") || note.lyric.EndsWith("eo") || note.lyric.EndsWith("êu") || note.lyric.EndsWith("iu")
                  || note.lyric.EndsWith("an") || note.lyric.EndsWith("ơn") || note.lyric.EndsWith("in") || note.lyric.EndsWith("en") || note.lyric.EndsWith("ên") || note.lyric.EndsWith("on") || note.lyric.EndsWith("ôn") || note.lyric.EndsWith("un") || note.lyric.EndsWith("ưn")
                  || note.lyric.EndsWith("am") || note.lyric.EndsWith("ơm") || note.lyric.EndsWith("im") || note.lyric.EndsWith("em") || note.lyric.EndsWith("êm") || note.lyric.EndsWith("om") || note.lyric.EndsWith("ôm") || note.lyric.EndsWith("um") || note.lyric.EndsWith("ưm")
                  || note.lyric.EndsWith("aN") || note.lyric.EndsWith("ơN") || note.lyric.EndsWith("iN") || note.lyric.EndsWith("eN") || note.lyric.EndsWith("êN") || note.lyric.EndsWith("oN") || note.lyric.EndsWith("ôN") || note.lyric.EndsWith("uN") || note.lyric.EndsWith("ưN")
                  || note.lyric.EndsWith("aJ") || note.lyric.EndsWith("iJ") || note.lyric.EndsWith("êJ")
                  || note.lyric.EndsWith("at") || note.lyric.EndsWith("ơt") || note.lyric.EndsWith("it") || note.lyric.EndsWith("et") || note.lyric.EndsWith("êt") || note.lyric.EndsWith("ot") || note.lyric.EndsWith("ôt") || note.lyric.EndsWith("ut") || note.lyric.EndsWith("ưt")
                  || note.lyric.EndsWith("aC") || note.lyric.EndsWith("iC") || note.lyric.EndsWith("êC")
                  || note.lyric.EndsWith("ak") || note.lyric.EndsWith("ơk") || note.lyric.EndsWith("ik") || note.lyric.EndsWith("ek") || note.lyric.EndsWith("êk") || note.lyric.EndsWith("ok") || note.lyric.EndsWith("ôk") || note.lyric.EndsWith("uk") || note.lyric.EndsWith("ưk")
                  || note.lyric.EndsWith("ap") || note.lyric.EndsWith("ơp") || note.lyric.EndsWith("ip") || note.lyric.EndsWith("ep") || note.lyric.EndsWith("êp") || note.lyric.EndsWith("op") || note.lyric.EndsWith("ôp") || note.lyric.EndsWith("up") || note.lyric.EndsWith("ưp")
                  || note.lyric.EndsWith("ia") || note.lyric.EndsWith("ua") || note.lyric.EndsWith("ưa")
                  || note.lyric.EndsWith("uya"));
            bool ViTriDai = (note.lyric.EndsWith("ay") || note.lyric.EndsWith("ây") || note.lyric.EndsWith("uy")
                  || note.lyric.EndsWith("au") || note.lyric.EndsWith("âu")
                  || note.lyric.EndsWith("oa") || note.lyric.EndsWith("oe") || note.lyric.EndsWith("uê"));
            bool ViTriTB = (note.lyric.EndsWith("ôN") || note.lyric.EndsWith("uN") || note.lyric.EndsWith("oN")
                  || note.lyric.EndsWith("ăt") || note.lyric.EndsWith("ât")
                  || note.lyric.EndsWith("ăk") || note.lyric.EndsWith("âk")
                  || note.lyric.EndsWith("ăp") || note.lyric.EndsWith("âp")
                  || note.lyric.EndsWith("ăn") || note.lyric.EndsWith("ân")
                  || note.lyric.EndsWith("ăN") || note.lyric.EndsWith("âN")
                  || note.lyric.EndsWith("ăm") || note.lyric.EndsWith("âm"));

            if (ViTriNgan) {
                ViTri = Short;
            }
            if (ViTriDai) {
                ViTri = Long;
            }
            if (ViTriTB) {
                ViTri = Medium;
            }
            var dem = note.lyric.Length;
            var phoneme = "";
            // 1 kí tự 
            if (dem == 1) {
                phoneme = $"- {note.lyric}";
                phoneme = phoneme.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                             .Replace("ư", "U").Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh")
                             .Replace("Z", "tr").Replace("T", "th");
            }
            // 2 kí tự CV, ví dụ: "ba"
            if ((dem == 2) && tontaiC) {
                phoneme = $"- {note.lyric}";
                phoneme = phoneme.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                             .Replace("ư", "U").Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh")
                             .Replace("Z", "tr").Replace("T", "th");
            }
            // 2 kí tự VV, ví dụ: "oa"
            if ((dem == 2) && kocoC && kocoCcuoi) {
                string V1 = note.lyric.Substring(0, 1);
                string V2 = note.lyric.Substring(1, 1);
                V1 = V1.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                     .Replace("ư", "U");
                V2 = V2.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                             .Replace("ư", "U").Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh")
                             .Replace("Z", "tr").Replace("T", "th");
                a = (note.lyric.EndsWith("ia") || note.lyric.EndsWith("ua") || note.lyric.EndsWith("ưa") || note.lyric.EndsWith("uya"));
                if (a) {
                    V2 = "A";
                }
                if (prevNeighbour == null) {
                    return new Result {
                        phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"- {V1}"  },
                            new Phoneme { phoneme = $"{V1} {V2}", position = ViTri  },

                        }
                    };
                }
            }
            // 2 kí tự VC, ví dụ "át"
            if ((dem == 2) && tontaiCcuoi) {
                string V1 = note.lyric.Substring(0, 1);
                string V2 = note.lyric.Substring(1, 1);
                V1 = V1.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                     .Replace("ư", "U");
                V2 = V2.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                     .Replace("ư", "U");
                if (prevNeighbour == null) {
                    return new Result {
                        phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"- {V1}"  },
                            new Phoneme { phoneme = $"{V1}{V2}", position = ViTri  },

                        }
                    };
                }
            }
            // 3 kí tự VVC chia 3 nốt, ví dụ: "oát"
            if ((dem == 3) && tontaiCcuoi && koVVCchia && kocoC) {
                string V1 = note.lyric.Substring(0, 1);
                string V2 = note.lyric.Substring(1, 1);
                string VC = note.lyric.Substring(1);
                V1 = V1.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                       .Replace("ư", "U");
                V2 = V2.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                       .Replace("ư", "U");
                VC = VC.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                       .Replace("ư", "U").Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh")
                       .Replace("Z", "tr").Replace("T", "th");
                if (ViTriDai) {
                    ViTri = Medium;
                }
                if (prevNeighbour == null) {
                    return new Result {
                        phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"- {V1}"  },
                            new Phoneme { phoneme = $"{V1} {V2}", position = Long  },
                            new Phoneme { phoneme = $"{VC}", position = ViTri  },
                        }
                    };
                }
            }
            // 3 kí tự VVV chia 3 nốt, ví dụ: "oan" "oai"
            if ((dem == 3) && koVVCchia && kocoC) {
                string V1 = note.lyric.Substring(0, 1);
                string V2 = note.lyric.Substring(1, 1);
                string V3 = note.lyric.Substring(2);
                V1 = V1.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                       .Replace("ư", "U");
                V2 = V2.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                       .Replace("ư", "U");
                V3 = V3.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                       .Replace("ư", "U").Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh")
                       .Replace("Z", "tr").Replace("T", "th");
                if (ViTriNgan) {
                    ViTri = Short;
                } else {
                    ViTri = Medium;
                }
                if (prevNeighbour == null) {
                    return new Result {
                        phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"- {V1}"  },
                            new Phoneme { phoneme = $"{V1} {V2}", position = Long  },
                            new Phoneme { phoneme = $"{V2} {V3}", position = ViTri  },
                        }
                    };
                }
            }
            // 3 kí tự VVV/VVC chia 2 nốt, ví dụ: "yên" "ướt"
            if ((dem == 3) && tontaiVVC && kocoC) {
                string V1 = note.lyric.Substring(0, 1);
                string VVC = note.lyric.Substring(0);
                V1 = V1.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                       .Replace("ư", "U");
                VVC = VVC.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                       .Replace("ư", "U").Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh")
                       .Replace("Z", "tr").Replace("T", "th");
                if (prevNeighbour == null) {
                    return new Result {
                        phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"- {V1}"  },
                            new Phoneme { phoneme = $"{VVC}", position = ViTri  },
                        }
                    };
                }
            }
            // 3 kí tự CVC, ví dụ: "hát"
            if (dem == 3 && tontaiC && tontaiCcuoi) {
                string C = note.lyric.Substring(0, 1);
                string V1 = note.lyric.Substring(1, 1);
                string V2 = note.lyric.Substring(2);
                C = C.Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh").Replace("Z", "tr").Replace("T", "th");
                V1 = V1.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U");
                V2 = V2.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U");
                if (prevNeighbour == null) {
                    return new Result {
                        phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"- {C}{V1}"  },
                            new Phoneme { phoneme = $"{V1}{V2}", position = ViTri  },

                        }
                    };
                }
            }
            // 3 kí tự CVV, ví dụ: "hoa"
            if (dem == 3 && tontaiC && kocoCcuoi) {
                string C = note.lyric.Substring(0, 1);
                string V1 = note.lyric.Substring(1, 1);
                string V2 = note.lyric.Substring(2);
                C = C.Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh").Replace("Z", "tr").Replace("T", "th");
                V1 = V1.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U");
                V2 = V2.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U")
                    .Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh").Replace("Z", "tr").Replace("T", "th"); ;
                a = (note.lyric.EndsWith("ia") || note.lyric.EndsWith("ua") || note.lyric.EndsWith("ưa") || note.lyric.EndsWith("uya"));
                if (a) {
                    V2 = "A";
                }
                if (prevNeighbour == null) {
                    return new Result {
                        phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"- {C}{V1}"  },
                            new Phoneme { phoneme = $"{V1} {V2}", position = ViTri  },

                        }
                    };
                }
            }
            // 4 kí tự VVVC có VVC liền, chia 3 nốt, ví dụ "uyết" "uyên"
            if (dem == 4 && kocoC && tontaiVVC) {
                string V1 = note.lyric.Substring(0, 1);
                string V2 = note.lyric.Substring(1, 1);
                string VVC = note.lyric.Substring(1);
                V1 = V1.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U");
                V2 = V2.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U");
                VVC = VVC.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                             .Replace("ư", "U").Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh")
                             .Replace("Z", "tr").Replace("T", "th");
                if (ViTriNgan) {
                    ViTri = Short;
                } else {
                    ViTri = Medium;
                }
                if (prevNeighbour == null) {
                    return new Result {
                        phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"- {V1}"  },
                            new Phoneme { phoneme = $"{V1} {V2}", position = Long  },
                            new Phoneme { phoneme = $"{VVC}", position = ViTri  },
                        }
                    };
                }
            }
            // 4 kí tự CVVC, có VVC liền, chia 2 nốt, ví dụ "thiết" "tiên"
            if (dem == 4 && tontaiVVC && tontaiC) {
                string C = note.lyric.Substring(0, 1);
                string V1 = note.lyric.Substring(1, 1);
                string VVC = note.lyric.Substring(1);
                C = C.Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh").Replace("Z", "tr").Replace("T", "th");
                V1 = V1.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U");
                VVC = VVC.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                             .Replace("ư", "U").Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh")
                             .Replace("Z", "tr").Replace("T", "th");
                if (prevNeighbour == null) {
                    return new Result {
                        phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"- {C}{V1}"  },
                            new Phoneme { phoneme = $"{VVC}", position = ViTri  },
                        }
                    };
                }
            }
            // 4 kí tự CVVC, chia 3 nốt, ví dụ "thoát"
            if (dem == 4 && tontaiC && tontaiCcuoi) {
                string C = note.lyric.Substring(0, 1);
                string V1 = note.lyric.Substring(1, 1);
                string V2 = note.lyric.Substring(2, 1);
                string VC = note.lyric.Substring(2);
                C = C.Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh").Replace("Z", "tr").Replace("T", "th");
                V1 = V1.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U");
                V2 = V2.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U");
                VC = VC.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                             .Replace("ư", "U").Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh")
                             .Replace("Z", "tr").Replace("T", "th");
                if (ViTriNgan) {
                    ViTri = Short;
                } else {
                    ViTri = Medium;
                }
                if (prevNeighbour == null) {
                    return new Result {
                        phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"- {C}{V1}"  },
                            new Phoneme { phoneme = $"{V1} {V2}", position = Long  },
                            new Phoneme { phoneme = $"{VC}", position = ViTri  },
                        }
                    };
                }
            }
            // 4 kí tự CVVV, chia 3 nốt, ví dụ "ngoại"
            if (dem == 4 && kocoCcuoi && tontaiC) {
                string C = note.lyric.Substring(0, 1);
                string V1 = note.lyric.Substring(1, 1);
                string V2 = note.lyric.Substring(2, 1);
                string V3 = note.lyric.Substring(3);
                C = C.Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh").Replace("Z", "tr").Replace("T", "th");
                V1 = V1.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U");
                V2 = V2.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U");
                V3 = V3.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U");
                if (ViTriNgan) {
                    ViTri = Short;
                } else {
                    ViTri = Medium;
                }
                if (prevNeighbour == null) {
                    return new Result {
                        phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"- {C}{V1}"  },
                            new Phoneme { phoneme = $"{V1} {V2}", position = Long  },
                            new Phoneme { phoneme = $"{V2} {V3}", position = ViTri  },
                        }
                    };
                }
            }
            // 5 kí tự CVVVC, có VVC liền, chia 3 nốt, ví dụ "thuyết"
            if (dem == 5 && tontaiVVC && tontaiC) {
                string C = note.lyric.Substring(0, 1);
                string V1 = note.lyric.Substring(1, 1);
                string V2 = note.lyric.Substring(2, 1);
                string VVC = note.lyric.Substring(2);
                C = C.Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh").Replace("Z", "tr").Replace("T", "th");
                V1 = V1.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U");
                V2 = V2.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U");
                VVC = VVC.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                             .Replace("ư", "U").Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh")
                             .Replace("Z", "tr").Replace("T", "th");
                if (ViTriNgan) {
                    ViTri = Short;
                } else {
                    ViTri = Medium;
                }
                if (prevNeighbour == null) {
                    return new Result {
                        phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"- {C}{V1}"  },
                            new Phoneme { phoneme = $"{V1} {V2}", position = Long  },
                            new Phoneme { phoneme = $"{VVC}", position = ViTri  },
                        }
                    };
                }
            }
            phoneme = phoneme.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                             .Replace("ư", "U").Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh")
                             .Replace("Z", "tr").Replace("T", "th");

            if (prevNeighbour != null) {
                var lyric = prevNeighbour?.phoneticHint ?? prevNeighbour?.lyric;
                var unicode = ToUnicodeElements(lyric);
                if (vowelLookup.TryGetValue(unicode.LastOrDefault() ?? string.Empty, out var vow)) {
                    bool qua;
                    string PR = prevNeighbour?.lyric;
                    qua = prevNeighbour?.lyric == "qua";
                    bool notqua;
                    if (qua == true) {
                        notqua = false;
                    } else notqua = true;
                    a = (PR.EndsWith("ua") || PR.EndsWith("ưa") || PR.EndsWith("ia") || PR.EndsWith("uya") && notqua);
                    if (a) {
                        vow = "A";
                    }
                    a = (PR.EndsWith("ung") || PR.EndsWith("ông") || PR.EndsWith("ong"));
                    if (a) {
                        vow = "m";
                    }

                    // 1 kí tự 
                    if (dem == 1) {
                        phoneme = $"{vow} {note.lyric}";
                        phoneme = phoneme.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                                     .Replace("ư", "U").Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh")
                                     .Replace("Z", "tr").Replace("T", "th");
                    }
                    // 2 kí tự CV, ví dụ: "ba"
                    if ((dem == 2) && tontaiC) {
                        phoneme = $"{vow} {note.lyric}";
                        phoneme = phoneme.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                                     .Replace("ư", "U").Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh")
                                     .Replace("Z", "tr").Replace("T", "th");
                    }
                    // 2 kí tự VV, ví dụ: "oa"
                    if ((dem == 2) && kocoC && kocoCcuoi) {
                        string V1 = note.lyric.Substring(0, 1);
                        string V2 = note.lyric.Substring(1, 1);
                        V1 = V1.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                             .Replace("ư", "U");
                        V2 = V2.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                                     .Replace("ư", "U").Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh")
                                     .Replace("Z", "tr").Replace("T", "th");
                        a = (note.lyric.EndsWith("ia") || note.lyric.EndsWith("ua") || note.lyric.EndsWith("ưa") || note.lyric.EndsWith("uya"));
                        if (a) {
                            V2 = "A";
                        }
                            return new Result {
                                phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{vow} {V1}"  },
                            new Phoneme { phoneme = $"{V1} {V2}", position = ViTri  },

                        }
                            };
                    }
                    // 2 kí tự VC, ví dụ "át"
                    if ((dem == 2) && tontaiCcuoi) {
                        string V1 = note.lyric.Substring(0, 1);
                        string V2 = note.lyric.Substring(1, 1);
                        V1 = V1.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                             .Replace("ư", "U");
                        V2 = V2.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                             .Replace("ư", "U");
                            return new Result {
                                phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{vow} {V1}"  },
                            new Phoneme { phoneme = $"{V1}{V2}", position = ViTri  },

                        }
                            };
                    }
                    // 3 kí tự VVC chia 3 nốt, ví dụ: "oát"
                    if ((dem == 3) && tontaiCcuoi && koVVCchia && kocoC) {
                        string V1 = note.lyric.Substring(0, 1);
                        string V2 = note.lyric.Substring(1, 1);
                        string VC = note.lyric.Substring(1);
                        V1 = V1.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                               .Replace("ư", "U");
                        V2 = V2.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                               .Replace("ư", "U");
                        VC = VC.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                               .Replace("ư", "U").Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh")
                               .Replace("Z", "tr").Replace("T", "th");
                        if (ViTriDai) {
                            ViTri = Medium;
                        }
                            return new Result {
                                phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{vow} {V1}"  },
                            new Phoneme { phoneme = $"{V1} {V2}", position = Long  },
                            new Phoneme { phoneme = $"{VC}", position = ViTri  },
                        }
                            };
                    }
                    // 3 kí tự VVV chia 3 nốt, ví dụ: "oan" "oai"
                    if ((dem == 3) && koVVCchia && kocoC) {
                        string V1 = note.lyric.Substring(0, 1);
                        string V2 = note.lyric.Substring(1, 1);
                        string V3 = note.lyric.Substring(2);
                        V1 = V1.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                               .Replace("ư", "U");
                        V2 = V2.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                               .Replace("ư", "U");
                        V3 = V3.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                               .Replace("ư", "U").Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh")
                               .Replace("Z", "tr").Replace("T", "th");
                        if (ViTriNgan) {
                            ViTri = Short;
                        } else {
                            ViTri = Medium;
                        }
                            return new Result {
                                phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{vow} {V1}"  },
                            new Phoneme { phoneme = $"{V1} {V2}", position = Long  },
                            new Phoneme { phoneme = $"{V2} {V3}", position = ViTri  },
                        }
                            };
                    }
                    // 3 kí tự VVV/VVC chia 2 nốt, ví dụ: "yên" "ướt"
                    if ((dem == 3) && tontaiVVC && kocoC) {
                        string V1 = note.lyric.Substring(0, 1);
                        string VVC = note.lyric.Substring(0);
                        V1 = V1.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                               .Replace("ư", "U");
                        VVC = VVC.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                               .Replace("ư", "U").Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh")
                               .Replace("Z", "tr").Replace("T", "th");
                            return new Result {
                                phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{vow} {V1}"  },
                            new Phoneme { phoneme = $"{VVC}", position = ViTri  },
                        }
                            };
                    }
                    // 3 kí tự CVC, ví dụ: "hát"
                    if (dem == 3 && tontaiC && tontaiCcuoi) {
                        string C = note.lyric.Substring(0, 1);
                        string V1 = note.lyric.Substring(1, 1);
                        string V2 = note.lyric.Substring(2);
                        C = C.Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh").Replace("Z", "tr").Replace("T", "th");
                        V1 = V1.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U");
                        V2 = V2.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U");
                            return new Result {
                                phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{vow} {C}{V1}"  },
                            new Phoneme { phoneme = $"{V1}{V2}", position = ViTri  },

                        }
                            };
                    }
                    // 3 kí tự CVV, ví dụ: "hoa"
                    if (dem == 3 && tontaiC && kocoCcuoi) {
                        string C = note.lyric.Substring(0, 1);
                        string V1 = note.lyric.Substring(1, 1);
                        string V2 = note.lyric.Substring(2);
                        C = C.Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh").Replace("Z", "tr").Replace("T", "th");
                        V1 = V1.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U");
                        V2 = V2.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U")
                            .Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh").Replace("Z", "tr").Replace("T", "th"); ;
                        a = (note.lyric.EndsWith("ia") || note.lyric.EndsWith("ua") || note.lyric.EndsWith("ưa") || note.lyric.EndsWith("uya"));
                        if (a) {
                            V2 = "A";
                        }
                            return new Result {
                                phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{vow} {C}{V1}"  },
                            new Phoneme { phoneme = $"{V1} {V2}", position = ViTri  },

                        }
                            };
                    }
                    // 4 kí tự VVVC có VVC liền, chia 3 nốt, ví dụ "uyết" "uyên"
                    if (dem == 4 && kocoC && tontaiVVC) {
                        string V1 = note.lyric.Substring(0, 1);
                        string V2 = note.lyric.Substring(1, 1);
                        string VVC = note.lyric.Substring(1);
                        V1 = V1.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U");
                        V2 = V2.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U");
                        VVC = VVC.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                                     .Replace("ư", "U").Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh")
                                     .Replace("Z", "tr").Replace("T", "th");
                        if (ViTriNgan) {
                            ViTri = Short;
                        } else {
                            ViTri = Medium;
                        }
                            return new Result {
                                phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{vow} {V1}"  },
                            new Phoneme { phoneme = $"{V1} {V2}", position = Long  },
                            new Phoneme { phoneme = $"{VVC}", position = ViTri  },
                        }
                            };
                    }
                    // 4 kí tự CVVC, có VVC liền, chia 2 nốt, ví dụ "thiết" "tiên"
                    if (dem == 4 && tontaiVVC && tontaiC) {
                        string C = note.lyric.Substring(0, 1);
                        string V1 = note.lyric.Substring(1, 1);
                        string VVC = note.lyric.Substring(1);
                        C = C.Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh").Replace("Z", "tr").Replace("T", "th");
                        V1 = V1.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U");
                        VVC = VVC.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                                     .Replace("ư", "U").Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh")
                                     .Replace("Z", "tr").Replace("T", "th");
                            return new Result {
                                phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{vow} {C}{V1}"  },
                            new Phoneme { phoneme = $"{VVC}", position = ViTri  },
                        }
                            };
                    }
                    // 4 kí tự CVVC, chia 3 nốt, ví dụ "thoát"
                    if (dem == 4 && tontaiC && tontaiCcuoi) {
                        string C = note.lyric.Substring(0, 1);
                        string V1 = note.lyric.Substring(1, 1);
                        string V2 = note.lyric.Substring(2, 1);
                        string VC = note.lyric.Substring(2);
                        C = C.Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh").Replace("Z", "tr").Replace("T", "th");
                        V1 = V1.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U");
                        V2 = V2.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U");
                        VC = VC.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                                     .Replace("ư", "U").Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh")
                                     .Replace("Z", "tr").Replace("T", "th");
                        if (ViTriNgan) {
                            ViTri = Short;
                        } else {
                            ViTri = Medium;
                        }
                            return new Result {
                                phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{vow} {C}{V1}"  },
                            new Phoneme { phoneme = $"{V1} {V2}", position = Long  },
                            new Phoneme { phoneme = $"{VC}", position = ViTri  },
                        }
                            };
                    }
                    // 4 kí tự CVVV, chia 3 nốt, ví dụ "ngoại"
                    if (dem == 4 && kocoCcuoi && tontaiC) {
                        string C = note.lyric.Substring(0, 1);
                        string V1 = note.lyric.Substring(1, 1);
                        string V2 = note.lyric.Substring(2, 1);
                        string V3 = note.lyric.Substring(3);
                        C = C.Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh").Replace("Z", "tr").Replace("T", "th");
                        V1 = V1.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U");
                        V2 = V2.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U");
                        V3 = V3.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U");
                        if (ViTriNgan) {
                            ViTri = Short;
                        } else {
                            ViTri = Medium;
                        }
                            return new Result {
                                phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{vow} {C}{V1}"  },
                            new Phoneme { phoneme = $"{V1} {V2}", position = Long  },
                            new Phoneme { phoneme = $"{V2} {V3}", position = ViTri  },
                        }
                            };
                    }
                    // 5 kí tự CVVVC, có VVC liền, chia 3 nốt, ví dụ "thuyết"
                    if (dem == 5 && tontaiVVC && tontaiC) {
                        string C = note.lyric.Substring(0, 1);
                        string V1 = note.lyric.Substring(1, 1);
                        string V2 = note.lyric.Substring(2, 1);
                        string VVC = note.lyric.Substring(2);
                        C = C.Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh").Replace("Z", "tr").Replace("T", "th");
                        V1 = V1.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U");
                        V2 = V2.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O").Replace("ư", "U");
                        VVC = VVC.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                                     .Replace("ư", "U").Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh")
                                     .Replace("Z", "tr").Replace("T", "th");
                        if (ViTriNgan) {
                            ViTri = Short;
                        } else {
                            ViTri = Medium;
                        }
                            return new Result {
                                phonemes = new Phoneme[] {
                            new Phoneme { phoneme = $"{vow} {C}{V1}"  },
                            new Phoneme { phoneme = $"{V1} {V2}", position = Long  },
                            new Phoneme { phoneme = $"{VVC}", position = ViTri  },
                        }
                            };
                    }
                    phoneme = phoneme.Replace("ă", "a").Replace("â", "A").Replace("ơ", "@").Replace("y", "i").Replace("ê", "E").Replace("ô", "O")
                                     .Replace("ư", "U").Replace("C", "ch").Replace("K", "kh").Replace("N", "ng").Replace("J", "nh")
                                     .Replace("Z", "tr").Replace("T", "th");
                }
            }
            // Get color
            string color = string.Empty;
            int toneShift = 0;
            if (note.phonemeAttributes != null) {
                var attr = note.phonemeAttributes.FirstOrDefault(attr => attr.index == 0);
                color = attr.voiceColor;
                toneShift = attr.toneShift;
            }
            if (singer.TryGetMappedOto(phoneme, note.tone + toneShift, color, out var oto)) {
                phoneme = oto.Alias;
            } else {
                phoneme = note.lyric;
            }
            return new Result {
                phonemes = new Phoneme[] {
                    new Phoneme {
                        phoneme = phoneme,
                    }
                },
            };
        }
    }
}
