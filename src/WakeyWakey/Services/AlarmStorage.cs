using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using WakeyWakey.Models;

namespace WakeyWakey.Services
{
    public class AlarmStorage
    {
        private static readonly string AppDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WakeyWakey"
        );

        private static readonly string FilePath = Path.Combine(AppDataPath, "alarms.json");

        public static List<Alarm> LoadAlarms()
        {
            try
            {
                if (!Directory.Exists(AppDataPath))
                {
                    Directory.CreateDirectory(AppDataPath);
                }

                if (!File.Exists(FilePath))
                {
                    return GetDefaultAlarms();
                }

                string json = File.ReadAllText(FilePath);
                var alarms = JsonSerializer.Deserialize<List<Alarm>>(json);
                return alarms ?? GetDefaultAlarms();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading alarms: {ex.Message}");
                return GetDefaultAlarms();
            }
        }

        public static void SaveAlarms(List<Alarm> alarms)
        {
            try
            {
                if (!Directory.Exists(AppDataPath))
                {
                    Directory.CreateDirectory(AppDataPath);
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(alarms, options);
                File.WriteAllText(FilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving alarms: {ex.Message}");
            }
        }

        private static List<Alarm> GetDefaultAlarms()
        {
            return new List<Alarm>
            {
                new Alarm
                {
                    Hour = 7,
                    Minute = 0,
                    Label = "Morning Alarm",
                    RepeatDays = DaysOfWeek.Monday | DaysOfWeek.Tuesday | DaysOfWeek.Wednesday | DaysOfWeek.Thursday | DaysOfWeek.Friday,
                    Difficulty = ChallengeDifficulty.Medium,
                    IsEnabled = false
                }
            };
        }
    }
}
