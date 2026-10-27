# Roles and rule sets

AiRole contains an ID and behavioral instructions. AiRule adds an optional exclusive-group ID. Constructor validation rejects null/whitespace identifiers and instruction strings. These types carry no authorization authority.

AiRuleSet copies direct rules, then included sets in argument order. Included sets are already constructed immutable values, so the normal constructor path cannot create cyclic set graphs. Flattening preserves first occurrence. Identical rule references deduplicate; distinct rules with the same ordinal ID fail. Within one set, different rules occupying the same non-null exclusive group fail immediately.

DefinitionSelection applies the same conflict rule across all selected sets and individual rules. WithRole replaces one role; WithRules adds sets in selection order. If a set conflicts, the assignment to the immutable selection never occurs. A context may register mutually exclusive alternatives, but a request may not select them together. ClearRules removes both effective rules and selected set metadata.

Registered role/rule objects are immutable contracts whose properties retain compiled defaults. After ApplyDefinitions, composition reads instruction text from the captured catalog's active content by ID. Updates must preserve registered IDs, exclusive groups and each rule set's flattened ordered references. Changing those structural contracts requires changing application registration; a content snapshot can update instruction strings independently. No nested rule-set graph is persisted. See [Definition serialization and updates](DefinitionSerialization.md).

No semantic inference is attempted. An author must declare known incompatibilities through exclusive groups or validate their domain configuration externally. Text position is deterministic, not an undocumented override mechanism. The same instruction text in two distinct non-conflicting rules is emitted twice; content-based deduplication is not performed.
