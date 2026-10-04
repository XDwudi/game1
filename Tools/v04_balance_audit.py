"""Read v0.4 runtime exports and source values; do not simulate a human playtest.

Run after a fresh Unity build has exported Artifacts/species-catalog.csv.
Runtime battle telemetry (RevisionContracts) is the authoritative integration result.
"""
from pathlib import Path
import csv
import json
import re
import statistics
import sys

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

ROOT = Path(__file__).resolve().parents[1]
CODE = ROOT / "Tidebreak/Assets/Scripts"
data = (CODE / "Core/GameData.cs").read_text(encoding="utf-8-sig")
content = (CODE / "Core/ExpeditionContent.cs").read_text(encoding="utf-8-sig")
encounter = (CODE / "Combat/EncounterDirector.cs").read_text(encoding="utf-8-sig")
rows = list(csv.DictReader((ROOT / "Artifacts/species-catalog.csv").open(encoding="utf-8-sig")))
assert len(rows) == 119 and sum(r["boss"] == "false" for r in rows) == 108

specs = re.findall(
    r'new WeaponSpec\("([^"]+)",\s*([\d.]+),\s*([\d.]+)f,\s*([\d.]+)f,\s*(\d+),\s*(\d+),', data
)
assert len(specs) == 6
health_match = re.search(r"boss=true,hp=i<9\?([\d.]+)\+i\*([\d.]+):i==9\?([\d.]+):([\d.]+)", content)
assert health_match, "Update the audit parser after changing the boss catalog formula."
base_hp, hp_step, kraken_hp, whale_hp = map(float, health_match.groups())
expected_health = [base_hp + i * hp_step for i in range(9)] + [kraken_hp, whale_hp]
assert [float(r["hp"]) for r in rows[108:]] == expected_health, "Runtime export is stale; build the current project first."

damage_match = re.search(
    r"DamageMultiplier.*?1\s*\+\s*([.\d]+)f\s*\*\s*Mathf.Min\((\d+),weaponLevel\)\s*\+\s*([.\d]+)f\s*\*\s*Mathf.Min\((\d+),damageRelics\)", data
)
assert damage_match
level_gain, level_cap, relic_gain, relic_cap = map(float, damage_match.groups())
maximum_flat_multiplier = 1 + level_gain * level_cap + relic_gain * relic_cap
assert 1.6 <= maximum_flat_multiplier <= 2.2

weapons = []
for index, (name, damage, interval, reload, magazine, pellets) in enumerate(specs):
    damage, interval, reload = map(float, (damage, interval, reload))
    magazine, pellets = int(magazine), int(pellets)
    # Three-round burst: two 0.11s intervals and 0.3s recovery. This sustained
    # model intentionally rounds away frame-timing and first-shot transients.
    effective_interval = (.11 * 2 + .3) / 3 if index == 4 else interval
    dps = damage * pellets * magazine / (effective_interval * magazine + reload)
    assert 20 <= dps <= 65, (name, dps)
    weapons.append({
        "weapon": name, "close_range_all_hit_sustained_dps": round(dps, 2),
        "magazine_damage": damage * pellets * magazine,
        "reload_seconds": reload, "rank4_multiplier": round(1 + 4 * level_gain, 2),
    })

def expected_gear(index):
    stage = index + 1
    return 1 + min(int(level_cap), (stage + 1) // 2) * level_gain + min(2, stage // 4) * relic_gain

bosses = []
for i, row in enumerate(rows[108:]):
    # A range, not an exact predicted clear: aim/travel, gates, healing and
    # mechanics do not collapse honestly to one deterministic TTK figure.
    gun = weapons[2 if i >= 3 else 0]
    raw_dps = gun["close_range_all_hit_sustained_dps"] * expected_gear(i)
    estimates = {}
    for label, exposed_share, weak_share, hit_rate in [
        ("learning_body_shots", .25, .15, .65),
        ("accurate_window_play", .45, .6, .85),
    ]:
        defense = .48 * (1 - exposed_share) + 1.35 * exposed_share
        weak = 1 + .4 * weak_share
        damage = raw_dps * defense * weak * hit_rate * 1.06
        estimates[label] = round(float(row["hp"]) / damage, 1)
    bosses.append({"id": 108 + i, "name": row["name"], "hp": float(row["hp"]),
                   "main_health_output_seconds_before_objective_time": estimates})

telemetry_path = ROOT / "Builds/Artifacts/ExpeditionQA/revision-combat.csv"
telemetry = []
if telemetry_path.exists():
    telemetry = list(csv.DictReader(telemetry_path.open(encoding="utf-8-sig")))
actual_bosses = [r for r in telemetry if r["kind"] == "boss"]
report = {
    "version": "0.4.0", "generic_damage_cap": round(maximum_flat_multiplier, 3),
    "weapons": weapons, "boss_output_ranges": bosses,
    "live_telemetry_available": bool(telemetry), "live_bosses_tested": len(actual_bosses),
    "live_boss_clear_count": sum(r["outcome"] == "cleared" for r in actual_bosses),
    "live_median_seconds": statistics.median(float(r["seconds"]) for r in actual_bosses) if actual_bosses else None,
    "limits": "Analytical output ranges only. No claim of fun or human skill calibration. Shotgun assumes all pellets land within 10m, arc excludes secondary targets. Boss estimates exclude keystone build effects and assume 48% guard / 135% recovery, 1.4 weak points and stated exposure shares; objective travel, gate HP, interruptions and healing add time. Live telemetry uses a deterministic auto-aim movement bot with legal ammo, damage, dashes and up to four finite medical items, without invulnerability. Live weapon samples shoot breakable objective targets: harpoon samples include its 1.5x objective bonus and must not be labeled ordinary-enemy DPS.",
}
destination = ROOT / "Artifacts/v04-balance-audit.json"
destination.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
print(json.dumps(report, ensure_ascii=False, indent=2))
