using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WakeyWakey.Models;
using WakeyWakey.Services;

namespace WakeyWakey.Views
{
    public partial class AlarmWindow : Window
    {
        private readonly Guid _alarmId;
        private readonly bool _isTestMode;
        private Alarm? _alarm;
        private AudioService? _audioService;
        private MathQuestion? _currentQuestion;
        private int _streak = 0;
        private bool _challengeSolved = false;
        private DispatcherTimer? _clockTimer;

        public AlarmWindow(Guid alarmId, bool isTestMode)
        {
            InitializeComponent();
            _alarmId = alarmId;
            _isTestMode = isTestMode;

            // Enforce window parameters programmatically as well to ensure correctness
            this.WindowStyle = WindowStyle.None;
            this.ResizeMode = ResizeMode.NoResize;
            this.WindowState = WindowState.Maximized;
            this.Topmost = true;

            Loaded += AlarmWindow_Loaded;
        }

        private void AlarmWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 1. Load Alarm info
            LoadAlarm();

            // 2. Display Test Mode UI if active
            if (_isTestMode)
            {
                TestModePanel.Visibility = Visibility.Visible;
            }

            // 3. Start Clock update timer
            _clockTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _clockTimer.Tick += ClockTimer_Tick;
            _clockTimer.Start();
            UpdateClockText();

            // 4. Start sound playback and 250ms volume lock
            _audioService = new AudioService();
            string? soundPath = _alarm?.SoundFilePath;
            _audioService.PlayAlarm(soundPath);

            // 5. Initialize streak visual circles and generate first math challenge
            InitializeStreakUI();
            GenerateNewChallenge();

            // 6. Force focus on the input box
            AnswerTextBox.Focus();
        }

        private void LoadAlarm()
        {
            List<Alarm> alarms = AlarmStorage.LoadAlarms();
            _alarm = alarms.FirstOrDefault(a => a.Id == _alarmId);

            if (_alarm == null)
            {
                // Fallback to default/test alarm
                _alarm = new Alarm
                {
                    Hour = DateTime.Now.Hour,
                    Minute = DateTime.Now.Minute,
                    Label = _isTestMode ? "Test Alarm Trigger" : "System Triggered Alarm",
                    Difficulty = ChallengeDifficulty.Medium
                };
            }

            LabelText.Text = _alarm.Label;
            ClockText.Text = $"{_alarm.Hour:D2}:{_alarm.Minute:D2}";
        }

        private void ClockTimer_Tick(object? sender, EventArgs e)
        {
            UpdateClockText();
        }

        private void UpdateClockText()
        {
            // While alarm is triggered, show active current time
            var now = DateTime.Now;
            ClockText.Text = $"{now.Hour:D2}:{now.Minute:D2}";
        }

        private void InitializeStreakUI()
        {
            StreakPanel.Children.Clear();
            int requiredStreak = _alarm?.ChallengeRequiredStreak ?? 3;
            for (int i = 0; i < requiredStreak; i++)
            {
                var border = new Border
                {
                    Width = 18,
                    Height = 18,
                    CornerRadius = new CornerRadius(9),
                    Background = new SolidColorBrush(Color.FromRgb(51, 65, 85)), // Slate Gray (inactive)
                    Margin = new Thickness(6, 0, 6, 0)
                };
                StreakPanel.Children.Add(border);
            }
        }

        private void GenerateNewChallenge()
        {
            ChallengeDifficulty difficulty = _alarm?.Difficulty ?? ChallengeDifficulty.Easy;
            _currentQuestion = MathChallenge.GenerateQuestion(difficulty);
            QuestionText.Text = _currentQuestion.QuestionText;
            AnswerTextBox.Clear();
            FeedbackText.Text = string.Empty;
        }

        private void SubmitAnswer()
        {
            if (_currentQuestion == null) return;

            string text = AnswerTextBox.Text.Trim();
            if (int.TryParse(text, out int userAnswer))
            {
                if (userAnswer == _currentQuestion.CorrectAnswer)
                {
                    _streak++;
                    FeedbackText.Text = "Correct!";
                    FeedbackText.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129)); // Green
                    UpdateStreakUI();

                    int requiredStreak = _alarm?.ChallengeRequiredStreak ?? 3;
                    if (_streak >= requiredStreak)
                    {
                        DismissAlarm();
                    }
                    else
                    {
                        // Generate next question
                        DispatcherTimer delayTimer = new DispatcherTimer
                        {
                            Interval = TimeSpan.FromMilliseconds(500)
                        };
                        delayTimer.Tick += (s, e) =>
                        {
                            delayTimer.Stop();
                            GenerateNewChallenge();
                            AnswerTextBox.Focus();
                        };
                        delayTimer.Start();
                    }
                }
                else
                {
                    _streak = 0;
                    FeedbackText.Text = $"Incorrect! Answer was {_currentQuestion.CorrectAnswer}. Streak reset.";
                    FeedbackText.Foreground = new SolidColorBrush(Color.FromRgb(244, 63, 94)); // Red
                    UpdateStreakUI();
                    
                    // Generate next question
                    DispatcherTimer delayTimer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromMilliseconds(1500)
                    };
                    delayTimer.Tick += (s, e) =>
                    {
                        delayTimer.Stop();
                        GenerateNewChallenge();
                        AnswerTextBox.Focus();
                    };
                    delayTimer.Start();
                }
            }
            else
            {
                FeedbackText.Text = "Please enter a valid integer.";
                FeedbackText.Foreground = new SolidColorBrush(Color.FromRgb(244, 63, 94));
            }
        }

        private void UpdateStreakUI()
        {
            var activeBrush = new SolidColorBrush(Color.FromRgb(16, 185, 129)); // Green
            var inactiveBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85)); // Slate Gray

            int requiredStreak = _alarm?.ChallengeRequiredStreak ?? 3;
            for (int i = 0; i < requiredStreak; i++)
            {
                if (i < StreakPanel.Children.Count && StreakPanel.Children[i] is Border border)
                {
                    border.Background = _streak > i ? activeBrush : inactiveBrush;
                }
            }
        }

        private void DismissAlarm()
        {
            _challengeSolved = true;
            _audioService?.StopAlarm();
            _clockTimer?.Stop();

            // Post-dismiss rescheduling logic
            if (!_isTestMode && _alarm != null)
            {
                List<Alarm> alarms = AlarmStorage.LoadAlarms();
                Alarm? match = alarms.FirstOrDefault(a => a.Id == _alarm.Id);
                
                if (match != null)
                {
                    if (match.IsRepeating)
                    {
                        // Repeating alarm - recalculate task trigger in Windows Task Scheduler
                        SchedulerService.RegisterAlarm(match);
                    }
                    else
                    {
                        // One-time alarm - disable after trigger
                        match.IsEnabled = false;
                        SchedulerService.UnregisterAlarm(match.Id);
                    }
                    AlarmStorage.SaveAlarms(alarms);
                }
            }

            MessageBox.Show("Good morning! Alarm dismissed.", "WakeyWakey", MessageBoxButton.OK, MessageBoxImage.Information);
            CloseWindowSafely();
        }

        private void ExitAlarmSafely()
        {
            _challengeSolved = true;
            _audioService?.StopAlarm();
            _clockTimer?.Stop();
            CloseWindowSafely();
        }

        private void CloseWindowSafely()
        {
            Loaded -= AlarmWindow_Loaded;
            _audioService?.Dispose();
            this.Close();
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            SubmitAnswer();
        }

        private void AnswerTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SubmitAnswer();
            }
        }

        private void ExitTest_Click(object sender, RoutedEventArgs e)
        {
            if (_isTestMode)
            {
                ExitAlarmSafely();
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && _isTestMode)
            {
                ExitAlarmSafely();
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!_challengeSolved && !_isTestMode)
            {
                e.Cancel = true; // Block alt-f4 and closing attempts
            }
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            if (!_challengeSolved && !_isTestMode)
            {
                // Force window back to front and focus
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        this.Topmost = false;
                        this.Topmost = true;
                        this.Activate();
                        this.Focus();
                        AnswerTextBox.Focus();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error activating window: {ex.Message}");
                    }
                }), DispatcherPriority.Normal);
            }
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized && !_challengeSolved && !_isTestMode)
            {
                // Prevent minimization
                this.WindowState = WindowState.Maximized;
            }
        }
    }
}
