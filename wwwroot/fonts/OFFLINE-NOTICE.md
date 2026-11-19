# Self-hosted fonts — provenance and licence

Phase 12 replaced the theme's Google Fonts `@import` with locally served
`woff2` files so the app renders its intended typography with **no external
network dependency** (the app ships as a container and may be used offline).

Both families are licensed under the **SIL Open Font License 1.1**, which
permits redistribution and self-hosting.

## Files

| File | Family | Weights | Axes | Size | Upstream |
| --- | --- | --- | --- | --- | --- |
| `figtree-latin.woff2` | Figtree | 400–700 | `wght` 300–900 | 20 KB | `https://fonts.gstatic.com/s/figtree/v9/_Xms-HUzqDCFdgfMm4S9DQ.woff2` |
| `figtree-latin-ext.woff2` | Figtree | 400–700 | `wght` 300–900 | 10 KB | `https://fonts.gstatic.com/s/figtree/v9/_Xms-HUzqDCFdgfMm4q9DbZs.woff2` |
| `bricolage-latin.woff2` | Bricolage Grotesque | 500–800 | `wght` 200–800, `opsz` 12–96 | 77 KB | `https://fonts.gstatic.com/s/bricolagegrotesque/v9/3y9K6as8bTXq_nANBjzKo3IeZx8z6up5BeSl9D4dj_x9PpZBMlGIInE.woff2` |
| `bricolage-latin-ext.woff2` | Bricolage Grotesque | 500–800 | `wght` 200–800, `opsz` 12–96 | 31 KB | `https://fonts.gstatic.com/s/bricolagegrotesque/v9/3y9K6as8bTXq_nANBjzKo3IeZx8z6up5BeSl9D4dj_x9PpZBMlGGInHEVA.woff2` |

**Total: ~138 KB.**

## These are variable fonts

Each file is a *variable* font — one file covers the whole weight range, and
the browser interpolates the real outlines. This is why the `@font-face` rules
in `wwwroot/css/cookingtime.css` use the **range** syntax:

```css
font-weight: 400 700;   /* Figtree            */
font-weight: 500 800;   /* Bricolage Grotesque */
```

Declaring discrete weights per file would make the browser pick one static
instance and **synthesise** the rest, which looks noticeably worse than true
interpolation.

`font-stretch: 100%` is declared for Bricolage Grotesque because the family
has a width axis upstream.

## Why two files per family

`latin` and `latin-ext` are separate subsets. The `unicode-range` descriptors
in `cookingtime.css` are copied verbatim from the upstream Google Fonts CSS so
the browser only downloads `latin-ext` when a character outside the Latin
range is actually present.

## Regenerating these files

```bash
# Requires a browser User-Agent so Google serves woff2 rather than ttf.
UA="Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
curl -s -A "$UA" "https://fonts.gstatic.com/s/figtree/v9/_Xms-HUzqDCFdgfMm4S9DQ.woff2" -o wwwroot/fonts/figtree-latin.woff2
curl -s -A "$UA" "https://fonts.gstatic.com/s/figtree/v9/_Xms-HUzqDCFdgfMm4q9DbZs.woff2" -o wwwroot/fonts/figtree-latin-ext.woff2
curl -s -A "$UA" "https://fonts.gstatic.com/s/bricolagegrotesque/v9/3y9K6as8bTXq_nANBjzKo3IeZx8z6up5BeSl9D4dj_x9PpZBMlGIInE.woff2" -o wwwroot/fonts/bricolage-latin.woff2
curl -s -A "$UA" "https://fonts.gstatic.com/s/bricolagegrotesque/v9/3y9K6as8bTXq_nANBjzKo3IeZx8z6up5BeSl9D4dj_x9PpZBMlGGInHEVA.woff2" -o wwwroot/fonts/bricolage-latin-ext.woff2
```

To confirm the download is a valid variable font:

```bash
python3 -c "from fontTools.ttLib import TTFont; f=TTFont('wwwroot/fonts/figtree-latin.woff2'); print('fvar' in f, [(a.axisTag,a.minValue,a.maxValue) for a in f['fvar'].axes])"
```

## Upstream sources

- Figtree — <https://github.com/google/fonts/tree/main/ofl/figtree>
- Bricolage Grotesque — <https://github.com/google/fonts/tree/main/ofl/bricolagegrotesque>

## SIL Open Font License 1.1

Both families are licensed under the SIL Open Font License, Version 1.1.
The canonical text is available at <https://scripts.sil.org/OFL>, and the
per-family licence files are `OFL.txt` in the upstream repositories above.

The short form of the licence: the fonts may be used, studied, modified and
redistributed freely as long as they are not sold on their own, and any
derivative keeps the licence. The fonts are provided "as is", without warranty.
