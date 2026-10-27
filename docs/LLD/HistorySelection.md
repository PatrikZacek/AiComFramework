# Context selection and history

Full recorded history contains every committed turn, regardless of the active domain. For a request, HistorySelection computes eligibility from turns whose recorded Request.Configuration.DomainId equals the active captured catalog's DomainId, plus explicit HistoryOptions.SharedTurnIds. Unknown sharing IDs are rejected; grants apply to that request only. Rejected validation attempts are not committed turns and cannot be selected or shared.

RecentHistory selects the newest N eligible turns, retaining chronology. Its default is 20 and zero selects no history. Counts are turns, not tokens. IHistorySelector receives an immutable eligible-turn array and returns IDs. Core rejects unknown, ineligible, duplicate, default-array and out-of-order results. A strategy cannot bypass domain eligibility.

HistorySelection records both the eligible IDs and actual transmitted IDs. Only selected turns' Exchange messages are replayed. Each Exchange contains the original rendered prompt and assistant text, excluding historical system instructions, reusable data and runtime context messages. This also avoids recursively replaying prior requests and exponentially duplicating history.

Role/rule changes and RefreshDefinitions in the same domain retain eligibility. UseContext preserves recorded history, but only turns matching its captured DomainId or explicit sharing grants are eligible. Different context types with the same overridden DomainId share eligibility; switching to a different DomainId excludes old turns unless explicitly shared. Returning to an earlier DomainId makes its exchanges eligible again. Context instance identity and tenant identity are not part of this policy; applications must use separate conversations for tenant isolation. A definition update does not rewrite earlier prompts or assistant replies.

Explicit sharing can disclose user or assistant text containing sensitive context-derived facts. Excluding original context messages cannot remove facts already quoted by the model. No semantic redaction or semantic history selection is implemented.

A selector failure prevents provider execution and adds no turn. Tests distinguish recorded, eligible and transmitted history, test invalid strategies, and demonstrate explicit one-turn cross-domain sharing.

Default domain IDs include the assembly simple name to separate identical full type names from different libraries. Assembly versions are intentionally excluded so routine version changes do not automatically rename a domain. An application-owned DomainId override replaces that default; its stability and uniqueness belong to the application. See [Domain contexts](DomainContexts.md). Membership validation uses hash sets plus a chronological subsequence comparison, avoiding repeated linear membership scans while retaining duplicate and ordering checks.
