import glob
import json
import sys
from collections import Counter


def main() -> int:
    files = sorted(glob.glob("ModuleData/Enlisted/Events/events_*.json"))
    if not files:
        print("[validate] No event packs found under ModuleData/Enlisted/Events/")
        return 2

    seen = set()
    bad = []
    total = 0

    for f in files:
        with open(f, encoding="utf-8") as fp:
            obj = json.load(fp)

        events = obj.get("events") or []
        for e in events:
            eid = (e.get("id") or "").strip()
            if not eid:
                bad.append(("MISSING_ID", f))
                continue

            if eid in seen:
                bad.append(("DUP_ID", eid, f))
            seen.add(eid)

            opts = ((e.get("content") or {}).get("options") or [])
            if not (2 <= len(opts) <= 4):
                bad.append(("BAD_OPT_COUNT", eid, len(opts), f))

            total += 1

    # Sanity counts
    c = Counter()
    for b in bad:
        c[b[0]] += 1

    print(f"[validate] files={len(files)} events={total} unique={len(seen)}")
    if bad:
        print("[validate] FAIL")
        for b in bad[:100]:
            print("[validate]", b)
        print("[validate] summary:", dict(c))
        return 2

    print("[validate] OK")
    return 0


if __name__ == "__main__":
    sys.exit(main())


