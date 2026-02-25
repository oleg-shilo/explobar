using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TsudaKageyu;

namespace Explobar
{
    public static class IconBrowser
    {
        public static void Show()
        {
            ApartmentState.STA.Run(() => new IconBrowserForm().ShowDialog());
        }

        public static void Show(string filePath)
        {
            ApartmentState.STA.Run(() =>
            {
                var form = new IconBrowserForm();

                // Set the path and load icons after form is shown
                form.Shown += (s, e) =>
                {
                    var pathField = form.Controls.Find("pathTextBox", true)[0] as TextBox;
                    if (pathField != null)
                        pathField.Text = filePath;
                };

                form.ShowDialog();
            });
        }
    }

    static class IconBrowserSettings
    {
        static string SettingsFilePath
        {
            get => Environment.SpecialFolder.LocalApplicationData.Combine("Explobar", "iconbrowser-history.txt").EnsureFileDir();
        }

        public static List<string> RecentFiles
        {
            get
            {
                try
                {
                    if (File.Exists(SettingsFilePath))
                    {
                        return File.ReadAllLines(SettingsFilePath)
                            .Where(line => !string.IsNullOrWhiteSpace(line))
                            .Take(10) // Keep last 10 files
                            .ToList();
                    }
                }
                catch
                {
                    // Ignore errors reading settings
                }
                return new List<string> { @"%SystemRoot%\System32\shell32.dll" };
            }
            set
            {
                try
                {
                    File.WriteAllLines(SettingsFilePath, value ?? new List<string>());
                }
                catch
                {
                    // Ignore errors writing settings
                }
            }
        }

        public static void AddRecentFile(string filePath)
        {
            var recent = RecentFiles;

            // Remove if already exists
            recent.Remove(filePath);

            // Add to front
            recent.Insert(0, filePath);

            // Keep only last 10
            if (recent.Count > 10)
                recent = recent.Take(10).ToList();

            RecentFiles = recent;
        }
    }

    public class IconBrowserForm : Form
    {
        ComboBox pathComboBox;
        Button browseButton;
        Button loadButton;
        FlowLayoutPanel iconsPanel;
        Label statusLabel;
        ToolTip toolTip;

        public IconBrowserForm()
        {
            InitializeComponents();
        }

        void InitializeComponents()
        {
            this.Text = "Icon Browser";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(400, 300);
            this.AllowDrop = true;
            this.DragEnter += Form_DragEnter;
            this.DragDrop += Form_DragDrop;
            this.Icon = Runtime.AppIcon;

            toolTip = new ToolTip();

            // Top panel with file selection
            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                Padding = new Padding(10)
            };

            var label = new Label
            {
                Text = "File Path:",
                Location = new Point(10, 10),
                AutoSize = true
            };

            pathComboBox = new ComboBox
            {
                Location = new Point(10, 30),
                Width = 550,
                DropDownStyle = ComboBoxStyle.DropDown,
                AllowDrop = true
            };

            // Load recent files into combobox
            foreach (var file in IconBrowserSettings.RecentFiles)
            {
                pathComboBox.Items.Add(file);
            }

            if (pathComboBox.Items.Count > 0)
                pathComboBox.SelectedIndex = 0;

            pathComboBox.KeyDown += PathComboBox_KeyDown;
            pathComboBox.SelectionChangeCommitted += PathComboBox_SelectionChangeCommitted;
            pathComboBox.DragEnter += Form_DragEnter;
            pathComboBox.DragDrop += Form_DragDrop;

            browseButton = new Button
            {
                Text = "Browse...",
                Location = new Point(570, 28),
                Width = 100,
                Height = 25
            };
            browseButton.Click += BrowseButton_Click;

            loadButton = new Button
            {
                Text = "Load Icons",
                Location = new Point(680, 28),
                Width = 100,
                Height = 25
            };
            loadButton.Click += LoadButton_Click;

            topPanel.Controls.AddRange(new Control[] { label, pathComboBox, browseButton, loadButton });

            // Status bar
            statusLabel = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 25,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                Text = "Ready"
            };

            // Icons panel with scroll
            iconsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(10),
                BackColor = Color.White
            };

            this.Controls.Add(iconsPanel);
            this.Controls.Add(topPanel);
            this.Controls.Add(statusLabel);

            // Load default file on start
            this.Load += (s, e) => LoadIcons();
        }

        void BrowseButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "Executable Files (*.exe;*.dll)|*.exe;*.dll|All Files (*.*)|*.*";
                dialog.Title = "Select File to Extract Icons";

                // Set initial directory to the folder of the current file
                try
                {
                    string currentPath = pathComboBox.Text.ExpandEnvars();
                    if (!string.IsNullOrEmpty(currentPath) && File.Exists(currentPath))
                    {
                        dialog.InitialDirectory = Path.GetDirectoryName(currentPath);
                    }
                }
                catch
                {
                    // Ignore errors, dialog will use default directory
                }

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    pathComboBox.Text = dialog.FileName;
                    LoadIcons();
                }
            }
        }

        void LoadButton_Click(object sender, EventArgs e)
        {
            LoadIcons();
        }

        void PathComboBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true; // Prevent the beep sound
                LoadIcons();
            }
        }

        void PathComboBox_SelectionChangeCommitted(object sender, EventArgs e)
        {
            // Automatically load icons when user selects from the dropdown
            Task.Run(() =>
            {
                Thread.Sleep(100); // Small delay to ensure UI updates
                this.Invoke((Action)(() => LoadIcons()));
            });
        }

        void Form_DragEnter(object sender, DragEventArgs e)
        {
            // Check if the dragged data contains file(s)
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        void Form_DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                // Get the file paths from the drag-drop data
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    // Use the first file
                    string filePath = files[0];
                    pathComboBox.Text = filePath;
                    LoadIcons();
                }
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"Error: {ex.Message}";
                MessageBox.Show(this, $"Error loading file:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void LoadIcons()
        {
            iconsPanel.SuspendLayout(); // Suspend layout to prevent flickering
            iconsPanel.Controls.Clear();
            statusLabel.Text = "Loading...";
            Application.DoEvents();

            try
            {
                string filePath = pathComboBox.Text.ExpandEnvars();

                if (!File.Exists(filePath))
                {
                    statusLabel.Text = $"Error: File not found: {filePath}";
                    iconsPanel.ResumeLayout();
                    return;
                }

                // Save to recent files
                IconBrowserSettings.AddRecentFile(pathComboBox.Text);

                // Update combobox items
                if (!pathComboBox.Items.Contains(pathComboBox.Text))
                {
                    pathComboBox.Items.Insert(0, pathComboBox.Text);
                    if (pathComboBox.Items.Count > 10)
                        pathComboBox.Items.RemoveAt(pathComboBox.Items.Count - 1);
                }

                var extractor = new IconExtractor(filePath);
                int iconCount = extractor.Count;

                if (iconCount == 0)
                {
                    statusLabel.Text = "No icons found in file";
                    iconsPanel.ResumeLayout();
                    return;
                }

                for (int i = 0; i < iconCount; i++)
                {
                    try
                    {
                        using (var icon = extractor.GetIcon(i))
                        {
                            if (icon != null)
                            {
                                AddIconPanel(icon, i, filePath);
                            }
                        }
                    }
                    catch
                    {
                        // Skip icons that can't be extracted
                    }
                }

                statusLabel.Text = $"Loaded {iconsPanel.Controls.Count} icons from {Path.GetFileName(filePath)}";
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"Error: {ex.Message}";
                MessageBox.Show(this, $"Error loading icons:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                iconsPanel.ResumeLayout(true); // Resume layout and perform pending layout
            }
        }

        void AddIconPanel(Icon icon, int index, string filePath)
        {
            var panel = new Panel
            {
                Width = 80,
                Height = 100,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(5),
                Cursor = Cursors.Hand,
                Tag = new IconInfo { FilePath = filePath, Index = index }
            };

            // Icon display
            var pictureBox = new PictureBox
            {
                Image = icon.ToBitmap(),
                Size = new Size(48, 48),
                Location = new Point(16, 10),
                SizeMode = PictureBoxSizeMode.CenterImage
            };

            // Index label
            var indexLabel = new Label
            {
                Text = index.ToString(),
                Location = new Point(0, 65),
                Width = 80,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font(this.Font.FontFamily, 8)
            };

            panel.Controls.Add(pictureBox);
            panel.Controls.Add(indexLabel);

            // Tooltip
            string iconPath = $"{Path.GetFileName(filePath)},{index}";
            toolTip.SetToolTip(panel, $"Index: {index}\nClick to copy: {iconPath}");
            toolTip.SetToolTip(pictureBox, $"Index: {index}\nClick to copy: {iconPath}");
            toolTip.SetToolTip(indexLabel, $"Index: {index}\nClick to copy: {iconPath}");

            // Click to copy icon reference
            EventHandler clickHandler = (s, e) =>
            {
                try
                {
                    var info = (IconInfo)panel.Tag;
                    string iconReference = $"{info.FilePath},{info.Index}";
                    Clipboard.SetText(iconReference);
                    statusLabel.Text = $"Copied to clipboard: {iconReference}";

                    // Visual feedback
                    var originalColor = panel.BackColor;
                    panel.BackColor = Color.LightBlue;
                    var timer = new System.Windows.Forms.Timer { Interval = 200 };
                    timer.Tick += (ts, te) =>
                    {
                        panel.BackColor = originalColor;
                        timer.Stop();
                        timer.Dispose();
                    };
                    timer.Start();
                }
                catch (Exception ex)
                {
                    statusLabel.Text = $"Error: {ex.Message}";
                    MessageBox.Show(this, $"Error loading icons:\n{ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            panel.Click += clickHandler;
            pictureBox.Click += clickHandler;
            indexLabel.Click += clickHandler;

            // Hover effect
            panel.MouseEnter += (s, e) => panel.BackColor = Color.FromArgb(240, 240, 255);
            panel.MouseLeave += (s, e) => panel.BackColor = SystemColors.Control;

            iconsPanel.Controls.Add(panel);
        }

        class IconInfo
        {
            public string FilePath { get; set; }
            public int Index { get; set; }
        }
    }
}