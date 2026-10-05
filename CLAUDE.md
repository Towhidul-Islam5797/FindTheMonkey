\# Find The Cheeky Monkey (working title: Monkey Rush)



Single-player kids' tap-to-find game. A monkey hides behind objects and the player taps to find it.

Unity 6000.3.6f1, URP 2D, Input System. Android build first, iOS build later.



\## How to work

\- One task at a time. Propose a plan first (files touched, what changes, done condition) and wait for approval before editing.

\- Read the relevant scripts before proposing anything. Do not assume what the code looks like.

\- Keep code simple, readable, and beginner-friendly. One clear job per script and per method.

\- Use current Unity 6 patterns only: Input System, no deprecated APIs.

\- No over-engineering. No new patterns or abstractions unless asked.

\- No unnecessary comments. Use clear names.

\- Explain what each change does and why, in plain simple language.

\- In scripts, check the latest uncommented #region (Code) #endregion block first.

\- Do not edit scenes or prefabs without asking. Describe the Inspector steps for me to do by hand.

\- Do not touch Assets/\_Recovery.



\## Project layout

\- Assets/Scripts: game code

\- Assets/Scenes: Gameplay, Menu, System

\- Assets/Settings/Scenes: the map and menu scenes in the build

\- Assets/Shop, Assets/Resources: shop prefabs

\- Assets/Audio: music, SFX, GameFeedbackManager prefab

\- Assets/Level\_Prefab, Assets/Player: level and monkey prefabs

\- Save data uses PlayerPrefs only.



\## Current state

\- Exists: coins, bananas, stars, score (LevelState), level unlocking (MapLevelManager), shop with skins, audio, vibration.

\- Partly done: countdown timer (only the 3-2-1 before a level), difficulty (levels unlock but do not get harder).

\- Missing: hint system, AdMob, daily rewards.

\- Empty placeholder scenes: WorldSelection, LevelSelection, MonkeyCollection, Rewards, Settings.



\## In scope now (Phase 2)

Harder monkey hiding in later levels, play timer, useful coins and hints, difficulty balancing, UI check on different phone sizes, costume/skin polish, audio and vibration polish, AdMob (rewarded and interstitial), release builds.



\## Out of scope

\- Moving or animated environments (Forest, Beach, Snow redesign).

\- Charity or donation features.

\- Store publishing. We give guidance only.



\## Known debt

\- Unused scripts: ArrivalLayerTrigger, LevelProgressionController, MapLevelLoader, GameplayInputBlocker, GameplayInputGuard.

\- Hard-coded SelectLevel1() to SelectLevel10() methods in MapLevelManager and MapWindowController.

\- Sound and vibration settings are not saved.

\- The Play button skips the menus and loads JungleMap.

\- A file name with many spaces: Resources/ShopWindow .prefab.

\- Debug logging is on in many scripts.

