using System;
using System.Drawing;
using System.Windows.Forms;
using HR_Management.BLL;
using HR_Management.DTO;

namespace HR_Management.GUI.UserControls
{
    [System.ComponentModel.DesignerCategory("Code")]
    public class ucPhongBan : UserControl
    {
        private readonly PhongBanBLL _bllPhongBan = new PhongBanBLL();
        private readonly NhanVienBLL _bllNhanVien = new NhanVienBLL();

        private DataGridView dgvPhongBan = null!;
        private DataGridView dgvNhanVienThuocPB = null!;
        private SplitContainer split = null!;

        private TextBox txtMaPB = null!;
        private TextBox txtTenPB = null!;
        private TextBox txtTruongPhong = null!;
        private ComboBox cboTrangThai = null!;
        private TextBox txtMoTa = null!;
        private Label lblSoLuongNV = null!;

        private bool isAddingNew = false;

        public ucPhongBan()
        {
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = ThemeColor.BodyBg;

            // ===== TOP: Action buttons bar =====
            FlowLayoutPanel pnlActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(16, 12, 16, 12),
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.White
            };
            pnlActions.Paint += (s, e) =>
            {
                using (Pen p = new Pen(ThemeColor.CardBorder, 1))
                {
                    e.Graphics.DrawLine(p, 0, pnlActions.Height - 1, pnlActions.Width, pnlActions.Height - 1);
                }
            };

            Button btnThemMoi = UIHelper.CreateButton("+ Thêm Phòng Ban", ThemeColor.Primary, Color.White, 140, 38);
            Button btnSua = UIHelper.CreateButton("✏ Sửa", ThemeColor.Info, Color.White, 95, 38);
            Button btnLuu = UIHelper.CreateButton("💾 Lưu Thay Đổi", ThemeColor.Success, Color.White, 125, 38);
            Button btnXoa = UIHelper.CreateButton("🗑 Xóa", ThemeColor.Danger, Color.White, 85, 38);
            Button btnLamMoi = UIHelper.CreateButton("↻ Làm mới", Color.FromArgb(100, 116, 139), Color.White, 95, 38);

            btnThemMoi.Click += BtnThemMoi_Click;
            btnSua.Click += BtnSua_Click;
            btnLuu.Click += BtnLuu_Click;
            btnXoa.Click += BtnXoa_Click;
            btnLamMoi.Click += (s, e) => LoadData();

            pnlActions.Controls.AddRange(new Control[] { btnThemMoi, btnSua, btnLuu, btnXoa, btnLamMoi });

            // ===== MAIN: SplitContainer (Nằm ngang: Danh sách phòng ban ở trên, Thông tin chi tiết ở dưới) =====
            split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterWidth = 12,
                BackColor = ThemeColor.BodyBg,
                Panel1MinSize = 50,
                Panel2MinSize = 50
            };
            split.Panel1.Padding = new Padding(16, 16, 16, 8);
            split.Panel2.Padding = new Padding(16, 8, 16, 16);

            this.Load += (s, e) => AdjustSplitter();
            this.SizeChanged += (s, e) => AdjustSplitter();

            // --- TRÊN: Danh sách phòng ban ---
            Panel pnlTop = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(16)
            };
            pnlTop.Paint += (s, e) =>
            {
                using (Pen p = new Pen(ThemeColor.CardBorder, 1))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, pnlTop.Width - 1, pnlTop.Height - 1);
                }
            };

            dgvPhongBan = new DataGridView { Dock = DockStyle.Fill };
            UIHelper.StyleDataGridView(dgvPhongBan);
            dgvPhongBan.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            dgvPhongBan.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Mã PB", DataPropertyName = "MaPhongBan", FillWeight = 15 });
            dgvPhongBan.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tên Phòng Ban", DataPropertyName = "TenPhongBan", FillWeight = 30 });
            dgvPhongBan.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Trưởng Phòng", DataPropertyName = "TruongPhong", FillWeight = 25 });
            dgvPhongBan.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Số NV", DataPropertyName = "SoLuongNhanVien", FillWeight = 12 });
            dgvPhongBan.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Trạng Thái", DataPropertyName = "TrangThai", FillWeight = 18 });

            dgvPhongBan.SelectionChanged += DgvPhongBan_SelectionChanged;
            dgvPhongBan.DoubleClick += (s, e) => BtnSua_Click(s, e);

            pnlTop.Controls.Add(dgvPhongBan);
            split.Panel1.Controls.Add(pnlTop);

            // --- DƯỚI: Thông tin phòng ban & Danh sách nhân sự trực thuộc ---
            Panel pnlBottom = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(16, 16, 16, 16)
            };
            pnlBottom.Paint += (s, e) =>
            {
                using (Pen p = new Pen(ThemeColor.CardBorder, 1))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, pnlBottom.Width - 1, pnlBottom.Height - 1);
                }
            };

            TableLayoutPanel tlpBottomLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(0)
            };
            tlpBottomLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54F)); // Form thông tin
            tlpBottomLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46F)); // Grid nhân viên
            tlpBottomLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // Cột trái: Form thông tin phòng ban
            Panel pnlFormWrapper = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 0, 18, 0)
            };

            Label lblFormTitle = new Label
            {
                Text = "THÔNG TIN PHÒNG BAN",
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                ForeColor = ThemeColor.TextPrimary,
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(0, 0, 0, 12)
            };

            TableLayoutPanel pnlForm = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 3,
                Padding = new Padding(0, 4, 0, 0)
            };
            pnlForm.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pnlForm.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            pnlForm.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pnlForm.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            pnlForm.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            pnlForm.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            pnlForm.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            Label l1 = new Label { Text = "Mã PB:", AutoSize = true, Anchor = AnchorStyles.Left, Font = ThemeColor.BodyFont };
            txtMaPB = new TextBox { Dock = DockStyle.Fill, Font = ThemeColor.BodyFont };
            pnlForm.Controls.Add(l1, 0, 0);
            pnlForm.Controls.Add(txtMaPB, 1, 0);

            Label l5 = new Label { Text = "Trạng thái:", AutoSize = true, Anchor = AnchorStyles.Left, Font = ThemeColor.BodyFont };
            cboTrangThai = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Font = ThemeColor.BodyFont };
            cboTrangThai.Items.AddRange(new object[] { "Hoạt động", "Tạm ngừng" });
            cboTrangThai.SelectedIndex = 0;
            pnlForm.Controls.Add(l5, 2, 0);
            pnlForm.Controls.Add(cboTrangThai, 3, 0);

            Label l2 = new Label { Text = "Tên PB:", AutoSize = true, Anchor = AnchorStyles.Left, Font = ThemeColor.BodyFont };
            txtTenPB = new TextBox { Dock = DockStyle.Fill, Font = ThemeColor.BodyFont };
            pnlForm.Controls.Add(l2, 0, 1);
            pnlForm.Controls.Add(txtTenPB, 1, 1);

            Label l4 = new Label { Text = "Số lượng NV:", AutoSize = true, Anchor = AnchorStyles.Left, Font = ThemeColor.BodyFont };
            lblSoLuongNV = new Label { Text = "0 nhân viên", AutoSize = true, Anchor = AnchorStyles.Left, Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = ThemeColor.Primary };
            pnlForm.Controls.Add(l4, 2, 1);
            pnlForm.Controls.Add(lblSoLuongNV, 3, 1);

            Label l3 = new Label { Text = "Trưởng PB:", AutoSize = true, Anchor = AnchorStyles.Left | AnchorStyles.Top, Font = ThemeColor.BodyFont, Padding = new Padding(0, 4, 0, 0) };
            txtTruongPhong = new TextBox { Dock = DockStyle.Top, Font = ThemeColor.BodyFont };
            pnlForm.Controls.Add(l3, 0, 2);
            pnlForm.Controls.Add(txtTruongPhong, 1, 2);

            Label l6 = new Label { Text = "Mô tả:", AutoSize = true, Anchor = AnchorStyles.Left | AnchorStyles.Top, Font = ThemeColor.BodyFont, Padding = new Padding(0, 4, 0, 0) };
            txtMoTa = new TextBox { Dock = DockStyle.Fill, Multiline = true, Font = ThemeColor.BodyFont, ScrollBars = ScrollBars.Vertical };
            pnlForm.Controls.Add(l6, 2, 2);
            pnlForm.Controls.Add(txtMoTa, 3, 2);

            foreach (Control c in pnlForm.Controls)
            {
                c.Margin = new Padding(3, 3, 6, 3);
            }

            pnlFormWrapper.Controls.Add(pnlForm);
            pnlFormWrapper.Controls.Add(lblFormTitle);

            // Cột phải: Danh sách nhân viên trực thuộc
            Panel pnlGridWrapper = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(18, 0, 0, 0)
            };
            pnlGridWrapper.Paint += (s, e) =>
            {
                using (Pen pen = new Pen(ThemeColor.CardBorder, 1))
                {
                    e.Graphics.DrawLine(pen, 0, 4, 0, pnlGridWrapper.Height - 4);
                }
            };

            Label lblSubTitle = new Label
            {
                Text = "NHÂN SỰ TRỰC THUỘC PHÒNG",
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                ForeColor = ThemeColor.TextPrimary,
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(0, 0, 0, 12)
            };

            dgvNhanVienThuocPB = new DataGridView { Dock = DockStyle.Fill };
            UIHelper.StyleDataGridView(dgvNhanVienThuocPB);
            dgvNhanVienThuocPB.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvNhanVienThuocPB.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Mã NV", DataPropertyName = "ID_NV", FillWeight = 20 });
            dgvNhanVienThuocPB.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Họ và Tên", DataPropertyName = "TenNV", FillWeight = 48 });
            dgvNhanVienThuocPB.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Chức Vụ", DataPropertyName = "ChucVu", FillWeight = 32 });

            pnlGridWrapper.Controls.Add(dgvNhanVienThuocPB);
            pnlGridWrapper.Controls.Add(lblSubTitle);

            tlpBottomLayout.Controls.Add(pnlFormWrapper, 0, 0);
            tlpBottomLayout.Controls.Add(pnlGridWrapper, 1, 0);

            pnlBottom.Controls.Add(tlpBottomLayout);
            split.Panel2.Controls.Add(pnlBottom);

            this.Controls.Add(split);
            this.Controls.Add(pnlActions);
        }

        public void LoadData()
        {
            try
            {
                var list = _bllPhongBan.GetAll();
                dgvPhongBan.DataSource = null;
                dgvPhongBan.DataSource = list;
                if (list.Count > 0) DisplayDepartment(list[0]); else ResetInput();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải danh sách phòng ban: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DgvPhongBan_SelectionChanged(object? sender, EventArgs e)
        {
            if (dgvPhongBan.SelectedRows.Count > 0)
            {
                var selected = dgvPhongBan.SelectedRows[0].DataBoundItem as PhongBanDTO;
                if (selected != null) DisplayDepartment(selected);
            }
        }

        private void DisplayDepartment(PhongBanDTO pb)
        {
            isAddingNew = false;
            txtMaPB.Text = pb.MaPhongBan; txtMaPB.ReadOnly = true;
            txtTenPB.Text = pb.TenPhongBan;
            txtTruongPhong.Text = pb.TruongPhong ?? "";
            lblSoLuongNV.Text = $"{pb.SoLuongNhanVien} nhân viên";
            cboTrangThai.SelectedItem = pb.TrangThai;
            txtMoTa.Text = pb.MoTa ?? "";
            try { dgvNhanVienThuocPB.DataSource = _bllNhanVien.Search("", pb.MaPhongBan, "ALL"); } catch { }
        }

        private void BtnThemMoi_Click(object? sender, EventArgs e)
        {
            isAddingNew = true; ResetInput(); txtMaPB.ReadOnly = false; txtMaPB.Focus();
        }

        private void BtnSua_Click(object? sender, EventArgs e)
        {
            if (dgvPhongBan.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn một phòng ban từ danh sách để sửa thông tin!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selected = dgvPhongBan.SelectedRows[0].DataBoundItem as PhongBanDTO;
            if (selected != null)
            {
                DisplayDepartment(selected);
                isAddingNew = false;
                txtMaPB.ReadOnly = true;
                txtTenPB.Focus();
                txtTenPB.SelectAll();
            }
        }

        private void ResetInput()
        {
            txtMaPB.Text = "PB" + DateTime.Now.ToString("MMddHHmm");
            txtTenPB.Text = ""; txtTruongPhong.Text = ""; lblSoLuongNV.Text = "0";
            cboTrangThai.SelectedIndex = 0; txtMoTa.Text = "";
            dgvNhanVienThuocPB.DataSource = null;
        }

        private void BtnLuu_Click(object? sender, EventArgs e)
        {
            string maPb = txtMaPB.Text.Trim();
            string tenPb = txtTenPB.Text.Trim();

            if (string.IsNullOrWhiteSpace(maPb))
            {
                txtMaPB.Focus();
                MessageBox.Show("Mã phòng ban không được để trống!", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(tenPb))
            {
                txtTenPB.Focus();
                MessageBox.Show("Tên phòng ban không được để trống!", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var pb = new PhongBanDTO
            {
                MaPhongBan = maPb,
                TenPhongBan = tenPb,
                TruongPhong = txtTruongPhong.Text.Trim(),
                TrangThai = cboTrangThai.SelectedItem?.ToString() ?? "Hoạt động",
                MoTa = txtMoTa.Text.Trim()
            };
            if (_bllPhongBan.Save(pb, isAddingNew, out string error))
            {
                MessageBox.Show(isAddingNew ? "Thêm phòng ban thành công!" : "Cập nhật thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadData();
            }
            else
            {
                MessageBox.Show(error, "Lỗi kiểm tra dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnXoa_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtMaPB.Text)) return;
            if (MessageBox.Show($"Xóa phòng ban '{txtTenPB.Text}'?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                if (_bllPhongBan.Delete(txtMaPB.Text.Trim(), out string error))
                { MessageBox.Show("Xóa thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information); LoadData(); }
                else MessageBox.Show(error, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void AdjustSplitter()
        {
            try
            {
                if (split != null && split.Height > 160)
                {
                    int targetDist = (int)(split.Height * 0.48);
                    int min = Math.Max(50, split.Panel1MinSize);
                    int max = split.Height - Math.Max(50, split.Panel2MinSize);
                    if (targetDist >= min && targetDist <= max)
                    {
                        split.SplitterDistance = targetDist;
                    }
                }
            }
            catch
            {
                // Tránh lỗi SplitterDistance của WinForms khi form chưa kịp render
            }
        }
    }
}
