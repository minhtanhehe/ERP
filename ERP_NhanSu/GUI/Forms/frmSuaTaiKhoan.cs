using System;
using System.Drawing;
using System.Windows.Forms;
using HR_Management.BLL;
using HR_Management.DTO;

namespace HR_Management.GUI.Forms
{
    [System.ComponentModel.DesignerCategory("Code")]
    public class frmSuaTaiKhoan : Form
    {
        private readonly HeThongBLL _bllHeThong = new HeThongBLL();
        private readonly HeThongDTO _account;

        private TextBox txtMaTK = null!;
        private TextBox txtID_NV = null!;
        private TextBox txtTenNV = null!;
        private TextBox txtTenDangNhap = null!;
        private TextBox txtMatKhauMoi = null!;
        private Button btnToggleShowPass = null!;
        private ComboBox cboQuyenHan = null!;
        private ComboBox cboTrangThai = null!;
        private bool isPasswordMasked = true;

        public frmSuaTaiKhoan(HeThongDTO account)
        {
            _account = account ?? throw new ArgumentNullException(nameof(account));
            InitializeComponent();
            LoadAccountData();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.AutoScaleDimensions = new SizeF(96F, 96F);
            this.Text = "CHỈNH SỬA TÀI KHOẢN HỆ THỐNG";
            this.Font = ThemeColor.BodyFont;
            this.BackColor = ThemeColor.BodyBg;
            this.ClientSize = new Size(620, 580);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            TableLayoutPanel tlpRoot = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Margin = new Padding(0),
                Padding = new Padding(0),
                BackColor = ThemeColor.BodyBg
            };
            tlpRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpRoot.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // Header
            tlpRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Body
            tlpRoot.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // Footer

            // ===== 1. HEADER =====
            TableLayoutPanel pnlHeader = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(18, 14, 18, 14),
                Margin = new Padding(0),
                BackColor = Color.White
            };
            pnlHeader.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pnlHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            pnlHeader.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            pnlHeader.Paint += (s, e) =>
            {
                using (Pen p = new Pen(ThemeColor.CardBorder, 1))
                {
                    e.Graphics.DrawLine(p, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
                }
            };

            Label lblIcon = new Label
            {
                Text = "✏️",
                Font = new Font("Segoe UI Emoji", 18F),
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 0, 12, 0)
            };

            FlowLayoutPanel flpTitle = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill,
                Margin = new Padding(0)
            };

            Label lblTitle = new Label
            {
                Text = "CHỈNH SỬA TÀI KHOẢN HỆ THỐNG",
                Font = new Font("Segoe UI", 12.5F, FontStyle.Bold),
                ForeColor = ThemeColor.Primary,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 3)
            };

            Label lblSubtitle = new Label
            {
                Text = "Cập nhật tên đăng nhập, mật khẩu, phân hệ truy cập hoặc trạng thái tài khoản",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = ThemeColor.TextSecondary,
                AutoSize = true,
                Margin = new Padding(0)
            };

            flpTitle.Controls.Add(lblTitle);
            flpTitle.Controls.Add(lblSubtitle);
            pnlHeader.Controls.Add(lblIcon, 0, 0);
            pnlHeader.Controls.Add(flpTitle, 1, 0);

            // ===== 2. BODY =====
            Panel pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 16, 20, 16),
                BackColor = ThemeColor.BodyBg
            };

            Panel pnlCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(20, 16, 20, 16)
            };
            pnlCard.Paint += (s, e) =>
            {
                using (Pen p = new Pen(ThemeColor.CardBorder, 1))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, pnlCard.Width - 1, pnlCard.Height - 1);
                }
            };

            Font inputFont = new Font("Segoe UI", 10F);
            Color roBack = Color.FromArgb(248, 250, 252);

            txtMaTK = new TextBox { ReadOnly = true, BackColor = roBack, Font = inputFont, BorderStyle = BorderStyle.FixedSingle, Dock = DockStyle.Fill, Margin = new Padding(0, 2, 0, 10) };
            txtID_NV = new TextBox { ReadOnly = true, BackColor = roBack, Font = inputFont, BorderStyle = BorderStyle.FixedSingle, Dock = DockStyle.Fill, Margin = new Padding(0, 2, 0, 10) };
            txtTenNV = new TextBox { ReadOnly = true, BackColor = roBack, Font = inputFont, BorderStyle = BorderStyle.FixedSingle, Dock = DockStyle.Fill, Margin = new Padding(0, 2, 0, 10) };

            txtTenDangNhap = new TextBox { Font = inputFont, BorderStyle = BorderStyle.FixedSingle, Dock = DockStyle.Fill, Margin = new Padding(0, 2, 0, 10) };

            TableLayoutPanel pnlPassHolder = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 2, 0, 10),
                Height = 32
            };
            pnlPassHolder.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            pnlPassHolder.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40F));

            txtMatKhauMoi = new TextBox
            {
                Font = inputFont,
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill,
                UseSystemPasswordChar = true,
                Margin = new Padding(0, 0, 6, 0)
            };
            txtMatKhauMoi.SetPlaceholder("Để trống nếu giữ nguyên mật khẩu cũ...");

            btnToggleShowPass = new Button
            {
                Text = "👁️",
                Font = new Font("Segoe UI Emoji", 10F),
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = ThemeColor.TextSecondary,
                Cursor = Cursors.Hand,
                Margin = new Padding(0)
            };
            btnToggleShowPass.FlatAppearance.BorderSize = 1;
            btnToggleShowPass.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnToggleShowPass.Click += (s, e) =>
            {
                isPasswordMasked = !isPasswordMasked;
                txtMatKhauMoi.UseSystemPasswordChar = isPasswordMasked;
                btnToggleShowPass.Text = isPasswordMasked ? "👁️" : "🙈";
            };
            pnlPassHolder.Controls.Add(txtMatKhauMoi, 0, 0);
            pnlPassHolder.Controls.Add(btnToggleShowPass, 1, 0);

            cboQuyenHan = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = inputFont,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 2, 0, 10)
            };
            cboQuyenHan.Items.AddRange(new object[]
            {
                "BANHANG - Phân hệ Bán hàng",
                "NHANSU - Phân hệ Nhân sự",
                "KHO - Phân hệ Kho",
                "LOGISTIC - Phân hệ Logistics",
                "TAICHINH - Phân hệ Tài chính",
                "SANXUAT - Phân hệ Sản xuất",
                "ALL - Toàn quyền (Quản trị viên)"
            });

            cboTrangThai = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = inputFont,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 2, 0, 10)
            };
            cboTrangThai.Items.AddRange(new object[] { "Hoạt động", "Bị khóa" });

            TableLayoutPanel tlpForm = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 7,
                Margin = new Padding(0)
            };
            tlpForm.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            tlpForm.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));

            int r = 0;
            AddFormRow(tlpForm, r++, "Mã tài khoản:", txtMaTK);
            AddFormRow(tlpForm, r++, "Nhân viên:", txtTenNV);
            AddFormRow(tlpForm, r++, "Tên đăng nhập *:", txtTenDangNhap);
            AddFormRow(tlpForm, r++, "Mật khẩu mới:", pnlPassHolder);
            AddFormRow(tlpForm, r++, "Quyền hạn phân hệ *:", cboQuyenHan);
            AddFormRow(tlpForm, r++, "Trạng thái tài khoản:", cboTrangThai);

            pnlCard.Controls.Add(tlpForm);
            pnlBody.Controls.Add(pnlCard);

            // ===== 3. FOOTER =====
            TableLayoutPanel pnlFooter = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(20, 12, 20, 12),
                BackColor = Color.White
            };
            pnlFooter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            pnlFooter.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pnlFooter.Paint += (s, e) =>
            {
                using (Pen p = new Pen(ThemeColor.CardBorder, 1))
                {
                    e.Graphics.DrawLine(p, 0, 0, pnlFooter.Width, 0);
                }
            };

            FlowLayoutPanel flpBtns = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                Margin = new Padding(0)
            };

            Button btnCancel = UIHelper.CreateButton("Hủy bỏ", Color.FromArgb(148, 163, 184), Color.White, 100, 38);
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            Button btnSave = UIHelper.CreateButton("✓ Lưu thay đổi", ThemeColor.Primary, Color.White, 140, 38);
            btnSave.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btnSave.Margin = new Padding(0, 0, 10, 0);
            btnSave.Click += BtnSave_Click;

            flpBtns.Controls.Add(btnCancel);
            flpBtns.Controls.Add(btnSave);
            pnlFooter.Controls.Add(flpBtns, 1, 0);

            tlpRoot.Controls.Add(pnlHeader, 0, 0);
            tlpRoot.Controls.Add(pnlBody, 0, 1);
            tlpRoot.Controls.Add(pnlFooter, 0, 2);

            this.Controls.Add(tlpRoot);
            this.ResumeLayout(false);
        }

        private void AddFormRow(TableLayoutPanel tlp, int row, string labelText, Control control)
        {
            Label lbl = new Label
            {
                Text = labelText,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = ThemeColor.TextPrimary,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 2, 10, 10)
            };
            tlp.Controls.Add(lbl, 0, row);
            tlp.Controls.Add(control, 1, row);
        }

        private void LoadAccountData()
        {
            txtMaTK.Text = _account.MaTaiKhoan;
            txtID_NV.Text = _account.ID_NV;
            txtTenNV.Text = !string.IsNullOrEmpty(_account.TenNV) ? $"{_account.TenNV} ({_account.ID_NV})" : _account.ID_NV;
            txtTenDangNhap.Text = _account.TenDangNhap;
            txtMatKhauMoi.Text = string.Empty;

            // Quyền hạn
            string q = (_account.QuyenHan ?? "").ToUpperInvariant();
            if (q.Contains("BANHANG") || q.Contains("BH")) cboQuyenHan.SelectedIndex = 0;
            else if (q.Contains("NHANSU") || q.Contains("NS")) cboQuyenHan.SelectedIndex = 1;
            else if (q.Contains("KHO")) cboQuyenHan.SelectedIndex = 2;
            else if (q.Contains("LOGISTIC")) cboQuyenHan.SelectedIndex = 3;
            else if (q.Contains("TAICHINH") || q.Contains("TC") || q.Contains("KT")) cboQuyenHan.SelectedIndex = 4;
            else if (q.Contains("SANXUAT") || q.Contains("SX")) cboQuyenHan.SelectedIndex = 5;
            else if (q == "ALL" || q.Contains("ADMIN")) cboQuyenHan.SelectedIndex = 6;
            else cboQuyenHan.SelectedIndex = 0;

            // Trạng thái
            if (HeThongBLL.IsActiveStatus(_account.TrangThai))
                cboTrangThai.SelectedIndex = 0;
            else
                cboTrangThai.SelectedIndex = 1;
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            string username = txtTenDangNhap.Text.Trim();
            if (!ValidationHelper.IsValidUsername(username, out string errUser))
            {
                MessageBox.Show(errUser, "Tên đăng nhập không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtTenDangNhap.Focus();
                return;
            }

            string selectedRole = cboQuyenHan.SelectedItem?.ToString() ?? "BANHANG - Phân hệ Bán hàng";
            string quyenHanCode = selectedRole.Split('-')[0].Trim();
            string vaiTroName = selectedRole.Contains("-") ? selectedRole.Split('-')[1].Trim() : selectedRole;

            _account.TenDangNhap = username;
            _account.QuyenHan = quyenHanCode;
            _account.VaiTro = vaiTroName;
            _account.TrangThai = cboTrangThai.SelectedItem?.ToString() ?? "Hoạt động";

            string newPass = txtMatKhauMoi.Text.Trim();
            if (!string.IsNullOrEmpty(newPass))
            {
                if (!ValidationHelper.IsValidPassword(newPass, out string errPass))
                {
                    MessageBox.Show(errPass, "Mật khẩu mới không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtMatKhauMoi.Focus();
                    return;
                }
                _account.MatKhau = newPass;
            }

            if (_bllHeThong.Update(_account, out string err))
            {
                MessageBox.Show("Cập nhật tài khoản thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show(err, "Lỗi cập nhật", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
