using System;

namespace WakeyWakey.Models
{
    [Flags]
    public enum DaysOfWeek
    {
        None = 0,
        Monday = 1,
        Tuesday = 2,
        Wednesday = 4,
        Thursday = 8,
        Friday = 16,
        Saturday = 32,
        Sunday = 64,
        All = 127
    }

    public enum ChallengeDifficulty
    {
        Easy,
        Medium,
        Hard
    }

    public class Alarm
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public int Hour { get; set; }
        public int Minute { get; set; }
        public DaysOfWeek RepeatDays { get; set; } = DaysOfWeek.None;
        public string Label { get; set; } = "Alarm";
        public bool IsEnabled { get; set; } = true;
        public ChallengeDifficulty Difficulty { get; set; } = ChallengeDifficulty.Easy;
        public string? SoundFilePath { get; set; }

        private int _challengeRequiredStreak = 0;
        public int ChallengeRequiredStreak
        {
            get
            {
                if (_challengeRequiredStreak <= 0)
                {
                    return Difficulty switch
                    {
                        ChallengeDifficulty.Easy => 2,
                        ChallengeDifficulty.Medium => 3,
                        ChallengeDifficulty.Hard => 3,
                        _ => 3
                    };
                }
                return _challengeRequiredStreak;
            }
            set => _challengeRequiredStreak = value;
        }

        public bool IsRepeating => RepeatDays != DaysOfWeek.None;

        public override string ToString()
        {
            return $"{Hour:D2}:{Minute:D2} - {Label}";
        }
    }
}
