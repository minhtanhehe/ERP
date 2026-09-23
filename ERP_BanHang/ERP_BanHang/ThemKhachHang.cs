using System;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Npgsql;

namespace ERP_BanHang
{
    public partial class ThemKhachHang : Form
    {
        private string connectionString = ConfigurationManager.ConnectionStrings["ERP_Connection"].ConnectionString;

        public ThemKhachHang()
        {
            InitializeComponent();
        }

        private void btnNextToTab2_Click(object sender, EventArgs e)
        {
            // Chuyển sang Tab 2 (Địa chỉ & Liên hệ)
            tabControlMain.SelectedIndex = 1;
        }

        private void btnLuu_Click(object sender, EventArgs e)
        {
            string idKH = LayGiaTri(txtID_KH, "VD: KH001");
            string tenDN = LayGiaTri(txtTenDoanhNghiep, "VD: Công ty TNHH Phân Phối Thành Đạt");
            string nguoiDD = LayGiaTri(txtNguoiDaiDien, "VD: Nguyễn Văn A");
            string mst = LayGiaTri(txtMaSoThue, "VD: 0314567890");
            string sdt = LayGiaTri(txtSDT, "VD: 0987654321");
            string email = LayGiaTri(txtEmail, "VD: contact@thanhdat.com");
            string diaChi = LayGiaTri(txtDiaChi, "VD: Số 10, Đường Cầu Giấy, Hà Nội");

            // ==========================================
            // 1. BẮT ĐIỀU KIỆN RÀNG BUỘC DỮ LIỆU (EMPTY CHECK)
            // ==========================================

            if (string.IsNullOrEmpty(idKH))
            {
                ThongBaoVaFocus("Vui lòng nhập Mã khách hàng!", 0, txtID_KH);
                return;
            }

            if (string.IsNullOrEmpty(tenDN))
            {
                ThongBaoVaFocus("Vui lòng nhập Tên doanh nghiệp!", 0, txtTenDoanhNghiep);
                return;
            }

            if (string.IsNullOrEmpty(mst))
            {
                ThongBaoVaFocus("Vui lòng nhập Mã số thuế!", 0, txtMaSoThue);
                return;
            }

            if (string.IsNullOrEmpty(sdt))
            {
                ThongBaoVaFocus("Vui lòng nhập Số điện thoại!", 1, txtSDT);
                return;
            }

            if (string.IsNullOrEmpty(email))
            {
                ThongBaoVaFocus("Vui lòng nhập Email!", 1, txtEmail);
                return;
            }

            if (string.IsNullOrEmpty(diaChi))
            {
                ThongBaoVaFocus("Vui lòng nhập Địa chỉ!", 1, txtDiaChi);
                return;
            }

            // ==========================================
            // 2. KIỂM TRA ĐỊNH DẠNG (FORMAT VALIDATION)
            // ==========================================

            // MST: 10 số hoặc 13 số (có hoặc không có dấu gạch ngang)
            if (!Regex.IsMatch(mst, @"^([0-9]{10}|[0-9]{10}-[0-9]{3}|[0-9]{13})$"))
            {
                ThongBaoVaFocus("Mã số thuế không hợp lệ! MST phải gồm 10 số hoặc 13 số (VD: 0314567890).", 0, txtMaSoThue);
                return;
            }

            // SĐT: Bắt đầu bằng 0 và đúng 10 chữ số
            if (!Regex.IsMatch(sdt, @"^0[0-9]{9}$"))
            {
                ThongBaoVaFocus("Số điện thoại không hợp lệ! SĐT phải bắt đầu bằng số 0 và gồm 10 chữ số.", 1, txtSDT);
                return;
            }

            // Email format
            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                ThongBaoVaFocus("Địa chỉ Email không đúng định dạng (VD: contact@domain.com)!", 1, txtEmail);
                return;
            }

            // ==========================================
            // 3. KIỂM TRA TRÙNG LẶP TOÀN BỘ TRONG POSTGRESQL (NEON)
            // ==========================================
            using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    // Truy vấn gộp kiểm tra tất cả các trường cần kiểm soát trùng lặp
                    string checkDuplicateQuery = @"
                        SELECT id_kh, tendoanhnghiep, masothue, sdt, email 
                        FROM khachhang 
                        WHERE LOWER(id_kh) = LOWER(@ID_KH)
                           OR LOWER(tendoanhnghiep) = LOWER(@TenDoanhNghiep)
                           OR masothue = @MaSoThue
                           OR sdt = @SDT
                           OR LOWER(email) = LOWER(@Email)";

                    using (NpgsqlCommand cmdCheck = new NpgsqlCommand(checkDuplicateQuery, conn))
                    {
                        cmdCheck.Parameters.AddWithValue("@ID_KH", idKH);
                        cmdCheck.Parameters.AddWithValue("@TenDoanhNghiep", tenDN);
                        cmdCheck.Parameters.AddWithValue("@MaSoThue", mst);
                        cmdCheck.Parameters.AddWithValue("@SDT", sdt);
                        cmdCheck.Parameters.AddWithValue("@Email", email);

                        using (NpgsqlDataReader reader = cmdCheck.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string dbId = reader["id_kh"]?.ToString() ?? "";
                                string dbTenDN = reader["tendoanhnghiep"]?.ToString() ?? "";
                                string dbMst = reader["masothue"]?.ToString() ?? "";
                                string dbSdt = reader["sdt"]?.ToString() ?? "";
                                string dbEmail = reader["email"]?.ToString() ?? "";

                                if (dbId.Equals(idKH, StringComparison.OrdinalIgnoreCase))
                                {
                                    ThongBaoLoiTrung($"Mã khách hàng [{idKH}] đã tồn tại!", 0, txtID_KH);
                                    return;
                                }
                                if (dbTenDN.Equals(tenDN, StringComparison.OrdinalIgnoreCase))
                                {
                                    ThongBaoLoiTrung($"Tên doanh nghiệp [{tenDN}] đã trùng với khách hàng khác!", 0, txtTenDoanhNghiep);
                                    return;
                                }
                                if (!string.IsNullOrEmpty(mst) && dbMst.Equals(mst, StringComparison.OrdinalIgnoreCase))
                                {
                                    ThongBaoLoiTrung($"Mã số thuế [{mst}] đã được đăng ký cho khách hàng khác!", 0, txtMaSoThue);
                                    return;
                                }
                                if (!string.IsNullOrEmpty(sdt) && dbSdt.Equals(sdt, StringComparison.OrdinalIgnoreCase))
                                {
                                    ThongBaoLoiTrung($"Số điện thoại [{sdt}] đã tồn tại trên hệ thống!", 1, txtSDT);
                                    return;
                                }
                                if (!string.IsNullOrEmpty(email) && dbEmail.Equals(email, StringComparison.OrdinalIgnoreCase))
                                {
                                    ThongBaoLoiTrung($"Email [{email}] đã được sử dụng bởi khách hàng khác!", 1, txtEmail);
                                    return;
                                }
                            }
                        }
                    }

                    // ==========================================
                    // 4. THỰC HIỆN THÊM MỚI KHI THỎA MÃN TẤT CẢ
                    // ==========================================
                    string insertQuery = @"
                        INSERT INTO khachhang (id_kh, nguoidaidien, tendoanhnghiep, masothue, diachi, sdt, email)
                        VALUES (@ID_KH, @NguoiDaiDien, @TenDoanhNghiep, @MaSoThue, @DiaChi, @SDT, @Email)";

                    using (NpgsqlCommand cmdInsert = new NpgsqlCommand(insertQuery, conn))
                    {
                        cmdInsert.Parameters.AddWithValue("@ID_KH", idKH);
                        cmdInsert.Parameters.AddWithValue("@NguoiDaiDien", string.IsNullOrEmpty(nguoiDD) ? (object)DBNull.Value : nguoiDD);
                        cmdInsert.Parameters.AddWithValue("@TenDoanhNghiep", tenDN);
                        cmdInsert.Parameters.AddWithValue("@MaSoThue", string.IsNullOrEmpty(mst) ? (object)DBNull.Value : mst);
                        cmdInsert.Parameters.AddWithValue("@DiaChi", string.IsNullOrEmpty(diaChi) ? (object)DBNull.Value : diaChi);
                        cmdInsert.Parameters.AddWithValue("@SDT", string.IsNullOrEmpty(sdt) ? (object)DBNull.Value : sdt);
                        cmdInsert.Parameters.AddWithValue("@Email", string.IsNullOrEmpty(email) ? (object)DBNull.Value : email);

                        cmdInsert.ExecuteNonQuery();
                    }

                    MessageBox.Show("Thêm khách hàng mới thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi khi thêm khách hàng: " + ex.Message, "Lỗi PostgreSQL", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // ==========================================
        // CÁC HÀM TRỢ GIÚP (HELPER METHODS)
        // ==========================================

        private void ThongBaoVaFocus(string message, int tabIndex, TextBox txt)
        {
            MessageBox.Show(message, "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            tabControlMain.SelectedIndex = tabIndex;
            txt.Focus();
        }

        private void ThongBaoLoiTrung(string message, int tabIndex, TextBox txt)
        {
            MessageBox.Show(message, "Trùng dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            tabControlMain.SelectedIndex = tabIndex;
            txt.Focus();
        }

        private string LayGiaTri(TextBox txt, string placeholder)
        {
            string val = txt.Text.Trim();
            return (val == placeholder) ? "" : val;
        }

        private void btnHuy_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // ==========================================
        // XỬ LÝ PLACEHOLDER
        // ==========================================
        private void RemovePlaceholder(object sender, EventArgs e)
        {
            TextBox txt = sender as TextBox;
            if (txt != null && txt.ForeColor == Color.Gray)
            {
                txt.Text = "";
                txt.ForeColor = Color.Black;
            }
        }

        private void SetPlaceholder(object sender, EventArgs e)
        {
            TextBox txt = sender as TextBox;
            if (txt != null && string.IsNullOrWhiteSpace(txt.Text))
            {
                if (txt == txtID_KH) txt.Text = "VD: KH001";
                else if (txt == txtTenDoanhNghiep) txt.Text = "VD: Công ty TNHH Phân Phối Thành Đạt";
                else if (txt == txtNguoiDaiDien) txt.Text = "VD: Nguyễn Văn A";
                else if (txt == txtMaSoThue) txt.Text = "VD: 0314567890";
                else if (txt == txtSDT) txt.Text = "VD: 0987654321";
                else if (txt == txtEmail) txt.Text = "VD: contact@thanhdat.com";
                else if (txt == txtDiaChi) txt.Text = "VD: Số 10, Đường Cầu Giấy, Hà Nội";

                txt.ForeColor = Color.Gray;
            }
        }
    }
}