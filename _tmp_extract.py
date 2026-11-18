
import zipfile, re, sys
z = zipfile.ZipFile('/tmp/docx1/doc.docx')
xml = z.read('word/document.xml').decode('utf-8')
# split into paragraphs
paras = re.split(r'</w:p>', xml)
out = []
for p in paras:
    texts = re.findall(r'<w:t[^>]*>(.*?)</w:t>', p, re.S)
    t = ''.join(texts)
    t = t.replace('&amp;','&').replace('&lt;','<').replace('&gt;','>').replace('&quot;','"').replace('&apos;',"'")
    out.append(t)
print('TOTAL_PARAS', len(out))
print('=====')
for i,t in enumerate(out):
    print(f'[{i}] {t}')
