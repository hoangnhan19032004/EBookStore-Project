using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Mood.Draw;
using Mood.EF2;
using BaiTapLon.Common;
using X.PagedList;

namespace BaiTapLon.Areas.Admin.Controllers
{
    public class UserController : BaseController
    {
        // GET: Admin/User
        public ActionResult Index(string searchString, int page = 1, int pageSize = 10)
        {
            var dao = new UserDraw();
            var model = dao.ListViewUser(searchString, page, pageSize);
            ViewBag.SearchString = searchString;
            return View(model);
        }

        // GET: Admin/User/Create
        [HttpGet]
        public ActionResult Create()
        {
            SetQuyenAdmin();
            return View(new User());
        }

        // POST: Admin/User/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(User entityUser)
        {
            SetQuyenAdmin(); // để ViewBag.IDQuyen không bị null khi reload view

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("CreateUser1", "Vui lòng nhập đầy đủ thông tin hợp lệ.");
                return View(entityUser);
            }

            var dao = new UserDraw();

            // Kiểm tra định dạng Email
            if (!dao.IsValidEmail(entityUser.Email))
            {
                ModelState.AddModelError("CreateUser1", "Định dạng email không hợp lệ!");
                return View(entityUser);
            }

            // Kiểm tra SĐT
            if (!dao.CheckSDT(entityUser.Phone))
            {
                ModelState.AddModelError("CreateUser1", "Số điện thoại không hợp lệ!");
                return View(entityUser);
            }

            // Kiểm tra trùng tên tài khoản
            if (!dao.checkUserName(entityUser.UserName))
            {
                ModelState.AddModelError("CreateUser1", "Tên tài khoản đã tồn tại!");
                return View(entityUser);
            }

            // Gán thông tin mặc định
            entityUser.PassWord = EncryptorMD5.GetMD5(entityUser.PassWord.Trim());
            entityUser.NgayTao = DateTime.Now;
            entityUser.Status = entityUser.Status;

            try
            {
                long id = dao.Insert(entityUser);
                if (id > 0)
                {
                    ModelState.AddModelError("CreateSuccess", "Thêm quản trị viên thành công!");
                    ModelState.Clear(); // Xóa dữ liệu form
                    return View(new User());
                }
                else
                {
                    ModelState.AddModelError("CreateUser1", "Không thể thêm người dùng. Vui lòng thử lại.");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("CreateUser1", "Lỗi khi thêm người dùng: " + ex.Message);
            }

            return View(entityUser);
        }

        // GET: Admin/User/Edit
        [HttpGet]
        public ActionResult Edit(int id)
        {
            var dao = new UserDraw();
            var user = dao.ViewDetail(id);

            if (user == null)
            {
                return HttpNotFound();
            }

            if (user.IDQuyen == 1)
                SetQuyenAdmin();
            else
                SetQuyen();

            return View(user);
        }

        // POST: Admin/User/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(User entity)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("capnhat", "Vui lòng nhập đầy đủ thông tin hợp lệ.");
                return View(entity);
            }

            var dao = new UserDraw();

            if (!string.IsNullOrEmpty(entity.PassWord))
                entity.PassWord = EncryptorMD5.GetMD5(entity.PassWord);

            if (!dao.IsValidEmail(entity.Email))
            {
                ModelState.AddModelError("capnhat", "Định dạng email không hợp lệ!");
                return View(entity);
            }

            if (!dao.CheckSDT(entity.Phone))
            {
                ModelState.AddModelError("capnhat", "Định dạng số điện thoại không hợp lệ!");
                return View(entity);
            }

            bool result = dao.Update(entity);
            if (result)
            {
                ModelState.AddModelError("capnhatSuccess", "Cập nhật thông tin người dùng thành công!");
            }
            else
            {
                ModelState.AddModelError("capnhat", "Cập nhật thất bại!");
            }

            if (entity.IDQuyen == 1)
                SetQuyenAdmin();
            else
                SetQuyen();

            return View(entity);
        }

        // Xóa
        [HttpDelete]
        public ActionResult Delete(long id)
        {
            new UserDraw().Delete(id);
            return RedirectToAction("Index");
        }

        // Ajax đổi trạng thái
        [HttpPost]
        public JsonResult ChangeStatus(long id)
        {
            var result = new UserDraw().ChangeStatus(id);
            return Json(new { status = result });
        }

        // Set quyền thường
        public void SetQuyen(long? selectedId = null)
        {
            var draw = new UserDraw();
            ViewBag.IDQuyen = new SelectList(draw.ListUserNomarl(), "IDQuyen", "TenQuyen", selectedId);
        }

        // Set quyền admin
        public void SetQuyenAdmin(long? selectedId = null)
        {
            var draw = new UserDraw();
            ViewBag.IDQuyen = new SelectList(draw.ListUserAdmin(), "IDQuyen", "TenQuyen", selectedId);
        }
    }
}
