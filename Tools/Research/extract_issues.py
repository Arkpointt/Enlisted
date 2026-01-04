import json
with open(r'C:\Dev\Qodana\qodana.sarif.json', 'r', encoding='utf-8') as f:
    data = json.load(f)
    results = data['runs'][0]['properties']['qodana.promo.results']
    relevant = [r for r in results if any(msg in r['message']['text'] for msg in ['Missing article', 'usually goes with an article', 'Backpay', 'vlandia', 'battania', 'khuzait', 'aserai', 'subsequent', 'dismiss', 'comma'])]
    for r in relevant:
        loc = r['locations'][0]['physicalLocation']
        print(f'{loc["artifactLocation"]["uri"]}:{loc["region"]["startLine"]}: {r["message"]["text"]}')
