# Playbook: Maintaining reusable agent guidance

Use when creating or revising a project skill or when implementation work establishes durable, reusable project knowledge.

- Keep `docs/agents/` playbooks canonical. Agent-specific skill files should be short discovery pointers to those playbooks, not competing copies.
- Prefer updating the closest playbook over creating overlapping guidance. Create a new playbook only for a distinct, reusable workflow or domain.
- Capture when to use the guidance, authoritative sources, stable contracts/constraints, important edge cases, and focused validation. Link to canon rather than copying it wholesale.
- Preserve the status of decisions: do not turn a proposal, open question, or *(tune)* value into a rule.
- Update all affected playbooks and their discovery pointers together; keep corresponding `.agents/skills/` and `.claude/skills/` pointers consistent.
- Remove stale references when a source moves. Do not encode temporary implementation details or one-off task notes as permanent guidance.

Before finishing, verify that the owning source remains authoritative, pointers resolve, and the new guidance adds useful context without duplicating existing material.
