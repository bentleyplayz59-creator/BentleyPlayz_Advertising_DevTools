# Bentley's Advertising Dev Tools

Companion SMAPI mod for [Bentley's Advertising](https://github.com/bentleyplayz59-creator/BentleyPlayz_Advertising). It adds a developer menu for forcing advertisements and letters on demand, for screenshots and testing.

**Requires** Bentley's Advertising 1.0.1 or newer (the build that includes the Dev Tools API) and SMAPI 4.3.0 or newer.

## Installation

1. Install Bentley's Advertising first.
2. Download the Dev Tools release ZIP and extract it.
3. Copy the `BentleyPlayz.AdvertisingDevTools` folder into your SMAPI `Mods` folder next to `BentleyPlayz.Advertising`.
4. Start the game through SMAPI and load a save.

## Using it

Tap the purple **DEV** button at the top-left of the screen (or press `F8` on a keyboard) to open the menu.

1. Pick an advertiser from the list, or leave **Random advertiser** selected.
2. Press a button:

| Button | What it does |
| --- | --- |
| Show ad popup | Closes the menu and shows that advertiser's popup right away, even while you have an ad-free subscription. |
| Letter: mailbox now | Puts one of that advertiser's letters in your mailbox immediately. |
| Letter: tomorrow | Queues one of that advertiser's letters for tomorrow morning. |
| Clear ad-free | Removes any active ad-free subscription time in the current save. |

Letters prefer ones you have not received yet. The menu header shows the same "next ad" / "ad-free" status as the AD menu. The advertiser list scrolls by dragging or with the arrows.

## Configuration

`config.json` is created on first run:

| Setting | Default | Meaning |
| --- | --- | --- |
| `ShowButton` | `true` | Show the DEV button (needed on touch screens). |
| `OpenMenuKey` | `F8` | Keyboard shortcut (`None` disables it). |
| `ButtonSize` | `64` | DEV button size in UI pixels (40-96). |
| `ButtonOffsetX` / `ButtonOffsetY` | `16` / `16` | DEV button distance from the left / top edge. |

## Build

GitHub Actions builds the mod with the official Pathoschild SMAPI mod build workflow and uploads the release ZIP as a workflow artifact on every push and pull request.
