---
name: logo-assets
description: Regenerate or restore the Rosella Rhythm footer logo PNGs (frontend/public/rosella-light.png and rosella-dark.png) from a base64 data-URI source. Use when the logo images are missing, need recreating, or when turning a new base64 logo export into the committed light/dark assets.
---

# Rosella Rhythm logo assets

The app footer (`frontend/src/App.tsx`) shows a brand logo that swaps by theme —
`rosella-light.png` in light mode, `rosella-dark.png` in dark mode. Both live in
`frontend/public/` and render at 56×56 with `object-fit: contain` (see `.app-logo`
in `frontend/src/styles/app.css`), so a non-square source is fine.

## Source / testing data

`rosella-draft.b64.txt` (in this folder) holds **draft** base64 renditions of the
logo: two `data:image/png;base64,…` data URIs, one per line. These are small
working drafts (~29 KB / ~27 KB decoded) — **not** the higher-res PNGs currently
shipped in `frontend/public/` (those are ~270 KB / ~230 KB). Keep them only as a
worked example of the regenerate-from-data-URI flow below.

## Regenerate PNGs from a base64 data-URI file

Given a file with one `data:image/<type>;base64,…` per line (line 1 → light,
line 2 → dark — confirm by opening the results, since the order isn't encoded):

PowerShell (from the repo root):
```powershell
$lines = Get-Content .claude/skills/logo-assets/rosella-draft.b64.txt
function Save-DataUri($s, $out) {
  $b64 = $s -replace '^data:image/\w+;base64,', ''
  [IO.File]::WriteAllBytes((Resolve-Path .).Path + "\$out", [Convert]::FromBase64String($b64))
}
Save-DataUri $lines[0] 'frontend\public\rosella-light.png'
Save-DataUri $lines[1] 'frontend\public\rosella-dark.png'
```

Bash (from the repo root):
```bash
sed -n '1p' .claude/skills/logo-assets/rosella-draft.b64.txt | sed 's#^data:image/[a-z]*;base64,##' | base64 -d > frontend/public/rosella-light.png
sed -n '2p' .claude/skills/logo-assets/rosella-draft.b64.txt | sed 's#^data:image/[a-z]*;base64,##' | base64 -d > frontend/public/rosella-dark.png
```

After regenerating, verify the footer logo in the running app and replace with a
full-res export if you want the shipped quality rather than these drafts.
