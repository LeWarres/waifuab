import json
from pathlib import Path

source = Path('Assets/Localization/strings.tsv')
target = Path('Tools/cerebron_story_dialogues.json')
rows = json.loads(target.read_text(encoding='utf-8-sig'))
lines = source.read_text(encoding='utf-8-sig').splitlines()
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
assert len(rows) == 54 and len(keys) == len(rows)
updates = {row['key']: '\t'.join(row[field].replace('\n', r'\n') for field in ['key', 'es', 'en', 'pt', 'ja']) for row in rows}
for i, line in enumerate(lines):
    key = line.split('\t', 1)[0]
    if key in updates:
        lines[i] = updates.pop(key)
lines.extend(updates.values())
source.write_text('\n'.join(lines) + '\n', encoding='utf-8')
target.write_text(json.dumps(rows, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print(f'{len(rows)} Cerebron keys in four languages; original Faceless encounter preserved.')
