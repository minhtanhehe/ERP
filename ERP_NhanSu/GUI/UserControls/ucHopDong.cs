using System;
using System.Drawing;
using System.Windows.Forms;
using HR_Management.BLL;
using HR_Management.DTO;
using HR_Management.GUI.Forms;

namespace HR_Management.GUI.UserControls
{
    [System.ComponentModel.DesignerCategory("Code")]
    public class ucHopDong : UserControl
    {
        private readonly HopDongBLL _bllHopDong = new HopDongBLL();
        private DataGridView dgvHopDong = null!;
        private Label lblTotalHd = null!;
        private Label lblExpiringHd = null!;

        public ucHopDong()
        {
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = ThemeColor.BodyBg;

            // ===== TOP: KPI Chips + Action Buttons =====
            FlowLayoutPanel pnlTopBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.White,
                Padding = new Padding(16, 12, 16, 12),
                WrapContents = true
            };
            pnlTopBar.Paint += (s, e) =>
            {
                using (Pen p = new Pen(ThemeColor.CardBorder, 1))
                {
                    e.Graphics.DrawLine(p, 0, pnlTopBar.Height - 1, pnlTopBar.Width, pnlTopBar.Height - 1);
                }
            };

            lblTotalHd = new Label
            {
                Text = "📄 Tổng HĐ: 0",
                Font = ThemeColor.BodyFontBold,
                ForeColor = ThemeColor.PrimaryText,
                BackColor = ThemeColor.PrimaryLight,
                Padding = new Padding(12, 8, 12, 8),
                AutoSize = true,
                Margin = new Padding(0, 2, 14, 6)
            };

            lblExpiringHd = new Label
            {
                Text = "⚠️ Sắp hết hạn (30 ngày): 0",
                Font = ThemeColor.BodyFontBold,
                ForeColor = ThemeColor.WarningText,
                BackColor = ThemeColor.WarningLight,
                Padding = new Padding(12, 8, 12, 8),
                AutoSize = true,
                Margin = new Padding(0, 2, 20, 6)
            };

            Button btnThem = UIHelper.CreateButton("+ Ký Mới HĐ", ThemeColor.Primary, Color.White, 130, 38);
            btnThem.Margin = new Padding(0, 0, 10, 6);
            btnThem.Click += BtnThem_Click;

            Button btnSua = UIHelper.CreateButton("✏ Sửa Chi Tiết", ThemeColor.Info, Color.White, 120, 38);
            btnSua.Margin = new Padding(0, 0, 10, 6);
            btnSua.Click += BtnSua_Click;

            Button btnGiaHan = UIHelper.CreateButton("⏳ Gia Hạn", ThemeColor.Success, Color.White, 110, 38);
            btnGiaHan.Margin = new Padding(0, 0, 10, 6);
            btnGiaHan.Click += BtnSua_Click;

            Button btnXoa = UIHelper.CreateButton("🗑 Xóa", ThemeColor.Danger, Color.White, 85, 38);
            btnXoa.Margin = new Padding(0, 0, 10, 6);
            btnXoa.Click += BtnXoa_Click;

            Button btnLamMoi = UIHelper.CreateButton("↻ Làm mới", Color.FromArgb(100, 116, 139), Color.White, 95, 38);
            btnLamMoi.Margin = new Padding(0, 0, 0, 6);
            btnLamMoi.Click += (s, e) => LoadData();

            pnlTopBar.Controls.AddRange(new Control[] { lblTotalHd, lblExpiringHd, btnThem, btnSua, btnGiaHan, btnXoa, btnLamMoi });

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

            dgvHopDong = new DataGridView { Dock = DockStyle.Fill };
            UIHelper.StyleDataGridView(dgvHopDong);
            dgvHopDong.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            dgvHopDong.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Mã HĐ", DataPropertyName = "MaHopDong", FillWeight = 10 });
            dgvHopDong.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Mã NV", DataPropertyName = "ID_NV", FillWeight = 8 });
            dgvHopDong.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Họ và Tên", DataPropertyName = "TenNV", FillWeight = 16 });
            dgvHopDong.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Loại Hợp Đồng", DataPropertyName = "LoaiHopDong", FillWeight = 16 });
            dgvHopDong.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Ngày Ký", DataPropertyName = "NgayKy", FillWeight = 10, DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy" } });
            dgvHopDong.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Bắt Đầu", DataPropertyName = "NgayBatDau", FillWeight = 10, DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy" } });
            dgvHopDong.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Hết Hạn", DataPropertyName = "NgayKetThuc", FillWeight = 10, DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy" } });
            dgvHopDong.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Lương CB", DataPropertyName = "LuongCoBan", FillWeight = 10, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } });
            dgvHopDong.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Trạng Thái", DataPropertyName = "TrangThai", FillWeight = 10 });

            dgvHopDong.DoubleClick += (s, e) => BtnSua_Click(s, e);

            pnlGridCard.Controls.Add(dgvHopDong);
            pnlGrid.Controls.Add(pnlGridCard);

            this.Controls.Add(pnlGrid);
            this.Controls.Add(pnlTopBar);
        }

        public void LoadData()
        {
            try
            {
                var list = _bllHopDong.GetAll();
                dgvHopDong.DataSource = null;
                dgvHopDong.DataSource = list;

                var expiring = _bllHopDong.GetExpiringContracts(30);
                lblTotalHd.Text = $"📝 Tổng HĐ: {list.Count:N0}";
                lblExpiringHd.Text = $"⚠ Sắp hết hạn: {expiring.Count:N0}";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải danh sách hợp đồng: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnThem_Click(object? sender, EventArgs e)
        {
            using (var frm = new frmHopDongDetail(null))
            {
                frm.ShowDialog();
                if (frm.IsSavedSuccessfully) LoadData();
            }
        }

        private void BtnSua_Click(object? sender, EventArgs e)
        {
            if (dgvHopDong.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn hợp đồng cần sửa/gia hạn!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selected = dgvHopDong.SelectedRows[0].DataBoundItem as HopDongDTO;
            if (selected != null)
            {
                using (var frm = new frmHopDongDetail(selected))
                {
                    frm.ShowDialog();
                    if (frm.IsSavedSuccessfully) LoadData();
                }
            }
        }

        private void BtnXoa_Click(object? sender, EventArgs e)
        {
            if (dgvHopDong.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn hợp đồng cần xóa!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selected = dgvHopDong.SelectedRows[0].DataBoundItem as HopDongDTO;
            if (selected != null)
            {
                if (MessageBox.Show($"Bạn có chắc chắn muốn xóa hợp đồng [{selected.MaHopDong}] của nhân viên {selected.TenNV}?", "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    if (_bllHopDong.Delete(selected.MaHopDong, out string error))
                    {
                        MessageBox.Show("Xóa hợp đồng thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadData();
                    }
                    else
                    {
                        MessageBox.Show(error, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}
