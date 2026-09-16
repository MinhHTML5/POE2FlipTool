# POE2FlipTool

Windows Forms (.NET 8, `net8.0-windows`) bot that reads Path of Exile 1/2 currency-exchange prices off the
game screen with a homemade template OCR, computes flip profit, writes prices to a Google Sheet, logs them
to daily CSVs, and shows everything in a grid. One solution, one project, no tests project.

## Build and run

```bash
dotnet build POE2FlipTool.csproj
```

- The exe must run from its own output folder: every path is relative (`data/`, `history/`, `cache/`).
- `data/**` is copied to the output folder (see csproj). `data/google_service.json` is the service-account
  key and is git-ignored; never commit it or paste its contents anywhere.
- On start a dialog asks POE2 (Yes) or POE1 (No); this picks `data/config/<poe1|poe2>/GeneralConfig.json`
  (spreadsheet id + tab name) and the `history/<poe>` and `cache/<poe>` folders.
- Known issue: the poe1 config points at tab `MinhFlipPOE2`, which does not exist in the spreadsheet
  (the tab is `AutoFlipPOE1`). POE1 mode shows a warning and an empty grid until that is fixed.
- Hotkeys are raw-input global hooks: Ctrl+N start/stop scan, Ctrl+0 single test OCR read.
  The scan auto-stops if the focused process is not `PathOfExile`.

## Architecture (who does what)

| File | Role |
|---|---|
| `Main.cs` / `Main.Designer.cs` | UI shell: categories, price grid, rate boxes, league dropdown, chart launch, toggles. |
| `Modules/PricingChecker.cs` | The scan script. Enqueues mouse/keyboard/OCR steps as `ICommand`s executed on a 10 ms timer. |
| `Modules/PriceBoard.cs` | In-memory model: items from the sheet, exchange rates, latest known prices, profits. |
| `Modules/ProfitCalculator.cs` | Port of the sheet formulas (see below). `RouteProfit` = per div + per 1M gold. |
| `Modules/PriceHistoryWriter.cs` | Append/read `history/<poe>/prices_YYYY-MM-DD.csv`. Columns matched by header name. |
| `Modules/GoogleSheetUpdater.cs` | Sheets API v4 via service account. `UpdateCell` is fire-and-forget and skips the `"~"` sentinel. |
| `Modules/LeagueService.cs` | League list from `pathofexile.com/api/trade[2]/data/leagues`, fallback to exchange digest. |
| `Modules/ExchangeVolumeService.cs` | GGG hourly currency-exchange digest, cached, at most one call per hour. |
| `Modules/ItemNameResolver.cs` | Metadata id <-> display name, cached; poe.ninja first, RePoE dump second. |
| `Modules/PoeHttp.cs` | Shared `HttpClient` (User-Agent required by GGG), reference currency ids, cache dir. |
| `Charts/` | `PriceChartForm` + `DayChartControl`: GDI+ day chart per item (no chart library). |
| `Utilities/` | OCR (template match against `data/ocrSample/*.png`), raw input hook, screen coords. |
| `OCRDebug.*` | Card shown per OCR read in the "OCR History" panel. Dismiss only; no error saving. |

Data flow of one scan: `PricingChecker.Start` reloads `A1:B` from the sheet into `PriceBoard`, enqueues
clicks + `ScreenShotAndRecord` per price, and after an item's last price runs `FinishReading`:
profit -> CSV append -> `PriceBoard.Apply` (merge, keep old values for unread fields) -> grid row refresh.

## Google Sheet layout (tab AutoFlipPOE2, same spreadsheet for both games)

- `B2` div->ex rate, `B3` div->chaos rate, `B4/B5/B6` gold fee per exalt/chaos/div.
- Column A: item names. A cell containing `!!` is config/header/excluded. A cell containing `~` is a
  category header, name = text between the tildes (`!! ~ FRAGMENT ~ !!`; `RITUAL~` without a space is fine).
- Column B: gold cost per item. Sheet row number is used directly as the write row.
- Prices: D sell-for-div, E buy-with-ex, G buy-with-chaos, J buy-with-div, K sell-for-ex, M sell-for-chaos
  (`PriceFields.SheetColumn`). Profit per 1M gold: F/H/L/N. Helper columns R..AG hold the intermediate
  math; `ProfitCalculator` comments reference those letters. Profit per div = T/X/AB/AF.
- Prices are written as formulas `=left/right` (as OCR read them); manual grid/rate edits write plain numbers.
- Reads use `UNFORMATTED_VALUE` so `1,000.00` comes back as a number.

## OCR / scan conventions

- Screen positions are ratios of the game window (`PointF` constants in `PricingChecker`). The exchange
  window must be alone and centered.
- OCR returns `TradeRatio(left, right)`; a "sell" price is `right/left` (reverse=true), a "buy" price is
  `left/right`. Unreadable (no `:`/non-numeric/zero on either side) -> `null`. Contract for a null read:
  the store callback is not called (board keeps the previous value), the sheet cell is not written,
  the CSV cell is blank (or no row at all if nothing was read), and "Last read" does not move.
  Exception: in `FinishReading`, if one side of a currency pair was read this run and the other was not,
  the missing side borrows the read value (`ItemReading.BorrowMissingPairValues`) and that value is
  written to its sheet cell as a plain number.
- `ActionCommand` marks itself done *before* running so a throwing step is skipped rather than retried
  forever. Keep it that way.
- Delay constants in `PricingChecker` are tuned by hand by the owner; don't "normalise" them.

## External APIs (all public, no OAuth)

- Leagues: `https://www.pathofexile.com/api/trade/data/leagues` (PoE1, filter `realm == "pc"`),
  `.../api/trade2/data/leagues` (PoE2, `realm == "poe2"`). First entry = current challenge league.
  The legacy `/api/leagues` ignores `realm=poe2`; `api.pathofexile.com/league` needs OAuth. Don't use them.
- Volume: `https://web.poecdn.com/api/currency-exchange[/poe2]/<unix hour>`; only closed hours have data,
  ~2.4 MB per hour for PoE2, every league in one response. Ids are metadata paths
  (`Metadata/Items/Currency/CurrencyModValues` = Divine). Divine/Exalted/Chaos ids are identical in both games.
- Names: poe.ninja `/{poe1|poe2}/api/economy/exchange/current/overview?league=&type=` (type is required;
  valid lists are in `ItemNameResolver`). It has no metadata ids and its icon stems are ambiguous across
  tiers, so it resolves only a few dozen ids; RePoE `https://repoe-fork.github.io[/poe2]/base_items.min.json`
  resolves the rest. Both are cached in `cache/<poe>/`.
- Always send a User-Agent (`PoeHttp`). Never poll GGG faster than once an hour.

## Editing gotchas

- The owner edits the form in the Visual Studio designer between sessions and VS rewrites
  `Main.Designer.cs` (control order, positions, trailing spaces in `// ` comment lines, line endings).
  Re-read the designer before touching it, anchor edits on control names rather than surrounding
  comments, and never re-add controls the owner removed.
- Most source files are UTF-8 with BOM and CRLF; `.gitignore` is CRLF. Preserve what a file already uses.
- Large heredocs through the Bash tool have failed on quoting; write scripts/files with the Write tool.
- `Main` methods `RefreshPriceGrid`, `RefreshPriceGridRow`, `OnRatesChanged`, `GetEnabledCategories`,
  `ShouldCheckExalt/Chaos`, `SelectedLeague` are called from `PricingChecker`; keep them public.
- Grid sorting is done by rebuilding rows from `PriceBoard.Items` in order (`ItemsInGridOrder`), not by
  DataGridView's own sort, so "Reset sorting" can restore sheet order. Null profits always sort last.

## Verifying changes without the game

- Logic: a throwaway console project that `<Compile Include>`s the needed `Modules/*.cs` and
  `DataModel/*.cs` files (they have no WinForms dependency) and prints results. Sheet-derived expected
  values for Orb of Chance row 12: F=1.12, H=4.14, L=-394.06; per div T=0.0966, X=0.1696, AB=-0.9707.
- UI: launch the built exe, answer the POE dialog with SendKeys (`FindWindow(null,"POE2?")` then
  `SetForegroundWindow`), move the window to (0,0) so clicks aren't clamped, and capture with
  `PrintWindow(..., 2)`; `CopyFromScreen` grabs the game if it is fullscreen on top.
- Never leave test data in `history/` or `cache/` under the real output folder; seed, capture, delete.
- Nothing in this repo is committed automatically; the owner commits.
