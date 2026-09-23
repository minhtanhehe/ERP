using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using HR_Management.DAL;
using HR_Management.DTO;

namespace HR_Management.BLL
{
    public class HeThongBLL
    {
        private readonly HeThongDAL _dal = new HeThongDAL();

        // Biến lưu thông tin phiên đăng nhập hiện tại
        public static HeThongDTO? CurrentUser { get; set; }

        /// <summary>
        /// Kiểm tra trạng thái tài khoản có đang hoạt động hay không (hỗ trợ cả HOATDONG, Hoạt động, Active,...)
        /// </summary>
        public static bool IsActiveStatus(string? trangThai)
        {
            if (string.IsNullOrWhiteSpace(trangThai)) return false;

            string s = trangThai.Trim();

            // 1. So khớp trực tiếp các từ khóa phổ biến
            if (s.Equals("Hoạt động", StringComparison.OrdinalIgnoreCase) ||
                s.Equals("HOATDONG", StringComparison.OrdinalIgnoreCase) ||
                s.Equals("HOAT_DONG", StringComparison.OrdinalIgnoreCase) ||
                s.Equals("Active", StringComparison.OrdinalIgnoreCase) ||
                s == "1")
            {
                return true;
            }

            // 2. Chuẩn hóa chuỗi (bỏ dấu tiếng Việt, bỏ khoảng trắng, dấu gạch nối, chuyển chữ hoa)
            string normalized = RemoveDiacritics(s).Replace(" ", "").Replace("_", "").Replace("-", "").ToUpperInvariant();

            if (normalized.Contains("HOATDONG") || normalized.Contains("ACTIVE") || normalized == "1")
            {
                return true;
            }

            // Nếu chứa các từ khóa khóa
            if (normalized.Contains("KHOA") || normalized.Contains("LOCK") || normalized.Contains("BLOCK") || normalized == "0")
            {
                return false;
            }

            return false;
        }

        private static string RemoveDiacritics(string text)
        {
            string normalizedString = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (char c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            return sb.ToString().Normalize(NormalizationForm.FormC).Replace("đ", "d").Replace("Đ", "D");
        }

        public HeThongDTO? Login(string username, string password, out string error)
        {
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                error = "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu!";
                return null;
            }

            var user = _dal.Authenticate(username.Trim(), password);
            if (user == null)
            {
                error = "Tên đăng nhập hoặc mật khẩu không chính xác!";
                return null;
            }

            if (!IsActiveStatus(user.TrangThai))
            {
                error = $"Tài khoản này đang ở trạng thái '{user.TrangThai}', không thể đăng nhập!";
                return null;
            }

            CurrentUser = user;
            return user;
        }

        public void SetCurrentUserByUsername(string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username)) return;

                var users = _dal.GetAll();
                var user = users.Find(u => 
                    (u.TenDangNhap != null && u.TenDangNhap.Equals(username, StringComparison.OrdinalIgnoreCase)) ||
                    (u.ID_NV != null && u.ID_NV.Equals(username, StringComparison.OrdinalIgnoreCase)) ||
                    (u.MaTaiKhoan != null && u.MaTaiKhoan.Equals(username, StringComparison.OrdinalIgnoreCase)));

                if (user != null)
                {
                    CurrentUser = user;
                }
                else
                {
                    // Tạo thông tin phiên đăng nhập dự phòng từ thông tin truyền vào
                    CurrentUser = new HeThongDTO
                    {
                        MaTaiKhoan = "AUTO_" + username,
                        TenDangNhap = username,
                        VaiTro = "Nhân viên",
                        QuyenHan = "ALL",
                        TrangThai = "Hoạt động"
                    };
                }
            }
            catch { }
        }

        /// <summary>
        /// Kiểm tra quyền hạn của tài khoản đối với phân hệ được chọn.
        /// </summary>
        public bool KiemTraQuyenPhanHe(HeThongDTO user, string tenPhanHe, out string error)
        {
            error = string.Empty;

            if (user == null)
            {
                error = "Thông tin tài khoản không hợp lệ!";
                return false;
            }

            string userQuyen = (user.QuyenHan ?? string.Empty).Trim().ToUpperInvariant();
            string vaiTro = (user.VaiTro ?? string.Empty).Trim().ToUpperInvariant();

            // 1. Quản trị viên hoặc tài khoản có quyền 'ALL' / 'ADMIN' được truy cập tất cả phân hệ
            if (userQuyen == "ALL" || userQuyen.Contains("ADMIN") || vaiTro.Contains("QUẢN TRỊ") || vaiTro.Contains("ADMIN"))
            {
                return true;
            }

            // 2. Xác định mã quyền yêu cầu cho từng phân hệ
            string phanHeKey = (tenPhanHe ?? string.Empty).Trim().ToLowerInvariant();
            string[] quyenHopLe;
            string maQuyenChuan;

            if (phanHeKey.Contains("nhân sự") || phanHeKey.Contains("nhansu") || phanHeKey.Contains("hr"))
            {
                quyenHopLe = new[] { "NHANSU", "NHAN_SU", "NS", "HR" };
                maQuyenChuan = "NHANSU";
            }
            else if (phanHeKey.Contains("sản xuất") || phanHeKey.Contains("sanxuat") || phanHeKey.Contains("manufacturing"))
            {
                quyenHopLe = new[] { "SANXUAT", "SAN_XUAT", "SX" };
                maQuyenChuan = "SANXUAT";
            }
            else if (phanHeKey.Contains("bán hàng") || phanHeKey.Contains("banhang") || phanHeKey.Contains("sales"))
            {
                quyenHopLe = new[] { "BANHANG", "BAN_HANG", "BH", "SALES" };
                maQuyenChuan = "BANHANG";
            }
            else if (phanHeKey.Contains("logistics") || phanHeKey.Contains("logistic"))
            {
                quyenHopLe = new[] { "LOGISTIC", "LOGISTICS", "LG" };
                maQuyenChuan = "LOGISTICS";
            }
            else if (phanHeKey.Contains("kho") || phanHeKey.Contains("warehouse"))
            {
                quyenHopLe = new[] { "KHO", "WAREHOUSE" };
                maQuyenChuan = "KHO";
            }
            else if (phanHeKey.Contains("tài chính") || phanHeKey.Contains("taichinh") || phanHeKey.Contains("finance"))
            {
                quyenHopLe = new[] { "TAICHINH", "TAI_CHINH", "TC", "FINANCE" };
                maQuyenChuan = "TAICHINH";
            }
            else
            {
                // Phân hệ không nằm trong danh mục kiểm soát chặt
                return true;
            }

            // 3. Tách danh sách quyền của người dùng (nếu có nhiều quyền cách nhau bởi dấu phẩy, chấm phẩy, khoảng trắng)
            var danhSachQuyenUser = userQuyen.Split(new[] { ',', ';', '|', ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string q in danhSachQuyenUser)
            {
                string qClean = q.Trim();
                foreach (string hopLe in quyenHopLe)
                {
                    if (qClean.Equals(hopLe, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            // 4. Nếu không khớp quyền hạn
            string quyenHienTai = string.IsNullOrWhiteSpace(user.QuyenHan) ? "Chưa cấp quyền" : user.QuyenHan;
            error = $"Tài khoản không có quyền truy cập vào phân hệ '{tenPhanHe}'!\n\n" +
                    $"• Quyền hạn tài khoản: [{quyenHienTai}]\n" +
                    $"• Quyền hạn yêu cầu: [{maQuyenChuan}]";
            return false;
        }

        public List<HeThongDTO> GetAll()
        {
            return _dal.GetAll();
        }

        public string GetNextId()
        {
            return _dal.GenerateNextId();
        }

        public void EnsureDefaultAdmin()
        {
            _dal.EnsureDefaultAdmin();
        }

        public bool CreateAccount(HeThongDTO ht, out string error)
        {
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(ht.ID_NV))
            {
                error = "Vui lòng chọn nhân viên sở hữu tài khoản!";
                return false;
            }

            if (!ValidationHelper.IsValidUsername(ht.TenDangNhap, out error))
            {
                return false;
            }

            if (!ValidationHelper.IsValidPassword(ht.MatKhau, out error))
            {
                return false;
            }

            if (_dal.CheckUsernameExists(ht.TenDangNhap))
            {
                error = $"Tên đăng nhập '{ht.TenDangNhap}' đã có người sử dụng, vui lòng chọn tên khác!";
                return false;
            }

            try
            {
                return _dal.Insert(ht);
            }
            catch (Exception ex)
            {
                error = "Lỗi tạo tài khoản (có thể tên đăng nhập đã trùng): " + ex.Message;
                return false;
            }
        }

        public bool Update(HeThongDTO ht, out string error)
        {
            error = string.Empty;
            if (ht == null || string.IsNullOrWhiteSpace(ht.MaTaiKhoan))
            {
                error = "Thông tin tài khoản không hợp lệ!";
                return false;
            }

            if (!ValidationHelper.IsValidUsername(ht.TenDangNhap, out error))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(ht.MatKhau) && !ValidationHelper.IsValidPassword(ht.MatKhau, out error))
            {
                return false;
            }

            // Kiểm tra trùng tên đăng nhập với tài khoản khác
            var all = _dal.GetAll();
            if (all.Any(a => a.MaTaiKhoan != ht.MaTaiKhoan && a.TenDangNhap.Equals(ht.TenDangNhap, StringComparison.OrdinalIgnoreCase)))
            {
                error = $"Tên đăng nhập '{ht.TenDangNhap}' đã được sử dụng bởi tài khoản khác!";
                return false;
            }

            try
            {
                return _dal.Update(ht);
            }
            catch (Exception ex)
            {
                error = "Lỗi cập nhật tài khoản: " + ex.Message;
                return false;
            }
        }

        public bool Delete(string maTaiKhoan, out string error)
        {
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(maTaiKhoan))
            {
                error = "Vui lòng chọn tài khoản cần xóa!";
                return false;
            }

            var all = _dal.GetAll();
            var target = all.FirstOrDefault(a => a.MaTaiKhoan == maTaiKhoan);
            if (target != null && target.TenDangNhap.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                error = "Không thể xóa tài khoản Quản trị viên (admin) mặc định của hệ thống!";
                return false;
            }

            try
            {
                return _dal.Delete(maTaiKhoan);
            }
            catch (Exception ex)
            {
                error = "Lỗi khi xóa tài khoản: " + ex.Message;
                return false;
            }
        }

        public bool ResetPassword(string maTaiKhoan, string newPassword, out string error)
        {
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(newPassword))
            {
                error = "Mật khẩu mới không được để trống!";
                return false;
            }
            return _dal.UpdatePassword(maTaiKhoan, newPassword);
        }

        public bool ToggleStatus(string maTaiKhoan, string currentStatus)
        {
            string newStatus = IsActiveStatus(currentStatus) ? "Bị khóa" : "Hoạt động";
            return _dal.ToggleStatus(maTaiKhoan, newStatus);
        }
    }
}
