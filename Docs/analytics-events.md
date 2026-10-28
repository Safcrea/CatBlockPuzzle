# Cat Block Puzzle analytics events

All events go through `CatAnalytics` (`Assets/_GameData/System/Scripts/CatBlockPuzzle/Services/CatAnalytics.cs`)
into AVN Analytics. The SDK is started by `CatAnalytics.Initialize()` before the first scene loads.

Level events share these params: `level_number`, `level_id`, `difficulty`, `chapter`, `attempt`
(lifetime attempt count for that level number, saved on device), `is_loop`.
Level browsing/preview (testing buttons) is never tracked.

## From the SDK (automatic)

| Event | Use |
| --- | --- |
| `first_open` | Installs |
| `session_start` / `session_end` | Retention, session length, device/country mix |

## Level funnel (the core difficulty data)

| Event | When | Extra params |
| --- | --- | --- |
| `level_start` | A level loads | `previous_outcome` (none/win/fail/quit/skip), `time_limit_seconds`, `hearts`, `cells`, `pieces`, `can_fail`, `coins`, `hints_in_stock`, `freezes_in_stock` |
| `level_complete` | Board filled | `stars`, `first_clear`, `coins_awarded`, `best_combo`, `play_seconds`, `wall_seconds`, `time_remaining`, `invalid_drops`, `hints_used`, `freezes_used`, `placements` |
| `level_fail` | Timer ran out or hearts gone | `reason` (timeout/out_of_hearts), `cells_filled`, `cells_total`, `fill_percent`, plus the progress params above |
| `level_quit` | Left an unfinished level | `reason` (restart/level_switch/menu), `fill_percent`, plus the progress params |
| `level_skip` | Skip button | `state` (in_progress/after_win/after_fail) |
| `invalid_drop` | Cat dropped where it does not fit | `invalid_drops`, `hearts_left` (-1 = no hearts level) |

An attempt that ends with the app being killed has a `level_start` and no end event.

## Learning and difficulty teaching

| Event | When |
| --- | --- |
| `tutorial_begin` / `tutorial_complete` | Level 1 hands-on tutorial (`tutorial_seconds`) |
| `mechanic_intro_shown` | First time a mechanic card shows (`mechanic`: Padlocks, KeyCats, ColourZones, SpareCats, Hearts, Shelf) |

## Power-ups and economy

| Event | When | Params |
| --- | --- | --- |
| `powerup_used` | Hint or freeze spent | `kind`, `stock_after`, `seconds_into_attempt`, `invalid_drops` |
| `powerup_prompt_shown` | Tried to use one with none left | `kind`, `coins`, `price`, `quantity`, `can_afford` |
| `powerup_prompt_closed` | Popup closed | `kind`, `purchased` |
| `powerup_purchase` | Bought with coins | `kind`, `quantity`, `price`, `coins_after` |
| `earn_virtual_currency` | Coins gained | `value`, `source` (level_win, ad_double_coins, daily_reward), `balance_after` |
| `spend_virtual_currency` | Coins spent | `value`, `item_name`, `balance_after` |

## Ads

| Event | When | Params |
| --- | --- | --- |
| `double_coins_offered` | Win screen shows the 2x button | `coins` |
| `ad_rewarded_request` | Ad requested | `placement` |
| `ad_rewarded_result` | Ad finished | `placement`, `outcome` (rewarded/unavailable/cancelled), `seconds` |
| `double_coins_claimed` | Reward paid | `coins` |

## Menus and engagement

| Event | When | Params |
| --- | --- | --- |
| `screen_view` | Page changes | `screen_name` (home, level_select, shop, daily_reward, gameplay, level_complete, level_fail, no_internet), `previous_screen`, `seconds_on_previous` |
| `home_button_tap` | Main menu button tapped | `button` (play, continue, levels, shop, daily_reward, collection, rooms, settings, quit) |
| `menu_open` | Pause or settings opened | `menu`, `from_pause` |
| `menu_close` | Pause or settings left | `menu`, `action` (pause: resume/restart/home/settings; settings: close), `seconds_open` |
| `setting_changed` | Toggle used | `setting` (music/sfx/haptics/reduced_motion), `enabled` |
| `daily_reward_open` | Daily reward page opened | `automatic`, `claimable` |
| `daily_reward_claim` | Claimed | `day`, `coins`, `hints`, `freezes` |
| `shop_offer_tap` | Shop item tapped | `product_id` |
| `coming_soon_shown` | Locked feature tapped | `feature` (Rooms, Collection) |

Bools arrive as 0/1.
