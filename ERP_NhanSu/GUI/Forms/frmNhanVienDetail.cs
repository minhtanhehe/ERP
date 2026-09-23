using System;
using System.Drawing;
using System.Windows.Forms;
using HR_Management.BLL;
using HR_Management.DTO;

namespace HR_Management.GUI.Forms
{
    [System.ComponentModel.DesignerCategory("Code")]
    public class frmNhanVienDetail : Form
    {
        private readonly NhanVienBLL _bllNhanVien = new NhanVienBLL();
        private readonly PhongBanBLL _bllPhongBan = new PhongBanBLL();

        private bool isNewMode;
        private NhanVienChiTietDTO employeeData;

        private Label lblHeader = null!;

        // Form Controls (Bảng NhanVien)
        private TextBox txtIdNv = null!;
        private TextBox txtTenNv = null!;
        private ComboBox cboGioiTinh = null!;
        private DateTimePicker dtpNgaySinh = null!;
        private TextBox txtSoDienThoai = null!;
        private TextBox txtEmail = null!;
        private TextBox txtDiaChi = null!;
        private ComboBox cboPhongBan = null!;
        private TextBox txtChucVu = null!;
        private NumericUpDown nudLuongCoBan = null!;
        private ComboBox cboTrangThai = null!;

        public bool IsSavedSuccessfully { get; private set; } = false;

        public frmNhanVienDetail(NhanVienChiTietDTO? existingEmployee = null)
        {
            this.isNewMode = (existingEmployee == null);
            this.employeeData = existingEmployee ?? new NhanVienChiTietDTO();

            InitializeComponent();
            LoadDepartments();
            BindData();
        }

        private void InitializeComponent()
        {
            this.Text = "HỒ SƠ NHÂN VIÊN";
            this.ClientSize = new Size(1020, 620);
            this.MinimumSize = new Size(880, 560);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = false;
            this.BackColor = ThemeColor.BodyBg;
            this.Font = ThemeColor.BodyFont;

            // Top Header
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = ThemeColor.Primary,
                Padding = new Padding(24, 16, 24, 16)
            };
            lblHeader = new Label
            {
                Text = "👤 HỒ SƠ NHÂN VIÊN",
                Font = ThemeColor.HeaderFont,
                ForeColor = Color.White,
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblHeader);

            // Center Body (Scrollable Panel)
            Panel pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White,
                Padding = new Padding(36, 24, 36, 24)
            };

            TableLayoutPanel tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 4,
                RowCount = 6,
                Padding = new Padding(0)
            };

            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            for (int i = 0; i < 6; i++)
            {
                tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }

            // Row 0: ID_NV & TenNV
            Label l1 = new Label { Text = "Mã NV (*):", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(4, 12, 12, 12) };
            txtIdNv = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(0, 10, 32, 10) };
            Label l2 = new Label { Text = "Họ và Tên (*):", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(4, 12, 12, 12) };
            txtTenNv = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(0, 10, 8, 10) };

            tlp.Controls.Add(l1, 0, 0); tlp.Controls.Add(txtIdNv, 1, 0);
            tlp.Controls.Add(l2, 2, 0); tlp.Controls.Add(txtTenNv, 3, 0);

            // Row 1: GioiTinh & NgaySinh
            Label l3 = new Label { Text = "Giới tính:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(4, 12, 12, 12) };
            cboGioiTinh = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 10, 32, 10) };
            cboGioiTinh.Items.AddRange(new object[] { "Nam", "Nữ", "Khác" });
            cboGioiTinh.SelectedIndex = 0;
            Label l4 = new Label { Text = "Ngày sinh:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(4, 12, 12, 12) };
            dtpNgaySinh = new DateTimePicker { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Short, Margin = new Padding(0, 10, 8, 10) };

            tlp.Controls.Add(l3, 0, 1); tlp.Controls.Add(cboGioiTinh, 1, 1);
            tlp.Controls.Add(l4, 2, 1); tlp.Controls.Add(dtpNgaySinh, 3, 1);

            // Row 2: SDT & Email
            Label l5 = new Label { Text = "Số điện thoại (*) (10 số):", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(4, 12, 12, 12) };
            txtSoDienThoai = new TextBox { Dock = DockStyle.Fill, MaxLength = 10, Margin = new Padding(0, 10, 32, 10) };
            txtSoDienThoai.KeyPress += (s, e) =>
            {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                {
                    e.Handled = true;
                }
            };
            Label l6 = new Label { Text = "Email (*):", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(4, 12, 12, 12) };
            txtEmail = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(0, 10, 8, 10) };

            tlp.Controls.Add(l5, 0, 2); tlp.Controls.Add(txtSoDienThoai, 1, 2);
            tlp.Controls.Add(l6, 2, 2); tlp.Controls.Add(txtEmail, 3, 2);

            // Row 3: PhongBan & ChucVu
            Label l7 = new Label { Text = "Phòng ban (*):", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(4, 12, 12, 12) };
            cboPhongBan = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 10, 32, 10) };
            Label l8 = new Label { Text = "Chức vụ (*):", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(4, 12, 12, 12) };
            txtChucVu = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(0, 10, 8, 10) };

            tlp.Controls.Add(l7, 0, 3); tlp.Controls.Add(cboPhongBan, 1, 3);
            tlp.Controls.Add(l8, 2, 3); tlp.Controls.Add(txtChucVu, 3, 3);

            // Row 4: LuongCoBan & TrangThai
            Label l9 = new Label { Text = "Lương cơ bản:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(4, 12, 12, 12) };
            nudLuongCoBan = new NumericUpDown { Dock = DockStyle.Fill, Maximum = 1000000000, ThousandsSeparator = true, Increment = 500000, Margin = new Padding(0, 10, 32, 10) };
            Label l10 = new Label { Text = "Trạng thái:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(4, 12, 12, 12) };
            cboTrangThai = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 10, 8, 10) };
            cboTrangThai.Items.AddRange(new object[] { "Đang làm việc", "Thử việc", "Nghỉ thai sản", "Đã nghỉ việc" });
            cboTrangThai.SelectedIndex = 0;

            tlp.Controls.Add(l9, 0, 4); tlp.Controls.Add(nudLuongCoBan, 1, 4);
            tlp.Controls.Add(l10, 2, 4); tlp.Controls.Add(cboTrangThai, 3, 4);

            // Row 5: DiaChi
            Label l11 = new Label { Text = "Địa chỉ liên hệ (*):", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(4, 12, 12, 12) };
            txtDiaChi = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(0, 10, 8, 10) };

            tlp.Controls.Add(l11, 0, 5);
            tlp.Controls.Add(txtDiaChi, 1, 5);
            tlp.SetColumnSpan(txtDiaChi, 3);

            pnlBody.Controls.Add(tlp);

            // Bottom Buttons
            Panel pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 80,
                BackColor = Color.White,
                Padding = new Padding(24, 16, 24, 16)
            };

            FlowLayoutPanel flpButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                FlowDirection = FlowDirection.RightToLeft
            };

            Button btnCancel = UIHelper.CreateButton("✖ Hủy Bỏ", Color.FromArgb(148, 163, 184), Color.White, 120, 44);
            btnCancel.Click += (s, e) => this.Close();

            Button btnSave = UIHelper.CreateButton("💾 Lưu Hồ Sơ", ThemeColor.Success, Color.White, 150, 44);
            btnSave.Margin = new Padding(0, 0, 16, 0);
            btnSave.Click += BtnSave_Click;

            flpButtons.Controls.Add(btnCancel);
            flpButtons.Controls.Add(btnSave);
            pnlBottom.Controls.Add(flpButtons);

            this.Controls.Add(pnlBody);
            this.Controls.Add(pnlBottom);
            this.Controls.Add(pnlHeader);
        }

        private void LoadDepartments()
        {
            var pbs = _bllPhongBan.GetAll();
            cboPhongBan.DisplayMember = "TenPhongBan";
            cboPhongBan.ValueMember = "MaPhongBan";
            cboPhongBan.DataSource = pbs;
        }

        private void BindData()
        {
            if (isNewMode)
            {
                this.Text = "THÊM MỚI HỒ SƠ NHÂN VIÊN";
                lblHeader.Text = "👤 THÊM HỒ SƠ NHÂN VIÊN MỚI";
                txtIdNv.Text = _bllNhanVien.GetNextId();
                txtIdNv.ReadOnly = false;
                dtpNgaySinh.Value = new DateTime(1995, 1, 1);
                nudLuongCoBan.Value = 10000000;
            }
            else
            {
                this.Text = "CẬP NHẬT THÔNG TIN HỒ SƠ NHÂN VIÊN";
                lblHeader.Text = $"👤 HỒ SƠ NHÂN VIÊN: {employeeData.ID_NV} - {employeeData.TenNV}";
                txtIdNv.Text = employeeData.ID_NV;
                txtIdNv.ReadOnly = true;
                txtTenNv.Text = employeeData.TenNV;
                cboGioiTinh.SelectedItem = employeeData.GioiTinh ?? "Nam";
                if (employeeData.NgaySinh.HasValue) dtpNgaySinh.Value = employeeData.NgaySinh.Value;
                txtSoDienThoai.Text = employeeData.SoDienThoai ?? "";
                txtEmail.Text = employeeData.Email ?? "";
                txtDiaChi.Text = employeeData.DiaChi ?? "";
                if (!string.IsNullOrEmpty(employeeData.MaPhongBan)) cboPhongBan.SelectedValue = employeeData.MaPhongBan;
                txtChucVu.Text = employeeData.ChucVu ?? "";
                nudLuongCoBan.Value = employeeData.LuongCoBan ?? 0;
                cboTrangThai.SelectedItem = employeeData.TrangThai ?? "Đang làm việc";
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            string idNv = txtIdNv.Text.Trim();
            string tenNv = txtTenNv.Text.Trim();
            string phone = txtSoDienThoai.Text.Trim();
            string email = txtEmail.Text.Trim();
            string diaChi = txtDiaChi.Text.Trim();
            string chucVu = txtChucVu.Text.Trim();
            string? maPhongBan = cboPhongBan.SelectedValue?.ToString();

            // 1. Kiểm tra Mã nhân viên
            if (string.IsNullOrWhiteSpace(idNv))
            {
                txtIdNv.Focus();
                MessageBox.Show("Mã nhân viên không được để trống!", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 2. Bắt lỗi Họ và tên (không được để trống, đúng định dạng, tối thiểu 2 từ)
            if (!ValidationHelper.IsValidFullName(tenNv, out string errName))
            {
                txtTenNv.Focus();
                MessageBox.Show(errName, "Họ tên không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 3. Bắt lỗi Số điện thoại (10 chữ số, đúng đầu số mạng Việt Nam 03, 05, 07, 08, 09)
            if (!ValidationHelper.IsValidPhoneNumber(phone, out string errPhone))
            {
                txtSoDienThoai.Focus();
                MessageBox.Show(errPhone, "Số điện thoại không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 4. Bắt lỗi Email (đúng chuẩn RFC, tên miền hợp lệ)
            if (!ValidationHelper.IsValidEmail(email, out string errEmail))
            {
                txtEmail.Focus();
                MessageBox.Show(errEmail, "Email không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 5. Bắt lỗi Ngày sinh & Độ tuổi lao động (từ đủ 18 đến 65 tuổi)
            if (!ValidationHelper.IsValidBirthDate(dtpNgaySinh.Value, out string errAge))
            {
                dtpNgaySinh.Focus();
                MessageBox.Show(errAge, "Độ tuổi không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 6. Phòng ban & Chức vụ
            if (string.IsNullOrWhiteSpace(maPhongBan))
            {
                cboPhongBan.Focus();
                MessageBox.Show("Vui lòng chọn phòng ban trực thuộc cho nhân viên!", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(chucVu))
            {
                txtChucVu.Focus();
                MessageBox.Show("Chức vụ của nhân viên không được để trống!", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 7. Địa chỉ liên hệ
            if (string.IsNullOrWhiteSpace(diaChi))
            {
                txtDiaChi.Focus();
                MessageBox.Show("Địa chỉ liên hệ không được để trống!", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 8. Lương cơ bản (> 0)
            if (!ValidationHelper.IsValidSalary(nudLuongCoBan.Value, out string errSalary))
            {
                nudLuongCoBan.Focus();
                MessageBox.Show(errSalary, "Mức lương không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Gán dữ liệu sang DTO
            employeeData.ID_NV = idNv;
            employeeData.TenNV = tenNv;
            employeeData.GioiTinh = cboGioiTinh.SelectedItem?.ToString();
            employeeData.NgaySinh = dtpNgaySinh.Value;
            employeeData.SoDienThoai = phone;
            employeeData.Email = email;
            employeeData.DiaChi = diaChi;
            employeeData.MaPhongBan = maPhongBan;
            employeeData.ChucVu = chucVu;
            employeeData.LuongCoBan = nudLuongCoBan.Value;
            employeeData.TrangThai = cboTrangThai.SelectedItem?.ToString();

            // Kiểm tra nghiệp vụ sâu & trùng lặp CSDL (trùng SĐT, trùng Email, trùng Mã NV)
            if (_bllNhanVien.Save(employeeData, isNewMode, out string error))
            {
                MessageBox.Show(isNewMode ? "Thêm mới nhân viên thành công!" : "Cập nhật thông tin nhân viên thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                IsSavedSuccessfully = true;
                this.Close();
            }
            else
            {
                MessageBox.Show(error, "Lỗi kiểm tra", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
