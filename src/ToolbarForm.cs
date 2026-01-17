using Shell32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Explobar
{
    public class ToolbarForm : Form
    {

        private System.Windows.Forms.Timer checkMouseTimer;
        private bool enableMouseCheck = false;
        private FlowLayoutPanel toolbarPanel;

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_SHOWWINDOW = 0x0040;

        public ToolbarForm(List<string> items)
        {
            this.Text = "Selected Items";
            this.TopMost = true;
            this.FormBorderStyle = FormBorderStyle.None;
            this.KeyPreview = true;
            this.ShowInTaskbar = false;
            this.BackColor = System.Drawing.Color.DarkGray;
            this.Padding = new Padding(1);
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;

            // Create toolbar panel
            toolbarPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = System.Drawing.Color.White,
                Padding = new Padding(1),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            foreach (var item in ToolbarItems.Items)
            {
                AddToolbarButton(item, items);
                // Add sample buttons (will be added dynamically in the future)
                // AddToolbarButton("1", (s, e) => MessageBox.Show("Button 1 clicked"));
                // AddToolbarButton("2", (s, e) => MessageBox.Show("Button 2 clicked"));
                // AddToolbarButton("3", (s, e) => MessageBox.Show("Button 3 clicked"));

                this.Controls.Add(toolbarPanel);

                // Ensure topmost when form is shown
                this.Shown += (s, e) =>
                {
                    SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                    this.Activate();
                };

                // Wait 2 seconds before starting mouse check
                var delayTimer = new System.Windows.Forms.Timer { Interval = 2000 };
                delayTimer.Tick += (s, e) =>
                {
                    delayTimer.Stop();
                    enableMouseCheck = true;
                    checkMouseTimer.Start();
                };
                delayTimer.Start();

                // Check mouse position every 100ms
                checkMouseTimer = new System.Windows.Forms.Timer { Interval = 100 };
                checkMouseTimer.Tick += CheckMouseTimer_Tick;
            }
        }
        private void AddToolbarButton(ToolbarItem info, List<string> selectedItems)
        {
            var button = new Button
            {
                Width = 30,
                Height = 30,
                Image = info.IconPath.ExtractIcon(),
                FlatStyle = FlatStyle.Flat,
                BackColor = System.Drawing.Color.White
            };
            button.Click += (_, _) =>
            {
                info.Execute(selectedItems);
                checkMouseTimer?.Stop();
                this.Close();
            };
            toolbarPanel.Controls.Add(button);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                checkMouseTimer?.Stop();
                this.Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void CheckMouseTimer_Tick(object? sender, EventArgs e)
        {
            if (!enableMouseCheck)
                return;
            try
            {
                var cursorPos = Cursor.Position;
                var formBounds = new System.Drawing.Rectangle(this.Location, this.Size);

                if (!formBounds.Contains(cursorPos))
                {
                    checkMouseTimer.Stop();
                    this.Close();
                }
            }
            catch
            {
                // Ignore errors
            }
        }
    }
}