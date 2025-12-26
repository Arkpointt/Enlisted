import json
with open(r'C:\Dev\Qodana\20yE6_6PNNM_494d3492-a2ad-4dfb-8d15-ced8d4bf380e_qodana.sarif.json', 'r', encoding='utf-8') as f:
    data = json.load(f)
    for run in data['runs']:
        for r in run.get('results', []):
            msg = r['message']['text']
            if 'article' in msg.lower():
                loc = r['locations'][0]['physicalLocation']
                print(f"{loc['artifactLocation']['uri']}:{loc['region']['startLine']}: {msg}")
        promo = run.get('properties', {}).get('qodana.promo.results', [])
        for r in promo:
            msg = r['message']['text']
            if 'article' in msg.lower():
                loc = r['locations'][0]['physicalLocation']
                print(f"PROMO: {loc['artifactLocation']['uri']}:{loc['region']['startLine']}: {msg}")
