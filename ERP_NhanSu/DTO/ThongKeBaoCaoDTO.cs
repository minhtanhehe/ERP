using System;

namespace HR_Management.DTO
{
    public class ThongKeBaoCaoDTO
    {
        public string MaBaoCao { get; set; } = string.Empty;
        public string TenBaoCao { get; set; } = string.Empty;
        public string LoaiBaoCao { get; set; } = string.Empty;
        public DateTime NgayLap { get; set; } = DateTime.Today;
        public string? ID_NV { get; set; }
        public string? NguoiLap { get; set; } // Hiển thị trên UI
        public string? NoiDung { get; set; }
    }

    public class DashboardOverviewDTO
    {
        public int TongNhanVien { get; set; }
        public int TongPhongBan { get; set; }
        public int HopDongHieuLuc { get; set; }
        public int HopDongSapHetHan { get; set; }
        public int NhanVienMoiThangNay { get; set; }
    }

    public class CompanyPayrollSummaryDTO
    {
        public int TongPhongBan { get; set; }
        public int TongNhanVien { get; set; }
        public decimal TongQuyLuong { get; set; }
        public decimal LuongTrungBinh { get; set; }
    }
}
