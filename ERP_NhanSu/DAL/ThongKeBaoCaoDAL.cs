using System;
using System.Collections.Generic;
using System.Data;
using HR_Management.DTO;
using System.Data.SqlClient;

namespace HR_Management.DAL
{
    public class ThongKeBaoCaoDAL
    {
        public DashboardOverviewDTO GetOverview()
        {
            DashboardOverviewDTO dto = new DashboardOverviewDTO();

            try
            {
                // 1. Tổng nhân viên (đang hoạt động / chưa nghỉ việc)
                dto.TongNhanVien = Convert.ToInt32(DatabaseHelper.ExecuteScalar(@"
                    SELECT COUNT(*) FROM NhanVien 
                    WHERE TrangThai IS NULL 
                       OR (TrangThai NOT LIKE '%nghỉ%' AND TrangThai NOT LIKE '%NGHI%')"));

                // 2. Tổng phòng ban đang hoạt động
                dto.TongPhongBan = Convert.ToInt32(DatabaseHelper.ExecuteScalar(@"
                    SELECT COUNT(*) FROM PhongBan 
                    WHERE trangThai IS NULL 
                       OR UPPER(trangThai) LIKE '%HOAT%' 
                       OR trangThai = 'Hoạt động' 
                       OR (trangThai NOT LIKE '%khóa%' AND trangThai NOT LIKE '%ngừng%')"));

                // 3. Hợp đồng hiệu lực
                dto.HopDongHieuLuc = Convert.ToInt32(DatabaseHelper.ExecuteScalar(@"
                    SELECT COUNT(*) FROM HopDong 
                    WHERE trangThai = 'Hiệu lực' 
                       OR UPPER(trangThai) LIKE '%HIEU%' 
                       OR UPPER(trangThai) LIKE '%HOAT%'"));

                // 4. Hợp đồng sắp hết hạn (<30 ngày)
                dto.HopDongSapHetHan = Convert.ToInt32(DatabaseHelper.ExecuteScalar(@"
                    SELECT COUNT(*) FROM HopDong 
                    WHERE ngayKetThuc IS NOT NULL 
                      AND ngayKetThuc >= CAST(GETDATE() AS DATE) 
                      AND ngayKetThuc <= DATEADD(day, 30, CAST(GETDATE() AS DATE))
                      AND (trangThai = 'Hiệu lực' OR UPPER(trangThai) LIKE '%HIEU%' OR UPPER(trangThai) LIKE '%HOAT%')"));
            }
            catch
            {
                // Fallback nếu có lỗi
            }
            return dto;
        }

        public CompanyPayrollSummaryDTO GetCompanyPayrollSummary()
        {
            var summary = new CompanyPayrollSummaryDTO();
            try
            {
                summary.TongPhongBan = Convert.ToInt32(DatabaseHelper.ExecuteScalar("SELECT COUNT(*) FROM PhongBan"));

                summary.TongNhanVien = Convert.ToInt32(DatabaseHelper.ExecuteScalar(@"
                    SELECT COUNT(*) FROM NhanVien 
                    WHERE TrangThai IS NULL OR (TrangThai NOT LIKE '%nghỉ%' AND TrangThai NOT LIKE '%NGHI%')"));

                string query = @"
                    SELECT COALESCE(SUM(LuongCoBan), 0) AS TongLuong, 
                           COALESCE(AVG(LuongCoBan), 0) AS LuongTB 
                    FROM NhanVien 
                    WHERE TrangThai IS NULL OR (TrangThai NOT LIKE '%nghỉ%' AND TrangThai NOT LIKE '%NGHI%')";
                DataTable dt = DatabaseHelper.ExecuteQuery(query);
                if (dt.Rows.Count > 0)
                {
                    summary.TongQuyLuong = dt.Rows[0]["TongLuong"] != DBNull.Value ? Convert.ToDecimal(dt.Rows[0]["TongLuong"]) : 0;
                    summary.LuongTrungBinh = dt.Rows[0]["LuongTB"] != DBNull.Value ? Convert.ToDecimal(dt.Rows[0]["LuongTB"]) : 0;
                }
            }
            catch
            {
                // Fallback
            }
            return summary;
        }

        public DataTable GetDepartmentStats()
        {
            string query = @"
                SELECT p.maPhongBan, 
                       p.tenPhongBan, 
                       COALESCE(p.truongPhong, N'Chưa có') AS truongPhong,
                       COUNT(nv.ID_NV) AS soNhanVien,
                       ROUND(COALESCE(AVG(nv.LuongCoBan), 0), 0) AS luongTB,
                       COALESCE(SUM(nv.LuongCoBan), 0) AS tongQuyLuong,
                       COALESCE(p.trangThai, N'Hoạt động') AS trangThai
                FROM PhongBan p
                LEFT JOIN NhanVien nv ON p.maPhongBan = nv.maPhongBan 
                     AND (nv.TrangThai IS NULL OR (nv.TrangThai NOT LIKE '%nghỉ%' AND nv.TrangThai NOT LIKE '%NGHI%'))
                GROUP BY p.maPhongBan, p.tenPhongBan, p.truongPhong, p.trangThai
                ORDER BY soNhanVien DESC, p.maPhongBan ASC";

            return DatabaseHelper.ExecuteQuery(query);
        }

        public DataTable GetContractTypeStats()
        {
            try
            {
                string query = @"
                    SELECT COALESCE(hd.loaiHopDong, N'Chưa ký HĐ') AS loaiHopDong,
                           COUNT(hd.maHopDong) AS soLuong,
                           COALESCE(SUM(hd.luongCoBan), 0) AS tongLuongThoaThuan
                    FROM HopDong hd
                    WHERE hd.trangThai = 'Hiệu lực' OR UPPER(hd.trangThai) LIKE '%HIEU%' OR UPPER(hd.trangThai) LIKE '%HOAT%'
                    GROUP BY hd.loaiHopDong
                    ORDER BY soLuong DESC";

                return DatabaseHelper.ExecuteQuery(query);
            }
            catch
            {
                return new DataTable();
            }
        }

        public List<ThongKeBaoCaoDTO> GetAllReports()
        {
            List<ThongKeBaoCaoDTO> list = new List<ThongKeBaoCaoDTO>();
            string query = @"
                SELECT bc.maBaoCao, bc.tenBaoCao, bc.loaiBaoCao, bc.ngayLap, bc.ID_NV, nv.TenNV AS NguoiLap, bc.noiDung
                FROM ThongKeBaoCao bc
                LEFT JOIN NhanVien nv ON bc.ID_NV = nv.ID_NV
                ORDER BY bc.ngayLap DESC";

            DataTable dt = DatabaseHelper.ExecuteQuery(query);
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new ThongKeBaoCaoDTO
                {
                    MaBaoCao = row["maBaoCao"]?.ToString() ?? "",
                    TenBaoCao = row["tenBaoCao"]?.ToString() ?? "",
                    LoaiBaoCao = row["loaiBaoCao"]?.ToString() ?? "",
                    NgayLap = DatabaseHelper.ToDateTime(row["ngayLap"]),
                    ID_NV = row["ID_NV"] != DBNull.Value ? row["ID_NV"].ToString() : "",
                    NguoiLap = row.Table.Columns.Contains("NguoiLap") && row["NguoiLap"] != DBNull.Value ? row["NguoiLap"].ToString() : "",
                    NoiDung = row["noiDung"] != DBNull.Value ? row["noiDung"].ToString() : ""
                });
            }
            return list;
        }

        public bool InsertReport(ThongKeBaoCaoDTO dto)
        {
            string query = @"
                INSERT INTO ThongKeBaoCao (maBaoCao, tenBaoCao, loaiBaoCao, ngayLap, ID_NV, noiDung)
                VALUES (@maBaoCao, @tenBaoCao, @loaiBaoCao, @ngayLap, @ID_NV, @noiDung)";

            SqlParameter[] param = {
                new SqlParameter("@maBaoCao", dto.MaBaoCao),
                new SqlParameter("@tenBaoCao", dto.TenBaoCao),
                new SqlParameter("@loaiBaoCao", dto.LoaiBaoCao),
                new SqlParameter("@ngayLap", dto.NgayLap),
                new SqlParameter("@ID_NV", (object?)dto.ID_NV ?? DBNull.Value),
                new SqlParameter("@noiDung", (object?)dto.NoiDung ?? DBNull.Value)
            };

            return DatabaseHelper.ExecuteNonQuery(query, param) > 0;
        }

        public bool UpdateReport(ThongKeBaoCaoDTO dto)
        {
            string query = @"
                UPDATE ThongKeBaoCao 
                SET tenBaoCao = @tenBaoCao, 
                    loaiBaoCao = @loaiBaoCao, 
                    noiDung = @noiDung
                WHERE maBaoCao = @maBaoCao";

            SqlParameter[] param = {
                new SqlParameter("@maBaoCao", dto.MaBaoCao),
                new SqlParameter("@tenBaoCao", dto.TenBaoCao),
                new SqlParameter("@loaiBaoCao", dto.LoaiBaoCao),
                new SqlParameter("@noiDung", (object?)dto.NoiDung ?? DBNull.Value)
            };

            return DatabaseHelper.ExecuteNonQuery(query, param) > 0;
        }

        public bool DeleteReport(string maBaoCao)
        {
            string query = "DELETE FROM ThongKeBaoCao WHERE maBaoCao = @maBaoCao";
            SqlParameter[] param = { new SqlParameter("@maBaoCao", maBaoCao) };
            return DatabaseHelper.ExecuteNonQuery(query, param) > 0;
        }

        public string GenerateNextId()
        {
            try
            {
                string query = "SELECT maBaoCao FROM ThongKeBaoCao WHERE maBaoCao LIKE 'BC%'";
                DataTable dt = DatabaseHelper.ExecuteQuery(query);
                int nextNumber = 1;
                foreach (DataRow row in dt.Rows)
                {
                    string idStr = row[0]?.ToString() ?? "";
                    if (idStr.Length > 2 && int.TryParse(idStr.Substring(2), out int num))
                    {
                        if (num >= nextNumber) nextNumber = num + 1;
                    }
                }
                return $"BC{nextNumber:D3}";
            }
            catch
            {
                return $"BC{DateTime.Now:fff}";
            }
        }
    }
}
