using System;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Npgsql;

namespace ERP_BanHang
{
    public partial class QlyDonHang : Form
    {
        public static string CurrentEmployeeId { get; set; } = "";
        public static string CurrentEmployeeName { get; set; } = "";
        public static string CurrentRole { get; set; } = "";

        private string connectionString = ConfigurationManager.ConnectionStrings["ERP_Connection"]?.ConnectionString
            ?? ConfigurationManager.ConnectionStrings["ERP_BanHang"]?.ConnectionString;

        private DataTable dtDonHang;

        private Timer longPressTimer;
        private int selectedRowIndexForDelete = -1;
        private const int LONG_PRESS_DURATION = 1000;

        // Biến lưu trạng thái tab hiện tại: false = Chưa thanh toán (mặc định), true = Đã thanh toán
        private bool isDaThanhToan = false;

        public QlyDonHang()
        {
            InitializeComponent();
            KhoiTaoLongPressTimer();
        }

        private void KhoiTaoLongPressTimer()
        {
            longPressTimer = new Timer();
            longPressTimer.Interval = LONG_PRESS_DURATION;
            longPressTimer.Tick += LongPressTimer_Tick;
        }

        private void QlyDonHang_Load(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Maximized;

            KhoiTaoCotBang();
            LoadDataDonHang();

            // Đăng ký sự kiện chuột cho bảng để xử lý nhấn giữ xóa dòng
            BangDonHang.MouseDown += BangDonHang_MouseDown;
            BangDonHang.MouseUp += BangDonHang_MouseUp;
        }

        // ==========================================
        // 1. KHỞI TẠO BẢNG (ĐÃ BỎ CỘT TRẠNG THÁI ĐƠN)
        // ==========================================
        private void KhoiTaoCotBang()
        {
            BangDonHang.Columns.Clear();
            BangDonHang.AutoGenerateColumns = false;

            // 1. Mã đơn hàng
            DataGridViewTextBoxColumn colMaDH = new DataGridViewTextBoxColumn();
            colMaDH.Name = "colMaDonHang";
            colMaDH.HeaderText = "MÃ ĐƠN HÀNG";
            colMaDH.DataPropertyName = "ID_DH";
            colMaDH.FillWeight = 12;
            colMaDH.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            colMaDH.DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            colMaDH.DefaultCellStyle.ForeColor = Color.FromArgb(13, 110, 253);
            BangDonHang.Columns.Add(colMaDH);

            // 2. Tên khách hàng
            DataGridViewTextBoxColumn colKH = new DataGridViewTextBoxColumn();
            colKH.Name = "colTenKhachHang";
            colKH.HeaderText = "TÊN KHÁCH HÀNG";
            colKH.DataPropertyName = "TenDoanhNghiep";
            colKH.FillWeight = 22;
            BangDonHang.Columns.Add(colKH);

            // 3. Nhân viên tạo
            DataGridViewTextBoxColumn colNV = new DataGridViewTextBoxColumn();
            colNV.Name = "colTenNhanVien";
            colNV.HeaderText = "NHÂN VIÊN TẠO";
            colNV.DataPropertyName = "TenNV";
            colNV.FillWeight = 16;
            BangDonHang.Columns.Add(colNV);

            // 4. Tổng số lượng
            DataGridViewTextBoxColumn colSL = new DataGridViewTextBoxColumn();
            colSL.Name = "colTongSoLuong";
            colSL.HeaderText = "TỔNG SỐ LƯỢNG";
            colSL.DataPropertyName = "TongSoLuong";
            colSL.FillWeight = 10;
            colSL.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            BangDonHang.Columns.Add(colSL);

            // 5. Tổng tiền
            DataGridViewTextBoxColumn colTT = new DataGridViewTextBoxColumn();
            colTT.Name = "colTongTien";
            colTT.HeaderText = "TỔNG TIỀN (VNĐ)";
            colTT.DataPropertyName = "TongTienCalc";
            colTT.FillWeight = 14;
            colTT.DefaultCellStyle.Format = "N0";
            colTT.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            colTT.DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            BangDonHang.Columns.Add(colTT);

            // 6. Ngày tạo
            DataGridViewTextBoxColumn colNgay = new DataGridViewTextBoxColumn();
            colNgay.Name = "colNgayTao";
            colNgay.HeaderText = "NGÀY TẠO";
            colNgay.DataPropertyName = "NgayTao";
            colNgay.FillWeight = 13;
            colNgay.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            BangDonHang.Columns.Add(colNgay);

            // 7. Trạng thái thanh toán
            DataGridViewTextBoxColumn colTTTT = new DataGridViewTextBoxColumn();
            colTTTT.Name = "colTrangThaiThanhToan";
            colTTTT.HeaderText = "THANH TOÁN";
            colTTTT.DataPropertyName = "TrangThaiTT";
            colTTTT.FillWeight = 13;
            colTTTT.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            BangDonHang.Columns.Add(colTTTT);

            // 8. Nút Xuất Hóa Đơn
            DataGridViewButtonColumn colXuatHD = new DataGridViewButtonColumn();
            colXuatHD.Name = "colXuatHoaDon";
            colXuatHD.HeaderText = "THAO TÁC";
            colXuatHD.Text = "Xuất hóa đơn";
            colXuatHD.UseColumnTextForButtonValue = true;
            colXuatHD.FlatStyle = FlatStyle.Flat;
            colXuatHD.FillWeight = 11;
            BangDonHang.Columns.Add(colXuatHD);

            BangDonHang.AllowUserToResizeColumns = true;
            BangDonHang.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        // ==========================================
        // 2. TẢI DỮ LIỆU TỪ CSDL NEON/POSTGRESQL
        // ==========================================
        private void LoadDataDonHang()
        {
            string query = @"
                SELECT 
                    DH.ID_DH,
                    KH.TenDoanhNghiep,
                    NV.TenNV,
                    COALESCE(SUM(CTDH.SoLuong), 0) AS TongSoLuong,
                    COALESCE(SUM(CTDH.ThanhTien), 0) AS TongTienCalc,
                    DH.NgayTao,
                    COALESCE(HD.TrangThai, N'Chưa thanh toán') AS TrangThaiTT
                FROM DonHang DH
                INNER JOIN KhachHang KH ON DH.ID_KH = KH.ID_KH
                INNER JOIN NhanVien NV ON DH.ID_NV = NV.ID_NV
                LEFT JOIN ChiTietDonHang CTDH ON DH.ID_DH = CTDH.ID_DH
                LEFT JOIN HoaDon HD ON DH.ID_DH = HD.ID_DH
                GROUP BY DH.ID_DH, KH.TenDoanhNghiep, NV.TenNV, DH.NgayTao, HD.TrangThai
                ORDER BY DH.NgayTao DESC";

            using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    NpgsqlDataAdapter da = new NpgsqlDataAdapter(query, conn);
                    dtDonHang = new DataTable();
                    da.Fill(dtDonHang);

                    LocDuLieu();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi kết nối CSDL PostgreSQL: " + ex.Message, "Lỗi PostgreSQL", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // ==========================================
        // 3. XỬ LÝ CHUYỂN TAB CHƯA / ĐÃ THANH TOÁN
        // ==========================================
        private void btnTabChuaThanhToan_Click(object sender, EventArgs e)
        {
            isDaThanhToan = false;

            // Đổi màu sắc giao diện active tab
            btnTabChuaThanhToan.BackColor = Color.FromArgb(13, 110, 253);
            btnTabChuaThanhToan.ForeColor = Color.White;

            btnTabDaThanhToan.BackColor = Color.LightGray;
            btnTabDaThanhToan.ForeColor = Color.Black;

            LocDuLieu();
        }

        private void btnTabDaThanhToan_Click(object sender, EventArgs e)
        {
            isDaThanhToan = true;

            // Đổi màu sắc giao diện active tab
            btnTabDaThanhToan.BackColor = Color.FromArgb(13, 110, 253);
            btnTabDaThanhToan.ForeColor = Color.White;

            btnTabChuaThanhToan.BackColor = Color.LightGray;
            btnTabChuaThanhToan.ForeColor = Color.Black;

            LocDuLieu();
        }

        // ==========================================
        // 4. LỌC DỮ LIỆU THEO TÌM KIẾM VÀ TAB
        // ==========================================
        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            LocDuLieu();
        }

        private void LocDuLieu()
        {
            if (dtDonHang == null) return;

            string keyword = txtSearch.Text.Trim().Replace("'", "''");

            DataView dv = dtDonHang.DefaultView;
            string filter = "1=1";

            // Lọc theo từ khóa tìm kiếm
            if (!string.IsNullOrEmpty(keyword))
            {
                filter += $" AND (ID_DH LIKE '%{keyword}%' OR TenDoanhNghiep LIKE '%{keyword}%' OR TenNV LIKE '%{keyword}%')";
            }

            // Lọc theo trạng thái tab
            if (isDaThanhToan)
            {
                filter += " AND TrangThaiTT = 'Đã thanh toán'";
            }
            else
            {
                filter += " AND (TrangThaiTT IS NULL OR TrangThaiTT <> 'Đã thanh toán')";
            }

            dv.RowFilter = filter;
            BangDonHang.DataSource = dv;
        }

        // ==========================================
        // 5. XỬ LÝ NHẤN GIỮ ĐỂ XÓA ĐƠN HÀNG
        // ==========================================
        private void BangDonHang_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                DataGridView.HitTestInfo hit = BangDonHang.HitTest(e.X, e.Y);

                if (hit.RowIndex >= 0 && BangDonHang.Columns[hit.ColumnIndex].Name != "colXuatHoaDon")
                {
                    selectedRowIndexForDelete = hit.RowIndex;
                    longPressTimer.Start();
                }
            }
        }

        private void BangDonHang_MouseUp(object sender, MouseEventArgs e)
        {
            longPressTimer.Stop();
        }

        private void LongPressTimer_Tick(object sender, EventArgs e)
        {
            longPressTimer.Stop();

            if (selectedRowIndexForDelete >= 0 && selectedRowIndexForDelete < BangDonHang.Rows.Count)
            {
                string maDonHang = BangDonHang.Rows[selectedRowIndexForDelete].Cells["colMaDonHang"].Value?.ToString();

                if (string.IsNullOrEmpty(maDonHang)) return;

                DialogResult result = MessageBox.Show(
                    $"Bạn có chắc chắn muốn XÓA đơn hàng [{maDonHang}] khỏi hệ thống?\n\nLưu ý: Dữ liệu giao hàng, chi tiết đơn hàng và hóa đơn liên quan cũng sẽ bị xóa vĩnh viễn!",
                    "Xác nhận xóa đơn hàng",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2);

                if (result == DialogResult.Yes)
                {
                    XoaDonHangCSDL(maDonHang);
                }
            }
        }

        private void XoaDonHangCSDL(string maDonHang)
        {
            using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    NpgsqlTransaction transaction = conn.BeginTransaction();

                    try
                    {
                        // 1. Xóa trong GiaoHang
                        using (NpgsqlCommand cmd1 = new NpgsqlCommand("DELETE FROM GiaoHang WHERE ID_DH = @ID_DH", conn, transaction))
                        {
                            cmd1.Parameters.AddWithValue("@ID_DH", maDonHang);
                            cmd1.ExecuteNonQuery();
                        }

                        // 2. Xóa trong ChiTietHoaDon & HoaDon
                        using (NpgsqlCommand cmd2 = new NpgsqlCommand("DELETE FROM ChiTietHoaDon WHERE ID_HD IN (SELECT ID_HD FROM HoaDon WHERE ID_DH = @ID_DH)", conn, transaction))
                        {
                            cmd2.Parameters.AddWithValue("@ID_DH", maDonHang);
                            cmd2.ExecuteNonQuery();
                        }

                        using (NpgsqlCommand cmd3 = new NpgsqlCommand("DELETE FROM HoaDon WHERE ID_DH = @ID_DH", conn, transaction))
                        {
                            cmd3.Parameters.AddWithValue("@ID_DH", maDonHang);
                            cmd3.ExecuteNonQuery();
                        }

                        // 3. Xóa trong ChiTietYeuCau & YeuCauSauBanHang
                        using (NpgsqlCommand cmd4 = new NpgsqlCommand("DELETE FROM ChiTietYeuCau WHERE ID_YC IN (SELECT ID_YC FROM YeuCauSauBanHang WHERE ID_DH = @ID_DH)", conn, transaction))
                        {
                            cmd4.Parameters.AddWithValue("@ID_DH", maDonHang);
                            cmd4.ExecuteNonQuery();
                        }

                        using (NpgsqlCommand cmd5 = new NpgsqlCommand("DELETE FROM YeuCauSauBanHang WHERE ID_DH = @ID_DH", conn, transaction))
                        {
                            cmd5.Parameters.AddWithValue("@ID_DH", maDonHang);
                            cmd5.ExecuteNonQuery();
                        }

                        // 4. Xóa ChiTietDonHang & DonHang
                        using (NpgsqlCommand cmd6 = new NpgsqlCommand("DELETE FROM ChiTietDonHang WHERE ID_DH = @ID_DH", conn, transaction))
                        {
                            cmd6.Parameters.AddWithValue("@ID_DH", maDonHang);
                            cmd6.ExecuteNonQuery();
                        }

                        using (NpgsqlCommand cmd7 = new NpgsqlCommand("DELETE FROM DonHang WHERE ID_DH = @ID_DH", conn, transaction))
                        {
                            cmd7.Parameters.AddWithValue("@ID_DH", maDonHang);
                            cmd7.ExecuteNonQuery();
                        }

                        transaction.Commit();

                        MessageBox.Show($"Đã xóa thành công đơn hàng [{maDonHang}]!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadDataDonHang();
                    }
                    catch (Exception exInner)
                    {
                        transaction.Rollback();
                        MessageBox.Show("Lỗi khi xóa dữ liệu liên quan: " + exInner.Message, "Lỗi PostgreSQL", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi kết nối CSDL: " + ex.Message, "Lỗi PostgreSQL", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // ==========================================
        // 6. ĐỊNH DẠNG MÀU SẮC BẢNG & SỰ KIỆN CLICK
        // ==========================================
        private void BangDonHang_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.Value is DateTime)
            {
                e.Value = ((DateTime)e.Value).ToString("dd/MM/yyyy HH:mm");
                e.FormattingApplied = true;
            }

            if (BangDonHang.Columns[e.ColumnIndex].Name == "colTrangThaiThanhToan" && e.Value != null)
            {
                string status = e.Value.ToString();
                if (status == "Đã thanh toán")
                {
                    e.CellStyle.BackColor = Color.FromArgb(212, 237, 218);
                    e.CellStyle.ForeColor = Color.FromArgb(21, 87, 36);
                    e.CellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                }
                else
                {
                    e.CellStyle.BackColor = Color.FromArgb(255, 243, 205);
                    e.CellStyle.ForeColor = Color.FromArgb(133, 100, 4);
                    e.CellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                }
            }
        }

        private void BangDonHang_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && BangDonHang.Columns[e.ColumnIndex].Name == "colXuatHoaDon")
            {
                string maDonHang = BangDonHang.Rows[e.RowIndex].Cells["colMaDonHang"].Value?.ToString();
                string trangThaiTT = BangDonHang.Rows[e.RowIndex].Cells["colTrangThaiThanhToan"].Value?.ToString();

                if (trangThaiTT != "Đã thanh toán")
                {
                    MessageBox.Show($"Đơn hàng [{maDonHang}] chưa hoàn tất thanh toán (Trạng thái: {trangThaiTT})!\nKhông thể xuất hóa đơn cho đơn hàng này.",
                                    "Cảnh báo",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning);
                    return;
                }

                ChiTietHoaDon chiTietHoaDonForm = new ChiTietHoaDon(maDonHang);
                chiTietHoaDonForm.ShowDialog();
            }
        }

        // ==========================================
        // 7. ĐIỀU HƯỚNG MENU SIDEBAR
        // ==========================================
        private void btnSanPham_Click(object sender, EventArgs e)
        {
            this.Hide();
            QlySanPham qlySanPhamForm = new QlySanPham();
            qlySanPhamForm.ShowDialog();
            this.Close();
        }

        private void btnNhaPhanPhoi_Click(object sender, EventArgs e)
        {
            this.Hide();
            QlyKhachHang qlyKhachHangForm = new QlyKhachHang();
            qlyKhachHangForm.ShowDialog();
            this.Close();
        }

        private void btnGiaoHang_Click(object sender, EventArgs e)
        {
            this.Hide();
            QlyGiaoHang qlyGiaoHangForm = new QlyGiaoHang();
            qlyGiaoHangForm.ShowDialog();
            this.Close();
        }

        private void btnHangTraLoi_Click(object sender, EventArgs e)
        {
            this.Hide();
            XulyHangLoi xulyHangLoiForm = new XulyHangLoi();
            xulyHangLoiForm.ShowDialog();
            this.Close();
        }

        private void btnCreateOrder_Click(object sender, EventArgs e)
        {
            TaoDonHang taoDonHang = new TaoDonHang();
            if (taoDonHang.ShowDialog() == DialogResult.OK)
            {
                LoadDataDonHang();
            }
        }

        private void btnThongKe_Click(object sender, EventArgs e)
        {
            this.Hide();
            BaoCaoThongKe baoCaoThongKeForm = new BaoCaoThongKe();
            baoCaoThongKeForm.ShowDialog();
            this.Close();
        }

        private void btnDangNhap_Click(object sender, EventArgs e)
        {
            DialogResult confirm = MessageBox.Show(
                "Bạn có chắc chắn muốn ĐĂNG XUẤT và quay lại màn hình đăng nhập?",
                "Xác nhận đăng xuất",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
            {
                try
                {
                    string tempPath = System.IO.Path.Combine(Application.StartupPath, "session.txt");
                    if (System.IO.File.Exists(tempPath))
                    {
                        System.IO.File.Delete(tempPath);
                    }

                    string baseDir = Application.StartupPath;
                    string targetExe = "ERP_Khach.exe";
                    string pathExeDangNhap = "";

                    string[] possiblePaths = new string[]
                    {
                        System.IO.Path.Combine(baseDir, targetExe),
                        System.IO.Path.Combine(baseDir, "..", targetExe),
                        System.IO.Path.Combine(baseDir, "..", "ERP_Khach", targetExe),
                        System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, @"..\..\..\..\ERP_Khach\bin\Debug\ERP_Khach.exe")),
                        System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, @"..\..\..\..\ERP_Khach\bin\Release\ERP_Khach.exe"))
                    };

                    foreach (string p in possiblePaths)
                    {
                        if (System.IO.File.Exists(p))
                        {
                            pathExeDangNhap = p;
                            break;
                        }
                    }

                    if (!string.IsNullOrEmpty(pathExeDangNhap))
                    {
                        System.Diagnostics.Process.Start(pathExeDangNhap);
                        Application.Exit();
                    }
                    else
                    {
                        MessageBox.Show("Không tìm thấy file ứng dụng Đăng nhập (ERP_Khach.exe)!\nVui lòng kiểm tra lại thư mục chứa file.",
                                        "Lỗi khởi chạy", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi khi đăng xuất: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}