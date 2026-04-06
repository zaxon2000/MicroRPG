# Asset Creation Strategy - MicroRPG

This document outlines the strategy for generating and managing assets for the MicroRPG project, ensuring visual consistency and technical compatibility with the existing Unity setup.

## 1. Visual Style & Aesthetic Goals
- **Primary Style**: 16-bit/32-bit Pixel Art.
- **Perspective**: Top-down / Orthographic (consistent with existing Tilemaps).
- **Consistency**: All new assets must match the clean, vibrant, and tile-based look of the current `RPGpack` and `sokoban` spritesheets.
- **Palette**: Use vibrant colors with distinct outlines to ensure readability against the stone/dirt tilemaps.

## 2. Asset Generation Workflow

### 2.1 2D Sprites (Characters, Items, Props)
- **Tool**: `mcp_unity_Unity_AssetGeneration_GenerateAsset` with `command: "GenerateSprite"`.
- **Prompting**: Use specific keywords: `"16-bit pixel art, top-down RPG style, [Description], clean lines, transparent background, stylized"`.
- **Post-Processing**: Always call `RemoveSpriteBackground` after generation to ensure perfect alpha transparency.
- **Refinement**: Use `EditSpriteWithPrompt` if the initial generation needs color adjustments or detail changes to match the existing set.

### 2.2 UI Elements
- **Icons**: Generate 64x64 or 128x128 pixel art icons for items (Hammer, Axe, Pan, Coin).
- **Frames/Buttons**: Use `GenerateSprite` with prompts for UI elements that complement the existing `PlayerUICanvas`.

### 2.3 Audio & SFX
- **Tool**: `mcp_unity_Unity_AssetGeneration_GenerateAsset` with `command: "GenerateSound"`.
- **Categories**:
    - **Actions**: "Mining/Rock hitting", "Axe swing", "Eating/Using item".
    - **Feedback**: "Coin pickup chime", "Quest completion fanfare", "Health low warning".

### 2.4 3D Meshes (Optional)
- While the project is primarily 2D, any 3D elements (e.g., for specialized VFX or background elements) should be generated using `GenerateMesh` with a low-poly or voxel-like prompt to maintain stylistic harmony.

## 3. Implementation & Validation

### 3.1 Organization
- **Sprites**: `Assets/Sprites/Generated/`
- **Prefabs**: `Assets/Prefabs/Generated/`
- **Audio**: `Assets/Audio/Generated/`
- **Materials**: `Assets/Materials/Generated/`

### 3.2 Verification
1. **Visual Check**: Place the new asset in the current scene near existing assets (e.g., near the Player or Boulder).
2. **Capture**: Use `mcp_unity_Unity_SceneView_Capture2DScene` to capture the area and verify that the asset doesn't "clash" with the background or existing sprites.
3. **Functional Check**: Ensure the asset is correctly assigned to its intended Prefab or ScriptableObject (e.g., `GroundItem` script).

## 4. Immediate Asset Needs (Based on Scene Analysis)
- **Enemy Variations**: New 16-bit pixel art enemy sprites (e.g., Slime, Skeleton).
- **Interactive Props**: Variation for "Chest", "QuestGiver", and "GroundItem" visuals.
- **VFX Sprites**: Pixel art particles for "HitEffect" and "Coin pickup".
