using System;
using WakeyWakey.Models;

namespace WakeyWakey.Services
{
    public class MathQuestion
    {
        public string QuestionText { get; set; } = string.Empty;
        public int CorrectAnswer { get; set; }
    }

    public class MathChallenge
    {
        private static readonly Random Rand = new Random();

        public static MathQuestion GenerateQuestion(ChallengeDifficulty difficulty)
        {
            switch (difficulty)
            {
                case ChallengeDifficulty.Easy:
                    return GenerateEasyQuestion();
                case ChallengeDifficulty.Medium:
                    return GenerateMediumQuestion();
                case ChallengeDifficulty.Hard:
                    return GenerateHardQuestion();
                default:
                    return GenerateEasyQuestion();
            }
        }

        private static MathQuestion GenerateEasyQuestion()
        {
            // Only addition and subtraction. Numbers from 1 to 20. No negative answers.
            int a = Rand.Next(1, 21);
            int b = Rand.Next(1, 21);
            bool isAddition = Rand.Next(2) == 0;

            if (isAddition)
            {
                return new MathQuestion
                {
                    QuestionText = $"{a} + {b}",
                    CorrectAnswer = a + b
                };
            }
            else
            {
                // Ensure no negative answers
                if (a < b)
                {
                    int temp = a;
                    a = b;
                    b = temp;
                }
                return new MathQuestion
                {
                    QuestionText = $"{a} - {b}",
                    CorrectAnswer = a - b
                };
            }
        }

        private static MathQuestion GenerateMediumQuestion()
        {
            // Addition, subtraction, and simple multiplication.
            // Numbers from 2 to 12.
            // Multiplication only uses 2-9.
            // No parentheses. No negative answers.
            int type = Rand.Next(3);
            if (type == 0)
            {
                // Addition: 2 to 12
                int a = Rand.Next(2, 13);
                int b = Rand.Next(2, 13);
                return new MathQuestion
                {
                    QuestionText = $"{a} + {b}",
                    CorrectAnswer = a + b
                };
            }
            else if (type == 1)
            {
                // Subtraction: 2 to 12
                int a = Rand.Next(2, 13);
                int b = Rand.Next(2, 13);
                if (a < b)
                {
                    int temp = a;
                    a = b;
                    b = temp;
                }
                return new MathQuestion
                {
                    QuestionText = $"{a} - {b}",
                    CorrectAnswer = a - b
                };
            }
            else
            {
                // Multiplication: one number 2 to 12, other 2 to 9
                int a = Rand.Next(2, 13);
                int b = Rand.Next(2, 10);
                
                // Randomize visually (e.g. 6 x 7 or 7 x 6)
                if (Rand.Next(2) == 0)
                {
                    return new MathQuestion
                    {
                        QuestionText = $"{a} * {b}",
                        CorrectAnswer = a * b
                    };
                }
                else
                {
                    return new MathQuestion
                    {
                        QuestionText = $"{b} * {a}",
                        CorrectAnswer = a * b
                    };
                }
            }
        }

        private static MathQuestion GenerateHardQuestion()
        {
            // Two-step arithmetic only. Numbers from 2 to 15.
            // Parentheses allowed but keep results under 150.
            // No division, no decimals, no negative answers.
            // Example: (7 + 5) * 3, 8 * 9 + 6.
            int type = Rand.Next(4);
            if (type == 0)
            {
                // Format: (A + B) * C
                while (true)
                {
                    int a = Rand.Next(2, 16);
                    int b = Rand.Next(2, 16);
                    int sum = a + b;
                    int maxC = 149 / sum;
                    if (maxC >= 2)
                    {
                        int c = Rand.Next(2, Math.Min(16, maxC + 1));
                        return new MathQuestion
                        {
                            QuestionText = $"({a} + {b}) * {c}",
                            CorrectAnswer = sum * c
                        };
                    }
                }
            }
            else if (type == 1)
            {
                // Format: (A - B) * C
                while (true)
                {
                    int a = Rand.Next(2, 16);
                    int b = Rand.Next(2, 16);
                    if (a < b) { int tmp = a; a = b; b = tmp; }
                    int diff = a - b;
                    int maxC = diff > 0 ? 149 / diff : 15;
                    int c = Rand.Next(2, Math.Min(16, maxC + 1));
                    return new MathQuestion
                    {
                        QuestionText = $"({a} - {b}) * {c}",
                        CorrectAnswer = diff * c
                    };
                }
            }
            else if (type == 2)
            {
                // Format: A * B + C
                while (true)
                {
                    int a = Rand.Next(2, 16);
                    int b = Rand.Next(2, 10); // multiplication usually keeps multipliers smaller
                    int product = a * b;
                    int maxC = 149 - product;
                    if (maxC >= 2)
                    {
                        int c = Rand.Next(2, Math.Min(16, maxC + 1));
                        return new MathQuestion
                        {
                            QuestionText = $"{a} * {b} + {c}",
                            CorrectAnswer = product + c
                        };
                    }
                }
            }
            else
            {
                // Format: A * B - C
                while (true)
                {
                    int a = Rand.Next(2, 16);
                    int b = Rand.Next(2, 10);
                    int product = a * b;
                    if (product >= 2)
                    {
                        int c = Rand.Next(2, Math.Min(16, product + 1));
                        return new MathQuestion
                        {
                            QuestionText = $"{a} * {b} - {c}",
                            CorrectAnswer = product - c
                        };
                    }
                }
            }
        }
    }
}
