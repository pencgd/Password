using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace TerVer2
{
    public partial class Form1 : Form
    {
        private static readonly Random random = new Random();
        private Color accentColor = Color.FromArgb(99, 102, 241);

        public Form1()
        {
            InitializeComponent();
            InitializePasswordChecker();
            InitializeGenerateButton();
            this.Paint += Form1_Paint;
            this.Resize += Form1_Resize;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            this.Invalidate();
        }

        private void InitializePasswordChecker()
        {
            txtPassword.TextChanged += TxtPassword_TextChanged;
            progressStrength.Minimum = 0;
            progressStrength.Maximum = 100;
            lblStrength.Text = "";
            lblCrackTime.Text = "";
            progressStrength.Value = 0;
        }

        private void InitializeGenerateButton()
        {
            btnGeneratePassword.Click += BtnGeneratePassword_Click;
            btnGeneratePassword.Paint += BtnGeneratePassword_Paint;
            btnGeneratePassword.MouseEnter += (s, e) => { btnGeneratePassword.BackColor = Color.FromArgb(129, 132, 255); btnGeneratePassword.Invalidate(); };
            btnGeneratePassword.MouseLeave += (s, e) => { btnGeneratePassword.BackColor = accentColor; btnGeneratePassword.Invalidate(); };
        }

        private void BtnGeneratePassword_Paint(object sender, PaintEventArgs e)
        {
            Button btn = sender as Button;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = GetRoundedRect(btn.ClientRectangle, 12))
            {
                btn.Region = new Region(path);
                using (SolidBrush brush = new SolidBrush(btn.BackColor))
                {
                    e.Graphics.FillPath(brush, path);
                }
            }
            TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, btn.ClientRectangle, btn.ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            e.Handled = true;
        }

        private GraphicsPath GetRoundedRect(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;
            Rectangle arc = new Rectangle(rect.Location, new Size(diameter, diameter));
            path.AddArc(arc, 180, 90);
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void BtnGeneratePassword_Click(object sender, EventArgs e)
        {
            string newPassword = GenerateStrongPassword();
            txtPassword.Text = newPassword;
        }

        private string GenerateStrongPassword()
        {
            const string lower = "abcdefghijklmnopqrstuvwxyz";
            const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string digits = "0123456789";
            const string special = "!@#$%^&*()";
            char[] password = new char[14];
            password[0] = lower[random.Next(lower.Length)];
            password[1] = upper[random.Next(upper.Length)];
            password[2] = digits[random.Next(digits.Length)];
            password[3] = special[random.Next(special.Length)];
            string allChars = lower + upper + digits + special;
            for (int i = 4; i < password.Length; i++)
            {
                password[i] = allChars[random.Next(allChars.Length)];
            }
            for (int i = 0; i < password.Length; i++)
            {
                int j = random.Next(password.Length);
                char temp = password[i];
                password[i] = password[j];
                password[j] = temp;
            }
            return new string(password);
        }

        private void TxtPassword_TextChanged(object sender, EventArgs e)
        {
            string password = txtPassword.Text;
            if (string.IsNullOrEmpty(password))
            {
                lblStrength.Text = "Нет пароля";
                lblCrackTime.Text = "—";
                progressStrength.Value = 0;
                UpdateProgressColor(0);
                return;
            }
            var result = EvaluatePassword(password);
            lblStrength.Text = result.Strength;
            lblCrackTime.Text = result.CrackTime;
            progressStrength.Value = result.Score;
            UpdateProgressColor(result.Score);
            if (result.Score < 30)
                lblStrength.ForeColor = Color.FromArgb(239, 68, 68);
            else if (result.Score < 70)
                lblStrength.ForeColor = Color.FromArgb(234, 179, 8);
            else
                lblStrength.ForeColor = Color.FromArgb(34, 197, 94);
        }

        private void UpdateProgressColor(int score)
        {
            if (score < 30)
                progressStrength.ForeColor = Color.FromArgb(239, 68, 68);
            else if (score < 70)
                progressStrength.ForeColor = Color.FromArgb(234, 179, 8);
            else
                progressStrength.ForeColor = Color.FromArgb(34, 197, 94);
        }

        private PasswordEvaluation EvaluatePassword(string password)
        {
            int score = 0;
            int length = password.Length;
            if (length >= 12) score += 25;
            else if (length >= 8) score += 15;
            else if (length >= 6) score += 5;
            if (Regex.IsMatch(password, @"\d")) score += 15;
            if (Regex.IsMatch(password, @"[A-Z]")) score += 15;
            if (Regex.IsMatch(password, @"[a-z]")) score += 10;
            if (Regex.IsMatch(password, @"[!@#$%^&*(),.?"":{}|<>]")) score += 20;
            if (length > 12) score += 15;
            else if (length > 10) score += 5;
            if (IsCommonPassword(password)) score -= 30;
            if (IsSequential(password)) score -= 20;
            if (HasRepeatingChars(password)) score -= 15;
            score = Math.Max(0, Math.Min(100, score));
            string strength;
            if (score >= 80) strength = "Очень высокая";
            else if (score >= 60) strength = "Высокая";
            else if (score >= 40) strength = "Средняя";
            else if (score >= 20) strength = "Низкая";
            else strength = "Очень низкая";
            string crackTime = CalculateCrackTime(password);
            return new PasswordEvaluation
            {
                Score = score,
                Strength = strength,
                CrackTime = crackTime
            };
        }

        private string CalculateCrackTime(string password)
        {
            int length = password.Length;
            int charsetSize = 0;
            if (Regex.IsMatch(password, @"[a-z]")) charsetSize += 26;
            if (Regex.IsMatch(password, @"[A-Z]")) charsetSize += 26;
            if (Regex.IsMatch(password, @"\d")) charsetSize += 10;
            if (Regex.IsMatch(password, @"[!@#$%^&*(),.?"":{}|<>]")) charsetSize += 32;
            if (charsetSize == 0) return "менее секунды";
            double combinations = Math.Pow(charsetSize, length);
            double attemptsPerSecond = 1_000_000_000;
            double seconds = combinations / attemptsPerSecond;
            if (seconds < 1) return "менее секунды";
            else if (seconds < 60) return $"{seconds:F1} сек";
            else if (seconds < 3600) return $"{seconds / 60:F1} мин";
            else if (seconds < 86400) return $"{seconds / 3600:F1} ч";
            else if (seconds < 31536000) return $"{seconds / 86400:F1} дн";
            else if (seconds < 31536000000) return $"{seconds / 31536000:F1} лет";
            else return "более 1000 лет";
        }

        private bool IsCommonPassword(string password)
        {
            string[] common = {
                "123456", "password", "123456789", "12345", "12345678",
                "qwerty", "abc123", "111111", "123123", "admin",
                "password123", "letmein", "welcome", "monkey", "dragon"
            };
            return common.Contains(password.ToLower());
        }

        private bool IsSequential(string password)
        {
            string lower = password.ToLower();
            string[] seq = {
                "123", "234", "345", "456", "567", "678", "789",
                "abc", "bcd", "cde", "def", "efg", "fgh", "ghi",
                "qwe", "wer", "ert", "rty", "tyu", "yui", "uio",
                "asd", "sdf", "dfg", "fgh", "ghj", "hjk", "jkl",
                "zxc", "xcv", "cvb", "vbn", "bnm"
            };
            return seq.Any(s => lower.Contains(s));
        }

        private bool HasRepeatingChars(string password)
        {
            for (int i = 0; i < password.Length - 2; i++)
            {
                if (password[i] == password[i + 1] && password[i] == password[i + 2])
                    return true;
            }
            return false;
        }

        private class PasswordEvaluation
        {
            public int Score { get; set; }
            public string Strength { get; set; }
            public string CrackTime { get; set; }
        }

        private void lblStrength_Click(object sender, EventArgs e)
        {
        }

        private void txtPassword_TextChanged_1(object sender, EventArgs e)
        {

        }
    }
}