using System;
using System.Collections.Generic;
using SephiriaEnhancements.Configuration;

namespace SephiriaEnhancements.DeveloperTools
{
    internal static class DeveloperPlayerDamageLocalization
    {
        internal const string SettingDeveloperPlayerDamage =
            "SephiriaEnhancements.Setting.DeveloperPlayerDamage";
        internal const string HelpDeveloperPlayerDamage =
            "SephiriaEnhancements.Help.DeveloperPlayerDamage";

        internal static readonly string[] DeveloperPlayerDamageMultiplierKeys =
        {
            "SephiriaEnhancements.DeveloperPlayerDamage.1x",
            "SephiriaEnhancements.DeveloperPlayerDamage.2x",
            "SephiriaEnhancements.DeveloperPlayerDamage.5x",
            "SephiriaEnhancements.DeveloperPlayerDamage.10x",
            "SephiriaEnhancements.DeveloperPlayerDamage.100x"
        };

        private static readonly Dictionary<string, Dictionary<string, string>>
            DeveloperPlayerDamageTexts =
                new Dictionary<string, Dictionary<string, string>>
                {
                    ["en-US"] = CreateTexts("Player damage multiplier",
                        "Multiplies damage created by the local player and their summoned units. Online clients cannot alter server-authoritative damage. Available only in developer builds."),
                    ["zh-CN"] = CreateTexts("玩家伤害倍率",
                        "放大本地玩家及其召唤单位创建的伤害；联机客户端无法修改由服务器裁定的伤害。仅在开发构建中提供。"),
                    ["zh-TW"] = CreateTexts("玩家傷害倍率",
                        "放大本機玩家及其召喚單位建立的傷害；連線用戶端無法修改由伺服器判定的傷害。僅在開發版本中提供。"),
                    ["ko-KR"] = CreateTexts("플레이어 피해 배율", "로컬 플레이어와 소환수가 생성한 피해를 늘립니다. 온라인 클라이언트에서는 서버가 판정한 피해를 변경할 수 없습니다. 개발 빌드에서만 제공됩니다."),
                    ["ja-JP"] = CreateTexts("プレイヤーのダメージ倍率", "ローカルプレイヤーとその召喚ユニットが生成するダメージを増やします。オンラインのクライアントではサーバーが判定するダメージを変更できません。開発ビルド専用です。"),
                    ["de-DE"] = CreateTexts("Spielerschaden-Multiplikator",
                    "Verstärkt Schaden des lokalen Spielers und seiner beschworenen Einheiten. Online-Clients können serverseitig bestimmten Schaden nicht ändern. Nur in Entwickler-Builds verfügbar."),
                    ["es-ES"] = CreateTexts("Multiplicador de daño del jugador",
                    "Amplifica el daño generado por el jugador local y sus invocaciones. Los clientes en línea no pueden modificar el daño resuelto por el servidor. Solo disponible en versiones de desarrollo."),
                    ["fr-FR"] = CreateTexts("Multiplicateur de dégâts du joueur",
                    "Amplifie les dégâts produits par le joueur local et ses invocations. Les clients en ligne ne peuvent pas modifier les dégâts déterminés par le serveur. Réservé aux versions de développement."),
                    ["it-IT"] = CreateTexts("Moltiplicatore danni del giocatore",
                    "Aumenta i danni generati dal giocatore locale e dalle sue evocazioni. I client online non possono modificare i danni determinati dal server. Disponibile solo nelle versioni di sviluppo."),
                    ["pl-PL"] = CreateTexts("Mnożnik obrażeń gracza", "Zwiększa obrażenia tworzone przez lokalnego gracza i jego przywołane jednostki. Klienci online nie mogą zmieniać obrażeń rozstrzyganych przez serwer. Tylko w wersji deweloperskiej."),
                    ["pt-BR"] = CreateTexts("Multiplicador de dano do jogador",
                    "Amplifica o dano gerado pelo jogador local e suas invocações. Clientes online não podem alterar danos determinados pelo servidor. Disponível apenas em versões de desenvolvimento."),
                    ["ru-RU"] = CreateTexts("Множитель урона игрока", "Усиливает урон, создаваемый локальным игроком и его призванными существами. Сетевые клиенты не могут менять урон, определяемый сервером. Только для сборок разработки."),
                    ["sv-SE"] = CreateTexts("Spelarskademultiplikator", "Ökar skada som skapas av den lokala spelaren och dess frammanade enheter. Onlineklienter kan inte ändra serverbestämd skada. Endast i utvecklarbyggen."),
                    ["th-TH"] = CreateTexts("ตัวคูณดาเมจผู้เล่น", "เพิ่มดาเมจที่ผู้เล่นในเครื่องและยูนิตอัญเชิญสร้างขึ้น ไคลเอนต์ออนไลน์เปลี่ยนดาเมจที่เซิร์ฟเวอร์คำนวณไม่ได้ มีเฉพาะรุ่นพัฒนา"),
                    ["tr-TR"] = CreateTexts("Oyuncu hasar çarpanı", "Yerel oyuncunun ve çağırdığı birimlerin oluşturduğu hasarı artırır. Çevrimiçi istemciler sunucunun belirlediği hasarı değiştiremez. Yalnızca geliştirme sürümlerinde bulunur.")
                };

        private static Dictionary<string, string> CreateTexts(string label, string help)
        {
            return new Dictionary<string, string>
            {
                [SettingDeveloperPlayerDamage] = label,
                [HelpDeveloperPlayerDamage] = help,
                [DeveloperPlayerDamageMultiplierKeys[0]] = "1×",
                [DeveloperPlayerDamageMultiplierKeys[1]] = "2×",
                [DeveloperPlayerDamageMultiplierKeys[2]] = "5×",
                [DeveloperPlayerDamageMultiplierKeys[3]] = "10×",
                [DeveloperPlayerDamageMultiplierKeys[4]] = "100×"
            };
        }

        internal static void Register(Action<string, string, string> addText, IEnumerable<string> languages)
        {
            LocalizationGroup.Register(addText, languages, DeveloperPlayerDamageTexts);
        }
    }
}
