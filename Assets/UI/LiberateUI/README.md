# LiberateUI

A flexible Unity UI Toolkit framework with responsive themes and multiple art styles.

## Features

- **3 Art Styles**: CelShadedBoldOutline, DarkHandDrawnFantasy, PixelArtRetro
- **Responsive Layouts**: Automatic Portrait/Landscape adaptation
- **6 Runtime Themes**: Every art style × resolution combination
- **Design Token System**: Consistent sizing, spacing, and typography
- **Modular Architecture**: Easy to customize and extend

## Quick Start

1. Add the `ThemeManager` prefab (located in `Prefabs/Themes/`) to your scene
2. Assign your `UIDocument` to the ThemeManager component
3. The theme system will automatically apply based on screen orientation

## Theme System Architecture

### How Themes Work

The theme system uses a **two-dimensional approach**:

1. **Resolution** (Portrait/Landscape) - Controls layout and component positioning
2. **Art Style** (3 variants) - Controls visual assets (backgrounds, icons, frames)

This creates **6 total themes** configured in `Prefabs/Themes/ThemeManager.prefab`:

- `Landscape--CelShadedBoldOutline`
- `Landscape--DarkHandDrawnFantasy`
- `Landscape--PixelArtRetro`
- `Portrait--CelShadedBoldOutline`
- `Portrait--DarkHandDrawnFantasy`
- `Portrait--PixelArtRetro`

The `ThemeManager` component automatically switches between Portrait/Landscape based on screen aspect ratio, and allows users to select their preferred art style.

### Style Hierarchy

#### UI/Base/ - Foundation Layer

Contains core design system:

- **Common.uss**: Design tokens (CSS variables) for sizes, spacing, fonts, and utility classes
  - Font size tokens (`--font-size-small`, `--font-size-medium`, etc.)
  - Button size tokens (`--button-height-small`, `--button-min-width-medium`, etc.)
  - Spacing tokens (`--spacing-xs` through `--spacing-xl`)
  - Border radius tokens
  - Utility classes (alignment, positioning, borders)

- **Common-{ArtStyle}.uss**: Art-specific base colors and styles

- **ThemeStyles/Portrait/** and **ThemeStyles/Landscape/**: Resolution-specific token overrides

All components reference these tokens for consistency.

#### UI/Themes/ - Theme Composition Layer

ThemeStyleSheet (.tss) files that compose the complete themes:

**Art Style Aggregators** (e.g., `CelShadedBoldOutline.tss`):
```css
@import url("../Base/Common.uss");
@import url("../Base/Common-CelShadedBoldOutline.uss");
@import url("../Components/Button/.../ActionButton-CelShadedBoldOutline.uss");
/* Imports all art-specific USS files across components */
```

**Resolution Aggregators** (e.g., `RuntimeTheme-Landscape.tss`):
```css
@import url("../Base/ThemeStyles/Landscape/Common-Landscape.uss");
@import url("../Components/Button/.../ActionButton-Landscape.uss");
/* Imports all resolution-specific USS files across components */
```

**Combined Themes** (e.g., `RuntimeTheme-Landscape-CelShadedBoldOutline.tss`):
```css
@import url("unity-theme://default");
@import url("CelShadedBoldOutline.tss");
@import url("RuntimeTheme-Landscape.tss");
```

The ThemeManager switches between these combined `.tss` files at runtime.

## Project Structure

```
LiberateUI/
├── Prefabs/
│   └── Themes/
│       └── ThemeManager.prefab        # Main theme controller
├── Resources/
│   └── LiberateUI/
│       ├── CelShadedBoldOutline/      # Art style assets
│       ├── DarkHandDrawnFantasy/
│       ├── PixelArtRetro/
│       └── Fonts/                      # Typography
├── Scenes/
│   └── MainMenu.unity                  # Demo scene
├── Scripts/
│   ├── Controllers/                    # Game logic controllers
│   ├── Events/                         # Event system
│   ├── Themes/
│   │   └── ThemeManager.cs            # Theme switching logic
│   ├── UIViews/                        # Screen controllers
│   │   ├── UIView.cs                  # Base class for all screens
│   │   ├── UIManager.cs               # Screen routing
│   │   ├── InventoryView.cs
│   │   └── SettingsView.cs
│   └── Utilities/
│       └── MediaQuery.cs              # Screen size detection
└── UI/
    ├── Base/                           # Design tokens & utilities
    ├── Themes/                         # ThemeStyleSheet (.tss) files
    ├── Components/                     # Reusable UI components
    ├── Inventory/                      # Inventory UI
    ├── MainMenu/                       # Main menu screens
    ├── Settings/                       # Settings UI
    └── CharacterPreview/               # Character preview UI
```

## Customization

### Adding a New Art Style

1. Create asset folder in `Resources/LiberateUI/YourStyle/`
2. Add art-specific USS files: `UI/Base/Common-YourStyle.uss`
3. Create component art files: `UI/ComponentName/ThemeStyles/YourStyle/ComponentName-YourStyle.uss`
4. Create aggregator: `UI/Themes/YourStyle.tss` importing all art USS files
5. Create combined themes: `RuntimeTheme-Portrait-YourStyle.tss` and `RuntimeTheme-Landscape-YourStyle.tss`
6. Add theme entries to ThemeManager prefab

### Modifying Design Tokens

Edit `UI/Base/Common.uss` to change sizes, spacing, or typography globally. All components reference these tokens.

### Creating Custom Components

Follow the three-layer pattern:
1. Create `ComponentName.uss` with base styles using design tokens
2. Add resolution-specific USS in `ThemeStyles/Portrait/` and `ThemeStyles/Landscape/`
3. Add art-specific USS in `ThemeStyles/{ArtStyle}/`
4. Import all files into appropriate theme aggregators

## API Reference

### ThemeManager

**Public Methods:**
- `ApplyTheme(string themeName)` - Manually switch to a specific theme

**Events (via ThemeEvents static class):**
- `ThemeChanged` - Fired when theme switches
- `ArtStyleChanged` - Fired when art style changes

**Events (via MediaQueryEvents static class):**
- `AspectRatioUpdated` - Fired when screen orientation changes

### UIManager

Manages overlay screens (Settings, Inventory, etc.)

**Public Methods:**
- `ToggleOverlayView(string overlayName)` - Show/hide a specific overlay

### UIView

Base class for creating new UI screens.

**Virtual Methods:**
- `Initialize()` - Setup UI elements and callbacks
- `Show()` - Display the screen
- `Hide()` - Hide the screen
- `Dispose()` - Cleanup when destroyed

## Requirements

- Unity 6.0 or newer

## Support & Community

Join our Discord server for support, feature requests, and to request new UI styles:

**Discord**: https://discord.gg/RVJxtkA7BS

- Request new art styles
- Report bugs
- Ask questions
- Share your implementations
- Get update notifications

## License Notes

This asset uses the following fonts under SIL Open Font License 1.1:
- Germania One
- Bangers
- Pixelify Sans

See `Third-Party Notices.txt` for full license details.
