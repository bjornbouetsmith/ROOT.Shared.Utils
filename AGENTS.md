# Agent Instructions

## Unit tests

Any agent that creates, changes, or reviews unit tests **must use the
repository's unit-testing skill**.

- Skill file: `.skills/unit-testing/SKILL.md`
- Load it with the `skill` tool using the ID `unit-testing` when available.
  If the skill is not registered with the tool, read
  `.skills/unit-testing/SKILL.md` directly and follow it before starting.
- Follow the skill's MSTest conventions, determinism and isolation rules, and
  verification workflow.
- A test change is not complete until it contains real assertions, the focused
  tests pass, and the full test suite has been run successfully.
