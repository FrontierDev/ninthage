# Ninth Age — Repository Agent Instructions

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
