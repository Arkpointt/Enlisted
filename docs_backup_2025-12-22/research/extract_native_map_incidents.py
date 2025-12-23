import argparse
import json
import re
from dataclasses import dataclass, asdict
from pathlib import Path


@dataclass(frozen=True)
class IncidentDef:
    id: str
    title: str
    description: str
    trigger: str
    type: str


_REGISTER_RE = re.compile(
    r'RegisterIncident\(\s*'
    r'"(?P<id>[^"]+)"\s*,\s*'
    r'"(?P<title>[^"]*)"\s*,\s*'
    r'"(?P<description>[^"]*)"\s*,\s*'
    r"IncidentsCampaignBehaviour\.IncidentTrigger\.(?P<trigger>[^,]+)\s*,\s*"
    r"IncidentsCampaignBehaviour\.IncidentType\.(?P<type>[^,)+]+)"
)


def _extract(text: str) -> list[IncidentDef]:
    items: list[IncidentDef] = []
    for m in _REGISTER_RE.finditer(text):
        raw_trigger = m.group("trigger").strip()
        # The decompile sometimes emits combined triggers like:
        #   LeavingTown | IncidentsCampaignBehaviour.IncidentTrigger.LeavingCastle
        # Normalize to a clean, readable form while keeping the information intact.
        trigger = (
            raw_trigger.replace("IncidentsCampaignBehaviour.IncidentTrigger.", "")
            .replace("IncidentTrigger.", "")
            .strip()
        )
        items.append(
            IncidentDef(
                id=m.group("id").strip(),
                title=m.group("title"),
                description=m.group("description"),
                trigger=trigger,
                type=m.group("type").strip(),
            )
        )
    # Stable output for diffs / tooling.
    items.sort(key=lambda x: x.id)
    return items


def _strip_loc(text: str) -> str:
    # Native strings are often in the form "{=SOMEKEY}Human text...".
    if text.startswith("{=") and "}" in text:
        return text.split("}", 1)[1]
    return text


def _to_markdown_table(incidents: list[IncidentDef]) -> str:
    # Keep it easy to scan + grep.
    lines: list[str] = []
    lines.append("| ID | Trigger | Type | Title |")
    lines.append("|---|---|---|---|")
    for inc in incidents:
        title = _strip_loc(inc.title).replace("\n", " ").strip()
        lines.append(f"| `{inc.id}` | `{inc.trigger}` | `{inc.type}` | {title} |")
    return "\n".join(lines) + "\n"


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Extract native Bannerlord Map Incident definitions from a decompiled IncidentsCampaignBehaviour.cs"
    )
    parser.add_argument(
        "--input",
        required=True,
        help="Path to IncidentsCampaignBehaviour.cs (decompiled).",
    )
    parser.add_argument(
        "--output",
        required=True,
        help="Path to write JSON output.",
    )
    parser.add_argument(
        "--markdown-output",
        required=False,
        default="",
        help="Optional path to also write a markdown table of incidents.",
    )
    args = parser.parse_args()

    input_path = Path(args.input)
    output_path = Path(args.output)

    text = input_path.read_text(encoding="utf-8", errors="replace")
    incidents = _extract(text)

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(
        json.dumps([asdict(x) for x in incidents], indent=2, ensure_ascii=False) + "\n",
        encoding="utf-8",
    )
    if args.markdown_output:
        md_path = Path(args.markdown_output)
        md_path.parent.mkdir(parents=True, exist_ok=True)
        md_path.write_text(_to_markdown_table(incidents), encoding="utf-8")
        print(f"Wrote incident markdown table to {md_path}")
    print(f"Wrote {len(incidents)} incidents to {output_path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())


