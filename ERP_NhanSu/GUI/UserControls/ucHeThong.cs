using System;
using System.Drawing;
using System.Windows.Forms;
using HR_Management.BLL;
using HR_Management.DTO;

namespace HR_Management.GUI.UserControls
{
    [System.ComponentModel.DesignerCategory("Code")]
    public class ucHeThong : UserControl
    {
        private readonly HeThongBLL _bllHeThong = new HeThongBLL();
        private readonly NhanVienBLL _bllNhanVien = new NhanVienBLL();
        private DataGridView dgvTaiKhoan = null!;

        public ucHeThong()
        {
            InitializeComponent();
            _bllHeThong.EnsureDefaultAdmin();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = ThemeColor.BodyBg;

            // ===== TOP: Action Buttons Bar =====
            FlowLayoutPanel pnlActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.White,
                Padding = new Padding(16, 12, 16, 12),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true
            };
            pnlActions.Paint += (s, e) =>
            {
                using (Pen p = new Pen(ThemeColor.CardBorder, 1))
                {
                    e.Graphics.DrawLine(p, 0, pnlActions.Height - 1, pnlActions.Width, pnlActions.Height - 1);
                }
            };

            Button btnCapTK = UIHelper.CreateButton("+ Cấp TK Mới", ThemeColor.Primary, Color.White, 130, 38);
            btnCapTK.Margin = new Padding(0, 0, 10, 6);
            btnCapTK.Click += BtnCapTK_Click;

            Button btnSuaTK = UIHelper.CreateButton("✏ Sửa TK", Color.FromArgb(2, 132, 199), Color.White, 115, 38);
            btnSuaTK.Margin = new Padding(0, 0, 10, 6);
            btnSuaTK.Click += BtnSuaTK_Click;

            Button btnXoaTK = UIHelper.CreateButton("🗑 Xóa TK", ThemeColor.Danger, Color.White, 115, 38);
            btnXoaTK.Margin = new Padding(0, 0, 10, 6);
            btnXoaTK.Click += BtnXoaTK_Click;

            Button btnResetPass = UIHelper.CreateButton("🔑 Đặt lại MK", ThemeColor.Warning, Color.White, 130, 38);
            btnResetPass.Margin = new Padding(0, 0, 10, 6);
            btnResetPass.Click += BtnResetPass_Click;

            Button btnToggleStatus = UIHelper.CreateButton("🔒 Khóa / Mở Khóa", Color.FromArgb(100, 116, 139), Color.White, 145, 38);
            btnToggleStatus.Margin = new Padding(0, 0, 10, 6);
            btnToggleStatus.Click += BtnToggleStatus_Click;

            Button btnLamMoi = UIHelper.CreateButton("↻ Làm mới", Color.FromArgb(148, 163, 184), Color.White, 110, 38);
            btnLamMoi.Margin = new Padding(0, 0, 0, 6);
            btnLamMoi.Click += (s, e) => LoadData();

            pnlActions.Controls.AddRange(new Control[] { btnCapTK, btnSuaTK, btnXoaTK, btnResetPass, btnToggleStatus, btnLamMoi });

            // ===== MAIN DATAGRIDVIEW =====
            Panel pnlGrid = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 12, 16, 16),
                BackColor = ThemeColor.BodyBg
            };

            Panel pnlGridCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(1)
            };
            pnlGridCard.Paint += (s, e) =>
            {
                using (Pen p = new Pen(ThemeColor.CardBorder, 1))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, pnlGridCard.Width - 1, pnlGridCard.Height - 1);
                }
            };

            dgvTaiKhoan = new DataGridView { Dock = DockStyle.Fill };
            UIHelper.StyleDataGridView(dgvTaiKhoan);
            dgvTaiKhoan.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            dgvTaiKhoan.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Mã TK", DataPropertyName = "MaTaiKhoan", FillWeight = 10 });
            dgvTaiKhoan.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tên Đăng Nhập", DataPropertyName = "TenDangNhap", FillWeight = 18 });
            dgvTaiKhoan.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Mã NV", DataPropertyName = "ID_NV", FillWeight = 10 });
            dgvTaiKhoan.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tên Nhân Viên", DataPropertyName = "TenNV", FillWeight = 20 });
            dgvTaiKhoan.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Vai Trò", DataPropertyName = "VaiTro", FillWeight = 16 });
            dgvTaiKhoan.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Trạng Thái", DataPropertyName = "TrangThai", FillWeight = 12 });
            dgvTaiKhoan.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Đăng Nhập Cuối", DataPropertyName = "LanDangNhapCuoi", FillWeight = 14, DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy HH:mm" } });

            pnlGridCard.Controls.Add(dgvTaiKhoan);
            pnlGrid.Controls.Add(pnlGridCard);

            this.Controls.Add(pnlGrid);
            this.Controls.Add(pnlActions);
        }

        public void LoadData()
        {
            try
            {
                var list = _bllHeThong.GetAll();
                dgvTaiKhoan.DataSource = null;
                dgvTaiKhoan.DataSource = list;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải danh sách tài khoản: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnCapTK_Click(object? sender, EventArgs e)
        {
            using (var f = new HR_Management.GUI.Forms.frmCapTaiKhoan())
            {
                if (f.ShowDialog() == DialogResult.OK)
                {
                    LoadData();
                }
            }
        }

        private void BtnResetPass_Click(object? sender, EventArgs e)
        {
            if (dgvTaiKhoan.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn tài khoản cần đặt lại mật khẩu!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selected = dgvTaiKhoan.SelectedRows[0].DataBoundItem as HeThongDTO;
            if (selected != null)
            {
                string defaultNewPass = "123456";
                if (MessageBox.Show($"Bạn có chắc chắn muốn đặt lại mật khẩu cho tài khoản '{selected.TenDangNhap}' về mặc định là [{defaultNewPass}]?", "Xác nhận đặt lại", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    if (_bllHeThong.ResetPassword(selected.MaTaiKhoan, defaultNewPass, out string err))
                    {
                        MessageBox.Show("Đặt lại mật khẩu thành công! Mật khẩu mới là: " + defaultNewPass, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show(err, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnSuaTK_Click(object? sender, EventArgs e)
        {
            if (dgvTaiKhoan.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn tài khoản cần sửa!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selected = dgvTaiKhoan.SelectedRows[0].DataBoundItem as HeThongDTO;
            if (selected != null)
            {
                using (var f = new HR_Management.GUI.Forms.frmSuaTaiKhoan(selected))
                {
                    if (f.ShowDialog() == DialogResult.OK)
                    {
                        LoadData();
                    }
                }
            }
        }

        private void BtnXoaTK_Click(object? sender, EventArgs e)
        {
            if (dgvTaiKhoan.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn tài khoản cần xóa!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selected = dgvTaiKhoan.SelectedRows[0].DataBoundItem as HeThongDTO;
            if (selected != null)
            {
                if (selected.TenDangNhap.Equals("admin", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("Không thể xóa tài khoản Quản trị viên (admin) mặc định của hệ thống!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string empInfo = !string.IsNullOrEmpty(selected.TenNV) ? $"{selected.TenNV} ({selected.ID_NV})" : selected.ID_NV;
                if (MessageBox.Show($"Bạn có chắc chắn muốn xóa tài khoản [{selected.TenDangNhap}] của nhân viên [{empInfo}] không?\n\nSau khi xóa, tài khoản này sẽ bị loại bỏ hoàn toàn khỏi hệ thống.",
                                    "Xác nhận xóa tài khoản", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    if (_bllHeThong.Delete(selected.MaTaiKhoan, out string err))
                    {
                        MessageBox.Show($"Xóa tài khoản [{selected.TenDangNhap}] thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadData();
                    }
                    else
                    {
                        MessageBox.Show("Lỗi: " + err, "Lỗi xóa tài khoản", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnToggleStatus_Click(object? sender, EventArgs e)
        {
            if (dgvTaiKhoan.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn tài khoản cần thay đổi trạng thái!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selected = dgvTaiKhoan.SelectedRows[0].DataBoundItem as HeThongDTO;
            if (selected != null)
            {
                string actionText = selected.TrangThai == "Hoạt động" ? "KHÓA" : "MỞ KHÓA";
                if (MessageBox.Show($"Bạn có chắc muốn {actionText} tài khoản [{selected.TenDangNhap}]?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    _bllHeThong.ToggleStatus(selected.MaTaiKhoan, selected.TrangThai);
                    LoadData();
                }
            }
        }
    }
}
