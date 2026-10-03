"""Prepare redistributable font subset and official TMP essentials before editor import."""
from pathlib import Path
import tarfile
from fontTools import subset

ROOT = Path(__file__).resolve().parents[1]
PROJECT = ROOT / "Tidebreak"
sources = "".join(p.read_text(encoding="utf-8-sig") for p in (PROJECT / "Assets/Scripts").rglob("*.cs"))
characters = "".join(sorted(set(sources + "".join(chr(i) for i in range(32, 127)) + "×→◆◇…–—。") - set("\r\n\t")))
font_source = ROOT / "ThirdParty/NotoSansSC-Regular.otf"
if font_source.exists():
    options = subset.Options()
    font = subset.load_font(str(font_source), options)
    sub = subset.Subsetter(options=options)
    sub.populate(text=characters)
    sub.subset(font)
    destination = PROJECT / "Assets/Resources/Fonts/SeaText-Regular.otf"
    destination.parent.mkdir(parents=True, exist_ok=True)
    subset.save_font(font, str(destination), options)
(PROJECT / "Assets/Art/Fonts/characters.txt").write_text(characters, encoding="utf-8")

# TMP 3.0.6 predates the non-interactive TMP_PackageResourceImporter API.
# Extract Unity's own bundled essentials with their original .meta files.
package = PROJECT / "Library/PackageCache/com.unity.textmeshpro@3.0.6/Package Resources/TMP Essential Resources.unitypackage"
if package.exists():
    with tarfile.open(package) as archive:
        members = {m.name: m for m in archive.getmembers()}
        for name, member in members.items():
            if not name.endswith("/pathname"):
                continue
            pathname = archive.extractfile(member).read().decode("utf-8").strip()
            if not pathname.startswith("Assets/TextMesh Pro/") and pathname != "Assets/TextMesh Pro":
                continue
            target = (PROJECT / pathname).resolve()
            if not target.is_relative_to(PROJECT.resolve()):
                raise ValueError("Resource path outside project")
            prefix = name.rsplit("/", 1)[0]
            asset = members.get(prefix + "/asset")
            meta = members.get(prefix + "/asset.meta")
            if asset is not None:
                target.parent.mkdir(parents=True, exist_ok=True)
                if not target.exists():
                    target.write_bytes(archive.extractfile(asset).read())
            else:
                target.mkdir(parents=True, exist_ok=True)
            if meta is not None and not Path(str(target) + ".meta").exists():
                Path(str(target) + ".meta").write_bytes(archive.extractfile(meta).read())
print("Prepared", len(characters), "font characters and TMP essentials")
