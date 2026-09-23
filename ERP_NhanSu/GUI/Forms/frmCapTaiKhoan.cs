using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using HR_Management.BLL;
using HR_Management.DTO;

namespace HR_Management.GUI.Forms
{
    [System.ComponentModel.DesignerCategory("Code")]
    public class frmCapTaiKhoan : Form
    {
        private readonly HeThongBLL _bllHeThong = new HeThongBLL();
        private readonly NhanVienBLL _bllNhanVien = new NhanVienBLL();
        private readonly PhongBanBLL _bllPhongBan = new PhongBanBLL();

        // Left Panel Controls
        private ComboBox cboPhongBan = null!;
        private TextBox txtSearchNV = null!;
        private DataGridView dgvNhanVien = null!;
        private Label lblTotalFilteredEmps = null!;

        // Right Panel Controls
        private TextBox txtMaNV = null!;
        private TextBox txtTenNV = null!;
        private TextBox txtChucVu = null!;
        private TextBox txtPhongBan = null!;
        private TextBox txtTenDangNhap = null!;
        private TextBox txtMatKhau = null!;
        private Button btnToggleShowPass = null!;
        private TextBox txtQuyenHan = null!;
        private ComboBox cboTrangThai = null!;
        private Label lblAccountStatus = null!;

        // Cached Data
        private List<NhanVienChiTietDTO> allEmployees = new List<NhanVienChiTietDTO>();
        private List<HeThongDTO> existingAccounts = new List<HeThongDTO>();
        private NhanVienChiTietDTO? currentSelectedEmployee = null;
        private bool isPasswordMasked = true;

        public frmCapTaiKhoan()
        {
            InitializeComponent();
            LoadInitialData();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // Phòng trường hợp màn hình nhỏ / DPI cao làm form vượt vùng làm việc
            Rectangle wa = Screen.FromControl(this).WorkingArea;
            if (Width > wa.Width || Height > wa.Height)
            {
                Size = new Size(Math.Min(Width, wa.Width), Math.Min(Height, wa.Height));
                CenterToScreen();
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // [FIX 1] Dùng DPI scaling thay cho Font scaling.
            // Font scaling với AutoScaleDimensions (7,15) làm layout bị co/giãn lệch so với
            // các Font "Segoe UI xx pt" đặt cứng -> chữ to hơn khung chứa.
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.AutoScaleDimensions = new SizeF(96F, 96F);

            this.Text = "CẤP TÀI KHOẢN ĐĂNG NHẬP MỚI";
            this.Font = ThemeColor.BodyFont;
            this.BackColor = ThemeColor.BodyBg;

            // [FIX 2] Tăng kích thước cửa sổ
            this.ClientSize = new Size(1240, 760);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // ================= ROOT CONTAINER =================
            TableLayoutPanel tlpRoot = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Margin = new Padding(0),
                Padding = new Padding(0),
                BackColor = ThemeColor.BodyBg
            };
            // [FIX 3] Luôn khai báo ColumnStyle 100% (mặc định là AutoSize -> con không giãn đúng)
            tlpRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpRoot.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // Header
            tlpRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Body
            tlpRoot.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // Footer

            // ================= 1. HEADER (tự co theo nội dung, không còn Height/Location cứng) =================
            TableLayoutPanel pnlHeader = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(18, 12, 18, 12),
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
                Text = "🔐",
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
                Text = "CẤP TÀI KHOẢN ĐĂNG NHẬP MỚI",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = ThemeColor.Primary,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 3)
            };

            Label lblSubtitle = new Label
            {
                Text = "Chọn nhân viên từ danh sách bên trái và thiết lập tài khoản phân quyền truy cập các phân hệ",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = ThemeColor.TextSecondary,
                AutoSize = true,
                Margin = new Padding(0)
            };

            flpTitle.Controls.Add(lblTitle);
            flpTitle.Controls.Add(lblSubtitle);
            pnlHeader.Controls.Add(lblIcon, 0, 0);
            pnlHeader.Controls.Add(flpTitle, 1, 0);

            // ================= 2. FOOTER (AutoSize) =================
            TableLayoutPanel tlpFooter = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(20, 12, 20, 12),
                Margin = new Padding(0),
                BackColor = Color.White
            };
            tlpFooter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpFooter.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tlpFooter.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlpFooter.Paint += (s, e) =>
            {
                using (Pen p = new Pen(ThemeColor.CardBorder, 1))
                {
                    e.Graphics.DrawLine(p, 0, 0, tlpFooter.Width, 0);
                }
            };

            Label lblTip = new Label
            {
                Text = "💡 Mẹo: Click chọn nhân viên từ danh sách để tự động điền thông tin.",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = ThemeColor.TextSecondary,
                AutoSize = true,
                Anchor = AnchorStyles.Left
            };

            FlowLayoutPanel flpButtons = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                FlowDirection = FlowDirection.RightToLeft,
                Anchor = AnchorStyles.Right,
                Margin = new Padding(0)
            };

            Button btnCancel = UIHelper.CreateButton("Hủy bỏ", Color.FromArgb(148, 163, 184), Color.White, 110, 40);
            btnCancel.Margin = new Padding(0);
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            Button btnSubmit = UIHelper.CreateButton("✓ Xác Nhận Tạo Tài Khoản", ThemeColor.Success, Color.White, 250, 40);
            btnSubmit.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btnSubmit.Margin = new Padding(0, 0, 12, 0);
            btnSubmit.Click += BtnSubmit_Click;

            flpButtons.Controls.Add(btnCancel);
            flpButtons.Controls.Add(btnSubmit);

            tlpFooter.Controls.Add(lblTip, 0, 0);
            tlpFooter.Controls.Add(flpButtons, 1, 0);

            // ================= 3. MAIN BODY =================
            Panel pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Padding = new Padding(16, 12, 16, 12),
                BackColor = ThemeColor.BodyBg
            };

            TableLayoutPanel tlpSplit = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0)
            };
            tlpSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48F));
            tlpSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52F));
            tlpSplit.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // ================= LEFT: EMPLOYEE SELECTION CARD =================
            Panel pnlLeftCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(14, 12, 14, 12),
                Margin = new Padding(0, 0, 8, 0)
            };
            pnlLeftCard.Paint += (s, e) =>
            {
                using (Pen p = new Pen(ThemeColor.CardBorder, 1))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, pnlLeftCard.Width - 1, pnlLeftCard.Height - 1);
                }
            };

            TableLayoutPanel tlpLeft = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            tlpLeft.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpLeft.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // Title
            tlpLeft.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // Filter
            tlpLeft.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // Count
            tlpLeft.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Grid

            // [FIX 4] Label mặc định AutoSize=false -> cao cố định 23px nên bị cắt chữ.
            // Đặt AutoSize = true để hàng AutoSize tính đúng chiều cao theo font/DPI.
            Label lblLeftTitle = new Label
            {
                Text = "1. CHỌN NHÂN VIÊN THEO PHÒNG BAN",
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                ForeColor = ThemeColor.TextPrimary,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 0, 0, 10)
            };

            TableLayoutPanel tlpFilter = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 10),
                Padding = new Padding(0)
            };
            tlpFilter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            tlpFilter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            tlpFilter.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            cboPhongBan = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10F),
                Margin = new Padding(0, 0, 5, 0)
            };
            cboPhongBan.SelectedIndexChanged += (s, e) => FilterEmployees();

            txtSearchNV = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F),
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(5, 0, 0, 0)
            };
            txtSearchNV.SetPlaceholder("Tìm kiếm theo tên / mã NV...");
            txtSearchNV.TextChanged += (s, e) => FilterEmployees();

            tlpFilter.Controls.Add(cboPhongBan, 0, 0);
            tlpFilter.Controls.Add(txtSearchNV, 1, 0);

            lblTotalFilteredEmps = new Label
            {
                Text = "📋 Danh sách hiển thị: Đang tải...",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = ThemeColor.Primary,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 0, 0, 8)
            };

            dgvNhanVien = new DataGridView
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                AutoGenerateColumns = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToResizeRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ScrollBars = ScrollBars.Vertical
            };
            UIHelper.StyleDataGridView(dgvNhanVien);

            dgvNhanVien.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvNhanVien.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 6, 6, 6);
            dgvNhanVien.DefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
            dgvNhanVien.RowTemplate.Height = 34;

            dgvNhanVien.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Mã NV", DataPropertyName = "ID_NV", Width = 80, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            dgvNhanVien.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Họ và Tên", DataPropertyName = "TenNV", MinimumWidth = 150, FillWeight = 40 });
            dgvNhanVien.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Chức Vụ", DataPropertyName = "ChucVu", MinimumWidth = 130, FillWeight = 33 });
            dgvNhanVien.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Phòng Ban", DataPropertyName = "PhongBan", MinimumWidth = 110, FillWeight = 27 });

            dgvNhanVien.SelectionChanged += DgvNhanVien_SelectionChanged;

            tlpLeft.Controls.Add(lblLeftTitle, 0, 0);
            tlpLeft.Controls.Add(tlpFilter, 0, 1);
            tlpLeft.Controls.Add(lblTotalFilteredEmps, 0, 2);
            tlpLeft.Controls.Add(dgvNhanVien, 0, 3);
            pnlLeftCard.Controls.Add(tlpLeft);

            // ================= RIGHT: ACCOUNT DETAILS CARD =================
            Panel pnlRightCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(16, 12, 16, 12),
                Margin = new Padding(8, 0, 0, 0),
                AutoScroll = true
            };
            pnlRightCard.Paint += (s, e) =>
            {
                using (Pen p = new Pen(ThemeColor.CardBorder, 1))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, pnlRightCard.Width - 1, pnlRightCard.Height - 1);
                }
            };

            TableLayoutPanel tlpRight = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 3,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            tlpRight.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpRight.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Title
            tlpRight.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Status banner
            tlpRight.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Form

            Label lblRightTitle = new Label
            {
                Text = "2. THÔNG TIN CẤP TÀI KHOẢN",
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                ForeColor = ThemeColor.TextPrimary,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 0, 0, 10)
            };

            // [FIX 5] Banner: bỏ Height=36 cứng, cho tự co và label tự xuống dòng khi dài
            Panel pnlStatusBanner = new Panel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = ThemeColor.PrimaryLight,
                Padding = new Padding(12, 9, 12, 9),
                Margin = new Padding(0, 0, 0, 10)
            };
            pnlStatusBanner.Paint += (s, e) =>
            {
                using (Pen p = new Pen(Color.FromArgb(191, 219, 254), 1))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, pnlStatusBanner.Width - 1, pnlStatusBanner.Height - 1);
                }
            };

            lblAccountStatus = new Label
            {
                Text = "ℹ️ Click chọn nhân viên từ danh sách bên trái để cấp tài khoản",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = ThemeColor.Primary,
                BackColor = Color.Transparent,
                AutoSize = true,
                Dock = DockStyle.Top
            };
            pnlStatusBanner.Controls.Add(lblAccountStatus);
            pnlStatusBanner.SizeChanged += (s, e) =>
            {
                int w = pnlStatusBanner.ClientSize.Width - pnlStatusBanner.Padding.Horizontal;
                if (w > 0) lblAccountStatus.MaximumSize = new Size(w, 0);
            };

            // ================= RIGHT FORM CONTROLS =================
            Font inputFont = new Font("Segoe UI", 10F);
            Font inputFontBold = new Font("Segoe UI", 10F, FontStyle.Bold);
            Color roBack = Color.FromArgb(248, 250, 252);

            txtMaNV = new TextBox
            {
                ReadOnly = true, BackColor = roBack, Font = inputFontBold,
                ForeColor = ThemeColor.TextPrimary, BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill, Margin = new Padding(0, 2, 8, 14)
            };

            txtTenNV = new TextBox
            {
                ReadOnly = true, BackColor = roBack, Font = inputFontBold,
                ForeColor = ThemeColor.Primary, BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill, Margin = new Padding(8, 2, 0, 14)
            };

            txtChucVu = new TextBox
            {
                ReadOnly = true, BackColor = roBack, Font = inputFont,
                ForeColor = ThemeColor.TextPrimary, BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill, Margin = new Padding(0, 2, 8, 14)
            };

            txtPhongBan = new TextBox
            {
                ReadOnly = true, BackColor = roBack, Font = inputFont,
                ForeColor = ThemeColor.TextPrimary, BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill, Margin = new Padding(8, 2, 0, 14)
            };

            txtTenDangNhap = new TextBox
            {
                Font = inputFont,
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 2, 0, 14)
            };
            txtTenDangNhap.SetPlaceholder("Admin tự nhập tên tài khoản mới...");

            TableLayoutPanel pnlPassHolder = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 2, 0, 14),
                Padding = new Padding(0)
            };
            pnlPassHolder.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            pnlPassHolder.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 48F));
            pnlPassHolder.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            txtMatKhau = new TextBox
            {
                Dock = DockStyle.Fill,
                UseSystemPasswordChar = true,
                Font = inputFont,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 0, 6, 0)
            };
            txtMatKhau.SetPlaceholder("Admin tự nhập mật khẩu mới...");

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
                txtMatKhau.UseSystemPasswordChar = isPasswordMasked;
                btnToggleShowPass.Text = isPasswordMasked ? "👁️" : "🙈";
            };
            pnlPassHolder.Controls.Add(txtMatKhau, 0, 0);
            pnlPassHolder.Controls.Add(btnToggleShowPass, 1, 0);

            txtQuyenHan = new TextBox
            {
                ReadOnly = true,
                BackColor = roBack,
                Font = inputFontBold,
                ForeColor = ThemeColor.Primary,
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 2, 0, 14)
            };

            cboTrangThai = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = inputFont,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 2, 0, 14)
            };
            cboTrangThai.Items.AddRange(new object[] { "Hoạt động", "Bị khóa" });
            cboTrangThai.SelectedIndex = 0;

            // [FIX 6] Form: 12 hàng AutoSize (nhãn / ô nhập xen kẽ).
            // "Quyền hạn" và "Trạng thái" cho chiếm trọn 2 cột để chuỗi dài không bị cắt.
            TableLayoutPanel tlpForm = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 12,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            tlpForm.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpForm.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            for (int i = 0; i < 12; i++)
                tlpForm.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            AddField(tlpForm, 0, CreateFormLabel("MÃ NHÂN VIÊN", false, true), txtMaNV, 0, 1);
            AddField(tlpForm, 0, CreateFormLabel("HỌ VÀ TÊN NHÂN VIÊN", false, false), txtTenNV, 1, 1);

            AddField(tlpForm, 1, CreateFormLabel("CHỨC VỤ", false, true), txtChucVu, 0, 1);
            AddField(tlpForm, 1, CreateFormLabel("PHÒNG BAN", false, false), txtPhongBan, 1, 1);

            AddField(tlpForm, 2, CreateFormLabel("TÊN ĐĂNG NHẬP (TÀI KHOẢN) *", true, true), txtTenDangNhap, 0, 2);
            AddField(tlpForm, 3, CreateFormLabel("MẬT KHẨU *", true, true), pnlPassHolder, 0, 2);
            AddField(tlpForm, 4, CreateFormLabel("QUYỀN HẠN PHÂN HỆ *", true, true), txtQuyenHan, 0, 2);
            AddField(tlpForm, 5, CreateFormLabel("TRẠNG THÁI TÀI KHOẢN", false, true), cboTrangThai, 0, 2);

            tlpRight.Controls.Add(lblRightTitle, 0, 0);
            tlpRight.Controls.Add(pnlStatusBanner, 0, 1);
            tlpRight.Controls.Add(tlpForm, 0, 2);
            pnlRightCard.Controls.Add(tlpRight);

            tlpSplit.Controls.Add(pnlLeftCard, 0, 0);
            tlpSplit.Controls.Add(pnlRightCard, 1, 0);
            pnlBody.Controls.Add(tlpSplit);

            tlpRoot.Controls.Add(pnlHeader, 0, 0);
            tlpRoot.Controls.Add(pnlBody, 0, 1);
            tlpRoot.Controls.Add(tlpFooter, 0, 2);

            this.Controls.Add(tlpRoot);

            this.ResumeLayout(false);
            this.PerformLayout();
        }

        /// <summary>Thêm 1 cặp (nhãn, ô nhập) vào bảng: nhãn ở hàng line*2, ô nhập ở hàng line*2+1.</summary>
        private static void AddField(TableLayoutPanel tlp, int line, Label lbl, Control ctl, int col, int colSpan)
        {
            tlp.Controls.Add(lbl, col, line * 2);
            tlp.SetColumnSpan(lbl, colSpan);
            tlp.Controls.Add(ctl, col, line * 2 + 1);
            tlp.SetColumnSpan(ctl, colSpan);
        }

        private Label CreateFormLabel(string text, bool isRequired, bool isLeft)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = isRequired ? Color.FromArgb(220, 38, 38) : Color.FromArgb(71, 85, 105),
                AutoSize = true,                 // tự tính chiều cao theo font/DPI
                Anchor = AnchorStyles.Left,
                Margin = isLeft ? new Padding(0, 4, 8, 4) : new Padding(8, 4, 0, 4)
            };
        }

        // ====================================================================
        //  PHẦN LOGIC BÊN DƯỚI GIỮ NGUYÊN
        // ====================================================================

        private void LoadInitialData()
        {
            try
            {
                var pbs = _bllPhongBan.GetAll();
                var pbOptions = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("ALL", "-- Tất cả 6 phòng ban --")
                };

                foreach (var pb in pbs)
                {
                    pbOptions.Add(new KeyValuePair<string, string>(pb.MaPhongBan, $"{pb.MaPhongBan} - {pb.TenPhongBan}"));
                }

                cboPhongBan.DataSource = pbOptions;
                cboPhongBan.DisplayMember = "Value";
                cboPhongBan.ValueMember = "Key";
                cboPhongBan.SelectedIndex = 0;

                allEmployees = _bllNhanVien.GetAll();
                existingAccounts = _bllHeThong.GetAll();

                FilterEmployees();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải dữ liệu khởi tạo: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FilterEmployees()
        {
            if (cboPhongBan.SelectedValue == null) return;

            string selectedPB = cboPhongBan.SelectedValue.ToString() ?? "ALL";
            string searchKw = txtSearchNV.Text.Trim().ToLower();

            var filtered = allEmployees.Where(emp =>
            {
                bool matchPB = (selectedPB == "ALL") || (emp.MaPhongBan == selectedPB);
                bool matchKw = string.IsNullOrEmpty(searchKw) ||
                               emp.TenNV.ToLower().Contains(searchKw) ||
                               emp.ID_NV.ToLower().Contains(searchKw) ||
                               (emp.ChucVu != null && emp.ChucVu.ToLower().Contains(searchKw));
                return matchPB && matchKw;
            }).ToList();

            dgvNhanVien.DataSource = null;
            dgvNhanVien.DataSource = filtered;

            lblTotalFilteredEmps.Text = $"📋 Danh sách hiển thị: {filtered.Count} nhân viên";

            if (filtered.Count > 0)
            {
                dgvNhanVien.ClearSelection();
                dgvNhanVien.Rows[0].Selected = true;
            }
            else
            {
                ClearSelection();
            }

            UpdateQuyenHanBySelectedPhongBan();
        }

        private void DgvNhanVien_SelectionChanged(object? sender, EventArgs e)
        {
            if (dgvNhanVien.SelectedRows.Count == 0)
            {
                ClearSelection();
                return;
            }

            var emp = dgvNhanVien.SelectedRows[0].DataBoundItem as NhanVienChiTietDTO;
            if (emp == null) return;

            currentSelectedEmployee = emp;

            txtMaNV.Text = emp.ID_NV;
            txtTenNV.Text = emp.TenNV;
            txtChucVu.Text = emp.ChucVu ?? "Nhân viên";
            txtPhongBan.Text = !string.IsNullOrEmpty(emp.PhongBan) ? emp.PhongBan : emp.MaPhongBan ?? "";

            txtTenDangNhap.Text = string.Empty;
            txtMatKhau.Text = string.Empty;

            UpdateQuyenHanBySelectedPhongBan(emp);

            var acc = existingAccounts.FirstOrDefault(a => a.ID_NV == emp.ID_NV);
            if (acc != null)
            {
                lblAccountStatus.Text = $"⚠️ Nhân viên đã có tài khoản: [{acc.TenDangNhap}] - Quyền: {acc.QuyenHan} ({acc.TrangThai})";
                lblAccountStatus.ForeColor = Color.FromArgb(180, 83, 9);
                if (lblAccountStatus.Parent != null) lblAccountStatus.Parent.BackColor = Color.FromArgb(254, 243, 199);
            }
            else
            {
                lblAccountStatus.Text = $"✅ Nhân viên chưa có tài khoản — Sẵn sàng cấp tài khoản mới";
                lblAccountStatus.ForeColor = Color.FromArgb(4, 120, 87);
                if (lblAccountStatus.Parent != null) lblAccountStatus.Parent.BackColor = Color.FromArgb(236, 253, 245);
            }
        }

        private void UpdateQuyenHanBySelectedPhongBan(NhanVienChiTietDTO? emp = null)
        {
            string selectedPB = cboPhongBan.SelectedValue?.ToString() ?? "ALL";

            if (selectedPB != "ALL")
            {
                txtQuyenHan.Text = GetRoleByDepartment(selectedPB, cboPhongBan.Text);
            }
            else
            {
                var currentEmp = emp ?? currentSelectedEmployee;
                if (currentEmp != null && !string.IsNullOrWhiteSpace(currentEmp.MaPhongBan))
                {
                    txtQuyenHan.Text = GetRoleByDepartment(currentEmp.MaPhongBan, currentEmp.PhongBan);
                }
                else
                {
                    txtQuyenHan.Text = "ALL - Toàn quyền (Quản trị viên / Trưởng phòng)";
                }
            }
        }

        private static string GetRoleByDepartment(string? maPb, string? tenPb = null)
        {
            if (string.IsNullOrWhiteSpace(maPb) || maPb.Equals("ALL", StringComparison.OrdinalIgnoreCase))
            {
                return "ALL - Toàn quyền (Quản trị viên / Trưởng phòng)";
            }

            string code = maPb.Trim().ToUpperInvariant();
            string name = (tenPb ?? "").Trim().ToLowerInvariant();

            // 1. Phân hệ Bán hàng (PB02 - Phòng Bán hàng)
            if (name.Contains("bán hàng") || name.Contains("kinh doanh") || code == "PB02" || code.Contains("BH"))
                return "BANHANG - Phân hệ Bán hàng";

            // 2. Phân hệ Nhân sự (PB01 - Phòng Nhân sự)
            if (name.Contains("nhân sự") || code == "PB01" || code.Contains("NS"))
                return "NHANSU - Phân hệ Nhân sự";

            // 3. Phân hệ Kho (PB03 - Phòng Kho)
            if (name.Contains("kho") || code == "PB03" || code.Contains("KHO"))
                return "KHO - Phân hệ Kho";

            // 4. Phân hệ Logistics (PB04 - Phòng Logistic)
            if (name.Contains("logistic") || name.Contains("giao nhận") || name.Contains("vận chuyển") || code == "PB04" || code.Contains("LOG"))
                return "LOGISTIC - Phân hệ Logistics";

            // 5. Phân hệ Tài chính (PB05 - Phòng Tài chính)
            if (name.Contains("tài chính") || name.Contains("kế toán") || code == "PB05" || code.Contains("TC") || code.Contains("KT"))
                return "TAICHINH - Phân hệ Tài chính";

            // 6. Phân hệ Sản xuất
            if (name.Contains("sản xuất") || code == "PB06" || code.Contains("SX"))
                return "SANXUAT - Phân hệ Sản xuất";

            return "BANHANG - Phân hệ Bán hàng";
        }

        private void ClearSelection()
        {
            currentSelectedEmployee = null;
            txtMaNV.Text = string.Empty;
            txtTenNV.Text = string.Empty;
            txtChucVu.Text = string.Empty;
            txtPhongBan.Text = string.Empty;
            txtTenDangNhap.Text = string.Empty;
            txtMatKhau.Text = string.Empty;
            UpdateQuyenHanBySelectedPhongBan();
            cboTrangThai.SelectedIndex = 0;
            lblAccountStatus.Text = "ℹ️ Click chọn nhân viên từ danh sách bên trái để cấp tài khoản";
            lblAccountStatus.ForeColor = ThemeColor.Primary;
            if (lblAccountStatus.Parent != null) lblAccountStatus.Parent.BackColor = ThemeColor.PrimaryLight;
        }

        private void BtnSubmit_Click(object? sender, EventArgs e)
        {
            if (currentSelectedEmployee == null || string.IsNullOrWhiteSpace(txtMaNV.Text))
            {
                MessageBox.Show("Vui lòng click chọn một nhân viên từ danh sách bên trái!", "Chưa chọn nhân viên", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string username = txtTenDangNhap.Text.Trim();
            string password = txtMatKhau.Text.Trim();

            if (!ValidationHelper.IsValidUsername(username, out string errUser))
            {
                MessageBox.Show(errUser, "Tên đăng nhập không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtTenDangNhap.Focus();
                return;
            }

            if (!ValidationHelper.IsValidPassword(password, out string errPass))
            {
                MessageBox.Show(errPass, "Mật khẩu không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtMatKhau.Focus();
                return;
            }

            var existingAcc = existingAccounts.FirstOrDefault(a => a.TenDangNhap.Equals(username, StringComparison.OrdinalIgnoreCase));
            if (existingAcc != null)
            {
                MessageBox.Show($"Tên đăng nhập '{username}' đã được sử dụng bởi người khác, vui lòng chọn tên đăng nhập khác!", "Trùng tên đăng nhập", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtTenDangNhap.Focus();
                return;
            }

            var currentAcc = existingAccounts.FirstOrDefault(a => a.ID_NV == currentSelectedEmployee.ID_NV);
            if (currentAcc != null)
            {
                if (MessageBox.Show($"Nhân viên '{currentSelectedEmployee.TenNV}' hiện đã có tài khoản [{currentAcc.TenDangNhap}]. Bạn có chắc chắn muốn cấp thêm tài khoản mới không?", "Xác nhận cấp thêm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    return;
                }
            }

            string rawRole = txtQuyenHan.Text.Trim();
            if (string.IsNullOrWhiteSpace(rawRole)) rawRole = "NHANSU - Phân hệ Nhân sự";
            string quyenHanCode = rawRole.Split('-')[0].Trim();
            string vaiTroName = rawRole.Contains("-") ? rawRole.Split('-')[1].Trim() : rawRole;

            var newAcc = new HeThongDTO
            {
                MaTaiKhoan = _bllHeThong.GetNextId(),
                ID_NV = currentSelectedEmployee.ID_NV,
                TenDangNhap = username,
                MatKhau = password,
                VaiTro = vaiTroName,
                TrangThai = cboTrangThai.SelectedItem?.ToString() ?? "Hoạt động",
                QuyenHan = quyenHanCode
            };

            if (_bllHeThong.CreateAccount(newAcc, out string err))
            {
                MessageBox.Show($"Cấp tài khoản mới thành công!\n- Nhân viên: {currentSelectedEmployee.TenNV}\n- Tên đăng nhập: {username}\n- Quyền hạn (quyenHan): {newAcc.QuyenHan}\n- Vai trò: {newAcc.VaiTro}", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Lỗi cấp tài khoản: " + err, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}