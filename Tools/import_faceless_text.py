import json
from pathlib import Path
p=Path('Assets/Localization/strings.tsv')
rows=json.loads(Path('Tools/faceless_story_dialogues.json').read_text(encoding='utf-8'))['rows']
lines=p.read_text(encoding='utf-8-sig').splitlines()
updates={r[0]:'\t'.join(x.replace('\n',r'\n') for x in r) for r in rows}
for i,line in enumerate(lines):
    key=line.split('\t',1)[0]
    if key in updates: lines[i]=updates.pop(key)
lines.extend(updates.values())
p.write_text('\n'.join(lines)+'\n',encoding='utf-8')
print(f'Updated {len(rows)} entries in four languages.')
