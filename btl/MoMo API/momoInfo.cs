using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BaiTapLon.MoMo_API
{
    public class momoInfo
    {
        
        public static readonly string endpoint = "https://test-payment.momo.vn/v2/gateway/api/create";
        public static readonly string partnerCode = "MOMOFV7H20220629";
        public static readonly string accessKey = "g6PalpwAQHlynvLT";
        public static readonly string serectkey = "DMmHfG6rdwaZEz53IlKggoV82IaCB8PX";
        public static readonly string orderInfo = "Nhà Sách EBook";
        public static readonly string returnUrl = "https://localhost:44350/confirm-orderPaymentOnlineMomo";
        public static readonly string notifyurl = "https://localhost:44350/cancel-orderMomo";

        /*
        public static readonly string endpoint = "https://test-payment.momo.vn/gw_payment/transactionProcessor";
        public static readonly string partnerCode = "MOMOOGKO20210828";
        public static readonly string accessKey = "XAHgqwYkP9MpFtAR";
        public static readonly string serectkey = "DYCxTKskptCiDL4SGI7TQOMPr7DEo2RH";
        public static readonly string orderInfo = "Hiệu sách Đông Triều";
        public static readonly string returnUrl = "https://hieusachviet.site/confirm-orderPaymentOnlineMomo";
        public static readonly string notifyurl = "https://hieusachviet.site/cancel-orderMomo";*/
    }
}