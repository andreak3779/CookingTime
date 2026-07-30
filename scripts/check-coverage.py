#!/usr/bin/env python3
"""Check line coverage of the CookingTime production code.

Reads one or more Cobertura XML files (typically
`tests/CookingTime.UnitTests/TestResults/*/coverage.cobertura.xml`)
and prints the production-code line coverage. Exits non-zero if any
project's coverage falls below the threshold.

Phase 7: only UnitTests is gated. ComponentTests and IntegrationTests
publish coverage as informational artifacts (their numbers reflect the
Razor UI surface, which UnitTests doesn't exercise by design).
"""

from __future__ import annotations

import argparse
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

# Folder prefixes that count as "production code". Anything else (tests,
# OriginalSource, generated) is excluded. Mirrors the <Include> filters in
# coverlet.runsettings.xml — coverlet emits relative paths like
# "Domain/Meals/ChickenMeal.cs" so we match on folder prefixes, not
# full namespaces.
PRODUCTION_FOLDERS = (
    "Domain/",
    "Application/",
    "Infrastructure/",
    "Pages/",
    "Layout/",
)


def project_name_from_path(path: Path) -> str:
    """Best-effort project name from a path like
    `tests/CookingTime.UnitTests/TestResults/...`."""
    parts = path.parts
    for i, part in enumerate(parts):
        if part == "TestResults" and i > 0:
            return parts[i - 1]
    return path.stem


def coverage_from_cobertura(xml_path: Path) -> tuple[int, int]:
    """Return (lines_covered, lines_valid) summed over all production classes."""
    tree = ET.parse(xml_path)
    root = tree.getroot()

    covered = 0
    total = 0
    for cls in root.iter("class"):
        filename = cls.get("filename", "")
        if not any(filename.startswith(folder) for folder in PRODUCTION_FOLDERS):
            continue
        for line in cls.iter("line"):
            hits = line.get("hits", "0")
            if hits != "0":
                covered += 1
            total += 1
    return covered, total


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "reports",
        nargs="+",
        type=Path,
        help="Cobertura XML files to check (one per test project).",
    )
    parser.add_argument(
        "--threshold",
        type=float,
        default=75.0,
        help="Minimum line coverage percentage (default: 75).",
    )
    parser.add_argument(
        "--gated-projects",
        nargs="+",
        default=("CookingTime.UnitTests",),
        help="Project names whose coverage must meet the threshold "
             "(default: CookingTime.UnitTests). Other projects are "
             "reported but not gated.",
    )
    args = parser.parse_args()

    print(f"Coverage threshold: {args.threshold:.2f}%")
    print(f"Gated projects: {', '.join(args.gated_projects)}")
    print()

    failures: list[str] = []
    for report in args.reports:
        if not report.exists():
            print(f"  [skip] {report} (not found)")
            continue
        project = project_name_from_path(report)
        covered, total = coverage_from_cobertura(report)
        if total == 0:
            rate = 0.0
        else:
            rate = covered / total * 100.0
        gated = project in args.gated_projects
        marker = "GATE " if gated else "info "
        status = "PASS" if (not gated or rate >= args.threshold) else "FAIL"
        print(f"  [{status}] {marker}{project:<30s} "
              f"{covered:>4d}/{total:<4d} lines  ({rate:6.2f}%)  "
              f"{report}")
        if gated and rate < args.threshold:
            failures.append(f"{project}: {rate:.2f}% < {args.threshold:.2f}%")

    print()
    if failures:
        print("FAIL: coverage gate(s) not met:")
        for f in failures:
            print(f"  - {f}")
        return 1
    print("PASS: all gated projects meet the coverage threshold.")
    return 0


if __name__ == "__main__":
    sys.exit(main())