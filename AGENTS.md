# AGENTS.md - FunGoo

## Imperatives

**IMPERATIVE**: Report to me ONLY in ASD-STE100 Simplified Technical English.

1. **NEVER PUSH WITHOUT PERMISSION.** Always ask before pushing to the remote. A Previous permission push for a particular set of work DOES NOT MEAN TO ALWAYS PUSH AFTER. Permissions are granted per set of work not for session length.
2. **NEVER FORCE PUSH.** Tell the user they have to force push instead of you.
3. **Always run `dotnet fantomas .` before committing code.** Format all F# files before staging.
4. **Prefer `ValueOption.*` combinators over nested matches.** When threading optional values, chain `ValueOption.map` / `bind` / `filter` / `map` on value, `ValueOption.iter`, `defaultValue`, `orElse`, and `AVal.map`/`AMap.tryFind` pipelines instead of hand-rolling nested `match ... with | ValueSome x -> ... | ValueNone -> ...` ladders. Match only when the two branches carry substantially different logic, not to unwrap and re-wrap.
5. Pull requests made with the `gh` command should use a markdown file as the PR body, not inline escaped markdown strings.
6. New F# files **MUST** be paired with its own signature file for source code. tests and samples do not requre it.
7. Comments go to the signature files. implementation quirk comments may stay in implementation files.

## Performance Considerations

Performance is a core feature of this DSL any change must take perf very seriously in consideration when applying a new change.

- Prefer erasable functions (let inline, static member inline) + [<InlineIfLambda>] over closures.
- Prefer structs over classes unless struct size is too large.
- Prefer object expressions to classes.
- Avoid heap allocations in hot paths.
- Favor functional programming patterns but allow mutable state when necessary for performance.
- Functional-looking API with self-contained mutation when required in hot paths.
- Readability is a MUST

## Changelog Management

We follow https://github.com/ionide/KeepAChangelog guidelines

Changelog Format:

```markdown
# Changelog

## [Unreleased]

### Added

- Instanced draws inside a `beginEffect`/`endEffect` scope are now shaded by the user shader when it opts into instancing (raylib: `in mat4 instanceTransform;`; MonoGame: an `Instanced` technique reading `TEXCOORD1..4`). Effects that don't opt in keep the previous PBR-instanced fallback. Skinned + instanced remains unsupported. See `docs/shader-uniforms.md` → "Instancing (opt-in)".

## [1.0.0] - 2026.01.13

### Added

- Initial release
```

Each section may contain the following categories:

- Added
- Changed
- Deprecated
- Removed
- Fixed
- Security

When adding entries to the changelog, make sure to follow format and categories.

### Writing style

The changelog is written for **developers upgrading their version**, not as a development
journal. Keep these rules in mind:

1. **Concise and reader-focused.** Each entry is one bullet that says what changed and why a
   user cares — not how it's implemented internally. No internal module/file paths, no build/
   milestone/phase numbers (e.g. "B12", "Phase 3"), no section references (e.g. "§6.2"), and no
   "mirrors the canonical X" narration. A reader should understand the entry without reading the
   code.

2. **Group by user-facing concern, not by task.** One bullet per feature/fix area. If multiple
   commits touch the same subsystem (e.g. several shadow-pass fixes), collapse them into one
   bullet that names each fix briefly, rather than one bullet per commit.

3. **Only released code can be Changed or Fixed.** Features that have never shipped belong in
   `Added` — there is no prior version to change from or fix against. Design choices and
   implementation details of a new feature are part of its `Added` description, not separate
   `Fixed`/`Changed` entries. Use `Changed`/`Fixed` only for modifications to already-released
   behavior (and mark breakage with **Breaking:** or **Breaking (behavioral):**).

4. **Lead with the affected surface.** Bold-prefix each bullet with the area:
   `**MonoGame 3D:**`, `**Core:**`, `**Raylib:**`, `**MonoGame 2D:**`, etc. — so a reader can
   scan for their backend. Keep breaking changes at the top of their category.

5. **Plain language.** Describe the user-visible effect ("shadows render correctly on scaled
   objects"), not the code diff ("BoundingSphere.Transform now scales center and radius"). The
   reader wants to know what they'll observe, not what line changed.
