import argparse
from pathlib import Path
import re


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", default="ModuleData/Languages/enlisted_strings.xml", help="Source enlisted_strings.xml")
    parser.add_argument("--language", required=True, help="Target language name as Bannerlord expects, e.g. French, German, Russian")
    parser.add_argument("--output", required=True, help="Output path for the translated XML file")
    parser.add_argument("--blank", action="store_true", help="Blank out all text attributes (keeps IDs).")
    args = parser.parse_args()

    src = Path(args.input)
    if not src.exists():
        raise FileNotFoundError(src)

    xml = src.read_text(encoding="utf-8")

    # Replace/insert language tag(s). Keep file format stable.
    xml = re.sub(r'(<tag\s+language=")[^"]+("\s*/>)', rf'\1{args.language}\2', xml, count=1, flags=re.IGNORECASE)

    if args.blank:
        # Replace text="..." with text=""
        xml = re.sub(r'\btext="[^"]*"', 'text=""', xml)

    out = Path(args.output)
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text(xml, encoding="utf-8")
    print(f"Wrote: {out}")


if __name__ == "__main__":
    main()


