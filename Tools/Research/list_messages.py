import json
with open(r'C:\Dev\Qodana\qodana.sarif.json', 'r', encoding='utf-8') as f:
    data = json.load(f)
    results = data['runs'][0]['properties']['qodana.promo.results']
    # Print first 100 result messages to see what they look like
    for i, r in enumerate(results[:100]):
        print(f"{i}: {r['message']['text']}")
