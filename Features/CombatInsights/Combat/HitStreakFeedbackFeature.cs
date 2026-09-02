using SephiriaEnhancements.Diagnostics;
using System;
using SephiriaEnhancements.Core;
using SephiriaEnhancements.Presentation;
using UnityEngine;

namespace SephiriaEnhancements.Combat
{
    internal sealed class HitStreakFeedbackFeature
    {
        private readonly HitStreakFeedbackView view = new HitStreakFeedbackView();
        private readonly HitStreakTracker hitStreak = new HitStreakTracker();
        private readonly DamageContextBuffer damageContexts = new DamageContextBuffer();
        private bool compatible = true;
        private float lastActivityAt = -1000f;

        internal bool IsRecent(float now) => now - lastActivityAt <= 2f;

        internal void Update(bool visible)
        {
            if (!compatible)
            {
                return;
            }

            try
            {
                view.Update(visible);
            }
            catch (Exception exception)
            {
                Disable(exception);
            }
        }

        internal void CaptureDamageDetail(UnitAvatar target, DamageData damage)
        {
            if (!compatible || target == null)
            {
                return;
            }

            try
            {
                CaptureDamageDetailCore(target, damage);
            }
            catch (Exception exception)
            {
                Disable(exception);
            }
        }

        private void CaptureDamageDetailCore(UnitAvatar target, DamageData damage)
        {
            bool indirect = damage.damageType == EDamageType.ElementalEffectDamage;
            float now = Time.unscaledTime;
            int targetId = target.GetInstanceID();
            if (damage.damage > 0f)
            {
                // The game's HP feedback truncates while shield feedback rounds.
                damageContexts.Record(now, targetId, (int)damage.damage, damage.position.x,
                    damage.position.y, indirect);
            }
            RecordRoundedComponent(now, targetId, damage.shieldDamage, damage.position, indirect);
            RecordRoundedComponent(now, targetId, damage.mpShieldDamage, damage.position, indirect);
        }

        internal void CaptureFeedback(DamageFeedback feedback, bool ownedContribution)
        {
            if (!compatible)
            {
                return;
            }

            try
            {
                CaptureFeedbackCore(feedback, ownedContribution);
            }
            catch (Exception exception)
            {
                Disable(exception);
            }
        }

        internal void Hide() => view.Hide();

        internal void Reset()
        {
            lastActivityAt = -1000f;
            hitStreak.Reset();
            damageContexts.Clear();
            view.Hide();
        }

        internal void Dispose() => view.Dispose();

        private void CaptureFeedbackCore(DamageFeedback feedback, bool ownedContribution)
        {
            // DamageFeedback.EMsgType is the game's native API; map it to the
            // CombatInsights hit-streak domain at this integration boundary.
            HitStreakImpact impact;
            switch ((DamageFeedback.EMsgType)feedback.msgType)
            {
                case DamageFeedback.EMsgType.Critical:
                    impact = HitStreakImpact.Critical;
                    break;
                case DamageFeedback.EMsgType.Execution:
                    impact = HitStreakImpact.Execution;
                    break;
                case DamageFeedback.EMsgType.Normal:
                    impact = HitStreakImpact.Normal;
                    break;
                default:
                    return;
            }

            bool indirect = false;
            damageContexts.TryMatch(Time.unscaledTime, feedback.self.GetInstanceID(),
                feedback.damageValue, feedback.position.x, feedback.position.y, out indirect);
            HitStreakUpdate update = hitStreak.Register(Time.unscaledTime, feedback.damageValue,
                impact, indirect);
            if (!indirect)
            {
                lastActivityAt = Time.unscaledTime;
            }
            if (update.Count == 0)
            {
                return;
            }
            if (update.Count == 1)
            {
                view.Hide();
                return;
            }

            Color color = new Color(feedback.r / 255f, feedback.g / 255f,
                feedback.b / 255f, feedback.a / 255f);
            view.Show(feedback.position, update, impact, color, ownedContribution);
        }

        private void Disable(Exception exception)
        {
            compatible = false;
            view.Hide();
            SupportLogger.Warning("hit_streak_feedback_failed", "[SephiriaEnhancements] Hit-streak feedback disabled " +
                "until the Mod is reloaded: " +
                exception.Message);
        }

        private void RecordRoundedComponent(float now, int targetId, float value,
            Vector3 position, bool indirect)
        {
            if (value > 0f)
            {
                damageContexts.Record(now, targetId, Mathf.RoundToInt(value), position.x,
                    position.y, indirect);
            }
        }
    }
}
