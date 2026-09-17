# TASK-002 r002 — Requirement Preview

Status: Preview Ready  
Mode: visual_iteration  
Task: TASK-002 / Version 1  

## Revision and artifact

- Preview: `requirement-preview.png`
- Real runtime source capture: `runtime-capture.png`
- Source: existing Windows build `Builds/Windows/StairsCrowd.exe`

## What changed

- Replaced the previous generated poster with a four-state deterministic layout based on the real game window.
- Preserved the actual project background, floating stair geometry, title art, settings button and existing home action.
- Added exact UI text and layout overlays for:
  1. Home: “冒险模式” with “每日挑战” directly below.
  2. Current-month calendar: “2026年9月”, 7-column date grid, mint completion mark and today highlight.
  3. Gameplay: top “每日挑战” HUD with exact “3:00”.
  4. Completion: mint check and only “返回主页”.

## Decisions preserved

- Current project camera/background and low-saturation teal / warm-ivory / coral / mint palette.
- No new character, fox, brand, reward, coin, streak, decorative slogan or level icon.
- Calendar is current-month only; completed state is mint check; completion action is only “返回主页”.
- No generated text or generated date grid was used.

## Assumptions introduced

- `2026年9月17日` is used as a static visual example, matching the Task’s prior preview date context.
- The runtime source capture shows the existing home/game frame; the four panels are a visual layout preview, not a claim that the new UI is already integrated.

## Does not define

- Final IMGUI pixel values, font import settings, safe-area constants or production assets.
- The daily level’s board layout or difficulty tuning.
- Runtime persistence keys, timeout implementation or input routing.

## Visual QA notes

- Exact Chinese strings are drawn deterministically and were checked at original resolution and the rendered preview scale.
- The previous r001 generated background/characters/brand are not reused.
- The actual runtime capture is kept beside the preview as the visual source evidence.
