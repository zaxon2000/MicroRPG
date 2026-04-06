# ActionButton Component - Design Token System

## Overview
The ActionButton has been refactored to use a scalable design token system that automatically adapts to different screen sizes via Portrait/Landscape themes.

## How to Use

### Basic Usage
The ActionButton supports two modifier types that can be combined:

#### Size Modifiers
- `action-button--small` - Compact size for tight spaces
- `action-button--medium` - Standard size (default)
- `action-button--large` - Large size for emphasis

#### Importance Modifiers
- `action-button--primary` - Default action (brown text)
- `action-button--secondary` - Less important action (muted appearance)
- `action-button--destructive` - Warning/danger action (red text)

### Example Usage in UXML

```xml
<!-- Medium Primary Button (Default) -->
<ui:Instance template="ActionButton" class="action-button--medium action-button--primary" />

<!-- Small Secondary Button -->
<ui:Instance template="ActionButton" class="action-button--small action-button--secondary" />

<!-- Large Destructive Button -->
<ui:Instance template="ActionButton" class="action-button--large action-button--destructive" />
```

## Responsive Scaling

The button automatically scales based on the active theme:

### Portrait/Mobile Theme
- **Small**: 32px height, 20px icon
- **Medium**: 44px height, 26px icon
- **Large**: 56px height, 36px icon

### Landscape/Desktop Theme
- **Small**: 44px height, 28px icon
- **Medium**: 60px height, 36px icon
- **Large**: 80px height, 52px icon

### Default (Fallback)
- **Small**: 40px height, 24px icon
- **Medium**: 56px height, 32px icon
- **Large**: 72px height, 48px icon

## Architecture

### Design Tokens Location
- **Base tokens**: `/Base/Common.uss` (default values)
- **Portrait overrides**: `/Base/ThemeStyles/Portrait/Common-Portrait.uss`
- **Landscape overrides**: `/Base/ThemeStyles/Landscape/Common-Landscape.uss`

### Component Structure
```
Components/Button/
├── ActionButton.uss              # Common styles (--common classes)
├── ActionButton.uxml             # Component template
├── ThemeStyles/
│   ├── Portrait/
│   │   └── ActionButton-Portrait.uss   # Portrait layout (--theme classes)
│   └── Landscape/
│       └── ActionButton-Landscape.uss  # Landscape layout (--theme classes)
```

### Available Design Tokens

#### Button Sizes
- `--button-height-{small|medium|large}`
- `--button-min-width-{small|medium|large}`
- `--button-padding-{small|medium|large}`

#### Icon Sizes
- `--button-icon-size-{small|medium|large}`
- `--button-icon-bg-size-{small|medium|large}`

#### Spacing
- `--spacing-xs` (4px)
- `--spacing-sm` (8px)
- `--spacing-md` (16px)
- `--spacing-lg` (24px)
- `--spacing-xl` (32px)

#### Border Radius
- `--border-radius-sm` (4px)
- `--border-radius-md` (6px)
- `--border-radius-lg` (10px)
- `--border-radius-xl` (25px)

## Benefits

✅ **Scalable**: Automatically adapts to mobile, tablet, and desktop screens
✅ **Consistent**: Uses centralized design tokens
✅ **Flexible**: Easy to create button variants without duplicating code
✅ **Maintainable**: Changes to token values update all buttons instantly
✅ **Follows Best Practices**: Based on modern web/app design systems (Material, Tailwind)
