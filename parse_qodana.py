import json
import sys

sarif_path = r"C:\Dev\Qodana\20yE6_6PNNM_d7a3021c-abd5-4243-a95f-1868b9dcd655_qodana.sarif.json"

with open(sarif_path, 'r', encoding='utf-8') as f:
    data = json.load(f)

# Extract results
results = data['runs'][0]['results']
rules = {rule['id']: rule for rule in data['runs'][0]['tool']['driver']['rules']}

print(f"Total issues found: {len(results)}\n")

# Group by rule
issues_by_rule = {}
for result in results:
    rule_id = result['ruleId']
    if rule_id not in issues_by_rule:
        issues_by_rule[rule_id] = []
    issues_by_rule[rule_id].append(result)

# Print summary
print("=" * 80)
print("ISSUE SUMMARY BY TYPE")
print("=" * 80)
for rule_id, issues in sorted(issues_by_rule.items(), key=lambda x: len(x[1]), reverse=True):
    rule_name = rules.get(rule_id, {}).get('shortDescription', {}).get('text', rule_id)
    print(f"{rule_id}: {len(issues)} issues")
    print(f"  Description: {rule_name}")
    print()

# Print detailed issues
print("\n" + "=" * 80)
print("DETAILED ISSUES")
print("=" * 80)

for rule_id, issues in sorted(issues_by_rule.items()):
    print(f"\n### {rule_id} ({len(issues)} issues)")
    print(f"Description: {rules.get(rule_id, {}).get('shortDescription', {}).get('text', 'N/A')}")
    print()

    for i, issue in enumerate(issues[:10], 1):  # Show first 10 of each type
        location = issue['locations'][0]['physicalLocation']
        file_path = location['artifactLocation']['uri']
        line = location['region']['startLine']
        message = issue['message']['text']

        print(f"  {i}. {file_path}:{line}")
        print(f"     {message}")
        print()

    if len(issues) > 10:
        print(f"  ... and {len(issues) - 10} more\n")
