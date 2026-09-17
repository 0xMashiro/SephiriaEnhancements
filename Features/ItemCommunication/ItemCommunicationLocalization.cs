using System;
using System.Collections.Generic;
using SephiriaEnhancements.Configuration;

namespace SephiriaEnhancements.ItemCommunication
{
    internal static class ItemCommunicationLocalization
    {
        internal const string Name = "SephiriaEnhancements.ItemCommunication.Name";
        internal const string RewardAction = "SephiriaEnhancements.ItemCommunication.RewardAction";
        internal const string BuyAction = "SephiriaEnhancements.ItemCommunication.BuyAction";
        internal const string ShareAction = "SephiriaEnhancements.ItemCommunication.ShareAction";
        internal const string RewardMessage = "SephiriaEnhancements.ItemCommunication.RewardMessage";
        internal const string BuyMessage = "SephiriaEnhancements.ItemCommunication.BuyMessage";
        internal const string ShareMessage = "SephiriaEnhancements.ItemCommunication.ShareMessage";
        internal const string Voucher = "SephiriaEnhancements.ItemCommunication.Voucher";
        internal const string SelectItem = "SephiriaEnhancements.ItemCommunication.SelectItem";
        internal const string Unavailable = "SephiriaEnhancements.ItemCommunication.Unavailable";
        internal const string Cooldown = "SephiriaEnhancements.ItemCommunication.Cooldown";
        internal const string TooLong = "SephiriaEnhancements.ItemCommunication.TooLong";
        internal const string Help = "SephiriaEnhancements.ItemCommunication.Help";
        private static readonly string[] Keys = { Name, RewardAction, BuyAction, ShareAction, RewardMessage, BuyMessage, ShareMessage, Voucher, SelectItem, Unavailable, Cooldown, TooLong, Help };
        private static readonly Dictionary<string, string[]> Texts = new()
        {
            ["en-US"] = new[] { "Item chat", "Ask who needs this", "Ask someone to buy", "Share product", "I can choose {0}. Anyone need it?", "I want {0}. My price: {1}; negotiation: {2}. Who can buy for less?", "For sale: {0}. My price: {1}.", "can use a trade voucher", "Select a reward or shop item", "This item cannot be shared right now.", "Please wait before sending again.", "This quote is too long to share in chat.", "Sends chat only; does not take or buy the item." },
            ["zh-CN"] = new[] { "物品交流", "询问谁需要", "询问代买", "分享商品", "我可以选：{0}。有人需要吗？", "我想要：{0}。我的购买价：{1}；谈判：{2}。谁买更划算？", "商店在售：{0}。我的购买价：{1}。", "可使用交易券", "请选择奖励或商人商品", "现在无法分享这件物品。", "请稍候再发送。", "报价过长，无法通过聊天发送。", "只发送聊天，不会领取或购买物品。" },
            ["zh-TW"] = new[] { "物品交流", "詢問誰需要", "詢問代買", "分享商品", "我可以選：{0}。有人需要嗎？", "我想要：{0}。我的購買價：{1}；談判：{2}。誰買更划算？", "商店在售：{0}。我的購買價：{1}。", "可使用交易券", "請選擇獎勵或商人商品", "現在無法分享這件物品。", "請稍候再傳送。", "報價過長，無法透過聊天傳送。", "只傳送聊天，不會領取或購買物品。" },
            ["ja-JP"] = new[] { "アイテムの相談", "欲しい人を聞く", "購入を頼む", "商品を共有", "{0}を選べます。欲しい人はいますか？", "{0}が欲しいです。私の価格：{1}、交渉：{2}。安く買える人はいますか？", "販売中：{0}。私の価格：{1}。", "取引券を使用可能", "報酬か店の商品を選んでください", "今はこのアイテムを共有できません。", "少し待ってから送信してください。", "価格情報が長すぎて送信できません。", "チャットのみ送信します。取得や購入はしません。" },
            ["ko-KR"] = new[] { "아이템 대화", "필요한 사람 묻기", "대신 구매 요청", "상품 공유", "{0} 선택 가능. 필요한 분 있나요?", "{0} 원해요. 제 가격: {1}, 협상: {2}. 더 싸게 살 분 있나요?", "판매 중: {0}. 제 가격: {1}.", "거래권 사용 가능", "보상이나 상점 상품을 선택하세요", "지금은 이 아이템을 공유할 수 없습니다.", "잠시 후 다시 보내세요.", "가격 정보가 너무 길어 보낼 수 없습니다.", "채팅만 보냅니다. 아이템을 받거나 구매하지 않습니다." },
            ["de-DE"] = new[] { "Gegenstände besprechen", "Wer braucht das?", "Um Kauf bitten", "Ware teilen", "Ich kann {0} wählen. Braucht das jemand?", "Ich möchte {0}. Mein Preis: {1}; Verhandlung: {2}. Wer kauft günstiger?", "Im Angebot: {0}. Mein Preis: {1}.", "Handelsgutschein nutzbar", "Belohnung oder Händlerware auswählen", "Dieser Gegenstand lässt sich gerade nicht teilen.", "Bitte vor dem erneuten Senden kurz warten.", "Diese Preisangabe ist zu lang für den Chat.", "Sendet nur Chat; nimmt oder kauft den Gegenstand nicht." },
            ["es-ES"] = new[] { "Chat de objetos", "¿Quién lo necesita?", "Pedir que lo compren", "Compartir producto", "Puedo elegir {0}. ¿Alguien lo necesita?", "Quiero {0}. Mi precio: {1}; negociación: {2}. ¿Quién compra más barato?", "En venta: {0}. Mi precio: {1}.", "puedo usar un vale de comercio", "Selecciona una recompensa o producto", "Ahora no se puede compartir este objeto.", "Espera antes de volver a enviar.", "El precio es demasiado largo para el chat.", "Solo envía un mensaje; no recoge ni compra el objeto." },
            ["fr-FR"] = new[] { "Discussion d’objets", "Qui en a besoin ?", "Demander un achat", "Partager le produit", "Je peux choisir {0}. Quelqu’un en a besoin ?", "Je veux {0}. Mon prix : {1} ; négociation : {2}. Qui peut payer moins ?", "En vente : {0}. Mon prix : {1}.", "bon d’échange utilisable", "Sélectionnez une récompense ou un produit", "Impossible de partager cet objet pour le moment.", "Patientez avant de renvoyer un message.", "Ce prix est trop long pour le chat.", "Envoie un message uniquement ; ne prend ni n’achète l’objet." },
            ["it-IT"] = new[] { "Chat degli oggetti", "Chi ne ha bisogno?", "Chiedi di comprarlo", "Condividi prodotto", "Posso scegliere {0}. Serve a qualcuno?", "Vorrei {0}. Mio prezzo: {1}; negoziazione: {2}. Chi paga meno?", "In vendita: {0}. Mio prezzo: {1}.", "posso usare un buono scambio", "Seleziona una ricompensa o un prodotto", "Al momento non puoi condividere questo oggetto.", "Attendi prima di inviare di nuovo.", "Il prezzo è troppo lungo per la chat.", "Invia solo un messaggio; non ritira né compra l’oggetto." },
            ["pl-PL"] = new[] { "Rozmowa o przedmiotach", "Komu to potrzebne?", "Poproś o zakup", "Udostępnij towar", "Mogę wybrać {0}. Ktoś potrzebuje?", "Chcę {0}. Moja cena: {1}; negocjacje: {2}. Kto kupi taniej?", "Na sprzedaż: {0}. Moja cena: {1}.", "mogę użyć bonu handlowego", "Wybierz nagrodę lub towar", "Nie można teraz udostępnić tego przedmiotu.", "Poczekaj przed ponownym wysłaniem.", "Ta cena jest za długa do wysłania na czacie.", "Wysyła tylko wiadomość; nie odbiera ani nie kupuje przedmiotu." },
            ["pt-BR"] = new[] { "Conversa sobre itens", "Quem precisa?", "Pedir uma compra", "Compartilhar produto", "Posso escolher {0}. Alguém precisa?", "Quero {0}. Meu preço: {1}; negociação: {2}. Quem compra mais barato?", "À venda: {0}. Meu preço: {1}.", "posso usar um vale de troca", "Selecione uma recompensa ou produto", "Não é possível compartilhar este item agora.", "Espere antes de enviar novamente.", "O preço é longo demais para o chat.", "Só envia uma mensagem; não pega nem compra o item." },
            ["ru-RU"] = new[] { "Обсуждение предметов", "Кому это нужно?", "Попросить купить", "Показать товар", "Могу выбрать {0}. Кому-нибудь нужно?", "Хочу {0}. Моя цена: {1}; переговоры: {2}. Кто купит дешевле?", "В продаже: {0}. Моя цена: {1}.", "могу использовать торговый талон", "Выберите награду или товар", "Сейчас нельзя поделиться этим предметом.", "Подождите перед повторной отправкой.", "Цена слишком длинная для сообщения в чат.", "Только отправляет сообщение; не забирает и не покупает предмет." },
            ["sv-SE"] = new[] { "Prata om föremål", "Vem behöver detta?", "Be någon köpa", "Dela vara", "Jag kan välja {0}. Behöver någon det?", "Jag vill ha {0}. Mitt pris: {1}; förhandling: {2}. Vem köper billigare?", "Till salu: {0}. Mitt pris: {1}.", "kan använda en handelskupong", "Välj en belöning eller butiksvara", "Det går inte att dela föremålet just nu.", "Vänta innan du skickar igen.", "Priset är för långt för chatten.", "Skickar bara chatt; tar eller köper inte föremålet." },
            ["th-TH"] = new[] { "คุยเรื่องไอเทม", "ถามว่าใครต้องการ", "ขอให้ช่วยซื้อ", "แชร์สินค้า", "ฉันเลือก {0} ได้ มีใครต้องการไหม?", "อยากได้ {0} ราคาฉัน: {1} ต่อรอง: {2} ใครซื้อถูกกว่าได้บ้าง?", "ขาย: {0} ราคาฉัน: {1}", "ใช้บัตรแลกเปลี่ยนได้", "เลือกรางวัลหรือสินค้าในร้าน", "ตอนนี้แชร์ไอเทมนี้ไม่ได้", "รอสักครู่ก่อนส่งอีกครั้ง", "ข้อมูลราคายาวเกินกว่าจะส่งในแชต", "ส่งแชตเท่านั้น ไม่รับหรือซื้อไอเทม" },
            ["tr-TR"] = new[] { "Eşya sohbeti", "Kimin ihtiyacı var?", "Satın almasını iste", "Ürünü paylaş", "{0} seçebilirim. İhtiyacı olan var mı?", "{0} istiyorum. Fiyatım: {1}; pazarlık: {2}. Kim daha ucuza alabilir?", "Satılık: {0}. Fiyatım: {1}.", "ticaret kuponu kullanabilirim", "Bir ödül veya mağaza ürünü seç", "Bu eşya şu anda paylaşılamıyor.", "Tekrar göndermeden önce bekle.", "Bu fiyat bilgisi sohbet için çok uzun.", "Yalnızca mesaj gönderir; eşyayı almaz veya satın almaz." },
        };
        internal static void Register(Action<string, string, string> addText, IEnumerable<string> languages) =>
            LocalizationGroup.Register(addText, languages, Keys, Texts);
    }
}
