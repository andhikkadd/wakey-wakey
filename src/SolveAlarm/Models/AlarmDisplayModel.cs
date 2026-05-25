using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using SolveAlarm.Models;
using SolveAlarm.Services;

namespace SolveAlarm.Models
{
    public class AlarmDisplayModel
    {
        private readonly Alarm _alarm;

        public AlarmDisplayModel(Alarm alarm)
        {
            _alarm = alarm;
        }

        public Guid Id => _alarm.Id;

        public bool IsEnabled
        {
            get => _alarm.IsEnabled;
            set => _alarm.IsEnabled = value;
        }

        public string StringTime => $"{_alarm.Hour:D2}:{_alarm.Minute:D2}";
        
        public string Label => _alarm.Label;

        public string RepeatDaysText
        {
            get
            {
                if (_alarm.RepeatDays == DaysOfWeek.None) return "Once";
                if (_alarm.RepeatDays == DaysOfWeek.All) return "Everyday";

                var list = new List<string>();
                if ((_alarm.RepeatDays & DaysOfWeek.Monday) != 0) list.Add("Mon");
                if ((_alarm.RepeatDays & DaysOfWeek.Tuesday) != 0) list.Add("Tue");
                if ((_alarm.RepeatDays & DaysOfWeek.Wednesday) != 0) list.Add("Wed");
                if ((_alarm.RepeatDays & DaysOfWeek.Thursday) != 0) list.Add("Thu");
                if ((_alarm.RepeatDays & DaysOfWeek.Friday) != 0) list.Add("Fri");
                if ((_alarm.RepeatDays & DaysOfWeek.Saturday) != 0) list.Add("Sat");
                if ((_alarm.RepeatDays & DaysOfWeek.Sunday) != 0) list.Add("Sun");

                return string.Join(" ", list);
            }
        }

        public bool IsSoundInvalid
        {
            get
            {
                if (string.IsNullOrEmpty(_alarm.SoundFilePath)) return false;
                return !File.Exists(_alarm.SoundFilePath) || !AudioService.IsSupportedFormat(_alarm.SoundFilePath);
            }
        }

        public string DifficultyText => _alarm.Difficulty.ToString().ToUpper();

        public Brush DifficultyBrush
        {
            get
            {
                return _alarm.Difficulty switch
                {
                    ChallengeDifficulty.Easy => new SolidColorBrush(Color.FromArgb(40, 16, 185, 129)),   // Glassy Green
                    ChallengeDifficulty.Medium => new SolidColorBrush(Color.FromArgb(40, 245, 158, 11)), // Glassy Amber
                    ChallengeDifficulty.Hard => new SolidColorBrush(Color.FromArgb(40, 239, 68, 68)),    // Glassy Red
                    _ => Brushes.Transparent
                };
            }
        }

        public Brush DifficultyTextBrush
        {
            get
            {
                return _alarm.Difficulty switch
                {
                    ChallengeDifficulty.Easy => new SolidColorBrush(Color.FromRgb(16, 185, 129)),   // Green
                    ChallengeDifficulty.Medium => new SolidColorBrush(Color.FromRgb(245, 158, 11)), // Amber
                    ChallengeDifficulty.Hard => new SolidColorBrush(Color.FromRgb(239, 68, 68)),    // Red
                    _ => Brushes.White
                };
            }
        }
    }
}
