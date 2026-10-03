#!/usr/bin/env python3
"""Create missing Unity .meta files (stable random GUIDs) and delete orphaned ones.

Unity needs a committed .meta next to every file and folder it imports: the sim package
(src/Deadswitch.Sim, src/Deadswitch.Host) and unity/Assets. Agents without the Unity Editor run this after adding
or removing files so GUIDs are created once and committed, never regenerated on each machine.

Usage: python3 tools/gen_meta.py [--check]   (--check: exit 1 if anything would change)
"""
import os
import sys
import uuid

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TREES = [os.path.join(ROOT, "src", "Deadswitch.Sim"), os.path.join(ROOT, "src", "Deadswitch.Host"), os.path.join(ROOT, "src", "Deadswitch.Art"),
         os.path.join(ROOT, "unity", "Assets")]
SKIP_DIRS = {"bin", "obj", ".vs"}
SKIP_SUFFIX = (".meta", ".csproj.user")

IMPORTER_BLOCK = """{name}:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""

TEXT_IMPORTER = {
    ".json": "TextScriptImporter",
    ".txt": "TextScriptImporter",
    ".bytes": "TextScriptImporter",
    ".asmdef": "AssemblyDefinitionImporter",
    ".uxml": None,  # ScriptedImporter, Unity fills it in; minimal meta is enough
    ".uss": None,
    ".tss": None,
    ".shader": "ShaderImporter",
}


def meta_for(path):
    guid = uuid.uuid4().hex
    head = "fileFormatVersion: 2\nguid: {}\n".format(guid)
    if os.path.isdir(path):
        return head + "folderAsset: yes\n" + IMPORTER_BLOCK.format(name="DefaultImporter")
    ext = os.path.splitext(path)[1].lower()
    if ext == ".cs":
        return head
    if os.path.basename(path) == "package.json":
        return head + IMPORTER_BLOCK.format(name="PackageManifestImporter")
    if ext in TEXT_IMPORTER:
        name = TEXT_IMPORTER[ext]
        return head + (IMPORTER_BLOCK.format(name=name) if name else "")
    return head + IMPORTER_BLOCK.format(name="DefaultImporter")


def main():
    check = "--check" in sys.argv
    changes = []
    for tree in TREES:
        if not os.path.isdir(tree):
            continue
        for dirpath, dirnames, filenames in os.walk(tree):
            dirnames[:] = sorted(d for d in dirnames if d not in SKIP_DIRS and not d.startswith("."))
            entries = [os.path.join(dirpath, d) for d in dirnames]
            entries += [os.path.join(dirpath, f) for f in sorted(filenames)
                        if not f.endswith(SKIP_SUFFIX) and not f.startswith(".")]
            for entry in entries:
                meta = entry + ".meta"
                if not os.path.exists(meta):
                    changes.append("create " + os.path.relpath(meta, ROOT))
                    if not check:
                        with open(meta, "w", newline="\n") as fh:
                            fh.write(meta_for(entry))
            for f in filenames:
                if f.endswith(".meta"):
                    target = os.path.join(dirpath, f[:-5])
                    if not os.path.exists(target):
                        changes.append("delete " + os.path.relpath(os.path.join(dirpath, f), ROOT))
                        if not check:
                            os.remove(os.path.join(dirpath, f))
    for c in changes:
        print(c)
    if check and changes:
        print("Unity .meta files are out of date. Run: python3 tools/gen_meta.py", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
