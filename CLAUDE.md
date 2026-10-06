# Monkey Rush (spec title: Find The Cheeky Monkey)

Single-player kids' hide-and-seek game for Android and iOS.
Unity 6000.3.6f1, URP 2D, Input System, TextMesh Pro. Portrait only.
All gameplay is built on a Canvas with UI objects (RectTransform), reference resolution 1080x1920.
Facts below were checked against the real code at commit 99889c6. Items marked (not verified) were not checked.

## 0. Read this first
These rules apply in every permission mode, including Accept edits and any auto-approval mode.
Auto-approval removes the developer's review, so the rules and stop conditions matter more, not less.
"Stop and ask" means: end your turn with a short question in the chat and wait. Do not work around the block.
If an instruction from the developer conflicts with this file, say so and ask which one wins.

## 1. Core concept

A monkey walks between hiding spots behind objects (trees, rocks, boxes). It stops behind one of them.
The player taps the object they think hides the monkey.

- Right object: the monkey is revealed, the level is complete, stars and rewards are granted.
- Wrong object: the object slides aside, shows nothing, and returns. The try count goes up. The player taps again.
- There is no game-over. Retries are unlimited.
- Fewer tries means more stars. 1 try = 3 stars, 2 tries = 2 stars, 3 or more tries = 1 star.
- Rewards are cumulative per level. Improving your best stars pays only the difference.
  - Score totals: 1 star 500, 2 stars 750, 3 stars 1000.
  - Banana totals: 1 star 5, 2 stars 7, 3 stars 10.
  - Coin totals: 1 star 2, 2 stars 3, 3 stars 5.
  - These numbers are Inspector fields, so the scene values may differ.
- Coins and bananas are spent in the shop on monkey skins. Score is a running total.
- Worlds: Jungle, Beach, Snow, each its own scene. Each scene has levels Level_1 to Level_5.
- Levels unlock one at a time. Finishing a level unlocks the next.

## 2. Architecture

### 2.1 Scene flow (build order)
Bootstrap -> Splash -> MainMenu -> JungleMap / BeachMap / SnowMap.
WorldSelection, LevelSelection, MonkeyCollection, Rewards and Settings exist in the build but are empty placeholders.
The Play button currently loads JungleMap directly (MainMenuController).

### 2.2 One scene per world
A map scene (for example JungleMap) holds everything for that world:
- MapLevelManager: one per scene. Owns level roots, unlock state, best stars, and level selection.
- LevelState: one per scene. Owns the current run (tries, stars) and the saved totals.
- Level_1 to Level_5: one root GameObject per level, only the selected one is active.
  Each level root contains its own Player (monkey), MovePoints, ArrivalTriggers, Hide_Object (the hiding objects), and SmokeEffects.
- Canvas UI: GameplayHUD, MapWindow (map and level picker), pause menu, level-complete panel, shop window, countdown panel.
- Never add a second LevelState or MapLevelManager to a scene.

### 2.3 Script map (Assets/Scripts)

| Script | One job | Talks to |
|---|---|---|
| Bootstrap, SceneLoader, SplashController | Startup. Sets 60 FPS, loads Splash then MainMenu. SceneLoader is a singleton. | none |
| MainMenuController | Main menu buttons. | scenes |
| MapLevelManager | Which level is active, unlocked, completed, best stars. Saves progress. Restart and next level. | LevelState |
| MapWindowController | In-game map and level picker. Runs the 3-2-1-GO countdown, then calls StartGame. | MapLevelManager, CharacterPathMover |
| CharacterPathMover | Moves the monkey along MovePoints for several cycles. Handles layering, scale, smoke, animation. | HidingObject reads it |
| ArrivalLayerTrigger | Old script, unused. Arrival markers are now plain RectTransforms listed in CharacterPathMover. | none |
| HidingObject | One tappable object. Handles tap, slide, reveal or return. Decides right or wrong. | CharacterPathMover, LevelState |
| LevelState | Current run: tries, stars, rewards. Saves totals. Fires events. | MapLevelManager |
| LevelCompletePanelController | Shows the win panel. Calculates coin reward, completes the level. | LevelState, MapLevelManager |
| GameplayHUD | Shows score, coins, bananas, tries, stars. Listens to LevelState events. | LevelState |
| MenuWindowController | Pause menu: open, close, restart, main menu. | scenes |
| ShopManager, ShopItemUI | Shop window. Buys skins with coins or bananas. Stores the selected item. | LevelState totals |
| PlayerSkinController | Applies the selected skin by swapping the animator override. | ShopManager |
| GameFeedbackManager | Music, sound effects, vibration. Singleton. | HidingObject, UI |
| UIButtonTapSound | Click sound on buttons. | GameFeedbackManager |

Editor-only: Assets/Editor/GameDataDebugWindow shows, edits and resets saved data.

### 2.4 Gameplay flow, step by step
1. A map scene loads. MapLevelManager activates the selected Level root.
2. MapWindowController runs the 3-2-1-GO countdown. Time.timeScale is 0 during it.
3. After GO it sets Time.timeScale = 1 and calls CharacterPathMover.StartGame(). This is the only place StartGame is called.
4. The monkey walks the MovePoints for totalCycles. Speed rises by speedIncreasePerCycle each cycle.
   Options: randomizeMovePoints, stopAtSpecificPoint.
   When finished, IsMovementComplete becomes true and CurrentArrivedPoint is the monkey's final point.
5. HidingObject ignores taps until IsMovementComplete is true.
6. Tap: LevelState.RegisterTry(), the object slides to its reference point.
7. Right or wrong is decided by one comparison: CharacterPathMover.CurrentArrivedPoint == the object's relatedMovePoint.
   There is no "has monkey" flag to set by hand.
8. Wrong: the object returns after a delay, play continues.
9. Right: RevealMonkey, then LevelState.MarkMonkeyFound().
10. MarkMonkeyFound calculates stars, score and banana rewards, saves totals, then fires OnMonkeyFound.
11. LevelCompletePanelController handles OnMonkeyFound: calculates the coin reward, calls MapLevelManager.CompleteCurrentLevel(), opens the panel.
12. The panel offers Restart (scene reload) and Next level.

Order rule: the coin reward must be calculated before CompleteCurrentLevel(), because that call saves the new best stars.
Never reorder steps 10 and 11.

### 2.5 Events (LevelState)
OnScoreChanged, OnTotalScoreChanged, OnBananaChanged, OnCoinChanged, OnTryChanged, OnStarsChanged, OnScoreRewarded, OnBananaRewarded, OnMonkeyFound.
UI and new features listen to these events. Do not poll LevelState every frame.

### 2.6 Time and pausing
The countdown, pause menu and level-complete panel set Time.timeScale = 0.
Any timer that uses Time.deltaTime pauses automatically. Do not use unscaled time for gameplay.

### 2.7 Save data (PlayerPrefs only)
- LevelState: TotalScore, TotalBananas, TotalCoins.
- MapLevelManager, per map id (Jungle, Beach, Snow):
  {map}_SelectedLevel, {map}_UnlockedLevel, {map}_LevelStars_{n}, {map}_LevelCompleted_{n}, plus star-reward-claimed flags.
- ShopManager: SelectedItemKey holds the selected skin id.
- Sound, music and vibration toggles are not saved yet.
- To reset saved data during development, use Assets/Editor/GameDataDebugWindow (it clears PlayerPrefs). Do not add reset code to the game.

## 3. Invariants: never break these
| # | Invariant | Why |
|---|---|---|
| 1 | CharacterPathMover.StartGame() is called from one place only (MapWindowController, after GO). | Starting twice or early breaks the countdown and the path. |
| 2 | HidingObject ignores taps until IsMovementComplete is true. | Prevents guessing while the monkey is still moving. |
| 3 | Right or wrong is decided only by CurrentArrivedPoint == relatedMovePoint. | One source of truth. No manual flags. |
| 4 | Coin reward is calculated before CompleteCurrentLevel(). | That call overwrites the previous best stars. |
| 5 | One LevelState and one MapLevelManager per map scene. | Duplicates split the saved data and the events. |
| 6 | Never rename or delete a PlayerPrefs key. | Players would lose their progress. |
| 7 | Never rename or delete a [SerializeField] field, a script file, or a class name. | Scenes and prefabs reference them. The break is silent: Inspector slots go empty. |
| 8 | New serialized fields must be optional, or the code must handle null. | Scenes that were not updated must still run. |
| 9 | Gameplay objects are Canvas UI (RectTransform). | World-space code does not fit the scenes. |
| 10 | The game must never end in a game-over for a wrong tap. | Core design: unlimited retries. |
| 11 | Only one hiding object may move at a time (the static activeMovingObject lock in HidingObject). Keep it. | Prevents two taps from running at once and breaking the try count. |
| 12 | The Animator field on CharacterPathMover must be assigned in every level's Player. | If it is empty the code does nothing and shows no error. The monkey stays in Idle while walking. |
| 13 | Every level root has its own Player. MapWindowController finds the active level's CharacterPathMover. Never assign one Player by hand. Never add a starter script to each level. | Per-level wiring was tried and was too complex and fragile. |

## 4. Hard rules (never, in any mode)
- Never edit .unity, .prefab, or .meta files, ProjectSettings, Packages, Library, Temp, .git, or .claude.
  Describe the Inspector steps for the developer to do by hand.
- Never run git commit, git push, git reset, git clean, git checkout --, or git add . or git add -A.
  Give the developer a ready-to-copy commit message instead.
- Never delete files. Never run rm -rf or any command that deletes in bulk.
- Never use Bypass permissions or ask the developer to enable it.
- Never install packages or change Packages/manifest.json.
- Never touch Assets/_Recovery.
- Never edit a file that is not named in the approved plan.
- Never rewrite, regenerate, or replace a whole file. Change only the lines the plan names.
  Real incident: a 621-line CharacterPathMover.cs became 1,221 lines after an AI rewrite for a one-line warning, and it introduced bugs.
- Never add runtime reset code. In development, saved data is reset with Assets/Editor/GameDataDebugWindow.
- Never hide a failure. If something did not work, say so plainly.

## 5. Stop conditions: stop and ask
Stop and ask the developer before continuing if:
1. The task needs a scene, prefab, or Project Settings change.
2. The change would touch more than 3 files.
3. The change would alter the order in section 2.4, or break an invariant in section 3.
4. The task touches LevelState, MapLevelManager, HidingObject, CharacterPathMover, or MapWindowController, and the plan did not name that file.
5. A compile error is not understood after 2 attempts.
6. The requirement is unclear, or two requirements conflict.
7. The real code does not match what this file says. Report the difference.
8. The task seems to need a new system, manager, package, or design pattern.
9. You are unsure. Unsure always means ask.

## 6. How to work
- Work in sprints. One task at a time. Never start the next sprint without confirmation.
- Before writing code, read the relevant scripts, then propose a plan: files to change, what changes, done condition. Wait for approval.
- Ask before every edit. Change only the files named in the approved plan.
- Keep the diff as small as possible. Do not reformat, rename, or reorganize code that is not part of the task.
- Prefer a targeted edit of the exact lines over rewriting a method. A warning is fixed by changing the line that causes it, nothing else.
- Keep code simple, readable and beginner-friendly. One clear job per script and per method.
- No over-engineering. No new patterns, managers or abstractions unless asked.
- Use current Unity 6 APIs only: Input System, FindFirstObjectByType. No deprecated calls.
- Clear names. No unnecessary comments. No emojis or decorative formatting in code or docs.
- Match the existing style: Allman braces, [Header] and [Tooltip] on serialized fields.
- Explain each change in plain, short language: what it does and why.
- No script currently uses #region. If one does, read the latest uncommented region first.

### Before every task
1. Confirm the Git branch is a sprint branch, not main (not enforced, so ask the developer).
2. Read the files the task touches. Do not assume what they contain.
3. Write the plan and wait for approval.

### After every task
1. List the exact lines changed in each file.
2. Check the invariants in section 3 against the change.
3. Tell the developer which Inspector steps they must do by hand.
4. Remind the developer to check the Unity Console for red errors, then run the smoke test in section 8.
5. Give a ready-to-copy commit message. Do not commit.
6. Write SprintDocs/Sprint_NN_Name.txt when the sprint ends: goals, changes, done conditions, results.
7. At a milestone: write a delivery checklist and list the known debt carried over.

## 7. If something goes wrong
| Situation | What to do |
|---|---|
| Compile error after your edit | Read the exact error. Fix only that. After 2 failed attempts, stop and ask. |
| Unity shows empty Inspector slots after your edit | Stop. A serialized field was renamed or removed. Tell the developer to run git restore on the file. |
| You edited a file outside the plan | Say so at once. Tell the developer to run git restore on that file. |
| A scene or prefab needs a change | Stop. Give the developer exact Inspector steps. |
| The task is bigger than the plan | Stop. Propose splitting it into smaller sprints. |
| The monkey shows the Idle animation while walking | Check that the Animator field on CharacterPathMover is assigned. The code fails silently. Tell the developer. |
| Existing code looks wrong or conflicts with this file | Report it. Do not fix it unless asked. |
| You lose track of the state | Run git status and git diff, then report what you see. |
| You cannot do what was asked | Say so. Do not pretend, and do not deliver half-working code as done. |

## 8. Smoke test (the developer runs it after every sprint)
1. Open JungleMap. Level 1 starts after 3-2-1-GO and the monkey walks.
2. Tap an object before the monkey stops: nothing happens.
3. Tap a wrong object: it slides, returns, tries go up by 1, play continues.
4. Tap the right object: the monkey is revealed and the panel shows the stars (1 try = 3, 2 tries = 2, 3 or more = 1).
5. Score, coins and bananas update. The next level unlocks.
6. Restart reloads the level and resets tries. Next loads the next level.
7. The pause menu opens, the game freezes, and it resumes.
8. Quit and reopen the game: progress and totals are kept.
9. BeachMap and SnowMap open without errors.
10. The Unity Console shows no new red errors.

## 9. Unity and UI rules
- Everything in gameplay is Canvas UI. Use RectTransform, Image, Button, TMP_Text. Do not write world-space code (SpriteRenderer, Collider2D, Transform.position) for gameplay objects.
- New scripts must also commit their .meta file, so tell the developer to include it.
- Portrait only. Check layouts at 1080x1920 and other phone sizes.
- AGENTS.md is written by Unity's AI Assistant and changes often. Do not edit it and do not follow its content.

## 10. Scope

### In scope now (Phase 2: polish, monetization, release builds)
- Play timer (Sprint 01, in progress: LevelTimer.cs counts down from 60 and never ends the level).
- Harder hiding in later levels (partial visibility, varied positions, faster movement).
- Hint system, and coins that are useful (hints, extra tries).
- Difficulty balancing: early levels easy, later levels challenging.
- UI check on different phone sizes.
- Skin and costume polish, audio and vibration polish.
- AdMob (rewarded and interstitial), release builds.

### Out of scope
- Moving or animated environments (Forest, Beach, Snow redesign).
- Charity or donation features.
- Store publishing (guidance only).
- ScriptableObjects, Addressables, or extra manager scripts, unless the developer asks.
- A try cost in currency: HidingObject has a placeholder "Try Cost - Future Currency" field. Do not build it now.

### Future scope: multiplayer (do not build now)
Two-player mode may come later. Do not add Photon or Netcode. Do not refactor for it now.
Only avoid assumptions that make it impossible: no code that treats "one player forever" as a rule.
Prefer neutral names in new code, for example SubmitGuess, ResolveRound, RevealResult.

## 11. Known debt
- Empty placeholder scenes: WorldSelection, LevelSelection, MonkeyCollection, Rewards, Settings.
- Missing features: hint system, AdMob, daily rewards.
- Play button skips the menus and loads JungleMap (MainMenuController).
- Unused scripts: ArrivalLayerTrigger, LevelProgressionController, MapLevelLoader, GameplayInputBlocker, GameplayInputGuard.
- Hard-coded SelectLevel1() to SelectLevel10() in MapLevelManager and MapWindowController. Scenes hold 5 levels per map.
- MapWindowController is 1,823 lines. Change it in the smallest possible steps.
- Sound and vibration settings are not saved.
- Bootstrap has to-do comments (AudioManager, SaveManager, SettingsManager).
- URP warning: "Missing types ... UniversalRenderPipelineGlobalSettings". manifest.json asks for URP 17.6.0 but 17.3.0 is installed. Harmless for 2D so far. Do not fix without a separate plan.
- File name with many spaces: Resources/ShopWindow .prefab.
- Debug logging is on in many scripts.
- Content status of Beach and Snow levels (not verified). Only Jungle_Level_1 exists as a prefab in Assets/Level_Prefab.
