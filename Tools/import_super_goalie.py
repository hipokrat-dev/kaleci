#!/usr/bin/env python3
"""Import selected art from the user's own, locally downloaded Super Goalie package.
Never imports the package's scripts, ProjectSettings, manifest, sounds or demo scene.
Run from any directory: python3 Tools/import_super_goalie.py /path/to/package.unitypackage
Then use Assets > Refresh in the open Unity project.
"""
import argparse
from pathlib import Path
import tarfile

PREFIXES = (
    "Assets/SuperGoalie/Animations/",
    "Assets/SuperGoalie/Meshes/Characters/",
    "Assets/SuperGoalie/Meshes/Ball/",
    "Assets/SuperGoalie/Textures/Entities/GoalKeeper/",
)


def import_art(package: Path, root: Path) -> int:
    writes = []
    with tarfile.open(package) as archive:
        members = {m.name: m for m in archive.getmembers()}
        for entry in archive.getmembers():
            if not entry.isfile() or not entry.name.endswith("/pathname"):
                continue
            lines = archive.extractfile(entry).read().decode("utf-8").splitlines()
            if not lines:
                continue
            path = lines[0].strip("\x00")
            if not path.startswith(PREFIXES) or ".." in Path(path).parts:
                continue
            key = entry.name.rsplit("/", 1)[0]
            if key + "/asset" not in members:
                continue
            for suffix, relative in (("/asset", path), ("/asset.meta", path + ".meta")):
                member = members.get(key + suffix)
                if member is None or not member.isfile():
                    continue
                target = root / relative
                # Unity upgrades .meta files; preserve them when the art already exists.
                if target.exists():
                    if suffix == "/asset" and target.read_bytes() != archive.extractfile(member).read():
                        raise RuntimeError(f"Existing different asset preserved; resolve manually: {relative}")
                    continue
                writes.append((target, archive.extractfile(member).read()))
        if not any(m.name.endswith("/pathname") for m in archive.getmembers()):
            raise RuntimeError("Not a Unity asset package")
    for target, content in writes:
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(content)
    model = root / "Assets/SuperGoalie/Meshes/Characters/Players/Player001/Character.fbx"
    if not model.exists():
        raise RuntimeError("Expected Super Goalie Character.fbx not found; no integration requested")
    (root / "Temp").mkdir(exist_ok=True)
    (root / "Temp/goalie-integration.request").touch()
    return len(writes)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("package", type=Path)
    args = parser.parse_args()
    project = Path(__file__).resolve().parents[1]
    count = import_art(args.package.expanduser(), project)
    print(f"{count} files added. Existing files preserved. Open Unity and choose Assets > Refresh.")
