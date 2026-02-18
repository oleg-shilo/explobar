using System;
using System.Drawing;
using System.Windows.Forms;

namespace Explobar
{
    class DisableDesigner
    {
    }
    // Custom form with button-like appearance
    class CustomButtonForm : Form
    {
        private bool isHovered = false;
        private bool isPressed = false;
        private ToolTip toolTip = new ToolTip();

        public CustomButtonForm()
        {
            SetStyle(ControlStyles.UserPaint |
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.DoubleBuffer |
                    ControlStyles.ResizeRedraw, true);

            toolTip.SetToolTip(this, "Open Explobar toolbar");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (toolTip != null)
                {
                    toolTip.Dispose();
                    toolTip = null;
                }
            }
            base.Dispose(disposing);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            isHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            isHovered = false;
            isPressed = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                isPressed = true;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left)
            {
                isPressed = false;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Determine colors based on state
            Color backgroundColor;
            Color borderColor;

            if (isPressed)
            {
                backgroundColor = Color.FromArgb(204, 228, 247); // Light blue
                borderColor = Color.FromArgb(0, 120, 215);
            }
            else if (isHovered)
            {
                backgroundColor = Color.FromArgb(229, 241, 251); // Very light blue
                borderColor = Color.FromArgb(0, 120, 215);
            }
            else
            {
                backgroundColor = Color.FromArgb(240, 240, 240); // Light gray
                borderColor = Color.FromArgb(173, 173, 173);
            }

            // Draw background
            using (SolidBrush bgBrush = new SolidBrush(backgroundColor))
            {
                g.FillRectangle(bgBrush, ClientRectangle);
            }

            // Draw border
            using (Pen borderPen = new Pen(borderColor, 1))
            {
                Rectangle borderRect = new Rectangle(0, 0, Width - 1, Height - 1);
                g.DrawRectangle(borderPen, borderRect);
            }

            // Draw expand symbol (chevron down)
            using (Pen iconPen = new Pen(Color.FromArgb(140, 140, 140), 1))
            {
                iconPen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
                iconPen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                iconPen.EndCap = System.Drawing.Drawing2D.LineCap.Round;

                // Calculate center position
                int centerX = Width / 2;
                int centerY = Height / 2;
                int size = 4; // Size of the chevron

                // Draw downward-pointing chevron (∨)
                Point[] chevronPoints = new Point[]
                {
                    new Point(centerX - size, centerY - 2),
                    new Point(centerX, centerY + 2),
                    new Point(centerX + size, centerY - 2)
                };

                g.DrawLines(iconPen, chevronPoints);
            }
        }
        public static void PlaceButtonOnWindow(IntPtr buttonHandle, IntPtr targetWindow, int x, int y)
        {
            // Set the button as a child window of the target window
            Desktop.SetParent(buttonHandle, targetWindow);

            // Position the button at the specified coordinates relative to the target window
            Desktop.SetWindowPos(buttonHandle, IntPtr.Zero, x, y, 0, 0, Desktop.SWP_NOSIZE | Desktop.SWP_SHOWWINDOW);
            // make sure the button is on op of all windows of this process (e.g. if the target window is behind another window of this process, the button should still be visible and clickable)
            Desktop.SetWindowPos(buttonHandle, Desktop.HWND_TOPMOST, 0, 0, 0, 0, Desktop.SWP_NOMOVE | Desktop.SWP_NOSIZE);
        }
    }
}