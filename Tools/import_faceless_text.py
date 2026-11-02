import json
from pathlib import Path
p=Path('Assets/Localization/strings.tsv')
rows=json.loads(Path('Tools/faceless_story_dialogues.json').read_text(encoding='utf-8'))['rows']
assert len({r[0] for r in rows})==len(rows)
assert all(len(r)==5 and all(r) for r in rows)
raw=p.read_bytes();newline='\r\n' if b'\r\n' in raw else '\n'
lines=raw.decode('utf-8-sig').splitlines()
updates={r[0]:'\t'.join(x.replace('\n',r'\n') for x in r) for r in rows}
for i,line in enumerate(lines):
    key=line.split('\t',1)[0]
    if key in updates: lines[i]=updates.pop(key)
lines.extend(updates.values())
p.write_bytes((newline.join(lines)+newline).encode('utf-8'))
report=['# Historia completa de Faceless','','Voz respetuosa, muy amable y algo excéntrica de artista. Observa detalles, pide permiso, escucha y cuida a T.exe sin decidir por ella. La cortesía no lo obliga a aceptar nuevas órdenes. Narración conversacional en primera persona.','','Ruta: planeta alienígena (BBA) → cuestionario alienígena → fortaleza (holo contra Cerebron) → espacio (Moho) → descenso → selva (R-20) → bosque (dibujos) → Gracias. Ilustraciones conservadas.','']
lookup={r[0]:r[1] for r in rows}
for c in range(5):
    report += [f'## {c+1}. '+lookup[f'story.faceless.{c}.title'],'']
    for side in ['intro','before','after','outro']:
        group=[r for r in rows if r[0].startswith(f'story.faceless.{c}.{side}.')]
        group.sort(key=lambda r:int(r[0].split('.')[-1]))
        report += [f'### {side}','']+[r[1]+'\n' for r in group]
report += ['## Descenso y bosque','']+[r[0]+'\n'+r[1]+'\n' for r in rows if r[0].startswith(('descent.','forest.'))]
Path('Tools/faceless_story.md').write_text('\n'.join(report).rstrip()+'\n',encoding='utf-8')
print(f'Updated {len(rows)} entries in four languages.')
