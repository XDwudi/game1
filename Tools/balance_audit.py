"""Pacing estimates, not an assertion that an automated pilot represents human play."""
from pathlib import Path
import json
import re

root = Path(__file__).resolve().parents[1]
source = (root / 'Tidebreak/Assets/Scripts/Core/GameData.cs').read_text(encoding='utf-8')
weapons = re.findall(r'new WeaponSpec\("([^"]+)", ([\d.]+), ([\d.]+)f, ([\d.]+)f, (\d+), (\d+),', source)
results = []
for name, damage, interval, reload, magazine, pellets in weapons:
    damage, interval, reload = map(float, (damage, interval, reload))
    magazine, pellets = int(magazine), int(pellets)
    sustained = damage * pellets * magazine / (interval * (magazine - 1) + reload)
    results.append({'weapon': name, 'base_sustained_dps_all_hits': round(sustained, 2)})

# Aiming 70% of shots at the body, 60% boss vulnerability uptime,
# one damage relic / one weapon upgrade by the first guardian,
# and three damage relics / five weapon upgrades by the final guardian.
encounters = []
pistol = results[0]['base_sustained_dps_all_hits']
for name, health, upgrades, relics in [('铁壳领主', 900, 1, 1), ('噬光灯笼鱼', 1700, 3, 2), ('风暴利维坦', 2600, 5, 3), ('克拉肯', 4000, 5, 3), ('幽海白鲸', 3500, 5, 3)]:
    expected = pistol * (1 + .18 * upgrades) * (1 + .14 * relics) * 1.06 * .7 * .84
    encounters.append({'boss': name, 'pistol_body_only_seconds': round(health / expected, 1)})
report = {'assumptions': '70% accuracy, base 8% critical chance, no weakpoint bonus, estimated 60% vulnerability uptime, upgrades as listed in script; movement and target reacquisition excluded.', 'weapons': results, 'pacing': encounters}
(root / 'Artifacts').mkdir(exist_ok=True)
(root / 'Artifacts/balance-audit.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps(report, ensure_ascii=False, indent=2))
