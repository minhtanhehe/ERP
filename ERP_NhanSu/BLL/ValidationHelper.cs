using System;
using System.Text.RegularExpressions;

namespace HR_Management.BLL
{
    /// <summary>
    /// Tiện ích kiểm tra và bắt lỗi dữ liệu chuẩn cho toàn bộ phân hệ Quản lý Nhân sự (ERP_NhanSu).
    /// Hỗ trợ cả tầng Giao diện (GUI) và tầng Nghiệp vụ (BLL).
    /// </summary>
    public static class ValidationHelper
    {
        // Regex email chuẩn RFC: tài khoản@tên_miền.phần_mở_rộng (ít nhất 2 ký tự domain)
        private static readonly Regex EmailRegex = new Regex(
            @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Regex số điện thoại di động Việt Nam: 10 số, bắt đầu bằng 03, 05, 07, 08, 09
        private static readonly Regex PhoneRegex = new Regex(
            @"^(03|05|07|08|09)[0-9]{8}$",
            RegexOptions.Compiled);

        // Regex chỉ chứa chữ số
        private static readonly Regex DigitsOnlyRegex = new Regex(
            @"^[0-9]+$",
            RegexOptions.Compiled);

        // Regex tên đăng nhập hợp lệ: chữ cái không dấu, chữ số, gạch dưới, dấu chấm
        private static readonly Regex UsernameRegex = new Regex(
            @"^[a-zA-Z0-9_.]+$",
            RegexOptions.Compiled);

        // Regex họ tên: chữ cái Unicode, khoảng trắng, dấu gạch nối, dấu nháy đơn
        private static readonly Regex FullNameRegex = new Regex(
            @"^[\p{L}\s'-]+$",
            RegexOptions.Compiled);

        /// <summary>
        /// Bắt lỗi địa chỉ Email (Gmail, Email công ty, ...).
        /// </summary>
        public static bool IsValidEmail(string? email, out string error)
        {
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(email))
            {
                error = "Địa chỉ email không được để trống!";
                return false;
            }

            string trimmed = email.Trim();

            if (trimmed.Contains(" "))
            {
                error = "Địa chỉ email không được chứa khoảng trắng!";
                return false;
            }

            if (trimmed.Length > 100)
            {
                error = "Địa chỉ email quá dài (tối đa 100 ký tự)!";
                return false;
            }

            if (!EmailRegex.IsMatch(trimmed))
            {
                error = "Địa chỉ email không đúng định dạng chuẩn (ví dụ: nhanvien@gmail.com hoặc nhanvien@acecook.vn)!";
                return false;
            }

            // Kiểm tra không có 2 dấu chấm liên tiếp
            if (trimmed.Contains(".."))
            {
                error = "Địa chỉ email không hợp lệ (chứa hai dấu chấm liên tiếp)!";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Bắt lỗi số điện thoại theo chuẩn mạng viễn thông Việt Nam (10 số, đầu 03, 05, 07, 08, 09).
        /// </summary>
        public static bool IsValidPhoneNumber(string? phone, out string error)
        {
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(phone))
            {
                error = "Số điện thoại không được để trống!";
                return false;
            }

            string trimmed = phone.Trim();

            if (!DigitsOnlyRegex.IsMatch(trimmed))
            {
                error = "Số điện thoại chỉ được chứa các chữ số (0-9), không được chứa chữ cái, khoảng trắng hoặc ký tự đặc biệt!";
                return false;
            }

            if (trimmed.Length != 10)
            {
                error = $"Số điện thoại phải gồm đúng 10 chữ số (hiện tại bạn đã nhập {trimmed.Length} số)!";
                return false;
            }

            if (!PhoneRegex.IsMatch(trimmed))
            {
                error = "Số điện thoại không hợp lệ! Đầu số phải thuộc các nhà mạng di động Việt Nam hợp lệ: 03, 05, 07, 08, 09 (ví dụ: 0912345678, 0388123456).";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Bắt lỗi Họ và tên nhân viên (tối thiểu 2 từ, chỉ chứa chữ, không chứa số/ký tự lạ).
        /// </summary>
        public static bool IsValidFullName(string? name, out string error)
        {
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(name))
            {
                error = "Họ và tên nhân viên không được để trống!";
                return false;
            }

            string trimmed = name.Trim();

            if (trimmed.Length < 2)
            {
                error = "Họ và tên quá ngắn!";
                return false;
            }

            if (!FullNameRegex.IsMatch(trimmed))
            {
                error = "Họ và tên không được chứa chữ số hoặc các ký tự đặc biệt (@, #, $, %, ^, &, *, ...) !";
                return false;
            }

            string[] parts = trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                error = "Vui lòng nhập đầy đủ cả Họ và Tên của nhân viên (tối thiểu 2 từ)!";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Bắt lỗi Ngày sinh & Độ tuổi lao động (mặc định từ đủ 18 tuổi đến 65 tuổi).
        /// </summary>
        public static bool IsValidBirthDate(DateTime? birthDate, out string error, int minAge = 18, int maxAge = 65)
        {
            error = string.Empty;

            if (!birthDate.HasValue)
            {
                error = "Ngày sinh không được để trống!";
                return false;
            }

            DateTime dt = birthDate.Value.Date;
            DateTime today = DateTime.Today;

            if (dt > today)
            {
                error = "Ngày sinh không thể lớn hơn ngày hiện tại!";
                return false;
            }

            // Tính tuổi chính xác theo ngày tháng
            int age = today.Year - dt.Year;
            if (dt > today.AddYears(-age))
            {
                age--;
            }

            if (age < minAge)
            {
                error = $"Nhân viên chưa đủ tuổi lao động hợp pháp! Yêu cầu phải từ đủ {minAge} tuổi trở lên (hiện tại tính theo ngày sinh là {age} tuổi).";
                return false;
            }

            if (age > maxAge)
            {
                error = $"Độ tuổi của nhân viên ({age} tuổi) vượt quá độ tuổi lao động quy định (tối đa {maxAge} tuổi)!";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Bắt lỗi Số Căn Cước Công Dân (12 số) hoặc CMND (9 số).
        /// </summary>
        public static bool IsValidCitizenId(string? cccd, out string error, bool isRequired = false)
        {
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(cccd))
            {
                if (isRequired)
                {
                    error = "Số CCCD/CMND không được để trống!";
                    return false;
                }
                return true; // Không bắt buộc và không nhập thì hợp lệ
            }

            string trimmed = cccd.Trim();

            if (!DigitsOnlyRegex.IsMatch(trimmed))
            {
                error = "Số CCCD chỉ được chứa các chữ số (0-9), không được chứa chữ cái hoặc ký tự đặc biệt!";
                return false;
            }

            if (trimmed.Length != 12 && trimmed.Length != 9)
            {
                error = $"Số CCCD phải gồm đúng 12 chữ số (hoặc 9 chữ số đối với CMND cũ), hiện tại bạn đang nhập {trimmed.Length} số!";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Bắt lỗi mức lương cơ bản (phải lớn hơn 0).
        /// </summary>
        public static bool IsValidSalary(decimal? salary, out string error)
        {
            error = string.Empty;

            if (!salary.HasValue || salary.Value <= 0)
            {
                error = "Mức lương cơ bản phải lớn hơn 0 VNĐ!";
                return false;
            }

            if (salary.Value > 1000000000m) // 1 tỷ VNĐ
            {
                error = "Mức lương cơ bản vượt quá giới hạn cho phép!";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Bắt lỗi Tên đăng nhập tài khoản hệ thống (4-30 ký tự, chữ/số/gạch dưới/chấm).
        /// </summary>
        public static bool IsValidUsername(string? username, out string error)
        {
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(username))
            {
                error = "Tên đăng nhập không được để trống!";
                return false;
            }

            string trimmed = username.Trim();

            if (trimmed.Contains(" "))
            {
                error = "Tên đăng nhập không được chứa khoảng trắng!";
                return false;
            }

            if (trimmed.Length < 4 || trimmed.Length > 30)
            {
                error = $"Tên đăng nhập phải có độ dài từ 4 đến 30 ký tự (hiện tại: {trimmed.Length} ký tự)!";
                return false;
            }

            if (!UsernameRegex.IsMatch(trimmed))
            {
                error = "Tên đăng nhập chỉ được chứa chữ cái không dấu (a-z), chữ số (0-9), dấu gạch dưới (_) hoặc dấu chấm (.)!";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Bắt lỗi Mật khẩu tài khoản hệ thống (tối thiểu 6 ký tự).
        /// </summary>
        public static bool IsValidPassword(string? password, out string error, int minLength = 6)
        {
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(password))
            {
                error = "Mật khẩu không được để trống!";
                return false;
            }

            if (password.Length < minLength)
            {
                error = $"Mật khẩu phải có độ dài tối thiểu từ {minLength} ký tự trở lên (hiện tại: {password.Length} ký tự)!";
                return false;
            }

            return true;
        }
    }
}
