# Encounter Forge Standalone

[![Patreon](https://img.shields.io/badge/Patreon-F96854?style=for-the-badge&logo=patreon&logoColor=white)](https://patreon.com/Elemor)
[![Version](https://img.shields.io/badge/Version-1.1.0-success?style=for-the-badge)](https://github.com/ElemorSeru/EncounterForgeStandalone/releases/latest)
<img alt="GitHub Downloads (all assets, latest release)" src="https://img.shields.io/github/downloads/ElemorSeru/EncounterForgeStandalone/latest/total">

**Encounter Forge Standalone** is a Windows desktop application that builds brand-new, balanced NPCs on demand, sized to a party, not a generic XP table or a basic CR lookup.

Instead of pulling a stat block from a standard monster set and hoping the CR math works out, Encounter Forge assembles a creature from a chassis, traits, actions, and (optionally) spells, then tunes its HP, AC, and damage so the resulting encounter actually lands at the difficulty you asked for.

> **Status:** Actively developed. Windows 10/11 required.

## Why Encounter Forge is different

Most CR generators (and most homebrew stat blocks) work backwards from the Dungeon Master's Guide CR/XP tables: pick a CR, look up its expected HP and damage, and build a creature around those numbers. The problem is that those tables assume a "standard" party, and most parties are not standard. A CR 5 monster can be a joke or a TPK depending on who is sitting at the table.

Encounter Forge flips that process around:

1. It estimates actual damage output and durability based on class averages using your party's size and level.
2. It uses your chosen difficulty (Easy/Medium/Hard/Deadly) to compute an encounter envelope, the total HP and DPR (Damage Per Round) the enemy side needs, as a whole, to make that fight play out the way that difficulty implies.
3. It splits that envelope across however many enemies you asked for, or amplifies it for a Solo Boss.
4. Then it picks a CR, but only to decide what kind of creature this is: its chassis, traits, actions, spell tier, and AC baseline. The CR does not set the creature's final HP or damage.
5. Finally, it tunes the assembled creature's HP, AC, and damage output to hit its slice of the envelope exactly, so the finished NPC actually performs the way the difficulty setting promised.

In short: a "Hard" fight with two generated enemies and a "Hard" fight with four generated enemies should both feel like a Hard fight for your specific table, just split up differently. That is not something CR-table-driven generators do well, because they are matching a number on a chart, not your party.

## Not an AI generator, and works fully offline

Encounter Forge contains no AI, no language model, and no network calls of any kind. Every creature is assembled procedurally from local JSON content pools (chassis archetypes, traits, actions, spells, names) using deterministic math you can read in plain code. There is nothing to configure, no API key, no tokens, no internet connection required, and no data ever leaves your machine. Generation is instant.

## Features

- **Procedural creature assembly** - chassis (combat role), traits (defensive/offensive/passive/movement/senses/reactions/legendary), actions (melee/ranged/special), and spells are drawn from content pools based on theme, CR tier, and chassis.
- **Encounter envelope balancing** - the whole encounter is sized to the party and difficulty first, then split across enemies. See The Math below.
- **Solo Boss mode** - a single creature gets HP x1.5, DPR x1.3, AC +2, three guaranteed actions, and a draw from the legendary trait pool (legendary actions, lair actions, legendary resistance) so it can stand up to a full party alone.
- **Live pre-generation readout** - as you adjust player count, level, enemy count, and difficulty, the app shows the target CR, projected enemy and party DPR and HP, rounds to defeat/threaten, and a color-coded outlook (Easy/Manageable/Risky/Dangerous) before you generate anything.
- **Combat Intensity Calibration** - an optional setting that shifts how aggressively enemies are sized across all difficulties, on a scale of -3 to +3. The live preview chart in the calibration dialog shows how each step affects drain time across Easy/Medium/Hard/Deadly for a reference party. Intended for playtesting; leave at 0 for the baseline math.
- **Post-generation results summary** - after generation, see each creature's actual HP, AC, DPR, and CR, plus the group's combined stats versus the party and the same outlook label, now based on real numbers.
- **PDF export** - export the generated stat block as a formatted PDF ready to print or reference at the table.
- **10 themes/creature types** - beast, undead, aberration, humanoid, elemental, fey, fiend, dragon, construct, monstrosity, or "any".
- **6 chassis archetypes** - brute, lurker, skirmisher, controller, artillery, leader; each with its own stat spread, size progression, guaranteed skills, and action/spell affinities.
- **Remember last used** - the app remembers your last settings, with a one-click reset from the settings panel.

## The Math

### 1. Party estimate

A party estimate is a pair of values, DPR and HP:

- Derived from player count and average level using per-class DPR curves and a flat HP-per-level model.

### 2. The encounter envelope

Each difficulty maps to a target "rounds to defeat the enemy" and "rounds the enemy needs to threaten the party":

| Difficulty | Rounds to Defeat | Rounds to Threaten | Outlook    |
|------------|------------------|--------------------|------------|
| Easy       | 2                | 6                  | Easy       |
| Medium     | 3                | 5.4                | Manageable |
| Hard       | 4                | 4.4                | Risky      |
| Deadly     | 5                | 3.5                | Dangerous  |

```
groupHP  = party.dpr * roundsToDefeat
groupDPR = (party.hp / roundsToThreaten) * economyFactor
```

`economyFactor` is a small multiplier (`1 + 0.04 * (enemyCount - 1)`, capped at `1.2`) that accounts for more attackers contributing slightly more total damage per round. Both totals are then divided by the enemy count to get each creature's HP/DPR target. Solo Boss further multiplies its single creature's targets by HP x1.5 / DPR x1.3.

### 3. Picking a flavor CR

The per-creature HP/DPR target is matched against a CR baseline table to find the closest CR. That CR drives only the chassis stat tier, trait/action/spell availability, and AC/save-DC baseline. It is not the creature's final HP or DPR.

### 4. Calibration pass

Every generated creature is then tuned to hit its envelope target exactly:

- **HP** is set directly to the target.
- **AC** is nudged by up to +/-2 based on how far the HP/DPR targets deviate from the chassis baseline.
- **Damage** is tuned in two passes: first an action may be swapped for a same-tier alternative closer to the target DPR, then any remaining gap is closed with a flat damage bonus spread across the creature's actions.

The result: the pre-generation readout and the post-generation results should track closely, regardless of party size, level, enemy count, or Solo Boss status.

### 5. Combat Intensity Calibration (optional)

An app setting (`combatIntensity`, integer -3 to +3, default 0) scales the `roundsToThreaten` target before the envelope is computed:

```
adjustedRoundsToThreaten = roundsToThreaten / (1 + offset * 0.12)
```

Positive offset means shorter drain time, so enemies are built to hit harder. Negative means longer drain and softer enemies. `roundsToDefeat` is never changed.

## Installation

1. Download the latest release from the [GitHub releases page](https://github.com/ElemorSeru/EncounterForgeStandalone/releases/latest).
2. Extract and run `EncounterForgeStandalone.exe`. No installer required.

> .NET 8 Desktop Runtime is required. If not already installed, Windows will prompt you to download it.

## Quick Start

1. Set your party size and average level.
2. Choose a difficulty, creature theme, and number of enemies.
3. Watch the live readout to see the projected outlook before you commit.
4. Click **Generate**. The results summary shows the generated creature(s) with final stats.
5. Export to PDF if you want a printable stat block for the table.

## Roadmap

- Additional themes, chassis, and content pool expansions.

Have a feature request or found a balance edge case? Open an issue on GitHub or drop a note on Patreon.

## About

Built and maintained by [Elemor](https://patreon.com/Elemor).

If you find this useful and want to support continued development, the Patreon link above is the best way to do that.

Bug reports and feature requests are welcome via the Issues tab on GitHub.
