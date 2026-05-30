using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WakeyWakey.Models;
using WakeyWakey.Services;
using WakeyWakey.Views;

namespace WakeyWakey
{
    public partial class MainWindow : Window
    {
        private List<Alarm> _alarms = new List<Alarm>();

        // ── Timer state ─────────────────────────────────────────────────────────
        private DispatcherTimer? _countdownTimer;
        private TimeSpan _timerRemaining;
        private bool _timerRunning = false;

        public MainWindow()
        {
            InitializeComponent();
            this.Title = "WakeyWakey Dashboard";
            SyncTasksWithDatabase();
            LoadAndDisplayAlarms();
        }

        // ═══════════════════════════════════════════════════════════════════════
        //   ALARM FEATURES
        // ═══════════════════════════════════════════════════════════════════════

        public void LoadAndDisplayAlarms()
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
                foreach (var alarm in alarms)
                {
                    if (alarm.IsEnabled)
                        SchedulerService.RegisterAlarm(alarm);
                    else
                        SchedulerService.UnregisterAlarm(alarm.Id);
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
                if (newAlarm.IsEnabled)
                {
                    try { SchedulerService.RegisterAlarm(newAlarm); }
                    catch (Exception ex) { MessageBox.Show(ex.Message, "Scheduler Error", MessageBoxButton.OK, MessageBoxImage.Error); }
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
                        if (alarm.IsEnabled)
                        {
                            try { SchedulerService.RegisterAlarm(alarm); }
                            catch (Exception ex) { MessageBox.Show(ex.Message, "Scheduler Error", MessageBoxButton.OK, MessageBoxImage.Error); }
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
                    SchedulerService.UnregisterAlarm(alarmId);
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
                            SchedulerService.RegisterAlarm(alarm);
                        else
                            SchedulerService.UnregisterAlarm(alarm.Id);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Scheduler Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
            var testWindow = new AlarmWindow(Guid.Empty, isTestMode: true);
            testWindow.Owner = this;
            testWindow.ShowDialog();
            LoadAndDisplayAlarms();
        }

        private void OpenPowerOptions_Click(object sender, RoutedEventArgs e)
        {
            try { Process.Start(new ProcessStartInfo("control.exe", "/name Microsoft.PowerOptions") { UseShellExecute = true }); }
            catch (Exception ex) { MessageBox.Show($"Could not open Power Options: {ex.Message}", "System Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        // ═══════════════════════════════════════════════════════════════════════
        //   TAB SWITCHING
        // ═══════════════════════════════════════════════════════════════════════

        private void AlarmsTab_Click(object sender, MouseButtonEventArgs e)
        {
            AlarmsPanel.Visibility = Visibility.Visible;
            TimerPanel.Visibility = Visibility.Collapsed;
            AlarmActions.Visibility = Visibility.Visible;

            // Active style
            AlarmsTabIndicator.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1B4B"));
            AlarmsTabIndicator.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4338CA"));
            AlarmsTabText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A78BFA"));

            // Inactive style
            TimerTabIndicator.Background = Brushes.Transparent;
            TimerTabIndicator.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B"));
            TimerTabText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569"));
        }

        private void TimerTab_Click(object sender, MouseButtonEventArgs e)
        {
            AlarmsPanel.Visibility = Visibility.Collapsed;
            TimerPanel.Visibility = Visibility.Visible;
            AlarmActions.Visibility = Visibility.Collapsed;

            // Active style
            TimerTabIndicator.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1B4B"));
            TimerTabIndicator.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4338CA"));
            TimerTabText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A78BFA"));

            // Inactive style
            AlarmsTabIndicator.Background = Brushes.Transparent;
            AlarmsTabIndicator.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B"));
            AlarmsTabText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569"));
        }

        // ═══════════════════════════════════════════════════════════════════════
        //   TIMER FEATURES
        // ═══════════════════════════════════════════════════════════════════════

        private void SetTimerDuration(int minutes)
        {
            if (_timerRunning) return; // Don't let presets change while running
            _timerRemaining = TimeSpan.FromMinutes(minutes);
            UpdateTimerDisplay();
            TimerStatusText.Text = $"Set to {minutes} min — ready";
            TimerStatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));
        }

        private void UpdateTimerDisplay()
        {
            TimerCountdownDisplay.Text = _timerRemaining.ToString(@"hh\:mm\:ss");
        }

        private void TimerPreset_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Tag is string tagStr && int.TryParse(tagStr, out int minutes))
                SetTimerDuration(minutes);
        }

        private void TimerSetCustom_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(CustomMinutesBox.Text, out int minutes) && minutes > 0)
                SetTimerDuration(minutes);
            else
                MessageBox.Show("Please enter a valid number of minutes.", "Invalid Duration", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void TimerStart_Click(object sender, RoutedEventArgs e)
        {
            if (_timerRemaining == TimeSpan.Zero)
            {
                MessageBox.Show("Set a duration first using a preset or custom minutes.", "No Duration Set", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_timerRunning)
            {
                // Pause
                _countdownTimer?.Stop();
                _timerRunning = false;
                TimerStartButton.Content = "▶  Resume";
                TimerStatusText.Text = "Paused";
                TimerStatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
            }
            else
            {
                // Start / Resume
                _countdownTimer?.Stop();
                _countdownTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                _countdownTimer.Tick += CountdownTick;
                _countdownTimer.Start();
                _timerRunning = true;
                TimerStartButton.Content = "⏸  Pause";
                TimerStatusText.Text = "Running...";
                TimerStatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
            }
        }

        private void CountdownTick(object? sender, EventArgs e)
        {
            if (_timerRemaining <= TimeSpan.Zero)
            {
                _countdownTimer?.Stop();
                _timerRunning = false;
                TimerStartButton.Content = "▶  Start Timer";
                TimerStatusText.Text = "Time's up!";
                TimerStatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                TimerCountdownDisplay.Text = "00:00:00";
                FireTimerAlert();
                return;
            }

            _timerRemaining -= TimeSpan.FromSeconds(1);
            UpdateTimerDisplay();

            // Color the display red in last 10 seconds
            TimerCountdownDisplay.Foreground = _timerRemaining.TotalSeconds <= 10
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F43F5E"))
                : Brushes.White;
        }

        private void FireTimerAlert()
        {
            var label = string.IsNullOrWhiteSpace(TimerLabelBox.Text) ? "Timer complete!" : TimerLabelBox.Text;
            var difficulty = (ChallengeDifficulty)TimerDifficultyBox.SelectedIndex;
            int streak = TimerStreakBox.SelectedIndex + 1;

            var win = new TimerWindow(label, difficulty, streak);
            win.Owner = this;
            win.ShowDialog();

            // Reset after dismiss
            TimerCountdownDisplay.Foreground = Brushes.White;
            TimerStatusText.Text = "Ready to start";
            TimerStatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));
        }

        private void TimerReset_Click(object sender, RoutedEventArgs e)
        {
            _countdownTimer?.Stop();
            _timerRunning = false;
            _timerRemaining = TimeSpan.Zero;
            TimerCountdownDisplay.Text = "00:00:00";
            TimerCountdownDisplay.Foreground = Brushes.White;
            TimerStartButton.Content = "▶  Start Timer";
            TimerStatusText.Text = "Ready to start";
            TimerStatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));
        }

        private void NumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !char.IsDigit(e.Text, 0);
        }

        // ═══════════════════════════════════════════════════════════════════════
        //   WINDOW CHROME
        // ═══════════════════════════════════════════════════════════════════════

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) this.DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;
        private void CloseButton_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}
