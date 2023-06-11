﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Markup.Xaml.MarkupExtensions;
using OpenUtau.Audio;
using OpenUtau.Classic;
using OpenUtau.Core;
using OpenUtau.App.Views;
using OpenUtau.Core.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace OpenUtau.App.ViewModels {
    class NoteDefaultsViewModel : ViewModelBase {

        [Reactive] public string DefaultLyric { get; set; }
        [Reactive] public int CurrentPortamentoLength { get; set; }
        [Reactive] public int CurrentPortamentoStart { get; set; }
        [Reactive] public float CurrentVibratoLength { get; set; }
        [Reactive] public float CurrentVibratoPeriod { get; set; }
        [Reactive] public float CurrentVibratoDepth { get; set; }
        [Reactive] public float CurrentVibratoIn { get; set; }
        [Reactive] public float CurrentVibratoOut { get; set; }
        [Reactive] public float CurrentVibratoShift { get; set; }
        [Reactive] public float AutoVibratoNoteLength { get; set; }
        [Reactive] public bool AutoVibratoToggle { get; set; }
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

        public bool IsPortamentoApplied => appliedPortamentoPreset != null;
        public bool IsVibratoApplied => appliedVibratoPreset != null;
        public NoteDefaultsViewModel() {
            DefaultLyric = NotePresets.Default.DefaultLyric;
            CurrentPortamentoLength = NotePresets.Default.DefaultPortamento.PortamentoLength;
            CurrentPortamentoStart = NotePresets.Default.DefaultPortamento.PortamentoStart;
            CurrentVibratoLength = NotePresets.Default.DefaultVibrato.VibratoLength;
            CurrentVibratoPeriod = NotePresets.Default.DefaultVibrato.VibratoPeriod;
            CurrentVibratoDepth = NotePresets.Default.DefaultVibrato.VibratoDepth;
            CurrentVibratoIn = NotePresets.Default.DefaultVibrato.VibratoIn;
            CurrentVibratoOut = NotePresets.Default.DefaultVibrato.VibratoOut;
            CurrentVibratoShift = NotePresets.Default.DefaultVibrato.VibratoShift;
            AutoVibratoNoteLength = NotePresets.Default.AutoVibratoNoteDuration;
            AutoVibratoToggle = NotePresets.Default.AutoVibratoToggle;
            PortamentoPresets = NotePresets.Default.PortamentoPresets;
            VibratoPresets = NotePresets.Default.VibratoPresets;

            this.WhenAnyValue(vm => vm.DefaultLyric)
                    .Subscribe(defaultLyric => {
                        NotePresets.Default.DefaultLyric = defaultLyric;
                        NotePresets.Save();
                    });
            this.WhenAnyValue(vm => vm.CurrentPortamentoLength)
                    .Subscribe(portamentoLength => {
                        NotePresets.Default.DefaultPortamento.PortamentoLength = portamentoLength;
                        NotePresets.Save();
                    });
            this.WhenAnyValue(vm => vm.CurrentPortamentoStart)
                    .Subscribe(portamentoStart => {
                        NotePresets.Default.DefaultPortamento.PortamentoStart = portamentoStart;
                        NotePresets.Save();
                    });
            this.WhenAnyValue(vm => vm.CurrentVibratoLength)
                    .Subscribe(vibratoLength => {
                        NotePresets.Default.DefaultVibrato.VibratoLength = Math.Max(0, Math.Min(100, vibratoLength));
                        NotePresets.Save();
                    });
            this.WhenAnyValue(vm => vm.CurrentVibratoPeriod)
                    .Subscribe(vibratoPeriod => {
                        NotePresets.Default.DefaultVibrato.VibratoPeriod = Math.Max(5, Math.Min(500, vibratoPeriod));
                        NotePresets.Save();
                    });
            this.WhenAnyValue(vm => vm.CurrentVibratoDepth)
                    .Subscribe(vibratoDepth => {
                        NotePresets.Default.DefaultVibrato.VibratoDepth = Math.Max(5, Math.Min(200, vibratoDepth));
                        NotePresets.Save();
                    });
            this.WhenAnyValue(vm => vm.CurrentVibratoIn)
                    .Subscribe(vibratoIn => {
                        NotePresets.Default.DefaultVibrato.VibratoIn = Math.Max(0, Math.Min(100, vibratoIn));
                        CurrentVibratoOut = (float)Math.Round(Math.Min(NotePresets.Default.DefaultVibrato.VibratoOut, 100 - NotePresets.Default.DefaultVibrato.VibratoIn), 1);
                        NotePresets.Default.DefaultVibrato.VibratoOut = CurrentVibratoOut;
                        NotePresets.Save();
                    });
            this.WhenAnyValue(vm => vm.CurrentVibratoOut)
                    .Subscribe(vibratoOut => {
                        NotePresets.Default.DefaultVibrato.VibratoOut = Math.Max(0, Math.Min(100, vibratoOut));
                        CurrentVibratoIn = (float)Math.Round(Math.Min(NotePresets.Default.DefaultVibrato.VibratoIn, 100 - NotePresets.Default.DefaultVibrato.VibratoOut), 1);
                        NotePresets.Default.DefaultVibrato.VibratoIn = CurrentVibratoIn;
                        NotePresets.Save();
                    });
            this.WhenAnyValue(vm => vm.CurrentVibratoShift)
                    .Subscribe(vibratoShift => {
                        NotePresets.Default.DefaultVibrato.VibratoShift = Math.Max(0, Math.Min(100, vibratoShift));
                        NotePresets.Save();
                    });
            this.WhenAnyValue(vm => vm.AutoVibratoToggle)
                    .Subscribe(autoVibratoToggle => {
                        NotePresets.Default.AutoVibratoToggle = autoVibratoToggle;
                        NotePresets.Save();
                    });
            this.WhenAnyValue(vm => vm.AutoVibratoNoteLength)
                    .Subscribe(autoVibratoNoteLength => {
                        NotePresets.Default.AutoVibratoNoteDuration = (int) Math.Max(10, autoVibratoNoteLength);
                        NotePresets.Save();
                    });
            this.WhenAnyValue(vm => vm.ApplyPortamentoPreset)
                .WhereNotNull()
                .Subscribe(portamentoPreset => {
                    if (portamentoPreset != null) {
                        CurrentPortamentoLength = portamentoPreset.PortamentoLength;
                        CurrentPortamentoStart = portamentoPreset.PortamentoStart;
                        NotePresets.Default.DefaultPortamento.PortamentoLength = CurrentPortamentoLength;
                        NotePresets.Default.DefaultPortamento.PortamentoStart = CurrentPortamentoStart;
                        NotePresets.Save();
                    }
                });
            this.WhenAnyValue(vm => vm.ApplyVibratoPreset)
                .WhereNotNull()
                .Subscribe(vibratoPreset => {
                    if (vibratoPreset != null) {
                        CurrentVibratoLength = Math.Max(0, Math.Min(100, vibratoPreset.VibratoLength));
                        CurrentVibratoPeriod = Math.Max(5, Math.Min(500, vibratoPreset.VibratoPeriod));
                        CurrentVibratoDepth = Math.Max(5, Math.Min(200, vibratoPreset.VibratoDepth));
                        CurrentVibratoIn = Math.Max(0, Math.Min(100, vibratoPreset.VibratoIn));
                        CurrentVibratoOut = Math.Max(0, Math.Min(100, vibratoPreset.VibratoOut));
                        CurrentVibratoShift = Math.Max(0, Math.Min(100, vibratoPreset.VibratoShift));
                        NotePresets.Default.DefaultVibrato.VibratoLength = CurrentVibratoLength;
                        NotePresets.Default.DefaultVibrato.VibratoPeriod = CurrentVibratoPeriod;
                        NotePresets.Default.DefaultVibrato.VibratoDepth = CurrentVibratoDepth;
                        NotePresets.Default.DefaultVibrato.VibratoIn = CurrentVibratoIn;
                        NotePresets.Default.DefaultVibrato.VibratoOut = CurrentVibratoOut;
                        NotePresets.Default.DefaultVibrato.VibratoShift = CurrentVibratoShift;
                        NotePresets.Save();
                    }
                });
        }

        public void SavePortamentoPreset(string name) {
            if (string.IsNullOrEmpty(name)) {
                return;
            }
            NotePresets.Default.PortamentoPresets.Add(new NotePresets.PortamentoPreset(name, CurrentPortamentoLength, CurrentPortamentoStart));
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
            NotePresets.Default.VibratoPresets.Add(new NotePresets.VibratoPreset(name, CurrentVibratoLength, CurrentVibratoPeriod, CurrentVibratoDepth, CurrentVibratoIn, CurrentVibratoOut, CurrentVibratoShift));
            NotePresets.Save();
        }

        public void RemoveAppliedVibratoPreset() {
            if (appliedVibratoPreset == null) {
                return;
            }
            NotePresets.Default.VibratoPresets.Remove(appliedVibratoPreset);
            NotePresets.Save();
        }

        public void ResetSettings() {
            DefaultLyric = NotePresets.Default.DefaultLyric;
            CurrentPortamentoLength = NotePresets.Default.DefaultPortamento.PortamentoLength;
            CurrentPortamentoStart = NotePresets.Default.DefaultPortamento.PortamentoStart;
            CurrentVibratoLength = NotePresets.Default.DefaultVibrato.VibratoLength;
            CurrentVibratoPeriod = NotePresets.Default.DefaultVibrato.VibratoPeriod;
            CurrentVibratoDepth = NotePresets.Default.DefaultVibrato.VibratoDepth;
            CurrentVibratoIn = NotePresets.Default.DefaultVibrato.VibratoIn;
            CurrentVibratoOut = NotePresets.Default.DefaultVibrato.VibratoOut;
            CurrentVibratoShift = NotePresets.Default.DefaultVibrato.VibratoShift;
            AutoVibratoNoteLength = NotePresets.Default.AutoVibratoNoteDuration;
            AutoVibratoToggle = NotePresets.Default.AutoVibratoToggle;
        }
    }
}
