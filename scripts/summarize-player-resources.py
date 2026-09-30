#!/usr/bin/env python3
"""Summarize an isolated Playwright Player resource capture as Markdown."""

from __future__ import annotations

import argparse
import json
import statistics
import sys
from pathlib import Path
from typing import Any, Callable


STATES = ("idle", "playing", "paused")


def metric_values(rows: list[dict[str, Any]], selector: Callable[[dict[str, Any]], Any]) -> list[float]:
    values = []
    for row in rows:
        value = selector(row)
        if isinstance(value, (int, float)) and not isinstance(value, bool):
            values.append(float(value))
    return values


def describe(values: list[float], unit: str = "") -> str:
    if not values:
        return "n/a"
    suffix = f" {unit}" if unit else ""
    return (
        f"{statistics.median(values):.1f}{suffix} "
        f"({min(values):.1f}–{max(values):.1f})"
    )


def median_or_unavailable(values: list[float]) -> str:
    return f"{statistics.median(values):.1f}" if values else "n/a"


def slope_per_hour(rows: list[dict[str, Any]], selector: Callable[[dict[str, Any]], Any]) -> float | None:
    points = []
    intervals = []
    for index, row in enumerate(rows):
        value = selector(row)
        window = row.get("window", index + 1)
        interval = row.get("seconds")
        if isinstance(value, (int, float)) and not isinstance(value, bool) and isinstance(window, (int, float)):
            points.append((float(window), float(value)))
            if isinstance(interval, (int, float)) and interval > 0:
                intervals.append(float(interval))
    if len(points) < 2 or not intervals:
        return None
    mean_window = statistics.mean(window for window, _ in points)
    mean_value = statistics.mean(value for _, value in points)
    variance = sum((window - mean_window) ** 2 for window, _ in points)
    if variance == 0:
        return None
    per_window = sum((window - mean_window) * (value - mean_value) for window, value in points) / variance
    return per_window * 3600 / statistics.median(intervals)


def tree_metric(row: dict[str, Any], key: str) -> Any:
    tree = row.get("osProcessTree")
    return tree.get(key) if isinstance(tree, dict) else None


def type_metric(row: dict[str, Any], family: str, process_type: str, key: str) -> Any:
    if family == "owned tree":
        grouped = tree_metric(row, "pssByType")
    else:
        grouped = row.get("browserPssByType")
    if not isinstance(grouped, dict):
        return None
    entry = grouped.get(process_type)
    return entry.get(key) if isinstance(entry, dict) else None


def load_capture(path: Path) -> list[dict[str, Any]]:
    with path.open(encoding="utf-8") as capture_file:
        capture = json.load(capture_file)
    if not isinstance(capture, list) or not capture:
        raise ValueError("capture must be a non-empty JSON array")
    if any(not isinstance(row, dict) or row.get("state") not in STATES for row in capture):
        raise ValueError("every capture row must have state idle, playing, or paused")
    return capture


def render(capture: list[dict[str, Any]], block_size: int) -> str:
    lines = [
        "# Player resource capture summary",
        "",
        f"Samples: {len(capture)}. Early/late comparisons use up to {block_size} windows per state.",
        "",
        "PSS and CPU describe the measured browser process tree. This report does not define portable budgets.",
        "",
        "| State | Windows | Owned-tree PSS MiB (median; range) | Early → late owned-tree PSS MiB | Renderer PSS MiB | GPU PSS MiB | CDP PSS MiB | CPU % of one core (median; max) | JS heap MiB | DOM nodes | Listeners |",
        "| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |",
    ]

    state_rows: dict[str, list[dict[str, Any]]] = {}
    for state in STATES:
        rows = [row for row in capture if row["state"] == state]
        if not rows:
            continue
        state_rows[state] = rows
        block = min(block_size, len(rows))
        tree_values = metric_values(rows, lambda row: tree_metric(row, "pssMiB"))
        early = metric_values(rows[:block], lambda row: tree_metric(row, "pssMiB"))
        late = metric_values(rows[-block:], lambda row: tree_metric(row, "pssMiB"))
        renderer = metric_values(rows, lambda row: type_metric(row, "owned tree", "renderer", "pssMiB"))
        gpu = metric_values(rows, lambda row: type_metric(row, "owned tree", "GPU", "pssMiB"))
        cdp = metric_values(rows, lambda row: row.get("browserPssMiB"))
        cpu = metric_values(rows, lambda row: tree_metric(row, "cpuPercentOfOneCore"))
        heap = metric_values(rows, lambda row: row.get("jsHeapMiB"))
        dom = metric_values(rows, lambda row: row.get("domNodes"))
        listeners = metric_values(rows, lambda row: row.get("eventListeners"))
        cpu_summary = f"{describe(cpu, '%')} / {max(cpu):.1f}%" if cpu else "n/a"
        lines.append(
            f"| {state} | {len(rows)} | {describe(tree_values)} | "
            f"{describe(early)} → {describe(late)} | {describe(renderer)} | {describe(gpu)} | "
            f"{describe(cdp)} | {cpu_summary} | "
            f"{describe(heap)} | {describe(dom)} | {describe(listeners)} |"
        )

    lines.extend(["", "## Owned-tree PSS by process type", ""])
    process_types = sorted({
        process_type
        for row in capture
        for process_type in (tree_metric(row, "pssByType") or {})
    })
    if process_types:
        lines.extend([
            "| State | Process type | Windows | Median PSS MiB | Early → late median PSS MiB | Process count range |",
            "| --- | --- | ---: | ---: | ---: | ---: |",
        ])
        for state, rows in state_rows.items():
            block = min(block_size, len(rows))
            for process_type in process_types:
                values = metric_values(rows, lambda row: type_metric(row, "owned tree", process_type, "pssMiB"))
                if not values:
                    continue
                early = metric_values(rows[:block], lambda row: type_metric(row, "owned tree", process_type, "pssMiB"))
                late = metric_values(rows[-block:], lambda row: type_metric(row, "owned tree", process_type, "pssMiB"))
                counts = metric_values(rows, lambda row: type_metric(row, "owned tree", process_type, "processCount"))
                lines.append(
                    f"| {state} | {process_type} | {len(values)} | {statistics.median(values):.1f} | "
                    f"{median_or_unavailable(early)} → {median_or_unavailable(late)} | "
                    f"{int(min(counts))}–{int(max(counts))} |"
                )
    else:
        lines.append("No owned-tree process-type readings are present.")

    lines.extend(["", "## Per-state linear trends", "", "Ordinary least-squares change per hour across the captured windows; descriptive, not an acceptance limit.", ""])
    lines.extend([
        "| State | Owned-tree PSS MiB/hour | Renderer PSS MiB/hour | JS heap MiB/hour | DOM nodes/hour | Event listeners/hour |",
        "| --- | ---: | ---: | ---: | ---: | ---: |",
    ])
    for state, rows in state_rows.items():
        trends = [
            slope_per_hour(rows, lambda row: tree_metric(row, "pssMiB")),
            slope_per_hour(rows, lambda row: type_metric(row, "owned tree", "renderer", "pssMiB")),
            slope_per_hour(rows, lambda row: row.get("jsHeapMiB")),
            slope_per_hour(rows, lambda row: row.get("domNodes")),
            slope_per_hour(rows, lambda row: row.get("eventListeners")),
        ]
        formatted = [f"{value:+.1f}" if value is not None else "n/a" for value in trends]
        lines.append(f"| {state} | " + " | ".join(formatted) + " |")

    lines.extend(["", "## Coverage", ""])
    for state, rows in state_rows.items():
        tree_rows = [row for row in rows if isinstance(row.get("osProcessTree"), dict)]
        if not tree_rows:
            lines.append(f"- {state}: Linux owned-process-tree measurements are unavailable.")
            continue
        process_counts = metric_values(tree_rows, lambda row: tree_metric(row, "processesAfter"))
        unavailable = sum(int(tree_metric(row, "memoryProcessesUnavailable") or 0) for row in tree_rows)
        added = sum(int(tree_metric(row, "processesAdded") or 0) for row in tree_rows)
        removed = sum(int(tree_metric(row, "processesRemoved") or 0) for row in tree_rows)
        process_range = f"{int(min(process_counts))}–{int(max(process_counts))}" if process_counts else "n/a"
        lines.append(
            f"- {state}: owned-tree samples {len(tree_rows)}/{len(rows)}; process count {process_range}; "
            f"unavailable owned-tree PSS reads {unavailable}; process additions/removals "
            f"{added}/{removed}."
        )
    return "\n".join(lines) + "\n"


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("capture", type=Path, help="native-resources.json from the isolated Playwright run")
    parser.add_argument("--block-size", type=int, default=10, help="windows used for early/late medians (default: 10)")
    arguments = parser.parse_args()
    if arguments.block_size < 1:
        parser.error("--block-size must be positive")
    try:
        print(render(load_capture(arguments.capture), arguments.block_size), end="")
    except (OSError, json.JSONDecodeError, ValueError) as error:
        print(f"Could not summarize Player resource capture: {error}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
