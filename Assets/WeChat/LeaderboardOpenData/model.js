'use strict';
// Own record only; friend records never leave the open data context.
const KEY = 'islands_campaign_rank_v1';
function valid(v) {
    return v && v.version === 1 && Number.isInteger(v.completedCount) && v.completedCount >= 0 && v.completedCount <= 20 &&
        Number.isSafeInteger(v.achievedAtMs) && v.achievedAtMs > 0 && typeof v.marker === 'string' && /^[a-f0-9]{32}$/.test(v.marker);
}
function parse(kv) {
    if (!Array.isArray(kv)) throw new Error('Invalid cloud response');
    const fields = kv.filter(x => x && x.key === KEY);
    if (!fields.length) return null;
    if (fields.length !== 1) throw new Error('Duplicate cloud record');
    const value = JSON.parse(fields[0].value);
    if (!valid(value)) throw new Error('Invalid cloud record');
    return value;
}
function merge(remote, local) {
    if (!valid(local) || (remote && !valid(remote))) throw new Error('Invalid record');
    if (!remote) return {...local};
    if (local.completedCount <= remote.completedCount) return {...remote};
    const merged = {...local, achievedAtMs: Math.max(local.achievedAtMs, remote.achievedAtMs + 1), marker: remote.marker};
    if (!valid(merged)) throw new Error('Unrepresentable achievement time');
    return merged;
}
function compare(a, b) {
    return b.record.completedCount - a.record.completedCount || a.record.achievedAtMs - b.record.achievedAtMs ||
        (a.record.marker < b.record.marker ? -1 : a.record.marker > b.record.marker ? 1 : 0);
}
function snapshot(users, own) {
    if (!Array.isArray(users)) throw new Error('Invalid friends response');
    const rows = [];
    for (const user of users) {
        try { const record = parse(user.KVDataList); if (record) rows.push({record, nickname: user.nickname || '微信玩家', avatarUrl: user.avatarUrl || '', self: false}); }
        catch (_) { /* A corrupt friend row cannot erase otherwise valid scores. */ }
    }
    let self = null;
    if (own) {
        const matches = rows.filter(r => r.record.marker === own.marker);
        if (matches.length <= 1) {
            self = matches[0] || {nickname: '微信玩家', avatarUrl: '', record: own};
            self.record = own; self.self = true;
            if (!matches.length) rows.push(self);
        }
    }
    rows.sort(compare); rows.forEach((r, i) => r.rank = i + 1);
    return {rows, self, empty: rows.filter(r => !r.self).length === 0};
}
module.exports = {KEY, valid, parse, merge, compare, snapshot};
