using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using HR_Management.DAL;
using HR_Management.DTO;

namespace HR_Management.BLL
{
    public class NhanVienBLL
    {
        private readonly NhanVienDAL _dal = new NhanVienDAL();

        public List<NhanVienChiTietDTO> GetAll()
        {
            return _dal.GetAll();
        }

        public NhanVienChiTietDTO? GetById(string idNv)
        {
            if (string.IsNullOrWhiteSpace(idNv)) return null;
            return _dal.GetById(idNv);
        }

        public List<NhanVienChiTietDTO> Search(string keyword, string? maPhongBan, string? trangThai)
        {
            return _dal.Search(keyword, maPhongBan, trangThai);
        }

        public string GetNextId()
        {
            return _dal.GenerateNextId();
        }

        public bool Save(NhanVienChiTietDTO dto, bool isNew, out string error)
        {
            error = string.Empty;

            // 1. Kiểm tra Mã nhân viên
            if (string.IsNullOrWhiteSpace(dto.ID_NV))
            {
                error = "Mã nhân viên không được để trống!";
                return false;
            }

            string idNv = dto.ID_NV.Trim();
            if (isNew && _dal.CheckIdExists(idNv))
            {
                error = $"Mã nhân viên '{idNv}' đã tồn tại trong hệ thống!";
                return false;
            }

            // 2. Kiểm tra Họ và tên (đúng định dạng chữ, tối thiểu 2 từ)
            if (!ValidationHelper.IsValidFullName(dto.TenNV, out error))
            {
                return false;
            }

            // 3. Kiểm tra Phòng ban
            if (string.IsNullOrWhiteSpace(dto.MaPhongBan))
            {
                error = "Vui lòng chọn phòng ban trực thuộc cho nhân viên!";
                return false;
            }

            // 4. Kiểm tra Chức vụ
            if (string.IsNullOrWhiteSpace(dto.ChucVu))
            {
                error = "Chức vụ của nhân viên không được để trống!";
                return false;
            }

            // 5. Bắt lỗi Số điện thoại (10 chữ số, mạng VN hợp lệ, không trùng)
            if (!ValidationHelper.IsValidPhoneNumber(dto.SoDienThoai, out error))
            {
                return false;
            }

            string phone = dto.SoDienThoai!.Trim();
            if (_dal.CheckPhoneExists(phone, isNew ? null : idNv))
            {
                error = $"Số điện thoại '{phone}' đã tồn tại trong hệ thống (đã đăng ký cho nhân viên khác)! Vui lòng kiểm tra lại.";
                return false;
            }

            // 6. Kiểm tra Email (đúng chuẩn RFC, đuôi hợp lệ, không trùng)
            if (!ValidationHelper.IsValidEmail(dto.Email, out error))
            {
                return false;
            }

            string email = dto.Email!.Trim();
            if (_dal.CheckEmailExists(email, isNew ? null : idNv))
            {
                error = $"Địa chỉ email '{email}' đã tồn tại trong hệ thống (thuộc nhân viên khác)!";
                return false;
            }

            // 7. Kiểm tra Ngày sinh & Độ tuổi lao động (từ đủ 18 đến 65 tuổi)
            if (!ValidationHelper.IsValidBirthDate(dto.NgaySinh, out error))
            {
                return false;
            }

            // 8. Kiểm tra Địa chỉ liên hệ
            if (string.IsNullOrWhiteSpace(dto.DiaChi))
            {
                error = "Địa chỉ liên hệ không được để trống!";
                return false;
            }

            // 9. Kiểm tra CCCD (nếu có nhập, đúng 12 số, không trùng)
            if (!string.IsNullOrWhiteSpace(dto.SoCCCD))
            {
                if (!ValidationHelper.IsValidCitizenId(dto.SoCCCD, out error))
                {
                    return false;
                }

                string cccd = dto.SoCCCD!.Trim();
                if (_dal.CheckCccdExists(cccd, isNew ? null : idNv))
                {
                    error = $"Số CCCD '{cccd}' đã tồn tại trong hệ thống (thuộc nhân viên khác)! Vui lòng kiểm tra lại.";
                    return false;
                }
            }

            // 10. Kiểm tra Lương cơ bản (> 0)
            if (!ValidationHelper.IsValidSalary(dto.LuongCoBan, out error))
            {
                return false;
            }

            if (isNew)
            {
                return _dal.InsertWithDetails(dto, out error);
            }
            else
            {
                return _dal.UpdateWithDetails(dto, out error);
            }
        }

        public bool HasRelatedTransactions(string idNv, out string details)
        {
            if (string.IsNullOrWhiteSpace(idNv))
            {
                details = string.Empty;
                return false;
            }
            return _dal.HasRelatedTransactions(idNv, out details);
        }

        public bool Deactivate(string idNv, out string error)
        {
            if (string.IsNullOrWhiteSpace(idNv))
            {
                error = "Vui lòng chọn nhân viên cần chuyển trạng thái!";
                return false;
            }
            return _dal.Deactivate(idNv, out error);
        }

        public bool Delete(string idNv, out string error)
        {
            if (string.IsNullOrWhiteSpace(idNv))
            {
                error = "Vui lòng chọn nhân viên cần xóa!";
                return false;
            }
            return _dal.Delete(idNv, out error);
        }
    }
}
