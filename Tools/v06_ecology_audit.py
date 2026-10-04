"""Independent finite-pool/economy model for v0.6. Runtime QA remains authoritative.

No Unity launch, no save mutation. Values are the documented design inputs;
source fingerprints make stale reports visible rather than silently reusable.
"""
import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def native_lure(k):
    return 0 if k < 6 else 1 if k < 9 else 2 if k < 11 else 3


def pool(island, lure):
    local = [island * 12 + k for k in range(12) if native_lure(k) <= lure]
    migrants = [((island + 8) % 9) * 12 + 1 + m for m in range(3) if lure >= m]
    return local + migrants


def audit():
    checks = 0
    rows = []
    for island in range(9):
        for lure in range(4):
            ids = pool(island, lure)
            natives = sum(i // 12 == island for i in ids)
            assert len(ids) == len(set(ids))
            assert natives / len(ids) > .70
            assert all(i // 12 == island for i in ids if i % 12 in (0, 5))
            assert island * 12 in ids and island * 12 + 5 in ids
            checks += 4
            for habitat in range(3):
                assert sum(i // 12 == island and i % 3 == habitat for i in ids) >= 2
                checks += 1
            # All other island pools, not only the preceding island.
            overlap = max(len(set(ids) & set(pool(other, lure))) / len(ids)
                          for other in range(9) if other != island)
            assert overlap < .30
            checks += 1
            rows.append(dict(island=island+1, lure=lure, ids=ids,
                             native_fraction=natives/len(ids), max_overlap=overlap))
    economy = []
    for island in range(9):
        # Conservative local base sale value: no gold/giant, elite, airshot,
        # weakshot, fortune or route premium. No quest/stipend/bounty income.
        mean_catch = 25 + island * 7 + 5
        mastery = 55 + island * 8
        for catches in (2, 4, 6):
            income = mean_catch * catches
            # Cheapest appropriate second meaningful purchase is a 40g line
            # bearing; repeats accrue 40g per rank, so this is a first-rank model.
            assert income >= mastery  # two local fish can fund rank I everywhere
            if catches >= 4:
                assert income >= mastery + 40
            checks += 2 if catches >= 4 else 1
            economy.append(dict(island=island+1, catches=catches,
                                base_income=income, mastery_rank_one=mastery,
                                mastery_and_bearing=mastery+40,
                                surplus_after_mastery=income-mastery))
        # All three research ranks are finite and always cost gold.
        prices = [mastery + rank * 40 for rank in range(3)]
        assert prices[0] > 0 and prices == sorted(set(prices))
        checks += 1
    sources = {}
    for relative in ("Tidebreak/Assets/Scripts/Core/ExpeditionContent.cs",
                     "Tidebreak/Assets/Scripts/Core/CoastalResearch.cs",
                     "Tidebreak/Assets/Scripts/Core/IslandMastery.cs",
                     "Tidebreak/Assets/Scripts/Core/ShopCatalog.cs"):
        sources[relative] = hashlib.sha256((ROOT/relative).read_bytes()).hexdigest()
    return dict(model="v0.6 finite ecology and conservative first-rank economy",
                checks=checks, passed=True, limitations=[
                    "Independent design model; the runtime pool must also be tested in Unity.",
                    "Income assumes winning, collecting and selling catches, excludes healing costs.",
                    "Affordable choices are not evidence of human-perceived fun or encounter difficulty."],
                source_sha256=sources, ecology=rows, economy=economy)


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", default="Builds/Artifacts/v06-ecology-audit.json")
    args = parser.parse_args()
    result = audit()
    target = ROOT / args.output
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"PASS {result['checks']} checks; 36 pools; 27 economy cases; {target}")
