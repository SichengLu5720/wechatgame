'use strict';
const model = require('./model');
const canvas = wx.getSharedCanvas(), ctx = canvas.getContext('2d');
let visible = false, local = null, own = null, activeSync = null, generation = 0;
let state = 'loading', data = {rows: [], self: null}, scroll = 0, view = null, touch = null, retry = null;
let logicalWidth = 520, logicalHeight = 800, timeout = null;
let pendingWrite = null;
const images = new Map();
const ink = '#475c59', paper = '#f5eedb', mint = '#dce8d7';
function call(name, options) {
    const writing = name === 'setUserCloudStorage';
    if (writing && pendingWrite) return Promise.reject(new Error('Previous write unresolved'));
    const ticket = writing ? {record: JSON.parse(options.KVDataList[0].value)} : null;
    if (writing) pendingWrite = ticket;
    return new Promise((resolve, reject) => {
        let settled = false;
        const timer = setTimeout(() => {settled = true; reject(new Error('Platform timeout'));}, 10000);
        const finish = (ok, result) => {
            if (writing && pendingWrite === ticket) pendingWrite = null;
            if (settled) {
                // A late read must never resume a stale merge. A late write may
                // release its gate, but cannot clear a newer write's ticket.
                if (writing && visible) refresh();
                return;
            }
            settled = true; clearTimeout(timer);
            if (ok) resolve(result); else reject(new Error('Platform unavailable'));
        };
        try { wx[name]({...options, success: result => finish(true, result), fail: () => finish(false)}); }
        catch (_) { finish(false); }
    });
}
// Serialize every read/merge/write. A read failure never becomes an empty record.
function sync() {
    if (activeSync) return activeSync.then(() => sync());
    const candidate = local && {...local};
    if (!model.valid(candidate)) return Promise.reject(new Error('Local progress unavailable'));
    activeSync = (async () => {
        const response = await call('getUserCloudStorage', {keyList: [model.KEY]});
        const remote = model.parse(response.KVDataList), merged = model.merge(remote, candidate);
        // A write timeout is not server cancellation. Only its own callback or
        // observing that exact committed value can permit the next write.
        if (pendingWrite && remote && remote.marker === pendingWrite.record.marker &&
            remote.completedCount === pendingWrite.record.completedCount && remote.achievedAtMs === pendingWrite.record.achievedAtMs) pendingWrite = null;
        if (!remote || JSON.stringify(remote) !== JSON.stringify(merged)) {
            await call('setUserCloudStorage', {KVDataList: [{key: model.KEY, value: JSON.stringify(merged)}]});
        }
        own = merged;
    })();
    const promise = activeSync;
    return promise.finally(() => { if (activeSync === promise) activeSync = null; });
}
async function refresh() {
    const request = ++generation; state = 'loading'; scroll = 0; draw();
    clearTimeout(timeout);
    timeout = setTimeout(() => { if (visible && request === generation) { state = 'failure'; draw(); } }, 12000);
    try {
        await sync();
        const result = await call('getFriendCloudStorage', {keyList: [model.KEY]});
        if (!visible || request !== generation) return;
        data = model.snapshot(result.data, own); state = data.empty ? 'empty' : 'ready';
        images.clear();
        for (const row of data.rows) {
            if (!row.avatarUrl || images.has(row.avatarUrl)) continue;
            const image = wx.createImage(); images.set(row.avatarUrl, {image, ready: false, failed: false});
            image.onload = () => { const entry = images.get(row.avatarUrl); if (entry && entry.image === image) {entry.ready = true; draw();} };
            image.onerror = () => { const entry = images.get(row.avatarUrl); if (entry && entry.image === image) {entry.failed = true; draw();} };
            image.src = row.avatarUrl;
        }
    } catch (_) { if (visible && request === generation) state = 'failure'; }
    finally { if (request === generation) {clearTimeout(timeout); draw();} }
}
function round(x, y, w, h, color, radius = 12) {
    ctx.fillStyle = color; ctx.beginPath();
    ctx.moveTo(x + radius, y); ctx.arcTo(x + w, y, x + w, y + h, radius);
    ctx.arcTo(x + w, y + h, x, y + h, radius); ctx.arcTo(x, y + h, x, y, radius);
    ctx.arcTo(x, y, x + w, y, radius); ctx.closePath(); ctx.fill();
}
function text(value, x, y, size = 22, align = 'left') {
    ctx.font = `${size}px sans-serif`; ctx.fillStyle = ink; ctx.textAlign = align; ctx.textBaseline = 'middle'; ctx.fillText(value, x, y);
}
function elide(value, width) {
    if (ctx.measureText(value).width <= width) return value;
    const chars = Array.from(value); while (chars.length && ctx.measureText(chars.join('') + '…').width > width) chars.pop();
    return chars.join('') + '…';
}
function row(item, y, fixed = false) {
    const w = logicalWidth - 8; round(0, y, w, 68, item.self || fixed ? mint : '#faf7ee');
    text(item.rank ? String(item.rank) : '—', 24, y + 34, 26, 'center');
    const entry = images.get(item.avatarUrl);
    if (entry && entry.ready) {
        ctx.save(); ctx.beginPath(); ctx.arc(72, y + 34, 22, 0, Math.PI * 2); ctx.clip(); ctx.drawImage(entry.image, 50, y + 12, 44, 44); ctx.restore();
    } else {
        round(50, y + 12, 44, 44, '#c7d9cc', 22);
        round(65, y + 20, 14, 14, '#6e8980', 7); round(59, y + 36, 26, 13, '#6e8980', 6);
    }
    ctx.font = '22px sans-serif';
    text(elide(item.nickname || '微信玩家', Math.max(20, w - 260)), 110, y + (item.self || fixed ? 26 : 34));
    if (item.self || fixed) text(item.rank ? '我' : state === 'loading' ? '名次加载中' : '名次暂不可用', 110, y + 49, 16);
    text(`已通关 ${item.record ? item.record.completedCount : 0} 关`, w - 16, y + 34, 20, 'right');
}
function degraded() {
    return data.rows.some(r => r.nickname === '微信玩家' || !r.avatarUrl || (images.get(r.avatarUrl) || {}).failed);
}
function draw() {
    if (!visible || !canvas.width || !canvas.height) return;
    ctx.setTransform(canvas.width / logicalWidth, 0, 0, canvas.height / logicalHeight, 0, 0);
    ctx.clearRect(0, 0, logicalWidth, logicalHeight); retry = null;
    const fixedY = logicalHeight - 68, profile = (state === 'ready' || state === 'empty') && degraded(), top = profile ? 80 : 0, height = fixedY - 28 - top;
    if (profile) { text('部分资料暂不可用', 0, 34, 20); retry = {x: logicalWidth - 110, y: 0, w: 102, h: 68}; round(retry.x, 0, 102, 68, '#dce5d7'); text('重试', retry.x + 51, 34, 22, 'center'); }
    if (state === 'ready') {
        scroll = Math.max(0, Math.min(scroll, Math.max(0, data.rows.length * 76 - 8 - height)));
        ctx.save(); ctx.beginPath(); ctx.rect(0, top, logicalWidth - 6, height); ctx.clip();
        data.rows.forEach((r, i) => { const y = top + i * 76 - scroll; if (y + 68 >= top && y <= top + height) row(r, y); }); ctx.restore();
        const total = data.rows.length * 76 - 8;
        if (total > height) { const thumb = Math.max(25, height * height / total); round(logicalWidth - 3, top + (height - thumb) * scroll / (total - height), 3, thumb, '#9eb9a5', 1); }
    } else {
        const y = top + height * .47;
        text(state === 'loading' ? '正在加载好友排行…' : state === 'empty' ? '暂无可显示的好友成绩' : '好友排行加载失败', logicalWidth / 2, y, 24, 'center');
        if (state === 'failure') { text('请稍后重试', logicalWidth / 2, y + 40, 20, 'center'); retry = {x: (logicalWidth - 170) / 2, y: y + 76, w: 170, h: 68}; round(retry.x, retry.y, 170, 68, '#e3ac9b'); text('重试', logicalWidth / 2, retry.y + 34, 22, 'center'); }
    }
    ctx.fillStyle = '#d3d9c8'; ctx.fillRect(0, fixedY - 14, logicalWidth - 8, 1);
    const knownSelf = (state === 'ready' || state === 'empty') && data.self;
    row(knownSelf || {record: own || local, nickname: '微信玩家', self: true}, fixedY, true);
}
function point(event) {
    const t = (event.changedTouches || event.touches || [])[0];
    if (!t || !view) return null;
    return {x: (t.clientX * view.devicePixelRatio - view.x) * logicalWidth / view.width,
        y: (t.clientY * view.devicePixelRatio - view.y) * logicalHeight / view.height};
}
function inside(p, r) { return p && r && p.x >= r.x && p.x <= r.x + r.w && p.y >= r.y && p.y <= r.y + r.h; }
wx.onTouchStart(event => {const p = point(event); touch = visible && inside(p, {x: 0, y: 0, w: logicalWidth, h: logicalHeight - 90}) ? {...p, startY: p.y, moved: false} : null;});
wx.onTouchMove(event => {const p = point(event); if (!visible || !touch || !p) return; touch.moved = touch.moved || Math.abs(p.y - touch.startY) > 8; scroll += touch.y - p.y; touch.y = p.y; draw();});
wx.onTouchEnd(event => {const p = point(event); if (visible && touch && !touch.moved && inside(p, retry) && inside({x: touch.x, y: touch.startY}, retry)) refresh(); touch = null;});
wx.onTouchCancel(() => {touch = null;});
wx.onMessage(message => {
    if (message.type === 'WXRender') {view = message; draw(); return;}
    if (message.type === 'WXDestroy') {visible = false; ++generation; clearTimeout(timeout); touch = null; return;}
    if (message.type === 'WXShow') {if (visible) refresh(); return;}
    if (message.type !== 'islands-friends-v1') return;
    if (model.valid(message.record)) local = message.record;
    if (message.command === 'sync') {sync().catch(() => {}); return;}
    if (message.command === 'layout') {logicalWidth = Math.max(300, message.width); logicalHeight = Math.max(300, message.height); draw(); return;}
    if (message.command === 'open') {visible = true; refresh();}
});
