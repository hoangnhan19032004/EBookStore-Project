using BaiTapLon.Models;
using Mood.Draw;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using Mood.EF2;
using System.Configuration;
using CommomSentMail;
using BaiTapLon.Common;
using BaiTapLon.MoMo_API;
using Newtonsoft.Json.Linq;
using System.Net;
using BaiTapLon.VNPAY_API;

namespace BaiTapLon.Controllers
{
    public class CartController : Controller
    {
        // GET: Cart
        private const string CartSession = "CartSession";// hằng số không thể đổi
        private const string OrderIDDel = "OrderID";

        public ActionResult Index()
        {
            var cart = Session[CartSession];

            var list = new List<CartItem>();
            ViewBag.totalProduct = 0;
            if (cart != null)
            {
                list = (List<CartItem>)cart;
                foreach (var item in list)
                {
                    ViewBag.totalProduct += (item.Quantity * item.Product.GiaTien);
                }
            }
            return View(list);
        }

        //Nhận 2 giá trị proID và số lượng.
        [HttpGet]
        public JsonResult AddItem(long productID, int quantity)
        {
            var product = new SanphamDraw().getByID(productID);
            var cart = Session[CartSession];
            var sessionUser = (UserLogin)Session[Constant.USER_SESSION];

            if (cart != null)
            {
                var list = (List<CartItem>)cart;// nếu nó có rồi nó sẽ ép kiểu sang kiểu list
                //Nếu chứa productID thì nó mới cộng 1
                if (list.Exists(x => x.Product.IDContent == productID))
                {
                    foreach (var item in list)
                    {
                        if (item.Product.IDContent == productID)
                        {
                            item.Quantity += quantity;
                            //tăng số lượng sản phẩm khi thêm tiếp sản phẩm cùng ID.
                        }
                    }
                    var cartCount1 = list.Count();

                    return Json(
                        new
                        {
                            cartCount = cartCount1
                        }
                        , JsonRequestBehavior.AllowGet);
                }

                else
                {
                    //Chưa có sản phẩm như vậy trong giỏ.
                    //Tạo mới đối tượng cart item
                    var item = new CartItem();
                    item.Product = product;
                    item.Quantity = quantity;
                    list.Add(item);
                    item.countCart = list.Count();
                    var cartCount1 = list.Count();
                    //Gán vào session

                    return Json(
                        new
                        {
                            cartCount = cartCount1
                        }
                        , JsonRequestBehavior.AllowGet);

                }

            }

            else
            {
                //Tạo mới đối tượng cart item
                var item = new CartItem();
                item.Product = product;
                item.Quantity = quantity;
                item.countCart = 1;
                var list = new List<CartItem>();

                list.Add(item);
                //Gán vào session
                Session[CartSession] = list;

            }

            return Json(
                 new
                 {
                     cartCount = 1
                 }
                , JsonRequestBehavior.AllowGet);


        }


        public JsonResult Update(string cartModel)
        {
            var jsonCart = new JavaScriptSerializer().Deserialize<List<CartItem>>(cartModel);
            var sessionCart = (List<CartItem>)Session[CartSession];

            foreach (var item in sessionCart)
            {
                var jsonItem = jsonCart.SingleOrDefault(x => x.Product.IDContent == item.Product.IDContent);
                {
                    //đúng sản phẩm ấy
                    if (jsonItem != null)
                    {
                        item.Quantity = jsonItem.Quantity;
                    }
                }

            }
            //sau khi cập nhật gán lại session lại
            Session[CartSession] = sessionCart;
            return Json(new
            {
                status = true
            });// trả về cho res bằng true, bản chất gọi đến server để làm việc
        }
        public JsonResult DeleteAll()
        {
            Session[CartSession] = null;
            return Json(new
            {
                status = true
            });// trả về cho res bằng true, bản chất gọi đến server để làm việc
        }

        public JsonResult Delete(long id)
        {
            //vẫn lấy ra danh sách giỏ hàng
            var sessionCart = (List<CartItem>)Session[CartSession];

            sessionCart.RemoveAll(x => x.Product.IDContent == id);
            Session[CartSession] = sessionCart;
            return Json(new
            {
                status = true
            });// trả về cho res bằng true, bản chất gọi đến server để làm việc
        }

        [HttpGet]
        public ActionResult PaymentMoMo()
        {
            var sessionUser = (UserLogin)Session[Constant.USER_SESSION];
            var list = new List<CartItem>();
            if (sessionUser != null)
            {
                var userLogin = new UserDraw().getByIDLogin(sessionUser.userId);
                ViewBag.LoginUser = userLogin;
            }

            ViewBag.totalProduct = 0;
            var cart = Session[CartSession];
            if (cart != null)
            {
                list = (List<CartItem>)cart;
                ViewBag.listCart = list;
            }
            return View(list);
        }

        [HttpPost]
        public ActionResult PaymentMoMo(string shipName, string shipAddress, string shipMobile, string shipMail)
        {
            // Lấy giỏ hàng từ session
            var cart = Session[CartSession] as List<CartItem>;
            if (cart == null || cart.Count == 0)
            {
                return RedirectToAction("Index", "Cart");
            }

            // Kiểm tra tồn kho
            bool isValidQuantity = !cart.Any(x => x.Quantity > x.Product.Soluong);
            if (!isValidQuantity)
            {
                ViewBag.Error = "Số lượng đặt hàng vượt quá số lượng sách cửa hàng";

                // bind lại data cho View PaymentMoMo
                var sessionUser = (UserLogin)Session[Constant.USER_SESSION];
                if (sessionUser != null)
                {
                    var userLogin = new UserDraw().getByIDLogin(sessionUser.userId);
                    ViewBag.LoginUser = userLogin;
                }
                ViewBag.listCart = cart;

                return View("PaymentMoMo", cart);
            }

            // Tổng tiền đã được tính bên View (giá sp + phí ship)
            string sumOrderStr = Request["sumOrder"];
            if (string.IsNullOrEmpty(sumOrderStr)) sumOrderStr = "0";
            long sumOrder = 0;
            long.TryParse(sumOrderStr, out sumOrder);

            string payment_method = Request["payment_method"] ?? "COD";

            // orderCode dùng CHUNG cho DB + MoMo + VNPAY
            string orderCode = XString.ToStringNospace(DateTime.Now.ToString("yyyyMMddHHmmssfff"));

            // Lưu đơn hàng trước
            var resultOrder = saveOrder(shipName, shipAddress, shipMobile, shipMail, payment_method, orderCode);
            if (!resultOrder)
            {
                return Redirect("/loi-thanh-toan");
            }

            // ================== THANH TOÁN COD ==================
            if (payment_method.Equals("COD"))
            {
                var OrderInfo = new OrderDraw().getOrderByOrderCode(orderCode);
                ViewBag.paymentStatus = OrderInfo.StatusPayment;
                ViewBag.Methodpayment = OrderInfo.DeliveryPaymentMethod;
                ViewBag.Sum = sumOrder;
                Session[CartSession] = null;
                var items = new OrderDraw().getProductByOrder_Details(OrderInfo.IDOder);
                ViewBag.listCart = items;

                return View("oderComplete", OrderInfo);

            }

            // ================== THANH TOÁN MOMO ==================
            if (payment_method.Equals("MOMO"))
            {
                Session[OrderIDDel] = null;

                string endpoint = momoInfo.endpoint;
                string partnerCode = momoInfo.partnerCode;
                string accessKey = momoInfo.accessKey;
                string serectkey = momoInfo.serectkey;
                string orderInfo = momoInfo.orderInfo;
                string returnUrl = momoInfo.returnUrl;
                string notifyurl = momoInfo.notifyurl;

                string amount = sumOrder.ToString();
                string requestId = Guid.NewGuid().ToString();
                string extraData = "";

                // Chuỗi ký để tạo chữ ký
                string rawHash =
     "accessKey=" + accessKey +
     "&amount=" + amount +
     "&extraData=" + extraData +
     "&ipnUrl=" + notifyurl +
     "&orderId=" + orderCode +
     "&orderInfo=" + orderInfo +
     "&partnerCode=" + partnerCode +
     "&redirectUrl=" + returnUrl +
     "&requestId=" + requestId +
     "&requestType=captureWallet";


                MoMoSecurity crypto = new MoMoSecurity();
                string signature = crypto.signSHA256(rawHash, serectkey);

                JObject message = new JObject
{
    { "partnerCode", partnerCode },
    { "requestId", requestId },
    { "amount", amount },
    { "orderId", orderCode },
    { "orderInfo", orderInfo },
    { "redirectUrl", returnUrl },
    { "ipnUrl", notifyurl },
    { "lang", "vi" },
    { "extraData", "" },
    { "requestType", "captureWallet" },
    { "signature", signature }
};


                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                string responseFromMomo = PayMentRequest.sendPaymentRequest(endpoint, message.ToString());
                JObject jmessage = JObject.Parse(responseFromMomo);

                // In ra log để debug nếu cần
                System.Diagnostics.Debug.WriteLine("MoMo response: " + jmessage.ToString());

                // 1. Đọc errorCode
                // ==== API MOMO V2 ====

                // Lấy resultCode
                var resultCode = (int?)jmessage["resultCode"] ?? -1;
                var messageMoMo = (string)jmessage["message"];

                // Nếu có lỗi
                if (resultCode != 0)
                {
                    ViewBag.Error = "MoMo trả về lỗi: " + resultCode + " - " + messageMoMo;
                    ViewBag.MoMoRaw = jmessage.ToString();

                    var sessionUser = (UserLogin)Session[Constant.USER_SESSION];
                    if (sessionUser != null)
                    {
                        var userLogin = new UserDraw().getByIDLogin(sessionUser.userId);
                        ViewBag.LoginUser = userLogin;
                    }

                    ViewBag.listCart = cart;
                    return View("PaymentMoMo", cart);
                }


                // 2. Nếu errorCode = 0 → lấy payUrl
                var payUrlToken = jmessage["payUrl"];
                if (payUrlToken == null || string.IsNullOrWhiteSpace(payUrlToken.ToString()))
                {
                    // MoMo bảo Success nhưng không trả URL
                    ViewBag.Error = "MoMo không trả về URL thanh toán.\nResponse: " + jmessage.ToString();
                    ViewBag.MoMoRaw = jmessage.ToString();

                    var sessionUser = (UserLogin)Session[Constant.USER_SESSION];
                    if (sessionUser != null)
                    {
                        var userLogin = new UserDraw().getByIDLogin(sessionUser.userId);
                        ViewBag.LoginUser = userLogin;
                    }
                    ViewBag.listCart = cart;

                    return View("PaymentMoMo", cart);
                }

                // 3. Lưu lại orderCode để nếu hủy thì xóa
                Session[OrderIDDel] = orderCode;

                // 4. Lúc này mới chắc chắn có URL → Redirect
                return Redirect(payUrlToken.ToString());

            }

            // ================== THANH TOÁN ATM (VNPAY) ==================
            if (payment_method.Equals("ATM_ONLINE"))
            {
                // sumOrder là VND → VNPAY yêu cầu nhân 100
                long vnpAmount = sumOrder * 100;

                var OrderInfo = new OrderDraw().getOrderByOrderCode(orderCode);
                ViewBag.paymentStatus = OrderInfo.StatusPayment;
                ViewBag.Methodpayment = OrderInfo.DeliveryPaymentMethod;
                ViewBag.Sum = sumOrder;
                Session[CartSession] = null;
                Session[OrderIDDel] = orderCode;

                //Build URL for VNPAY
                string vnp_Returnurl = ConfigurationManager.AppSettings["vnp_Returnurl"]; //URL nhận kết quả trả về 
                string vnp_Url = ConfigurationManager.AppSettings["vnp_Url"]; //URL thanh toán của VNPAY 
                string vnp_TmnCode = ConfigurationManager.AppSettings["vnp_TmnCode"]; //Mã website
                string vnp_HashSecret = ConfigurationManager.AppSettings["vnp_HashSecret"]; //Chuỗi bí mật

                VnPayLibrary vnpay = new VnPayLibrary();

                vnpay.AddRequestData("vnp_Version", VnPayLibrary.VERSION);
                vnpay.AddRequestData("vnp_Command", "pay");
                vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
                vnpay.AddRequestData("vnp_Amount", vnpAmount.ToString());
                vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
                vnpay.AddRequestData("vnp_CurrCode", "VND");
                vnpay.AddRequestData("vnp_IpAddr", Utils.GetIpAddress());
                vnpay.AddRequestData("vnp_Locale", "vn");
                vnpay.AddRequestData("vnp_OrderInfo", "Thanh toan don hang:" + orderCode);
                vnpay.AddRequestData("vnp_OrderType", "other"); //default value: other
                vnpay.AddRequestData("vnp_ReturnUrl", vnp_Returnurl);
                vnpay.AddRequestData("vnp_TxnRef", orderCode); // Mã tham chiếu đơn hàng

                string paymentUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);
                Response.Redirect(paymentUrl);
            }

            // Nếu không rơi vào 3 loại trên thì quay về giỏ hàng
            return RedirectToAction("Index", "Cart");
        }

        public ActionResult Success(Orders OrderInfo)
        {
            return View(OrderInfo);
        }

        // VNPAY return URL
        public ActionResult confirm_orderPaymentOnline()
        {
            // VNPAY trả về:
            // vnp_ResponseCode == "00"  -> thành công
            // khác "00" -> thất bại / hủy

            string responseCode = Request["vnp_ResponseCode"];
            string orderCode = Request["vnp_TxnRef"]; // mã đơn hàng đã gửi sang
            string amountStr = Request["vnp_Amount"]; // đã nhân 100

            if (responseCode == "00")
            {
                var OrderInfo = new OrderDraw().getOrderByOrderCode(orderCode);
                if (OrderInfo != null)
                {
                    // Cập nhật tồn kho theo Order_Detail
                    var order_detail = new OrderDraw().getProductByOrder_Details(OrderInfo.IDOder);
                    foreach (var item in order_detail)
                    {
                        new SanphamDraw().UpdateTonKho(item.ProductID, (int)item.Quanlity);
                    }

                    // Thanh toán thành công
                    OrderInfo.StatusPayment = 1;
                    new OrderDraw().UpdateTrangThaiThanhToan(OrderInfo);

                    long amount = 0;
                    long.TryParse(amountStr, out amount);
                    // vnp_Amount = tiền * 100
                    ViewBag.Sum = amount / 100;

                    Session[CartSession] = null;
                    var items = new OrderDraw().getProductByOrder_Details(OrderInfo.IDOder);
                    ViewBag.listCart = items;

                    return View("oderComplete", OrderInfo);

                }

                ViewBag.status = true;
                return View();
            }
            else
            {
                ViewBag.status = false;
                return View("cancel_order");
            }
        }

        // MOMO return URL
        public ActionResult confirm_orderPaymentOnline_momo()
        {
            string resultCode = Request["resultCode"];
            string orderCode = Request["orderId"];
            string amountStr = Request["amount"];

            if (resultCode == "0")
            {
                var OrderInfo = new OrderDraw().getOrderByOrderCode(orderCode);
                var order_detail = new OrderDraw().getProductByOrder_Details(OrderInfo.IDOder);

                foreach (var item in order_detail)
                {
                    new SanphamDraw().UpdateTonKho(item.ProductID, (int)item.Quanlity);
                }

                OrderInfo.StatusPayment = 1;
                new OrderDraw().UpdateTrangThaiThanhToan(OrderInfo);

                long amount = 0;
                long.TryParse(amountStr, out amount);
                ViewBag.Sum = amount;

                Session[CartSession] = null;

                return View("oderComplete", OrderInfo);
            }
            else
            {
                return View("cancel_order_momo");
            }
        }


        public bool saveOrder(string shipName, string shipAddress, string shipMobile, string shipMail, string payment_method, string oderCode)
        {
            var userSession = (UserLogin)Session[Common.Constant.USER_SESSION];
            var order = new Orders();
            order.NgayTao = DateTime.Now;
            order.ShipName = shipName;
            order.ShipAddress = shipAddress;
            order.ShipEmail = shipMail;
            if (userSession != null)
            {
                order.CustomerID = userSession.userId;
            }
            order.ShipMobile = shipMobile;
            order.Status = 0;
            order.NhanHang = 0;
            order.GiaoHang = 0;

            if (payment_method.Equals("MOMO"))
            {
                order.DeliveryPaymentMethod = "Cổng thanh toán momo";
                order.OrderCode = oderCode;
            }
            if (payment_method.Equals("COD"))
            {
                order.DeliveryPaymentMethod = "COD";
                order.OrderCode = oderCode;
            }
            if (payment_method.Equals("ATM_ONLINE"))
            {
                order.DeliveryPaymentMethod = "ATM";
                order.OrderCode = oderCode;
            }
            if (payment_method.Equals("NL"))
            {
                order.DeliveryPaymentMethod = "Ngân Lượng";
                order.OrderCode = oderCode;
            }

            // 2 = chờ thanh toán (MOMO/VNPAY)
            order.StatusPayment = 2;

            var total = 0;
            var result = false;
            try
            {
                var detailDraw = new Order_DetailDraw();
                var idOrder = new OrderDraw().Insert(order);
                var cartItemProduct = (List<CartItem>)Session[CartSession];
                foreach (var item in cartItemProduct)
                {
                    //Insert Oder_Details
                    var order_Detail = new Order_Detail();
                    order_Detail.ProductID = item.Product.IDContent;
                    order_Detail.OderID = idOrder;
                    order_Detail.Quanlity = item.Quantity;
                    total += (item.Product.GiaTien * item.Quantity);
                    int temp = 0;
                    if (item.Product.PriceSale != null)
                    {
                        temp = (((int)item.Product.GiaTien) - ((int)item.Product.GiaTien / 100 * (int)item.Product.PriceSale));
                    }
                    else
                    {
                        temp = (int)item.Product.GiaTien;
                    }
                    order_Detail.Price = temp;

                    int topHot = (item.Product.Tophot + 1);
                    int soLuongUpdate = (item.Product.Soluong - item.Quantity);
                    var resTop = new SanphamDraw().UpdateTopHot(item.Product.IDContent, topHot);
                    //Update soluong moi
                    var rs = new SanphamDraw().UpdateSoLuong(item.Product.IDContent, soLuongUpdate);
                    result = detailDraw.Insert(order_Detail);
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public ActionResult cancel_order_momo()
        {
            if (Session[OrderIDDel] != null)
            {
                string orderCode = Session[OrderIDDel].ToString();
                var OrderInfo = new OrderDraw().getOrderByOrderCode(orderCode);
                new OrderDraw().Delete(OrderInfo.IDOder);
                Session[OrderIDDel] = null;
                ViewBag.status = false;
            }
            return View();
        }

        public ActionResult cancel_order()
        {
            if (Session[OrderIDDel] != null)
            {
                string orderCode = Session[OrderIDDel].ToString();
                var OrderInfo = new OrderDraw().getOrderByOrderCode(orderCode);
                new OrderDraw().Delete(OrderInfo.IDOder);
                Session[OrderIDDel] = null;
                ViewBag.status = false;
            }
            return View();
        }
    }
}
