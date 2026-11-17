# `##DIRECTORY`

Includes every `.sql` file in a folder, in an order you control. You can drop files you don't want, run key files first, run dependent files last, and sort everything in between.

```sql
-- ##DIRECTORY: <path> [exclude: <entry>, ...] [sort: <pinned-first>, ..., <SORT-MARKER>, <pinned-last>, ...]
```

Only `<path>` is required. `[exclude:]` and `[sort:]` are optional, can come in either order, and can each appear once.

---

## Syntax

### The directive line

- **Exact form:** write `-- ##DIRECTORY:`. Spaces are fine before `--` and between `--` and `##`, but **not** between `##` and `DIRECTORY` or before the `:`.
- **Wrong forms fail silently:** a line like `--## DIRECTORY:` or `-- ##DIRECTORY :` is not an error. tsmake treats it as an ordinary comment, so **nothing is included**.
- **Case:** the directive name and the clause keywords ignore case (`##directory:`, `[EXCLUDE: …]`).
- **Comments:** anything after `##::` is a comment and is ignored.

### `<path>` (required)

| Form | Example | Resolves to |
|---|---|---|
| Relative | `Common\tables` | `##ROOT` + path. Paths are relative to **ROOT**, not to the file that contains the directive. |
| Rooted | `~\Common\tables` | `##ROOT` + path (an explicit way to write the same thing) |
| Parent | `..\shared\tables` | Up from `##ROOT` |
| Absolute | `D:\repos\lib\tables` | As written |
| UNC | `\\server\share\tables` | As written |

- **The folder must exist:** if it doesn't, the directive is invalid.
- **Quotes:** they're optional, even when the path contains spaces. They're **required** if the path contains `,`, `(`, `)` or a quote character. Use `"double"` or `'single'` quotes.
- **What gets included:** only the folder's **own `*.sql` files**. The extension match ignores case.
- **What's ignored:** subfolders, and files like `.txt`, `.sql.bak` or `.sqlx`.

### Entries (used by both `[exclude:]` and `[sort:]`)

- **Separators:** entries are separated by commas, and spaces around them don't matter.
- **Quotes:** quote an entry if it contains `,`, `(`, `)`, `[`, `]` or a quote character.
- **Matching:** case-insensitive, against the **file name** (`orders.sql`) or the full path.
- **Wildcards:** `*` matches zero or more characters and `?` matches exactly one. Everything else is literal, and `[abc]` sets are not supported.
- **What a wildcard is compared against:** the file name, unless the entry contains `\` or `/`. Then it's compared against the full path.
- ⚠️ **Entries that match nothing are silently ignored.** A typo won't raise an error. The file just won't be excluded or pinned.

### `[exclude: …]`

- **What it does:** removes every file that matches any of its entries.
- **It runs first:** an excluded file can **never** be pulled back in by `[sort:]`.
- **Every entry must end in `.sql`:** this applies to wildcards too, so write `old_*.sql`, not `old_*`.

### `[sort: …]` and the sort marker

A `[sort:]` list contains file entries and **at most one** sort marker. The marker has **two jobs**:

1. **It sets how files are sorted:**

   | Marker | Order |
   |---|---|
   | `ALPHA` | By name, A→Z |
   | `ALPHADESC` | By name, Z→A |
   | `TIMESTAMP` | By last-modified time, oldest first |
   | `TIMESTAMPDESC` | By last-modified time, newest first |

2. **It's a placeholder:** every file that isn't excluded and isn't pinned goes **at the marker's position**, in the marker's order.

Entries **before** the marker are pinned **first**, and entries **after** it are pinned **last**. Both groups keep the exact order you list them in:

```
[sort: settings.sql, customers.sql, ALPHA, audit_log.sql, cleanup.sql]
       '------ pinned first -----'    |    '------ pinned last -----'
                                      '-- every other file goes here, A->Z
```

- ⚠️ **A wildcard pin pulls in *all* of its matches as a group** at that position. The group is ordered by the marker, so with `ALPHADESC` it runs Z→A.
- ⚠️ **The first matching entry wins:** each file goes to the first entry that matches it. Entries **before** the marker are checked before entries **after** it. So if a file matches a pin on both sides, **it goes to the front**. This includes a wildcard before the marker swallowing a file you meant to pin last (Example 2.4).
- **No marker at all:** the listed files go first and the rest follow `ALPHA`, as if `ALPHA` were the last entry.
- **Case and quoting:** markers ignore case (`alphaDESC`). Quoting one (`'ALPHA'`) makes it a literal file name.
- **Extensions:** sort entries don't have to end in `.sql`. But only `.sql` files are ever candidates, so `settings` matches nothing. Write `settings.sql` or `settings*`.

### How sorting works

- ⚠️ **`ALPHA` uses character codes, ignoring case.** It is **not** "natural" (numeric) sorting. The order is `-` < digits < letters < `_`. So:
  - `10_index.sql` sorts before `1_create.sql`.
  - `orders.sql` sorts before `order_items.sql`.
  - `_cleanup.sql` sorts after every letter.

  Zero-pad numeric prefixes (`01_`, `02_`, `10_`). See Example 6.
- **`TIMESTAMP` and `TIMESTAMPDESC` use each file's last-modified time.** Files with the same time are ordered **A→Z by name**, even with `TIMESTAMPDESC`.
- ⚠️ **Last-modified times change** whenever a file is edited, copied or checked out. That's fine for local scripts, but it makes builds hard to repeat. Prefer name-based ordering for anything shipped.

### Defaults

| You write | You get |
|---|---|
| No clauses | Every top-level `.sql` file, `ALPHA` |
| No `[exclude:]` | Nothing excluded |
| No `[sort:]` | `ALPHA`, nothing pinned |
| `[sort:]` with no marker | Listed files first, then the rest `ALPHA` |
| `[sort: ALPHA]` on its own | Same as having no `[sort:]` |
| Everything excluded | Valid, but includes nothing (no warning) |

### Errors

A malformed directive is marked invalid, with a message:

| Directive | Problem |
|---|---|
| `-- ##DIRECTORY: no_such_folder` | `Specified ##DIRECTORY does not exist: <full path>` |
| `-- ##DIRECTORY: Reports Data (v2)` | A path containing `(` `)` `,` or quotes must be quoted |
| `-- ##DIRECTORY: Common [exlude: scratch.sql]` | `Unknown clause '[exlude:'` |
| `-- ##DIRECTORY: Common [exclude: scratch]` | `[exclude:] entries must be .sql files` |
| `-- ##DIRECTORY: Common [sort: ALPHA, TIMESTAMP]` | Only one sort marker is allowed |
| `-- ##DIRECTORY: Common [sort: ALPHA] [sort: TIMESTAMP]` | Only one `[sort: ...]` clause is allowed |
| `-- ##DIRECTORY: Common [exclude: scratch.sql,]` | Trailing comma (empty entries, `,,` and empty clauses are also errors) |

---

## Examples

Every output below comes from running the directive through tsmake. Each file is included, and processed, in the order shown.

### 1. The basics: one small folder

```
\alpha_sixes\
|-- 6a.sql
|-- 6b.sql
|-- 6c.sql
|-- 6d.sql
|-- 6e.sql
```

| # | Directive | Output | What it shows |
|---|---|---|---|
| 1.1 | `-- ##DIRECTORY: alpha_sixes` | 6a, 6b, 6c, 6d, 6e | Default: `ALPHA` |
| 1.2 | `-- ##DIRECTORY: alpha_sixes [exclude: 6e.sql]` | 6a, 6b, 6c, 6d | Exclude |
| 1.3 | `-- ##DIRECTORY: alpha_sixes [sort: ALPHADESC]` | 6e, 6d, 6c, 6b, 6a | Marker only: sort direction |
| 1.4 | `-- ##DIRECTORY: alpha_sixes [sort: 6d.sql, ALPHA]` | **6d**, 6a, 6b, 6c, 6e | Pin first |
| 1.5 | `-- ##DIRECTORY: alpha_sixes [sort: ALPHA, 6a.sql]` | 6b, 6c, 6d, 6e, **6a** | Pin last |
| 1.6 | `-- ##DIRECTORY: alpha_sixes [exclude: 6e.sql] [sort: 6d.sql, ALPHA, 6a.sql]` | **6d**, 6b, 6c, **6a** | Exclude, pin first, and pin last |
| 1.7 | `-- ##DIRECTORY: alpha_sixes [sort: 6e.sql, ALPHADESC, 6a.sql]` | **6e**, 6d, 6c, 6b, **6a** | Pins around a `DESC` placeholder |
| 1.8 | `-- ##DIRECTORY: alpha_sixes [sort: 6c.sql, 6a.sql]` | **6c**, **6a**, 6b, 6d, 6e | No marker: pins first, the rest `ALPHA` |
| 1.9 | `-- ##DIRECTORY: alpha_sixes [sort: 6a.sql, ALPHA, 6a.sql]` | **6a**, 6b, 6c, 6d, 6e | ⚠️ Pinned on both sides: goes to the **front** |
| 1.10 | `-- ##DIRECTORY: alpha_sixes [exclude: 6b.sql] [sort: 6b.sql, ALPHA]` | 6a, 6c, 6d, 6e | ⚠️ An excluded file can't be pinned back in |
| 1.11 | `-- ##DIRECTORY: alpha_sixes [exclude: *.sql]` | *(nothing)* | Valid, but empty |
| 1.12 | `--## DIRECTORY: alpha_sixes` | *(nothing)* | ⚠️ Not a directive: there's a space after `##` |

### 2. Dependency layering in one folder: tables, then functions, views and procs

```
\Common\
|-- func.format_bytes.sql
|-- func.split_string.sql
|-- notes.txt                     (ignored: not .sql)
|-- proc.create_order.sql
|-- proc.get_customer.sql
|-- proc.purge_audit_log.sql
|-- scratch.sql                   (work in progress: don't ship)
|-- table.audit_log.sql
|-- table.customers.sql
|-- table.order_items.sql
|-- table.orders.sql
|-- table.products.sql
|-- table.settings.sql
|-- view.customer_summary.sql
|-- view.open_orders.sql
|-- archive\
    |-- table.old_orders.sql      (ignored: subfolder)
```

**2.1 Default `ALPHA` order is wrong for dependencies.** Procs would be created before the tables they use, and `scratch.sql` would ship.

```sql
-- ##DIRECTORY: Common
```
```
func.format_bytes.sql, func.split_string.sql, proc.create_order.sql, proc.get_customer.sql,
proc.purge_audit_log.sql, scratch.sql, table.audit_log.sql, table.customers.sql, table.orders.sql,
table.order_items.sql, table.products.sql, table.settings.sql, view.customer_summary.sql, view.open_orders.sql
```

**2.2 The recipe.** Key tables come first, then every other table via the `ALPHA` placeholder, then functions, views and procs.

```sql
-- ##DIRECTORY: Common [exclude: scratch.sql] [sort: table.settings.sql, table.customers.sql, ALPHA, func.*.sql, view.*.sql, proc.*.sql]
```
```
 1. table.settings.sql           <-- pinned first, in listed order
 2. table.customers.sql          <--'
 3. table.audit_log.sql          <-- ALPHA placeholder: everything not excluded and not pinned
 4. table.orders.sql                 (here, that's just the remaining tables)
 5. table.order_items.sql
 6. table.products.sql
 7. func.format_bytes.sql        <-- func.*.sql  \
 8. func.split_string.sql                         |
 9. view.customer_summary.sql    <-- view.*.sql   |  pinned last, in listed order:
10. view.open_orders.sql                          |  funcs, then views, then procs.
11. proc.create_order.sql        <-- proc.*.sql   |  Each group is sorted by the marker (ALPHA).
12. proc.get_customer.sql                         |
13. proc.purge_audit_log.sql                     /
```

**2.3 The same layering, with the placeholder in a different role.** Here `table.*.sql` is pinned *before* the marker, so the placeholder holds "everything else" (functions and views), and only procs are pinned last. The output matches 2.2 for this folder. The difference is what happens to a *new* kind of file, say `type.*.sql`: it would land in the placeholder, between the tables and the procs.

```sql
-- ##DIRECTORY: Common [exclude: scratch.sql] [sort: table.settings.sql, table.customers.sql, table.*.sql, ALPHA, proc.*.sql]
```
```
table.settings.sql, table.customers.sql,                                       <-- exact pins (claimed first)
table.audit_log.sql, table.orders.sql, table.order_items.sql, table.products.sql,  <-- table.*.sql (the rest)
func.format_bytes.sql, func.split_string.sql, view.customer_summary.sql, view.open_orders.sql,  <-- ALPHA
proc.create_order.sql, proc.get_customer.sql, proc.purge_audit_log.sql         <-- proc.*.sql
```

**2.4 ⚠️ A wildcard before the marker can swallow a "pin last".** `table.*.sql` is checked first and claims `table.audit_log.sql`, so the `table.audit_log.sql` entry after the marker matches nothing.

```sql
-- ##DIRECTORY: Common [exclude: scratch.sql] [sort: table.*.sql, ALPHA, table.audit_log.sql]
```
```
table.audit_log.sql, table.customers.sql, table.orders.sql, ...   <-- audit_log is FIRST, not last
```
To fix it, pin the tables you want explicitly (as in 2.2), or give `audit_log` its own folder or directive (Example 3).

**2.5 Exclude with wildcards, plus a pin and `ALPHADESC`.**

```sql
-- ##DIRECTORY: Common [exclude: scratch.sql, proc.*.sql, view.*.sql] [sort: table.settings.sql, ALPHADESC]
```
```
table.settings.sql, table.products.sql, table.order_items.sql, table.orders.sql, table.customers.sql,
table.audit_log.sql, func.split_string.sql, func.format_bytes.sql
```

**2.6 ⚠️ A pin with no extension matches nothing, silently.** `table.settings` is not `table.settings.sql`, so nothing is pinned and you get plain `ALPHA`.

```sql
-- ##DIRECTORY: Common [exclude: scratch.sql] [sort: table.settings, ALPHA]
```
```
func.format_bytes.sql, func.split_string.sql, proc.create_order.sql, ... table.settings.sql, view.customer_summary.sql, view.open_orders.sql
```

### 3. Dependency layering across folders: one directive per layer

Splitting objects into folders often makes the ordering simpler. The directives run top to bottom, so every table is created before any proc.

```
\Schema\
|-- tables\
|   |-- audit_log.sql
|   |-- customers.sql
|   |-- order_items.sql
|   |-- orders.sql
|   |-- products.sql
|   |-- settings.sql
|-- procs\
    |-- create_order.sql
    |-- create_order.test.sql
    |-- get_customer.sql
    |-- purge_audit_log.sql
```

```sql
-- ##DIRECTORY: Schema\tables [sort: settings.sql, customers.sql, ALPHA, audit_log.sql]
-- ##DIRECTORY: Schema\procs [exclude: *.test.sql]
```
```
tables\settings.sql, tables\customers.sql,                      <-- pinned first
tables\orders.sql, tables\order_items.sql, tables\products.sql, <-- ALPHA placeholder
tables\audit_log.sql,                                           <-- pinned last
procs\create_order.sql, procs\get_customer.sql, procs\purge_audit_log.sql   <-- 2nd directive (tests excluded)
```

`~\Schema\tables` resolves to the same folder as `Schema\tables`.

### 4. Timestamps

```
\patches\                         (last modified)
|-- add_index_orders.sql          2026-03-02 10:00
|-- baseline.sql                  2026-01-05 09:00
|-- fix_settings_defaults.sql     2026-02-14 16:30
|-- rename_customer_cols.sql      2026-04-20 11:15
|-- seed_data.sql                 2026-01-06 08:00
|-- seed_lookups.sql              2026-01-06 08:00   (same time as seed_data.sql)
```

| # | Directive | Output |
|---|---|---|
| 4.1 | `-- ##DIRECTORY: patches [sort: TIMESTAMP]` | baseline, seed_data, seed_lookups, fix_settings_defaults, add_index_orders, rename_customer_cols |
| 4.2 | `-- ##DIRECTORY: patches [sort: TIMESTAMPDESC]` | rename_customer_cols, add_index_orders, fix_settings_defaults, **seed_data, seed_lookups**, baseline |
| 4.3 | `-- ##DIRECTORY: patches [sort: baseline.sql, seed_*.sql, TIMESTAMPDESC]` | **baseline**, **seed_data, seed_lookups**, rename_customer_cols, add_index_orders, fix_settings_defaults |
| 4.4 | `-- ##DIRECTORY: patches [sort: baseline.sql, TIMESTAMP, fix_*.sql]` | **baseline**, seed_data, seed_lookups, add_index_orders, rename_customer_cols, **fix_settings_defaults** |

- **4.2:** files with the same timestamp stay A→Z, even with `DESC`.
- **4.3:** the wildcard group (`seed_*.sql`) follows the marker, but with tied times it falls back to A→Z.
- ⚠️ **Editing or checking out these files changes their timestamps, and with them the build order.**

### 5. Quoting and wildcards

```
\Reports Data (v2)\
|-- r1.sql
|-- r12.sql
|-- report_daily.sql
|-- report_daily.test.sql
|-- report_monthly.sql
|-- report_monthly.test.sql
|-- scratch_1.sql
|-- scratch_old.sql
|-- totals, by region.sql
```

| # | Directive | Output |
|---|---|---|
| 5.1 | `-- ##DIRECTORY: Reports Data (v2)` | ❌ invalid: the path contains `(` `)`, so it must be quoted |
| 5.2 | `-- ##DIRECTORY: "Reports Data (v2)" [exclude: *.test.sql, scratch*.sql, "totals, by region.sql"]` | r1, r12, report_daily, report_monthly |
| 5.3 | `-- ##DIRECTORY: 'Reports Data (v2)' [exclude: r?.sql, *.test.sql]` | r12, report_daily, report_monthly, scratch_1, scratch_old, *totals, by region* |
| 5.4 | `-- ##DIRECTORY: 'Reports Data (v2)' [exclude: *.test.sql, scratch*.sql] [sort: report_*.sql, ALPHA]` | **report_daily, report_monthly**, r1, r12, *totals, by region* |

- **5.2:** `*.test.sql` and `scratch*.sql` each drop several files. The name containing a comma has to be quoted.
- **5.3:** `?` matches exactly one character, so `r?.sql` drops `r1.sql` but keeps `r12.sql`.

### 6. ⚠️ `ALPHA` is not numeric

```
\steps\                           \steps\  (zero-padded)
|-- 1_create.sql                  |-- 01_create.sql
|-- 2_load.sql                    |-- 02_load.sql
|-- 10_index.sql                  |-- 10_index.sql
|-- _cleanup.sql                  |-- 99_cleanup.sql
|-- Setup.sql
|-- setup_users.sql
```

`-- ##DIRECTORY: steps` gives these orders:

- **Left folder:** **10_index**, 1_create, 2_load, Setup, setup_users, **_cleanup**. `10` sorts before `1_`, and `_` sorts after letters.
- **Right folder:** 01_create, 02_load, 10_index, 99_cleanup, as intended.
