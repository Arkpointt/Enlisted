import json
with open(r'C:\Dev\Qodana\qodana.sarif.json', 'r', encoding='utf-8') as f:
    data = json.load(f)
    results = data['runs'][0].get('results', [])
    for r in results:
        msg = r['message']['text']
        if 'Missing article' in msg or 'usually goes with an article' in msg:
             loc = r['locations'][0]['physicalLocation']
             print(f'{loc["artifactLocation"]["uri"]}:{loc["region"]["startLine"]}: {msg}')
