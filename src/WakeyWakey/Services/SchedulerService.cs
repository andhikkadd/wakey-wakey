using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32.TaskScheduler;
using WakeyWakey.Models;

namespace WakeyWakey.Services
{
    public class SchedulerService
    {
        public static void RegisterAlarm(Alarm alarm)
        {
            try
            {
                using (TaskService ts = new TaskService())
                {
                    string taskName = $"WakeyWakey_Alarm_{alarm.Id}";

                    // If task already exists, delete it first to recreate
                    if (ts.GetTask(taskName) != null)
                    {
                        ts.RootFolder.DeleteTask(taskName);
                    }

                    if (!alarm.IsEnabled)
                    {
                        return; // Disabled alarms don't need a scheduled task
                    }

                    TaskDefinition td = ts.NewTask();
                    td.RegistrationInfo.Description = $"WakeyWakey: {alarm.Label}";

                    // Set action to run the application with the --trigger-alarm argument
                    string exePath = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "WakeyWakey.exe");
                    
                    // Make sure path is wrapped in quotes in case it contains spaces
                    td.Actions.Add(new ExecAction($"\"{exePath}\"", $"--trigger-alarm {alarm.Id}"));

                    // Setup trigger
                    if (alarm.IsRepeating)
                    {
                        var weeklyTrigger = new WeeklyTrigger();
                        weeklyTrigger.DaysOfWeek = MapDaysOfWeek(alarm.RepeatDays);
                        
                        // Set start boundary to today at the alarm's time
                        DateTime start = DateTime.Today.AddHours(alarm.Hour).AddMinutes(alarm.Minute);
                        weeklyTrigger.StartBoundary = start;
                        
                        td.Triggers.Add(weeklyTrigger);
                    }
                    else
                    {
                        var timeTrigger = new TimeTrigger();
                        DateTime nextRun = GetNextOneTimeOccurrence(alarm.Hour, alarm.Minute);
                        timeTrigger.StartBoundary = nextRun;
                        
                        td.Triggers.Add(timeTrigger);
                    }

                    // Crucial Settings to wake computer and ensure reliability
                    td.Settings.WakeToRun = true; // Wake from sleep
                    td.Settings.DisallowStartIfOnBatteries = false; // Run on battery
                    td.Settings.StopIfGoingOnBatteries = false; // Keep running if battery status changes
                    td.Settings.RunOnlyIfNetworkAvailable = false; // Offline execution
                    td.Settings.Enabled = true;
                    td.Settings.ExecutionTimeLimit = TimeSpan.FromHours(2); // Auto stop after 2 hours safety limit
                    td.Settings.Priority = ProcessPriorityClass.High;

                    // Register task in Root Folder
                    ts.RootFolder.RegisterTaskDefinition(taskName, td);
                    Debug.WriteLine($"Successfully registered task: {taskName}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to register task in Task Scheduler: {ex.Message}");
                // In a real desktop app, we can throw or handle. We'll handle gracefully but log it.
                throw new InvalidOperationException("Failed to register alarm with Windows Task Scheduler. Please check if you have permissions.", ex);
            }
        }

        public static void UnregisterAlarm(Guid alarmId)
        {
            try
            {
                using (TaskService ts = new TaskService())
                {
                    string taskName = $"WakeyWakey_Alarm_{alarmId}";
                    if (ts.GetTask(taskName) != null)
                    {
                        ts.RootFolder.DeleteTask(taskName);
                        Debug.WriteLine($"Successfully deleted task: {taskName}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to unregister task: {ex.Message}");
            }
        }

        public static DateTime GetNextOneTimeOccurrence(int hour, int minute)
        {
            DateTime nextRun = DateTime.Today.AddHours(hour).AddMinutes(minute);
            if (nextRun <= DateTime.Now)
            {
                nextRun = nextRun.AddDays(1);
            }
            return nextRun;
        }

        public static DateTime GetNextRepeatingOccurrence(Alarm alarm)
        {
            DateTime now = DateTime.Now;
            DateTime baseToday = DateTime.Today.AddHours(alarm.Hour).AddMinutes(alarm.Minute);
            
            // Loop through next 7 days to find the closest matching day
            for (int i = 0; i < 8; i++)
            {
                DateTime candidate = baseToday.AddDays(i);
                if (candidate > now && IsDaySelected(alarm.RepeatDays, candidate.DayOfWeek))
                {
                    return candidate;
                }
            }
            
            // Fallback to tomorrow if none matches (should not happen for repeating)
            return now.AddDays(1);
        }

        private static bool IsDaySelected(DaysOfWeek repeatDays, DayOfWeek dayOfWeek)
        {
            DaysOfWeek checkFlag = dayOfWeek switch
            {
                DayOfWeek.Monday => DaysOfWeek.Monday,
                DayOfWeek.Tuesday => DaysOfWeek.Tuesday,
                DayOfWeek.Wednesday => DaysOfWeek.Wednesday,
                DayOfWeek.Thursday => DaysOfWeek.Thursday,
                DayOfWeek.Friday => DaysOfWeek.Friday,
                DayOfWeek.Saturday => DaysOfWeek.Saturday,
                DayOfWeek.Sunday => DaysOfWeek.Sunday,
                _ => DaysOfWeek.None
            };
            return (repeatDays & checkFlag) != 0;
        }

        private static DaysOfTheWeek MapDaysOfWeek(DaysOfWeek days)
        {
            DaysOfTheWeek result = 0;
            if ((days & DaysOfWeek.Sunday) != 0) result |= DaysOfTheWeek.Sunday;
            if ((days & DaysOfWeek.Monday) != 0) result |= DaysOfTheWeek.Monday;
            if ((days & DaysOfWeek.Tuesday) != 0) result |= DaysOfTheWeek.Tuesday;
            if ((days & DaysOfWeek.Wednesday) != 0) result |= DaysOfTheWeek.Wednesday;
            if ((days & DaysOfWeek.Thursday) != 0) result |= DaysOfTheWeek.Thursday;
            if ((days & DaysOfWeek.Friday) != 0) result |= DaysOfTheWeek.Friday;
            if ((days & DaysOfWeek.Saturday) != 0) result |= DaysOfTheWeek.Saturday;
            return result;
        }
    }
}
