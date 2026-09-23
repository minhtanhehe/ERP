using System;
using System.Configuration;
using System.Data;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Npgsql;

namespace ERP_BanHang
{
    public partial class SuaKhachHang : Form
    {
        private string connectionString = ConfigurationManager.ConnectionStrings["ERP_Connection"].ConnectionString;
        private string maKH;

        public SuaKhachHang(string idKhachHang)
        {
            InitializeComponent();
            this.maKH = idKhachHang;
        }

        private void SuaKhachHang_Load(object sender, EventArgs e)
        {
            txtMaKH.Text = maKH;
            LoadThongTinKhachHang();
        }

        private void LoadThongTinKhachHang()
        {
            string query = @"
                SELECT 
                    tendoanhnghiep, 
                    nguoidaidien, 
                    sdt, 
                    email, 
                    diachi, 
                    masothue 
                FROM khachhang 
                WHERE LOWER(id_kh) = LOWER(@ID_KH)";

            using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ID_KH", maKH);
                        using (NpgsqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                txtTenDN.Text = reader["tendoanhnghiep"] != DBNull.Value ? reader["tendoanhnghiep"].ToString() : "";
                                txtNguoiDaiDien.Text = reader["nguoidaidien"] != DBNull.Value ? reader["nguoidaidien"].ToString() : "";
                                txtSDT.Text = reader["sdt"] != DBNull.Value ? reader["sdt"].ToString() : "";
                                txtEmail.Text = reader["email"] != DBNull.Value ? reader["email"].ToString() : "";
                                txtDiaChi.Text = reader["diachi"] != DBNull.Value ? reader["diachi"].ToString() : "";
                                txtMST.Text = reader["masothue"] != DBNull.Value ? reader["masothue"].ToString() : "";
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi tải thông tin khách hàng từ CSDL: " + ex.Message, "Lỗi PostgreSQL", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnLuu_Click(object sender, EventArgs e)
        {
            string tenDN = txtTenDN.Text.Trim();
            string nguoiDD = txtNguoiDaiDien.Text.Trim();
            string sdt = txtSDT.Text.Trim();
            string email = txtEmail.Text.Trim();
            string mst = txtMST.Text.Trim();
            string diaChi = txtDiaChi.Text.Trim();

            // ==========================================
            // 1. KIỂM TRA RÀNG BUỘC BẮT BUỘC (EMPTY CHECK)
            // ==========================================
            if (string.IsNullOrEmpty(tenDN))
            {
                ThongBaoVaFocus("Vui lòng nhập Tên doanh nghiệp / Đại lý!", txtTenDN);
                return;
            }

            if (string.IsNullOrEmpty(mst))
            {
                ThongBaoVaFocus("Vui lòng nhập Mã số thuế!", txtMST);
                return;
            }

            if (string.IsNullOrEmpty(sdt))
            {
                ThongBaoVaFocus("Vui lòng nhập Số điện thoại!", txtSDT);
                return;
            }

            if (string.IsNullOrEmpty(email))
            {
                ThongBaoVaFocus("Vui lòng nhập Email!", txtEmail);
                return;
            }

            if (string.IsNullOrEmpty(diaChi))
            {
                ThongBaoVaFocus("Vui lòng nhập Địa chỉ!", txtDiaChi);
                return;
            }

            // ==========================================
            // 2. KIỂM TRA ĐỊNH DẠNG (FORMAT VALIDATION)
            // ==========================================
            if (!Regex.IsMatch(mst, @"^([0-9]{10}|[0-9]{10}-[0-9]{3}|[0-9]{13})$"))
            {
                ThongBaoVaFocus("Mã số thuế không hợp lệ! MST phải gồm 10 hoặc 13 chữ số (VD: 0314567890).", txtMST);
                return;
            }

            if (!Regex.IsMatch(sdt, @"^0[0-9]{9}$"))
            {
                ThongBaoVaFocus("Số điện thoại không hợp lệ! SĐT phải bắt đầu bằng số 0 và gồm đúng 10 chữ số.", txtSDT);
                return;
            }

            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                ThongBaoVaFocus("Địa chỉ Email không đúng định dạng (VD: contact@domain.com)!", txtEmail);
                return;
            }

            // ==========================================
            // 3. KIỂM TRA TRÙNG LẶP DỮ LIỆU VỚI KHÁCH HÀNG KHÁC
            // ==========================================
            using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    string checkDuplicateQuery = @"
                        SELECT tendoanhnghiep, masothue, sdt, email 
                        FROM khachhang 
                        WHERE LOWER(id_kh) <> LOWER(@ID_KH)
                          AND (
                                LOWER(tendoanhnghiep) = LOWER(@TenDN)
                             OR masothue = @MST
                             OR sdt = @SDT
                             OR LOWER(email) = LOWER(@Email)
                          )";

                    using (NpgsqlCommand cmdCheck = new NpgsqlCommand(checkDuplicateQuery, conn))
                    {
                        cmdCheck.Parameters.AddWithValue("@ID_KH", maKH);
                        cmdCheck.Parameters.AddWithValue("@TenDN", tenDN);
                        cmdCheck.Parameters.AddWithValue("@MST", mst);
                        cmdCheck.Parameters.AddWithValue("@SDT", sdt);
                        cmdCheck.Parameters.AddWithValue("@Email", email);

                        using (NpgsqlDataReader reader = cmdCheck.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string dbTenDN = reader["tendoanhnghiep"]?.ToString() ?? "";
                                string dbMst = reader["masothue"]?.ToString() ?? "";
                                string dbSdt = reader["sdt"]?.ToString() ?? "";
                                string dbEmail = reader["email"]?.ToString() ?? "";

                                if (dbTenDN.Equals(tenDN, StringComparison.OrdinalIgnoreCase))
                                {
                                    ThongBaoVaFocus($"Tên doanh nghiệp [{tenDN}] đã trùng với một khách hàng khác!", txtTenDN);
                                    return;
                                }
                                if (dbMst.Equals(mst, StringComparison.OrdinalIgnoreCase))
                                {
                                    ThongBaoVaFocus($"Mã số thuế [{mst}] đã trùng với một khách hàng khác!", txtMST);
                                    return;
                                }
                                if (dbSdt.Equals(sdt, StringComparison.OrdinalIgnoreCase))
                                {
                                    ThongBaoVaFocus($"Số điện thoại [{sdt}] đã trùng với một khách hàng khác!", txtSDT);
                                    return;
                                }
                                if (dbEmail.Equals(email, StringComparison.OrdinalIgnoreCase))
                                {
                                    ThongBaoVaFocus($"Email [{email}] đã trùng với một khách hàng khác!", txtEmail);
                                    return;
                                }
                            }
                        }
                    }

                    // ==========================================
                    // 4. THỰC HIỆN CẬP NHẬT DỮ LIỆU
                    // ==========================================
                    string updateQuery = @"
                        UPDATE khachhang 
                        SET tendoanhnghiep = @TenDN, 
                            nguoidaidien = @NguoiDaiDien, 
                            sdt = @SDT, 
                            email = @Email, 
                            diachi = @DiaChi, 
                            masothue = @MST 
                        WHERE LOWER(id_kh) = LOWER(@ID_KH)";

                    using (NpgsqlCommand cmd = new NpgsqlCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@TenDN", tenDN);
                        cmd.Parameters.AddWithValue("@NguoiDaiDien", string.IsNullOrEmpty(nguoiDD) ? (object)DBNull.Value : nguoiDD);
                        cmd.Parameters.AddWithValue("@SDT", sdt);
                        cmd.Parameters.AddWithValue("@Email", email);
                        cmd.Parameters.AddWithValue("@DiaChi", diaChi);
                        cmd.Parameters.AddWithValue("@MST", mst);
                        cmd.Parameters.AddWithValue("@ID_KH", maKH);

                        cmd.ExecuteNonQuery();
                    }

                    MessageBox.Show($"Cập nhật thành công thông tin khách hàng [{maKH}]!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi khi lưu dữ liệu: " + ex.Message, "Lỗi PostgreSQL", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ThongBaoVaFocus(string message, TextBox txt)
        {
            MessageBox.Show(message, "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txt.Focus();
        }

        private void btnHuy_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}