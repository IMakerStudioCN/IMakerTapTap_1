
# -*- coding: utf-8 -*-
import io, difflib, re

P = r'E:/AAA/test/Assets/Scripts/AllSoftwares/WebSoftware/baListController.cs'
raw = open(P, 'rb').read()
try:
    text = raw.decode('gbk')
    enc = 'gbk'
except Exception:
    text = raw.decode('utf-8'); enc = 'utf-8'
print('encoding =', enc, '| has BOM =', raw[:3] == b'\xef\xbb\xbf', '| CRLF count =', text.count('\r\n'))

before = text
nl = '\r\n' if '\r\n' in text else '\n'

ensure = nl.join([
    '        private void OnEnable()',
    '        {',
    '            EnsureTodayLists();',
    '            UpdateBarList();',
    '        }',
    '',
    '        // \u8865\u9f50\u201c\u5e8f\u7ae0\uff08\u7b2c 0 \u5929\uff09~\u5f53\u5929\u201d\u5e94\u8be5\u62e5\u6709\u7684\u5e16\u5b50\uff1b\u987a\u4fbf\u6e05\u6389\u8bfb\u6863\u53cd\u5e8f\u5217\u5316\u51fa\u6765\u7684\u7a7a\u58f3\u6761\u76ee',
    '        private void EnsureTodayLists()',
    '        {',
    '            if (baListSystem == null) return;',
    '',
    '            List<baLineList_SO> owned = baListSystem.GetbaLineListYouHave();',
    '',
    '            if (owned != null)',
    '            {',
    '                owned.RemoveAll(x => x == null || x.baLineList == null || x.baLineList.Count == 0);',
    '            }',
    '',
    '            int today = this.GetModel<ITimeModel>().days;',
    '',
    '            for (int day = 0; day <= today; day++)',
    '            {',
    '                baListSystem.AddbaLineList(day);',
    '            }',
    '        }',
    ''
])

assert 'private void OnEnable' not in before, 'already has OnEnable'
i_start = before.index('        private void Start()')
before = before[:i_start] + ensure + nl + before[i_start:]

# Start() 里：在 UpdateBarList() 之前补一次
i_start = before.index('        private void Start()')
i_update = before.index('            UpdateBarList();', i_start)
before = before[:i_update] + '            EnsureTodayLists();' + nl + before[i_update:]

after = before
open(P, 'wb').write(after.encode(enc))

d = list(difflib.unified_diff(before=text.splitlines(), a=after.splitlines(), lineterm='', n=3))
out = ['--- diff (before -> after) ---'] + d[:80]
open(r'E:/AAA/test/_patch.txt', 'w', encoding='utf-8').write('\n'.join(out))
print('patched, brace balance:', after.count('{') - after.count('}'))
