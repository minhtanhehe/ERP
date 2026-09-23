using Npgsql;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ERP_BanHang
{
    public partial class BaoCaoThongKe : Form
    {
        private string connectionString = ConfigurationManager.ConnectionStrings["ERP_Connection"].ConnectionString;

        public BaoCaoThongKe()
        {
            InitializeComponent();
        }

        private void BaoCaoThongKe_Load(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Maximized;

            // Đặt thời gian mặc định: Đầu tháng hiện tại -> Hôm nay
            DateTime now = DateTime.Now;
            dtpFromDate.Value = new DateTime(now.Year, now.Month, 1);
            dtpToDate.Value = now;

            if (cboReportType.Items.Count > 0)
            {
                cboReportType.SelectedIndex = 0;
            }

            TaiTongQuanKPI();
            TaiDuLieuBaoCao();
        }

        // ==========================================
        // 1. TÍNH TOÁN CÁC THẺ KPI TỔNG QUAN
        // ==========================================
        private void TaiTongQuanKPI()
        {
            using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    // KPI 1: Tổng doanh thu từ bảng HoaDon
                    string queryDoanhThu = "SELECT COALESCE(SUM(TongTien), 0) FROM HoaDon WHERE TrangThai = N'Đã thanh toán'";
                    using (NpgsqlCommand cmd1 = new NpgsqlCommand(queryDoanhThu, conn))
                    {
                        decimal tongDoanhThu = Convert.ToDecimal(cmd1.ExecuteScalar());
                        lblKPI1Value.Text = string.Format("{0:#,##0} VNĐ", tongDoanhThu);
                    }

                    // KPI 2: Số đơn hàng đã giao thành công
                    string queryDonHang = "SELECT COUNT(DISTINCT ID_DH) FROM GiaoHang WHERE TrangThaiGiaoHang = N'Đã giao' OR TrangThaiGiaoHang = N'Hoàn thành'";
                    using (NpgsqlCommand cmd2 = new NpgsqlCommand(queryDonHang, conn))
                    {
                        int donHoanThanh = Convert.ToInt32(cmd2.ExecuteScalar());
                        lblKPI2Value.Text = donHoanThanh.ToString("#,##0") + " đơn";
                    }

                    // KPI 3: Số yêu cầu sau bán hàng
                    string queryYeuCau = "SELECT COUNT(*) FROM YeuCauSauBanHang";
                    using (NpgsqlCommand cmd3 = new NpgsqlCommand(queryYeuCau, conn))
                    {
                        int soYeuCau = Convert.ToInt32(cmd3.ExecuteScalar());
                        lblKPI3Value.Text = soYeuCau.ToString("#,##0") + " yêu cầu";
                    }
                }
                catch (Exception)
                {
                    lblKPI1Value.Text = "0 VNĐ";
                    lblKPI2Value.Text = "0 đơn";
                    lblKPI3Value.Text = "0 yêu cầu";
                }
            }
        }

        // ==========================================
        // 2. LẤY DỮ LIỆU BÁO CÁO THEO BỘ LỌC
        // ==========================================
        private void TaiDuLieuBaoCao()
        {
            DateTime tuNgay = dtpFromDate.Value.Date;
            DateTime denNgay = dtpToDate.Value.Date.AddDays(1).AddSeconds(-1);

            dgvData.Columns.Clear();
            dgvData.AutoGenerateColumns = false;

            using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    DataTable dt = new DataTable();

                    if (cboReportType.SelectedIndex == 0)
                    {
                        // ----- BÁO CÁO 1: DOANH THU ĐƠN HÀNG -----
                        TaoCotBaoCaoDonHang();

                        string query = @"
                            SELECT 
                                DH.ID_DH,
                                DH.NgayTao,
                                KH.TenDoanhNghiep AS TenKhachHang,
                                NV.TenNV AS NguoiLap,
                                COALESCE(HD.TongTien, 0) AS TongTien,
                                DH.TrangThai
                            FROM DonHang DH
                            LEFT JOIN KhachHang KH ON DH.ID_KH = KH.ID_KH
                            LEFT JOIN NhanVien NV ON DH.ID_NV = NV.ID_NV
                            LEFT JOIN HoaDon HD ON DH.ID_DH = HD.ID_DH
                            WHERE DH.NgayTao >= @TuNgay AND DH.NgayTao <= @DenNgay
                            ORDER BY DH.NgayTao DESC";

                        using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@TuNgay", tuNgay);
                            cmd.Parameters.AddWithValue("@DenNgay", denNgay);
                            NpgsqlDataAdapter da = new NpgsqlDataAdapter(cmd);
                            da.Fill(dt);
                        }
                    }
                    else
                    {
                        // ----- BÁO CÁO 2: YÊU CẦU SAU BÁN HÀNG & LỖI SẢN PHẨM -----
                        TaoCotBaoCaoYeuCau();

                        string query = @"
                            SELECT 
                                YC.ID_YC,
                                YC.ID_DH,
                                KH.TenDoanhNghiep AS TenKhachHang,
                                H.TenHang AS TenSanPham,
                                CTYC.SoLuong,
                                CTYC.TinhTrang,
                                YC.LoaiYeuCau,
                                YC.NgayYeuCau,
                                YC.TrangThai
                            FROM YeuCauSauBanHang YC
                            LEFT JOIN ChiTietYeuCau CTYC ON YC.ID_YC = CTYC.ID_YC
                            LEFT JOIN SanPham SP ON CTYC.ID_SP = SP.ID_SP
                            LEFT JOIN HangHoa H ON SP.MaHang = H.MaHang
                            LEFT JOIN KhachHang KH ON YC.ID_KH = KH.ID_KH
                            WHERE YC.NgayYeuCau >= @TuNgay AND YC.NgayYeuCau <= @DenNgay
                            ORDER BY YC.NgayYeuCau DESC";

                        using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@TuNgay", tuNgay);
                            cmd.Parameters.AddWithValue("@DenNgay", denNgay);
                            NpgsqlDataAdapter da = new NpgsqlDataAdapter(cmd);
                            da.Fill(dt);
                        }
                    }

                    dgvData.DataSource = dt;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi tải dữ liệu báo cáo: " + ex.Message, "Lỗi PostgreSQL", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // ==========================================
        // 3. TẠO CỘT CHO BẢNG DATAGRIDVIEW
        // ==========================================
        private void TaoCotBaoCaoDonHang()
        {
            DataGridViewTextBoxColumn colMaDH = new DataGridViewTextBoxColumn
            {
                Name = "colID_DH",
                HeaderText = "MÃ ĐƠN HÀNG",
                DataPropertyName = "ID_DH",
                FillWeight = 15,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(13, 110, 253) }
            };
            colMaDH.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvData.Columns.Add(colMaDH);

            DataGridViewTextBoxColumn colNgay = new DataGridViewTextBoxColumn
            {
                Name = "colNgayTao",
                HeaderText = "NGÀY TẠO ĐƠN",
                DataPropertyName = "NgayTao",
                FillWeight = 18,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Format = "dd/MM/yyyy HH:mm" }
            };
            colNgay.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvData.Columns.Add(colNgay);

            DataGridViewTextBoxColumn colKhach = new DataGridViewTextBoxColumn
            {
                Name = "colTenKhachHang",
                HeaderText = "TÊN DOANH NGHIỆP / KHÁCH HÀNG",
                DataPropertyName = "TenKhachHang",
                FillWeight = 30
            };
            dgvData.Columns.Add(colKhach);

            DataGridViewTextBoxColumn colNV = new DataGridViewTextBoxColumn
            {
                Name = "colNguoiLap",
                HeaderText = "NHÂN VIÊN LẬP",
                DataPropertyName = "NguoiLap",
                FillWeight = 20
            };
            dgvData.Columns.Add(colNV);

            DataGridViewTextBoxColumn colTongTien = new DataGridViewTextBoxColumn
            {
                Name = "colTongTien",
                HeaderText = "TỔNG TIỀN (VNĐ)",
                DataPropertyName = "TongTien",
                FillWeight = 20,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "#,##0", Font = new Font("Segoe UI", 9F, FontStyle.Bold) }
            };
            colTongTien.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvData.Columns.Add(colTongTien);

            DataGridViewTextBoxColumn colTrangThai = new DataGridViewTextBoxColumn
            {
                Name = "colTrangThai",
                HeaderText = "TRẠNG THÁI",
                DataPropertyName = "TrangThai",
                FillWeight = 17,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            };
            colTrangThai.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvData.Columns.Add(colTrangThai);
        }

        private void TaoCotBaoCaoYeuCau()
        {
            DataGridViewTextBoxColumn colMaYC = new DataGridViewTextBoxColumn
            {
                Name = "colID_YC",
                HeaderText = "MÃ YÊU CẦU",
                DataPropertyName = "ID_YC",
                FillWeight = 12,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.Crimson }
            };
            colMaYC.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvData.Columns.Add(colMaYC);

            DataGridViewTextBoxColumn colMaDH = new DataGridViewTextBoxColumn
            {
                Name = "colID_DH",
                HeaderText = "MÃ ĐƠN HÀNG",
                DataPropertyName = "ID_DH",
                FillWeight = 12,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            };
            colMaDH.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvData.Columns.Add(colMaDH);

            DataGridViewTextBoxColumn colKhach = new DataGridViewTextBoxColumn
            {
                Name = "colTenKhachHang",
                HeaderText = "KHÁCH HÀNG",
                DataPropertyName = "TenKhachHang",
                FillWeight = 22
            };
            dgvData.Columns.Add(colKhach);

            DataGridViewTextBoxColumn colTenSP = new DataGridViewTextBoxColumn
            {
                Name = "colTenSanPham",
                HeaderText = "SẢN PHẨM PHẢN HỒI",
                DataPropertyName = "TenSanPham",
                FillWeight = 22
            };
            dgvData.Columns.Add(colTenSP);

            DataGridViewTextBoxColumn colSL = new DataGridViewTextBoxColumn
            {
                Name = "colSoLuong",
                HeaderText = "SL",
                DataPropertyName = "SoLuong",
                FillWeight = 8,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            };
            colSL.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvData.Columns.Add(colSL);

            DataGridViewTextBoxColumn colTinhTrang = new DataGridViewTextBoxColumn
            {
                Name = "colTinhTrang",
                HeaderText = "TÌNH TRẠNG / LÝ DO LỖI",
                DataPropertyName = "TinhTrang",
                FillWeight = 24
            };
            dgvData.Columns.Add(colTinhTrang);

            DataGridViewTextBoxColumn colTrangThai = new DataGridViewTextBoxColumn
            {
                Name = "colTrangThai",
                HeaderText = "TRẠNG THÁI",
                DataPropertyName = "TrangThai",
                FillWeight = 15,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            };
            colTrangThai.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvData.Columns.Add(colTrangThai);
        }

        // ==========================================
        // 4. SỰ KIỆN LỌC VÀ ĐIỀU HƯỚNG
        // ==========================================
        private void cboReportType_SelectedIndexChanged(object sender, EventArgs e)
        {
            TaiDuLieuBaoCao();
        }

        private void btnFilter_Click(object sender, EventArgs e)
        {
            if (dtpFromDate.Value > dtpToDate.Value)
            {
                MessageBox.Show("Từ ngày không được lớn hơn Đến ngày!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            TaiDuLieuBaoCao();
        }

        private void btnExportExcel_Click(object sender, EventArgs e)
        {
            if (dgvData.Rows.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu trong bảng để xuất báo cáo!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // =========================================================================
            // THIẾT LẬP CẤU HÌNH LICENSE CHO EPPLUS ĐỂ TRÁNH LỖI LICENSE KHI XUẤT FILE
            // =========================================================================
            ExcelPackage.License.SetNonCommercialPersonal("Acecook ERP");

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                string reportTypeName = cboReportType.SelectedIndex == 0 ? "DoanhThuDonHang" : "YeuCauSauBanHang";
                sfd.Filter = "Excel Files (*.xlsx)|*.xlsx";
                sfd.FileName = $"BaoCao_{reportTypeName}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                sfd.Title = "Chọn nơi lưu tệp báo cáo Excel";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        FileInfo fileInfo = new FileInfo(sfd.FileName);
                        using (ExcelPackage package = new ExcelPackage(fileInfo))
                        {
                            string sheetName = cboReportType.SelectedIndex == 0 ? "Doanh Thu Đơn Hàng" : "Yêu Cầu Sau Bán Hàng";
                            ExcelWorksheet ws = package.Workbook.Worksheets.Add(sheetName);

                            // Hiển thị đường lưới trang tính
                            ws.View.ShowGridLines = true;

                            // -------------------------------------------------------------
                            // 1. BANNER TIÊU ĐỀ BÁO CÁO (HEADER)
                            // -------------------------------------------------------------
                            int totalCols = dgvData.Columns.Count;

                            // Tên doanh nghiệp
                            ws.Cells[2, 2].Value = "CÔNG TY CỔ PHẦN ACECOOK VIỆT NAM";
                            ws.Cells[2, 2].Style.Font.Bold = true;
                            ws.Cells[2, 2].Style.Font.Size = 11;
                            ws.Cells[2, 2].Style.Font.Color.SetColor(Color.FromArgb(108, 117, 125));

                            // Tiêu đề báo cáo
                            ws.Cells[3, 2, 3, totalCols + 1].Merge = true;
                            ws.Cells[3, 2].Value = cboReportType.SelectedIndex == 0
                                ? "BÁO CÁO DOANH THU BÁN HÀNG CHITIẾT"
                                : "BÁO CÁO YÊU CẦU SAU BÁN HÀNG & LỖI SẢN PHẨM";
                            ws.Cells[3, 2].Style.Font.Bold = true;
                            ws.Cells[3, 2].Style.Font.Size = 16;
                            ws.Cells[3, 2].Style.Font.Color.SetColor(Color.FromArgb(220, 53, 69)); // Đỏ Acecook
                            ws.Cells[3, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

                            // Kỳ báo cáo
                            ws.Cells[4, 2].Value = $"Kỳ báo cáo: Từ ngày {dtpFromDate.Value:dd/MM/yyyy} đến ngày {dtpToDate.Value:dd/MM/yyyy}";
                            ws.Cells[4, 2].Style.Font.Italic = true;
                            ws.Cells[4, 2].Style.Font.Size = 10;
                            ws.Cells[4, 2].Style.Font.Color.SetColor(Color.Gray);

                            // -------------------------------------------------------------
                            // 2. CỘT TIÊU ĐỀ (HEADER COLUMNS)
                            // -------------------------------------------------------------
                            int startRow = 6;
                            int startCol = 2; // Cột B

                            for (int i = 0; i < dgvData.Columns.Count; i++)
                            {
                                var cell = ws.Cells[startRow, startCol + i];
                                cell.Value = dgvData.Columns[i].HeaderText;

                                cell.Style.Font.Bold = true;
                                cell.Style.Font.Size = 10;
                                cell.Style.Font.Color.SetColor(Color.White);
                                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                                cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(13, 110, 253)); // Xanh Primary
                                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                                cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                                cell.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(222, 226, 230));
                            }
                            ws.Row(startRow).Height = 28;

                            // -------------------------------------------------------------
                            // 3. ĐỔ DỮ LIỆU TỪ DATAGRIDVIEW
                            // -------------------------------------------------------------
                            int currentRow = startRow + 1;
                            for (int r = 0; r < dgvData.Rows.Count; r++)
                            {
                                ws.Row(currentRow).Height = 22;
                                for (int c = 0; c < dgvData.Columns.Count; c++)
                                {
                                    var cell = ws.Cells[currentRow, startCol + c];
                                    object val = dgvData.Rows[r].Cells[c].Value;

                                    if (val != null)
                                    {
                                        if (val is DateTime dateVal)
                                        {
                                            cell.Value = dateVal;
                                            cell.Style.Numberformat.Format = "dd/mm/yyyy hh:mm";
                                            cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                                        }
                                        else if (decimal.TryParse(val.ToString(), out decimal decVal) && dgvData.Columns[c].Name == "colTongTien")
                                        {
                                            cell.Value = decVal;
                                            cell.Style.Numberformat.Format = "#,##0";
                                            cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                                            cell.Style.Font.Bold = true;
                                        }
                                        else if (int.TryParse(val.ToString(), out int intVal) && dgvData.Columns[c].Name == "colSoLuong")
                                        {
                                            cell.Value = intVal;
                                            cell.Style.Numberformat.Format = "#,##0";
                                            cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                                        }
                                        else
                                        {
                                            cell.Value = val.ToString();
                                            if (dgvData.Columns[c].Name.Contains("ID_") || dgvData.Columns[c].Name.Contains("TrangThai"))
                                                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                                            else
                                                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                                        }
                                    }

                                    // Zebra Striping (Tô màu dòng xen kẽ)
                                    if (r % 2 == 1)
                                    {
                                        cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                                        cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(248, 249, 250));
                                    }

                                    cell.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(222, 226, 230));
                                    cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                                }
                                currentRow++;
                            }

                            // -------------------------------------------------------------
                            // 4. DÒNG TỔNG CỘNG
                            // -------------------------------------------------------------
                            if (cboReportType.SelectedIndex == 0)
                            {
                                ws.Row(currentRow).Height = 25;
                                ws.Cells[currentRow, startCol, currentRow, startCol + 3].Merge = true;
                                ws.Cells[currentRow, startCol].Value = "TỔNG CỘNG DOANH THU:";
                                ws.Cells[currentRow, startCol].Style.Font.Bold = true;
                                ws.Cells[currentRow, startCol].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                                var totalCell = ws.Cells[currentRow, startCol + 4];
                                totalCell.Formula = $"SUM({ws.Cells[startRow + 1, startCol + 4].Address}:{ws.Cells[currentRow - 1, startCol + 4].Address})";
                                totalCell.Style.Font.Bold = true;
                                totalCell.Style.Font.Color.SetColor(Color.FromArgb(220, 53, 69));
                                totalCell.Style.Numberformat.Format = "#,##0 VNĐ";
                                totalCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                                for (int c = 0; c < dgvData.Columns.Count; c++)
                                {
                                    var cell = ws.Cells[currentRow, startCol + c];
                                    cell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                                    cell.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                                    cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(233, 236, 239));
                                }
                                currentRow += 2;
                            }

                            // -------------------------------------------------------------
                            // 5. KHU VỰC CHỮ KÝ
                            // -------------------------------------------------------------
                            int signRow = currentRow + 1;

                            ws.Cells[signRow, startCol + 1].Value = "NGƯỜI LẬP BÁO CÁO";
                            ws.Cells[signRow, startCol + 1].Style.Font.Bold = true;
                            ws.Cells[signRow, startCol + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                            ws.Cells[signRow, totalCols].Value = "XÁC NHẬN KẾ TOÁN TRƯỞNG";
                            ws.Cells[signRow, totalCols].Style.Font.Bold = true;
                            ws.Cells[signRow, totalCols].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                            ws.Cells[signRow + 1, startCol + 1].Value = "(Ký, ghi rõ họ tên)";
                            ws.Cells[signRow + 1, startCol + 1].Style.Font.Italic = true;
                            ws.Cells[signRow + 1, startCol + 1].Style.Font.Size = 9;
                            ws.Cells[signRow + 1, startCol + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                            ws.Cells[signRow + 1, totalCols].Value = "(Ký, đóng dấu)";
                            ws.Cells[signRow + 1, totalCols].Style.Font.Italic = true;
                            ws.Cells[signRow + 1, totalCols].Style.Font.Size = 9;
                            ws.Cells[signRow + 1, totalCols].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                            // -------------------------------------------------------------
                            // 6. TỰ ĐỘNG CĂN CHỈNH ĐỘ RỘNG CỘT (AUTOFIT)
                            // -------------------------------------------------------------
                            for (int col = 1; col <= totalCols + 2; col++)
                            {
                                ws.Column(col).AutoFit();
                                ws.Column(col).Width += 4;
                            }

                            package.Save();
                        }

                        MessageBox.Show("Xuất báo cáo Excel thành công!\nTệp đã được lưu tại: " + sfd.FileName,
                                        "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Đã xảy ra lỗi trong quá trình xuất Excel: " + ex.Message,
                                        "Lỗi xuất file", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        // ==========================================
        // 5. ĐIỀU HƯỚNG VÀ ĐĂNG XUẤT
        // ==========================================
        private void btnSanPham_Click(object sender, EventArgs e)
        {
            this.Hide();
            QlySanPham qlySanPhamForm = new QlySanPham();
            qlySanPhamForm.ShowDialog();
            this.Close();
        }

        private void btnDonHang_Click(object sender, EventArgs e)
        {
            this.Hide();
            QlyDonHang qlyDonHangForm = new QlyDonHang();
            qlyDonHangForm.ShowDialog();
            this.Close();
        }

        private void btnNhaPhanPhoi_Click(object sender, EventArgs e)
        {
            this.Hide();
            QlyKhachHang qlyKhachHangForm = new QlyKhachHang();
            qlyKhachHangForm.ShowDialog();
            this.Close();
        }

        private void btnHangTraLoi_Click(object sender, EventArgs e)
        {
            this.Hide();
            XulyHangLoi xulyHangLoiForm = new XulyHangLoi();
            xulyHangLoiForm.ShowDialog();
            this.Close();
        }

        private void btnGiaoHang_Click(object sender, EventArgs e)
        {
            this.Hide();
            QlyGiaoHang qlyGiaoHangForm = new QlyGiaoHang();
            qlyGiaoHangForm.ShowDialog();
            this.Close();
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
                    // 1. Xóa file phiên làm việc tạm (nếu có)
                    string tempPath = System.IO.Path.Combine(Application.StartupPath, "session.txt");
                    if (System.IO.File.Exists(tempPath))
                    {
                        System.IO.File.Delete(tempPath);
                    }

                    // 2. Thuật toán tìm file ERP_Khach.exe linh hoạt
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

                    // 3. Khởi chạy ứng dụng đăng nhập và đóng ứng dụng hiện tại
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