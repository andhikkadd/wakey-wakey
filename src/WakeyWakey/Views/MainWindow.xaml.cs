using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WakeyWakey.Models;
using WakeyWakey.Services;
using WakeyWakey.Views;

namespace WakeyWakey
{
    public partial class MainWindow : Window
    {
        private List<Alarm> _alarms = new List<Alarm>();

        public MainWindow()
        {
            InitializeComponent();
            this.Title = "WakeyWakey Dashboard"; // Hardcoded to match FindWindow title in Mutex check!
            
            // Clean up any stale Scheduler tasks on startup that might have been deleted from JSON
            SyncTasksWithDatabase();
            
            LoadAndDisplayAlarms();
        }

        private void LoadAndDisplayAlarms()
        {
            _alarms = AlarmStorage.LoadAlarms();
            var displayList = _alarms.Select(a => new AlarmDisplayModel(a)).ToList();
            AlarmsItemsControl.ItemsSource = displayList;
        }

        private void SyncTasksWithDatabase()
        {
            try
            {
                var alarms = AlarmStorage.LoadAlarms();
                
                // Set scheduler in sync with enabled status
                foreach (var alarm in alarms)
                {
                    if (alarm.IsEnabled)
                    {
                        SchedulerService.RegisterAlarm(alarm);
                    }
                    else
                    {
                        SchedulerService.UnregisterAlarm(alarm.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Initial scheduler sync warning: {ex.Message}");
            }
        }

        private void AddAlarm_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AlarmEditDialog();
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                var newAlarm = dialog.AlarmResult;
                _alarms.Add(newAlarm);
                AlarmStorage.SaveAlarms(_alarms);

                // Register with Task Scheduler
                if (newAlarm.IsEnabled)
                {
                    try
                    {
                        SchedulerService.RegisterAlarm(newAlarm);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Scheduler Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                LoadAndDisplayAlarms();
            }
        }

        private void EditAlarm_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Guid alarmId)
            {
                Alarm? alarm = _alarms.FirstOrDefault(a => a.Id == alarmId);
                if (alarm != null)
                {
                    var dialog = new AlarmEditDialog(alarm);
                    dialog.Owner = this;
                    if (dialog.ShowDialog() == true)
                    {
                        AlarmStorage.SaveAlarms(_alarms);

                        // Update in Task Scheduler
                        if (alarm.IsEnabled)
                        {
                            try
                            {
                                SchedulerService.RegisterAlarm(alarm);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message, "Scheduler Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                        else
                        {
                            SchedulerService.UnregisterAlarm(alarm.Id);
                        }

                        LoadAndDisplayAlarms();
                    }
                }
            }
        }

        private void DeleteAlarm_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Guid alarmId)
            {
                var result = MessageBox.Show("Are you sure you want to delete this alarm?", "Delete Alarm", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    // Remove from Task Scheduler
                    SchedulerService.UnregisterAlarm(alarmId);

                    // Remove from List
                    _alarms.RemoveAll(a => a.Id == alarmId);
                    AlarmStorage.SaveAlarms(_alarms);

                    LoadAndDisplayAlarms();
                }
            }
        }

        private void ToggleAlarm_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb && cb.Tag is Guid alarmId)
            {
                Alarm? alarm = _alarms.FirstOrDefault(a => a.Id == alarmId);
                if (alarm != null)
                {
                    alarm.IsEnabled = cb.IsChecked == true;
                    AlarmStorage.SaveAlarms(_alarms);

                    try
                    {
                        if (alarm.IsEnabled)
                        {
                            SchedulerService.RegisterAlarm(alarm);
                        }
                        else
                        {
                            SchedulerService.UnregisterAlarm(alarm.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Scheduler Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        // Revert check state in UI
                        cb.IsChecked = !cb.IsChecked;
                        alarm.IsEnabled = cb.IsChecked == true;
                        AlarmStorage.SaveAlarms(_alarms);
                    }

                    LoadAndDisplayAlarms();
                }
            }
        }

        private void TestTrigger_Click(object sender, RoutedEventArgs e)
        {
            // Launch the alarm window in Test mode
            var testWindow = new AlarmWindow(Guid.Empty, isTestMode: true);
            testWindow.Owner = this;
            testWindow.ShowDialog();
            
            // Refresh main list when returning (custom sounds might have been validated/invalidated)
            LoadAndDisplayAlarms();
        }

        private void OpenPowerOptions_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("control.exe", "/name Microsoft.PowerOptions") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open Power Options: {ex.Message}", "System Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
