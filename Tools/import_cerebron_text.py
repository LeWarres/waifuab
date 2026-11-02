import json
from pathlib import Path

source = Path('Assets/Localization/strings.tsv')
target = Path('Tools/cerebron_story_dialogues.json')
rows = json.loads(target.read_text(encoding='utf-8-sig'))
raw=source.read_bytes()
newline='\r\n' if b'\r\n' in raw else '\n'
lines = raw.decode('utf-8-sig').splitlines()
existing = {line.split('\t', 1)[0]: line.split('\t') for line in lines}
keys = {row['key'] for row in rows}
# The same exchange from Faceless's story, with the speaker sides reversed by Story.Part.
for side, count in [('before', 4), ('after', 2)]:
    for n in range(1, count + 1):
        key = f'story.cerebron.2.{side}.{n}'
        cells = existing[f'story.faceless.1.{side}.{n}']
        row = dict(zip(['key', 'es', 'en', 'pt', 'ja'], [key] + cells[1:]))
        if key not in keys:
            rows.append(row)
            keys.add(key)
        else:
            next(r for r in rows if r['key']==key).update(row)
assert len(rows) == 54 and len(keys) == len(rows)
updates = {row['key']: '\t'.join(row[field].replace('\n', r'\n') for field in ['key', 'es', 'en', 'pt', 'ja']) for row in rows}
for i, line in enumerate(lines):
    key = line.split('\t', 1)[0]
    if key in updates:
        lines[i] = updates.pop(key)
lines.extend(updates.values())
source.write_bytes((newline.join(lines) + newline).encode('utf-8'))
target.write_text(json.dumps(rows, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
lookup={r['key']:r['es'] for r in rows}
report=['# Historia de Cerebron','','Voz inteligente y ruda de héroe de acción de los noventa. Habla con firmeza y humor seco; observa el equipo y el terreno, busca salidas y sabe cuándo conviene negociar. La narración sigue siendo una conversación en primera persona, sin remates en cada frase ni conocimientos ajenos.','','Ruta: espacio/Roger → planeta alienígena/BBA → fortaleza/holo con Faceless → Thulu/Crochtrain → Baratito con Doris y un amigo → Gracias. Arte y mecánicas conservados.','']
for c in range(4):
    report += [f'## {c+1}. '+lookup[f'story.cerebron.{c}.title'],'']
    for side in ['intro','before','after','outro']:
        group=sorted((r for r in rows if r['key'].startswith(f'story.cerebron.{c}.{side}.')),key=lambda r:int(r['key'].split('.')[-1]))
        report += [f'### {side}','']+[r['es']+'\n' for r in group]
Path('Tools/cerebron_story.md').write_text('\n'.join(report).rstrip()+'\n',encoding='utf-8')
print(f'{len(rows)} Cerebron keys in four languages; original Faceless encounter preserved.')
