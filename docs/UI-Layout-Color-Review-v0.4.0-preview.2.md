# LAN Rescue UI Review – v0.4.0-preview.2

## Design direction
The interface now uses a restrained neutral canvas with one primary blue accent. Semantic colors are reserved for meaning: green = healthy/success, amber = warning/unverified, red = conflict/danger. Most navigation, cards, grids and form controls remain neutral so the application does not become visually noisy.

## Surface hierarchy

| Role | Light | Dark | Usage |
|---|---|---|---|
| App frame / base | `#E1E8F1` | `#09111E` | Outer shell and window frame |
| Content canvas | `#F0F4FA` | `#142337` | Main page work area, deliberately distinct from the shell |
| Sidebar | `#ECF1F7` | `#0F1B2B` | Navigation rail |
| Card surface | `#FFFFFF` | `#1D2D42` | Main cards, grids, panels |
| Elevated surface | `#FFFFFF` | `#273A52` | Menus, popups, elevated controls |
| Input surface | `#FFFFFF` | `#101D30` | TextBox and ComboBox |
| Primary accent | `#2459A8` | `#8DBBFF` | Focus, navigation emphasis, links |
| Primary button | `#2459A8` | `#245DB8` | Main action with white text |
| Success | `#187348` | `#5DD995` | Confirmed healthy/success state |
| Warning | `#8A570C` | `#FFD166` | Unknown, not checked, or unverified state |
| Danger | `#B42336` | `#FF8796` | Confirmed conflict or destructive/danger state |

The work canvas is intentionally one step brighter than the application frame in Light mode and one step brighter than the outer frame in Dark mode. Cards sit above the canvas, making the content boundary visible without adding decorative gradients or many hues.

## Color discipline by screen

- **Dashboard:** slate/white surfaces + blue structure; green for online/success, amber for unknown or unverified, red for IP conflict. No extra decorative accent colors.
- **Devices:** neutral table and inspector surfaces + blue focus/selection; use semantic status colors only for actual status values.
- **IP Conflicts:** neutral information panels + amber for the safety notice/unverified state; red is reserved for confirmed danger, not generic warning.
- **IP Recovery:** neutral form surfaces + blue for the workflow/action hierarchy; amber for allocation caution and red only for disabled/danger notices.
- **Diagnostics:** neutral surfaces + amber for checks that are not run or not confirmed. Avoid green until a check really passes.
- **Settings / Device Editor:** neutral surfaces + blue for focus and primary action. Status hues are not used decoratively.

## Spacing and layout standards

- Shell: 240px navigation rail; 72px topbar; 36px status footer.
- Content inset: 24px horizontal and 20px vertical.
- Base spacing rhythm: 4 / 8 / 12 / 16 / 20 / 24px. Avoid ad-hoc 3px / 7px / 9px gaps unless needed for a control template.
- Cards: 20px standard padding; 14px corner radius; 14–16px vertical separation.
- Inputs and buttons: 38px height/minimum height; normal text 12px, secondary metadata generally 10–11px.
- Device Inspector: 300px target width with a 14px separation from the grid.
- Devices toolbar uses a WrapPanel so its controls wrap rather than collide at the minimum supported window width.

## DataGrid sizing

- Data row: **46px**. A little more comfortable for scanning than the earlier 44px row while still dense enough for IT inventory.
- Header: **44px**.
- Cell horizontal padding: **10px**, centered vertically.
- Text: 12px, no wrapping, ellipsis for long values. Full values remain available in the inspector where present.
- Alternating rows use only a subtle neutral tint; hover and selected states use the primary blue family, not extra status colors.
- Row/column virtualization is enabled in the shared DataGrid style.

## Verification performed

- Both color dictionaries have matching resource keys.
- All XAML files are XML-well-formed.
- Resource references and event handlers are checked statically.
- WCAG contrast was checked for the core text, secondary text, primary button and semantic warning/danger surfaces. This is a source-level check; actual monitor rendering and Windows text rasterization still need a visual check on the target Windows machine.

## Contrast spot-check (source palette)

| Text / UI pair | Light | Dark |
|---|---:|---:|
| Primary text on content canvas | 14.34:1 | 14.61:1 |
| Secondary text on content canvas | 5.42:1 | 9.23:1 |
| Secondary text on card | 5.98:1 | 8.12:1 |
| White text on primary button | 6.84:1 | 6.30:1 |
| Warning text on warning surface | 6.37:1 | 10.47:1 |
| Danger text on danger surface | 6.85:1 | 8.68:1 |
| Border on card surface | 3.36:1 | 4.02:1 |
| Border on work canvas / elevated surface | 3.05:1 | 3.34:1 |

The outer-frame-to-canvas luminance ratio is approximately 1.12:1 in Light and 1.19:1 in Dark; the canvas-to-card ratio is approximately 1.10:1 in Light and 1.14:1 in Dark. This is a subtle structural separation rather than a competing high-contrast panel. The content text and primary component boundaries have their own stronger contrast.

## Target-device visual review checklist

1. At 1120×700, confirm Devices toolbar wraps without overlap and grid/inspector remain visible.
2. At 1440×860, check the balance of canvas whitespace, KPI cards, device grid and inspector.
3. Check 46px rows with long hostnames, IPv4 addresses and management values.
4. Confirm selected row remains distinct from hover and alternating row colors.
5. Open ComboBox, ContextMenu and Device Editor in both themes. Verify the popup and dialog surfaces remain visually separated from the content canvas.
6. Check the status footer does not crowd the page at minimum width.
