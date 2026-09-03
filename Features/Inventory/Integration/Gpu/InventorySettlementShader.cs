namespace SephiriaEnhancements.Inventory.Integration.Gpu;

internal static class InventorySettlementShader
{
    internal const string Source = @"StructuredBuffer<int> S : register(t0);
StructuredBuffer<int> L : register(t1);
RWStructuredBuffer<int> R : register(u0);
groupshared int occupancy[64], levels[64], multipliers[64], disables[64], bypasses[64];
groupshared int positions[64], rotations[64], resolved[64], pathStamp[64], valid;
groupshared uint low[64], high[64];
void invalidate() { int original; InterlockedExchange(valid, 0, original); }
int field(int item, int member) { return S[S[5] + item * 20 + member]; }
int occupant(int x, int y) {
    int cell = y * S[0] + x;
    return x >= 0 && x < S[0] && y >= 0 && cell >= 0 && cell < S[1] ? occupancy[cell] : -1;
}
bool artifact(int item) { return item >= 0 && field(item, 0) != 0; }
bool hasCategory(int item, int category) {
    return category < 32 ? (low[item] & (1u << category)) != 0 : (high[item] & (1u << (category - 32))) != 0;
}
void setCategory(inout uint a, inout uint b, int category) {
    if (category < 32) a |= 1u << category; else b |= 1u << (category - 32);
}
void tablet(int projection, int origin) {
    if (projection < 0 || origin < 0 || origin >= S[1]) { invalidate(); return; }
    bool hasPlaced = false, placedHit = false;
    [loop] for (int c = 0; c < S[projection]; c++) {
        int p = S[projection + 2] + c * 3, cell = S[p + 1];
        int item = S[p + 2] != 0 ? occupancy[cell] : -1;
        if (S[p] == 1) { if (item < 0) return; }
        else if (S[p] == 2) { if (!artifact(item)) return; }
        else if (S[p] == 3) { hasPlaced = true; placedHit = placedHit || cell == origin; }
        else return;
    }
    if (hasPlaced && !placedHit) return;
    [loop] for (int e = 0; e < S[projection + 1]; e++) {
        int p = S[projection + 3] + e * 4, cell = S[p + 1];
        if (S[p + 3] == 0) continue;
        if (S[p] == 1) InterlockedAdd(levels[cell], S[p + 2]);
        else if (S[p] == 2) InterlockedAdd(disables[cell], 1);
        else if (S[p] == 3) InterlockedAdd(bypasses[cell], 1);
        else if (S[p] == 4) InterlockedAdd(multipliers[cell], S[p + 2]);
        else invalidate();
    }
}
bool criteria(int item, int cell) {
    int kind = field(item, 4), state = field(item, 5);
    if (kind == 0 || state == 0) return true;
    int w = S[0], storage = S[1], x = cell % w, y = cell / w;
    if (kind == 1) return y == 0;
    if (kind == 2) return cell >= storage - 6;
    if (kind == 3) return x == 0 || x == 5;
    if (kind == 4) return x > 0 && y > 0 && x < w - 1 && cell + 7 <= storage - 1;
    if (kind == 5) return x <= 0 || y <= 0 || x >= w - 1 || cell >= storage - 6;
    if (kind == 6) return x > 0 && x < w - 1 && (storage % w == 0 || y < (storage + w - 1) / w - 1 || x < storage % w - 1)
        && occupancy[cell - 1] < 0 && occupancy[cell + 1] < 0;
    if (kind == 7) return x > 0 && x < w - 1 && cell + 1 < storage && artifact(occupancy[cell - 1]) && artifact(occupancy[cell + 1]);
    if (kind == 8 || kind == 9) {
        bool allOccupied = true, anyMagic = false;
        [unroll] for (int dy = -1; dy <= 1; dy++)
        [unroll] for (int dx = -1; dx <= 1; dx++) {
            if (dx == 0 && dy == 0) continue;
            int neighbor = occupant(x + dx, y + dy);
            allOccupied = allOccupied && neighbor >= 0;
            anyMagic = anyMagic || (neighbor >= 0 && field(neighbor, 6) != 0);
        }
        return kind == 8 ? allOccupied : anyMagic;
    }
    return kind == 10 && state == 1;
}

[numthreads(64, 1, 1)]
void Solve(uint3 group : SV_GroupID, uint lane : SV_GroupIndex) {
    if (group.x >= (uint)L[0]) return;
    int n = S[2], categories = S[3], offset = group.x * S[13];
    int input = 4 + group.x * n * 2;
    if (lane == 0) valid = 1;
    occupancy[lane] = -1; low[lane] = high[lane] = 0; resolved[lane] = 1; pathStamp[lane] = 0;
    if (lane < (uint)S[1]) {
        int p = S[4] + lane * 4;
        levels[lane] = S[p]; multipliers[lane] = S[p + 1]; disables[lane] = S[p + 2]; bypasses[lane] = S[p + 3];
    }
    if (lane < (uint)n) { positions[lane] = L[input + lane * 2]; rotations[lane] = L[input + lane * 2 + 1]; }
    GroupMemoryBarrierWithGroupSync();
    if (lane < (uint)n) {
        int cell = positions[lane], old;
        if (cell < 0 || cell >= S[1]) { invalidate(); positions[lane] = 0; }
        else {
            InterlockedCompareExchange(occupancy[cell], -1, lane, old);
            if (old != -1) invalidate();
            if (field(lane, 0) != 0) InterlockedAdd(levels[cell], field(lane, 2));
        }
    }
    GroupMemoryBarrierWithGroupSync();

    if (lane < (uint)n && field(lane, 19) != 0) {
        int rotation = rotations[lane];
        if (rotation < 0 || rotation > 3) invalidate();
        else tablet(S[S[6] + (lane * S[1] + positions[lane]) * 4 + rotation], positions[lane]);
    }
    [loop] for (int f = lane; f < S[8]; f += 64) tablet(S[S[7] + f * 2 + 1], S[S[7] + f * 2]);
    GroupMemoryBarrierWithGroupSync();

    if (lane < (uint)n) {
        int cell = positions[lane], p = offset + 1 + lane * 3;
        R[p] = R[p + 1] = R[p + 2] = 0;
        if (field(lane, 0) != 0) {
            int level = multipliers[cell] == 0 ? levels[cell] : levels[cell] * multipliers[cell];
            bool enabled = S[10] != 0 && S[11] > 0 && disables[cell] <= 0 && level >= 0 &&
                (bypasses[cell] > 0 || criteria(lane, cell)) && field(lane, 3) != 0;
            R[p] = enabled; R[p + 1] = level; R[p + 2] = enabled ? min(field(lane, 1), level) : 0;
            uint a = 0, b = 0;
            int kind = field(lane, 9);
            if (kind == 0) {
                [loop] for (int c = 0; c < field(lane, 8); c++) setCategory(a, b, S[field(lane, 7) + c]);
            } else if (kind == 1) setCategory(a, b, S[field(lane, 10) + (cell / S[0]) % field(lane, 11)]);
            low[lane] = a; high[lane] = b; resolved[lane] = kind != 2;
        }
    }
    GroupMemoryBarrierWithGroupSync();
    if (lane == 0) {
        int stack[64];
        [loop] for (int i = 0; i < n; i++) {
            if (!artifact(i) || field(i, 9) != 2 || resolved[i] != 0) continue;
            int depth = 0, node = i;
            uint a = 0, b = 0;
            [loop] while (true) {
                if (resolved[node] != 0) { a = low[node]; b = high[node]; break; }
                if (pathStamp[node] == i + 1) break;
                pathStamp[node] = i + 1; stack[depth++] = node;
                int cell = positions[node];
                int target = occupant(cell % S[0] + field(node, 12), cell / S[0] + field(node, 13));
                if (!artifact(target)) break;
                if (field(target, 9) == 2) { node = target; continue; }
                if (field(target, 14) != 0) { a = low[target]; b = high[target]; }
                break;
            }
            [loop] for (int j = 0; j < depth; j++) { low[stack[j]] = a; high[stack[j]] = b; resolved[stack[j]] = 1; }
        }
        [loop] for (int item = 0; item < n; item++) {
            if (!artifact(item) || field(item, 9) != 3) continue;
            uint a = 0, b = 0;
            int origin = positions[item];
            [loop] for (int category = 0; category < categories; category++) {
                int count = 0;
                [loop] for (int j = 0; j < field(item, 17); j++) {
                    int p = field(item, 16) + j * 2;
                    int other = occupant(origin % S[0] + S[p], origin / S[0] + S[p + 1]);
                    if (artifact(other) && hasCategory(other, category)) count++;
                }
                if (count > 0 && count >= field(item, 18)) setCategory(a, b, category);
            }
            low[item] = a; high[item] = b;
        }
    }
    GroupMemoryBarrierWithGroupSync();
    if (lane < (uint)categories) {
        int count = S[S[9] + lane * 2], present = S[S[9] + lane * 2 + 1];
        [loop] for (int item = 0; item < n; item++) {
            if (!artifact(item)) continue;
            bool duplicate = false;
            if (S[12] != 0) {
                [loop] for (int earlier = 0; earlier < item; earlier++)
                    if (artifact(earlier) && field(earlier, 15) == field(item, 15)) duplicate = true;
            }
            if (!duplicate && hasCategory(item, lane)) { count++; present = 1; }
        }
        R[offset + 1 + n * 3 + lane] = count;
        R[offset + 1 + n * 3 + categories + lane] = present;
    }
    if (lane == 0) R[offset] = valid;
    DeviceMemoryBarrierWithGroupSync();
    if (lane == 0) {
        int deactivated = 0, enabledCount = 0, breakpoints = 0, capped = 0, excess = 0, moved = 0, rotated = 0;
        [loop] for (int item = 0; item < n; item++) {
            int original = S[14] + item * 3;
            moved += positions[item] != S[original];
            rotated += field(item, 19) != 0 && rotations[item] != S[original + 1];
            if (!artifact(item)) continue;
            int p = offset + 1 + item * 3;
            deactivated += S[original + 2] != 0 && R[p] == 0;
            enabledCount += R[p] != 0;
            capped += R[p] != 0 ? R[p + 2] : 0;
            excess += max(0, R[p + 1] - field(item, 1));
        }
        [loop] for (int category = 0; category < categories; category++) {
            int p = S[15] + category * 2, count = R[offset + 1 + n * 3 + category];
            [loop] for (int t = 0; t < S[p + 1]; t++) if (count >= S[S[p] + t]) breakpoints += S[S[p] + t];
        }
        int p = offset + S[13] - 7;
        R[p] = deactivated; R[p + 1] = enabledCount; R[p + 2] = breakpoints; R[p + 3] = capped;
        R[p + 4] = excess; R[p + 5] = moved; R[p + 6] = rotated;
    }
}



";
}
