const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const model = require('./model');
const record = (completedCount, achievedAtMs, id = 'a') => ({version: 1, completedCount, achievedAtMs, marker: id.repeat(32)});
const kv = r => r ? [{key: model.KEY, value: JSON.stringify(r)}] : [];
async function fixture() {
    let remote = null, failRead = false, failFriends = false, writes = [], texts = [], friendRequests = 0, pendingWrite = null, holdWrite = false;
    let holdRead = false, heldReads = [], readRequests = 0, holdFriends = false, heldFriends = [];
    let message, start, end, move, timerId = 0;const timers = new Map();
    const ctx = new Proxy({}, {get: (_, name) => name === 'measureText' ? s => ({width: s.length * 12}) : name === 'fillText' ? s => texts.push(s) : () => {}, set: () => true});
    const wx = {
        getSharedCanvas: () => ({width: 520, height: 800, getContext: () => ctx}),
        getUserCloudStorage: opt => {readRequests++; if(holdRead) heldReads.push(opt); else if(failRead) opt.fail({}); else opt.success({KVDataList: kv(remote)});},
        setUserCloudStorage: opt => {
            const value=JSON.parse(opt.KVDataList[0].value);writes.push(value);
            const operation={commit:()=>{remote=value;},callback:()=>opt.success({})};
            if(holdWrite) pendingWrite=operation; else {operation.commit();operation.callback();}
        },
        getFriendCloudStorage: opt => {friendRequests++;if(holdFriends) heldFriends.push(opt); else if(failFriends) opt.fail({}); else opt.success({data: [{nickname: 'fixture', KVDataList: kv(remote)}]});},
        createImage: () => ({}), onMessage: cb => message = cb, onTouchStart: cb => start = cb, onTouchEnd: cb => end = cb, onTouchMove: cb => move = cb, onTouchCancel: () => {}
    };
    const context = {wx, require: () => model, setTimeout: (cb,ms) => {const id=++timerId;timers.set(id,{cb,ms});return id;}, clearTimeout: id => timers.delete(id)};
    vm.runInNewContext(fs.readFileSync(require.resolve('./index.js'), 'utf8'), context);
    const flush = async () => { for (let i = 0; i < 30; i++) await Promise.resolve(); };
    const send = (command, r) => message({type: 'islands-friends-v1', command, record: r, width: 520, height: 800});
    const expireRequests=async()=>{for(const [id,timer] of [...timers])if(timer.ms===10000){timers.delete(id);timer.cb();}await flush();};
    send('open', record(6, 100)); await flush(); assert.equal(writes.length, 1); assert(texts.includes('已通关 6 关'));
    texts=[];send('open', record(6, 200));await flush();assert.equal(writes.length,1,'same score rewrote time');assert.equal(remote.achievedAtMs,100);
    send('sync', record(5, 300));await flush();assert.equal(writes.length,1,'lower score overwrote cloud');
    send('sync', record(7, 90, 'b'));await flush();assert.equal(remote.completedCount,7);assert.equal(remote.achievedAtMs,101);assert.equal(remote.marker,'a'.repeat(32));
    failRead=true;send('open',record(8,400));await flush();assert.equal(writes.length,2);assert(texts.includes('好友排行加载失败'),'read failure missing');
    failRead=false;failFriends=true;send('open',record(8,400));await flush();assert.equal(remote.completedCount,8);assert(texts.includes('名次暂不可用'));
    failFriends=false;send('open',record(8,400));await flush();assert.equal(writes.length,3);
    holdWrite=true;send('sync',record(9,500));await flush();send('sync',record(10,600));await flush();assert.equal(writes.length,4,'concurrent write escaped serialization');
    holdWrite=false;pendingWrite.commit();pendingWrite.callback();await flush();assert.equal(writes.length,5);assert.equal(remote.completedCount,10);
    message({type:'WXDestroy'});const previous=texts.length;send('sync',record(11,700));await flush();assert.equal(texts.length,previous,'hidden callback painted');
    message({type:'WXRender',x:20,y:100,width:1040,height:1600,devicePixelRatio:2});send('layout');send('open',record(11,700));await flush();
    start({touches:[{clientX:30,clientY:80}]});move({touches:[{clientX:30,clientY:60}]});end({changedTouches:[{clientX:30,clientY:60}]});
    // No callback -> bounded failure -> a fresh request. The old read cannot resume a merge.
    holdRead=true;texts=[];send('open',record(12,800));await flush();const staleRead=heldReads.pop(),beforeReads=readRequests;
    await expireRequests();assert(texts.includes('好友排行加载失败'));
    holdRead=false;send('open',record(12,800));await flush();assert(readRequests>beforeReads);assert.equal(remote.completedCount,12);
    const beforeLateRead=writes.length;staleRead.success({KVDataList:kv(record(1,1))});staleRead.fail({});await flush();assert.equal(writes.length,beforeLateRead);assert.equal(remote.completedCount,12);
    // Timed-out writes retain an exclusive ticket until callback or read-back proves commit.
    holdWrite=true;send('open',record(13,900));await flush();const lateWrite=pendingWrite;
    await expireRequests();const beforePendingRetry=writes.length;send('open',record(14,1000));await flush();assert.equal(writes.length,beforePendingRetry,'unresolved write allowed a parallel overwrite');
    holdWrite=false;lateWrite.commit();lateWrite.callback();await flush();assert.equal(remote.completedCount,14,'late completion did not recover newest score');
    // Commit succeeds but acknowledgement is lost: read-back releases only the matching ticket.
    holdWrite=true;send('open',record(15,1100));await flush();const lostAck=pendingWrite;lostAck.commit();await expireRequests();
    send('open',record(16,1200));await flush();const newerWrite=pendingWrite;assert.notEqual(newerWrite,lostAck);assert.equal(writes.at(-1).completedCount,16);
    lostAck.callback();await flush();const beforeNewTicket=writes.length;send('sync',record(17,1300));await flush();assert.equal(writes.length,beforeNewTicket,'old acknowledgement cleared newer write ticket');
    holdWrite=false;newerWrite.commit();newerWrite.callback();await flush();assert.equal(remote.completedCount,17);
    // A late friend response from a closed generation cannot repaint the reopened list.
    holdFriends=true;send('open',record(17,1300));await flush();const staleFriends=heldFriends.pop();message({type:'WXDestroy'});
    holdFriends=false;send('open',record(17,1300));await flush();texts=[];staleFriends.success({data:[{nickname:'STALE RESPONSE',KVDataList:kv(record(20,1,'c'))}]});await flush();assert(!texts.includes('STALE RESPONSE'));
    // The only visible row is self: empty-state profile degradation must include a working retry.
    send('open',record(17,1300));await flush();assert(texts.includes('暂无可显示的好友成绩'));assert(texts.includes('部分资料暂不可用'));assert(texts.includes('重试'));
    const beforeProfileRetry=friendRequests;start({touches:[{clientX:471,clientY:84}]});end({changedTouches:[{clientX:471,clientY:84}]});await flush();assert(friendRequests>beforeProfileRetry,'empty profile retry did not request friends');
    for(let i=1;i<writes.length;i++)assert(writes[i].completedCount>=writes[i-1].completedCount,'write history decreased');
    assert(friendRequests>3);console.log('LEADERBOARD RUNTIME PASS: no-callback timeout/recovery; stale reads/friends; unresolved writes; late acknowledgements; read-back reconciliation; empty profile retry; serialized monotonic history; lifecycle/touch');
}
fixture().catch(error=>{console.error(error);process.exitCode=1;});
