"""Source-derived output and a stated income model; not a substitute for human playtests."""
from pathlib import Path
import json
import random
import re
import statistics
root = Path(__file__).resolve().parents[1]
source = (root / 'Tidebreak/Assets/Scripts/Core/GameData.cs').read_text(encoding='utf-8-sig')
specs = re.findall(r'new WeaponSpec\("([^"]+)", ([\d.]+), ([\d.]+)f, ([\d.]+)f, (\d+), (\d+),', source)
assert len(specs) == 3
weapons = []
for name, damage, interval, reload, magazine, pellets in specs:
    damage, interval, reload = map(float, (damage, interval, reload))
    magazine, pellets = int(magazine), int(pellets)
    weapons.append({
        'weapon': name,
        'auto_reload_dps_all_hits': round(damage * pellets * magazine / (interval * magazine + reload), 2),
        'prompt_manual_reload_dps_all_hits': round(damage * pellets * magazine / (interval * (magazine - 1) + max(interval, reload)), 2),
    })
# Body shots only, 70% accuracy, base crit, 60% exposure / 40% guarded at 0.7x.
# Purchase levels are scenarios, not guaranteed equipment or free perks.
encounters = []
for name, level in [('Crab', 1), ('Angler', 3), ('Leviathan', 4), ('Kraken', 4), ('WhiteWhale', 4)]:
    hp = int(re.search(r'kind == CreatureKind\.' + name + r'\) return (\d+)', source).group(1))
    row = {'boss': name, 'health': hp, 'weapon_upgrade_level': level, 'damage_perks': 0}
    for weapon in (weapons[0], weapons[2]):
        dps = weapon['auto_reload_dps_all_hits'] * (1 + .18 * level) * (1 + .08 * .75) * .7 * (.6 + .4 * .7)
        row[weapon['weapon'] + '_output_seconds'] = round(hp / dps, 1)
    encounters.append(row)
def campaign_income(seed, air_chance, weak_chance):
    rng = random.Random(seed)
    income = 0
    for stage in range(1, 10):
        act = (stage - 1) // 3
        boss = stage % 3 == 0
        for _ in range(1 if boss else 3 + act):
            elite = not boss and rng.random() < .1 + act * .12
            quality = rng.random() if not boss else 1
            multiplier = 2.6 if quality < .045 else 1.6 if quality < .16 else 1
            air = not boss and rng.random() < air_chance
            weak = rng.random() < weak_chance
            basis = 150 + stage * 15 if boss else (52 if elite else 26) + stage * 3
            income += round(basis * multiplier * (1.25 if air else 1) * (1.15 if weak else 1) * 1.15)
    return income

economy = []
for label, air, weak in [('no_execution_bonus', 0, 0), ('mixed_execution', .2, .35)]:
    samples = sorted(campaign_income(i, air, weak) for i in range(10000))
    economy.append({'scenario': label, 'air_kill_chance': air, 'weak_kill_chance': weak,
                    'earned_p10': samples[1000], 'earned_median': statistics.median(samples),
                    'earned_p90': samples[9000]})
report = {
    'version': '0.2.0',
    'limits': 'Analytical model, not player telemetry. DPS excludes movement and reacquisition. Shotgun values assume every pellet hits within 10 m; 35 m damage is 36% before missed pellets.',
    'boss_assumptions': '70% accuracy, 8% crit, no weakpoint hits, no damage perks, 60% exposed / 40% guarded; upgrades explicitly listed. Body-shot output time excludes all downtime.',
    'weapons': weapons, 'boss_pacing': encounters,
    'economy_assumptions': '10,000 deterministic seeds; only the 24 required normal fish and 3 bosses; all Shoal routes; no fortune perk; all catches sold. Excludes initial 25 coins, healing expense, extra fishing and rare bosses. Base final-hit bonuses also apply to boss contracts in the runtime.',
    'economy': economy,
    'fixed_shop_costs': {'all_5_weapon_levels': 825, 'all_3_rod_levels': 315, 'all_3_armor_levels': 390,
                         'both_extra_weapons': 410, 'kraken_bait': 180, 'all_above': 2120,
                         'heal_40_hp': 25, 'one_random_perk_by_act': [80,110,140]},
}
(root / 'Artifacts').mkdir(exist_ok=True)
text = json.dumps(report, ensure_ascii=False, indent=2)
(root / 'Artifacts/balance-audit.json').write_text(text, encoding='utf-8')
print(text)
