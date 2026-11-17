# Tokens: an introduction

Tokens are placeholders in your `.sql` files. When tsmake runs a build (or a documentation run), it swaps each token for a value that you supply at the command line. It's simple string replacement: handy for version numbers, copyright lines, project URLs and anything else that changes between builds but shouldn't be hard-coded.

```sql
-- {{##COPYRIGHT}}
DECLARE @CurrentVersion varchar(20) = N'{{##VERSION}}';
```

```powershell
Invoke-TsmBuild -Tokens "##COPYRIGHT: 2026 - OverAchiever Productions, LLC." -Version "7.1.2";
```

```sql
-- 2026 - OverAchiever Productions, LLC.
DECLARE @CurrentVersion varchar(20) = N'7.1.2';
```

Tokens have two parts:

1. **Tokens**: the `{{##NAME}}` markers in your `.sql` files. These are what tsmake looks for.
2. **Token definitions**: the `NAME: value` pairs you pass in when you run tsmake. These say what each token is replaced with.

A token is replaced only when a definition with the same name exists.

---

## 1. Tokens in `.sql` files

A token is written as `{{##NAME}}`:

- **Fixed parts:** `{{##` and `}}` are literal, with no whitespace anywhere inside the token.
- **`NAME`:** upper-case letters (`A`-`Z`) and numbers (`0`-`9`). Dashes are allowed one at a time, and only **between** letters or numbers, as in `PROJECT-SITE`.
- **Case:** matching is **case-sensitive**. `{{##copyright}}` is not a token.

### Matches

| In your `.sql` file | Token name |
|---|---|
| `{{##COPYRIGHT}}` | `COPYRIGHT` |
| `{{##VERSION}}` | `VERSION` |
| `{{##PROJECT-SITE}}` | `PROJECT-SITE` |
| `{{##V2}}` | `V2` |

### Not tokens

None of these are replaced. They're left exactly as written.

| In your `.sql` file | Why it's not a token |
|---|---|
| `{{##copyright}}` | Not upper-case. |
| `{{##Copyright}}` | Not *entirely* upper-case. |
| `{{COPYRIGHT}}` | Missing the `##`. |
| `{{#COPYRIGHT}}` | Only one `#`. |
| `##{{COPYRIGHT}}` | The old syntax, with `##` outside the braces. |
| `{##COPYRIGHT}` | Single braces. |
| `{{##COPYRIGHT}` | Missing a closing brace. |
| `{{ ##COPYRIGHT}}` | Whitespace before the `##`. |
| `{{## COPYRIGHT}}` | Whitespace after the `##`. |
| `{{##COPYRIGHT }}` | Whitespace before the `}}`. |
| `{{##COPY RIGHT}}` | Whitespace in the name. |
| `{{##COPY_RIGHT}}` | Underscores aren't allowed. |
| `{{##PROJECT--SITE}}` | Two dashes in a row. |
| `{{##-COPYRIGHT}}`, `{{##COPYRIGHT-}}` | Dashes must be *between* letters or numbers. |
| `{{##}}` | No name. |
| `{COPYRIGHT}`, `{copyright}`, `{SYSTEM}` | Not tokens. This is the single-brace style S4 uses for its own runtime placeholders, and tsmake never touches it, even if you define a token with the same name. |

### Where tokens are replaced

- **Anywhere in the output:** code, strings and comments, in every file included in the build.
- **Inside strings, apostrophes in the value are doubled** so the string stays valid T-SQL. With a value of `Lots of fixin' and stuff`:

  | In your `.sql` file | In the build output |
  |---|---|
  | `PRINT N'{{##SUMMARY}}';` | `PRINT N'Lots of fixin'' and stuff';` |
  | `-- {{##SUMMARY}}` | `-- Lots of fixin' and stuff` |

  The same goes for `]` inside `[bracketed identifiers]` and `"` inside `"quoted identifiers"`.
- **Not in tsmake directives.** Directive lines never reach the output, so a line like `-- ##FILE: {{##VERSION}}.sql` is reported as a syntax error. Comment directives (`-- ##COMMENT:` and `-- ##::`) are the exception, because they're only notes.

---

## 2. Token definitions

Token definitions are passed in when you run tsmake, as `NAME: value` strings through `-Tokens`:

```powershell
Invoke-TsmBuild -Tokens "##COPYRIGHT: 2026 - OverAchiever Productions, LLC.", "##PROJECT-SITE: https://www.totalsql.com/sql-server-solutions/tsmake";
```

`-Version` and `-Summary` are shortcuts for two built-in tokens: `-Version` defines `{{##VERSION}}` and `-Summary` defines `{{##SUMMARY}}`.

### The rules

- **Name:** everything before the **first** `:`.
  - The `##` prefix is optional but recommended: `copyright: 1996` works too.
  - Same character rules as tokens: letters, numbers, and single dashes between them.
  - **Not case-sensitive:** names are stored upper-case. So `"##copyright: 1996"` defines `##COPYRIGHT` and replaces every `{{##COPYRIGHT}}` in your files.
- **Value:** everything after the first `:`. Values can contain colons, so URLs are fine.
  - **Trimmed:** leading and trailing whitespace is removed, so `"##copyright:   1996"` is replaced with exactly `1996`.
  - **Can be empty:** the token is then replaced with nothing.
  - **No line breaks:** CR and LF characters aren't allowed.
- **Duplicates:** if a name is defined more than once, the **last** definition wins. `-Version` and `-Summary` override `##VERSION` or `##SUMMARY` passed through `-Tokens`.

### Valid definitions

| `-Tokens` value | Defines | Replacement text | Notes |
|---|---|---|---|
| `"##COPYRIGHT: 1996"` | `##COPYRIGHT` | `1996` | |
| `"##copyright: 1996"` | `##COPYRIGHT` | `1996` | Names aren't case-sensitive. |
| `"##Copyright:1996"` | `##COPYRIGHT` | `1996` | No space needed after the `:`. |
| `"##copyright:   1996   "` | `##COPYRIGHT` | `1996` | Values are trimmed. |
| `"copyright: 1996"` | `##COPYRIGHT` | `1996` | The `##` is optional. |
| `"  ##copyright  :  1996"` | `##COPYRIGHT` | `1996` | Whitespace around the name is ignored. |
| `"##PROJECT-SITE: https://www.totalsql.com/tsmake"` | `##PROJECT-SITE` | `https://www.totalsql.com/tsmake` | Split at the *first* `:`. |
| `"##RATIO: 3:1 (or 6:2)"` | `##RATIO` | `3:1 (or 6:2)` | |
| `"##SUMMARY: Lots of fixin' and stuff"` | `##SUMMARY` | `Lots of fixin' and stuff` | Apostrophes are doubled when replaced inside a string. |
| `"##EMPTY:"` | `##EMPTY` | *(nothing)* | |
| `"##BLANK:     "` | `##BLANK` | *(nothing)* | Whitespace only is trimmed to nothing. |

### Invalid definitions

An invalid definition stops the build before any files are processed. The error shows the definition you passed and the reason:

```text
Invalid Token: [##COPY_RIGHT: 1996]. Invalid token name '##COPY_RIGHT': names may only contain letters (A-Z) and numbers (0-9) - with single dashes between them (e.g., ##PROJECT-SITE) - and an optional ## prefix.
```

| `-Tokens` value | Problem |
|---|---|
| `"##COPY_RIGHT: 1996"` | Underscores aren't allowed. |
| `"##COPY RIGHT: 1996"` | Whitespace in the name. |
| `"##PROJECT--SITE: x"` | Two dashes in a row. |
| `"##-COPYRIGHT: x"`, `"##COPYRIGHT-: x"` | Dashes must be *between* letters or numbers. |
| `"#COPYRIGHT: 1996"` | Only one `#`. |
| `"{{##COPYRIGHT}}: 1996"` | That's the syntax for a token in a `.sql` file, not a name. |
| `"##COPYRIGHT 1996"` | No `:`. Definitions must use `name: value`. |
| `": 1996"` | No name. |
| ``"##COPYRIGHT: line one`r`nline two"`` | Line breaks (CR or LF) aren't allowed in values. |

---

## Tokens without a definition are left as-is (for now)

> [!NOTE]
> If a token in your `.sql` files has **no definition**, it's left exactly as written: no error and no warning, it just isn't replaced. Other tokens on the same line are still replaced. A future version of tsmake will change this, with options for what to do about unmatched tokens.

With only `##VERSION: 7.1.2` defined:

| In your `.sql` file | In the build output |
|---|---|
| `SELECT '{{##VERSION}}', '{{##BUILD-DATE}}';` | `SELECT '7.1.2', '{{##BUILD-DATE}}';` |
