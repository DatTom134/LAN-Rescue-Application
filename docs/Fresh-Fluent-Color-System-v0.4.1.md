# Fresh Fluent – Color System & Contrast Notes (v0.4.1)

## Design direction
Fresh Fluent – Bright Blue / Sky Glass uses a bright, clean Light theme and a luminous navy Dark theme. The visual hierarchy stays consistent: app frame → sidebar/content canvas → cards and grids → elevated popups/inputs. Blue indicates primary interaction. Green, amber and rose-red are reserved for meaningful device/network states.

## Core palette

| Role | Light | Dark |
|---|---|---|
| App frame | `#E8F1FC` | `#0F1B2D` |
| Sidebar | `#EDF4FC` | `#14243A` |
| Content canvas | `#F5F9FF` | `#192B43` |
| Card | `#FFFFFF` | `#203650` |
| Elevated surface | `#FFFFFF` | `#263F5B` |
| Surface glass | `#EDF4FC` | `#1B3048` |
| Input | `#FFFFFF` | `#14243A` |
| Primary text | `#17324F` | `#F4F8FF` |
| Secondary text | `#536B86` | `#C4D5E8` |
| Border | `#D4E1F0` | `#3A5674` |
| Control border | `#7892AF` | `#6B86A4` |
| Primary accent/button | `#2563EB` | `#60A5FA` |
| Button hover | `#1D4ED8` | `#93C5FD` |
| Primary button text | `#FFFFFF` | `#10243B` |
| Selected surface | `#DCEBFF` | `#264E78` |

## Semantic colors

| Meaning | Light | Dark |
|---|---|---|
| Online / Success | `#15803D` | `#4ADE80` |
| Offline | `#64748B` | `#A8B7C9` |
| Warning / Unverified | `#A16207` | `#FBBF24` |
| IP Conflict / Danger | `#B91C1C` | `#FDA4AF` |
| Information | `#2563EB` | `#60A5FA` |

The offline color is neutral by design. The informational state reuses the primary accent instead of introducing a fifth decorative hue. Both themes expose `OfflineBrush` for consistent use in device-state templates. Use semantic colors in badges, icons, or small status accents rather than filling entire DataGrid rows.

## Contrast spot-check
Ratios below are calculated from the final source palette using the WCAG relative-luminance formula. Normal text target is 4.5:1.

| Text / background pair | Contrast |
|---|---:|
| Light – primary text / content | 12.38:1 |
| Light – secondary text / content | 5.21:1 |
| Light – white text / primary button | 5.17:1 |
| Light – white text / primary button hover | 6.70:1 |
| Dark – primary text / card | 11.57:1 |
| Dark – secondary text / card | 8.23:1 |
| Dark – navy text / primary button | 6.18:1 |
| Dark – navy text / primary button hover | 8.71:1 |
| Light – warning text / warning surface | 6.38:1 |
| Light – danger text / danger surface | 7.30:1 |
| Dark – warning text / warning surface | 10.61:1 |
| Dark – danger text / danger surface | 10.10:1 |
| Light – success text / success surface | 4.76:1 |
| Dark – success text / success surface | 7.32:1 |

All listed text pairs exceed 4.5:1. These are source-level color calculations; they do not replace visual inspection at Windows display scaling, high contrast settings, or across translucent native backdrops.

## Control boundaries
The general border remains subtle to separate cards without adding visual noise. Interactive controls use a distinct `ControlBorderBrush` so TextBox, ComboBox, buttons and popup edges remain identifiable on both bright and dark surfaces. Focus uses the primary accent and a 2px border. Disabled controls use secondary text/surface tokens without reducing the whole control to an unreadable opacity.

## Color discipline by screen
- Dashboard: Blue structure; Green for online/success; Amber for unknown/unverified; Red for confirmed conflict.
- Devices: neutral rows and panels; Blue for selection/focus; status colors only in status badges/icons.
- IP Conflicts: Amber for unverified/safety context; Red only for confirmed conflict.
- IP Recovery: Blue for workflow; Amber for allocation caution; Red for destructive or confirmed danger.
- Diagnostics: Blue for test structure; Amber until a check is actually verified; Green only after a passing result.
- Settings and Device Editor: neutral surfaces and Blue focus/action.

## Preserved application behavior
No C# behavior, database schema, SQLite path, repository, device record, or CRUD action is changed by this palette update. Light/Dark theme switching continues to use the existing dynamic resource dictionaries and persisted per-user preference.

## Validation boundary
This release is statically checked for XML well-formedness, matching Light/Dark resource keys, XAML resource references and event-handler references. A real `dotnet build` and runtime visual review still need to be performed on Windows with the .NET 10 SDK.
