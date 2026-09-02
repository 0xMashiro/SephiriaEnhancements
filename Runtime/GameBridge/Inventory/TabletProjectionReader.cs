#nullable disable
using SephiriaEnhancements.Runtime.Inventory;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using SephiriaEnhancements.Runtime;

namespace SephiriaEnhancements.Runtime.GameBridge.Inventory
{
    internal sealed class TabletProjectionReader
    {
        private const int MaximumCachedQueries = 4096;
        private const int MaximumCachedPlacementSets = 256;
        private readonly Dictionary<QueryKey, QueryProjection> cache =
            new Dictionary<QueryKey, QueryProjection>();
        private readonly Dictionary<PlacementKey,
            TabletPlacementProjectionSnapshot[]> placementCache =
            new Dictionary<PlacementKey, TabletPlacementProjectionSnapshot[]>();
        private readonly RuntimeMetrics metrics;

        internal TabletProjectionReader(RuntimeMetrics metrics)
        {
            this.metrics = metrics;
        }

        internal TabletRotationProjectionSnapshot[] CaptureAllRotations(
            string conditionQuery, string effectQuery, int width, int height,
            int storage, int originX, int originY)
        {
            var result = new TabletRotationProjectionSnapshot[4];
            for (int rotation = 0; rotation < result.Length; rotation++)
            {
                QueryProjection criteria = Read(conditionQuery, true, width,
                    height, storage, originX, originY, rotation);
                QueryProjection effects = Read(effectQuery, false, width,
                    height, storage, originX, originY, rotation);
                bool succeeded = criteria.Succeeded && effects.Succeeded;
                string issue = criteria.Succeeded ? effects.Issue : criteria.Issue;
                result[rotation] = new TabletRotationProjectionSnapshot(rotation,
                    criteria.Additions, effects.Additions, succeeded, issue);
            }
            return result;
        }

        internal TabletPlacementProjectionSnapshot[] CaptureAllPlacements(
            string conditionQuery, string effectQuery, int width, int height,
            int storage)
        {
            var key = new PlacementKey(conditionQuery, effectQuery, width,
                height, storage);
            if (placementCache.TryGetValue(key, out var cached))
            {
                return cached;
            }
            if (placementCache.Count >= MaximumCachedPlacementSets)
            {
                placementCache.Clear();
            }

            var result = new TabletPlacementProjectionSnapshot[storage];
            for (int index = 0; index < storage; index++)
            {
                int x = index % width;
                int y = index / width;
                result[index] = new TabletPlacementProjectionSnapshot(index,
                    x, y, CaptureAllRotations(conditionQuery, effectQuery,
                        width, height, storage, x, y));
            }
            placementCache[key] = result;
            return result;
        }

        internal void Clear()
        {
            cache.Clear();
            placementCache.Clear();
        }

        private QueryProjection Read(string query, bool criteria, int width,
            int height, int storage, int originX, int originY, int rotation)
        {
            var key = new QueryKey(query, criteria, width, height, storage,
                originX, originY, rotation);
            if (cache.TryGetValue(key, out QueryProjection cached))
            {
                metrics?.RecordTabletQuery(cacheHit: true, 0f,
                    cached.Succeeded);
                return cached;
            }

            if (cache.Count >= MaximumCachedQueries)
            {
                cache.Clear();
            }

            long started = Stopwatch.GetTimestamp();
            QueryProjection projection;
            try
            {
                List<StoneTablet.AdditionMetadata> parsed = StoneTablet.ParseQuery(
                    query ?? string.Empty, width, height, storage,
                    new ItemPosition(originX, originY), rotation, out var _);
                var additions = new TabletAdditionSnapshot[parsed.Count];
                for (int index = 0; index < parsed.Count; index++)
                {
                    additions[index] = Convert(parsed[index], criteria, width,
                        height, storage);
                }
                projection = new QueryProjection(additions, true, string.Empty);
            }
            catch (Exception ex)
            {
                projection = new QueryProjection(
                    Array.Empty<TabletAdditionSnapshot>(), false,
                    ex.GetType().Name);
            }

            float elapsed = (float)((Stopwatch.GetTimestamp() - started) *
                1000d / Stopwatch.Frequency);
            metrics?.RecordTabletQuery(cacheHit: false, elapsed,
                projection.Succeeded);
            cache[key] = projection;
            return projection;
        }

        private static TabletAdditionSnapshot Convert(
            StoneTablet.AdditionMetadata metadata, bool criteria, int width,
            int height, int storage)
        {
            int x = metadata.position.x;
            int y = metadata.position.y;
            bool valid = x >= 0 && y >= 0 && x < width && y < height &&
                x + y * width < storage;
            TabletCriteriaKind criteriaKind = TabletCriteriaKind.Unknown;
            TabletEffectKind effectKind = TabletEffectKind.Unknown;
            int level = 0;
            string value = metadata.value ?? string.Empty;

            if (criteria)
            {
                if (value == "ITEM") criteriaKind = TabletCriteriaKind.AnyItem;
                else if (value == "CHARM") criteriaKind = TabletCriteriaKind.Artifact;
                else if (value == "PLACED") criteriaKind = TabletCriteriaKind.Placed;
            }
            else if (int.TryParse(value, NumberStyles.Integer,
                CultureInfo.InvariantCulture, out level))
            {
                effectKind = TabletEffectKind.IncreaseLevel;
            }
            else if (value == "X") effectKind = TabletEffectKind.Disable;
            else if (value == "IGNORECRITERIA")
                effectKind = TabletEffectKind.IgnoreCriteria;
            else
            {
                string[] parts = value.Split('/');
                if (parts.Length == 2 && parts[0] == "MUL" &&
                    int.TryParse(parts[1], NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out level))
                {
                    effectKind = TabletEffectKind.MultiplyLevel;
                }
            }

            return new TabletAdditionSnapshot(x, y, value, valid,
                metadata.isXWorldPosition, metadata.isYWorldPosition,
                metadata.borderTop, metadata.borderRight, metadata.borderBottom,
                metadata.borderLeft, criteriaKind, effectKind, level);
        }

        private readonly struct QueryProjection
        {
            internal QueryProjection(TabletAdditionSnapshot[] additions,
                bool succeeded, string issue)
            {
                Additions = additions;
                Succeeded = succeeded;
                Issue = issue;
            }

            internal TabletAdditionSnapshot[] Additions { get; }
            internal bool Succeeded { get; }
            internal string Issue { get; }
        }

        private readonly struct QueryKey : IEquatable<QueryKey>
        {
            private readonly string query;
            private readonly bool criteria;
            private readonly int width;
            private readonly int height;
            private readonly int storage;
            private readonly int originX;
            private readonly int originY;
            private readonly int rotation;

            internal QueryKey(string query, bool criteria, int width, int height,
                int storage, int originX, int originY, int rotation)
            {
                this.query = query ?? string.Empty;
                this.criteria = criteria;
                this.width = width;
                this.height = height;
                this.storage = storage;
                this.originX = originX;
                this.originY = originY;
                this.rotation = rotation;
            }

            public bool Equals(QueryKey other)
            {
                return criteria == other.criteria && width == other.width &&
                    height == other.height && storage == other.storage &&
                    originX == other.originX && originY == other.originY &&
                    rotation == other.rotation && string.Equals(query,
                        other.query, StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is QueryKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = StringComparer.Ordinal.GetHashCode(query);
                    hash = hash * 397 ^ criteria.GetHashCode();
                    hash = hash * 397 ^ width;
                    hash = hash * 397 ^ height;
                    hash = hash * 397 ^ storage;
                    hash = hash * 397 ^ originX;
                    hash = hash * 397 ^ originY;
                    return hash * 397 ^ rotation;
                }
            }
        }

        private readonly struct PlacementKey : IEquatable<PlacementKey>
        {
            private readonly string conditionQuery;
            private readonly string effectQuery;
            private readonly int width;
            private readonly int height;
            private readonly int storage;

            internal PlacementKey(string conditionQuery, string effectQuery,
                int width, int height, int storage)
            {
                this.conditionQuery = conditionQuery ?? string.Empty;
                this.effectQuery = effectQuery ?? string.Empty;
                this.width = width;
                this.height = height;
                this.storage = storage;
            }

            public bool Equals(PlacementKey other)
            {
                return width == other.width && height == other.height &&
                    storage == other.storage && string.Equals(conditionQuery,
                        other.conditionQuery, StringComparison.Ordinal) &&
                    string.Equals(effectQuery, other.effectQuery,
                        StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is PlacementKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = StringComparer.Ordinal.GetHashCode(conditionQuery);
                    hash = hash * 397 ^ StringComparer.Ordinal.GetHashCode(
                        effectQuery);
                    hash = hash * 397 ^ width;
                    hash = hash * 397 ^ height;
                    return hash * 397 ^ storage;
                }
            }
        }
    }
}
