"""Read-only content audit, apart from the requested report. Uses Git's ignore rules.
Run: python Tools/Audit-Repository.py [--forbidden-root PATH]
"""
import argparse
import hashlib
import json
import pathlib
import re
import subprocess
import wave
import array

root = pathlib.Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser()
parser.add_argument('--forbidden-root', action='append', default=[])
args = parser.parse_args()
paths = subprocess.check_output(['git','ls-files','--cached','--others','--exclude-standard','-z'], cwd=root).decode().split('\0')
paths = sorted(set(p for p in paths if p and (root/p).is_file()))
failures = []
for name in paths:
    p = root/name
    if any(segment in name.split('/') for segment in ('Library','Temp','PackageCache','UserSettings','.git')):
        failures.append('Generated/local state included: '+name)
    if re.search(r'\.(blend\d*|keystore|jks|pfx|pem|key)$',name,re.I):
        failures.append('Excluded authoring/credential extension: '+name)
    data = p.read_bytes()
    if b'\0' not in data and p.suffix not in ('.png','.wav','.jpg'):
        text = data.decode('utf-8-sig',errors='replace').lower().replace('\\','/')
        for forbidden in args.forbidden_root:
            if forbidden.lower().replace('\\','/').rstrip('/') in text:
                failures.append('Reference to forbidden workspace in '+name)
    if name.startswith('UnityXRTemplate/Assets/') and not p.name.startswith('.') and not name.endswith('.meta') and not (root/(name+'.meta')).exists():
        failures.append('Missing asset metadata: '+name)
manifest = json.loads((root/'UnityXRTemplate/Packages/manifest.json').read_text())
for package, version in manifest['dependencies'].items():
    if version.startswith(('file:','git+file:')):
        failures.append('Local package dependency: '+package)
journal = root/'Docs/PROJECT_JOURNAL.md'
if not journal.exists() or journal.stat().st_size != 0:
    failures.append('Project journal is not empty')
audio = []
for p in sorted((root/'UnityXRTemplate/Assets/StarterKit/Audio').glob('*.wav')):
    with wave.open(str(p),'rb') as w:
        samples = array.array('h',w.readframes(w.getnframes()))
        peak = max(abs(x) for x in samples)/32768
        audio.append({'file':p.name,'seconds':round(len(samples)/w.getframerate()/w.getnchannels(),3),'sample_rate':w.getframerate(),'channels':w.getnchannels(),'peak':round(peak,4),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()})
        if peak >= .999: failures.append('Clipping audio: '+p.name)
report = {'file_count':len(paths),'failures':failures,'journal_bytes':journal.stat().st_size,'audio':audio,'notes':['File/path/metadata audit, not a legal opinion.','WAV checks establish format and peak levels, not a human listening review.','Provenance and package licensing reviewed separately in Docs/REDISTRIBUTION.md.']}
(root/'Reports').mkdir(exist_ok=True)
(root/'Reports/CONTENT_AUDIT.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps({'files':len(paths),'failures':failures,'journal_bytes':journal.stat().st_size},indent=2))
raise SystemExit(bool(failures))
