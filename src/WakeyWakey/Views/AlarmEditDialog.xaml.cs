using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using WakeyWakey.Models;
using WakeyWakey.Services;

namespace WakeyWakey.Views
{
    public partial class AlarmEditDialog : Window
    {
        public Alarm AlarmResult { get; private set; }
        private bool _isLoading = false;

        public AlarmEditDialog(Alarm? existingAlarm = null)
        {
            InitializeComponent();

            if (existingAlarm != null)
            {
                TitleText.Text = "Edit Alarm";
                AlarmResult = existingAlarm;
                LoadAlarmData(existingAlarm);
            }
            else
            {
                TitleText.Text = "Add New Alarm";
                AlarmResult = new Alarm();
                HourTextBox.Text = "07";
                MinuteTextBox.Text = "00";
                // Default Easy difficulty is selected, default streak is 2 (Index 1)
                RequiredStreakComboBox.SelectedIndex = 1;
            }

            // Hook text change to validate sound path in real-time
            SoundPathTextBox.TextChanged += SoundPathTextBox_TextChanged;
        }

        private void LoadAlarmData(Alarm alarm)
        {
            _isLoading = true;
            
            HourTextBox.Text = alarm.Hour.ToString("D2");
            MinuteTextBox.Text = alarm.Minute.ToString("D2");
            LabelTextBox.Text = alarm.Label;
            DifficultyComboBox.SelectedIndex = (int)alarm.Difficulty;
            RequiredStreakComboBox.SelectedIndex = Math.Clamp(alarm.ChallengeRequiredStreak - 1, 0, 9);
            SoundPathTextBox.Text = alarm.SoundFilePath ?? string.Empty;

            // Weekdays
            ChkMon.IsChecked = (alarm.RepeatDays & DaysOfWeek.Monday) != 0;
            ChkTue.IsChecked = (alarm.RepeatDays & DaysOfWeek.Tuesday) != 0;
            ChkWed.IsChecked = (alarm.RepeatDays & DaysOfWeek.Wednesday) != 0;
            ChkThu.IsChecked = (alarm.RepeatDays & DaysOfWeek.Thursday) != 0;
            ChkFri.IsChecked = (alarm.RepeatDays & DaysOfWeek.Friday) != 0;
            ChkSat.IsChecked = (alarm.RepeatDays & DaysOfWeek.Saturday) != 0;
            ChkSun.IsChecked = (alarm.RepeatDays & DaysOfWeek.Sunday) != 0;

            ValidateSoundFile(alarm.SoundFilePath);

            _isLoading = false;
        }

        private void SoundPathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateSoundFile(SoundPathTextBox.Text);
        }

        private void ValidateSoundFile(string? path)
        {
            if (string.IsNullOrEmpty(path))
            {
                SoundWarningText.Text = string.Empty;
                SoundWarningText.Visibility = Visibility.Collapsed;
                return;
            }

            if (AudioService.ValidateSoundFile(path, out string err))
            {
                SoundWarningText.Text = "✓ Sound file valid.";
                SoundWarningText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 185, 129)); // Green
                SoundWarningText.Visibility = Visibility.Visible;
            }
            else
            {
                SoundWarningText.Text = $"⚠ Warning: {err} App will use fallback alarm sound.";
                SoundWarningText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(244, 63, 94)); // Red
                SoundWarningText.Visibility = Visibility.Visible;
            }
        }

        private void BrowseSound_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Audio Files (*.mp3; *.wav)|*.mp3;*.wav|All Files (*.*)|*.*",
                Title = "Select Custom Alarm Sound"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                SoundPathTextBox.Text = openFileDialog.FileName;
            }
        }

        private void DifficultyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading || RequiredStreakComboBox == null) return;

            int selectedIndex = DifficultyComboBox.SelectedIndex;
            if (selectedIndex == 0) // Easy
            {
                RequiredStreakComboBox.SelectedIndex = 1; // 2 in a row
            }
            else // Medium or Hard
            {
                RequiredStreakComboBox.SelectedIndex = 2; // 3 in a row
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Parse & Validate modern numeric time
            if (!int.TryParse(HourTextBox.Text, out int hour) || hour < 0 || hour > 23 ||
                !int.TryParse(MinuteTextBox.Text, out int minute) || minute < 0 || minute > 59)
            {
                MessageBox.Show("Please enter a valid time (Hour 0-23, Minute 0-59).", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            AlarmResult.Hour = hour;
            AlarmResult.Minute = minute;
            AlarmResult.Label = string.IsNullOrWhiteSpace(LabelTextBox.Text) ? "Alarm" : LabelTextBox.Text.Trim();
            AlarmResult.Difficulty = (ChallengeDifficulty)DifficultyComboBox.SelectedIndex;
            AlarmResult.ChallengeRequiredStreak = RequiredStreakComboBox.SelectedIndex + 1;
            AlarmResult.SoundFilePath = string.IsNullOrWhiteSpace(SoundPathTextBox.Text) ? null : SoundPathTextBox.Text.Trim();

            // Set repeating days
            DaysOfWeek repeat = DaysOfWeek.None;
            if (ChkMon.IsChecked == true) repeat |= DaysOfWeek.Monday;
            if (ChkTue.IsChecked == true) repeat |= DaysOfWeek.Tuesday;
            if (ChkWed.IsChecked == true) repeat |= DaysOfWeek.Wednesday;
            if (ChkThu.IsChecked == true) repeat |= DaysOfWeek.Thursday;
            if (ChkFri.IsChecked == true) repeat |= DaysOfWeek.Friday;
            if (ChkSat.IsChecked == true) repeat |= DaysOfWeek.Saturday;
            if (ChkSun.IsChecked == true) repeat |= DaysOfWeek.Sunday;
            AlarmResult.RepeatDays = repeat;

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                this.DragMove();
            }
        }

        // Modern Scrolling UX: Scroll over textboxes to change time!
        private void HourTextBox_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (int.TryParse(HourTextBox.Text, out int hour))
            {
                hour = e.Delta > 0 ? (hour + 1) % 24 : (hour + 23) % 24;
                HourTextBox.Text = hour.ToString("D2");
            }
            e.Handled = true;
        }

        private void MinuteTextBox_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (int.TryParse(MinuteTextBox.Text, out int minute))
            {
                minute = e.Delta > 0 ? (minute + 1) % 60 : (minute + 59) % 60;
                MinuteTextBox.Text = minute.ToString("D2");
            }
            e.Handled = true;
        }

        // Numeric entry restriction
        private void NumericTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !char.IsDigit(e.Text, 0);
        }
    }
}
