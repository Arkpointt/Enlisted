import json
with open(r'C:\Dev\Qodana\qodana.sarif.json', 'r', encoding='utf-8') as f:
    data = json.load(f)
    # Check all runs and all properties
    for run in data['runs']:
        # Check standard results
        for r in run.get('results', []):
            msg = r['message']['text']
            if 'article' in msg.lower():
                loc = r['locations'][0]['physicalLocation']
                print(f"{loc['artifactLocation']['uri']}:{loc['region']['startLine']}: {msg}")
        # Check promo results
        promo = run.get('properties', {}).get('qodana.promo.results', [])
        for r in promo:
            msg = r['message']['text']
            if 'article' in msg.lower():
                loc = r['locations'][0]['physicalLocation']
                print(f"PROMO: {loc['artifactLocation']['uri']}:{loc['region']['startLine']}: {msg}")
