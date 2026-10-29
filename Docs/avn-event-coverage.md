# AVN game event coverage

The standard remains version 5. Existing standard event names, states and closed enums remain intact. Game-specific families below add detail without changing those enums. Original lowercase GameAnalytics events still run for existing dashboards.

| Family | Coverage |
| --- | --- |
| `DAILY_PUZZLE_ANALYSIS` | Started, Completed, Failed, Restarted, Left and Choice, including recovered process kills and abandoned results. Separate attempt counters and UTC puzzle date keep these out of campaign funnels. |
| `LEVEL_ANALYSIS` extras | Started includes mechanics, spares, blocked tiles, drop limit and original launch source. Results include stars/best stars, movement counters, mistakes, best combo, first move timing and power-up counts. Restart/leave also include current counters. |
| `GAMEPLAY_ANALYSIS` | Each completed drag outcome; hint target found/not found and inventory/free-fail-streak source; move tip shown. No event per drag frame. |
| `PROGRESSION_ANALYSIS` | Chapter unlocked, chapter star gate blocked and endless mode entered. |
| `TUTORIAL_PROGRESS_ANALYSIS` | Each first-level tutorial step entered, joined to the attempt. Standard tutorial terminal events still identify the final/interrupted step. |
| `PURCHASE_ANALYSIS` | Shop and gameplay power-up/bundle purchase success, insufficient coins, invalid offer, full inventory and unavailable; includes item, price, quantity, balance and shortfall. |
| `NAVIGATION_ANALYSIS` | Level selection and paging, offline Retry/Okay, dismissals, notification toggle, and distinct result Next/Home/Retry/Skip/Double Reward actions. |
| `AD_REQUEST_ANALYSIS` | All requests, provider acceptance and terminal results, linked by request ID. Simulated rewards explicitly marked and never counted as real ad impressions. |

All new families use the same SDK sender and attempt join keys as the standard. Active attempts/results carry `GAME_MODE`; Daily Puzzle events also carry `PUZZLE_DATE`. Core mode/date survive checkpoint recovery. Campaign attempt counter keys retain their old format.

`MOVES_START`/`MOVES_LEFT` remain mistake hearts. Drop-limit levels instead use `DROP_LIMIT`, `DROPS_USED`, `DROPS_LEFT` and `DROPS_OVER_LIMIT`. This is a star threshold, not a hard failure limit. Non-drop-limit levels omit those fields. An unmade first move is omitted rather than sent as a negative duration.

The attempt clock checks the gameplay timer and modal gates in addition to the SDK background/ad gates. It counts normal play and in-game invalid-drop return motion, but excludes Pause, Settings, power-up purchases, tutorials, hidden gameplay and meta screens. Popup roots are observed for deactivation, and closes are deduplicated. Explicit user dismissal reports Dismissed; navigation-driven deactivation reports System.

Ad provider rejection no longer produces Show followed by NotReady. Synchronous callbacks are held until the provider returns. A timeout releases the pending request; late callbacks cannot grant a second reward. The installed wrapper API exposes acceptance, not an impression/on-screen callback: standard Show means an accepted provider call until such a callback is available. Real provider code requires `USE_AVNADS_PLUGIN`; this change does not install or enable an absent ad SDK. The default simulation remains available and is labelled in request telemetry.

Real-money IAP/subscriptions, revive, interstitial and app-open events remain inapplicable because the game does not implement those features. Disabled controls do not generate synthetic taps. Killed-attempt recovery retains the last core checkpoint; movement result extras are available for normal finishes/restarts/leaves, not reconstructed after process death.

## Manual checks for the game owner

- Win/fail/retry/leave both campaign and Daily Puzzle; verify distinct families, counters and matching attempt IDs. Background/force-close/relaunch a Daily Puzzle and check recovered mode/date.
- Pause, open Settings and a power-up purchase popup, then resume; verify those intervals do not inflate attempt SECONDS and each popup has one close.
- Place/reposition/return/misdrop/cancel cats; finish a drop-limit level and compare result counters, stars and combo with the game.
- Use a hint with/without a target; fail repeatedly until a free hint is offered. Confirm source/outcome and inventory stays unchanged for the free hint.
- Exercise tutorial steps, chapter gates/unlocks and endless entry; select levels and page through the level list.
- Buy power-ups/bundles with enough/insufficient coins and full inventory; check item, result and shortfall. Try offline Retry/Okay.
- Request a double reward in simulation; check SIMULATED=1 with no real Show/Closed. With the actual ads plugin enabled, check accepted, unavailable, cancelled, exception, missing-callback timeout and late-callback paths on a device.

Runtime and server delivery verification are left to the game owner; authoring validation is compilation and static payload review.
