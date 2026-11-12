#!/usr/bin/env python3
"""Restore a mobile subset from the user's locally licensed KCISA unitypackage.

Only art is imported. Original package, scripts and ProjectSettings are untouched.
The generated placement JSON and all package art remain excluded from public Git.
"""
import argparse
import json
from pathlib import Path
import re
import tarfile
import tempfile

STRUCTURE = set(range(9, 23)) | set(range(69, 81)) | set(range(108, 112)) | set(range(118, 123)) | {136, 139, 140, 141, 142, 184} | set(range(172, 177))
GUID = re.compile(r"guid\s*:\s*([0-9a-f]{32})")


def category(path):
    m = re.search(r"SM_(\d+)_", Path(path).name)
    return int(m[1]) if m else 0


def restore(package, root):
    with tempfile.TemporaryDirectory(prefix="kcisa-") as temp:
        cache = Path(temp)
        paths, sizes = {}, {}
        # Read small YAML assets in order; avoid repeated seeks through a 5.6 GB gzip.
        with tarfile.open(package, "r:gz") as archive:
            for member in archive:
                key, _, kind = member.name.rpartition("/")
                if not member.isfile() or not re.fullmatch(r"[0-9a-f]{32}", key):
                    continue
                if kind == "pathname":
                    path = archive.extractfile(member).read().decode().splitlines()[0].strip("\0")
                    if path.startswith("Assets/HandballStadium/") and ".." not in Path(path).parts:
                        paths[key] = path
                elif kind == "asset" and member.size < 24_000_000:
                    # Assets may precede their pathname; cache only readable Unity YAML.
                    data = archive.extractfile(member).read()
                    if data.startswith(b"%YAML"):
                        (cache / key).write_bytes(data)
                if kind == "asset":
                    sizes[key] = member.size
        reverse = {v: k for k, v in paths.items()}
        scene_guid = reverse.get("Assets/HandballStadium/Scenes/Demo_SKS.unity")
        if not scene_guid:
            raise RuntimeError("Expected KCISA Demo_SKS scene not found")
        scene = (cache / scene_guid).read_text()
        chosen, entries = set(), []
        for block in re.split(r"(?m)^--- !u!", scene):
            if not block.startswith("1001 "):
                continue
            source = re.search(r"m_SourcePrefab: \{fileID: \d+, guid: (\w+)", block)
            if not source or category(paths.get(source[1], "")) not in STRUCTURE:
                continue
            if "m_TransformParent: {fileID: 0}" not in block:
                raise RuntimeError("Package layout changed: nested placement requires explicit handling")
            prefab = (cache / source[1]).read_text()
            refs = GUID.findall(prefab)
            models = sorted({g for g in refs if paths.get(g, "").endswith(".fbx")})
            materials = [g for g in refs if paths.get(g, "").endswith(".mat")]
            if len(models) != 1 or not materials:
                raise RuntimeError("Unexpected prefab layout: " + paths[source[1]])
            values = dict(re.findall(r"propertyPath: (\S+)\s+value: ([^\n]+)", block))
            def vector(name, axes, default):
                return {a: float(values.get(name + "." + a, default if a != "w" else 1)) for a in axes}
            entries.append(dict(name=Path(paths[source[1]]).stem, category=category(paths[source[1]]),
                                model=paths[models[0]], materials=[paths[m] for m in materials],
                                position=vector("m_LocalPosition", "xyz", 0),
                                rotation=vector("m_LocalRotation", "xyzw", 0),
                                scale=vector("m_LocalScale", "xyz", 1)))
            chosen.update(models + materials)
        # Only albedo maps are needed for distant stadium structures.
        for guid in list(chosen):
            if paths[guid].endswith(".mat"):
                material = (cache / guid).read_text()
                albedo = re.search(r"_MainTex:\s+m_Texture: \{fileID: \d+, guid: (\w+)", material)
                if albedo and albedo[1] in paths:
                    chosen.add(albedo[1])
        if not entries:
            raise RuntimeError("No stadium placements selected")
        print(f"Selected {len(entries)} placements, {len(chosen)} source assets, {sum(sizes[g] for g in chosen)/1024**2:.1f} MiB", flush=True)
        pending = []
        with tarfile.open(package, "r:gz") as archive:
            for member in archive:
                key, _, kind = member.name.rpartition("/")
                if key not in chosen or kind not in ("asset", "asset.meta") or not member.isfile():
                    continue
                relative = paths[key] + (".meta" if kind == "asset.meta" else "")
                target = root / relative
                data = archive.extractfile(member).read()
                if target.exists():
                    # Unity upgrades serialized .mat files on import. Preserve them just
                    # like metadata; only immutable model/texture conflicts abort restoration.
                    if kind == "asset" and target.suffix != ".mat" and target.read_bytes() != data:
                        raise RuntimeError("Different existing asset preserved: " + relative)
                    continue
                staged = cache / (key + "." + kind)
                staged.write_bytes(data)
                pending.append((staged, target))
        # Commit only after all existing files have been checked.
        for staged, target in pending:
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(staged.read_bytes())
        output = root / "Assets/HandballStadium/PenaltyLayout.json"
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_text(json.dumps(dict(entries=entries), ensure_ascii=False))
        (root / "Temp").mkdir(exist_ok=True)
        (root / "Temp/stadium-integration.request").touch()
        print(f"Added {len(pending)} files; existing files preserved. Unity: Assets > Refresh.")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("package", type=Path)
    args = parser.parse_args()
    restore(args.package.expanduser(), Path(__file__).resolve().parents[1])
