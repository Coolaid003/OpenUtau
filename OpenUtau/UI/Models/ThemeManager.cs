﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace OpenUtau.UI.Models
{
    class ThemeManager
    {
        static public string[] noteStrings = new String[12] { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

        static int[] blackKeys = { 1, 3, 6, 8, 10 };

        // Window UI

        // Midi editor background
        public static SolidColorBrush TickLineBrushLight = new SolidColorBrush();
        public static SolidColorBrush TickLineBrushDark = new SolidColorBrush();
        public static SolidColorBrush BarNumberBrush = new SolidColorBrush();

        // Midi editor markers
        public static SolidColorBrush PlayPosMarkerHighlightBrush = new SolidColorBrush();
        
        // Midi note
        public static SolidColorBrush[] NoteFillBrushes = new SolidColorBrush[]
        {
            new SolidColorBrush(), new SolidColorBrush(), new SolidColorBrush(), new SolidColorBrush(), new SolidColorBrush(), new SolidColorBrush(), new SolidColorBrush(), new SolidColorBrush()
        };

        public static SolidColorBrush[] NoteStrokeBrushes = new SolidColorBrush[]
        {
            new SolidColorBrush(), new SolidColorBrush(), new SolidColorBrush(), new SolidColorBrush(), new SolidColorBrush(), new SolidColorBrush(), new SolidColorBrush(), new SolidColorBrush()
        };

        public static SolidColorBrush[] NoteFillActiveBrushes = new SolidColorBrush[]
        {
            new SolidColorBrush(), new SolidColorBrush(), new SolidColorBrush(), new SolidColorBrush(), new SolidColorBrush(), new SolidColorBrush(), new SolidColorBrush(), new SolidColorBrush()
        };

        public static SolidColorBrush[] NoteStrokeActiveBrushes = new SolidColorBrush[]
        {
            new SolidColorBrush(), new SolidColorBrush(), new SolidColorBrush(), new SolidColorBrush(), new SolidColorBrush(), new SolidColorBrush(), new SolidColorBrush(), new SolidColorBrush()
        };

        
        public static bool LoadTheme(){
            TickLineBrushLight.Color = (Color)Application.Current.FindResource("TickLineColorLight");
            TickLineBrushDark.Color = (Color)Application.Current.FindResource("TickLineColorDark");
            BarNumberBrush.Color = (Color)Application.Current.FindResource("BarNumberColor");
            
            PlayPosMarkerHighlightBrush.Color = (Color)Application.Current.FindResource("PlayPosMarkerHighlightColor");

            NoteFillBrushes[0].Color = (Color)Application.Current.FindResource("NoteFillColorACh0");
            NoteStrokeBrushes[0].Color = (Color)Application.Current.FindResource("NoteStrokeColorCh0");
            NoteFillActiveBrushes[0].Color = (Color)Application.Current.FindResource("NoteFillActiveColorACh0");
            NoteStrokeActiveBrushes[0].Color = (Color)Application.Current.FindResource("NoteStrokeActiveColorCh0");

            return true;
        }

        static public System.Windows.Media.Brush getNoteBackgroundBrush(int noteNo)
        {
            if (blackKeys.Contains(noteNo % 12)) return (LinearGradientBrush)System.Windows.Application.Current.FindResource("BlackKeyBrushNormal");
            else if (noteNo % 12 == 0) return (LinearGradientBrush)System.Windows.Application.Current.FindResource("CenterKeyBrushNormal");
            else return (LinearGradientBrush)System.Windows.Application.Current.FindResource("WhiteKeyBrushNormal");
        }

        static public System.Windows.Style getKeyStyle(int keyNo)
        {
            if (blackKeys.Contains(keyNo % 12)) return (System.Windows.Style)System.Windows.Application.Current.FindResource("BlackKeyStyle");
            else if (keyNo % 12 == 0) return (System.Windows.Style)System.Windows.Application.Current.FindResource("CenterKeyStyle");
            else return (System.Windows.Style)System.Windows.Application.Current.FindResource("WhiteKeyStyle");
        }

        static public System.Windows.Media.Brush getNoteBrush(int noteNo)
        {
            if (blackKeys.Contains(noteNo % 12)) return (SolidColorBrush)System.Windows.Application.Current.FindResource("BlackKeyNoteBrushNormal");
            else if (noteNo % 12 == 0) return (SolidColorBrush)System.Windows.Application.Current.FindResource("CenterKeyNoteBrushNormal");
            else return (SolidColorBrush)System.Windows.Application.Current.FindResource("WhiteKeyNoteBrushNormal");
        }

        static public System.Windows.Media.Brush getNoteTrackBrush(int noteNo)
        {
            if (blackKeys.Contains(noteNo % 12)) return (SolidColorBrush)System.Windows.Application.Current.FindResource("BlackKeyTrackBrushNormal");
            else return (SolidColorBrush)System.Windows.Application.Current.FindResource("WhiteKeyTrackBrushNormal");
        }

        static public System.Windows.Media.Brush getTickLineBrush()
        {
            return (SolidColorBrush)System.Windows.Application.Current.FindResource("ScrollBarBrushNormal");
        }

        static public System.Windows.Media.Brush getBarNumberBrush()
        {
            return (SolidColorBrush)System.Windows.Application.Current.FindResource("ScrollBarBrushActive");
        }
    }
}
