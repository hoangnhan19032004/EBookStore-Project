const cbBody = document.getElementById("cb-body");
const cbText = document.getElementById("cb-text");
const chatbot = document.getElementById("chatbot");

function toggleChat() {
    chatbot.style.display =
        chatbot.style.display === "block"
            ? "none" : "block";
}

function sendMsg() {

    let msg = cbText.value.trim();
    if (!msg) return;

    append("user", msg);

    const reply = botReply(msg);

    setTimeout(() => {
        append("bot", reply);
    }, 600);

    cbText.value = "";
}

function append(type, text) {
    cbBody.innerHTML +=
        `<div class='${type}'>${text}</div>`;
    cbBody.scrollTop = cbBody.scrollHeight;
}

function botReply(msg) {

    msg = msg.toLowerCase();

    if (msg.includes("giá")) {
        return "📚 Giá sách từ 30.000đ – 350.000đ ạ";
    }

    if (msg.includes("ship")) {
        return "🚚 Ship toàn quốc 30.000đ";
    }

    if (msg.includes("mở") || msg.includes("giờ")) {
        return "🕗 Shop mở cửa từ 8h – 21h hàng ngày";
    }

    if (msg.includes("thanh toán")) {
        return "💳 Thanh toán: VNPAY, chuyển khoản, COD";
    }

    if (msg.includes("liên hệ")) {
        return "📞 Hotline: 0909 xxx xxx";
    }

    return "😅 Em chưa hiểu ý anh/chị rồi, hỏi lại giúp em nha.";
}
