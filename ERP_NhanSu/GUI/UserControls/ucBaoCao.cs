using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using HR_Management.BLL;
using HR_Management.DTO;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using iText.IO.Font;
using PageSize = iText.Kernel.Geom.PageSize;
using TextAlignment = iText.Layout.Properties.TextAlignment;
using UnitValue = iText.Layout.Properties.UnitValue;
using PdfDocument = iText.Kernel.Pdf.PdfDocument;
using PdfWriter = iText.Kernel.Pdf.PdfWriter;
using PdfFont = iText.Kernel.Font.PdfFont;
using PdfFontFactory = iText.Kernel.Font.PdfFontFactory;
using Document = iText.Layout.Document;
using Paragraph = iText.Layout.Element.Paragraph;
using Table = iText.Layout.Element.Table;
using Cell = iText.Layout.Element.Cell;
using DeviceRgb = iText.Kernel.Colors.DeviceRgb;

namespace HR_Management.GUI.UserControls
{
    [System.ComponentModel.DesignerCategory("Code")]
    public class ucBaoCao : UserControl
    {
        private readonly ThongKeBaoCaoBLL _bllBaoCao = new ThongKeBaoCaoBLL();

        // Controls Tab 1
        private Label lblTotalDepts = null!;
        private Label lblTotalEmps = null!;
        private Label lblTotalSalary = null!;
        private Label lblAvgSalary = null!;
        private DataGridView dgvDeptStats = null!;
        private DataGridView dgvContractStats = null!;
        private Label lblInsightContent = null!;

        // Controls Tab 2
        private TabControl tabStats = null!;
        private TextBox txtSearchHistory = null!;
        private ComboBox cboFilterType = null!;
        private DataGridView dgvHistoryReports = null!;
        private Label lblDetailTitle = null!;
        private Label lblDetailMeta = null!;
        private TextBox txtDetailContent = null!;
        private Button btnExportSingle = null!;
        private Button btnDeleteSingle = null!;
        private Button btnEditSingle = null!;

        private List<ThongKeBaoCaoDTO> _allReportsCache = new List<ThongKeBaoCaoDTO>();
        private ThongKeBaoCaoDTO? _selectedReport = null;

        // ==== Spacing constants (tweak here to change global spacing) ====
        private const int CardGap = 10;           // gap between sibling cards
        private const int SectionGap = 12;         // gap between major sections
        private const int GridRowHeight = 32;
        private const int GridHeaderHeight = 38;

        public ucBaoCao()
        {
            InitializeComponent();
            LoadAllStats();
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = ThemeColor.BodyBg;

            // ===== TOP: Actions Bar =====
            FlowLayoutPanel pnlActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.White,
                Padding = new Padding(16, 14, 16, 14),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true
            };
            pnlActions.Paint += (s, e) =>
            {
                using (Pen p = new Pen(ThemeColor.CardBorder, 1))
                {
                    e.Graphics.DrawLine(p, 0, pnlActions.Height - 1, pnlActions.Width, pnlActions.Height - 1);
                }
            };

            Button btnCreateReport = UIHelper.CreateButton("+ Lập Báo Cáo Mới", ThemeColor.Primary, Color.White, 165, 40);
            btnCreateReport.Margin = new Padding(0, 0, 10, 0);
            btnCreateReport.Click += BtnCreateReport_Click;

            Button btnEditReport = UIHelper.CreateButton("✏ Sửa Báo Cáo", ThemeColor.Info, Color.White, 140, 40);
            btnEditReport.Margin = new Padding(0, 0, 10, 0);
            btnEditReport.Click += BtnEditReport_Click;

            Button btnExportExcel = UIHelper.CreateButton("📊 Xuất Báo Cáo Excel", ThemeColor.Success, Color.White, 185, 40);
            btnExportExcel.Margin = new Padding(0, 0, 10, 0);
            btnExportExcel.Click += BtnExportExcel_Click;

            Button btnRefresh = UIHelper.CreateButton("↻ Làm mới dữ liệu", Color.FromArgb(100, 116, 139), Color.White, 160, 40);
            btnRefresh.Margin = new Padding(0, 0, 0, 0);
            btnRefresh.Click += (s, e) => LoadAllStats();

            pnlActions.Controls.AddRange(new Control[] { btnCreateReport, btnEditReport, btnExportExcel, btnRefresh });

            // Container for Tabs
            Panel pnlTabContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, SectionGap, 16, 16),
                BackColor = ThemeColor.BodyBg
            };

            tabStats = new TabControl
            {
                Dock = DockStyle.Fill,
                Padding = new Point(18, 10),
                Font = ThemeColor.BodyFont
            };

            // =========================================================================
            // TAB 1: CƠ CẤU NHÂN SỰ & QUỸ LƯƠNG PHÒNG BAN
            // =========================================================================
            TabPage tabDept = new TabPage
            {
                Text = "📊 Cơ Cấu Nhân Sự & Quỹ Lương",
                BackColor = ThemeColor.BodyBg,
                Padding = new Padding(14)
            };

            // 1.1 KPI Cards Row (taller + gaps between cards so numbers don't crowd the border)
            TableLayoutPanel pnlKpiRow = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 108,
                ColumnCount = 4,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, SectionGap),
                BackColor = Color.Transparent
            };
            pnlKpiRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            pnlKpiRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            pnlKpiRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            pnlKpiRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            lblTotalDepts = new Label();
            lblTotalEmps = new Label();
            lblTotalSalary = new Label();
            lblAvgSalary = new Label();

            AddKpiCard(pnlKpiRow, "🏢 TỔNG PHÒNG BAN", lblTotalDepts, ThemeColor.Primary, "Bộ máy hoạt động", 0);
            AddKpiCard(pnlKpiRow, "👥 TỔNG NHÂN SỰ", lblTotalEmps, ThemeColor.Success, "Nhân lực toàn công ty", 1);
            AddKpiCard(pnlKpiRow, "💰 TỔNG QUỸ LƯƠNG/THÁNG", lblTotalSalary, ThemeColor.Warning, "Lương cơ bản chi trả", 2);
            AddKpiCard(pnlKpiRow, "💵 LƯƠNG BÌNH QUÂN", lblAvgSalary, ThemeColor.Info, "Mức TB / nhân viên", 3);

            // 1.2 SplitContainer for Tables & Insights
            SplitContainer splitDept = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterWidth = 12,
                Panel1MinSize = 50,
                Panel2MinSize = 50
            };
            splitDept.SizeChanged += (s, e) => AdjustDeptSplitter(splitDept);

            // Top: Department Table Card
            Panel pnlDeptCard = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(16) };
            pnlDeptCard.Paint += (s, e) =>
            {
                using (Pen p = new Pen(ThemeColor.CardBorder, 1))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, pnlDeptCard.Width - 1, pnlDeptCard.Height - 1);
                }
            };

            Label lblDeptTitle = new Label
            {
                Text = "🏢 CHI TIẾT CƠ CẤU NHÂN SỰ VÀ QUỸ LƯƠNG THEO TỪNG PHÒNG BAN",
                Font = ThemeColor.SubHeaderFont,
                ForeColor = ThemeColor.TextPrimary,
                Dock = DockStyle.Top,
                Height = 36,
                Margin = new Padding(0, 0, 0, 8),
                AutoSize = false
            };

            dgvDeptStats = new DataGridView { Dock = DockStyle.Fill };
            UIHelper.StyleDataGridView(dgvDeptStats);
            dgvDeptStats.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvDeptStats.ColumnHeadersHeight = GridHeaderHeight;
            dgvDeptStats.RowTemplate.Height = GridRowHeight;
            dgvDeptStats.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
            dgvDeptStats.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

            dgvDeptStats.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Mã PB", DataPropertyName = "maPhongBan", FillWeight = 9, MinimumWidth = 70 });
            dgvDeptStats.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tên Phòng Ban", DataPropertyName = "tenPhongBan", FillWeight = 24, MinimumWidth = 140 });
            dgvDeptStats.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Trưởng Phòng", DataPropertyName = "truongPhong", FillWeight = 19, MinimumWidth = 120 });
            dgvDeptStats.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Số NV",
                DataPropertyName = "soNhanVien",
                FillWeight = 10,
                MinimumWidth = 80,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "#,##0" }
            });
            dgvDeptStats.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Lương TB",
                DataPropertyName = "luongTB",
                FillWeight = 17,
                MinimumWidth = 120,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "#,##0 VNĐ" }
            });
            dgvDeptStats.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Tổng Quỹ Lương",
                DataPropertyName = "tongQuyLuong",
                FillWeight = 21,
                MinimumWidth = 150,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "#,##0 VNĐ" }
            });
            dgvDeptStats.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Trạng Thái", DataPropertyName = "trangThai", FillWeight = 14, MinimumWidth = 100 });

            pnlDeptCard.Controls.Add(dgvDeptStats);
            pnlDeptCard.Controls.Add(lblDeptTitle);
            splitDept.Panel1.Controls.Add(pnlDeptCard);

            // Bottom: 2 Columns (Contract distribution + Insights)
            TableLayoutPanel tlpBottom = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            tlpBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            tlpBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));

            // Left Bottom: Contract Stats Card
            Panel pnlContractCard = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(16), Margin = new Padding(0, 0, CardGap / 2, 0) };
            pnlContractCard.Paint += (s, e) =>
            {
                using (Pen p = new Pen(ThemeColor.CardBorder, 1))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, pnlContractCard.Width - 1, pnlContractCard.Height - 1);
                }
            };
            Label lblContractTitle = new Label
            {
                Text = "📑 PHÂN BỔ THEO LOẠI HỢP ĐỒNG LAO ĐỘNG",
                Font = ThemeColor.SubHeaderFont,
                ForeColor = ThemeColor.TextPrimary,
                Dock = DockStyle.Top,
                Height = 36,
                Margin = new Padding(0, 0, 0, 8),
                AutoSize = false
            };
            dgvContractStats = new DataGridView { Dock = DockStyle.Fill };
            UIHelper.StyleDataGridView(dgvContractStats);
            dgvContractStats.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvContractStats.ColumnHeadersHeight = GridHeaderHeight;
            dgvContractStats.RowTemplate.Height = GridRowHeight;
            dgvContractStats.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
            dgvContractStats.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Loại Hợp Đồng", DataPropertyName = "loaiHopDong", FillWeight = 44, MinimumWidth = 140 });
            dgvContractStats.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Số Lượng",
                DataPropertyName = "soLuong",
                FillWeight = 22,
                MinimumWidth = 90,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "#,##0" }
            });
            dgvContractStats.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Tổng Lương HĐ",
                DataPropertyName = "tongLuongThoaThuan",
                FillWeight = 34,
                MinimumWidth = 140,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "#,##0 VNĐ" }
            });

            pnlContractCard.Controls.Add(dgvContractStats);
            pnlContractCard.Controls.Add(lblContractTitle);
            tlpBottom.Controls.Add(pnlContractCard, 0, 0);

            // Right Bottom: Management Insight Card
            Panel pnlInsightCard = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(18), Margin = new Padding(CardGap / 2, 0, 0, 0) };
            pnlInsightCard.Paint += (s, e) =>
            {
                using (Pen p = new Pen(ThemeColor.CardBorder, 1))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, pnlInsightCard.Width - 1, pnlInsightCard.Height - 1);
                }
                using (Brush b = new SolidBrush(ThemeColor.Primary))
                {
                    e.Graphics.FillRectangle(b, 0, 0, 4, pnlInsightCard.Height);
                }
            };
            Label lblInsightTitle = new Label
            {
                Text = "💡 ĐÁNH GIÁ & GỢI Ý QUẢN TRỊ NHÂN LỰC",
                Font = ThemeColor.SubHeaderFont,
                ForeColor = ThemeColor.Primary,
                Dock = DockStyle.Top,
                Height = 36,
                Margin = new Padding(0, 0, 0, 8),
                AutoSize = false
            };
            lblInsightContent = new Label
            {
                Dock = DockStyle.Fill,
                Font = ThemeColor.BodyFont,
                ForeColor = ThemeColor.TextPrimary,
                Text = "Đang tổng hợp phân tích...",
                TextAlign = ContentAlignment.TopLeft,
                Padding = new Padding(6, 4, 4, 4),
                AutoEllipsis = false
            };
            pnlInsightCard.Controls.Add(lblInsightContent);
            pnlInsightCard.Controls.Add(lblInsightTitle);
            tlpBottom.Controls.Add(pnlInsightCard, 1, 0);

            splitDept.Panel2.Controls.Add(tlpBottom);

            // Add KPI and Split to Tab 1
            Panel pnlTab1Content = new Panel { Dock = DockStyle.Fill };
            pnlTab1Content.Controls.Add(splitDept);
            pnlTab1Content.Controls.Add(pnlKpiRow);
            tabDept.Controls.Add(pnlTab1Content);

            // =========================================================================
            // TAB 2: NHẬT KÝ BÁO CÁO & LƯU TRỮ
            // =========================================================================
            TabPage tabHistory = new TabPage
            {
                Text = "📋 Nhật Ký Báo Cáo & Lưu Trữ",
                BackColor = ThemeColor.BodyBg,
                Padding = new Padding(14)
            };

            // Filter Toolbar on Tab 2
            Panel pnlFilter = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.White,
                Padding = new Padding(16, 12, 16, 12),
                Margin = new Padding(0, 0, 0, SectionGap)
            };
            pnlFilter.Paint += (s, e) =>
            {
                using (Pen p = new Pen(ThemeColor.CardBorder, 1))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, pnlFilter.Width - 1, pnlFilter.Height - 1);
                }
            };

            FlowLayoutPanel flpFilters = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true
            };

            Label lblSearch = new Label { Text = "🔍 Tìm kiếm:", AutoSize = true, Margin = new Padding(0, 9, 8, 0), Font = ThemeColor.BodyFontBold };
            txtSearchHistory = new TextBox { Width = 240, Margin = new Padding(0, 5, 20, 0), Font = ThemeColor.BodyFont };
            txtSearchHistory.TextChanged += (s, e) => ApplyHistoryFilter();

            Label lblType = new Label { Text = "Loại báo cáo:", AutoSize = true, Margin = new Padding(0, 9, 8, 0), Font = ThemeColor.BodyFontBold };
            cboFilterType = new ComboBox { Width = 190, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 5, 20, 0), Font = ThemeColor.BodyFont };
            cboFilterType.Items.AddRange(new object[] { "Tất cả", "Cơ cấu phòng ban", "Biến động nhân sự", "Thống kê quỹ lương", "Hợp đồng lao động", "Khác" });
            cboFilterType.SelectedIndex = 0;
            cboFilterType.SelectedIndexChanged += (s, e) => ApplyHistoryFilter();

            Button btnClearFilter = UIHelper.CreateButton("✕ Bỏ lọc", Color.FromArgb(241, 245, 249), ThemeColor.TextSecondary, 90, 34);
            btnClearFilter.Margin = new Padding(0, 3, 0, 0);
            btnClearFilter.Click += (s, e) =>
            {
                txtSearchHistory.Text = string.Empty;
                cboFilterType.SelectedIndex = 0;
            };

            flpFilters.Controls.AddRange(new Control[] { lblSearch, txtSearchHistory, lblType, cboFilterType, btnClearFilter });
            pnlFilter.Controls.Add(flpFilters);

            // SplitContainer for Master-Detail Report view
            SplitContainer splitHistory = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterWidth = 12,
                Panel1MinSize = 50,
                Panel2MinSize = 50
            };
            splitHistory.SizeChanged += (s, e) => AdjustHistorySplitter(splitHistory);

            // Master List Card (Top)
            Panel pnlListCard = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(16) };
            pnlListCard.Paint += (s, e) =>
            {
                using (Pen p = new Pen(ThemeColor.CardBorder, 1))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, pnlListCard.Width - 1, pnlListCard.Height - 1);
                }
            };
            Label lblHistoryListTitle = new Label
            {
                Text = "📑 DANH SÁCH BÁO CÁO ĐÃ LƯU TRONG HỆ THỐNG",
                Font = ThemeColor.SubHeaderFont,
                ForeColor = ThemeColor.TextPrimary,
                Dock = DockStyle.Top,
                Height = 36,
                Margin = new Padding(0, 0, 0, 8),
                AutoSize = false
            };

            dgvHistoryReports = new DataGridView { Dock = DockStyle.Fill };
            UIHelper.StyleDataGridView(dgvHistoryReports);
            dgvHistoryReports.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvHistoryReports.ColumnHeadersHeight = GridHeaderHeight;
            dgvHistoryReports.RowTemplate.Height = GridRowHeight;
            dgvHistoryReports.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
            dgvHistoryReports.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Mã BC", DataPropertyName = "MaBaoCao", FillWeight = 9, MinimumWidth = 70 });
            dgvHistoryReports.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tên Báo Cáo", DataPropertyName = "TenBaoCao", FillWeight = 25, MinimumWidth = 150 });
            dgvHistoryReports.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Loại Báo Cáo", DataPropertyName = "LoaiBaoCao", FillWeight = 17, MinimumWidth = 130 });
            dgvHistoryReports.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Ngày Lập",
                DataPropertyName = "NgayLap",
                FillWeight = 12,
                MinimumWidth = 100,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy" }
            });
            dgvHistoryReports.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Người Lập", DataPropertyName = "NguoiLap", FillWeight = 15, MinimumWidth = 120 });
            dgvHistoryReports.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tóm Tắt Nội Dung", DataPropertyName = "NoiDung", FillWeight = 22, MinimumWidth = 160 });

            dgvHistoryReports.SelectionChanged += DgvHistoryReports_SelectionChanged;
            dgvHistoryReports.DoubleClick += (s, e) => BtnEditReport_Click(s, e);

            pnlListCard.Controls.Add(dgvHistoryReports);
            pnlListCard.Controls.Add(lblHistoryListTitle);
            splitHistory.Panel1.Controls.Add(pnlListCard);

            // Detail Card (Bottom) — header rebuilt with a TableLayoutPanel so the
            // title, meta line and action buttons each get their own row/cell and
            // never overlap regardless of title length or font size.
            Panel pnlDetailCard = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(18) };
            pnlDetailCard.Paint += (s, e) =>
            {
                using (Pen p = new Pen(ThemeColor.CardBorder, 1))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, pnlDetailCard.Width - 1, pnlDetailCard.Height - 1);
                }
            };

            TableLayoutPanel tlpDetailHeader = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 72,
                ColumnCount = 2,
                RowCount = 2,
                Margin = new Padding(0, 0, 0, 10),
                AutoSize = false
            };
            tlpDetailHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpDetailHeader.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tlpDetailHeader.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            tlpDetailHeader.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));

            lblDetailTitle = new Label
            {
                Text = "XEM CHI TIẾT NỘI DUNG BÁO CÁO",
                Font = ThemeColor.SubHeaderFont,
                ForeColor = ThemeColor.Primary,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };

            lblDetailMeta = new Label
            {
                Text = "Vui lòng chọn một báo cáo từ danh sách trên để xem chi tiết.",
                Font = ThemeColor.SmallFont,
                ForeColor = ThemeColor.TextSecondary,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };

            FlowLayoutPanel flpDetailButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            btnDeleteSingle = UIHelper.CreateButton("🗑 Xóa Báo Cáo", ThemeColor.Danger, Color.White, 135, 36);
            btnDeleteSingle.Margin = new Padding(8, 3, 0, 3);
            btnDeleteSingle.Click += BtnDeleteSingle_Click;

            btnEditSingle = UIHelper.CreateButton("✏ Sửa Báo Cáo", ThemeColor.Info, Color.White, 135, 36);
            btnEditSingle.Margin = new Padding(8, 3, 0, 3);
            btnEditSingle.Click += BtnEditReport_Click;

            btnExportSingle = UIHelper.CreateButton("🗎 Xuất Báo Cáo (PDF)", ThemeColor.Primary, Color.White, 185, 36);
            btnExportSingle.Margin = new Padding(0, 3, 0, 3);
            btnExportSingle.Click += BtnExportPdf_Click;

            flpDetailButtons.Controls.AddRange(new Control[] { btnDeleteSingle, btnEditSingle, btnExportSingle });

            tlpDetailHeader.Controls.Add(lblDetailTitle, 0, 0);
            tlpDetailHeader.Controls.Add(flpDetailButtons, 1, 0);
            tlpDetailHeader.Controls.Add(lblDetailMeta, 0, 1);
            tlpDetailHeader.SetColumnSpan(lblDetailMeta, 2);

            txtDetailContent = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                BackColor = Color.FromArgb(248, 250, 252),
                ScrollBars = ScrollBars.Vertical,
                Font = ThemeColor.BodyFont,
                Margin = new Padding(0, 10, 0, 0)
            };

            pnlDetailCard.Controls.Add(txtDetailContent);
            pnlDetailCard.Controls.Add(tlpDetailHeader);
            splitHistory.Panel2.Controls.Add(pnlDetailCard);

            Panel pnlTab2Content = new Panel { Dock = DockStyle.Fill };
            pnlTab2Content.Controls.Add(splitHistory);
            pnlTab2Content.Controls.Add(pnlFilter);
            tabHistory.Controls.Add(pnlTab2Content);

            // Add TabPages to TabControl
            tabStats.TabPages.Add(tabDept);
            tabStats.TabPages.Add(tabHistory);

            pnlTabContainer.Controls.Add(tabStats);

            this.Controls.Add(pnlTabContainer);
            this.Controls.Add(pnlActions);

            this.Load += (s, e) =>
            {
                AdjustDeptSplitter(splitDept);
                AdjustHistorySplitter(splitHistory);
            };
        }

        /// <summary>
        /// Adds a KPI card into a TableLayoutPanel cell with a consistent gap
        /// to its neighbours, so cards never touch each other or the row edges.
        /// </summary>
        private void AddKpiCard(TableLayoutPanel row, string title, Label valueLabel, Color accent, string subtitle, int column)
        {
            var card = UIHelper.CreateKpiCard(title, valueLabel, accent, subtitle);
            card.Margin = new Padding(
                column == 0 ? 0 : CardGap / 2,
                0,
                column == row.ColumnCount - 1 ? 0 : CardGap / 2,
                0);
            row.Controls.Add(card, column, 0);
        }

        private void AdjustDeptSplitter(SplitContainer split)
        {
            try
            {
                if (split != null && split.Height > 220)
                {
                    int target = (int)(split.Height * 0.58);
                    int min = Math.Max(50, split.Panel1MinSize);
                    int max = split.Height - Math.Max(50, split.Panel2MinSize);
                    if (target >= min && target <= max)
                    {
                        split.SplitterDistance = target;
                    }
                }
            }
            catch
            {
                // Tránh lỗi SplitterDistance khi WinForms chưa kịp render
            }
        }

        private void AdjustHistorySplitter(SplitContainer split)
        {
            try
            {
                if (split != null && split.Height > 220)
                {
                    int target = (int)(split.Height * 0.48);
                    int min = Math.Max(50, split.Panel1MinSize);
                    int max = split.Height - Math.Max(50, split.Panel2MinSize);
                    if (target >= min && target <= max)
                    {
                        split.SplitterDistance = target;
                    }
                }
            }
            catch
            {
                // Tránh lỗi SplitterDistance khi WinForms chưa kịp render
            }
        }

        public void LoadAllStats()
        {
            var summary = new CompanyPayrollSummaryDTO();
            DataTable dtDept = new DataTable();
            DataTable dtContract = new DataTable();

            try
            {
                // 1. KPI Summary
                summary = _bllBaoCao.GetCompanyPayrollSummary();
                lblTotalDepts.Text = summary.TongPhongBan.ToString("N0");
                lblTotalEmps.Text = summary.TongNhanVien.ToString("N0");
                lblTotalSalary.Text = summary.TongQuyLuong.ToString("#,##0") + " đ";
                lblAvgSalary.Text = summary.LuongTrungBinh.ToString("#,##0") + " đ";
            }
            catch { }

            try
            {
                // 2. Department Breakdown
                dtDept = _bllBaoCao.GetDepartmentStats();
                dgvDeptStats.DataSource = dtDept;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi lấy thống kê phòng ban: " + ex.Message, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            try
            {
                // 3. Contract Breakdown
                dtContract = _bllBaoCao.GetContractTypeStats();
                dgvContractStats.DataSource = dtContract;
            }
            catch { }

            try
            {
                // 4. Update Insights
                UpdateInsights(summary, dtDept, dtContract);
            }
            catch { }

            try
            {
                // 5. Report History
                _allReportsCache = _bllBaoCao.GetAllReports();
                ApplyHistoryFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi lấy nhật ký báo cáo: " + ex.Message, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void UpdateInsights(CompanyPayrollSummaryDTO summary, DataTable dtDept, DataTable dtContract)
        {
            // Blank line between bullets gives the label breathing room —
            // without it the lines visually run into one another.
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("• Quy mô & Tổ chức: Doanh nghiệp hiện có " + summary.TongPhongBan + " phòng ban với " + summary.TongNhanVien + " nhân sự đang công tác.");
            sb.AppendLine();

            if (dtDept.Rows.Count > 0)
            {
                string topDeptName = dtDept.Rows[0]["tenPhongBan"]?.ToString() ?? "";
                string topDeptCount = dtDept.Rows[0]["soNhanVien"]?.ToString() ?? "0";
                sb.AppendLine("• Đơn vị đông nhân sự nhất: " + topDeptName + " (" + topDeptCount + " nhân viên).");
                sb.AppendLine();
            }

            sb.AppendLine("• Quỹ lương: Tổng chi phí lương cơ bản ước tính " + summary.TongQuyLuong.ToString("#,##0") + " VNĐ/tháng (bình quân " + summary.LuongTrungBinh.ToString("#,##0") + " VNĐ/người).");
            sb.AppendLine();

            if (dtContract.Rows.Count > 0)
            {
                string topContract = dtContract.Rows[0]["loaiHopDong"]?.ToString() ?? "";
                string topContractCount = dtContract.Rows[0]["soLuong"]?.ToString() ?? "0";
                sb.AppendLine("• Cơ cấu hợp đồng: Đa số nhân sự theo diện \"" + topContract + "\" (" + topContractCount + " hợp đồng).");
                sb.AppendLine();
            }
            sb.AppendLine("• Khuyến nghị: Duy trì tỷ lệ hợp đồng chính thức ổn định trên 70% và rà soát các hợp đồng sắp hết hạn định kỳ.");

            lblInsightContent.Text = sb.ToString();
        }

        private void ApplyHistoryFilter()
        {
            string keyword = txtSearchHistory.Text.Trim().ToLower();
            string selectedType = cboFilterType.SelectedItem?.ToString() ?? "Tất cả";

            var filtered = _allReportsCache.Where(r =>
            {
                bool matchKeyword = string.IsNullOrEmpty(keyword) ||
                    (r.MaBaoCao ?? "").ToLower().Contains(keyword) ||
                    (r.TenBaoCao ?? "").ToLower().Contains(keyword) ||
                    (r.NguoiLap ?? "").ToLower().Contains(keyword) ||
                    (r.NoiDung ?? "").ToLower().Contains(keyword);

                bool matchType = selectedType == "Tất cả" || (r.LoaiBaoCao ?? "").Equals(selectedType, StringComparison.OrdinalIgnoreCase);

                return matchKeyword && matchType;
            }).ToList();

            dgvHistoryReports.DataSource = filtered;

            if (filtered.Count > 0)
            {
                DisplayReportDetail(filtered[0]);
            }
            else
            {
                DisplayReportDetail(null);
            }
        }

        private void DgvHistoryReports_SelectionChanged(object? sender, EventArgs e)
        {
            if (dgvHistoryReports.SelectedRows.Count > 0)
            {
                var selected = dgvHistoryReports.SelectedRows[0].DataBoundItem as ThongKeBaoCaoDTO;
                DisplayReportDetail(selected);
            }
        }

        private void DisplayReportDetail(ThongKeBaoCaoDTO? report)
        {
            _selectedReport = report;
            if (report == null)
            {
                lblDetailTitle.Text = "XEM CHI TIẾT NỘI DUNG BÁO CÁO";
                lblDetailMeta.Text = "Không có báo cáo nào được chọn.";
                txtDetailContent.Text = string.Empty;
                btnExportSingle.Enabled = false;
                btnDeleteSingle.Enabled = false;
                btnEditSingle.Enabled = false;
                return;
            }

            btnExportSingle.Enabled = true;
            btnDeleteSingle.Enabled = true;
            btnEditSingle.Enabled = true;

            lblDetailTitle.Text = (report.TenBaoCao ?? "").ToUpper();
            lblDetailMeta.Text = $"Mã BC: {report.MaBaoCao}   |   Loại: {report.LoaiBaoCao}   |   Ngày lập: {report.NgayLap:dd/MM/yyyy}   |   Người lập: {report.NguoiLap ?? "N/A"}";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("══════════════════════════════════════════════════════════════════");
            sb.AppendLine($"   THÔNG TIN BÁO CÁO: {report.TenBaoCao}");
            sb.AppendLine("══════════════════════════════════════════════════════════════════");
            sb.AppendLine($"• Mã định danh:   {report.MaBaoCao}");
            sb.AppendLine($"• Phân loại:      {report.LoaiBaoCao}");
            sb.AppendLine($"• Ngày tạo:       {report.NgayLap:dd/MM/yyyy}");
            sb.AppendLine($"• Người lập:      {report.NguoiLap ?? "N/A"} (ID: {report.ID_NV ?? "N/A"})");
            sb.AppendLine();
            sb.AppendLine("--- NỘI DUNG & KẾT QUẢ GHI NHẬN ---");
            sb.AppendLine(string.IsNullOrWhiteSpace(report.NoiDung) ? "(Không có nội dung ghi chú bổ sung)" : report.NoiDung);

            txtDetailContent.Text = sb.ToString();
        }

        private void BtnDeleteSingle_Click(object? sender, EventArgs e)
        {
            if (_selectedReport == null) return;

            var dr = MessageBox.Show(
                $"Bạn có chắc chắn muốn xóa báo cáo [{_selectedReport.MaBaoCao}] \"{_selectedReport.TenBaoCao}\" khỏi hệ thống?",
                "Xác nhận xóa báo cáo",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (dr == DialogResult.Yes)
            {
                if (_bllBaoCao.DeleteReport(_selectedReport.MaBaoCao, out string err))
                {
                    MessageBox.Show("Xóa báo cáo thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadAllStats();
                }
                else
                {
                    MessageBox.Show("Xóa báo cáo thất bại: " + err, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnExportPdf_Click(object? sender, EventArgs e)
        {
            if (_selectedReport == null)
            {
                MessageBox.Show("Vui lòng chọn báo cáo cần xuất PDF!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "PDF Files (*.pdf)|*.pdf";
                sfd.FileName = $"{_selectedReport.MaBaoCao}_{DateTime.Now:yyyyMMdd_HHmm}.pdf";
                sfd.Title = "Chọn nơi lưu file Báo Cáo PDF";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        using (var writer = new PdfWriter(sfd.FileName))
                        using (var pdf = new PdfDocument(writer))
                        using (var doc = new Document(pdf, PageSize.A4))
                        {
                            doc.SetMargins(36, 36, 36, 36);

                            // Load Fonts
                            string fontDir = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
                            string arialPath = System.IO.Path.Combine(fontDir, "arial.ttf");
                            string arialBdPath = System.IO.Path.Combine(fontDir, "arialbd.ttf");
                            string arialItPath = System.IO.Path.Combine(fontDir, "ariali.ttf");

                            PdfFont fontRegular = File.Exists(arialPath) ? PdfFontFactory.CreateFont(arialPath, PdfEncodings.IDENTITY_H) : PdfFontFactory.CreateFont();
                            PdfFont fontBold = File.Exists(arialBdPath) ? PdfFontFactory.CreateFont(arialBdPath, PdfEncodings.IDENTITY_H) : fontRegular;
                            PdfFont fontItalic = File.Exists(arialItPath) ? PdfFontFactory.CreateFont(arialItPath, PdfEncodings.IDENTITY_H) : fontRegular;

                            // 1. Header Banner
                            Table headerTable = new Table(UnitValue.CreatePercentArray(new float[] { 60, 40 })).UseAllAvailableWidth();
                            headerTable.SetMarginBottom(15);

                            Cell cellLeft = new Cell().Add(new Paragraph("CÔNG TY CỔ PHẦN ACECOOK VIỆT NAM\nPHÂN HỆ QUẢN TRỊ NHÂN SỰ ERP")
                                .SetFont(fontBold).SetFontSize(10).SetFontColor(new DeviceRgb(100, 116, 139)))
                                .SetBorder(iText.Layout.Borders.Border.NO_BORDER);

                            Cell cellRight = new Cell().Add(new Paragraph("Mẫu số: BC-NS-01\nNgày in: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"))
                                .SetFont(fontItalic).SetFontSize(9).SetFontColor(new DeviceRgb(148, 163, 184)).SetTextAlignment(TextAlignment.RIGHT))
                                .SetBorder(iText.Layout.Borders.Border.NO_BORDER);

                            headerTable.AddCell(cellLeft);
                            headerTable.AddCell(cellRight);
                            doc.Add(headerTable);

                            // 2. Title
                            Paragraph pTitle = new Paragraph((_selectedReport.TenBaoCao ?? "BÁO CÁO THỐNG KÊ").ToUpper())
                                .SetFont(fontBold)
                                .SetFontSize(16)
                                .SetFontColor(new DeviceRgb(2, 132, 199))
                                .SetTextAlignment(TextAlignment.CENTER)
                                .SetMarginBottom(4);
                            doc.Add(pTitle);

                            Paragraph pSub = new Paragraph($"Mã định danh báo cáo: {_selectedReport.MaBaoCao}")
                                .SetFont(fontItalic)
                                .SetFontSize(10)
                                .SetFontColor(new DeviceRgb(100, 116, 139))
                                .SetTextAlignment(TextAlignment.CENTER)
                                .SetMarginBottom(20);
                            doc.Add(pSub);

                            // 3. Metadata Table
                            Table metaTable = new Table(UnitValue.CreatePercentArray(new float[] { 22, 28, 22, 28 })).UseAllAvailableWidth();
                            metaTable.SetMarginBottom(20);

                            void AddMetaCell(string label, string val)
                            {
                                Cell cLbl = new Cell().Add(new Paragraph(label).SetFont(fontBold).SetFontSize(9).SetFontColor(new DeviceRgb(51, 65, 85)))
                                    .SetBackgroundColor(new DeviceRgb(241, 245, 249))
                                    .SetBorder(new iText.Layout.Borders.SolidBorder(new DeviceRgb(203, 213, 225), 1))
                                    .SetPadding(6);
                                Cell cVal = new Cell().Add(new Paragraph(val).SetFont(fontRegular).SetFontSize(9).SetFontColor(new DeviceRgb(30, 41, 59)))
                                    .SetBorder(new iText.Layout.Borders.SolidBorder(new DeviceRgb(203, 213, 225), 1))
                                    .SetPadding(6);
                                metaTable.AddCell(cLbl);
                                metaTable.AddCell(cVal);
                            }

                            AddMetaCell("Mã Báo Cáo:", _selectedReport.MaBaoCao ?? "N/A");
                            AddMetaCell("Loại Báo Cáo:", _selectedReport.LoaiBaoCao ?? "N/A");
                            AddMetaCell("Ngày Lập:", _selectedReport.NgayLap.ToString("dd/MM/yyyy"));
                            AddMetaCell("Người Lập:", $"{_selectedReport.NguoiLap ?? "N/A"} ({_selectedReport.ID_NV ?? "N/A"})");

                            doc.Add(metaTable);

                            // 4. Content Section
                            Paragraph pSectionTitle = new Paragraph("NỘI DUNG VÀ KẾT QUẢ GHI NHẬN")
                                .SetFont(fontBold)
                                .SetFontSize(12)
                                .SetFontColor(new DeviceRgb(30, 41, 59))
                                .SetMarginBottom(8);
                            doc.Add(pSectionTitle);

                            string content = string.IsNullOrWhiteSpace(_selectedReport.NoiDung)
                                ? "(Báo cáo không có nội dung ghi chú bổ sung.)"
                                : _selectedReport.NoiDung;

                            Table contentBox = new Table(1).UseAllAvailableWidth();
                            Cell contentCell = new Cell().Add(new Paragraph(content)
                                .SetFont(fontRegular)
                                .SetFontSize(10)
                                .SetMultipliedLeading(1.4f))
                                .SetBackgroundColor(new DeviceRgb(248, 250, 252))
                                .SetBorder(new iText.Layout.Borders.SolidBorder(new DeviceRgb(226, 232, 240), 1))
                                .SetPadding(14);
                            contentBox.AddCell(contentCell);
                            contentBox.SetMarginBottom(35);
                            doc.Add(contentBox);

                            // 5. Sign-off block
                            Table signTable = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 })).UseAllAvailableWidth();
                            signTable.SetKeepTogether(true);

                            Cell signLeft = new Cell().Add(new Paragraph("NGƯỜI LẬP BÁO CÁO\n(Ký và ghi rõ họ tên)\n\n\n\n\n" + (_selectedReport.NguoiLap ?? "Người lập"))
                                .SetFont(fontBold).SetFontSize(10).SetTextAlignment(TextAlignment.CENTER))
                                .SetBorder(iText.Layout.Borders.Border.NO_BORDER);

                            Cell signRight = new Cell().Add(new Paragraph("BAN GIÁM ĐỐC / TRƯỞNG PHÒNG\n(Ký tên và đóng dấu)\n\n\n\n\n" + "(Đã duyệt)")
                                .SetFont(fontBold).SetFontSize(10).SetTextAlignment(TextAlignment.CENTER))
                                .SetBorder(iText.Layout.Borders.Border.NO_BORDER);

                            signTable.AddCell(signLeft);
                            signTable.AddCell(signRight);
                            doc.Add(signTable);
                        }

                        MessageBox.Show("Xuất báo cáo PDF thành công tại:\n" + sfd.FileName, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Lỗi xuất PDF: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnCreateReport_Click(object? sender, EventArgs e)
        {
            using (Form f = new Form())
            {
                f.Text = "LẬP BÁO CÁO THỐNG KÊ MỚI";
                f.ClientSize = new Size(820, 520);
                f.MinimumSize = new Size(700, 480);
                f.StartPosition = FormStartPosition.CenterParent;
                f.FormBorderStyle = FormBorderStyle.Sizable;
                f.MaximizeBox = false;
                f.MinimizeBox = false;
                f.BackColor = Color.White;
                f.Font = ThemeColor.BodyFont;

                Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 64, Padding = new Padding(24, 16, 24, 16) };
                Label lHeader = new Label { Text = "TẠO BÁO CÁO NHÂN SỰ MỚI", Font = ThemeColor.SubHeaderFont, ForeColor = ThemeColor.Primary, AutoSize = true };
                pnlHeader.Controls.Add(lHeader);

                TableLayoutPanel tlp = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 2,
                    RowCount = 3,
                    Padding = new Padding(36, 20, 36, 20),
                    AutoScroll = true
                };
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160F));
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

                tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
                tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

                Label l1 = new Label { Text = "Tên báo cáo:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(4, 10, 16, 10) };
                TextBox txtTenBC = new TextBox { Dock = DockStyle.Fill, Text = "Báo cáo cơ cấu nhân sự định kỳ", Margin = new Padding(0, 8, 16, 8) };

                Label l2 = new Label { Text = "Loại báo cáo:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(4, 10, 16, 10) };
                ComboBox cboLoai = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 8, 16, 8) };
                cboLoai.Items.AddRange(new object[] { "Cơ cấu phòng ban", "Biến động nhân sự", "Thống kê quỹ lương", "Hợp đồng lao động", "Khác" });
                cboLoai.SelectedIndex = 0;

                Label l3 = new Label { Text = "Nội dung ghi chú:", AutoSize = true, Anchor = AnchorStyles.Left | AnchorStyles.Top, Padding = new Padding(0, 6, 0, 0), Margin = new Padding(4, 10, 16, 10) };
                TextBox txtNoiDung = new TextBox
                {
                    Dock = DockStyle.Fill,
                    Multiline = true,
                    ScrollBars = ScrollBars.Vertical,
                    Text = "Tổng hợp số liệu thực tế nhân sự và quỹ lương từ hệ thống ERP.",
                    Margin = new Padding(0, 8, 16, 8)
                };

                tlp.Controls.Add(l1, 0, 0); tlp.Controls.Add(txtTenBC, 1, 0);
                tlp.Controls.Add(l2, 0, 1); tlp.Controls.Add(cboLoai, 1, 1);
                tlp.Controls.Add(l3, 0, 2); tlp.Controls.Add(txtNoiDung, 1, 2);

                Panel pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 76, Padding = new Padding(24, 14, 24, 14) };
                FlowLayoutPanel flpButtons = new FlowLayoutPanel
                {
                    Dock = DockStyle.Right,
                    AutoSize = true,
                    WrapContents = false,
                    FlowDirection = FlowDirection.RightToLeft
                };

                Button btnCancel = UIHelper.CreateButton("Hủy", Color.FromArgb(148, 163, 184), Color.White, 95, 38);
                btnCancel.Click += (s2, e2) => f.Close();

                Button btnSave = UIHelper.CreateButton("💾 Lưu Báo Cáo", ThemeColor.Success, Color.White, 150, 38);
                btnSave.Margin = new Padding(0, 0, 14, 0);
                btnSave.Click += (s2, e2) =>
                {
                    string tenBc = txtTenBC.Text.Trim();
                    string noiDung = txtNoiDung.Text.Trim();

                    if (string.IsNullOrWhiteSpace(tenBc))
                    {
                        txtTenBC.Focus();
                        MessageBox.Show("Tên báo cáo không được để trống!", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(noiDung))
                    {
                        txtNoiDung.Focus();
                        MessageBox.Show("Nội dung báo cáo không được để trống!", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    var report = new ThongKeBaoCaoDTO
                    {
                        MaBaoCao = _bllBaoCao.GetNextId(),
                        TenBaoCao = tenBc,
                        LoaiBaoCao = cboLoai.SelectedItem?.ToString() ?? "Cơ cấu phòng ban",
                        NgayLap = DateTime.Today,
                        ID_NV = HeThongBLL.CurrentUser?.ID_NV,
                        NoiDung = noiDung
                    };

                    if (_bllBaoCao.CreateReport(report, out string err))
                    {
                        MessageBox.Show("Lưu báo cáo vào hệ thống thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        f.Close();
                        LoadAllStats();
                        tabStats.SelectedIndex = 1; // Chuyển sang Tab Nhật Ký để xem ngay
                    }
                    else
                    {
                        MessageBox.Show(err, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };

                flpButtons.Controls.Add(btnCancel);
                flpButtons.Controls.Add(btnSave);
                pnlBottom.Controls.Add(flpButtons);

                f.Controls.Add(tlp);
                f.Controls.Add(pnlBottom);
                f.Controls.Add(pnlHeader);
                f.ShowDialog();
            }
        }

        private void BtnEditReport_Click(object? sender, EventArgs e)
        {
            if (_selectedReport == null)
            {
                if (dgvHistoryReports.SelectedRows.Count > 0)
                {
                    _selectedReport = dgvHistoryReports.SelectedRows[0].DataBoundItem as ThongKeBaoCaoDTO;
                }
            }

            if (_selectedReport == null)
            {
                MessageBox.Show("Vui lòng chọn một báo cáo từ 'Nhật Ký Báo Cáo' để chỉnh sửa!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                if (tabStats.SelectedIndex != 1)
                {
                    tabStats.SelectedIndex = 1;
                }
                return;
            }

            using (Form f = new Form())
            {
                f.Text = $"CHỈNH SỬA BÁO CÁO - {_selectedReport.MaBaoCao}";
                f.ClientSize = new Size(820, 520);
                f.MinimumSize = new Size(700, 480);
                f.StartPosition = FormStartPosition.CenterParent;
                f.FormBorderStyle = FormBorderStyle.Sizable;
                f.MaximizeBox = false;
                f.MinimizeBox = false;
                f.BackColor = Color.White;
                f.Font = ThemeColor.BodyFont;

                Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 64, Padding = new Padding(24, 16, 24, 16) };
                Label lHeader = new Label { Text = $"CHỈNH SỬA THÔNG TIN BÁO CÁO [{_selectedReport.MaBaoCao}]", Font = ThemeColor.SubHeaderFont, ForeColor = ThemeColor.Primary, AutoSize = true };
                pnlHeader.Controls.Add(lHeader);

                TableLayoutPanel tlp = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 2,
                    RowCount = 3,
                    Padding = new Padding(36, 20, 36, 20),
                    AutoScroll = true
                };
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160F));
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

                tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
                tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

                Label l1 = new Label { Text = "Tên báo cáo:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(4, 10, 16, 10) };
                TextBox txtTenBC = new TextBox { Dock = DockStyle.Fill, Text = _selectedReport.TenBaoCao, Margin = new Padding(0, 8, 16, 8) };

                Label l2 = new Label { Text = "Loại báo cáo:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(4, 10, 16, 10) };
                ComboBox cboLoai = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 8, 16, 8) };
                cboLoai.Items.AddRange(new object[] { "Cơ cấu phòng ban", "Biến động nhân sự", "Thống kê quỹ lương", "Hợp đồng lao động", "Khác" });
                int idx = cboLoai.FindStringExact(_selectedReport.LoaiBaoCao ?? "");
                cboLoai.SelectedIndex = idx >= 0 ? idx : 0;

                Label l3 = new Label { Text = "Nội dung ghi chú:", AutoSize = true, Anchor = AnchorStyles.Left | AnchorStyles.Top, Padding = new Padding(0, 6, 0, 0), Margin = new Padding(4, 10, 16, 10) };
                TextBox txtNoiDung = new TextBox
                {
                    Dock = DockStyle.Fill,
                    Multiline = true,
                    ScrollBars = ScrollBars.Vertical,
                    Text = _selectedReport.NoiDung ?? "",
                    Margin = new Padding(0, 8, 16, 8)
                };

                tlp.Controls.Add(l1, 0, 0); tlp.Controls.Add(txtTenBC, 1, 0);
                tlp.Controls.Add(l2, 0, 1); tlp.Controls.Add(cboLoai, 1, 1);
                tlp.Controls.Add(l3, 0, 2); tlp.Controls.Add(txtNoiDung, 1, 2);

                Panel pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 76, Padding = new Padding(24, 14, 24, 14) };
                FlowLayoutPanel flpButtons = new FlowLayoutPanel
                {
                    Dock = DockStyle.Right,
                    AutoSize = true,
                    WrapContents = false,
                    FlowDirection = FlowDirection.RightToLeft
                };

                Button btnCancel = UIHelper.CreateButton("Hủy", Color.FromArgb(148, 163, 184), Color.White, 95, 38);
                btnCancel.Click += (s2, e2) => f.Close();

                Button btnSave = UIHelper.CreateButton("💾 Lưu Thay Đổi", ThemeColor.Success, Color.White, 150, 38);
                btnSave.Margin = new Padding(0, 0, 14, 0);
                btnSave.Click += (s2, e2) =>
                {
                    string tenBc = txtTenBC.Text.Trim();
                    string noiDung = txtNoiDung.Text.Trim();

                    if (string.IsNullOrWhiteSpace(tenBc))
                    {
                        txtTenBC.Focus();
                        MessageBox.Show("Tên báo cáo không được để trống!", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(noiDung))
                    {
                        txtNoiDung.Focus();
                        MessageBox.Show("Nội dung báo cáo không được để trống!", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    _selectedReport.TenBaoCao = tenBc;
                    _selectedReport.LoaiBaoCao = cboLoai.SelectedItem?.ToString() ?? "Cơ cấu phòng ban";
                    _selectedReport.NoiDung = noiDung;

                    if (_bllBaoCao.UpdateReport(_selectedReport, out string err))
                    {
                        MessageBox.Show("Cập nhật báo cáo thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        f.Close();
                        string curId = _selectedReport.MaBaoCao;
                        LoadAllStats();
                        tabStats.SelectedIndex = 1;

                        foreach (DataGridViewRow r in dgvHistoryReports.Rows)
                        {
                            if (r.DataBoundItem is ThongKeBaoCaoDTO dto && dto.MaBaoCao == curId)
                            {
                                r.Selected = true;
                                DisplayReportDetail(dto);
                                break;
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show(err, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };

                flpButtons.Controls.Add(btnCancel);
                flpButtons.Controls.Add(btnSave);
                pnlBottom.Controls.Add(flpButtons);

                f.Controls.Add(tlp);
                f.Controls.Add(pnlBottom);
                f.Controls.Add(pnlHeader);
                f.ShowDialog();
            }
        }

        private void BtnExportExcel_Click(object? sender, EventArgs e)
        {
            OfficeOpenXml.ExcelPackage.License.SetNonCommercialPersonal("Acecook ERP");

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "Excel Files (*.xlsx)|*.xlsx";

                if (tabStats.SelectedIndex == 0)
                {
                    sfd.FileName = $"BaoCao_CoCau_QuyLuong_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
                    sfd.Title = "Chọn nơi lưu file Excel Báo Cáo Cơ Cấu & Quỹ Lương";
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        try
                        {
                            DataTable dt = _bllBaoCao.GetDepartmentStats();
                            FileInfo fileInfo = new FileInfo(sfd.FileName);
                            if (fileInfo.Exists) fileInfo.Delete();

                            using (var package = new OfficeOpenXml.ExcelPackage(fileInfo))
                            {
                                var ws = package.Workbook.Worksheets.Add("Cơ Cấu Quỹ Lương");
                                ws.View.ShowGridLines = true;

                                // Header Company
                                ws.Cells[1, 1].Value = "CÔNG TY CỔ PHẦN ACECOOK VIỆT NAM";
                                ws.Cells[1, 1].Style.Font.Bold = true;
                                ws.Cells[1, 1].Style.Font.Size = 11;
                                ws.Cells[1, 1].Style.Font.Color.SetColor(Color.FromArgb(100, 116, 139));

                                ws.Cells[2, 1, 2, 7].Merge = true;
                                ws.Cells[2, 1].Value = "BÁO CÁO CƠ CẤU NHÂN SỰ & QUỸ LƯƠNG PHÒNG BAN";
                                ws.Cells[2, 1].Style.Font.Bold = true;
                                ws.Cells[2, 1].Style.Font.Size = 16;
                                ws.Cells[2, 1].Style.Font.Color.SetColor(Color.FromArgb(2, 132, 199));
                                ws.Cells[2, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                                ws.Cells[3, 1, 3, 7].Merge = true;
                                ws.Cells[3, 1].Value = $"Thời điểm xuất dữ liệu: {DateTime.Now:dd/MM/yyyy HH:mm:ss} | Hệ thống ERP Acecook";
                                ws.Cells[3, 1].Style.Font.Italic = true;
                                ws.Cells[3, 1].Style.Font.Size = 10;
                                ws.Cells[3, 1].Style.Font.Color.SetColor(Color.Gray);
                                ws.Cells[3, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                                string[] headers = { "Mã PB", "Tên Phòng Ban", "Trưởng Phòng", "Số Nhân Viên", "Lương TB (VNĐ)", "Tổng Quỹ Lương (VNĐ)", "Trạng Thái" };
                                int startRow = 5;
                                for (int i = 0; i < headers.Length; i++)
                                {
                                    var cell = ws.Cells[startRow, i + 1];
                                    cell.Value = headers[i];
                                    cell.Style.Font.Bold = true;
                                    cell.Style.Font.Color.SetColor(Color.White);
                                    cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                    cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(2, 132, 199));
                                    cell.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                                    cell.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                                    cell.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin, Color.FromArgb(203, 213, 225));
                                }
                                ws.Row(startRow).Height = 28;

                                int curRow = startRow + 1;
                                long totalNV = 0;
                                decimal totalLuong = 0;

                                foreach (DataRow row in dt.Rows)
                                {
                                    string maPB = row["maPhongBan"]?.ToString() ?? "";
                                    string tenPB = row["tenPhongBan"]?.ToString() ?? "";
                                    string truongPhong = row["truongPhong"]?.ToString() ?? "";
                                    int soNV = row["soNhanVien"] != DBNull.Value ? Convert.ToInt32(row["soNhanVien"]) : 0;
                                    decimal luongTB = row["luongTB"] != DBNull.Value ? Convert.ToDecimal(row["luongTB"]) : 0;
                                    decimal tongLuong = row["tongQuyLuong"] != DBNull.Value ? Convert.ToDecimal(row["tongQuyLuong"]) : 0;
                                    string trangThai = row["trangThai"]?.ToString() ?? "";

                                    totalNV += soNV;
                                    totalLuong += tongLuong;

                                    ws.Cells[curRow, 1].Value = maPB;
                                    ws.Cells[curRow, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                                    ws.Cells[curRow, 2].Value = tenPB;
                                    ws.Cells[curRow, 3].Value = truongPhong;

                                    ws.Cells[curRow, 4].Value = soNV;
                                    ws.Cells[curRow, 4].Style.Numberformat.Format = "#,##0";
                                    ws.Cells[curRow, 4].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;

                                    ws.Cells[curRow, 5].Value = luongTB;
                                    ws.Cells[curRow, 5].Style.Numberformat.Format = "#,##0 \"VNĐ\"";
                                    ws.Cells[curRow, 5].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;

                                    ws.Cells[curRow, 6].Value = tongLuong;
                                    ws.Cells[curRow, 6].Style.Numberformat.Format = "#,##0 \"VNĐ\"";
                                    ws.Cells[curRow, 6].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;

                                    ws.Cells[curRow, 7].Value = trangThai;
                                    ws.Cells[curRow, 7].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                                    for (int col = 1; col <= 7; col++)
                                    {
                                        ws.Cells[curRow, col].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin, Color.FromArgb(226, 232, 240));
                                    }
                                    ws.Row(curRow).Height = 22;
                                    curRow++;
                                }

                                // Total Row
                                ws.Cells[curRow, 1, curRow, 3].Merge = true;
                                ws.Cells[curRow, 1].Value = "TỔNG CỘNG";
                                ws.Cells[curRow, 1].Style.Font.Bold = true;
                                ws.Cells[curRow, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                                ws.Cells[curRow, 4].Value = totalNV;
                                ws.Cells[curRow, 4].Style.Font.Bold = true;
                                ws.Cells[curRow, 4].Style.Numberformat.Format = "#,##0";
                                ws.Cells[curRow, 4].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;

                                ws.Cells[curRow, 5].Value = totalNV > 0 ? (totalLuong / totalNV) : 0;
                                ws.Cells[curRow, 5].Style.Font.Bold = true;
                                ws.Cells[curRow, 5].Style.Numberformat.Format = "#,##0 \"VNĐ\"";
                                ws.Cells[curRow, 5].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;

                                ws.Cells[curRow, 6].Value = totalLuong;
                                ws.Cells[curRow, 6].Style.Font.Bold = true;
                                ws.Cells[curRow, 6].Style.Numberformat.Format = "#,##0 \"VNĐ\"";
                                ws.Cells[curRow, 6].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;

                                ws.Cells[curRow, 7].Value = "";

                                for (int col = 1; col <= 7; col++)
                                {
                                    var cell = ws.Cells[curRow, col];
                                    cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                    cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(241, 245, 249));
                                    cell.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium, Color.FromArgb(148, 163, 184));
                                }
                                ws.Row(curRow).Height = 26;

                                ws.Cells[ws.Dimension.Address].AutoFitColumns();
                                package.Save();
                            }

                            MessageBox.Show("Xuất file báo cáo Excel thành công tại:\n" + sfd.FileName, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Lỗi xuất file Excel: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                else
                {
                    sfd.FileName = $"NhatKy_BaoCao_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
                    sfd.Title = "Chọn nơi lưu file Excel Nhật Ký Báo Cáo";
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        try
                        {
                            var list = _bllBaoCao.GetAllReports();
                            FileInfo fileInfo = new FileInfo(sfd.FileName);
                            if (fileInfo.Exists) fileInfo.Delete();

                            using (var package = new OfficeOpenXml.ExcelPackage(fileInfo))
                            {
                                var ws = package.Workbook.Worksheets.Add("Nhật Ký Báo Cáo");
                                ws.View.ShowGridLines = true;

                                // Header Company
                                ws.Cells[1, 1].Value = "CÔNG TY CỔ PHẦN ACECOOK VIỆT NAM";
                                ws.Cells[1, 1].Style.Font.Bold = true;
                                ws.Cells[1, 1].Style.Font.Size = 11;
                                ws.Cells[1, 1].Style.Font.Color.SetColor(Color.FromArgb(100, 116, 139));

                                ws.Cells[2, 1, 2, 6].Merge = true;
                                ws.Cells[2, 1].Value = "DANH SÁCH BÁO CÁO THỐNG KÊ ĐÃ LƯU TRỮ";
                                ws.Cells[2, 1].Style.Font.Bold = true;
                                ws.Cells[2, 1].Style.Font.Size = 16;
                                ws.Cells[2, 1].Style.Font.Color.SetColor(Color.FromArgb(2, 132, 199));
                                ws.Cells[2, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                                ws.Cells[3, 1, 3, 6].Merge = true;
                                ws.Cells[3, 1].Value = $"Thời điểm xuất dữ liệu: {DateTime.Now:dd/MM/yyyy HH:mm:ss} | Hệ thống ERP Acecook";
                                ws.Cells[3, 1].Style.Font.Italic = true;
                                ws.Cells[3, 1].Style.Font.Size = 10;
                                ws.Cells[3, 1].Style.Font.Color.SetColor(Color.Gray);
                                ws.Cells[3, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                                string[] headers = { "Mã BC", "Tên Báo Cáo", "Loại Báo Cáo", "Ngày Lập", "Người Lập", "Tóm Tắt Nội Dung" };
                                int startRow = 5;
                                for (int i = 0; i < headers.Length; i++)
                                {
                                    var cell = ws.Cells[startRow, i + 1];
                                    cell.Value = headers[i];
                                    cell.Style.Font.Bold = true;
                                    cell.Style.Font.Color.SetColor(Color.White);
                                    cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                    cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(2, 132, 199));
                                    cell.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                                    cell.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                                    cell.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin, Color.FromArgb(203, 213, 225));
                                }
                                ws.Row(startRow).Height = 28;

                                int curRow = startRow + 1;
                                foreach (var r in list)
                                {
                                    ws.Cells[curRow, 1].Value = r.MaBaoCao ?? "";
                                    ws.Cells[curRow, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                                    ws.Cells[curRow, 2].Value = r.TenBaoCao ?? "";
                                    ws.Cells[curRow, 3].Value = r.LoaiBaoCao ?? "";

                                    ws.Cells[curRow, 4].Value = r.NgayLap;
                                    ws.Cells[curRow, 4].Style.Numberformat.Format = "dd/MM/yyyy";
                                    ws.Cells[curRow, 4].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                                    ws.Cells[curRow, 5].Value = r.NguoiLap ?? "";
                                    ws.Cells[curRow, 6].Value = r.NoiDung ?? "";

                                    for (int col = 1; col <= 6; col++)
                                    {
                                        ws.Cells[curRow, col].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin, Color.FromArgb(226, 232, 240));
                                    }
                                    ws.Row(curRow).Height = 22;
                                    curRow++;
                                }

                                ws.Cells[ws.Dimension.Address].AutoFitColumns();
                                package.Save();
                            }

                            MessageBox.Show("Xuất nhật ký báo cáo Excel thành công tại:\n" + sfd.FileName, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Lỗi xuất file Excel: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }
    }
}