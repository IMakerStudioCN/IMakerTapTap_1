
# -*- coding: utf-8 -*-
import re, os, glob, uuid, json

BASE = r'E:/AAA/test/Assets/Config/baLine/mybaReply'
LIST = os.path.join(BASE, 'list')

TPL_EMPTY   = open(os.path.join(BASE, 'balaReply05.asset'), 'rb').read().decode('utf-8')   # clicks/replies 空
TPL_ZERO    = open(os.path.join(BASE, 'evilReply05.asset'), 'rb').read().decode('utf-8')   # clicks/replies = 0
TPL_META    = open(os.path.join(BASE, 'balaReply05.asset.meta'), 'rb').read().decode('utf-8')

def yq(s):
    buf = []
    for ch in s:
        o = ord(ch)
        if ch == '"': buf.append('\\"')
        elif ch == '\\': buf.append('\\\\')
        elif 0x20 <= o <= 0x7e: buf.append(ch)
        else: buf.append('\\u%04X' % o)
    return '"' + ''.join(buf) + '"'

used = set()
for root, dirs, files in os.walk(r'E:/AAA/test/Assets'):
    for f in files:
        if f.endswith('.meta'):
            t = open(os.path.join(root, f), 'rb').read().decode('utf-8', 'ignore')
            m = re.search(r'^guid:\s*([0-9a-f]{32})', t, re.M)
            if m: used.add(m.group(1))
def new_guid():
    while True:
        g = uuid.uuid4().hex
        if g not in used:
            used.add(g); return g

CAT = {'bala':'balaReply','evil':'evilReply','fair':'fairReply','normal':'normalReply','other':'otherReply'}
NEW = [
    ('bala', 6, '狗不理', u'虾仁的包子还有吗'),
    ('bala', 7, '鱼3', u'第二个包子半价哦'),
    ('bala', 8, '胡饼', u'这和我买的猫粮有什么关系'),
    ('evil', 6, '瓶中船', u'这文笔也敢发出来，勇气可嘉'),
    ('evil', 7, '四季平安', u'浪费我三分钟生命'),
    ('evil', 8, '老大', u'写的什么玩意儿，退钱'),
    ('fair', 5, '建国路住户', u'说得在理'),
    ('fair', 6, '北岛信物', u'有道理，我也这么觉得'),
    ('fair', 7, '新晋士兵', u'中肯'),
    ('normal', 5, '等风起', u'就那样吧'),
    ('normal', 6, '一氧化二氢', u'看完了，没什么特别的感觉'),
    ('normal', 7, '六月雨', u'还行，期待后续'),
    ('other', 3, '404', u'路由器坏了有没有人懂'),
    ('other', 4, '绝对零度', u'顺便问一下东山食堂几点关门'),
    ('other', 5, '你的小海星', u'今晚有流星雨记得看'),
]

made = {}
for cat, idx, writter, content in NEW:
    name = '%s%02d' % (CAT[cat], idx)
    p = os.path.join(BASE, name + '.asset')
    assert not os.path.exists(p), p
    tpl = TPL_ZERO if cat == 'evil' else TPL_EMPTY
    t = tpl
    t = re.sub(r'^  m_Name: \S+$', '  m_Name: %s' % name, t, count=1, flags=re.M)
    t = re.sub(r'^  content: ".*"$', '  content: %s' % yq(content), t, count=1, flags=re.M)
    t = re.sub(r'^  writter: ".*"$', '  writter: %s' % yq(writter), t, count=1, flags=re.M)
    open(p, 'wb').write(t.encode('utf-8'))
    g = new_guid()
    bundle = '%s_asset' % name.lower()
    mt = re.sub(r'^guid: [0-9a-f]+$', 'guid: ' + g, TPL_META, count=1, flags=re.M)
    mt = re.sub(r'^(  assetBundleName: ).*$', r'\g<1>' + bundle, mt, count=1, flags=re.M)
    open(p + '.meta', 'wb').write(mt.encode('utf-8'))
    made.setdefault(cat, []).append((name, g, bundle))

# 追加进对应 list（保持作者/文件顺序：接在最后一条之后）
for cat, items in made.items():
    lp = os.path.join(LIST, CAT[cat] + 'List.asset')
    t = open(lp, 'rb').read().decode('utf-8')
    entries = re.findall(r'^  - \{fileID: 11400000, guid: [0-9a-f]+, type: 2\}$', t, re.M)
    last = entries[-1]
    add = ''.join('  - {fileID: 11400000, guid: %s, type: 2}' % g for _, g, _ in items)
    t2 = t.replace(last, last + '\n' + add, 1) if t.count(last) >= 1 else None
    # 用更稳的方式：在最后一条 entry 后插入
    idx = t.rfind(last)
    t2 = t[:idx + len(last)] + '\n' + add + t[idx + len(last):]
    open(lp, 'wb').write(t2.encode('utf-8'))

# 顺手修掉 normalReply04 遗留的 bundle 名（还是 "normalreply03 1_asset"）
mp = os.path.join(BASE, 'normalReply04.asset.meta')
mt = open(mp, 'rb').read().decode('utf-8')
mt2 = re.sub(r'^(  assetBundleName: ).*$', r'\g<1>normalreply04_asset', mt, count=1, flags=re.M)
fixed = mt2 != mt
if fixed: open(mp, 'wb').write(mt2.encode('utf-8'))

print(json.dumps({'made': {k: [(n, b) for n, g, b in v] for k, v in made.items()}, 'normalReply04_bundle_fixed': fixed}, ensure_ascii=True, indent=1))
