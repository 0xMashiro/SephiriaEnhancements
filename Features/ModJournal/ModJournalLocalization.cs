using System;
using System.Collections.Generic;

namespace SephiriaEnhancements.ModJournal
{
    internal static class ModJournalLocalization
    {
        internal const string About = "SephiriaEnhancements.ModJournal.About";
        internal const string Description = "SephiriaEnhancements.ModJournal.Description";
        internal const string Visit = "SephiriaEnhancements.ModJournal.Visit";
        internal const string Livestream = "SephiriaEnhancements.ModJournal.Livestream";
        internal const string Profile = "SephiriaEnhancements.ModJournal.Profile";

        private static readonly Dictionary<string, string[]> Texts = new()
        {
            ["en-US"] = new[] {
                "About this mod",
                "This mod is fully open source and free. Get it from Nexus Mods or GitHub. If you paid for this mod, you were scammed.\n\nMade with affection and support for streamer Xianyu. {0}",
                "Open {0}", "Xianyu’s livestream", "Xianyu’s Bilibili profile" },
            ["zh-CN"] = new[] {
                "关于本 MOD",
                "本 MOD 完全开源、免费，请从 Nexus Mods 或 GitHub 获取。若你为本 MOD 付过费，你受到了诈骗。\n\n开发者以此 MOD 表达对主播咸鱼（Xianyu）的偏爱与支持。{0}",
                "打开{0}", "咸鱼直播间", "咸鱼的 Bilibili 主页" },
            ["zh-TW"] = new[] {
                "關於本 MOD",
                "本 MOD 完全開源、免費，請從 Nexus Mods 或 GitHub 取得。若你曾付費購買本 MOD，你受到了詐騙。\n\n開發者以此 MOD 表達對實況主鹹魚（Xianyu）的偏愛與支持。{0}",
                "開啟{0}", "鹹魚直播間", "鹹魚的 Bilibili 主頁" },
            ["ko-KR"] = new[] {
                "모드 소개",
                "이 모드는 완전한 오픈 소스이며 무료입니다. Nexus Mods 또는 GitHub에서 받으세요. 이 모드를 유료로 구매했다면 사기를 당한 것입니다.\n\n개발자는 이 모드에 스트리머 Xianyu를 향한 특별한 애정과 응원을 담았습니다. {0}",
                "{0} 열기", "Xianyu 생방송", "Xianyu의 Bilibili 프로필" },
            ["ja-JP"] = new[] {
                "このMODについて",
                "このMODは完全にオープンソースで、無料です。Nexus ModsまたはGitHubから入手してください。このMODにお金を払った場合、詐欺に遭っています。\n\n開発者はこのMODに、配信者の咸魚（Xianyu）への特別な愛情と応援を込めています。{0}",
                "{0}を開く", "Xianyuの配信", "XianyuのBilibiliプロフィール" },
            ["de-DE"] = new[] {
                "Über diese Mod",
                "Diese Mod ist vollständig quelloffen und kostenlos. Lade sie von Nexus Mods oder GitHub herunter. Wenn du für diese Mod bezahlt hast, wurdest du betrogen.\n\nMit besonderer Zuneigung und Unterstützung für den Streamer Xianyu entwickelt. {0}",
                "{0} öffnen", "Xianyus Livestream", "Xianyus Bilibili-Profil" },
            ["es-ES"] = new[] {
                "Acerca del mod",
                "Este mod es totalmente de código abierto y gratuito. Consíguelo en Nexus Mods o GitHub. Si has pagado por este mod, te han estafado.\n\nCreado con especial cariño y apoyo al streamer Xianyu. {0}",
                "Abrir {0}", "Directo de Xianyu", "Perfil de Xianyu en Bilibili" },
            ["fr-FR"] = new[] {
                "À propos du mod",
                "Ce mod est entièrement open source et gratuit. Téléchargez-le sur Nexus Mods ou GitHub. Si vous avez payé pour ce mod, vous avez été victime d’une arnaque.\n\nCréé avec une affection particulière et en soutien au streamer Xianyu. {0}",
                "Ouvrir {0}", "Direct de Xianyu", "Profil Bilibili de Xianyu" },
            ["it-IT"] = new[] {
                "Informazioni sul mod",
                "Questo mod è completamente open source e gratuito. Scaricalo da Nexus Mods o GitHub. Se hai pagato per questo mod, sei stato truffato.\n\nCreato con particolare affetto e sostegno per lo streamer Xianyu. {0}",
                "Apri {0}", "Diretta di Xianyu", "Profilo Bilibili di Xianyu" },
            ["pl-PL"] = new[] {
                "O modyfikacji",
                "Ta modyfikacja jest w pełni otwarta i bezpłatna. Pobierz ją z Nexus Mods lub GitHub. Jeśli za nią zapłacono, doszło do oszustwa.\n\nStworzona ze szczególną sympatią i wsparciem dla streamera Xianyu. {0}",
                "Otwórz {0}", "Transmisja Xianyu", "Profil Xianyu na Bilibili" },
            ["pt-BR"] = new[] {
                "Sobre o mod",
                "Este mod é totalmente de código aberto e gratuito. Obtenha-o no Nexus Mods ou GitHub. Se você pagou por este mod, caiu em um golpe.\n\nCriado com carinho especial e apoio ao streamer Xianyu. {0}",
                "Abrir {0}", "Transmissão de Xianyu", "Perfil de Xianyu no Bilibili" },
            ["ru-RU"] = new[] {
                "О моде",
                "Этот мод полностью бесплатен, а его исходный код открыт. Скачивайте его с Nexus Mods или GitHub. Если вы заплатили за этот мод, вас обманули.\n\nСоздан с особой симпатией и в поддержку стримера Xianyu. {0}",
                "Открыть {0}", "Трансляция Xianyu", "Профиль Xianyu на Bilibili" },
            ["sv-SE"] = new[] {
                "Om modden",
                "Den här modden har helt öppen källkod och är gratis. Hämta den från Nexus Mods eller GitHub. Om du betalade för modden blev du lurad.\n\nSkapad med särskild tillgivenhet och stöd för streamern Xianyu. {0}",
                "Öppna {0}", "Xianyus livesändning", "Xianyus Bilibili-profil" },
            ["th-TH"] = new[] {
                "เกี่ยวกับม็อดนี้",
                "ม็อดนี้เปิดเผยซอร์สโค้ดทั้งหมดและให้ใช้ฟรี ดาวน์โหลดได้จาก Nexus Mods หรือ GitHub หากคุณจ่ายเงินซื้อม็อดนี้ แสดงว่าคุณถูกหลอก\n\nผู้พัฒนาสร้างม็อดนี้เพื่อแสดงความเอ็นดูเป็นพิเศษและสนับสนุนสตรีมเมอร์ Xianyu {0}",
                "เปิด {0}", "ไลฟ์ของ Xianyu", "โปรไฟล์ Bilibili ของ Xianyu" },
            ["tr-TR"] = new[] {
                "Mod hakkında",
                "Bu mod tamamen açık kaynaklı ve ücretsizdir. Nexus Mods veya GitHub’dan edinin. Bu mod için ödeme yaptıysanız dolandırıldınız.\n\nYayıncı Xianyu’ya özel bir sevgi ve destekle geliştirildi. {0}",
                "{0} aç", "Xianyu canlı yayını", "Xianyu’nun Bilibili profili" }
        };

        internal static void Register(Action<string, string, string> addText,
            IEnumerable<string> languages)
        {
            foreach (string language in languages)
            {
                string[] texts = Texts.TryGetValue(language, out var translated)
                    ? translated : Texts["en-US"];
                addText(language, About, texts[0]);
                addText(language, Description, texts[1]);
                addText(language, Visit, texts[2]);
                addText(language, Livestream, texts[3]);
                addText(language, Profile, texts[4]);
            }
        }
    }
}
