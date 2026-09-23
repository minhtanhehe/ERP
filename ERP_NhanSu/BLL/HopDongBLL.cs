using System;
using System.Collections.Generic;
using HR_Management.DAL;
using HR_Management.DTO;

namespace HR_Management.BLL
{
    public class HopDongBLL
    {
        private readonly HopDongDAL _dal = new HopDongDAL();

        public List<HopDongDTO> GetAll()
        {
            return _dal.GetAll();
        }

        public List<HopDongDTO> GetExpiringContracts(int withinDays = 30)
        {
            return _dal.GetExpiringContracts(withinDays);
        }

        public string GetNextId()
        {
            return _dal.GenerateNextId();
        }

        public bool Save(HopDongDTO hd, bool isNew, out string error)
        {
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(hd.MaHopDong))
            {
                error = "Mã hợp đồng không được để trống!";
                return false;
            }

            if (string.IsNullOrWhiteSpace(hd.ID_NV))
            {
                error = "Vui lòng chọn nhân viên ký hợp đồng!";
                return false;
            }

            if (string.IsNullOrWhiteSpace(hd.LoaiHopDong))
            {
                error = "Vui lòng chọn loại hợp đồng lao động!";
                return false;
            }

            if (hd.NgayKetThuc.HasValue && hd.NgayKetThuc.Value <= hd.NgayBatDau)
            {
                error = "Ngày kết thúc hợp đồng phải sau ngày bắt đầu hợp đồng!";
                return false;
            }

            if (!ValidationHelper.IsValidSalary(hd.LuongCoBan, out error))
            {
                return false;
            }

            try
            {
                if (isNew)
                {
                    if (_dal.CheckExists(hd.MaHopDong))
                    {
                        error = $"Mã hợp đồng '{hd.MaHopDong}' đã tồn tại trong hệ thống!";
                        return false;
                    }
                    return _dal.Insert(hd);
                }
                else
                {
                    return _dal.Update(hd);
                }
            }
            catch (Exception ex)
            {
                error = "Lỗi lưu hợp đồng: " + ex.Message;
                return false;
            }
        }

        public bool Delete(string maHopDong, out string error)
        {
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(maHopDong))
            {
                error = "Vui lòng chọn hợp đồng cần xóa!";
                return false;
            }
            return _dal.Delete(maHopDong);
        }
    }
}
