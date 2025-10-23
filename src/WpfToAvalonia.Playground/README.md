# WPF to Avalonia Playground

An interactive Avalonia desktop application for testing and verifying WPF XAML to Avalonia XAML conversions in real-time.

## Features

- **Side-by-Side Editors**: View WPF input and Avalonia output simultaneously
- **Syntax Highlighting**: Code editors with monospace fonts and line numbers
- **Real-Time Conversion**: Convert WPF XAML to Avalonia with a single click
- **Diagnostics Panel**: See transformation statistics, errors, warnings, and info messages
- **Sample WPF XAML**: Includes sample WPF code with triggers to demonstrate conversion capabilities
- **Dark Theme**: VS Code-style dark theme for comfortable viewing

## Running the Playground

```bash
dotnet run --project src/WpfToAvalonia.Playground/WpfToAvalonia.Playground.csproj
```

## UI Layout

```
┌─────────────────────────────────────────────────────────────────┐
│ Toolbar: [Open File] [Convert] [Save] [Mode] [Options]         │
├──────────────────────┬──────────────────────┬────────────────────┤
│                      │                      │   Statistics       │
│   WPF Input          │   Avalonia Output    │   - Total: X       │
│   (Editable)         │   (Read-Only)        │   - Errors: X      │
│                      │                      │   - Warnings: X    │
│   [Code Editor]      │   [Code Editor]      │   - Info: X        │
│                      │                      │                    │
│                      │                      │   Diagnostics      │
│                      │                      │   [List of issues] │
│                      │                      │                    │
└──────────────────────┴──────────────────────┴────────────────────┘
│ Status: Ready                      Conversion Time: X.XXms       │
└─────────────────────────────────────────────────────────────────┘
```

## Current Capabilities

The playground demonstrates the current transformation engine capabilities:

- ✅ Namespace transformation (WPF → Avalonia)
- ✅ Control mappings (Window, Button, TextBlock, etc.)
- ✅ Property transformations
- ✅ Basic style transformations
- ✅ Trigger detection and transformation (in progress)
- ✅ Binding syntax preservation
- ✅ Resource dictionary handling

## Known Limitations

- File open/save dialogs need UI thread integration (currently placeholder)
- C# code transformation not yet implemented (XAML only)
- Some advanced WPF features may show warnings

## Sample XAML

The playground loads with sample WPF XAML that includes:
- Window with Resources
- Style with Triggers (IsMouseOver, IsPressed)
- Common controls (TextBlock, Button, TextBox, CheckBox)
- Layout containers (Grid, StackPanel)

This sample demonstrates the trigger transformation feature that converts WPF triggers to Avalonia pseudoclass selectors.
