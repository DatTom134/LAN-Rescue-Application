# LAN Rescue v0.4.1 – Fresh Fluent

## Design update
- Applied the Fresh Fluent – Bright Blue / Sky Glass palette to Light and Dark themes.
- Updated application frame, sidebar, content canvas, cards, elevated popups and input surfaces.
- Set primary accent to vivid blue in Light (`#2563EB`) and sky blue in Dark (`#60A5FA`).
- Used dark navy text on the Dark-theme primary button to retain readable contrast on the brighter blue surface.
- Reserved green, amber and rose-red for semantic status only: Online/Success, Warning/Unverified, and IP Conflict/Danger.
- Added a separate stronger control border token for buttons, TextBox, ComboBox and popup boundaries while keeping card borders subtle.
- Preserved the existing dynamic theme switch and per-user theme preference.

## Compatibility and retained behavior
- WPF / .NET 10; no new UI dependency.
- No changes to Domain, Application, Infrastructure, SQLite schema, database path or device CRUD behavior.
- Existing Light/Dark theme preference continues to work.

## Validation
- Both theme dictionaries use matching resource keys.
- XAML is checked for well-formedness, missing resource references and missing event-handler references.
- WCAG contrast spot-checks are documented in `Fresh-Fluent-Color-System-v0.4.1.md`.
- A real WPF build and visual review must still be run on Windows with the .NET 10 SDK; this package was not built with the Windows desktop runtime in the packaging environment.

## v0.4.1 UI correction — Device Profile and shared controls
- Fixed TextBox content-host vertical layout to prevent clipped glyphs.
- Added theme-aware ScrollBar styling for Light/Dark.
- Enforced readable primary-action foreground in Device Profile.
- Improved Device Profile form spacing, Firmware version full-width layout, and fixed footer divider.
- Added project-wide UI quality gates to prevent recurrence across future screens.

### UI refinement 2 – input scrolling and theme control colors

- Single-line TextBox vertical scrolling is disabled by default; multiline Notes keeps its explicit automatic vertical scrolling.
- TextBox template now binds scroll settings from the TextBox itself, preventing the template from silently overriding per-control behavior.
- Reduced vertical input padding to protect text baselines in fixed-height fields.
- Light primary actions now use a soft sky-blue fill and deep-blue text instead of a dark saturated blue fill.
- Dark controls use brighter sky-blue accents, richer blue input borders/surfaces, and more vivid hover/selected states.
