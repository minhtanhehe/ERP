using System;
using System.Windows.Forms;
using OfficeOpenXml;

namespace ERP_BanHang
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // Thiết lập LicenseContext chuẩn, tương thích với hầu hết các bản EPPlus
            try { ExcelPackage.License.SetNonCommercialPersonal("Acecook ERP"); } catch { }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new QlyDonHang());
        }
    }
}
