using ClosedXML.Excel;
using Mood.Draw;
using Mood.EF2;
using Mood.HoaDonModel;
using System;
using System.Linq;
using System.Web.Mvc;
using System.IO;

namespace BaiTapLon.Areas.Admin.Controllers
{
    public class HoaDonController : BaseController
    {
        int soTien = 0;

        // ==============================
        // LIST & FILTER
        // ==============================

        public ActionResult Index(string searchString, int page = 1, int pagesize = 5)
        {
            var hoaDon = new OrderDraw().ListALL(searchString, page, pagesize);
            return View(hoaDon);
        }

        public ActionResult XacNhan(string searchString, int page = 1, int pagesize = 5)
        {
            var hoaDon = new OrderDraw().ListALL(searchString, page, pagesize);
            return View(hoaDon);
        }

        public ActionResult DongGoi(string searchString, int page = 1, int pagesize = 5)
        {
            var hoaDon = new OrderDraw().listChoGiao(searchString, page, pagesize);
            return View(hoaDon);
        }

        public ActionResult XuatKho(string searchString, int page = 1, int pagesize = 5)
        {
            var xuatKho = new OrderDraw().listXuatKho(searchString, page, pagesize);
            return View(xuatKho);
        }

        public ActionResult HoanThanh(string searchString, int page = 1, int pagesize = 5)
        {
            var hoaDon = new OrderDraw().listHoanThanh(searchString, page, pagesize);
            return View(hoaDon);
        }

        public ActionResult TraLai(string searchString, int page = 1, int pagesize = 5)
        {
            var hoaDon = new OrderDraw().listTraLai(searchString, page, pagesize);
            return View(hoaDon);
        }

        // ==============================
        // DETAILS
        // ==============================

        public ActionResult Details(long id, string searchString, int page = 1, int pagesize = 5)
        {
            var hoaDonModel = new OrderDraw().getOrderByID(id);
            ViewBag.hoaDonSanPham =
                new Order_DetailDraw().chiTietHoaDon(id, searchString, page, pagesize);

            ViewBag.total = 0;

            var listItem =
                new Order_DetailDraw().dataExport(id, searchString);

            foreach (var item in listItem)
            {
                ViewBag.total += item.Price * item.Quanlity;
            }

            soTien = ViewBag.total;

            return View(hoaDonModel);
        }

        // ==============================
        // AJAX STATUS
        // ==============================

        [HttpPost]
        public JsonResult ChangeStatusOrder(long id)
            => Json(new { status = new OrderDraw().ChangeStatusOrder(id) });

        [HttpPost]
        public JsonResult ChangeGiaoHangOrder(long id)
            => Json(new { GiaoHang = new OrderDraw().ChangeGiaoHang(id) });

        [HttpPost]
        public JsonResult ChangeXuatKhoOrder(long id)
            => Json(new { XuatKho = new OrderDraw().ChangeXuatKho(id) });

        [HttpPost]
        public JsonResult ChangeSuccessOrder(long id)
            => Json(new { NhanHang = new OrderDraw().ChangeHoanThanh(id) });

        [HttpPost]
        public JsonResult ChangeGiaoHangTraLai(long id)
            => Json(new { NhanHang = new OrderDraw().ChangeHoanThanh(id) });

        [HttpPost]
        public JsonResult ChangeGiaoHangThatBai(long id)
            => Json(new { GiaoHang = new OrderDraw().ChangeHoanThanhGiao(id) });

        // ==============================
        // DELETE
        // ==============================

        [HttpDelete]
        public ActionResult Delete(long id)
        {
            new OrderDraw().Delete(id);
            return RedirectToAction("XacNhan");
        }

        // ==============================
        // GHI CHÚ
        // ==============================

        [HttpGet]
        public ActionResult GhiChu(long id)
        {
            var hoaDon = new OrderDraw().getOrderByID(id);
            ViewBag.soTien = soTien;
            return View(hoaDon);
        }

        [HttpPost]
        public ActionResult GhiChu(Orders model)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("ghiChu", "Không thể cập nhật");
                return View("GhiChu");
            }

            var draw = new OrderDraw();
            bool result = draw.UpdateGhiChu(model);

            if (result)
                ModelState.AddModelError("ghiChuSuccess", "✅ Thêm ghi chú thành công");
            else
                ModelState.AddModelError("ghiChu", "❌ Thêm ghi chú thất bại");

            return View("GhiChu");
        }

        // ==============================
        // EXPORT EXCEL ✅ FINAL
        // ==============================

        public ActionResult ExportExel(long id)
        {
            try
            {
                string templatePath =
                    Server.MapPath("~/Resource/Template/Hoa_Don_Template.xlsx");

                if (!System.IO.File.Exists(templatePath))
                {
                    return Content("❌ Không tìm thấy file template: " + templatePath);
                }

                var wb = new XLWorkbook(templatePath);
                var ws = wb.Worksheet(1);

                var list =
                    new Order_DetailDraw().dataExport(id, "");

                if (list == null || list.Count == 0)
                    return Content("❌ Không có dữ liệu để export");

                var item = list[0];

                // Header
                ws.Cell("E6").Value = item.OderID;

                ws.Cell("E7").Value =
                    Convert.ToDateTime(item.NgayTao)
                        .ToString("dd/MM/yyyy");

                ws.Cell("B15").Value = item.TenKH;
                ws.Cell("B18").Value = item.Address;
                ws.Cell("F15").Value = item.Email;
                ws.Cell("D15").Value = "0" + item.Phone;

                ws.Cell("D18").Value =
                    item.Status == 0 ? "Chờ duyệt" : "Đã duyệt";

                if (item.GiaoHang == 2)
                {
                    if (item.NhanHang == 1)
                        ws.Cell("F18").Value = "Đã hoàn tất";
                    else if (item.NhanHang == 2)
                        ws.Cell("F18").Value = "Trả lại";
                    else
                        ws.Cell("F18").Value = "Đang giao";
                }
                else if (item.GiaoHang == 1)
                {
                    ws.Cell("F18").Value = "Chờ xuất kho";
                }
                else
                {
                    ws.Cell("F18").Value = "Chờ đóng gói";
                }

                ws.Cell("B21").Value =
                    item.DeliveryPaymentMethod == "COD"
                        ? "Tiền mặt"
                        : "Thanh toán online";

                if (!string.IsNullOrEmpty(item.ghiChu))
                    ws.Cell("D21").Value = item.ghiChu;

                // Table products

                int row = 24;

                foreach (var sp in list)
                {
                    ws.Cell("B" + row).Value = sp.Name;
                    ws.Cell("C" + row).Value = sp.Quanlity;
                    ws.Cell("D" + row).Value = sp.Price;
                    ws.Cell("F" + row).Value = sp.Price * sp.Quanlity;
                    row++;
                }

                // Save file

                using (var ms = new MemoryStream())
                {
                    wb.SaveAs(ms);
                    ms.Position = 0;

                    string fileName =
                        $"HoaDon_{item.OderID}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

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
