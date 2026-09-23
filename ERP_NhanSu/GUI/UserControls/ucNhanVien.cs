using System;
using System.Drawing;
using System.Windows.Forms;
using HR_Management.BLL;
using HR_Management.DTO;
using System.Collections.Generic;
using HR_Management.GUI.Forms;

namespace HR_Management.GUI.UserControls
{
    [System.ComponentModel.DesignerCategory("Code")]
    public class ucNhanVien : UserControl
    {
        private readonly NhanVienBLL _bll = new NhanVienBLL();
        private readonly PhongBanBLL _bllPB = new PhongBanBLL();

        private DataGridView dgv = null!;
        private TextBox txtSearch = null!;
        private ComboBox cboFilterPB = null!;
        private ComboBox cboFilterTrangThai = null!;

        public ucNhanVien()
        {
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = ThemeColor.BodyBg;

            // ===== TOP: Filter + Action bar =====
            FlowLayoutPanel pnlFilterBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.White,
                Padding = new Padding(16, 12, 16, 12),
                WrapContents = true
            };
            pnlFilterBar.Paint += (s, e) =>
            {
                using (Pen p = new Pen(ThemeColor.CardBorder, 1))
                {
                    e.Graphics.DrawLine(p, 0, pnlFilterBar.Height - 1, pnlFilterBar.Width, pnlFilterBar.Height - 1);
                }
            };

            txtSearch = new TextBox
            {
                Width = 260,
                Height = 30,
                Font = ThemeColor.BodyFont,
                Margin = new Padding(0, 6, 12, 6)
            };
            txtSearch.SetPlaceholder("🔍 Tìm kiếm theo tên hoặc mã NV...");
            txtSearch.TextChanged += (s, e) => ApplyFilter();

            cboFilterPB = new ComboBox
            {
                Width = 190,
                Height = 30,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = ThemeColor.BodyFont,
                Margin = new Padding(0, 6, 12, 6)
            };
            cboFilterPB.Items.Insert(0, "-- Tất cả phòng ban --");
            cboFilterPB.SelectedIndex = 0;
            cboFilterPB.SelectedIndexChanged += (s, e) => ApplyFilter();

            cboFilterTrangThai = new ComboBox
            {
                Width = 170,
                Height = 30,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = ThemeColor.BodyFont,
                Margin = new Padding(0, 6, 16, 6)
            };
            cboFilterTrangThai.Items.AddRange(new object[] { "-- Tất cả trạng thái --", "Đang làm việc", "Đã nghỉ việc" });
            cboFilterTrangThai.SelectedIndex = 0;
            cboFilterTrangThai.SelectedIndexChanged += (s, e) => ApplyFilter();

            // Action buttons
            Button btnThem = UIHelper.CreateButton("+ Thêm Nhân Viên", ThemeColor.Primary, Color.White, 150, 38);
            btnThem.Margin = new Padding(0, 0, 10, 6);
            btnThem.Click += BtnThem_Click;

            Button btnSua = UIHelper.CreateButton("✏ Sửa / Chi Tiết", ThemeColor.Info, Color.White, 130, 38);
            btnSua.Margin = new Padding(0, 0, 10, 6);
            btnSua.Click += BtnSua_Click;

            Button btnXoa = UIHelper.CreateButton("🗑 Xóa", ThemeColor.Danger, Color.White, 90, 38);
            btnXoa.Margin = new Padding(0, 0, 10, 6);
            btnXoa.Click += BtnXoa_Click;

            Button btnLamMoi = UIHelper.CreateButton("↻ Làm mới", Color.FromArgb(100, 116, 139), Color.White, 100, 38);
            btnLamMoi.Margin = new Padding(0, 0, 0, 6);
            btnLamMoi.Click += (s, e) => { txtSearch.Text = ""; cboFilterPB.SelectedIndex = 0; cboFilterTrangThai.SelectedIndex = 0; LoadData(); };

            pnlFilterBar.Controls.AddRange(new Control[] { txtSearch, cboFilterPB, cboFilterTrangThai, btnThem, btnSua, btnXoa, btnLamMoi });

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

            dgv = new DataGridView { Dock = DockStyle.Fill };
            UIHelper.StyleDataGridView(dgv);
            dgv.AutoGenerateColumns = false;

            // --- Sizing fixes -------------------------------------------------
            // The old code used AutoSizeColumnsMode.Fill with small MinimumWidth
            // values. When there are 10+ columns, Fill squeezes every column down
            // to its minimum, and the header text ends up wrapping onto two ugly
            // lines. Switching to fixed, generous widths (with horizontal
            // scrolling when the window is narrower than the total) keeps every
            // column readable regardless of window size.
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            dgv.ScrollBars = ScrollBars.Both;
            dgv.RowTemplate.Height = 40;
            dgv.ColumnHeadersHeight = 42;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgv.RowHeadersWidth = 28; // slimmer selector strip instead of the wide default
            dgv.RowHeadersVisible = true;

            dgv.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font(ThemeColor.BodyFont.FontFamily, 9.75f, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgv.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 8, 0);

            dgv.DefaultCellStyle.Padding = new Padding(6, 2, 6, 2);
            dgv.DefaultCellStyle.Font = ThemeColor.BodyFont;
            dgv.DefaultCellStyle.WrapMode = DataGridViewTriState.False;

            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Mã NV",
                DataPropertyName = "ID_NV",
                Width = 80,
                SortMode = DataGridViewColumnSortMode.Automatic
            });
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Họ và Tên",
                DataPropertyName = "TenNV",
                Width = 190
            });
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Chức Vụ",
                DataPropertyName = "ChucVu",
                Width = 150
            });
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Phòng Ban",
                DataPropertyName = "PhongBan",
                Width = 160
            });
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Giới Tính",
                DataPropertyName = "GioiTinh",
                Width = 95,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Ngày Sinh",
                DataPropertyName = "NgaySinh",
                Width = 115,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Format = "dd/MM/yyyy",
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                }
            });
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "SĐT",
                DataPropertyName = "SoDienThoai",
                Width = 125
            });
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Email",
                DataPropertyName = "Email",
                Width = 200
            });
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Địa Chỉ",
                DataPropertyName = "DiaChi",
                Width = 200
            });
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Lương Cơ Bản",
                DataPropertyName = "LuongCoBan",
                Width = 145,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Format = "#,##0 đ",
                    Alignment = DataGridViewContentAlignment.MiddleRight
                }
            });
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Trạng Thái",
                DataPropertyName = "TrangThai",
                Width = 130,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            // If the card is wider than the sum of the fixed column widths,
            // stretch the last column to soak up the extra space instead of
            // leaving a dead grey gap on the right.
            dgv.Columns[dgv.Columns.Count - 1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            dgv.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) BtnSua_Click(null, EventArgs.Empty); };

            pnlGridCard.Controls.Add(dgv);
            pnlGrid.Controls.Add(pnlGridCard);

            this.Controls.Add(pnlGrid);
            this.Controls.Add(pnlFilterBar);
        }

        public void LoadData()
        {
            try
            {
                var list = _bll.GetAll();
                dgv.DataSource = null;
                dgv.DataSource = list;
                LoadPhongBanFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải danh sách nhân viên: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadPhongBanFilter()
        {
            try
            {
                cboFilterPB.Items.Clear();
                cboFilterPB.Items.Add("-- Tất cả PB --");
                foreach (var pb in _bllPB.GetAll())
                    cboFilterPB.Items.Add(pb.MaPhongBan);
                cboFilterPB.SelectedIndex = 0;
            }
            catch { }
        }

        private void ApplyFilter()
        {
            string kw = txtSearch.Text.Trim();
            string pb = (cboFilterPB.SelectedIndex > 0) ? cboFilterPB.SelectedItem?.ToString() ?? "" : "";
            string tt = (cboFilterTrangThai.SelectedIndex > 0) ? cboFilterTrangThai.SelectedItem?.ToString() ?? "" : "";

            try
            {
                var list = _bll.Search(kw, pb, string.IsNullOrEmpty(tt) ? "ALL" : tt);
                dgv.DataSource = null;
                dgv.DataSource = list;
            }
            catch { }
        }

        private void BtnThem_Click(object? sender, EventArgs e)
        {
            using var frm = new frmNhanVienDetail(null);
            frm.ShowDialog();
            if (frm.IsSavedSuccessfully) LoadData();
        }

        private void BtnSua_Click(object? sender, EventArgs e)
        {
            if (dgv.SelectedRows.Count == 0) { MessageBox.Show("Vui lòng chọn nhân viên để sửa!"); return; }
            var nv = dgv.SelectedRows[0].DataBoundItem as NhanVienChiTietDTO;
            if (nv == null) return;
            using var frm = new frmNhanVienDetail(nv);
            frm.ShowDialog();
            if (frm.IsSavedSuccessfully) LoadData();
        }

        private void BtnXoa_Click(object? sender, EventArgs e)
        {
            if (dgv.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn nhân viên cần xóa!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var nv = dgv.SelectedRows[0].DataBoundItem as NhanVienChiTietDTO;
            if (nv == null) return;

            // 1. Kiểm tra xem nhân viên đã có dữ liệu giao dịch liên kết chưa (đơn hàng, hóa đơn, lương, phiếu thu/chi,...)
            if (_bll.HasRelatedTransactions(nv.ID_NV, out string details))
            {
                var confirmResult = MessageBox.Show(
                    $"Nhân viên '{nv.TenNV}' ({nv.ID_NV}) hiện đang có dữ liệu giao dịch trong hệ thống ({details}).\n\n" +
                    "Theo nguyên tắc quản lý ERP, không thể xóa vĩnh viễn hồ sơ để bảo toàn tính toàn vẹn của lịch sử bán hàng và tài chính.\n\n" +
                    "Bạn có muốn chuyển trạng thái nhân viên sang 'Đã nghỉ việc' và khóa tài khoản đăng nhập không?",
                    "Cảnh báo ràng buộc dữ liệu",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirmResult == DialogResult.Yes)
                {
                    if (_bll.Deactivate(nv.ID_NV, out string error))
                    {
                        MessageBox.Show($"Đã chuyển trạng thái nhân viên '{nv.TenNV}' sang 'Đã nghỉ việc' và khóa tài khoản thành công!",
                                        "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadData();
                    }
                    else
                    {
                        MessageBox.Show("Lỗi: " + error, "Lỗi cập nhật", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                return;
            }

            // 2. Với nhân viên chưa có giao dịch nào phát sinh: Cho phép xóa vĩnh viễn
            if (MessageBox.Show($"Bạn có chắc chắn muốn xóa vĩnh viễn nhân viên '{nv.TenNV}' ({nv.ID_NV}) khỏi hệ thống?\n(Dữ liệu sau khi xóa sẽ không thể phục hồi)",
                                "Xác nhận xóa vĩnh viễn", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                if (_bll.Delete(nv.ID_NV, out string error))
                {
                    MessageBox.Show("Xóa nhân viên thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadData();
                }
                else
                {
                    MessageBox.Show(error, "Lỗi xóa nhân viên", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }
    }
}