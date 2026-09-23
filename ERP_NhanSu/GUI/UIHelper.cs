using System;
using System.Drawing;
using System.Windows.Forms;

namespace HR_Management.GUI
{
    public static class UIHelper
    {
        public static void StyleDataGridView(DataGridView dgv)
        {
            // Enable double buffering via reflection to eliminate flickering
            try
            {
                typeof(DataGridView).InvokeMember("DoubleBuffered",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.SetProperty,
                    null, dgv, new object[] { true });
            }
            catch { }

            dgv.BackgroundColor = Color.White;
            dgv.BorderStyle = BorderStyle.None;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgv.GridColor = Color.FromArgb(241, 245, 249);
            dgv.RowHeadersVisible = false;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.MultiSelect = false;
            dgv.AutoGenerateColumns = false;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.AllowUserToResizeRows = false;
            dgv.RowTemplate.Height = 44;
            dgv.Font = ThemeColor.BodyFont;

            // Header styling
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(71, 85, 105);
            dgv.ColumnHeadersDefaultCellStyle.Font = ThemeColor.BodyFontBold;
            dgv.ColumnHeadersDefaultCellStyle.Padding = new Padding(14, 0, 14, 0);
            dgv.ColumnHeadersHeight = 46;

            // Rows styling
            dgv.DefaultCellStyle.BackColor = Color.White;
            dgv.DefaultCellStyle.ForeColor = Color.FromArgb(15, 23, 42);
            dgv.DefaultCellStyle.Padding = new Padding(14, 0, 14, 0);
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(239, 246, 255);
            dgv.DefaultCellStyle.SelectionForeColor = Color.FromArgb(29, 78, 216);

            // Alternating rows
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 251, 253);

            // Automatic Status Badge & Role Badge Pill Rendering
            dgv.CellPainting += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && e.Value != null)
                {
                    string colName = dgv.Columns[e.ColumnIndex].DataPropertyName;
                    if (string.IsNullOrEmpty(colName)) colName = dgv.Columns[e.ColumnIndex].Name;

                    if ((colName.Equals("TrangThai", StringComparison.OrdinalIgnoreCase) || 
                         colName.Equals("VaiTro", StringComparison.OrdinalIgnoreCase) ||
                         colName.Equals("LoaiBaoCao", StringComparison.OrdinalIgnoreCase)) && e.Graphics != null)
                    {
                        e.PaintBackground(e.CellBounds, true);
                        string text = e.Value.ToString() ?? "";

                        Color bg = ThemeColor.NeutralBg;
                        Color fg = ThemeColor.NeutralText;

                        if (text.Contains("Hoạt động") || text.Contains("Đang làm") || text.Contains("Hiệu lực"))
                        {
                            bg = ThemeColor.SuccessBg; fg = ThemeColor.SuccessText;
                        }
                        else if (text.Contains("Thử việc") || text.Contains("Sắp hết hạn") || text.Contains("Tạm ngừng") || text.Contains("Biến động"))
                        {
                            bg = ThemeColor.WarningBg; fg = ThemeColor.WarningText;
                        }
                        else if (text.Contains("nghỉ") || text.Contains("Hết hạn") || text.Contains("chấm dứt") || text.Contains("Khóa"))
                        {
                            bg = ThemeColor.DangerBg; fg = ThemeColor.DangerText;
                        }
                        else if (text.Contains("Admin") || text.Contains("Quản trị"))
                        {
                            bg = ThemeColor.PurpleBg; fg = ThemeColor.PurpleText;
                        }
                        else if (text.Contains("Trưởng phòng") || text.Contains("Hợp đồng"))
                        {
                            bg = ThemeColor.InfoBg; fg = ThemeColor.InfoText;
                        }
                        else if (text.Contains("HR") || text.Contains("Nhân sự") || text.Contains("Cơ cấu") || text.Contains("Phòng ban"))
                        {
                            bg = ThemeColor.PrimaryLight; fg = ThemeColor.Primary;
                        }
                        else if (text.Contains("Quỹ lương") || text.Contains("Lương"))
                        {
                            bg = ThemeColor.SuccessBg; fg = ThemeColor.SuccessText;
                        }

                        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        SizeF sz = e.Graphics.MeasureString(text, ThemeColor.SmallFontBold);
                        int pillW = (int)sz.Width + 20;
                        int pillH = 26;
                        int pillX = e.CellBounds.X + 12;
                        int pillY = e.CellBounds.Y + (e.CellBounds.Height - pillH) / 2;

                        if (pillW > e.CellBounds.Width - 16) pillW = e.CellBounds.Width - 16;
                        if (pillW < 40) pillW = 40;

                        Rectangle pillRect = new Rectangle(pillX, pillY, pillW, pillH);
                        using (System.Drawing.Drawing2D.GraphicsPath path = GetRoundedPath(pillRect, 6))
                        {
                            using (Brush b = new SolidBrush(bg))
                            {
                                e.Graphics.FillPath(b, path);
                            }
                        }

                        TextRenderer.DrawText(e.Graphics, text, ThemeColor.SmallFontBold, pillRect, fg,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

                        e.Handled = true;
                    }
                }
            };
        }

        private static System.Drawing.Drawing2D.GraphicsPath GetRoundedPath(Rectangle rect, int radius)
        {
            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            float r = radius * 2f;
            path.AddArc(rect.X, rect.Y, r, r, 180, 90);
            path.AddArc(rect.Right - r, rect.Y, r, r, 270, 90);
            path.AddArc(rect.Right - r, rect.Bottom - r, r, r, 0, 90);
            path.AddArc(rect.X, rect.Bottom - r, r, r, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static Button CreateButton(string text, Color backColor, Color foreColor, int width = 130, int height = 44)
        {
            Size sz = TextRenderer.MeasureText(text, ThemeColor.BodyFontBold);
            int finalWidth = Math.Max(width, sz.Width + 24);

            Button btn = new Button
            {
                Text = text,
                BackColor = backColor,
                ForeColor = foreColor,
                Size = new Size(finalWidth, height),
                MinimumSize = new Size(finalWidth, height),
                MaximumSize = new Size(0, height),
                AutoSize = false,
                Padding = Padding.Empty,
                TextAlign = ContentAlignment.MiddleCenter,
                UseCompatibleTextRendering = false,
                FlatStyle = FlatStyle.Flat,
                Font = ThemeColor.BodyFontBold,
                Cursor = Cursors.Hand,
                Margin = new Padding(6)
            };
            btn.FlatAppearance.BorderSize = 0;

            // Map hover colors with modern lightness
            Color hoverColor = ControlPaint.Light(backColor);
            if (backColor == ThemeColor.Primary) hoverColor = ThemeColor.PrimaryHover;
            else if (backColor == ThemeColor.Success) hoverColor = ThemeColor.SuccessHover;
            else if (backColor == ThemeColor.Warning) hoverColor = ThemeColor.WarningHover;
            else if (backColor == ThemeColor.Danger) hoverColor = ThemeColor.DangerHover;
            else if (backColor == ThemeColor.Info) hoverColor = ThemeColor.InfoHover;
            else if (backColor == Color.FromArgb(100, 116, 139)) hoverColor = Color.FromArgb(71, 85, 105);

            btn.FlatAppearance.MouseOverBackColor = hoverColor;
            btn.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(hoverColor);

            return btn;
        }

        public static Panel CreateCard(int width = 0, int height = 0)
        {
            Panel pnl = new Panel
            {
                BackColor = ThemeColor.CardBg,
                Padding = new Padding(16)
            };
            if (width > 0 && height > 0)
            {
                pnl.Size = new Size(width, height);
            }
            pnl.Paint += (s, e) =>
            {
                using (Pen p = new Pen(ThemeColor.CardBorder, 1))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, pnl.Width - 1, pnl.Height - 1);
                }
            };
            return pnl;
        }

        public static Panel CreateKpiCard(string title, Label valueLabel, Color accentColor, string subtitle = "Thực tế hệ thống")
        {
            Panel card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(6),
                Padding = new Padding(16, 10, 16, 10),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            card.Paint += (s, e) =>
            {
                using (Pen borderPen = new Pen(ThemeColor.CardBorder, 1))
                {
                    e.Graphics.DrawRectangle(borderPen, 0, 0, card.Width - 1, card.Height - 1);
                }
                using (Brush accentBrush = new SolidBrush(accentColor))
                {
                    e.Graphics.FillRectangle(accentBrush, 0, 0, 4, card.Height);
                }
            };

            TableLayoutPanel tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.Transparent,
                Padding = new Padding(6, 12, 0, 12),
                Margin = new Padding(0),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));  // Title
            tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));  // Value
            tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));  // Subtitle

            Label lblCardTitle = new Label
            {
                Text = title.ToUpper(),
                Font = ThemeColor.SmallFontBold,
                ForeColor = ThemeColor.TextSecondary,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4)
            };

            valueLabel.Text = "0";
            valueLabel.Font = new Font("Segoe UI", 24F, FontStyle.Bold);
            valueLabel.ForeColor = accentColor;
            valueLabel.Dock = DockStyle.Fill;
            valueLabel.TextAlign = ContentAlignment.MiddleLeft;
            valueLabel.AutoSize = true;
            valueLabel.Margin = new Padding(0, 0, 0, 4);

            Label lblSub = new Label
            {
                Text = subtitle,
                Font = ThemeColor.SmallFont,
                ForeColor = ThemeColor.TextMuted,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true,
                Margin = new Padding(0)
            };

            tlp.Controls.Add(lblCardTitle, 0, 0);
            tlp.Controls.Add(valueLabel, 0, 1);
            tlp.Controls.Add(lblSub, 0, 2);

            card.Controls.Add(tlp);

            return card;
        }
    }

    public static class TextBoxExtensions
    {
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string lParam);
        private const int EM_SETCUEBANNER = 0x1501;

        public static void SetPlaceholder(this TextBox textBox, string placeholder)
        {
            if (textBox.IsHandleCreated)
            {
                SendMessage(textBox.Handle, EM_SETCUEBANNER, 0, placeholder);
            }
            else
            {
                textBox.HandleCreated += (s, e) => SendMessage(textBox.Handle, EM_SETCUEBANNER, 0, placeholder);
            }
        }
    }
}
