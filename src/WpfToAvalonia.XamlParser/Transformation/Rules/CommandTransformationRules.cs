using WpfToAvalonia.XamlParser.UnifiedAst;

namespace WpfToAvalonia.XamlParser.Transformation.Rules;

/// <summary>
/// Base class for command binding transformation rules.
/// Handles WPF command bindings and transforms them to Avalonia equivalents.
/// Implements tasks 2.5.7.3.1-2.5.7.3.4: Command binding transformations
/// </summary>
public abstract class CommandTransformationRuleBase : PropertyTransformationRuleBase
{
    public override bool CanTransformProperty(UnifiedXamlProperty property)
    {
        // Check for Command property bindings
        return property.PropertyName.EndsWith("Command") ||
               property.PropertyName == "Command" ||
               (property.HasMarkupExtension &&
                property.MarkupExtension?.ExtensionName == "Binding" &&
                IsCommandBinding(property.MarkupExtension));
    }

    private bool IsCommandBinding(UnifiedXamlMarkupExtension? extension)
    {
        if (extension == null) return false;

        // Check if binding path suggests a command
        if (extension.Parameters.TryGetValue("Path", out var path))
        {
            var pathStr = path?.ToString() ?? "";
            return pathStr.EndsWith("Command") || pathStr.Contains("Command.");
        }

        return false;
    }
}

/// <summary>
/// Transforms ICommand bindings from WPF to Avalonia.
/// Task 2.5.7.3.1: Parse ICommand bindings
/// Task 2.5.7.3.4: Update command binding syntax
/// </summary>
public sealed class CommandBindingTransformationRule : CommandTransformationRuleBase
{
    public override string Name => "TransformCommandBinding";

    public override int Priority => 80;

    public override UnifiedXamlProperty? TransformProperty(UnifiedXamlProperty property, TransformationContext context)
    {
        // Command bindings work the same in both WPF and Avalonia
        // The main difference is in the underlying command implementation

        if (property.PropertyName.EndsWith("Command"))
        {
            context.RecordTransformation(
                Name,
                "CommandBinding",
                $"Command property detected: {property.PropertyName}. " +
                "Command bindings are supported in Avalonia with similar syntax. " +
                "Ensure your ViewModel uses ICommand interface (works in both frameworks).");
        }

        // Check for static command references (like ApplicationCommands.Copy)
        var value = property.Value?.ToString() ?? "";
        if (value.Contains("ApplicationCommands") ||
            value.Contains("NavigationCommands") ||
            value.Contains("MediaCommands") ||
            value.Contains("ComponentCommands") ||
            value.Contains("EditingCommands"))
        {
            context.RecordTransformation(
                Name,
                "StaticCommand",
                $"WPF static command detected: {value}. " +
                "WPF's static command classes (ApplicationCommands, NavigationCommands, etc.) are not available in Avalonia. " +
                "Migration options:\n" +
                "1. Implement commands in your ViewModel using ICommand or ReactiveCommand\n" +
                "2. Use Avalonia's built-in commands where available\n" +
                "3. For Copy/Cut/Paste, bind to TextBox.CopyCommand, TextBox.CutCommand, TextBox.PasteCommand");
        }

        return property;
    }
}

/// <summary>
/// Transforms CommandParameter bindings.
/// Task 2.5.7.3.2: Transform command parameters
/// </summary>
public sealed class CommandParameterTransformationRule : PropertyTransformationRuleBase
{
    public override string Name => "TransformCommandParameter";

    public override int Priority => 79;

    public override bool CanTransformProperty(UnifiedXamlProperty property)
    {
        return property.PropertyName.EndsWith("CommandParameter") ||
               property.PropertyName == "CommandParameter";
    }

    public override UnifiedXamlProperty? TransformProperty(UnifiedXamlProperty property, TransformationContext context)
    {
        context.RecordTransformation(
            Name,
            "CommandParameter",
            "CommandParameter is supported in Avalonia with the same syntax. " +
            "Works identically to WPF - the parameter is passed to ICommand.Execute(object parameter).");

        // Check if using ElementName binding for CommandParameter
        if (property.HasMarkupExtension && property.MarkupExtension != null)
        {
            var binding = property.MarkupExtension;
            if (binding.Parameters.ContainsKey("ElementName"))
            {
                context.RecordTransformation(
                    Name,
                    "CommandParameterElementName",
                    "CommandParameter with ElementName binding. " +
                    "Supported in Avalonia, but ensure the referenced element is defined before this element in XAML.");
            }

            if (binding.Parameters.ContainsKey("RelativeSource"))
            {
                var relativeSource = binding.Parameters["RelativeSource"]?.ToString() ?? "";
                context.RecordTransformation(
                    Name,
                    "CommandParameterRelativeSource",
                    $"CommandParameter with RelativeSource: {relativeSource}. " +
                    "RelativeSource is supported in Avalonia 11+, but syntax may differ slightly. " +
                    "Verify RelativeSource mode (Self, TemplatedParent, FindAncestor).");
            }
        }

        return property;
    }
}

/// <summary>
/// Detects and transforms RoutedCommand/RoutedUICommand usage.
/// Task 2.5.7.3.3: Handle RoutedCommand to ReactiveCommand
/// </summary>
public sealed class RoutedCommandTransformationRule : ElementTransformationRuleBase
{
    public override string Name => "TransformRoutedCommand";

    public override int Priority => 78;

    public override bool CanTransformElement(UnifiedXamlElement element)
    {
        var typeName = element.ElementType?.Name ?? "";
        return typeName == "RoutedCommand" ||
               typeName == "RoutedUICommand" ||
               typeName.EndsWith(".RoutedCommand") ||
               typeName.EndsWith(".RoutedUICommand");
    }

    public override UnifiedXamlElement? TransformElement(UnifiedXamlElement element, TransformationContext context)
    {
        var typeName = element.ElementType?.Name ?? "";

        context.RecordTransformation(
            Name,
            "RoutedCommand",
            $"{typeName} detected in XAML. " +
            "Avalonia doesn't have RoutedCommand or RoutedUICommand. " +
            "Migration options:\n" +
            "1. Replace with ICommand implementation in ViewModel (recommended)\n" +
            "2. Use ReactiveCommand from ReactiveUI NuGet package\n" +
            "3. Implement custom ICommand class\n" +
            "\n" +
            "Example ViewModel implementation:\n" +
            "public ICommand MyCommand { get; }\n" +
            "MyCommand = new RelayCommand(Execute, CanExecute);\n" +
            "\n" +
            "Or with ReactiveCommand:\n" +
            "MyCommand = ReactiveCommand.Create(Execute, canExecuteObservable);");

        return element;
    }
}

/// <summary>
/// Transforms CommandBinding elements (WPF command routing infrastructure).
/// </summary>
public sealed class CommandBindingElementTransformationRule : ElementTransformationRuleBase
{
    public override string Name => "TransformCommandBindingElement";

    public override int Priority => 77;

    public override bool CanTransformElement(UnifiedXamlElement element)
    {
        var typeName = element.ElementType?.Name ?? "";
        return typeName == "CommandBinding" || typeName.EndsWith(".CommandBinding");
    }

    public override UnifiedXamlElement? TransformElement(UnifiedXamlElement element, TransformationContext context)
    {
        var commandProperty = element.Properties.FirstOrDefault(p => p.PropertyName == "Command");
        var commandValue = commandProperty?.Value?.ToString() ?? "unknown";

        context.RecordTransformation(
            Name,
            "CommandBindingElement",
            $"CommandBinding element detected for command: {commandValue}. " +
            "Avalonia doesn't have CommandBinding infrastructure for command routing. " +
            "Migration approach:\n" +
            "1. Remove <CommandBinding> from XAML\n" +
            "2. Bind controls directly to ICommand properties in ViewModel\n" +
            "3. Implement command logic in ViewModel instead of code-behind\n" +
            "\n" +
            "Example transformation:\n" +
            "WPF:\n" +
            "<Window.CommandBindings>\n" +
            "  <CommandBinding Command=\"ApplicationCommands.Copy\" Executed=\"Copy_Executed\"/>\n" +
            "</Window.CommandBindings>\n" +
            "\n" +
            "Avalonia:\n" +
            "ViewModel: public ICommand CopyCommand { get; }\n" +
            "XAML: <Button Command=\"{{Binding CopyCommand}}\">Copy</Button>");

        return element;
    }
}

/// <summary>
/// Transforms InputBinding elements (keyboard shortcuts and gestures).
/// Task 2.5.7.3.5: Map input gestures and keyboard shortcuts (XAML aspect)
/// </summary>
public sealed class InputBindingTransformationRule : ElementTransformationRuleBase
{
    public override string Name => "TransformInputBinding";

    public override int Priority => 76;

    public override bool CanTransformElement(UnifiedXamlElement element)
    {
        var typeName = element.ElementType?.Name ?? "";
        return typeName == "KeyBinding" ||
               typeName == "MouseBinding" ||
               typeName == "InputBinding" ||
               typeName.EndsWith(".KeyBinding") ||
               typeName.EndsWith(".MouseBinding") ||
               typeName.EndsWith(".InputBinding");
    }

    public override UnifiedXamlElement? TransformElement(UnifiedXamlElement element, TransformationContext context)
    {
        var typeName = element.ElementType?.Name ?? "";
        var keyProperty = element.Properties.FirstOrDefault(p => p.PropertyName == "Key");
        var gestureProperty = element.Properties.FirstOrDefault(p => p.PropertyName == "Gesture");
        var modifiersProperty = element.Properties.FirstOrDefault(p => p.PropertyName == "Modifiers");
        var commandProperty = element.Properties.FirstOrDefault(p => p.PropertyName == "Command");

        var keyValue = keyProperty?.Value?.ToString() ?? "";
        var gestureValue = gestureProperty?.Value?.ToString() ?? "";
        var modifiersValue = modifiersProperty?.Value?.ToString() ?? "";
        var commandValue = commandProperty?.Value?.ToString() ?? "";

        if (typeName.Contains("KeyBinding"))
        {
            context.RecordTransformation(
                Name,
                "KeyBinding",
                $"KeyBinding detected: Key={keyValue}, Modifiers={modifiersValue}, Command={commandValue}. " +
                "Avalonia supports KeyBindings with similar syntax. " +
                "Transformation:\n" +
                "1. KeyBinding syntax is mostly compatible\n" +
                "2. Use <Window.KeyBindings> or <UserControl.KeyBindings>\n" +
                "3. Key and Modifiers properties work the same\n" +
                "\n" +
                "Example:\n" +
                "<Window.KeyBindings>\n" +
                "  <KeyBinding Gesture=\"Ctrl+S\" Command=\"{{Binding SaveCommand}}\"/>\n" +
                "</Window.KeyBindings>\n" +
                "\n" +
                "Note: Gesture property is preferred over separate Key and Modifiers in Avalonia.");

            // Check for Gesture property format
            if (!string.IsNullOrEmpty(gestureValue))
            {
                context.RecordTransformation(
                    Name,
                    "KeyGesture",
                    $"KeyGesture '{gestureValue}' detected. Avalonia supports similar gesture syntax (e.g., 'Ctrl+S', 'Alt+F4').");
            }
        }
        else if (typeName.Contains("MouseBinding"))
        {
            context.RecordTransformation(
                Name,
                "MouseBinding",
                "MouseBinding detected. " +
                "Avalonia has limited support for MouseBinding. " +
                "Consider using standard mouse events (PointerPressed, PointerReleased) with commands instead. " +
                "Alternative: Implement Interaction.Behaviors with EventTriggerBehavior from Avalonia.Xaml.Interactions.");
        }

        return element;
    }
}

/// <summary>
/// Detects CommandTarget property usage.
/// </summary>
public sealed class CommandTargetTransformationRule : PropertyTransformationRuleBase
{
    public override string Name => "TransformCommandTarget";

    public override int Priority => 75;

    public override bool CanTransformProperty(UnifiedXamlProperty property)
    {
        return property.PropertyName == "CommandTarget";
    }

    public override UnifiedXamlProperty? TransformProperty(UnifiedXamlProperty property, TransformationContext context)
    {
        context.RecordTransformation(
            Name,
            "CommandTarget",
            "CommandTarget property detected. " +
            "Avalonia doesn't have CommandTarget property (WPF command routing feature). " +
            "In Avalonia, commands execute in the current DataContext. " +
            "If you need to target a specific element, pass it via CommandParameter:\n" +
            "CommandParameter=\"{Binding #TargetElement}\"");

        return property;
    }
}

/// <summary>
/// Transforms InputGestureText property (display-only keyboard shortcut hints).
/// </summary>
public sealed class InputGestureTextTransformationRule : PropertyTransformationRuleBase
{
    public override string Name => "TransformInputGestureText";

    public override int Priority => 74;

    public override bool CanTransformProperty(UnifiedXamlProperty property)
    {
        return property.PropertyName == "InputGestureText";
    }

    public override UnifiedXamlProperty? TransformProperty(UnifiedXamlProperty property, TransformationContext context)
    {
        var gestureText = property.Value?.ToString() ?? "";

        context.RecordTransformation(
            Name,
            "InputGestureText",
            $"InputGestureText='{gestureText}' detected. " +
            "Avalonia supports InputGesture on MenuItem for displaying keyboard shortcuts. " +
            "The property name and usage are the same as WPF.");

        return property;
    }
}
