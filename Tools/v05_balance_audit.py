"""Reproducible design model, not human telemetry or an automated clear certificate.

Python standard library only. Reads current C# weapon/shop/growth prices, reports
source hashes, then compares declared aim assumptions and *paid* progression plans.
`--self-test` checks accounting and model monotonicity; it does not run Unity.
"""
from __future__ import annotations

import argparse
import csv
from dataclasses import asdict, dataclass
import hashlib
import json
import math
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
CODE = ROOT / "Tidebreak/Assets/Scripts"
if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")


@dataclass(frozen=True)
class Aim:
    name: str
    hit: float
    weak_given_hit: float
    quick_reload: float
    boss_firing_uptime: float
    normal_firing_uptime: float
    reaction_seconds: float
    kits_per_island: int


# Deliberate sensitivity inputs, not percentiles measured from players. Weak
# probability is conditional on a hit. No profile gets perfect aim/positioning.
AIMS = [
    Aim("保守", .55, .08, .10, .55, .78, .55, 2),
    Aim("普通", .72, .28, .40, .68, .86, .35, 1),
    Aim("精准", .90, .65, .85, .80, .93, .20, 0),
]
PLANS = {
    "精准猎手": {
        1: ["weapon"], 2: ["key0", "rod"], 3: ["weapon", "hull"],
        4: ["harpoon", "scope"], 5: ["key5", "weapon"],
        6: ["hull", "brake"], 7: ["weapon", "scope"],
        8: ["key6", "hull"], 9: ["weapon"],
    },
    "近岸机动": {
        1: ["hull"], 2: ["shotgun", "key2"], 3: ["weapon", "rod"],
        4: ["hull", "brake"], 5: ["key5", "weapon", "boots"],
        6: ["hull", "brake"], 7: ["weapon"],
        8: ["key8", "boots"], 9: ["weapon"],
    },
    "元素渔猎": {
        1: ["rod"], 2: ["key1", "weapon"], 3: ["carbine", "hull"],
        4: ["weapon"], 5: ["key4", "hull"], 6: ["weapon", "rod"],
        7: ["arc"], 8: ["key7", "hull"], 9: ["weapon"],
    },
}


def read_source(relative: str) -> str:
    return (CODE / relative).read_text(encoding="utf-8-sig")


def numbers(text: str) -> list[float]:
    return [float(v) for v in re.findall(r"-?(?:\d+\.\d+|\.\d+|\d+)", text)]


def read_contract() -> dict:
    data = read_source("Core/GameData.cs")
    shop = read_source("Core/ShopCatalog.cs")
    build = read_source("Core/VoyageBuilds.cs")
    player = read_source("Combat/AnglerController.cs")
    source_files = ["Core/GameData.cs", "Core/ShopCatalog.cs", "Core/VoyageBuilds.cs",
                    "Core/ExpeditionContent.cs", "Core/IslandObjectives.cs",
                    "Core/ExpeditionDirector.cs", "Combat/AnglerController.cs",
                    "Combat/Enemy.cs"]
    if (CODE / "Core/EncounterTuning.cs").exists():
        source_files.append("Core/EncounterTuning.cs")
    research_reward = 0
    if (CODE / "Core/CoastalResearch.cs").exists():
        source_files.append("Core/CoastalResearch.cs")
        research_reward = int(re.search(r'const int stipend = (\d+)', read_source("Core/CoastalResearch.cs"))[1])
    specs = []
    for match in re.finditer(r'new WeaponSpec\("([^"]+)",([^\)]+)\)', data):
        d, interval, reload, magazine, pellets, spread = numbers(match[2])
        specs.append(dict(name=match[1], damage=d, interval=interval, reload=reload,
                          magazine=int(magazine), pellets=int(pellets), spread=spread))
    assert len(specs) == 6, "Weapon source format changed; audit needs updating."
    offers = {}
    pattern = r'new ShopOffer\("([^"]+)","([^"]+)","(?:[^"\\]|\\.)*",(\d+),(\d+),(\d+),(\d+),'
    for m in re.finditer(pattern, shop):
        offers[m[1]] = dict(name=m[2], category=int(m[3]), island=int(m[4]),
                             cost=int(m[5]), max=int(m[6]))
    assert all(i in offers for i in ("weapon", "rod", "hull", "medkit", "heal"))
    growth = re.search(r'DamageMultiplier.*?1\s*\+\s*([.\d]+)f\s*\*\s*Mathf.Min\((\d+),weaponLevel\)\s*\+\s*([.\d]+)f\s*\*\s*Mathf.Min\((\d+),damageRelics\)', data)
    assert growth, "Growth source format changed."
    key = re.search(r'Price\(int act\).*?return (\d+)\+act\*(\d+)', build)
    assert key, "Keystone pricing source format changed."
    assert "rhythm>=6" in build and "rhythm+=weak?2:1" in build and "m*=1.85f" in build, "Update rhythm model to match source."
    assert "steamHits>=5" in build and "thunderHits>=7" in build, "Update elemental charge model to match source."
    # Burst intervals are not the single-shot WeaponSpec.interval.
    inner = re.search(r'burstAt=Time.time\+([.\d]+)f/game.Run.FireRateMultiplier', player)
    recovery = re.search(r'burstRemaining==0\)fireAt=Time.time\+([.\d]+)f', player)
    assert inner and recovery
    return dict(weapons=specs, offers=offers, damage_per_level=float(growth[1]),
                damage_level_cap=int(growth[2]), damage_per_relic=float(growth[3]),
                damage_relic_cap=int(growth[4]), key_base=int(key[1]), key_step=int(key[2]),
                burst_inner=float(inner[1]), burst_recovery=float(recovery[1]),
                research_reward=research_reward,
                sources={f: hashlib.sha256((CODE / f).read_bytes()).hexdigest() for f in source_files})


def reel_seconds(rod: int, dt: float = .01) -> float:
    """Exact current green-only hold policy, with no input latency, travel or failure."""
    age, progress, tension = 0., 0., .1
    while age < 40:
        age += dt
        if age <= 1.9:
            continue
        surge = math.sin((age - 1.9) * 2) > .28
        hold = not surge
        progress = max(0., progress + ((.2 * (1 + rod * .24)) if hold else -.018) * dt)
        tension = max(0., tension + ((.12 / (1 + rod * .22)) if hold else -.5) * dt)
        assert tension < 1, "Green-only reference policy unexpectedly snaps the line."
        if progress >= 1:
            return age
    raise AssertionError("Reference reel policy cannot complete.")


def rhythm_rate(aim: Aim) -> float:
    """Stationary event rate of body +1 / weak +2 / miss -1 / trigger at 6."""
    dist = [1., 0., 0., 0., 0., 0.]
    p_weak = aim.hit * aim.weak_given_hit
    for _ in range(2000):
        nxt = [0.] * 6
        for state, probability in enumerate(dist):
            for step, chance in [(-1, 1-aim.hit), (1, aim.hit-p_weak), (2, p_weak)]:
                target = max(0, state+step)
                if target >= 6:
                    target = 0
                nxt[target] += probability*chance
        if max(abs(a-b) for a,b in zip(nxt,dist)) < 1e-12:
            dist = nxt
            break
        dist = nxt
    return dist[4]*p_weak + dist[5]*aim.hit


def sustained_dps(c: dict, weapon: int, aim: Aim, level: int, keys=(), distance=12., boss=True) -> float:
    w = c["weapons"][weapon]
    interval = (c["burst_inner"] * 2 + c["burst_recovery"]) / 3 if weapon == 4 else w["interval"]
    # Successful quick reload at 60% + remaining .12s; expected interpolation.
    reload = w["reload"] * (1 - aim.quick_reload) + (w["reload"] * .60 + .12) * aim.quick_reload
    if 2 in keys:
        reload /= 1.125  # Declared 50% uptime for the 25% reload speed modifier.
    raw = w["damage"] * w["pellets"] * w["magazine"] / (interval * w["magazine"] + reload)
    falloff = 1.0 if weapon != 1 else 1 - .78 * min(1., max(0., (distance - 8) / 19))
    growth = 1 + c["damage_per_level"] * min(level, c["damage_level_cap"])
    weak_bonus = 1 + .4 * aim.weak_given_hit
    if 6 in keys and distance > 12:
        weak_bonus += 1.4 * .35 * aim.weak_given_hit
    base = raw * falloff * growth * aim.hit * weak_bonus * 1.06
    if 0 in keys:
        base *= 1 + .85 * rhythm_rate(aim)
    if 2 in keys:
        base *= 1.125  # 50% of shots occur during three-second dash buff.
    # Counterattack is excluded: attack timing, successful dodges and shot
    # timing require live mechanics. Chain specialization has no single target DPS.
    firing = aim.boss_firing_uptime if boss else aim.normal_firing_uptime
    damage = base * firing
    # v0.5 built-in elemental specializations no longer grant free generic DoT
    # stacks. Hits are banked through cooldown but cannot exceed the threshold.
    hit_frequency = w["magazine"]*w["pellets"]/(interval*w["magazine"]+reload)*aim.hit*firing
    if 1 in keys:
        damage += (24 + min(5,level) * 6) * min(.25, hit_frequency/5)
    if 7 in keys:
        damage += (38 + min(5,level) * 7) * min(.20, hit_frequency/7)
    if 4 in keys:
        damage += base*firing*.20/6
    return damage


def fish_price(stage: int, aim: Aim, lure: int = 0) -> float:
    # Exact expected rarity/elite multipliers; assumes no chum, no fortune,
    # no aerial finishing bonus, and default Shoal route. Monetary rounding
    # contributes less than one coin per fish and is intentionally omitted.
    count = [6, 9, 11, 12][lure]
    base = 25 + (stage - 1) * 7 + (count - 1)
    rarity = .045 * 2.6 + (.16 - .045) * 1.6 + .84
    elite = 1 + (.08 + stage * .018) * .65
    return base * rarity * elite * (1 + .15 * aim.weak_given_hit) * 1.15


def offer_price(c: dict, item: str, levels: dict) -> int:
    if item.startswith("key"):
        return c["key_base"] + int(item[3:]) // 3 * c["key_step"]
    o = c["offers"][item]
    return o["cost"] + levels.get(item, 0) * 40 if 1 < o["max"] <= 5 else o["cost"]


def route_weapon(route: str, owned: dict) -> int:
    return 2 if route == "精准猎手" and owned.get("harpoon") else 1 if route == "近岸机动" and owned.get("shotgun") else 5 if route == "元素渔猎" and owned.get("arc") else 3 if owned.get("carbine") else 0


def economy(c: dict, aim: Aim, route: str, research=False) -> list[dict]:
    wallet, earned, spent = 25., 0., 0.
    owned, keys, result, pending = {}, [], [], []
    for stage in range(1, 10):
        start_wallet = wallet
        # Two optional fish per island, only the first of the campaign is
        # mandatory. No secrets, random relics or mission-minion loot included.
        fish_count = 3 if research else 2
        research_income = c["research_reward"] if research else 0
        fish_income = fish_count * fish_price(stage, aim)
        stipend = 70 + stage * 15
        wallet += fish_income + stipend + research_income
        earned += fish_income + stipend + research_income
        kits = aim.kits_per_island if stage >= 2 else 0
        # One late-game reserve also for precise profile; there is no assertion
        # that anyone actually consumes all the purchased kits.
        kits = max(kits, 1 if stage >= 7 else 0)
        supplies = kits * c["offers"]["medkit"]["cost"] + c["offers"]["heal"]["cost"]
        assert wallet >= supplies
        wallet -= supplies
        spent += supplies
        pending.extend(PLANS[route][stage])
        purchased, optional_fish, deferred = [], 0, []
        for item in pending:
            price = offer_price(c, item, owned)
            # Grinding is a disclosed requirement, capped to avoid silently
            # granting infinite currency. A player can defer any purchase.
            while wallet < price and optional_fish < 8:
                income = fish_price(stage, aim)
                wallet += income
                earned += income
                optional_fish += 1
            if wallet < price:
                deferred.append(item)
                continue
            wallet -= price
            spent += price
            purchased.append(dict(id=item, paid=price))
            if item.startswith("key"):
                keys.append(int(item[3:]))
            else:
                owned[item] = owned.get(item, 0) + 1
        pending = deferred
        weapon = route_weapon(route, owned)
        distance = 7 if route == "近岸机动" else 18
        hp = (64 + 5 * 2.5) * (1 + (stage-1) * .19)
        normal_dps = sustained_dps(c, weapon, aim, owned.get("weapon", 0), keys, distance, False)
        fish_combat = hp / max(1., normal_dps)
        # 18s per fish covers pickup/recast/local selling movement; it is an
        # assumption, not a recorded route time. No dialogue or exploration.
        fish_cycle = reel_seconds(owned.get("rod", 0)) + fish_combat + 18
        before_boss = wallet
        bounty = (140 + (stage - 1) * 30) * 1.15 * (1 + .15 * aim.weak_given_hit)
        handin = 45 + stage * 8
        wallet += bounty + handin
        earned += bounty + handin
        assert abs(25 + earned - spent - wallet) < .00001
        result.append(dict(stage=stage, profile=aim.name, route=route,
                           research_completed=research, research_stipend=research_income,
                           starting_wallet=round(start_wallet, 2), planned_fish=fish_count,
                           optional_extra_fish=optional_fish, extra_fishing_minutes=round(optional_fish*fish_cycle/60, 2),
                           mission_stipend=stipend, supply_spend=supplies, kits_bought=kits,
                           purchases=purchased, deferred=deferred, before_boss_wallet=round(before_boss, 2),
                           ending_wallet=round(wallet, 2), total_earned=round(earned, 2), total_spent=round(spent, 2),
                           weapon_level=owned.get("weapon", 0), hull_level=owned.get("hull", 0),
                           keys=list(keys), weapon=weapon, pre_defence_boss_dps=round(sustained_dps(c, weapon, aim, owned.get("weapon", 0), keys, distance), 2),
                           normal_health_seconds=round(fish_combat, 2),
                           elite_health_seconds_before_mechanic=round(hp*2.15/max(1.,normal_dps*.8), 2)))
    return result


def audit(self_test: bool) -> dict:
    c = read_contract()
    rows = [r for aim in AIMS for route in PLANS for r in economy(c, aim, route)]
    research_rows = [r for aim in AIMS for route in PLANS for r in economy(c, aim, route, True)]
    # Current stock+mandatory rewards; multiplicative loot bonuses are listed
    # separately to avoid calling the baseline an exact wallet prediction.
    mandatory = [dict(stage=s, before_boss=70+s*15, base_boss=140+(s-1)*30,
                      after_handover=45+s*8) for s in range(1, 10)]
    weapons = []
    for i, w in enumerate(c["weapons"]):
        t = (c["burst_inner"]*2+c["burst_recovery"])/3 if i == 4 else w["interval"]
        weapons.append(dict(**w, sustained_all_hit_base_dps=round(w["damage"]*w["pellets"]*w["magazine"]/(w["magazine"]*t+w["reload"]), 2)))
    precision = []
    for a in AIMS:
        p = a.hit*a.weak_given_hit
        rate = p**4*(1-p)/(1-p**4)
        precision.append(dict(profile=a.name, weak_hit_probability=p,
                              v04_shots_per_precision_proc=round(1/rate, 1),
                              v04_expected_damage_gain_percent=round(.65*rate*100, 3),
                              v05_shots_per_precision_proc=round(1/rhythm_rate(a), 1),
                              v05_expected_damage_gain_percent=round(.85*rhythm_rate(a)*100, 3)))
    tuning = []
    tuning_path = CODE/"Core/EncounterTuning.cs"
    if tuning_path.exists():
        tune_source = tuning_path.read_text(encoding="utf-8-sig")
        arrays = {}
        for field in ("ExposureSeconds", "ExposureBonus", "MistakeBase", "MistakePerPhase", "PressureSeconds"):
            match = re.search(field+r' = \{([^}]+)\}', tune_source)
            assert match, "Encounter tuning format changed: "+field
            arrays[field] = numbers(match[1])
            assert len(arrays[field]) == 11
        for i in range(11):
            # Hull is the paid precision plan, not arbitrary free QA equipment.
            route_row = next(r for r in rows if r["route"]=="精准猎手" and r["profile"]=="普通" and r["stage"]==min(9,i+1))
            hp = 100 + route_row["hull_level"]*25
            mistakes = [arrays["MistakeBase"][i]+p*arrays["MistakePerPhase"][i] for p in (1,2,3)]
            tuning.append(dict(boss_id=108+i, purchased_hull_health=hp,
                               exposure_seconds=[arrays["ExposureSeconds"][i]+(p-1)*.5 for p in (1,2,3)],
                               exposure_multiplier=arrays["ExposureBonus"][i], failure_damage=mistakes,
                               pressure_seconds=[arrays["PressureSeconds"][i]-(p-1)*.6 for p in (1,2,3)],
                               full_health_survivable_phase3_mistakes=math.ceil(hp/mistakes[2])-1,
                               note="Arithmetic upper bound before other enemy attacks; no armour, regeneration or medical item counted. Timing and survival need live tests."))
    telemetry = []
    for path in [ROOT/"Documentation/TestResults/v04-full-combat.csv", ROOT/"Documentation/TestResults/v04-final-legendary-combat.csv"]:
        if path.exists():
            telemetry.append(dict(file=str(path.relative_to(ROOT)), rows=list(csv.DictReader(path.open(encoding="utf-8-sig")))))
    findings = [
        "All prices and weapon inputs are read from C#; aim/uptime/travel assumptions are design sensitivity inputs, not player observations.",
        "Wallet comparison purchases real staged offers with the actual escalating prices; medkits are budgeted, not granted for free.",
        "No random relic, secret reward, perfect-dodge counterattack or repeat-boss farming is credited to a planned build.",
        "Health-output seconds exclude boss objective travel, guards, timed attack opportunities, invulnerability, interruption and player death. They cannot certify a full encounter clear.",
        "v0.5 precision rhythm gives partial progress for a body hit and costs one point on a miss; the old consecutive-weakpoint dead choice is removed.",
        "Base fixed nine-island income already supports a focused build. Income should not be globally cut before measuring optional fishing and supply consumption.",
        "v0.4 deterministic bot grants gear/medkits. Its 4-kit whale clear is historical regression evidence, not a calibrated human difficulty target.",
    ]
    checks = []
    if self_test:
        all_rows = rows + research_rows
        assert len(all_rows) == 162
        assert all(r["ending_wallet"] >= 0 and r["before_boss_wallet"] >= 0 for r in all_rows)
        assert all(r["weapon_level"] <= min(5, 1+(r["stage"]-1)//2) for r in all_rows)
        assert all(all(k//3 <= (r["stage"]-2)//3 for k in r["keys"]) for r in all_rows)
        for r in all_rows:
            for item in r["purchases"]:
                if not item["id"].startswith("key"):
                    assert c["offers"][item["id"]]["island"] <= r["stage"]
        for w in range(6):
            values=[sustained_dps(c,w,a,1) for a in AIMS]
            assert values == sorted(values)
            assert sustained_dps(c,w,AIMS[1],5) > sustained_dps(c,w,AIMS[1],0)
        assert reel_seconds(0) > reel_seconds(1) > reel_seconds(2) > reel_seconds(3)
        assert all(r["research_stipend"] == c["research_reward"] for r in research_rows)
        checks = ["162 route/profile/research/island cases", "wallet conservation and no negative gold",
                  "staged weapon and keystone licenses", "all offer unlocks respected",
                  "aim and upgrade monotonicity for all six weapons", "rod upgrades shorten reel policy"]
    return dict(model="v0.5 design sensitivity model", sources=c["sources"],
                profiles=[asdict(a) for a in AIMS], weapons=weapons,
                mandatory_base_income=mandatory,
                mandatory_base_income_total=sum(sum(r[k] for k in ("before_boss","base_boss","after_handover")) for r in mandatory),
                reel_reference_seconds=[round(reel_seconds(i),2) for i in range(4)],
                precision_keystone_sensitivity=precision, paid_build_routes=rows,
                optional_research_routes=research_rows,
                mechanic_failure_contract=tuning,
                historical_v04_bot_telemetry=telemetry, findings=findings, self_test_checks=checks)


if __name__ == "__main__":
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--self-test", action="store_true")
    args=parser.parse_args()
    report=audit(args.self_test)
    dest=ROOT/"Artifacts/v05-balance-audit.json"
    dest.parent.mkdir(parents=True,exist_ok=True)
    dest.write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding="utf-8")
    csv_path=ROOT/"Artifacts/v05-economy-routes.csv"
    table=report["paid_build_routes"]+report["optional_research_routes"]
    with csv_path.open("w",encoding="utf-8-sig",newline="") as f:
        writer=csv.DictWriter(f,fieldnames=table[0].keys());writer.writeheader()
        for row in table:
            writer.writerow({k:json.dumps(v,ensure_ascii=False) if isinstance(v,(list,dict)) else v for k,v in row.items()})
    print(json.dumps(dict(report=str(dest),routes=str(csv_path),checks=report["self_test_checks"],
                          precision=report["precision_keystone_sensitivity"],
                          endgame=[r for r in table if r["stage"]==9 and not r["research_completed"]]),ensure_ascii=False,indent=2))
