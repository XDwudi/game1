"""Audit actual runtime catalog. Estimates do not substitute for human playtests."""
from pathlib import Path
import csv, json, random, re, statistics
root = Path(__file__).resolve().parents[1]
source = (root/'Tidebreak/Assets/Scripts/Core/GameData.cs').read_text(encoding='utf-8-sig')
catalog = list(csv.DictReader((root/'Artifacts/species-catalog.csv').open(encoding='utf-8-sig')))
assert len(catalog) == len({r['name'] for r in catalog}) == 119
assert sum(r['boss']=='false' for r in catalog) == 108
specs = re.findall(r'new WeaponSpec\("([^"]+)",\s*([\d.]+),\s*([\d.]+)f,\s*([\d.]+)f,\s*(\d+),\s*(\d+),', source)
assert len(specs)==6
weapons=[]
for index,(name,damage,interval,reload,magazine,pellets) in enumerate(specs):
    damage,interval,reload=map(float,(damage,interval,reload))
    magazine,pellets=int(magazine),int(pellets)
    interval=(.11*2+.3)/3 if index==4 else interval
    weapons.append(dict(weapon=name,damage=damage,pellets=pellets,magazine=magazine,effective_interval=round(interval,4),reload=reload,sustained_dps_all_hits=round(damage*pellets*magazine/(interval*magazine+reload),2)))
def campaign_income(seed,air_chance,weak_chance):
    rng=random.Random(seed);income=0;milestones=[]
    for island in range(9):
        stage=island+1;quota=3 if island<2 else 4 if island<6 else 5
        tier=0 if stage<2 else 1 if stage<4 else 2 if stage<7 else 3
        pool=[r for r in catalog if r['boss']=='false' and int(r['island'])==stage and int(r['lure'])<=tier]
        for _ in range(quota):
            row=rng.choice(pool);elite=rng.random()<.08+stage*.018;quality=rng.random()
            bonus=2.6 if quality<.045 else 1.6 if quality<.16 else 1
            bonus*=1.65 if elite else 1
            bonus*=1.25 if rng.random()<air_chance else 1
            bonus*=1.15 if rng.random()<weak_chance else 1
            income+=round(int(row['value'])*bonus*1.15)
        bonus=1.15 if rng.random()<weak_chance else 1
        income+=25+round(int(catalog[108+island]['value'])*1.15*bonus)+50+stage*10
        milestones.append(income)
    return income,milestones
economy=[]
for label,air,weak in [('no_execution_bonus',0,0),('mixed_execution',.2,.35)]:
    runs=[campaign_income(seed,air,weak) for seed in range(10000)]
    samples=sorted(row[0] for row in runs)
    economy.append(dict(scenario=label,earned_p10=samples[1000],earned_median=statistics.median(samples),earned_p90=samples[9000],median_cumulative_by_island=[statistics.median(row[1][i] for row in runs) for i in range(9)]))
bosses=[]
for index,row in enumerate(catalog[108:]):
    level=min(5,1+min(index,8)//2);armor=.65 if row['trait']=='Armored' else 1;estimates={}
    for weapon in (weapons[0],weapons[2]):
        dps=weapon['sustained_dps_all_hits']*(1+.18*level)*1.06*.7*(.6+.4*.72)*armor
        estimates[weapon['weapon']]=round(float(row['hp'])/dps,1)
    bosses.append(dict(name=row['name'],health=float(row['hp']),attack=row['attack'],weapon_upgrade_level=level,body_shot_output_seconds=estimates))
report=dict(version='0.3.0',species=119,normal_species=108,anatomical_families=12,primary_attack_mechanics=14,fixed_offers=23,random_perks=20,weapons=weapons,bosses=bosses,economy=economy,
limits='Analytical estimate, not human playtesting. DPS assumes all pellets hit within 10m; shotgun damage reaches 30% at 35m before spread misses. Boss estimates use 70% body-shot accuracy, base crit, 60% exposure / 40% guard and actual armor trait. Excludes movement and aim reacquisition. Burst recovery modeled; arc chaining excluded.',
economy_assumptions='10000 seeds; minimum 37 research fish, 9 bosses, 9 first relics and 9 story hand-ins; Shoal routes; no fortune/chum; strongest island-licensed lure. Excludes initial 25 coins, guardian loot, hidden caches, repeated catches to reach two species, extra fishing, healing and purchases.',
upgrade_costs=dict(all_weapon_levels=825,all_rod_levels=315,all_armor_levels=390,five_paid_guns=1520,three_lures=595,three_bag_levels=330,brake_levels=345,scope_levels=230,boots_levels=230,forbidden_bait=260,all_fixed_equipment=5040,soup_40hp=25))
text=json.dumps(report,ensure_ascii=False,indent=2)
(root/'Artifacts/balance-audit.json').write_text(text,encoding='utf-8')
print(text)
