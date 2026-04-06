# Asset Creation Strategy - MicroRPG

This document outlines the strategy for generating and managing assets for the MicroRPG project, specifically adhering to the **PixelArtRetro** aesthetic to ensure visual consistency and technical compatibility.

## 1. Visual Style: PixelArtRetro
All new assets must strictly follow the **PixelArtRetro** style found in the `Assets/UI/LiberateUI/Resources/LiberateUI/PixelArtRetro` folder.

- **Aesthetic**: Classic 16-bit/32-bit "Retro" pixel art.
- **Color Palette**: Use the vibrant yet slightly muted "retro" palette consistent with the `PixelArtRetro` sample assets (e.g., the Deep Blue/Teal/Sage tones).
- **Outlines**: Thick, clean pixel-perfect outlines (often dark or black) to define shapes clearly.
- **Perspective**: Top-down or "2.5D" isometric-lite (consistent with the character and item previews).
- **Detailing**: High-contrast shading with distinct pixel clusters; avoid blurry gradients.

## 2. Asset Generation Workflow

### 2.1 2D Sprites & Spritesheets (Characters, Items, Props)
- **Tool**: `mcp_unity_Unity_AssetGeneration_GenerateAsset` with `command: "GenerateSprite"` or `"GenerateSpritesheet"`.
- **Prompting**: Use the prefix: `"PixelArtRetro style, 16-bit high-quality pixel art, [Description], clean thick outlines, vibrant retro colors, transparent background, stylized"`.
- **Specifics for Spritesheets**: When generating animations (walking, attacking), ensure the grid alignment matches the existing `RPGpack` or `sokoban` sheets if they are being replaced or augmented.

### 2.2 Tilesheets & Environments
- **Tool**: `GenerateSpritesheet` or `GenerateImage` (for large background tiles).
- **Prompting**: `"PixelArtRetro style, seamless floor/wall tile, top-down RPG, [Material: Stone/Dirt/Grass], 32x32 pixel grid consistency"`.
- **Note**: Ensure tiles are designed for a grid-based system (e.g., 32x32 or 64x64 pixels per unit).

### 2.3 UI Elements (Icons, Buttons, Frames)
- **Tool**: `GenerateSprite`.
- **Icons**: 64x64 or 128x128 for inventory items (match the resolution of `PixelArtRetro/Items`).
- **Frames/Buttons**: Follow the "heavy border" look of the `PixelArtRetro/Frames` and `PixelArtRetro/Buttons` folders.
- **Prompting**: `"PixelArtRetro UI style, [Item/Button Name], 16-bit pixel art icon, thick borders, retro aesthetic"`.

### 2.4 Audio & SFX
- **Tool**: `GenerateSound`.
- **Style**: "8-bit/16-bit Chiptune" or "Retro Arcade" sounds to match the visual era.
- **Categories**:
    - **Actions**: "8-bit sword slash", "Classic retro mining tink", "Pixel-collecting chime".
    - **Feedback**: "Retro quest complete fanfare", "Old-school health low beep".

## 3. Implementation & Validation

### 3.1 Organization
- **Sprites**: `Assets/Sprites/Generated/PixelArtRetro/`
- **Prefabs**: `Assets/Prefabs/Generated/PixelArtRetro/`
- **Audio**: `Assets/Audio/Generated/Retro/`
- **UI**: `Assets/Custom UI/PixelArtRetro/`

### 3.2 Verification
1. **Side-by-Side Check**: Place the new asset in a scene next to a `PixelArtRetro` sample asset (e.g., `Starlight_Dagger`).
2. **Capture**: Use `mcp_unity_Unity_SceneView_Capture2DScene` to verify that the lighting and contrast match the existing environment.
3. **Integration**: Update `UIDocument` files in `Custom UI` to point to the new PixelArtRetro-styled icons and textures.

## 4. Priority Assets (PixelArtRetro Style)
- **Enemy Overhaul**: Redesign Slime, Skeleton, and other enemies in the Retro style.
- **UI Refresh**: Update the `PlayerUICanvas` and `QuestLogUI` with Retro-styled buttons and frames.
- **Item Icons**: Generate high-quality retro icons for all `GroundItem` types (Hammer, Axe, Pan).
