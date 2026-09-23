using System;
using System.Drawing;
using System.Windows.Forms;
using HR_Management.BLL;
using HR_Management.DTO;

namespace HR_Management.GUI.Forms
{
    [System.ComponentModel.DesignerCategory("Code")]
    public class frmHopDongDetail : Form
    {
        private readonly HopDongBLL _bllHopDong = new HopDongBLL();
        private readonly NhanVienBLL _bllNhanVien = new NhanVienBLL();

        private bool isNewMode;
        private HopDongDTO hopDongData;

        private Label lblHeader = null!;

        private TextBox txtMaHopDong = null!;
        private ComboBox cboNhanVien = null!;
        private ComboBox cboLoaiHopDong = null!;
        private DateTimePicker dtpNgayKy = null!;
        private DateTimePicker dtpNgayBatDau = null!;
        private DateTimePicker dtpNgayKetThuc = null!;
        private CheckBox chkKhongThoiHan = null!;
        private NumericUpDown nudLuongCoBan = null!;
        private ComboBox cboTrangThai = null!;

        public bool IsSavedSuccessfully { get; private set; } = false;

        public frmHopDongDetail(HopDongDTO? existingHopDong = null)
        {
            this.isNewMode = (existingHopDong == null);
            this.hopDongData = existingHopDong ?? new HopDongDTO();

            InitializeComponent();
            LoadEmployees();
            BindData();
        }

        private void InitializeComponent()
        {
            this.Text = "HỢP ĐỒNG LAO ĐỘNG";
            this.ClientSize = new Size(1400, 800);
            this.MinimumSize = new Size(1400, 800);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = false;
            this.BackColor = Color.White;
            this.Font = ThemeColor.BodyFont;

            // Header
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = ThemeColor.Primary,
                Padding = new Padding(24, 16, 24, 16)
            };
            lblHeader = new Label
            {
                Text = "📝 HỢP ĐỒNG LAO ĐỘNG",
                Font = ThemeColor.HeaderFont,
                ForeColor = Color.White,
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblHeader);

            // Form Content
            TableLayoutPanel tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3, // Label, Input, Checkbox
                RowCount = 8,
                Padding = new Padding(36, 24, 36, 24),
                AutoScroll = true
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220F));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));

            for (int i = 0; i < 8; i++)
            {
                tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }

            // Ma Hop Dong
            Label l1 = new Label { Text = "Mã hợp đồng (*):", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(4, 12, 16, 12) };
            txtMaHopDong = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(0, 10, 16, 10) };
            tlp.Controls.Add(l1, 0, 0); tlp.Controls.Add(txtMaHopDong, 1, 0);
            tlp.SetColumnSpan(txtMaHopDong, 2);

            // Nhan Vien
            Label l2 = new Label { Text = "Nhân viên (*):", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(4, 12, 16, 12) };
            cboNhanVien = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 10, 16, 10) };
            tlp.Controls.Add(l2, 0, 1); tlp.Controls.Add(cboNhanVien, 1, 1);
            tlp.SetColumnSpan(cboNhanVien, 2);

            // Loai Hop Dong
            Label l3 = new Label { Text = "Loại hợp đồng:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(4, 12, 16, 12) };
            cboLoaiHopDong = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 10, 16, 10) };
            cboLoaiHopDong.Items.AddRange(new object[] {
                "Hợp đồng thử việc (2 tháng)",
                "Hợp đồng xác định thời hạn 1 năm",
                "Hợp đồng xác định thời hạn 3 năm",
                "Hợp đồng không xác định thời hạn"
            });
            cboLoaiHopDong.SelectedIndex = 1;
            cboLoaiHopDong.SelectedIndexChanged += CboLoaiHopDong_SelectedIndexChanged;
            tlp.Controls.Add(l3, 0, 2); tlp.Controls.Add(cboLoaiHopDong, 1, 2);
            tlp.SetColumnSpan(cboLoaiHopDong, 2);

            // Ngay Ky
            Label l4 = new Label { Text = "Ngày ký kết:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(4, 12, 16, 12) };
            dtpNgayKy = new DateTimePicker { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Short, Margin = new Padding(0, 10, 16, 10) };
            tlp.Controls.Add(l4, 0, 3); tlp.Controls.Add(dtpNgayKy, 1, 3);
            tlp.SetColumnSpan(dtpNgayKy, 2);

            // Ngay Bat Dau
            Label l5 = new Label { Text = "Ngày bắt đầu hiệu lực:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(4, 12, 16, 12) };
            dtpNgayBatDau = new DateTimePicker { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Short, Margin = new Padding(0, 10, 16, 10) };
            tlp.Controls.Add(l5, 0, 4); tlp.Controls.Add(dtpNgayBatDau, 1, 4);
            tlp.SetColumnSpan(dtpNgayBatDau, 2);

            // Ngay Ket Thuc & Checkbox
            Label l6 = new Label { Text = "Ngày kết thúc:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(4, 12, 16, 12) };
            dtpNgayKetThuc = new DateTimePicker { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Short, Margin = new Padding(0, 10, 16, 10) };
            chkKhongThoiHan = new CheckBox { Text = "Vô thời hạn", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(12, 12, 8, 12) };
            chkKhongThoiHan.CheckedChanged += (s, e) => dtpNgayKetThuc.Enabled = !chkKhongThoiHan.Checked;
            tlp.Controls.Add(l6, 0, 5); tlp.Controls.Add(dtpNgayKetThuc, 1, 5); tlp.Controls.Add(chkKhongThoiHan, 2, 5);

            // Luong Co Ban
            Label l7 = new Label { Text = "Lương thỏa thuận (VNĐ):", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(4, 12, 16, 12) };
            nudLuongCoBan = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                Maximum = 1000000000,
                ThousandsSeparator = true,
                Increment = 500000,
                Margin = new Padding(0, 10, 16, 10)
            };
            tlp.Controls.Add(l7, 0, 6); tlp.Controls.Add(nudLuongCoBan, 1, 6);
            tlp.SetColumnSpan(nudLuongCoBan, 2);

            // Trang Thai
            Label l8 = new Label { Text = "Trạng thái hợp đồng:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(4, 12, 16, 12) };
            cboTrangThai = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 10, 16, 10) };
            cboTrangThai.Items.AddRange(new object[] { "Hiệu lực", "Hết hạn", "Đã chấm dứt" });
            cboTrangThai.SelectedIndex = 0;
            tlp.Controls.Add(l8, 0, 7); tlp.Controls.Add(cboTrangThai, 1, 7);
            tlp.SetColumnSpan(cboTrangThai, 2);

            // Buttons
            Panel pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 90,
                BackColor = ThemeColor.BodyBg,
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

            Button btnSave = UIHelper.CreateButton("💾 Lưu Hợp Đồng", ThemeColor.Success, Color.White, 150, 44);
            btnSave.Margin = new Padding(0, 0, 16, 0);
            btnSave.Click += BtnSave_Click;

            flpButtons.Controls.Add(btnCancel);
            flpButtons.Controls.Add(btnSave);
            pnlBottom.Controls.Add(flpButtons);

            this.Controls.Add(tlp);
            this.Controls.Add(pnlBottom);
            this.Controls.Add(pnlHeader);
        }

        private void CboLoaiHopDong_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cboLoaiHopDong.SelectedIndex == 0) // Thu viec 2 thang
            {
                chkKhongThoiHan.Checked = false;
                dtpNgayKetThuc.Value = dtpNgayBatDau.Value.AddMonths(2);
            }
            else if (cboLoaiHopDong.SelectedIndex == 1) // 1 nam
            {
                chkKhongThoiHan.Checked = false;
                dtpNgayKetThuc.Value = dtpNgayBatDau.Value.AddYears(1);
            }
            else if (cboLoaiHopDong.SelectedIndex == 2) // 3 nam
            {
                chkKhongThoiHan.Checked = false;
                dtpNgayKetThuc.Value = dtpNgayBatDau.Value.AddYears(3);
            }
            else if (cboLoaiHopDong.SelectedIndex == 3) // Khong xac dinh
            {
                chkKhongThoiHan.Checked = true;
            }
        }

        private void LoadEmployees()
        {
            var emps = _bllNhanVien.GetAll();
            cboNhanVien.DisplayMember = "TenNV";
            cboNhanVien.ValueMember = "ID_NV";
            cboNhanVien.DataSource = emps;
        }

        private void BindData()
        {
            if (isNewMode)
            {
                this.Text = "KÝ MỚI HỢP ĐỒNG LAO ĐỘNG";
                lblHeader.Text = "📝 KÝ MỚI HỢP ĐỒNG LAO ĐỘNG";
                txtMaHopDong.Text = _bllHopDong.GetNextId();
                txtMaHopDong.ReadOnly = false;
                dtpNgayKy.Value = DateTime.Today;
                dtpNgayBatDau.Value = DateTime.Today;
                dtpNgayKetThuc.Value = DateTime.Today.AddYears(1);
                nudLuongCoBan.Value = 10000000;
            }
            else
            {
                this.Text = "ĐIỀU CHỈNH / GIA HẠN HỢP ĐỒNG LAO ĐỘNG";
                lblHeader.Text = $"📝 CẬP NHẬT HỢP ĐỒNG: {hopDongData.MaHopDong}";
                txtMaHopDong.Text = hopDongData.MaHopDong;
                txtMaHopDong.ReadOnly = true;
                cboNhanVien.SelectedValue = hopDongData.ID_NV;
                cboLoaiHopDong.SelectedItem = hopDongData.LoaiHopDong;
                dtpNgayKy.Value = hopDongData.NgayKy;
                dtpNgayBatDau.Value = hopDongData.NgayBatDau;

                if (hopDongData.NgayKetThuc.HasValue)
                {
                    chkKhongThoiHan.Checked = false;
                    dtpNgayKetThuc.Value = hopDongData.NgayKetThuc.Value;
                }
                else
                {
                    chkKhongThoiHan.Checked = true;
                }

                nudLuongCoBan.Value = hopDongData.LuongCoBan;
                cboTrangThai.SelectedItem = hopDongData.TrangThai;
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            string maHd = txtMaHopDong.Text.Trim();
            string? idNv = cboNhanVien.SelectedValue?.ToString();
            string? loaiHd = cboLoaiHopDong.SelectedItem?.ToString();

            if (string.IsNullOrWhiteSpace(maHd))
            {
                txtMaHopDong.Focus();
                MessageBox.Show("Mã hợp đồng không được để trống!", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(idNv))
            {
                cboNhanVien.Focus();
                MessageBox.Show("Vui lòng chọn nhân viên ký hợp đồng!", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(loaiHd))
            {
                cboLoaiHopDong.Focus();
                MessageBox.Show("Vui lòng chọn loại hợp đồng lao động!", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!chkKhongThoiHan.Checked && dtpNgayKetThuc.Value.Date <= dtpNgayBatDau.Value.Date)
            {
                dtpNgayKetThuc.Focus();
                MessageBox.Show("Ngày kết thúc hợp đồng phải sau ngày bắt đầu hợp đồng!", "Thời gian không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!ValidationHelper.IsValidSalary(nudLuongCoBan.Value, out string errSalary))
            {
                nudLuongCoBan.Focus();
                MessageBox.Show(errSalary, "Mức lương không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            hopDongData.MaHopDong = maHd;
            hopDongData.ID_NV = idNv;
            hopDongData.LoaiHopDong = loaiHd;
            hopDongData.NgayKy = dtpNgayKy.Value;
            hopDongData.NgayBatDau = dtpNgayBatDau.Value;
            hopDongData.NgayKetThuc = chkKhongThoiHan.Checked ? (DateTime?)null : dtpNgayKetThuc.Value;
            hopDongData.LuongCoBan = nudLuongCoBan.Value;
            hopDongData.TrangThai = cboTrangThai.SelectedItem?.ToString() ?? "Hiệu lực";

            if (_bllHopDong.Save(hopDongData, isNewMode, out string error))
            {
                MessageBox.Show(isNewMode ? "Ký mới hợp đồng thành công!" : "Cập nhật hợp đồng thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                IsSavedSuccessfully = true;
                this.Close();
            }
            else
            {
                MessageBox.Show(error, "Lỗi kiểm tra dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
