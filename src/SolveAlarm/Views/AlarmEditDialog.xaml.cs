using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using SolveAlarm.Models;
using SolveAlarm.Services;

namespace SolveAlarm.Views
{
    public partial class AlarmEditDialog : Window
    {
        public Alarm AlarmResult { get; private set; }
        private bool _isLoading = false;

        public AlarmEditDialog(Alarm? existingAlarm = null)
        {
            InitializeComponent();
            PopulateTimeComboBoxes();

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
                HourComboBox.SelectedValue = 7;
                MinuteComboBox.SelectedValue = 0;
                // Default Easy difficulty is selected, default streak is 2 (Index 1)
                RequiredStreakComboBox.SelectedIndex = 1;
            }

            // Hook text change to validate sound path in real-time
            SoundPathTextBox.TextChanged += SoundPathTextBox_TextChanged;
        }

        private void PopulateTimeComboBoxes()
        {
            for (int i = 0; i < 24; i++)
            {
                HourComboBox.Items.Add(i);
            }
            for (int i = 0; i < 60; i++)
            {
                MinuteComboBox.Items.Add(i);
            }
        }

        private void LoadAlarmData(Alarm alarm)
        {
            _isLoading = true;
            
            HourComboBox.SelectedValue = alarm.Hour;
            MinuteComboBox.SelectedValue = alarm.Minute;
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

        private void SoundPathTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
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

        private void DifficultyComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
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
            // Gather time
            if (HourComboBox.SelectedValue == null || MinuteComboBox.SelectedValue == null)
            {
                MessageBox.Show("Please select a valid Hour and Minute.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            AlarmResult.Hour = (int)HourComboBox.SelectedValue;
            AlarmResult.Minute = (int)MinuteComboBox.SelectedValue;
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
    }
}
