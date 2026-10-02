from pathlib import Path
r=Path(__file__).resolve().parents[1]
a=r/'Assets/Scripts/Core/CombatReviewObjectId.cs'
for p in (r/'Assets').rglob('*.cs'):
    if p!=a: assert 'GetInstanceID(' not in p.read_text(), str(p)
s=a.read_text()
assert 'EntityId.ToULong(value.GetEntityId()).ToString(CultureInfo.InvariantCulture)' in s
assert s.index('#else') < s.index('GetInstanceID()') < s.index('#endif')
e=(r/'Assets/Editor/CombatReviewFixture.cs').read_text()
assert 'Dictionary<string,string>' in e and 'Dictionary<string,float>' in e
assert 'public string kind,detail,actorId,targetId' in e
assert 'id_format=decimal-string-v2; ids=session-local' in e
print('PASS: all runtime/editor IDs route through compatible adapter, fixture retains full-width keys')
