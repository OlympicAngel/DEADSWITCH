#!/usr/bin/env python3
"""USS lint: fails on selectors and properties Unity's UI Toolkit rejects or ignores.

Browsers (tools/uipreview) accept full CSS, so these only break in the Editor: an invalid selector such as
`.a + .b` drops the WHOLE stylesheet at import, and `:last-child` or unknown properties are silently ignored.
Use the `is-first` / `is-last` classes that Kit.MarkEnds adds instead of :first-child / :last-child.
"""
import glob
import os
import re
import sys

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
UI = os.path.join(ROOT, "unity", "Assets", "Game", "Resources", "UI")

PROPS = set("""
align-content align-items align-self all aspect-ratio background-color background-image background-position
background-position-x background-position-y background-repeat background-size border-bottom-color
border-bottom-left-radius border-bottom-right-radius border-bottom-width border-color border-left-color
border-left-width border-radius border-right-color border-right-width border-top-color border-top-left-radius
border-top-right-radius border-top-width border-width bottom color cursor display filter flex flex-basis
flex-direction flex-grow flex-shrink flex-wrap font-size height justify-content left letter-spacing margin
margin-bottom margin-left margin-right margin-top max-height max-width min-height min-width opacity overflow
padding padding-bottom padding-left padding-right padding-top position right rotate scale text-overflow
text-shadow top transform-origin transition transition-delay transition-duration transition-property
transition-timing-function translate visibility white-space width word-spacing
-unity-background-image-tint-color -unity-background-scale-mode -unity-editor-text-rendering-mode -unity-font
-unity-font-definition -unity-font-style -unity-material -unity-overflow-clip-box -unity-paragraph-spacing
-unity-slice-bottom -unity-slice-left -unity-slice-right -unity-slice-scale -unity-slice-top -unity-slice-type
-unity-text-align -unity-text-generator -unity-text-outline -unity-text-outline-color -unity-text-outline-width
-unity-text-overflow-position
""".split())
PSEUDO = {"hover", "active", "inactive", "focus", "disabled", "enabled", "checked", "selected", "root"}
# SPEC-046: the UI scales from a 1080 px reference (3 px per dp); essential text never goes below 11 dp
FONT_FLOOR_PX = 33


def lint(path):
    problems = []
    src = re.sub(r"/\*.*?\*/", lambda m: "\n" * m.group(0).count("\n"), open(path, encoding="utf-8").read(), flags=re.S)
    for m in re.finditer(r"([^{}]+)\{([^{}]*)\}", src):
        line = src[: m.start(1) + len(m.group(1)) - len(m.group(1).lstrip())].count("\n") + 1
        selectors = m.group(1).strip()
        if selectors.startswith("@"):
            continue
        for sel in selectors.split(","):
            sel = sel.strip()
            if re.search(r"[+~\[]", sel):
                problems.append((line, f"unsupported selector '{sel}' (no +, ~ or [attr] in USS; this drops the whole sheet)"))
            for pseudo in re.findall(r"::?([a-z-]+)", sel):
                if pseudo not in PSEUDO:
                    problems.append((line, f"unsupported pseudo-class ':{pseudo}' in '{sel}' (use is-first / is-last classes)"))
        for decl in m.group(2).split(";"):
            if ":" not in decl:
                continue
            prop, value = (p.strip() for p in decl.split(":", 1))
            if prop and not prop.startswith("--") and prop not in PROPS:
                problems.append((line, f"unknown USS property '{prop}'"))
            size = re.fullmatch(r"(\d+)px", value)
            if size and (prop == "font-size" or prop.startswith("--fs-")) and int(size.group(1)) < FONT_FLOOR_PX:
                problems.append((line, f"{prop} {value} is below the {FONT_FLOOR_PX} px type floor (use a --fs-* token)"))
    return problems


def main():
    files = sorted(glob.glob(os.path.join(UI, "*.uss")) + glob.glob(os.path.join(UI, "*.tss")))
    failed = 0
    for f in files:
        for line, msg in lint(f):
            print(f"{os.path.relpath(f, ROOT)}:{line}: {msg}")
            failed += 1
    if failed:
        print(f"uss lint: {failed} problem(s)")
        return 1
    print(f"uss lint: {len(files)} files clean")
    return 0


if __name__ == "__main__":
    sys.exit(main())
