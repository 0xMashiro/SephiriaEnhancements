using SephiriaEnhancements.Diagnostics;
using System;
using System.Collections.Generic;
using System.Text;
using SephiriaEnhancements.Combat;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Core;
using SephiriaEnhancements.Integration;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SephiriaEnhancements.Presentation
{
    internal sealed class CombatInsightsHud : IDisposable
    {
        private const float LiveBaseScale = 0.8f;
        private const float PulseWidth = 112f;
        private const float PartyLedgerWidth = 198f;
        private const float BossLedgerWidth = 236f;
        private static readonly Color SoftInk =
            new Color(0.055f, 0.05f, 0.062f, 0.70f);
        private static readonly Color ReportInk =
            new Color(0.045f, 0.041f, 0.052f, 0.90f);
        private static readonly Color ShadowInk =
            new Color(0f, 0f, 0f, 0.34f);
        private static readonly Color Paper =
            new Color(0.92f, 0.94f, 0.96f, 1f);
        private static readonly Color Muted =
            new Color(0.57f, 0.62f, 0.68f, 1f);
        private static readonly Color Quiet =
            new Color(0.39f, 0.43f, 0.48f, 1f);
        private static readonly Color Moss =
            new Color(0.58f, 0.76f, 0.38f, 1f);
        private static readonly Color Brass =
            new Color(0.73f, 0.54f, 0.28f, 1f);
        private static readonly Color LocalWash =
            new Color(0.58f, 0.76f, 0.38f, 0.08f);

        private readonly List<LiveRow> liveRows = new List<LiveRow>(5);
        private readonly List<ReportRow> reportRows = new List<ReportRow>(4);
        private GameObject pulseObject, ledgerObject, reportObject,
            reportShadowObject;
        private RectTransform pulseRect, ledgerRect, liveRowsRoot,
            reportRect, reportShadowRect, reportRowsRoot;
        private CanvasGroup pulseGroup, ledgerGroup, reportGroup,
            reportShadowGroup;
        private TextMeshProUGUI pulseText, liveKicker, liveTotal,
            reportTitle, reportMeta, damageHeading, shareHeading,
            averageDpsHeading, damageMix, localFinalBlows, dismissHint;
        private OutcomeChip normalOutcome, minibossOutcome, bossOutcome;
        private TextMeshProUGUI fontTemplate;
        private float nextLookup, nextProjection, liveReveal, reportReveal;
        private float reportScale = 1f;
        private int lastScale = -1, failedAttachAttempts;
        private bool attachWarningLogged;

        internal bool IsAttached => pulseObject != null;
        internal bool IsReportPresented => IsPresented(reportObject, reportGroup);
        internal bool IsActiveInHierarchy =>
            IsPresented(pulseObject, pulseGroup) ||
            IsPresented(ledgerObject, ledgerGroup) ||
            IsPresented(reportObject, reportGroup);

        internal void Update(bool allowed, CombatInsightsController model)
        {
            float now = Time.unscaledTime;
            if (pulseObject == null && allowed && now >= nextLookup)
            {
                nextLookup = now + 1f;
                TryAttach();
            }
            if (pulseObject == null) return;
            if (lastScale != ModSettings.DamageStatisticsScaleIndex)
                ApplyScale();

            CombatInsightsViewMode mode = allowed
                ? model.ViewMode : CombatInsightsViewMode.Hidden;
            bool showLive = mode == CombatInsightsViewMode.Pulse ||
                mode == CombatInsightsViewMode.Party ||
                mode == CombatInsightsViewMode.Boss;
            bool showReport = mode == CombatInsightsViewMode.Report &&
                model.EncounterReport != null;
            if ((showLive || showReport) && now >= nextProjection)
            {
                nextProjection = now + 0.2f;
                if (showLive) ProjectLive(model, mode);
                if (showReport) ProjectReport(model.EncounterReport);
            }

            liveReveal = Mathf.MoveTowards(liveReveal, showLive ? 1f : 0f,
                Time.unscaledDeltaTime * (showLive ? 9f : 14f));
            reportReveal = Mathf.MoveTowards(reportReveal,
                showReport ? 1f : 0f,
                Time.unscaledDeltaTime * (showReport ? 6f : 10f));
            PresentLive(showLive);
            string closeBinding = showReport && model.CanDismissPresentedReport
                ? NativeReportDismissal.BindingLabel() : string.Empty;
            dismissHint.text = string.IsNullOrEmpty(closeBinding) ? string.Empty
                : string.Format(ModLocalization.Get(ModLocalization.ReportDismissHint),
                    closeBinding);
            PresentReport(showReport);
        }

        internal void AttachBrowser(RectTransform parent, TextMeshProUGUI template)
        {
            fontTemplate = template;
            CreateEncounterReport(parent);
        }

        internal float DrawBrowser(CombatStatisticsSnapshot snapshot, bool floor)
        {
            ProjectReport(snapshot);
            if (floor) reportTitle.text = ModLocalization.Get(ModLocalization.CurrentFloorStatistics);
            dismissHint.text = ModLocalization.Get(floor
                ? ModLocalization.FloorBattleTime : ModLocalization.EncounterBattleTime);
            reportObject.SetActive(true);
            reportShadowObject.SetActive(true);
            reportRect.localScale = reportShadowRect.localScale = Vector3.one;
            reportRect.anchoredPosition = Vector2.zero;
            reportShadowRect.anchoredPosition = new Vector2(0f, -4f);
            reportGroup.alpha = reportShadowGroup.alpha = 1f;
            return reportRect.sizeDelta.y;
        }

        internal void InvalidateLayout() => lastScale = -1;

        internal void Hide()
        {
            liveReveal = reportReveal = 0f;
            HideObjects();
        }

        public void Dispose()
        {
            Destroy(pulseObject);
            Destroy(ledgerObject);
            Destroy(reportObject);
            Destroy(reportShadowObject);
            liveRows.Clear();
            reportRows.Clear();
            pulseObject = ledgerObject = reportObject =
                reportShadowObject = null;
            fontTemplate = null;
        }

        private void TryAttach()
        {
            UIManager manager = UIManager.Instance;
            UI_PlayerMP mp = manager?.GetElement<UI_PlayerMP>();
            UI_HUDMapViewer map = manager?.GetElement<UI_HUDMapViewer>();
            RectTransform mapRect = map?.transform as RectTransform;
            Canvas canvas = mapRect?.GetComponentInParent<Canvas>();
            RectTransform hudRoot = canvas?.rootCanvas?.transform as
                RectTransform;
            TextMeshProUGUI template = mp?.mpBar?.valueText;
            template ??= map?.GetComponentInChildren<TextMeshProUGUI>(true);
            if (template == null || hudRoot == null)
            {
                failedAttachAttempts++;
                if (failedAttachAttempts >= 10 && !attachWarningLogged)
                {
                    attachWarningLogged = true;
                    SupportLogger.Warning("combat_hud_anchors_pending", "[SephiriaEnhancements] Combat Insights " +
                        "HUD anchors are unavailable; retrying once per second.");
                }
                return;
            }

            fontTemplate = template;
            CreatePulse(hudRoot);
            CreateLiveLedger(hudRoot);
            CreateEncounterReport(hudRoot);
            reportShadowRect.SetAsLastSibling();
            reportRect.SetAsLastSibling();
            pulseRect.SetAsLastSibling();
            ledgerRect.SetAsLastSibling();
            ApplyScale();
            SupportLogger.Info("combat_hud_attached", "[SephiriaEnhancements] Responsive Combat Insights HUD " +
                "attached with right-side live statistics and a centered " +
                "encounter report; raycasts disabled.");
        }

        private void CreatePulse(RectTransform parent)
        {
            pulseObject = NewPanel("Sephiria Enhancements — DPS Pulse", parent,
                new Vector2(PulseWidth, 20f), SoftInk,
                out pulseRect, out pulseGroup);
            AnchorRightCenter(pulseRect);
            Image accent = CreateImage("Accent", pulseRect, Moss);
            SetTopRect(accent.rectTransform, 0f, PulseWidth - 2f, 0f, 20f);
            pulseText = CreateText("Pulse", pulseRect, 0.62f,
                TextAlignmentOptions.MidlineLeft, autoSize: false);
            SetTopRect(pulseText.rectTransform, 8f, 6f, 0f, 20f);
            pulseText.color = Paper;
            pulseObject.SetActive(false);
        }

        private void CreateLiveLedger(RectTransform parent)
        {
            ledgerObject = NewPanel("Sephiria Enhancements — Live Combat",
                parent, new Vector2(PartyLedgerWidth, 60f), SoftInk,
                out ledgerRect, out ledgerGroup);
            AnchorRightCenter(ledgerRect);

            liveKicker = CreateText("Kicker", ledgerRect, 0.57f,
                TextAlignmentOptions.BottomLeft, autoSize: true);
            liveKicker.color = Muted;
            liveTotal = CreateText("Total", ledgerRect, 0.57f,
                TextAlignmentOptions.BottomRight, autoSize: true);
            liveTotal.color = Moss;
            Image rule = CreateImage("Rule", ledgerRect, Brass);
            SetTopRect(rule.rectTransform, 6f, 6f, 20f, 1f);

            GameObject rowsObject = new GameObject("Rows",
                typeof(RectTransform));
            liveRowsRoot = rowsObject.GetComponent<RectTransform>();
            liveRowsRoot.SetParent(ledgerRect, false);
            liveRowsRoot.anchorMin = Vector2.zero;
            liveRowsRoot.anchorMax = Vector2.one;
            liveRowsRoot.offsetMin = new Vector2(4f, 4f);
            liveRowsRoot.offsetMax = new Vector2(-4f, -23f);
            ledgerObject.SetActive(false);
        }

        private void CreateEncounterReport(RectTransform parent)
        {
            reportShadowObject = NewPanel(
                "Sephiria Enhancements — Encounter Report Shadow", parent,
                new Vector2(EncounterReportLayout.Width, 180f), ShadowInk,
                out reportShadowRect, out reportShadowGroup);
            reportObject = NewPanel(
                "Sephiria Enhancements — Encounter Report", parent,
                new Vector2(EncounterReportLayout.Width, 180f), ReportInk,
                out reportRect, out reportGroup);
            AnchorCenter(reportShadowRect);
            AnchorCenter(reportRect);

            Image accent = CreateImage("Accent", reportRect, Moss);
            SetTopRect(accent.rectTransform, 0f, 0f, 0f, 2f);
            reportTitle = CreateText("Title", reportRect, 0.72f,
                TextAlignmentOptions.MidlineLeft, autoSize: true);
            reportTitle.color = Paper;
            reportMeta = CreateText("Meta", reportRect, 0.54f,
                TextAlignmentOptions.MidlineRight, autoSize: true);
            reportMeta.color = Muted;

            Image headerRule = CreateImage("Header Rule", reportRect,
                new Color(0.73f, 0.54f, 0.28f, 0.55f));
            SetTopRect(headerRule.rectTransform, 10f, 10f, 23f, 1f);
            damageHeading = CreateText("Damage Heading", reportRect, 0.46f,
                TextAlignmentOptions.MidlineRight, autoSize: true);
            damageHeading.color = Quiet;
            shareHeading = CreateText("Share Heading", reportRect, 0.46f,
                TextAlignmentOptions.MidlineRight, autoSize: true);
            shareHeading.color = Quiet;
            averageDpsHeading = CreateText("Average DPS Heading", reportRect,
                0.46f, TextAlignmentOptions.MidlineRight, autoSize: true);
            averageDpsHeading.color = Quiet;

            GameObject rowsObject = new GameObject("Player Rows",
                typeof(RectTransform));
            reportRowsRoot = rowsObject.GetComponent<RectTransform>();
            reportRowsRoot.SetParent(reportRect, false);
            normalOutcome = new OutcomeChip(reportRect, fontTemplate,
                "Normal Outcome");
            minibossOutcome = new OutcomeChip(reportRect, fontTemplate,
                "Miniboss Outcome");
            bossOutcome = new OutcomeChip(reportRect, fontTemplate,
                "Boss Outcome");
            damageMix = CreateText("Damage Mix", reportRect, 0.49f,
                TextAlignmentOptions.Center, autoSize: true);
            damageMix.color = Muted;
            localFinalBlows = CreateText("Local Final Blows", reportRect,
                0.54f, TextAlignmentOptions.Center, autoSize: true);
            localFinalBlows.color = Moss;
            dismissHint = CreateText("Dismiss Hint", reportRect, 0.49f,
                TextAlignmentOptions.Center, autoSize: true);
            dismissHint.color = Muted;

            reportObject.SetActive(false);
            reportShadowObject.SetActive(false);
        }

        private void ProjectLive(CombatInsightsController model,
            CombatInsightsViewMode mode)
        {
            if (mode == CombatInsightsViewMode.Pulse)
            {
                SetActive(ledgerObject, false);
                SetActive(pulseObject, true);
                pulseText.text = ModLocalization.Get(ModLocalization.Dps) +
                    "  " + DpsFormatter.Compact(model.LocalDps);
                return;
            }

            SetActive(pulseObject, false);
            SetActive(ledgerObject, true);
            if (mode == CombatInsightsViewMode.Boss)
                ProjectBoss(model);
            else
                ProjectParty(model);
        }

        private void ProjectBoss(CombatInsightsController model)
        {
            liveKicker.text = ModLocalization.Get(ModLocalization.DamageShare);
            int count = EnsureLiveRows(model.Players.Count);
            float maximum = 1f;
            for (int index = 0; index < count; index++)
            {
                maximum = Mathf.Max(maximum, model.BossDamage.TryGetValue(
                    model.Players[index].Key, out float damage) ? damage : 0f);
            }
            liveTotal.text = "Σ " + DpsFormatter.Compact(model.BossTotal) +
                "  ·  " + DpsFormatter.Seconds(model.BossElapsed);
            for (int index = 0; index < count; index++)
            {
                CombatInsightsController.PlayerDamageState state =
                    model.Players[index];
                model.BossDamage.TryGetValue(state.Key, out float damage);
                string value = DpsFormatter.Compact(damage) + "  ·  " +
                    DpsFormatter.Percent(damage, model.BossTotal);
                liveRows[index].Show(state.Name, damage, maximum,
                    value, state.IsLocal, index == 0 && damage > 0f);
            }
            ResizeLive(count, BossLedgerWidth, 112f);
        }

        private void ProjectParty(CombatInsightsController model)
        {
            liveKicker.text = "5S DPS";
            int count = EnsureLiveRows(model.Players.Count);
            float maximum = 1f;
            float teamDps = 0f;
            for (int index = 0; index < count; index++)
            {
                maximum = Mathf.Max(maximum,
                    model.Players[index].RollingDps);
                teamDps += model.Players[index].RollingDps;
            }
            liveTotal.text = "Σ " + DpsFormatter.Compact(teamDps);
            for (int index = 0; index < count; index++)
            {
                CombatInsightsController.PlayerDamageState state =
                    model.Players[index];
                liveRows[index].Show(state.Name, state.RollingDps,
                    maximum, DpsFormatter.Compact(state.RollingDps),
                    state.IsLocal, false);
            }
            ResizeLive(count, PartyLedgerWidth, 72f);
        }

        private void ProjectReport(CombatStatisticsSnapshot report)
        {
            string title = ModLocalization.Get(ModLocalization.CombatSummary)
                .ToUpperInvariant();
            reportTitle.text = report is EncounterReportSnapshot encounter && encounter.Kind == EncounterReportKind.Boss
                ? "BOSS  ·  " + title : title;
            reportMeta.text = DpsFormatter.Seconds(report.Duration) +
                "  ·  " + ModLocalization.Get(ModLocalization.Defeated) +
                " ×" + report.DefeatedCount;
            damageHeading.text = ModLocalization.Get(
                ModLocalization.ReportDamage);
            shareHeading.text = ModLocalization.Get(
                ModLocalization.ReportShare);
            averageDpsHeading.text = ModLocalization.Get(
                ModLocalization.ReportAverageDps);

            int count = EnsureReportRows(report.Players.Count);
            float maximum = 1f;
            for (int index = 0; index < count; index++)
                maximum = Mathf.Max(maximum, report.Players[index].Damage);
            for (int index = 0; index < count; index++)
            {
                CombatStatisticsPlayerSnapshot player = report.Players[index];
                reportRows[index].Show(index, player, maximum,
                    report.TotalDamage, report.Duration);
            }

            normalOutcome.Show(ModLocalization.Get(
                ModLocalization.NormalEnemy), report.NormalDefeated);
            minibossOutcome.Show(ModLocalization.Get(
                ModLocalization.MinibossEnemy), report.MinibossDefeated);
            bossOutcome.Show(ModLocalization.Get(
                ModLocalization.BossEnemy), report.BossDefeated);
            damageMix.text = FormatDamageMix(report);
            bool showFinalBlows = report.LocalFinalBlows > 0;
            localFinalBlows.gameObject.SetActive(showFinalBlows);
            if (showFinalBlows)
            {
                localFinalBlows.text = "◆  " + ModLocalization.Get(
                    ModLocalization.FinalBlows) + "  ×" +
                    report.LocalFinalBlows;
            }
            ResizeReport(count, showFinalBlows);
        }

        private int EnsureLiveRows(int requested)
        {
            int count = Mathf.Min(requested, 5);
            while (liveRows.Count < count)
                liveRows.Add(new LiveRow(liveRowsRoot, fontTemplate,
                    liveRows.Count));
            for (int index = 0; index < liveRows.Count; index++)
                liveRows[index].Root.SetActive(index < count);
            return count;
        }

        private int EnsureReportRows(int requested)
        {
            int count = requested;
            while (reportRows.Count < count)
                reportRows.Add(new ReportRow(reportRowsRoot, fontTemplate,
                    reportRows.Count));
            for (int index = 0; index < reportRows.Count; index++)
                reportRows[index].Root.SetActive(index < count);
            return count;
        }

        private void ResizeLive(int count, float width, float valueWidth)
        {
            float height = 25f + count * 16f;
            ledgerRect.sizeDelta = new Vector2(width, height);
            SetTopRect(liveKicker.rectTransform, 7f, width * 0.48f,
                2f, 17f);
            SetTopRect(liveTotal.rectTransform, width * 0.42f, 7f,
                2f, 17f);
            for (int index = 0; index < count; index++)
                liveRows[index].SetLayout(width - 8f, valueWidth, index);
        }

        private void ResizeReport(int count, bool showFinalBlows)
        {
            var layout = new EncounterReportLayout(count, showFinalBlows);
            const float width = EncounterReportLayout.Width;
            const float rowsTop = EncounterReportLayout.RowsTop;
            const float rowHeight = EncounterReportLayout.RowHeight;
            reportRect.sizeDelta = reportShadowRect.sizeDelta =
                new Vector2(width, layout.Height);

            SetTopRect(reportTitle.rectTransform, 10f, width * 0.5f,
                4f, 18f);
            SetTopRect(reportMeta.rectTransform, width * 0.5f, 10f,
                4f, 18f);
            LayoutReportColumns(width, 25f, 10f,
                damageHeading.rectTransform, shareHeading.rectTransform,
                averageDpsHeading.rectTransform);
            SetTopRect(reportRowsRoot, 0f, 0f, rowsTop,
                count * rowHeight);
            for (int index = 0; index < count; index++)
                reportRows[index].SetLayout(width, index, rowHeight);
            SetTopRect(damageMix.rectTransform, 10f, 10f,
                layout.DamageMixTop, 12f);

            float chipGap = 5f;
            float chipWidth = (width - 20f - chipGap * 2f) / 3f;
            normalOutcome.SetLayout(width, 10f, layout.OutcomesTop,
                chipWidth, 25f);
            minibossOutcome.SetLayout(width, 10f + chipWidth + chipGap,
                layout.OutcomesTop, chipWidth, 25f);
            bossOutcome.SetLayout(width,
                10f + (chipWidth + chipGap) * 2f,
                layout.OutcomesTop, chipWidth, 25f);
            SetTopRect(localFinalBlows.rectTransform, 10f, 10f,
                layout.FinalBlowsTop, showFinalBlows ? 14f : 0f);
            SetTopRect(dismissHint.rectTransform, 10f, 10f,
                layout.DismissHintTop, 16f);
        }

        private void ApplyScale()
        {
            float value = ModSettings.DamageStatisticsScale * LiveBaseScale;
            Vector3 liveScale = new Vector3(value, value, 1f);
            if (pulseRect != null) pulseRect.localScale = liveScale;
            if (ledgerRect != null) ledgerRect.localScale = liveScale;
            lastScale = ModSettings.DamageStatisticsScaleIndex;
        }

        private void PresentLive(bool targetVisible)
        {
            pulseGroup.alpha = ledgerGroup.alpha = liveReveal;
            if (!targetVisible && liveReveal <= 0.001f)
            {
                SetActive(pulseObject, false);
                SetActive(ledgerObject, false);
            }
        }

        private void PresentReport(bool targetVisible)
        {
            if (targetVisible || reportReveal > 0.001f)
            {
                SetActive(reportShadowObject, true);
                SetActive(reportObject, true);
            }
            float eased = 1f - Mathf.Pow(1f - reportReveal, 3f);
            Rect canvas = ((RectTransform)reportRect.parent).rect;
            reportScale = EncounterReportLayout.FitScale(canvas.width, canvas.height,
                reportRect.sizeDelta.y, ModSettings.DamageStatisticsScale);
            float entranceScale = Mathf.Lerp(0.965f, 1f, eased) *
                reportScale;
            Vector3 scale = new Vector3(entranceScale, entranceScale, 1f);
            reportRect.localScale = reportShadowRect.localScale = scale;
            reportRect.anchoredPosition = new Vector2(0f,
                Mathf.Lerp(-4f, 0f, eased));
            reportShadowRect.anchoredPosition = reportRect.anchoredPosition +
                new Vector2(0f, -4f);
            reportGroup.alpha = reportReveal;
            reportShadowGroup.alpha = reportReveal * 0.8f;
            if (!targetVisible && reportReveal <= 0.001f)
            {
                SetActive(reportObject, false);
                SetActive(reportShadowObject, false);
            }
        }

        private void HideObjects()
        {
            SetActive(pulseObject, false);
            SetActive(ledgerObject, false);
            SetActive(reportObject, false);
            SetActive(reportShadowObject, false);
        }

        private static void LayoutReportColumns(float panelWidth, float top,
            float height, RectTransform damage, RectTransform share,
            RectTransform averageDps)
        {
            const float padding = 14f;
            const float damageWidth = 44f;
            const float shareWidth = 32f;
            const float dpsWidth = 48f;
            const float gap = 5f;
            float dpsLeft = panelWidth - padding - dpsWidth;
            float shareLeft = dpsLeft - gap - shareWidth;
            float damageLeft = shareLeft - gap - damageWidth;
            SetTopRect(damage, damageLeft,
                panelWidth - damageLeft - damageWidth, top, height);
            SetTopRect(share, shareLeft,
                panelWidth - shareLeft - shareWidth, top, height);
            SetTopRect(averageDps, dpsLeft, padding, top, height);
        }

        private static string FormatDamageMix(CombatStatisticsSnapshot report)
        {
            var result = new StringBuilder();
            result.Append(ModLocalization.Get(ModLocalization.ReportDamageMix));
            float otherDamage = 0f;
            int shown = 0;
            for (int index = 0; index < report.DamageTypes.Count; index++)
            {
                CombatStatisticsDamageTypeSnapshot item =
                    report.DamageTypes[index];
                if (item.Type == EncounterDamageType.Unknown || shown >= 3)
                {
                    otherDamage += item.Damage;
                    continue;
                }
                result.Append("  ·  ");
                result.Append(DamageTypeName(item.Type));
                result.Append(' ');
                result.Append(DpsFormatter.Percent(item.Damage,
                    report.TotalDamage));
                shown++;
            }
            if (otherDamage > 0f || shown == 0)
            {
                result.Append("  ·  ");
                result.Append(ModLocalization.Get(ModLocalization.DamageOther));
                result.Append(' ');
                result.Append(DpsFormatter.Percent(
                    otherDamage > 0f ? otherDamage : report.TotalDamage,
                    report.TotalDamage));
            }
            return result.ToString();
        }

        private static string DamageTypeName(EncounterDamageType type)
        {
            switch (type)
            {
                case EncounterDamageType.Physical:
                    return ModLocalization.Get(ModLocalization.DamagePhysical);
                case EncounterDamageType.Fire:
                    return ModLocalization.Get(ModLocalization.DamageFire);
                case EncounterDamageType.Ice:
                    return ModLocalization.Get(ModLocalization.DamageIce);
                case EncounterDamageType.Lightning:
                    return ModLocalization.Get(ModLocalization.DamageLightning);
                case EncounterDamageType.Chaos:
                    return ModLocalization.Get(ModLocalization.DamageChaos);
                case EncounterDamageType.Normal:
                    return ModLocalization.Get(ModLocalization.DamageNormal);
                case EncounterDamageType.Mixed:
                    return ModLocalization.Get(ModLocalization.DamageMixed);
                default:
                    return ModLocalization.Get(ModLocalization.DamageOther);
            }
        }

        private static void AnchorRightCenter(RectTransform rect)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-18f, 0f);
        }

        private static void AnchorCenter(RectTransform rect)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 38f);
        }

        private static GameObject NewPanel(string name, RectTransform parent,
            Vector2 size, Color color, out RectTransform rect,
            out CanvasGroup group)
        {
            GameObject root = new GameObject(name, typeof(RectTransform),
                typeof(CanvasGroup), typeof(Image));
            rect = root.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            group = root.GetComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;
            Image image = root.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return root;
        }

        private TextMeshProUGUI CreateText(string name, RectTransform parent,
            float ratio, TextAlignmentOptions alignment, bool autoSize)
        {
            return CreateTextFromTemplate(name, parent, fontTemplate, ratio,
                alignment, autoSize);
        }

        private static TextMeshProUGUI CreateTextFromTemplate(string name,
            RectTransform parent, TextMeshProUGUI template, float ratio,
            TextAlignmentOptions alignment, bool autoSize)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform),
                typeof(TextMeshProUGUI));
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>();
            text.font = template.font;
            text.fontSharedMaterial = template.fontSharedMaterial;
            text.fontStyle = FontStyles.Bold;
            float size = Mathf.Max(8f, template.fontSize * ratio);
            text.fontSize = size;
            if (autoSize)
                NativeLocalizedText.SetShrinkOnlySize(text, size, Mathf.Max(8f, size * 0.82f));
            else
                text.enableAutoSizing = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.alignment = alignment;
            text.richText = false;
            text.raycastTarget = false;
            SephiriaEnhancements.Integration.NativeLocalizedText.BindFont(text, template);
            return text;
        }

        private static Image CreateImage(string name, RectTransform parent,
            Color color)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform),
                typeof(Image));
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Image image = obj.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void SetTopRect(RectTransform rect, float left,
            float right, float top, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2((left - right) * 0.5f, -top);
            rect.sizeDelta = new Vector2(-(left + right), height);
        }

        private static bool IsPresented(GameObject obj, CanvasGroup group) =>
            obj != null && group != null && obj.activeInHierarchy &&
            group.alpha > 0.001f;

        private static void Destroy(GameObject obj)
        {
            if (obj != null) UnityEngine.Object.Destroy(obj);
        }

        private static void SetActive(GameObject obj, bool active)
        {
            if (obj != null && obj.activeSelf != active)
                obj.SetActive(active);
        }

        private sealed class LiveRow
        {
            internal GameObject Root { get; }
            private readonly TextMeshProUGUI marker, name, value;
            private readonly Image bar;

            internal LiveRow(RectTransform parent, TextMeshProUGUI template,
                int index)
            {
                Root = new GameObject("Row " + index,
                    typeof(RectTransform));
                RectTransform rect = Root.GetComponent<RectTransform>();
                rect.SetParent(parent, false);
                bar = CreateImage("Bar", rect,
                    new Color(0.58f, 0.76f, 0.38f, 0.7f));
                marker = CreateTextFromTemplate("Marker", rect, template,
                    0.49f, TextAlignmentOptions.Center,
                    autoSize: false);
                name = CreateTextFromTemplate("Name", rect, template,
                    0.54f, TextAlignmentOptions.MidlineLeft,
                    autoSize: false);
                value = CreateTextFromTemplate("Value", rect, template,
                    0.54f, TextAlignmentOptions.MidlineRight,
                    autoSize: false);
            }

            internal void SetLayout(float width, float valueWidth, int index)
            {
                RectTransform rect = Root.transform as RectTransform;
                SetTopRect(rect, 0f, 0f, index * 16f, 15f);
                SetTopRect(marker.rectTransform, 2f, width - 18f, 0f, 14f);
                SetTopRect(name.rectTransform, 20f, valueWidth + 6f,
                    0f, 14f);
                SetTopRect(value.rectTransform, width - valueWidth, 2f,
                    0f, 14f);
            }

            internal void Show(string player, float amount, float maximum,
                string display, bool local, bool mvp)
            {
                marker.text = mvp ? "★" : local ? "◆" : string.Empty;
                marker.color = mvp ? Brass : Moss;
                name.text = CombatInsightsText.SingleLinePlayerName(player);
                name.color = local ? Paper : Muted;
                value.text = display;
                value.color = amount > 0f ? Paper : Muted;
                RectTransform root = Root.transform as RectTransform;
                float width = Mathf.Max(0f, root.rect.width - 4f);
                bar.rectTransform.anchorMin =
                    bar.rectTransform.anchorMax = Vector2.zero;
                bar.rectTransform.pivot = Vector2.zero;
                bar.rectTransform.anchoredPosition = new Vector2(2f, 0f);
                bar.rectTransform.sizeDelta = new Vector2(width *
                    Mathf.Clamp01(amount / maximum), 1f);
                bar.color = local ? Moss : Brass;
            }
        }

        private sealed class ReportRow
        {
            internal GameObject Root { get; }
            private readonly Image wash, bar;
            private readonly TextMeshProUGUI rank, marker, name, damage,
                share, averageDps;
            private float maximumBarWidth;

            internal ReportRow(RectTransform parent, TextMeshProUGUI template,
                int index)
            {
                Root = new GameObject("Player " + index,
                    typeof(RectTransform), typeof(Image));
                RectTransform rect = Root.GetComponent<RectTransform>();
                rect.SetParent(parent, false);
                wash = Root.GetComponent<Image>();
                wash.raycastTarget = false;
                bar = CreateImage("Contribution", rect,
                    new Color(0.58f, 0.76f, 0.38f, 0.55f));
                rank = CreateTextFromTemplate("Rank", rect, template, 0.48f,
                    TextAlignmentOptions.MidlineRight, autoSize: false);
                marker = CreateTextFromTemplate("Marker", rect, template,
                    0.48f, TextAlignmentOptions.Center,
                    autoSize: false);
                name = CreateTextFromTemplate("Name", rect, template, 0.58f,
                    TextAlignmentOptions.MidlineLeft, autoSize: false);
                damage = CreateTextFromTemplate("Damage", rect, template,
                    0.56f, TextAlignmentOptions.MidlineRight,
                    autoSize: false);
                share = CreateTextFromTemplate("Share", rect, template, 0.54f,
                    TextAlignmentOptions.MidlineRight, autoSize: false);
                averageDps = CreateTextFromTemplate("Average DPS", rect,
                    template, 0.56f, TextAlignmentOptions.MidlineRight,
                    autoSize: false);
            }

            internal void SetLayout(float panelWidth, int index,
                float rowHeight)
            {
                RectTransform rect = Root.transform as RectTransform;
                SetTopRect(rect, 8f, 8f, index * rowHeight,
                    rowHeight - 2f);
                float width = panelWidth - 16f;
                const float padding = 6f;
                const float rankWidth = 14f;
                const float markerWidth = 18f;
                const float damageWidth = 44f;
                const float shareWidth = 32f;
                const float dpsWidth = 48f;
                const float gap = 5f;
                float dpsLeft = width - padding - dpsWidth;
                float shareLeft = dpsLeft - gap - shareWidth;
                float damageLeft = shareLeft - gap - damageWidth;
                SetTopRect(rank.rectTransform, padding,
                    width - padding - rankWidth, 0f, rowHeight - 4f);
                SetTopRect(marker.rectTransform, padding + rankWidth,
                    width - padding - rankWidth - markerWidth,
                    0f, rowHeight - 4f);
                float nameLeft = padding + rankWidth + markerWidth + 4f;
                SetTopRect(name.rectTransform, nameLeft,
                    width - damageLeft + gap, 0f, rowHeight - 4f);
                SetTopRect(damage.rectTransform, damageLeft,
                    width - damageLeft - damageWidth,
                    0f, rowHeight - 4f);
                SetTopRect(share.rectTransform, shareLeft,
                    width - shareLeft - shareWidth,
                    0f, rowHeight - 4f);
                SetTopRect(averageDps.rectTransform, dpsLeft, padding,
                    0f, rowHeight - 4f);
                maximumBarWidth = width - nameLeft - padding;
                bar.rectTransform.anchorMin =
                    bar.rectTransform.anchorMax = new Vector2(0f, 1f);
                bar.rectTransform.pivot = new Vector2(0f, 1f);
                bar.rectTransform.anchoredPosition = new Vector2(nameLeft,
                    -(rowHeight - 3f));
                bar.rectTransform.sizeDelta =
                    new Vector2(maximumBarWidth, 1f);
            }

            internal void Show(int index,
                CombatStatisticsPlayerSnapshot player, float maximum,
                float total, float duration)
            {
                bool mvp = index == 0 && player.Damage > 0f;
                rank.text = (index + 1).ToString("00");
                rank.color = Quiet;
                marker.text = mvp
                    ? player.IsLocal ? "★◆" : "★"
                    : player.IsLocal ? "◆" : string.Empty;
                marker.color = mvp ? Brass : Moss;
                name.text = CombatInsightsText.SingleLinePlayerName(
                    player.Name);
                name.color = player.IsLocal ? Paper : Muted;
                damage.text = DpsFormatter.Compact(player.Damage);
                share.text = DpsFormatter.Percent(player.Damage, total);
                averageDps.text = DpsFormatter.Rate(player.Damage, duration);
                damage.color = share.color = averageDps.color =
                    player.Damage > 0f ? Paper : Muted;
                wash.color = player.IsLocal ? LocalWash : Color.clear;
                bar.rectTransform.sizeDelta = new Vector2(maximumBarWidth *
                    Mathf.Clamp01(player.Damage / maximum), 1f);
                bar.color = player.IsLocal ? Moss : Brass;
            }
        }

        private sealed class OutcomeChip
        {
            private readonly RectTransform rect;
            private readonly TextMeshProUGUI label, value;

            internal OutcomeChip(RectTransform parent,
                TextMeshProUGUI template, string name)
            {
                GameObject root = new GameObject(name, typeof(RectTransform),
                    typeof(Image));
                rect = root.GetComponent<RectTransform>();
                rect.SetParent(parent, false);
                Image background = root.GetComponent<Image>();
                background.color = new Color(1f, 1f, 1f, 0.035f);
                background.raycastTarget = false;
                label = CreateTextFromTemplate("Label", rect, template,
                    0.44f, TextAlignmentOptions.Center, autoSize: true);
                label.color = Quiet;
                value = CreateTextFromTemplate("Value", rect, template,
                    0.62f, TextAlignmentOptions.Center, autoSize: false);
                value.color = Paper;
            }

            internal void SetLayout(float panelWidth, float left, float top,
                float width, float height)
            {
                SetTopRect(rect, left, panelWidth - left - width,
                    top, height);
                SetTopRect(label.rectTransform, 4f, 4f, 1f, 11f);
                SetTopRect(value.rectTransform, 4f, 4f, 12f, 12f);
            }

            internal void Show(string name, int count)
            {
                label.text = name.ToUpperInvariant();
                value.text = "×" + count;
                value.color = count > 0 ? Paper : Quiet;
            }
        }
    }
}
