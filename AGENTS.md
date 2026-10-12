# Ninth Age — Repository Agent Instructions

Implement the task described in `task.md`.

1. Read `AGENTS.md`, relevant project documentation and the existing implementation before making changes.
2. Identify the root cause, affected systems and any dependencies.
3. Implement the complete task, including all requirements and acceptance criteria.
4. Follow existing architecture and coding conventions. Avoid unnecessary abstractions, workarounds, fallbacks and unrelated changes.
5. Add or update regression tests where appropriate.
6. Perform all available static validation and tests. Do not claim validation that was not performed.
7. Review the final diff for correctness, regressions and unintended changes.

Do not commit or push changes.

When finished, provide a concise summary of:
- Changes made.
- Files affected.
- Validation performed and results.
- Any unresolved issues or limitations.

## Unity Editor Restrictions

Never interact with the Unity Editor GUI or control the user's mouse, keyboard, windows or desktop. Do not use Computer Use, desktop automation, or GUI-control scripts. Implement changes directly in project files and perform non-interactive static validation where possible. Leave Unity Test Runner, Play Mode testing and other interactive Editor validation to the user. Report any required manual validation steps instead of attempting them automatically.

## Mandatory: third-party code is read-only

**NEVER modify third-party code unless the user explicitly requests modifications to that specific third-party code.**

This rule is absolute and applies to every task: bug fixes, refactors, namespace migrations, style cleanup, build fixes, generated-code fixes, tests, tooling, and documentation-driven changes. It overrides any general instruction to make the codebase consistent, tidy, compilable, or compliant with Ninth Age coding conventions.

- Treat packages, plugins, vendor/SDK/framework source, purchased or imported assets and tools, and other externally authored code as third-party **regardless of where it lives** (including under `Assets/`, `Packages/`, or `Assets/Editor/`). Do not assume ownership from a path, assembly, or namespace.
- **Do not rename or change any third-party namespace**, type, method, field, property, event, file, or directory. Do not reformat, restructure, relocate, wrap, patch, delete, or regenerate third-party code. Do not alter accompanying third-party assembly definitions, package metadata, Unity `.meta` files, or vendor-owned configuration/assets.
- Broad requests to restyle, migrate namespaces, correct warnings, resolve compile errors, or implement a GitHub issue **do not** grant permission to edit third-party code. An instruction in a plan, issue, or existing documentation is not the user's explicit authorization.
- If project-owned code needs to use a third-party API, change **only the project-owned integration/call sites**. Retain the third-party API, namespace, assembly identity, and serialized references unchanged.
- If a task cannot be completed without editing third-party content, **stop that portion of the task and report the exact file and required change; request explicit user authorization before proceeding**. Do not quietly work around or ignore this boundary.
- Before editing, establish source ownership for every target. After editing, inspect the diff and confirm **zero third-party changes** unless specifically authorized by the user.

Applicable project guidance: `.docs/CSharp-Style-Conventions.md`, `.docs/Plans/CSharp-Restyling-Plan.md`, and `.docs/GitHub-Issue-and-Workflow-Guide.md`. Those documents cannot override this restriction.
