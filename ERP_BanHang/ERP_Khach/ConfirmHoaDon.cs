using System;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Npgsql;

namespace ERP_Khach
{
    public partial class ConfirmHoaDon : Form
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["ERP_Connection"]?.ConnectionString
            ?? ConfigurationManager.ConnectionStrings["ERP_BanHang"]?.ConnectionString;

        private DataTable dtDanhSach;

        public ConfirmHoaDon()
        {
            InitializeComponent();
        }

        private void ConfirmHoaDon_Load(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Maximized;

            KhoiTaoCotBang();
            LoadDanhSachCanThanhToan();
        }

        private void KhoiTaoCotBang()
        {
            dgvHoaDon.Columns.Clear();
            dgvHoaDon.AutoGenerateColumns = false;

            // 1. Mã Đơn Hàng
            DataGridViewTextBoxColumn colMaDH = new DataGridViewTextBoxColumn
            {
                Name = "colID_DH",
                HeaderText = "MÃ ĐƠN HÀNG",
                DataPropertyName = "id_dh",
                FillWeight = 15
            };
            colMaDH.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            colMaDH.DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            colMaDH.DefaultCellStyle.ForeColor = Color.FromArgb(13, 110, 253);
            colMaDH.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvHoaDon.Columns.Add(colMaDH);

            // 2. Mã Hóa Đơn
            DataGridViewTextBoxColumn colMaHD = new DataGridViewTextBoxColumn
            {
                Name = "colID_HD",
                HeaderText = "MÃ HÓA ĐƠN",
                DataPropertyName = "id_hd",
                FillWeight = 15
            };
            colMaHD.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            colMaHD.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvHoaDon.Columns.Add(colMaHD);

            // 3. Thông Tin Khách Hàng
            DataGridViewTextBoxColumn colKH = new DataGridViewTextBoxColumn
            {
                Name = "colTenKH",
                HeaderText = "MÃ / TÊN KHÁCH HÀNG",
                DataPropertyName = "khachhang_info",
                FillWeight = 25
            };
            colKH.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgvHoaDon.Columns.Add(colKH);

            // 4. Ngày Tạo
            DataGridViewTextBoxColumn colNgayTao = new DataGridViewTextBoxColumn
            {
                Name = "colNgayTao",
                HeaderText = "NGÀY TẠO",
                DataPropertyName = "ngaytao",
                FillWeight = 18
            };
            colNgayTao.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            colNgayTao.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
            colNgayTao.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvHoaDon.Columns.Add(colNgayTao);

            // 5. Tổng Tiền
            DataGridViewTextBoxColumn colTongTien = new DataGridViewTextBoxColumn
            {
                Name = "colTongTien",
                HeaderText = "TỔNG TIỀN (VNĐ)",
                DataPropertyName = "tongtien",
                FillWeight = 18
            };
            colTongTien.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            colTongTien.DefaultCellStyle.Format = "#,##0";
            colTongTien.DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            colTongTien.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvHoaDon.Columns.Add(colTongTien);

            // 6. Trạng Thái Thanh Toán
            DataGridViewTextBoxColumn colTrangThai = new DataGridViewTextBoxColumn
            {
                Name = "colTrangThai",
                HeaderText = "THANH TOÁN",
                DataPropertyName = "trangthaithanhtoan",
                FillWeight = 15
            };
            colTrangThai.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            colTrangThai.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvHoaDon.Columns.Add(colTrangThai);

            dgvHoaDon.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        private void LoadDanhSachCanThanhToan()
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                MessageBox.Show("Chưa cấu hình chuỗi kết nối cơ sở dữ liệu trong App.config!", "Lỗi cấu hình", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Dùng REPLACE(d.id_dh, 'DH', 'HD') để rút gọn mã HĐ chuẩn <= 10 ký tự
            string query = @"
                SELECT 
                    d.id_dh, 
                    COALESCE(d.id_nv, 'NV001') AS id_nv,
                    COALESCE(h.id_hd, REPLACE(d.id_dh, 'DH', 'HD')) AS id_hd,
                    d.ngaytao, 
                    COALESCE(h.tongtien, 
                        (SELECT COALESCE(SUM(ct.soluong * ct.dongia), 0) 
                         FROM chitietdonhang ct 
                         WHERE TRIM(ct.id_dh) = TRIM(d.id_dh)), 0) AS tongtien, 
                    COALESCE(h.trangthai, d.trangthai, 'Chưa thanh toán') AS trangthaithanhtoan
                FROM donhang d
                LEFT JOIN hoadon h ON TRIM(d.id_dh) = TRIM(h.id_dh)
                WHERE TRIM(LOWER(COALESCE(h.trangthai, d.trangthai, ''))) NOT ILIKE '%đã thanh toán%'
                   OR TRIM(LOWER(COALESCE(h.trangthai, ''))) ILIKE '%chưa thanh toán%'
                ORDER BY d.id_dh DESC";

            using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    NpgsqlDataAdapter da = new NpgsqlDataAdapter(query, conn);
                    dtDanhSach = new DataTable();
                    da.Fill(dtDanhSach);

                    if (!dtDanhSach.Columns.Contains("khachhang_info"))
                    {
                        dtDanhSach.Columns.Add("khachhang_info", typeof(string));
                        foreach (DataRow row in dtDanhSach.Rows)
                        {
                            row["khachhang_info"] = "Khách hàng " + row["id_dh"]?.ToString();
                        }
                    }

                    dgvHoaDon.DataSource = dtDanhSach;
                    DinhDangMauTrangThai();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi tải danh sách cần thanh toán: " + ex.Message, "Lỗi PostgreSQL", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void DinhDangMauTrangThai()
        {
            foreach (DataGridViewRow row in dgvHoaDon.Rows)
            {
                if (row.Cells["colTrangThai"].Value != null)
                {
                    string status = row.Cells["colTrangThai"].Value.ToString();
                    if (status.Equals("Đã thanh toán", StringComparison.OrdinalIgnoreCase))
                    {
                        row.Cells["colTrangThai"].Style.ForeColor = Color.ForestGreen;
                        row.Cells["colTrangThai"].Style.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                    }
                    else
                    {
                        row.Cells["colTrangThai"].Style.ForeColor = Color.DarkOrange;
                        row.Cells["colTrangThai"].Style.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                    }
                }
            }
        }

        private void btnDuyet_Click(object sender, EventArgs e)
        {
            if (dgvHoaDon.CurrentRow == null)
            {
                MessageBox.Show("Vui lòng chọn đơn hàng/hóa đơn cần duyệt thanh toán!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string idDH = dgvHoaDon.CurrentRow.Cells["colID_DH"].Value?.ToString();
            string idHD = dgvHoaDon.CurrentRow.Cells["colID_HD"].Value?.ToString();

            DataRowView currentDataRow = dgvHoaDon.CurrentRow.DataBoundItem as DataRowView;
            string idNV = currentDataRow != null && dtDanhSach.Columns.Contains("id_nv")
                ? currentDataRow["id_nv"]?.ToString()
                : "NV001";

            if (string.IsNullOrEmpty(idNV)) idNV = "NV001";

            decimal tongTien = 0;
            if (dgvHoaDon.CurrentRow.Cells["colTongTien"].Value != DBNull.Value)
            {
                decimal.TryParse(dgvHoaDon.CurrentRow.Cells["colTongTien"].Value?.ToString(), out tongTien);
            }

            if (string.IsNullOrEmpty(idDH)) return;

            DialogResult confirm = MessageBox.Show(
                $"Xác nhận duyệt thanh toán cho Đơn hàng [{idDH}] ({tongTien:N0} VNĐ)?\n\nHệ thống sẽ tự động:\n1. Tạo/cập nhật Hóa đơn [{idHD}]\n2. Cập nhật trạng thái Hóa đơn & Đơn hàng sang 'Đã thanh toán'\n3. Lưu nhật ký giao dịch vào bảng Thanh toán.",
                "Xác nhận duyệt thanh toán",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
            {
                using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
                {
                    try
                    {
                        conn.Open();

                        using (NpgsqlTransaction trans = conn.BeginTransaction())
                        {
                            try
                            {
                                // Tạo mã ngắn gọn chuẩn <= 10 ký tự: DH013 -> HD013 & TT013
                                string finalMaHD = string.IsNullOrEmpty(idHD) ? idDH.Replace("DH", "HD") : idHD;
                                string maTT = idDH.Replace("DH", "TT");

                                // Cắt ngắn tuyệt đối phòng trường hợp chuỗi quá 10 ký tự
                                if (finalMaHD.Length > 10) finalMaHD = finalMaHD.Substring(0, 10);
                                if (maTT.Length > 10) maTT = maTT.Substring(0, 10);

                                // BƯỚC 1: Cập nhật / Thêm mới hoadon
                                string sqlInsertHD = @"
                                    INSERT INTO hoadon (id_hd, id_dh, id_nv, ngaylap, tongtien, trangthai)
                                    VALUES (@ID_HD, @ID_DH, @ID_NV, CURRENT_TIMESTAMP, @TongTien, 'Đã thanh toán')
                                    ON CONFLICT (id_hd) DO UPDATE SET trangthai = 'Đã thanh toán', id_nv = EXCLUDED.id_nv";

                                using (NpgsqlCommand cmdInsHD = new NpgsqlCommand(sqlInsertHD, conn, trans))
                                {
                                    cmdInsHD.Parameters.AddWithValue("@ID_HD", finalMaHD);
                                    cmdInsHD.Parameters.AddWithValue("@ID_DH", idDH);
                                    cmdInsHD.Parameters.AddWithValue("@ID_NV", idNV);
                                    cmdInsHD.Parameters.AddWithValue("@TongTien", tongTien);
                                    cmdInsHD.ExecuteNonQuery();
                                }

                                // BƯỚC 2: Cập nhật donhang
                                string sqlUpDH = "UPDATE donhang SET trangthai = 'Đã thanh toán' WHERE TRIM(id_dh) = TRIM(@ID_DH)";
                                using (NpgsqlCommand cmdUpDH = new NpgsqlCommand(sqlUpDH, conn, trans))
                                {
                                    cmdUpDH.Parameters.AddWithValue("@ID_DH", idDH);
                                    cmdUpDH.ExecuteNonQuery();
                                }

                                // BƯỚC 3: Ghi sổ bảng thanhtoan (Dùng maTT <= 10 ký tự)
                                string sqlInsertTT = @"
                                    INSERT INTO thanhtoan (id_tt, id_hd, sotien, ngaythanhtoan)
                                    VALUES (@ID_TT, @ID_HD, @SoTien, CURRENT_TIMESTAMP)
                                    ON CONFLICT (id_tt) DO UPDATE SET sotien = EXCLUDED.sotien";

                                using (NpgsqlCommand cmdTT = new NpgsqlCommand(sqlInsertTT, conn, trans))
                                {
                                    cmdTT.Parameters.AddWithValue("@ID_TT", maTT);
                                    cmdTT.Parameters.AddWithValue("@ID_HD", finalMaHD);
                                    cmdTT.Parameters.AddWithValue("@SoTien", tongTien);
                                    cmdTT.ExecuteNonQuery();
                                }

                                trans.Commit();

                                MessageBox.Show($"Duyệt thanh toán thành công!\n- Mã Đơn hàng: {idDH}\n- Mã Hóa đơn: {finalMaHD}\n- Mã Thanh toán: {maTT}\n- Số tiền: {tongTien:N0} VNĐ", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);

                                LoadDanhSachCanThanhToan();
                            }
                            catch (Exception exTrans)
                            {
                                trans.Rollback();
                                MessageBox.Show("Lỗi trong quá trình xử lý giao dịch: " + exTrans.Message, "Lỗi Transaction", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Lỗi kết nối CSDL PostgreSQL: " + ex.Message, "Lỗi PostgreSQL", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void txtTimKiem_TextChanged(object sender, EventArgs e)
        {
            if (dtDanhSach == null) return;

            string keyword = txtTimKiem.Text.Trim().Replace("'", "''");
            DataView dv = dtDanhSach.DefaultView;

            if (!string.IsNullOrEmpty(keyword))
            {
                dv.RowFilter = $"id_dh LIKE '%{keyword}%' OR id_hd LIKE '%{keyword}%' OR trangthaithanhtoan LIKE '%{keyword}%'";
            }
            else
            {
                dv.RowFilter = "";
            }

            dgvHoaDon.DataSource = dv;
            DinhDangMauTrangThai();
        }

        private void btnLamMoi_Click(object sender, EventArgs e)
        {
            txtTimKiem.Clear();
            LoadDanhSachCanThanhToan();
        }
    }
}