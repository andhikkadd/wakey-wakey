using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using WakeyWakey.Models;
using WakeyWakey.Services;

namespace WakeyWakey.Views
{
    public partial class TimerWindow : Window
    {
        private readonly AudioService _audio = new AudioService();
        private readonly ChallengeDifficulty _difficulty;
        private readonly int _requiredStreak;
        private int _currentStreak = 0;
        private MathQuestion _currentQuestion = null!;

        public TimerWindow(string label, ChallengeDifficulty difficulty, int requiredStreak)
        {
            InitializeComponent();
            _difficulty = difficulty;
            _requiredStreak = requiredStreak;

            TimerLabelText.Text = label;
            BuildStreakDots();
            NextQuestion();

            // Start blaring the alarm
            _audio.PlayAlarm(null);
            AnswerTextBox.Focus();
        }

        private void BuildStreakDots()
        {
            StreakPanel.Children.Clear();
            for (int i = 0; i < _requiredStreak; i++)
            {
                var dot = new Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = new SolidColorBrush(Color.FromRgb(30, 41, 59)), // unfilled
                    Stroke = new SolidColorBrush(Color.FromRgb(71, 85, 105)),
                    StrokeThickness = 1,
                    Margin = new Thickness(4, 0, 4, 0),
                    Name = $"Dot_{i}"
                };
                StreakPanel.Children.Add(dot);
            }
        }

        private void UpdateStreakDots()
        {
            for (int i = 0; i < StreakPanel.Children.Count; i++)
            {
                if (StreakPanel.Children[i] is Ellipse dot)
                {
                    if (i < _currentStreak)
                    {
                        dot.Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129)); // green
                        dot.Stroke = new SolidColorBrush(Color.FromRgb(5, 150, 105));
                    }
                    else
                    {
                        dot.Fill = new SolidColorBrush(Color.FromRgb(30, 41, 59));
                        dot.Stroke = new SolidColorBrush(Color.FromRgb(71, 85, 105));
                    }
                }
            }
        }

        private void NextQuestion()
        {
            _currentQuestion = MathChallenge.GenerateQuestion(_difficulty);
            QuestionText.Text = _currentQuestion.QuestionText + " = ?";
            AnswerTextBox.Clear();
            FeedbackText.Text = string.Empty;
            AnswerTextBox.Focus();
        }

        private void Submit_Click(object sender, RoutedEventArgs e) => CheckAnswer();
        private void AnswerTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) CheckAnswer();
        }

        private void CheckAnswer()
        {
            if (!int.TryParse(AnswerTextBox.Text.Trim(), out int answer))
            {
                FeedbackText.Text = "Enter a number!";
                FeedbackText.Foreground = new SolidColorBrush(Color.FromRgb(244, 63, 94));
                return;
            }

            if (answer == _currentQuestion.CorrectAnswer)
            {
                _currentStreak++;
                UpdateStreakDots();

                if (_currentStreak >= _requiredStreak)
                {
                    // All done — dismiss
                    _audio.StopAlarm();
                    Close();
                }
                else
                {
                    FeedbackText.Text = $"✓ Correct! {_requiredStreak - _currentStreak} more to go...";
                    FeedbackText.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                    NextQuestion();
                }
            }
            else
            {
                _currentStreak = 0;
                UpdateStreakDots();
                FeedbackText.Text = $"✗ Wrong! Answer was {_currentQuestion.CorrectAnswer}. Start over.";
                FeedbackText.Foreground = new SolidColorBrush(Color.FromRgb(244, 63, 94));
                NextQuestion();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _audio.StopAlarm();
            _audio.Dispose();
            base.OnClosed(e);
        }
    }
}
