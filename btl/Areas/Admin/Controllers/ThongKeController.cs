using ClosedXML.Excel;
using Mood.Draw;
using Mood.ThongKeModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.IO;

namespace BaiTapLon.Areas.Admin.Controllers
{
    public class ThongKeController : BaseController
    {
        // ===========================
        // INDEX
        // ===========================
        public ActionResult Index()
        {
            return View();
        }

        // ===========================
        // TOP PRODUCT
        // ===========================
        public ActionResult ThongKeSanPhamHot()
        {
            ViewBag.listSanPhamHot = new SanphamDraw().listTopSellings(5);
            return View();
        }

        // ===========================
        // DOANH THU TABLE
        // ===========================
        public ActionResult DoanhThu(string fromDate, string todate, int page = 1, int pageSize = 20)
        {
            var thongkeList =
                new OrderDraw().getDoanhThu(fromDate, todate, page, pageSize);

            int tongThu = 0;
            int tongLai = 0;

            foreach (var item in thongkeList)
            {
                tongThu += (int)item.DoanhThu;
                tongLai += (int)item.TongLai;
            }

            ViewBag.tongThu = tongThu;
            ViewBag.tongLai = tongLai;

            ViewBag.fromDate = fromDate;
            ViewBag.todate = todate;

            return View(thongkeList);
        }

        // ===========================
        // DOANH THU CHART
        // ===========================
        public ActionResult DoanhThuChart(string fromDate, string todate)
        {
            var thongkeList =
                new OrderDraw().getDoanhThuChart(fromDate, todate);

            List<int> listDoanhThu = new List<int>();
            List<int> listTongLai = new List<int>();
            List<string> doanhsoNgay = new List<string>();

            int tongThu = 0;
            int tongLai = 0;

            foreach (var item in thongkeList)
            {
                tongThu += (int)item.DoanhThu;
                tongLai += (int)item.TongLai;

                listDoanhThu.Add((int)item.DoanhThu);
                listTongLai.Add((int)item.TongLai);

                // ✅ FIX FORMAT DATE AN TOÀN
                doanhsoNgay.Add(
                    Convert.ToDateTime(item.DoanhThuNgay)
                           .ToString("dd/MM/yyyy"));
            }

            ViewBag.listDoanhThu = listDoanhThu;
            ViewBag.listTongLai = listTongLai;

            ViewBag.doanhsoNgay = doanhsoNgay;
            ViewBag.tongThu = tongThu;
            ViewBag.tongLai = tongLai;

            ViewBag.fromDate = fromDate;
            ViewBag.todate = todate;

            return View();
        }

        // ===========================
        // ✅ EXPORT EXCEL FINAL
        // ===========================
        public ActionResult ExportExel(string fromDate, string toDate)
        {
            try
            {
                // ✅ MAP PATH CHUẨN MVC
                string templatePath =
                    Server.MapPath("~/Resource/Template/Tempalet_Doanh_Thu.xlsx");

                // ✅ CHECK FILE TEMPLATE
                if (!System.IO.File.Exists(templatePath))
                {
                    return Content("❌ Không tìm thấy file template: " + templatePath);
                }

                // ✅ LOAD TEMPLATE
                var wb = new XLWorkbook(templatePath);
                var ws = wb.Worksheet(1);

                // ======================
                // HEADER
                // ======================
                if (!string.IsNullOrEmpty(fromDate) || !string.IsNullOrEmpty(toDate))
                    ws.Cell("C1").Value = "THỐNG KÊ DOANH THU";
                else
                    ws.Cell("C1").Value = "THỐNG KÊ DOANH THU CẢ NĂM";

                var list =
                    new OrderDraw().getDoanhThuChart(fromDate, toDate);

                ws.Cell("D6").Value = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

                ws.Cell("B16").Value = fromDate;
                ws.Cell("D16").Value = toDate;

                // ======================
                // DATA TABLE
                // ======================
                int row = 19;

                foreach (var item in list)
                {
                    ws.Cell("B" + row).Value =
                        Convert.ToDateTime(item.DoanhThuNgay)
                               .ToString("dd/MM/yyyy");

                    ws.Cell("C" + row).Value = item.DoanhThu;
                    ws.Cell("D" + row).Value = item.TongLai;

                    row++;
                }

                // ======================
                // DOWNLOAD FILE
                // ======================
                using (var ms = new MemoryStream())
                {
                    wb.SaveAs(ms);
                    ms.Position = 0;

                    string fileName;

                    if (!string.IsNullOrEmpty(fromDate) || !string.IsNullOrEmpty(toDate))
                    {
                        fileName =
                            $"DoanhThu_{fromDate}_{toDate}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                    }
                    else
                    {
                        fileName =
                            $"DoanhThu_CaNam_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                    }

                    return File(
                        ms.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName);
                }
            }
            catch (Exception ex)
            {
                return Content("❌ Export thất bại: " + ex.Message);
            }
        }
    }
}
