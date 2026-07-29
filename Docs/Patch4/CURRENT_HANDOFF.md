# GameWork Patch 4.0 — Current Cross-Chat Handoff

Last updated: **2026-07-30 00:10 (+03:00)**

Repository: `timurkamurka11/skinny-to-beast-clicker`

Working branch: `patch-4.0`

Canonical long-form history: `Docs/Patch4/CHECKPOINT.md`

This file is the **latest operational continuation point**. Read it before doing more Patch 4 work.

## User workflow preference

The assistant must edit and commit Patch 4 files directly to GitHub.

The user should normally need only:

```bat
git pull origin patch-4.0
```

Do not introduce Unity installers, Unity Hub installers, PowerShell auto-install scripts, branch switching automation, stash/reset workflows or unrelated local-file synchronization unless the user explicitly requests them.

Do not ask the user to write or edit project code manually. Fix errors directly in GitHub, then tell the user to run `git pull origin patch-4.0`.

## Protected scope

Do not change:

- `MainMenuLoop.mp4`;
- menu scenes, prefabs, transitions or button logic;
- music, ambient audio or audio mixers;
- settings UI, persistence, language, vibration or notifications.

Patch 4 work must remain isolated under:

- `Assets/GameWorkPatch4/`
- `Docs/Patch4/`

## Unity environment confirmed

- Unity Editor: `6000.3.19f1`
- Project successfully opened in the real Unity Editor.
- The initial Safe Mode compilation problem was resolved.
- Safe Mode disappeared and the project opened normally.
- `Assets/Scenes/SampleScene.unity` was opened.
- The scene currently contains only `Main Camera`, `Global Light 2D` and `DontDestroyOnLoad`; it is not the full gameplay room.

## Real compilation fixes already completed

### Texture maximum size

Old direct `TextureImporter.maxTextureSize` usage was incompatible with the installed Unity version.

Fixed by using `TextureImporterPlatformSettings`.

Commit:

`b1ab5f6 fix(patch4): configure max texture size through platform settings`

### Sprite pivot and alignment

Old direct `TextureImporter.spriteAlignment` usage was incompatible with Unity `6000.3.19f1`.

Fixed through `TextureImporterSettings` plus `ReadTextureSettings` / `SetTextureSettings`.

Commit:

`61951d6 fix(patch4): apply sprite pivot through TextureImporterSettings`

The corrected local file was restored with:

```bat
git fetch origin patch-4.0 && git restore --source=origin/patch-4.0 -- Assets/GameWorkPatch4/Editor/Patch4LayerImportPostprocessor.cs
```

After this, Unity left Safe Mode and compiled successfully.

## Production Dashboard status

Dashboard path:

`Tools → GameWork → Patch 4.0 → Open Production Dashboard`

The user ran button 1, previously named:

`1. Download Adobe Sources`

It failed with:

- approved neutral master: HTTP 403;
- rigging reference: HTTP 404;
- masks: HTTP 404;
- total: `0 files downloaded, 12 failed`.

Cause: old Adobe short URLs were temporary and expired.

## Adobe dependency removed

Patch 4 no longer depends on the expired Adobe links.

### Embedded repository art source

Added:

`Assets/GameWorkPatch4/Editor/Patch4EmbeddedArtSource.cs`

It stores a compact repository-owned representation of the approved character source and expands it locally in Unity.

Commit:

`a921ea1 fix(patch4): embed approved draft art source in repository`

### Local source and mask restoration

Replaced network downloading in:

`Assets/GameWorkPatch4/Editor/Patch4AdobeMaskDownloader.cs`

The existing menu/button name is preserved for compatibility, but the command now works entirely locally. It creates:

- `FatMan_NeutralFront_Master.png` at `1024 × 1536`;
- `FatMan_Rigging_Reference.png`;
- 10 deterministic masks:
  - hair;
  - face base;
  - eyebrows;
  - nose;
  - ears;
  - neck;
  - upper clothes;
  - lower clothes;
  - hands;
  - shoes.

No Adobe request is made.

Commit:

`12e37f2 fix(patch4): replace expired Adobe URLs with repository-owned source restoration`

## Exact next action

The user has not yet pulled commits `a921ea1` and `12e37f2` into the local Unity project.

First run:

```bat
git pull origin patch-4.0
```

Wait for Unity to finish recompiling.

Then open:

`Tools → GameWork → Patch 4.0 → Open Production Dashboard`

Press:

`1. Download Adobe Sources`

Despite the old label, this now restores the character and masks locally from repository data.

Expected Console message:

```text
Patch 4 restored the neutral master, rigging reference and 10 deterministic masks from GitHub-owned data. No Adobe download is required. Run Art/Bake Draft Layer Pack next.
```

Then press:

`2. Bake Draft Layer Pack`

After the bake completes, inspect Console and the Dashboard. The next assistant should request a screenshot only if there is a new error or if the PASS/WAIT status needs interpretation.

## Do not do yet

- Do not approve `Patch4ArtReadiness.asset`.
- Do not force `productionArtApproved = true`.
- Do not activate Patch 4 in runtime.
- Do not merge `patch-4.0` into `main`.
- Do not modify protected menu/audio/settings files.

## Remaining work after the local bake

- Run draft-layer validation.
- Rebuild locked runtime assets.
- Run safety validation.
- Run compilation and Editor smoke reports.
- Run EditMode and PlayMode tests.
- Find/open the actual gameplay room containing the legacy `CharacterRigController`.
- Install Patch 4 beside the selected legacy character in rollback mode.
- Manually repaint hidden joints and final facial poses.
- Complete Sprite Skin weight painting.
- Review all ten animations in Play Mode.
- Approve readiness only after all technical and human checks pass.

## Exact prompt for a new chat

Copy and send this message in the new chat:

> Продолжаем GameWork Patch 4.0. Репозиторий `timurkamurka11/skinny-to-beast-clicker`, ветка `patch-4.0`. Сначала прочитай через GitHub файлы `Docs/Patch4/CURRENT_HANDOFF.md` и `Docs/Patch4/CHECKPOINT.md`, ничего не придумывай и продолжай строго с последней точки. Ты сам редактируешь и коммитишь файлы прямо в GitHub, а я только выполняю `git pull origin patch-4.0` и проверяю Unity. Unity `6000.3.19f1` уже установлен. Последние важные коммиты: `a921ea1` и `12e37f2`; Adobe-ссылки удалены из рабочего процесса, button 1 Dashboard теперь восстанавливает мастер и 10 масок локально. Не меняй `MainMenuLoop.mp4`, меню, музыку и настройки. Текущий следующий шаг: после моего `git pull` повторно запустить button 1 в Production Dashboard, затем button 2 Bake Draft Layer Pack.
