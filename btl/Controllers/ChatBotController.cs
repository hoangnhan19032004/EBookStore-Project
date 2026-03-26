using System.Threading.Tasks;
using System.Web.Mvc;
using BaiTapLon.Services;

namespace BaiTapLon.Controllers
{
    public class ChatBotController : Controller
    {
        private readonly OpenAIService _openAIService;

        public ChatBotController()
        {
            _openAIService = new OpenAIService();
        }

        // ============================
        // API nhận câu hỏi từ chatbot
        // ============================
        [HttpPost]
        public ActionResult Ask(string message) // ✅ BỎ async + Task
        {
            // ✅ Chặn input rỗng
            if (string.IsNullOrWhiteSpace(message))
            {
                return Json(new
                {
                    reply = "😊 Bạn hãy nhập câu hỏi về sách, giá tiền, tác giả hoặc thể loại nhé!"
                });
            }

            // ===========================================
            // 👉 CHỌN 1 TRONG 2 CHẾ ĐỘ
            // ===========================================

            // ---------- ✅ CHẠY AI THẬT (có API key + quota) ----------
            // var answer = await _openAIService.AskAsync(message);

            // ---------- ✅ DEMO OFFLINE (không cần OpenAI) ----------
            var answer = FakeReply(message);

            // ✅ Trả JSON về frontend
            return Json(new
            {
                reply = answer
            });
        }

        // ===========================================
        // 🤖 BOT DEMO OFFLINE (KHÔNG CẦN OPENAI)
        // ===========================================
        private string FakeReply(string msg)
        {
            msg = msg.ToLower();

            // ====== CHÀO HỎI ======
            if (msg.Contains("xin chào") || msg.Contains("chào") || msg.Contains("hi") || msg.Contains("hello"))
                return "👋 Xin chào! Mình là **EBookBot** – là một trợ lý tư vấn sách của bạn. Mình có thể giúp bạn:\n" +
                       "🔍 Tìm sách theo tên\n" +
                       "📚 Gợi ý theo thể loại\n" +
                       "💰 Hỏi giá\n" +
                       "🚚 Tìm thông tin giao hàng\n" +
                       "👉 Bạn muốn tìm sách gì hôm nay?";

            // ====== SMALL TALK ======
            if (msg.Contains("bạn là ai"))
                return "🤖 Mình là **EBookBot** – trợ lý bán sách ảo của website. Mình giúp bạn tìm sách nhanh & tiện hơn 😊";

            if (msg.Contains("bạn tên là gì"))
                return "✨ Mình tên là **EBookBot** – người bạn đồng hành của mọi mọt sách 📖";

            if (msg.Contains("cảm ơn") || msg.Contains("thank"))
                return "💙 Không có gì đâu! Rất vui được hỗ trợ bạn. Nếu cần tìm sách khác, cứ hỏi mình nhé!";

            if (msg.Contains("tạm biệt") || msg.Contains("bye"))
                return "👋 Tạm biệt bạn! Chúc bạn tìm được cuốn sách thật ưng ý. Hẹn gặp lại!";

            // ====== TÌM SÁCH THEO NGÔN NGỮ ======
            if (msg.Contains("c#") || msg.Contains("c sharp"))
                return "📘 Shop đang có:\n" +
                       "- *Lập trình C# căn bản* – 150.000đ\n" +
                       "- *C# nâng cao & thực hành dự án* – 220.000đ";

            if (msg.Contains("java"))
                return "📙 Gợi ý cho bạn:\n" +
                       "- *Java cho người mới bắt đầu* – 170.000đ\n" +
                       "- *Lập trình Java OOP* – 240.000đ";

            if (msg.Contains("python"))
                return "📗 Python đang rất hot:\n" +
                       "- *Python căn bản* – 160.000đ\n" +
                       "- *Python cho Data Science* – 290.000đ";

            if (msg.Contains("web") || msg.Contains("html"))
                return "💻 Web dev có:\n" +
                       "- *HTML & CSS cho người mới* – 120.000đ\n" +
                       "- *JavaScript nâng cao* – 190.000đ";

            // ====== THỂ LOẠI ======
            if (msg.Contains("tiểu thuyết"))
                return "📖 Bestseller:\n" +
                       "- *Nhà giả kim*\n" +
                       "- *Đắc nhân tâm*\n" +
                       "- *Tôi thấy hoa vàng trên cỏ xanh*";

            if (msg.Contains("kinh doanh"))
                return "📊 Sách kinh doanh nổi bật:\n" +
                       "- *Cha giàu cha nghèo*\n" +
                       "- *Tư duy nhanh & chậm*\n" +
                       "- *Đời ngắn đừng ngủ dài*";

            if (msg.Contains("self-help") || msg.Contains("tâm lý"))
                return "🌱 Sách phát triển bản thân:\n" +
                       "- *Dám bị ghét*\n" +
                       "- *Atomic Habits*\n" +
                       "- *Hiểu về trái tim*";

            // ====== GIÁ CẢ ======
            if (msg.Contains("giá") || msg.Contains("bao nhiêu"))
                return "💰 Giá sách dao động từ **50.000đ – 350.000đ/cuốn**.\n" +
                       "👉 Bạn đang quan tâm cuốn nào để mình báo giá chính xác hơn?";

            // ====== VẬN CHUYỂN ======
            if (msg.Contains("ship") || msg.Contains("giao") || msg.Contains("vận chuyển"))
                return "🚚 Ship toàn quốc.\n" +
                       "- Nội thành: 1–2 ngày\n" +
                       "- Ngoại tỉnh: 2–4 ngày\n" +
                       "💸 Phí vận chuyển: **30.000đ/lần**.";

            // ====== THANH TOÁN ======
            if (msg.Contains("thanh toán") || msg.Contains("trả"))
                return "💳 Shop hỗ trợ:\n" +
                       "- COD (trả tiền khi nhận hàng)\n" +
                       "- Chuyển khoản ngân hàng\n" +
                       "- Ví **VNPAY**";

            // ====== GIỜ LÀM VIỆC ======
            if (msg.Contains("giờ") || msg.Contains("mở cửa"))
                return "⏰ Shop hoạt động: **8h – 21h mỗi ngày**, kể cả cuối tuần.";

            // ====== TÁC GIẢ ======
            if (msg.Contains("tác giả"))
                return "🖊️ Một số tác giả nổi tiếng:\n" +
                       "- Paulo Coelho\n" +
                       "- Dale Carnegie\n" +
                       "- Nguyễn Nhật Ánh\n" +
                       "- Robin Sharma";

            // ====== LIÊN HỆ ======
            if (msg.Contains("liên hệ") || msg.Contains("hotline") || msg.Contains("số điện thoại"))
                return "📞 Hotline CSKH: **0909 123 456**\n" +
                       "📧 Email: nhasachebook@gmail.com";

            // ====== GỢI Ý TỰ ĐỘNG ======
            if (msg.Contains("gợi ý") || msg.Contains("nên đọc"))
                return "✨ Bạn có thể thử:\n" +
                       "📖 *Đắc Nhân Tâm*\n" +
                       "📗 *Atomic Habits*\n" +
                       "📘 *Lập trình C# căn bản*\n" +
                       "👉 Bạn muốn đọc thể loại nào?";

            // ====== KHÔNG HIỂU ======
            return "🤔 EBookBot belum hiểu lắm.\n" +
                   "👉 Bạn có thể hỏi mình về:\n" +
                   "• Sách lập trình C#, Java, Python\n" +
                   "• Tiểu thuyết, kinh doanh, self-help\n" +
                   "• Giá sách & vận chuyển\n" +
                   "• Thanh toán & liên hệ";
        }

    }
}
