using System;
using System.Collections.Generic;
using System.Data;
using HR_Management.DAL;
using HR_Management.DTO;

namespace HR_Management.BLL
{
    public class ThongKeBaoCaoBLL
    {
        private readonly ThongKeBaoCaoDAL _dal = new ThongKeBaoCaoDAL();

        public DashboardOverviewDTO GetOverview()
        {
            return _dal.GetOverview();
        }

        public CompanyPayrollSummaryDTO GetCompanyPayrollSummary()
        {
            return _dal.GetCompanyPayrollSummary();
        }

        public DataTable GetDepartmentStats()
        {
            return _dal.GetDepartmentStats();
        }

        public DataTable GetContractTypeStats()
        {
            return _dal.GetContractTypeStats();
        }

        public List<ThongKeBaoCaoDTO> GetAllReports()
        {
            return _dal.GetAllReports();
        }

        public string GetNextId()
        {
            return _dal.GenerateNextId();
        }

        public bool CreateReport(ThongKeBaoCaoDTO dto, out string error)
        {
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(dto.TenBaoCao))
            {
                error = "Tên báo cáo không được để trống!";
                return false;
            }

            if (string.IsNullOrWhiteSpace(dto.NoiDung))
            {
                error = "Nội dung báo cáo không được để trống!";
                return false;
            }

            if (string.IsNullOrWhiteSpace(dto.MaBaoCao))
            {
                dto.MaBaoCao = _dal.GenerateNextId();
            }

            try
            {
                return _dal.InsertReport(dto);
            }
            catch (Exception ex)
            {
                error = "Lỗi lưu báo cáo: " + ex.Message;
                return false;
            }
        }

        public bool UpdateReport(ThongKeBaoCaoDTO dto, out string error)
        {
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(dto.TenBaoCao))
            {
                error = "Tên báo cáo không được để trống!";
                return false;
            }

            if (string.IsNullOrWhiteSpace(dto.NoiDung))
            {
                error = "Nội dung báo cáo không được để trống!";
                return false;
            }

            if (string.IsNullOrWhiteSpace(dto.MaBaoCao))
            {
                error = "Mã báo cáo không hợp lệ!";
                return false;
            }

            try
            {
                return _dal.UpdateReport(dto);
            }
            catch (Exception ex)
            {
                error = "Lỗi cập nhật báo cáo: " + ex.Message;
                return false;
            }
        }

        public bool DeleteReport(string maBaoCao, out string error)
        {
            error = string.Empty;
            try
            {
                if (string.IsNullOrWhiteSpace(maBaoCao))
                {
                    error = "Mã báo cáo không hợp lệ!";
                    return false;
                }
                return _dal.DeleteReport(maBaoCao);
            }
            catch (Exception ex)
            {
                error = "Lỗi xóa báo cáo: " + ex.Message;
                return false;
            }
        }
    }
}
