# `##FLAG-FILE` and `##FLAG-DIRECTORY`

These directives decide **at build time** which file, or which folder's contents, to include. The choice depends on the `#flags` you pass to `Invoke-TsmBuild -Flags`. Use them to:

- **swap in specialized code** when a flag is set, such as Azure-only code for an `#Azure` build;
- **fall back to the normal code** when it isn't;
- or **include extra code only when a flag is set**, and nothing otherwise.

```sql
-- ##FLAG-FILE(<keys>): <path> (<keys>): <path> ...
-- ##FLAG-DIRECTORY(<keys>): <directory> (<keys>): <directory> ...
```

Each `(<keys>): <target>` pair is an **option**. A directive needs at least one option.

---

## How it works

### FLAG directives become `##FILE` and `##DIRECTORY` directives

At build time, tsmake picks **one** option, or none, and rewrites the FLAG directive as a plain directive:

- `##FLAG-FILE` becomes a `##FILE` directive.
- `##FLAG-DIRECTORY` becomes a `##DIRECTORY` directive.
- If no option is picked, the directive becomes **nothing**.

| You write | With `-Flags "#Azure"` it becomes | With no flags it becomes |
|---|---|---|
| `-- ##FLAG-FILE(#Azure): azure.sql (DEFAULT): onprem.sql` | `-- ##FILE: azure.sql` | `-- ##FILE: onprem.sql` |
| `-- ##FLAG-DIRECTORY(#Azure): funcs\azure [sort: setup.sql, ALPHA] (DEFAULT): funcs\onprem` | `-- ##DIRECTORY: funcs\azure [sort: setup.sql, ALPHA]` | `-- ##DIRECTORY: funcs\onprem` |
| `-- ##FLAG-FILE(#Azure): azure_extras.sql` | `-- ##FILE: azure_extras.sql` | *(nothing)* |

From then on, it's an ordinary `##FILE` or `##DIRECTORY` directive:

- What it includes goes **exactly where the directive is**.
- The included files are processed like any other file, including their own directives.
- Paths, `[exclude:]` and `[sort:]` follow the same rules. See [`##DIRECTORY`](directory.md) for the details.

### Which option is picked

1. tsmake checks your `-Flags` **in the order you passed them**. The **first** flag that has an option wins.
2. If none of your flags has an option, or you passed no flags, the `(DEFAULT)` option is used.
3. If there's no `(DEFAULT)` option, **nothing is included**.

- ⚠️ **The order of `-Flags` decides, not the order of the options.** With `-Flags "#RDS", "#Azure"`, an `(#RDS)` option beats an `(#Azure)` option, wherever each one appears in the directive (Example 2).
- **Flags apply to the whole build.** That includes files that were themselves included by a FLAG directive (Example 6).

---

## Passing flags: `Invoke-TsmBuild -Flags`

```powershell
Invoke-TsmBuild -Flags "#Azure"
Invoke-TsmBuild -Flags "#RDS", "#Azure"
```

- **Every flag must start with `#`.** `-Flags "Azure"` throws: `Flags MUST start with the '#' sign. Invalid Flags: [Azure].`
- ⚠️ **Quote your flags.** In PowerShell, an unquoted `#` starts a comment, so `-Flags #Azure` passes nothing. PowerShell then fails with `Missing an argument for parameter 'Flags'`.
- **Case doesn't matter:** `-Flags "#azure"` picks an `(#Azure)` option.
- **Clean-up:** spaces around a flag are trimmed, and blank entries are ignored.
- **Unused flags are fine:** a flag that no directive uses is silently ignored.

---

## Syntax

### The directive line

- **The first option is part of the name:** the first option's keys go right after the directive name, as in `-- ##FLAG-FILE(#Azure): azure.sql`.
- **Aliases:** `##FLAGS-FILE` and `##FLAGS-DIRECTORY` work the same way.
- **Case:** the directive name and the keys ignore case.
- **Spaces:**
  - Allowed before `--`, between `--` and `##`, between the name and `(`, and inside the parentheses.
  - Not allowed between `##` and `FLAG`.
  - At most **one** space is allowed before each option's `:`.
- **One line:** the whole directive must be on a single line.
- **Comments:** anything after `##::` is a comment and is ignored.
- **Malformed FLAG directives are errors.** Any line that starts with `-- ##FLAG-FILE` or `-- ##FLAG-DIRECTORY` (or a `FLAGS-` alias) is treated as a FLAG directive. If it's malformed, the build reports an error **against that line**. Malformed includes:
  - no keys;
  - bad key names;
  - a missing `:`;
  - two or more spaces before a `:`.
- **Lookalike names are errors too.** `##FLAG`, `##FLAGS`, `##FLAG-DEFAULT` and `##FLAG-END`, followed by `(` or `:`, aren't directives. The error says which directive to use instead (Example 7).
- ⚠️ **Only one wrong form still fails silently:** a space after `##`, as in `-- ## FLAG-FILE(...)`. That's the same as for every other directive. tsmake treats the line as an ordinary comment, so **nothing is included**, and the line stays in the build output as a comment.
  - Names that only *start* with `FLAG` are ordinary comments too, such as `##FLAG-FILES:` or `##FLAGGED`.
  - So is `##FLAG` without a `(` or `:` after it, as in `-- ##FLAG this for review`.

### Options: `(<keys>): <target>`

- **Keys:** a key is a `#flag` or `DEFAULT`.
  - Flag names may contain only `A-Z`, `a-z`, `0-9`, `-` and `_`.
  - The `#` is optional inside the directive, so `(Azure)` is the same as `(#Azure)`.
- **Several keys:** `(#RDS, #Azure): cloud.sql` is picked when **any** of those flags is passed.
  - ⚠️ It means "either", not "both". There's no way to require several flags at once.
- **No duplicates:** each key may appear only once per directive, across all of its options. That includes `DEFAULT`.
- **Any order:** options can come in any order, and `(DEFAULT)` doesn't have to be last.

### `##FLAG-FILE` targets

- **What a target is:** a file path, in the same forms as `##FILE`.
  - Relative paths are relative to **`##ROOT`**, not to the file that contains the directive.
  - `~\...`, absolute and UNC paths also work.
- **Quotes:**
  - Spaces are fine without quotes: `(DEFAULT): My Scripts\on prem.sql`.
  - Quote the path with `"double"` or `'single'` quotes if it contains `(`, `)` or a quote character. An unquoted `(` starts the next option.

### `##FLAG-DIRECTORY` targets

- **What a target is:** a complete `##DIRECTORY` payload, meaning a path plus any `[exclude: ...]` and `[sort: ...]` clauses. For example: `(#Azure): funcs\azure [exclude: setup.sql] [sort: ALPHADESC]`.
- **Where it ends:** each option's target runs until the next `(<keys>):` or the end of the line.
- **Details:** see [`##DIRECTORY`](directory.md) for paths, quoting, `[exclude:]` and `[sort:]`.

### ⚠️ Every option is checked, not just the one that's picked

tsmake validates **every** option on every build. If any option's file or folder doesn't exist, or has bad syntax, the **whole directive is invalid** and the build reports an error. That happens even for builds that would never pick that option. So a broken `(#Azure)` path stops an on-premises build too, and a broken path can't hide until someone builds with that flag.

### Errors

A malformed directive is reported as an error, with its line number and a message:

| Directive | Problem |
|---|---|
| `-- ##FLAG-FILE: azure.sql` | `Expected a (#FLAG) or (DEFAULT) option immediately after ##FLAG-FILE`. The first option's keys are part of the name. |
| `-- ##FLAG-FILE` | `No (#FLAG) or (DEFAULT) options found.` |
| `-- ##FLAG-FILE(#Azure)  : azure.sql` | `Missing ':' after '(#Azure)' (at most one space is allowed before it).` |
| `-- ##FLAG-FILE(#Azure) azure.sql` | `Missing ':' after '(#Azure)'` |
| `-- ##FLAG-FILE(#My Flag): azure.sql` | Names may only contain `A-Z`, `a-z`, `0-9`, `-` and `_` |
| `-- ##FLAG(#Azure): azure.sql` | `Directive 'FLAG' is not valid. Use FLAG-FILE or FLAG-DIRECTORY instead.` (same for `##FLAGS`) |
| `-- ##FLAG-DEFAULT:` | `Directive 'FLAG-DEFAULT' is not valid. Use CONDITION-DEFAULT instead.` |
| `-- ##FLAG-END:` | `Directive 'FLAG-END' is not valid. Use CONDITION-END instead.` |
| `-- ##FLAG-FILE(#Azure): no_such.sql (DEFAULT): onprem.sql` | `Path specified for (#AZURE): does not exist: no_such.sql`. This is reported even with no flags. |
| `-- ##FLAG-FILE(#Azure): azure.sql (#azure): cloud.sql` | `Duplicate directive '#AZURE'.` (keys ignore case) |
| `-- ##FLAG-FILE(#Azure, #RDS): cloud.sql (#RDS): rds.sql` | `Duplicate directive '#RDS'.` |
| `-- ##FLAG-FILE(DEFAULT): onprem.sql (DEFAULT): azure.sql` | `Duplicate directive 'DEFAULT'.` |
| `-- ##FLAG-FILE(#Azure): azure.sql (DEFAULT) onprem.sql` | `Missing ':' after '(DEFAULT)'` |
| `-- ##FLAG-FILE(#Azure): azure.sql (DEFAULT)  : onprem.sql` | `Missing ':' after '(DEFAULT)' (at most one space is allowed before it)` |
| `-- ##FLAG-FILE(#Azure): azure.sql (#My Flag): onprem.sql` | Names may only contain `A-Z`, `a-z`, `0-9`, `-` and `_` |
| `-- ##FLAG-FILE(#Azure): Odd (v2)\azure.sql` | `Missing ':' after '(v2)'`. The unquoted `(` starts a new option, so quote the path. |
| `-- ##FLAG-FILE(#Azure):` | The option has no path |
| `-- ##FLAG-DIRECTORY(#Azure): funcs\nope (DEFAULT): funcs\onprem` | `Directory specified for (#AZURE) does not exist: <full path>` |
| `-- ##FLAG-DIRECTORY(#Azure): funcs\azure [sort: ALPHA, TIMESTAMP] (DEFAULT): funcs\onprem` | `Invalid ##DIRECTORY details for (#Azure): Only one sort marker ... is allowed` |
| `-- ##FLAG-DIRECTORY: funcs\azure` | `Expected a (#FLAG) or (DEFAULT) option immediately after ##FLAG-DIRECTORY` |
| `-- ##FLAG-DIRECTORY(#Azure)  : funcs\azure` | `Missing ':' after '(#Azure)' (at most one space is allowed before it).` |
| `-- ##FLAG-FILE(#Azure): {{##NAME}}.sql` | `Tokens can't be used within directives - they are never replaced there.` |

### ⚠️ Header comments

If you remove header comments (`-CommentDirectives RemoveHeader` or `RemoveAll`), a FLAG directive that comes **before a file's first line of code** is part of that file's header. It's removed along with the header, so it never runs. Put FLAG directives below the first line of code, or don't remove header comments.

---

## Examples

Every output below comes from running the directive through tsmake. The examples use this project:

```
\
|-- app.build.sql
|-- azure.sql             PRINT 'azure';
|-- onprem.sql            PRINT 'onprem';
|-- rds.sql               PRINT 'rds';
|-- cloud.sql             PRINT 'cloud';
|-- azure_extras.sql      PRINT 'azure_extras';
|-- funcs\
    |-- azure\
    |   |-- a.sql         PRINT 'azure a';
    |   |-- b.sql         PRINT 'azure b';
    |   |-- setup.sql     PRINT 'azure setup';
    |-- onprem\
        |-- a.sql         PRINT 'onprem a';
        |-- b.sql         PRINT 'onprem b';
```

In each example, the directive goes between the first and last lines of `app.build.sql`:

```sql
-- ##OUTPUT: app_latest.sql
PRINT 'start';
-- <the directive>
PRINT 'end';
```

Each **Output** column shows what ends up between `start` and `end`.

### 1. Specialized code, with a default fallback

```sql
-- ##FLAG-FILE(#Azure): azure.sql (DEFAULT): onprem.sql
```

| `-Flags` | Becomes | Output | Why |
|---|---|---|---|
| *(none)* | `-- ##FILE: onprem.sql` | onprem | No flags, so `(DEFAULT)` is used |
| `"#Azure"` | `-- ##FILE: azure.sql` | azure | `#Azure` has an option |
| `"#azure"` | `-- ##FILE: azure.sql` | azure | Flags ignore case |
| `"#RDS"` | `-- ##FILE: onprem.sql` | onprem | `#RDS` has no option, so `(DEFAULT)` is used |

### 2. Several specializations

```sql
-- ##FLAG-FILE(#Azure): azure.sql (#RDS): rds.sql (DEFAULT): onprem.sql
```

| `-Flags` | Output | Why |
|---|---|---|
| `"#Azure"` | azure | |
| `"#RDS"` | rds | |
| `"#RDS", "#Azure"` | **rds** | ⚠️ The first of `-Flags` that has an option wins |
| `"#Azure", "#RDS"` | **azure** | Same rule, with the flags in the other order |
| *(none)* | onprem | |

### 3. One option for several flags

```sql
-- ##FLAG-FILE(#RDS, #Azure): cloud.sql (DEFAULT): onprem.sql
```

| `-Flags` | Output |
|---|---|
| `"#RDS"` | cloud |
| `"#Azure"` | cloud |
| *(none)* | onprem |

`(#RDS, #Azure)` means **either** flag. There's no syntax for "only when *both* are set".

### 4. Include something only when a flag is set

Leave out `(DEFAULT)`. Without a matching flag, the directive includes **nothing**:

```sql
-- ##FLAG-FILE(#Azure): azure_extras.sql
```

| `-Flags` | Output |
|---|---|
| *(none)* | *(nothing)* |
| `"#Azure"` | azure_extras |
| `"#RDS"` | *(nothing)* |

The same works with several flags. This directive includes `cloud.sql` for `#RDS` **or** `#Azure`, and nothing otherwise:

```sql
-- ##FLAG-FILE(#RDS, #Azure): cloud.sql
```

A directive with **only** a `(DEFAULT)` option, such as `-- ##FLAG-FILE(DEFAULT): onprem.sql`, always includes its file, whatever the flags. It's the same as `-- ##FILE: onprem.sql`.

### 5. Folders: `##FLAG-DIRECTORY`

Each option is a complete `##DIRECTORY` payload, so every option can have its own `[exclude:]` and `[sort:]`:

```sql
-- ##FLAG-DIRECTORY(#Azure): funcs\azure [sort: setup.sql, ALPHA] (DEFAULT): funcs\onprem
```

| `-Flags` | Becomes | Output |
|---|---|---|
| *(none)* | `-- ##DIRECTORY: funcs\onprem` | onprem a, onprem b |
| `"#Azure"` | `-- ##DIRECTORY: funcs\azure [sort: setup.sql, ALPHA]` | **azure setup**, azure a, azure b |

With no `(DEFAULT)`, the folder is included only when the flag is set:

```sql
-- ##FLAG-DIRECTORY(#Azure): funcs\azure [exclude: setup.sql] [sort: ALPHADESC]
```

| `-Flags` | Output |
|---|---|
| *(none)* | *(nothing)* |
| `"#Azure"` | azure b, azure a |

### 6. Combining directives, and FLAG directives in included files

Every FLAG directive in a build sees the same `-Flags`, so one flag can switch several pieces at once:

```sql
-- ##FLAG-FILE(#Azure): azure.sql (DEFAULT): onprem.sql
-- ##FLAG-DIRECTORY(#Azure): funcs\azure [exclude: setup.sql] (DEFAULT): funcs\onprem
```

| `-Flags` | Output |
|---|---|
| *(none)* | onprem, onprem a, onprem b |
| `"#Azure"` | azure, azure a, azure b |

A file that's included by a FLAG directive can contain FLAG directives of its own. Here's `nested.sql`:

```sql
PRINT 'nested start';
-- ##FLAG-FILE(#Azure): azure.sql
PRINT 'nested end';
```

And here's the directive in `app.build.sql` that includes it:

```sql
-- ##FLAG-FILE(#Azure, #RDS): nested.sql
```

| `-Flags` | Output |
|---|---|
| `"#Azure"` | nested start, azure, nested end |
| `"#RDS"` | nested start, nested end |
| *(none)* | *(nothing)* |

### 7. Common mistakes

Each of these is reported as an error against its line. The build doesn't silently skip the code you meant to include:

| Line | Error |
|---|---|
| `-- ##FLAG-FILE: azure.sql` | No `(keys)`: `Expected a (#FLAG) or (DEFAULT) option immediately after ##FLAG-FILE` |
| `-- ##FLAG(#Azure): azure.sql` | `Directive 'FLAG' is not valid. Use FLAG-FILE or FLAG-DIRECTORY instead.` |
| `-- ##FLAG-FILE(#Azure)  : azure.sql` | Two spaces before the `:`: `Missing ':' after '(#Azure)' (at most one space is allowed before it).` |
| `-- ##FLAG-FILE(#Azure) azure.sql` | No `:`: `Missing ':' after '(#Azure)'` |

⚠️ This one still **fails silently**. It isn't a directive, so it's an ordinary comment: it includes nothing, raises no error, and stays in the output as a comment.

| Line | Problem |
|---|---|
| `-- ## FLAG-FILE(#Azure): azure.sql` | A space after `##`. This applies to every directive. |
