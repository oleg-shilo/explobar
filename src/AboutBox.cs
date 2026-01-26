using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace Explobar
{
    public class AboutBox : Form
    {
        public AboutBox()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "About Explobar";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ClientSize = new Size(400, 200);
            this.BackColor = Color.White;
            this.Padding = new Padding(20);
            this.TopMost = true;

            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            var productName = ((AssemblyProductAttribute)Attribute.GetCustomAttribute(
                assembly, typeof(AssemblyProductAttribute), false))?.Product ?? "Explobar";
            var copyright = ((AssemblyCopyrightAttribute)Attribute.GetCustomAttribute(
                assembly, typeof(AssemblyCopyrightAttribute), false))?.Copyright ?? "";

            // App name label
            var lblAppName = new Label
            {
                Text = productName,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 20)
            };
            this.Controls.Add(lblAppName);

            // Version label
            var lblVersion = new Label
            {
                Text = $"Version {version.Major}.{version.Minor}.{version.Build}",
                Font = new Font("Segoe UI", 10F),
                AutoSize = true,
                Location = new Point(20, 55)
            };
            this.Controls.Add(lblVersion);

            // Description label
            var lblDescription = new Label
            {
                Text = "Windows Explorer toolbar extension\nfor quick access to common operations",
                Font = new Font("Segoe UI", 9F),
                AutoSize = true,
                Location = new Point(20, 85)
            };
            this.Controls.Add(lblDescription);

            // Copyright label
            if (!string.IsNullOrEmpty(copyright))
            {
                var lblCopyright = new Label
                {
                    Text = copyright,
                    Font = new Font("Segoe UI", 8F),
                    ForeColor = Color.Gray,
                    AutoSize = true,
                    Location = new Point(20, 125)
                };
                this.Controls.Add(lblCopyright);
            }

            // GitHub link
            var linkGitHub = new LinkLabel
            {
                Text = "Visit GitHub Repository",
                Font = new Font("Segoe UI", 9F),
                AutoSize = true,
                Location = new Point(20, 150)
            };
            linkGitHub.LinkClicked += (s, e) =>
            {
                try
                {
                    Process.Start("https://github.com/oleg-shilo/explobar");
                }
                catch (Exception ex)
                {
                    Runtime.ShowError($"Failed to open link: {ex.Message}");
                }
            };
            this.Controls.Add(linkGitHub);

            // OK button
            var btnOK = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Size = new Size(75, 30),
                Location = new Point(305, 150),
                FlatStyle = FlatStyle.System
            };
            this.Controls.Add(btnOK);
            this.AcceptButton = btnOK;
        }

        public static new void Show()
        {
            using (var aboutBox = new AboutBox())
            {
                aboutBox.ShowDialog();
            }
        }
    }
}