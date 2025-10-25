using WpfToAvalonia.XamlParser.UnifiedAst;

namespace WpfToAvalonia.XamlParser.Transformation.Rules;

/// <summary>
/// Transforms WPF Trigger elements to Avalonia Style elements with pseudoclass selectors.
///
/// WPF Example:
/// <Style TargetType="Button">
///     <Style.Triggers>
///         <Trigger Property="IsMouseOver" Value="True">
///             <Setter Property="Background" Value="Red" />
///         </Trigger>
///     </Style.Triggers>
/// </Style>
///
/// Avalonia Output:
/// <Style Selector="Button:pointerover">
///     <Setter Property="Background" Value="Red" />
/// </Style>
/// </summary>
public sealed class TriggerToStyleSelectorTransformer : ElementTransformationRuleBase
{
    public override string Name => "TriggerToStyleSelector";

    public override int Priority => 200; // High priority - run before general trigger rule

    public override bool CanTransformElement(UnifiedXamlElement element)
    {
        // Only transform Trigger elements that can be converted to pseudoclasses
        return element.TypeName == "Trigger" && CanConvertToPseudoclass(element);
    }

    public override UnifiedXamlElement? TransformElement(UnifiedXamlElement element, TransformationContext context)
    {

        var propertyAttr = element.Properties.FirstOrDefault(p => p.PropertyName == "Property");
        var valueAttr = element.Properties.FirstOrDefault(p => p.PropertyName == "Value");

        if (propertyAttr == null || valueAttr == null)
        {
            return element; // Can't transform without property and value
        }

        var property = propertyAttr.GetStringValue();
        var value = valueAttr.GetStringValue();

        // Get the pseudoclass mapping
        var pseudoclass = GetPseudoclassForTrigger(property ?? "", value ?? "");
        if (pseudoclass == null)
        {
            return element; // No mapping available
        }

        // Get the parent Style element to determine the target type
        var parentStyle = FindParentStyle(element);
        if (parentStyle == null)
        {
            context.RecordTransformation(
                Name,
                "Trigger",
                $"Cannot convert trigger to style selector: no parent Style found");
            return element;
        }

        var targetType = GetStyleTargetType(parentStyle);
        if (string.IsNullOrEmpty(targetType))
        {
            context.RecordTransformation(
                Name,
                "Trigger",
                $"Cannot convert trigger to style selector: Style has no TargetType");
            return element;
        }

        // Create a new Style element with selector and store it in metadata
        var newStyle = new UnifiedXamlElement
        {
            #pragma warning disable CS0618
            TypeName = "Style",
            #pragma warning restore CS0618
            SourceXmlElement = element.SourceXmlElement,
            XmlNode = element.XmlNode,
            Location = element.Location,
            Formatting = element.Formatting
        };

        // Add Selector property
        var selectorProperty = new UnifiedXamlProperty
        {
            PropertyName = "Selector",
            Value = $"{targetType}{pseudoclass}",
            Kind = PropertyKind.Attribute,
            Parent = newStyle
        };
        newStyle.AddProperty(selectorProperty);

        // Copy all Setter children from the trigger to the new style
        foreach (var child in element.Children.Where(c => c.TypeName == "Setter"))
        {
            var clonedSetter = CloneElement(child);
            clonedSetter.Parent = newStyle;
            newStyle.AddChild(clonedSetter);
        }

        context.RecordTransformation(
            Name,
            "Trigger",
            $"Converted Trigger (Property={property}, Value={value}) to Style with Selector=\"{targetType}{pseudoclass}\"");

        // Store the new style in parent style's metadata for later addition
        if (!parentStyle.Metadata.ContainsKey("ConvertedTriggerStyles"))
        {
            parentStyle.Metadata["ConvertedTriggerStyles"] = new List<UnifiedXamlElement>();
        }

        var convertedStyles = (List<UnifiedXamlElement>)parentStyle.Metadata["ConvertedTriggerStyles"];
        convertedStyles.Add(newStyle);

        // Mark this trigger as converted so it can be removed
        element.Metadata["ConvertedToPseudoclass"] = true;

        return element;
    }

    private bool CanConvertToPseudoclass(UnifiedXamlElement element)
    {
        var propertyAttr = element.Properties.FirstOrDefault(p => p.PropertyName == "Property");
        var valueAttr = element.Properties.FirstOrDefault(p => p.PropertyName == "Value");

        if (propertyAttr == null || valueAttr == null)
        {
            return false;
        }

        var property = propertyAttr.GetStringValue();
        var value = valueAttr.GetStringValue();

        return GetPseudoclassForTrigger(property ?? "", value ?? "") != null;
    }

    private string? GetPseudoclassForTrigger(string property, string value)
    {
        // Map WPF trigger properties to Avalonia pseudoclasses
        var mapping = new Dictionary<(string Property, string Value), string>
        {
            { ("IsMouseOver", "True"), ":pointerover" },
            { ("IsPressed", "True"), ":pressed" },
            { ("IsFocused", "True"), ":focus" },
            { ("IsEnabled", "False"), ":disabled" },
            { ("IsSelected", "True"), ":selected" },
            { ("IsChecked", "True"), ":checked" },
            { ("IsChecked", "False"), ":unchecked" },
            { ("IsReadOnly", "True"), ":readonly" },
            { ("IsKeyboardFocused", "True"), ":focus" },
            { ("IsKeyboardFocusWithin", "True"), ":focus-within" },
            { ("IsMouseDirectlyOver", "True"), ":pointerover" },
            { ("IsDragging", "True"), ":dragging" },
        };

        if (mapping.TryGetValue((property, value), out var pseudoclass))
        {
            return pseudoclass;
        }

        // Handle inverted cases (e.g., IsEnabled="True" → :not(:disabled))
        if (property == "IsEnabled" && value == "True")
        {
            return null; // Default state, no pseudoclass needed
        }

        return null;
    }

    private UnifiedXamlElement? FindParentStyle(UnifiedXamlElement element)
    {
        var current = element.Parent;
        while (current != null)
        {
            if (current is UnifiedXamlElement parentElement && parentElement.TypeName == "Style")
            {
                return parentElement;
            }
            current = current.Parent;
        }
        return null;
    }

    private string? GetStyleTargetType(UnifiedXamlElement styleElement)
    {
        var targetTypeProperty = styleElement.Properties.FirstOrDefault(p => p.PropertyName == "TargetType");
        return targetTypeProperty?.GetStringValue();
    }

    private UnifiedXamlElement CloneElement(UnifiedXamlElement source)
    {
        var clone = new UnifiedXamlElement
        {
            #pragma warning disable CS0618
            TypeName = source.TypeName,
            #pragma warning restore CS0618
            SourceXmlElement = source.SourceXmlElement,
            XmlNode = source.XmlNode,
            Location = source.Location,
            Formatting = source.Formatting,
            TextContent = source.TextContent,
            XKey = source.XKey,
            XName = source.XName
        };

        // Clone properties
        foreach (var prop in source.Properties)
        {
            var clonedProp = new UnifiedXamlProperty
            {
                PropertyName = prop.PropertyName,
                Value = prop.Value,
                Kind = prop.Kind,
                AttachedOwnerType = prop.AttachedOwnerType,
                MarkupExtension = prop.MarkupExtension,
                Parent = clone
            };
            clone.AddProperty(clonedProp);
        }

        // Clone children recursively
        foreach (var child in source.Children)
        {
            var clonedChild = CloneElement(child);
            clonedChild.Parent = clone;
            clone.AddChild(clonedChild);
        }

        return clone;
    }
}

/// <summary>
/// Transforms WPF DataTrigger elements to Avalonia-compatible patterns.
/// DataTriggers in WPF bind to data and change properties based on binding values.
/// In Avalonia, this can be achieved through:
/// 1. Data binding with converters
/// 2. Reactive extensions (IObservable)
/// 3. Behaviors (Avalonia.Xaml.Interactivity)
///
/// WPF Example:
/// <DataTrigger Binding="{Binding IsActive}" Value="True">
///     <Setter Property="Background" Value="Green" />
/// </DataTrigger>
///
/// Avalonia Approach 1 (MultiBinding with converter):
/// <Border Background="{Binding IsActive, Converter={StaticResource BoolToBackgroundConverter}}" />
///
/// Avalonia Approach 2 (Behavior):
/// <Border>
///     <i:Interaction.Behaviors>
///         <behaviors:DataTriggerBehavior Binding="{Binding IsActive}" Value="True">
///             <behaviors:ChangePropertyAction PropertyName="Background" Value="Green" />
///         </behaviors:DataTriggerBehavior>
///     </i:Interaction.Behaviors>
/// </Border>
/// </summary>
public sealed class DataTriggerToBindingTransformer : ElementTransformationRuleBase
{
    public override string Name => "DataTriggerToBinding";

    public override int Priority => 200; // High priority - run before general trigger warnings

    public override bool CanTransformElement(UnifiedXamlElement element)
    {
        return element.TypeName == "DataTrigger";
    }

    public override UnifiedXamlElement? TransformElement(UnifiedXamlElement element, TransformationContext context)
    {
        var binding = element.Properties.FirstOrDefault(p => p.PropertyName == "Binding");
        var value = element.Properties.FirstOrDefault(p => p.PropertyName == "Value");
        var setters = element.Children.Where(c => c.TypeName == "Setter").ToList();

        if (binding == null || value == null || setters.Count == 0)
        {
            AddBasicWarning(element, context);
            return element;
        }

        // Analyze the binding and setters to provide specific guidance
        var bindingPath = ExtractBindingPath(binding);
        var targetValue = value.GetStringValue();

        if (setters.Count == 1 && IsSimplePropertySetter(setters[0]))
        {
            // Simple case: single property setter
            var setter = setters[0];
            var targetProperty = setter.Properties.FirstOrDefault(p => p.PropertyName == "Property")?.GetStringValue();
            var setterValue = setter.Properties.FirstOrDefault(p => p.PropertyName == "Value")?.GetStringValue();

            if (!string.IsNullOrEmpty(targetProperty) && !string.IsNullOrEmpty(setterValue))
            {
                AddConverterSuggestion(element, context, bindingPath, targetValue, targetProperty, setterValue);
                return element;
            }
        }

        // Complex case: multiple setters or complex bindings
        AddBehaviorSuggestion(element, context, bindingPath, targetValue, setters.Count);
        return element;
    }

    private string? ExtractBindingPath(UnifiedXamlProperty bindingProperty)
    {
        if (bindingProperty.MarkupExtension != null)
        {
            // Check for Binding-specific path
            if (bindingProperty.MarkupExtension.Binding?.Path != null)
            {
                return bindingProperty.MarkupExtension.Binding.Path;
            }

            // Check positional argument (e.g., {Binding IsActive})
            if (bindingProperty.MarkupExtension.PositionalArgument is string positional)
            {
                return positional;
            }

            // Check named parameters
            if (bindingProperty.MarkupExtension.Parameters.TryGetValue("Path", out var pathValue))
            {
                return pathValue?.ToString();
            }
        }
        return null;
    }

    private bool IsSimplePropertySetter(UnifiedXamlElement setter)
    {
        return setter.Properties.Count == 2 && // Property and Value
               setter.Children.Count == 0;     // No complex value
    }

    private void AddConverterSuggestion(
        UnifiedXamlElement element,
        TransformationContext context,
        string? bindingPath,
        string? targetValue,
        string targetProperty,
        string setterValue)
    {
        var suggestion = $@"DataTrigger can be replaced with value converter:

WPF DataTrigger:
<DataTrigger Binding=""{{Binding {bindingPath}}}"" Value=""{targetValue}"">
    <Setter Property=""{targetProperty}"" Value=""{setterValue}"" />
</DataTrigger>

Avalonia Approach 1 - Direct binding with converter:
<{GetParentControlType(element)} {targetProperty}=""{{Binding {bindingPath}, Converter={{StaticResource BoolTo{targetProperty}Converter}}}}"" />

Avalonia Approach 2 - Multi-value converter:
Create a converter that maps '{targetValue}' → '{setterValue}' for the {targetProperty} property.";

        element.AddDiagnostic(
            "DATATRIGGER_CONVERTER_PATTERN",
            suggestion,
            Core.Diagnostics.DiagnosticSeverity.Info);

        context.RecordTransformation(
            Name,
            "DataTrigger",
            $"DataTrigger on '{bindingPath}' can be replaced with value converter for {targetProperty}");
    }

    private void AddBehaviorSuggestion(
        UnifiedXamlElement element,
        TransformationContext context,
        string? bindingPath,
        string? targetValue,
        int setterCount)
    {
        var suggestion = $@"DataTrigger requires Avalonia.Xaml.Interactivity behaviors:

1. Install package: Avalonia.Xaml.Interactivity
2. Add namespace: xmlns:i=""using:Avalonia.Xaml.Interactivity""
3. Replace DataTrigger with:

<i:Interaction.Behaviors>
    <ia:DataTriggerBehavior Binding=""{{Binding {bindingPath}}}""
                            ComparisonCondition=""Equal""
                            Value=""{targetValue}"">
        <!-- Add {setterCount} ChangePropertyAction(s) for each Setter -->
        <ia:ChangePropertyAction PropertyName=""YourProperty"" Value=""YourValue"" />
    </ia:DataTriggerBehavior>
</i:Interaction.Behaviors>

Alternative: Use reactive patterns with IObservable or implement in ViewModel.";

        element.AddDiagnostic(
            "DATATRIGGER_BEHAVIOR_PATTERN",
            suggestion,
            Core.Diagnostics.DiagnosticSeverity.Info);

        context.RecordTransformation(
            Name,
            "DataTrigger",
            $"DataTrigger on '{bindingPath}' requires Avalonia.Xaml.Interactivity behaviors ({setterCount} setters)");
    }

    private void AddBasicWarning(UnifiedXamlElement element, TransformationContext context)
    {
        element.AddDiagnostic(
            "DATATRIGGER_NOT_SUPPORTED",
            "DataTrigger is not directly supported in Avalonia. Consider using: 1) Value converters, 2) Avalonia.Xaml.Interactivity behaviors, or 3) Reactive patterns in ViewModel.",
            Core.Diagnostics.DiagnosticSeverity.Warning);

        context.RecordTransformation(
            Name,
            "DataTrigger",
            "DataTrigger requires manual conversion");
    }

    private string GetParentControlType(UnifiedXamlElement element)
    {
        var parent = element.Parent;
        while (parent != null)
        {
            if (parent is UnifiedXamlElement parentElement &&
                !parentElement.TypeName.Contains('.') &&
                !parentElement.TypeName.StartsWith("Style"))
            {
                return parentElement.TypeName;
            }
            parent = parent.Parent;
        }
        return "Control";
    }
}

/// <summary>
/// Transforms WPF EventTrigger elements to Avalonia animation patterns.
/// EventTriggers in WPF typically trigger storyboard animations on events like Loaded, MouseEnter, etc.
/// In Avalonia, animations are handled differently:
/// 1. CSS-like animations in XAML
/// 2. Code-behind animations
/// 3. Behaviors (Avalonia.Xaml.Interactivity)
///
/// WPF Example:
/// <EventTrigger RoutedEvent="Loaded">
///     <BeginStoryboard>
///         <Storyboard>
///             <DoubleAnimation Storyboard.TargetProperty="Opacity"
///                            From="0" To="1" Duration="0:0:0.5" />
///         </Storyboard>
///     </BeginStoryboard>
/// </EventTrigger>
///
/// Avalonia Approach 1 (Style Animation):
/// <Style Selector="Button:loaded">
///     <Style.Animations>
///         <Animation Duration="0:0:0.5">
///             <KeyFrame Cue="0%"><Setter Property="Opacity" Value="0"/></KeyFrame>
///             <KeyFrame Cue="100%"><Setter Property="Opacity" Value="1"/></KeyFrame>
///         </Animation>
///     </Style.Animations>
/// </Style>
///
/// Avalonia Approach 2 (Code-behind):
/// var animation = new DoubleTransition { Property = OpacityProperty, Duration = TimeSpan.FromMilliseconds(500) };
/// </summary>
public sealed class EventTriggerToAnimationTransformer : ElementTransformationRuleBase
{
    public override string Name => "EventTriggerToAnimation";

    public override int Priority => 200; // High priority - run before general trigger warnings

    public override bool CanTransformElement(UnifiedXamlElement element)
    {
        return element.TypeName == "EventTrigger";
    }

    public override UnifiedXamlElement? TransformElement(UnifiedXamlElement element, TransformationContext context)
    {
        var routedEvent = element.Properties.FirstOrDefault(p => p.PropertyName == "RoutedEvent")?.GetStringValue();
        var storyboard = FindStoryboard(element);

        if (string.IsNullOrEmpty(routedEvent))
        {
            AddBasicWarning(element, context);
            return element;
        }

        // Check if this can be mapped to a pseudoclass animation
        var pseudoclass = MapEventToPseudoclass(routedEvent);

        if (pseudoclass != null && storyboard != null)
        {
            var animations = AnalyzeStoryboard(storyboard);
            AddStyleAnimationSuggestion(element, context, routedEvent, pseudoclass, animations);
        }
        else if (storyboard != null)
        {
            var animations = AnalyzeStoryboard(storyboard);
            AddCodeBehindSuggestion(element, context, routedEvent, animations);
        }
        else
        {
            AddBasicWarning(element, context);
        }

        return element;
    }

    private string? MapEventToPseudoclass(string routedEvent)
    {
        // Map common WPF routed events to Avalonia pseudoclasses or lifecycle events
        var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "MouseEnter", ":pointerover" },
            { "MouseLeave", ":not(:pointerover)" },
            { "GotFocus", ":focus" },
            { "LostFocus", ":not(:focus)" },
            { "Loaded", ":loaded" }, // Note: Avalonia doesn't have :loaded, needs code-behind
        };

        // Remove namespace prefix if present (e.g., "Button.Click" -> "Click")
        var eventName = routedEvent.Contains('.') ? routedEvent.Split('.').Last() : routedEvent;

        return mapping.TryGetValue(eventName, out var pseudoclassValue) ? pseudoclassValue : null;
    }

    private UnifiedXamlElement? FindStoryboard(UnifiedXamlElement eventTrigger)
    {
        // Look for BeginStoryboard -> Storyboard pattern
        var beginStoryboard = eventTrigger.Children.FirstOrDefault(c => c.TypeName == "BeginStoryboard");
        if (beginStoryboard != null)
        {
            return beginStoryboard.Children.FirstOrDefault(c => c.TypeName == "Storyboard");
        }

        // Direct storyboard
        return eventTrigger.Children.FirstOrDefault(c => c.TypeName == "Storyboard");
    }

    private List<AnimationInfo> AnalyzeStoryboard(UnifiedXamlElement storyboard)
    {
        var animations = new List<AnimationInfo>();

        foreach (var child in storyboard.Children)
        {
            // Try both formats: "Storyboard.TargetProperty" and "TargetProperty"
            var targetProperty = child.Properties.FirstOrDefault(p =>
                p.PropertyName == "Storyboard.TargetProperty" ||
                p.PropertyName == "TargetProperty")?.GetStringValue();

            var animInfo = new AnimationInfo
            {
                AnimationType = child.TypeName,
                TargetProperty = targetProperty,
                From = child.Properties.FirstOrDefault(p => p.PropertyName == "From")?.GetStringValue(),
                To = child.Properties.FirstOrDefault(p => p.PropertyName == "To")?.GetStringValue(),
                Duration = child.Properties.FirstOrDefault(p => p.PropertyName == "Duration")?.GetStringValue(),
                EasingFunction = child.Children.FirstOrDefault(c => c.TypeName.Contains("Easing"))?.TypeName
            };

            animations.Add(animInfo);
        }

        return animations;
    }

    private void AddStyleAnimationSuggestion(
        UnifiedXamlElement element,
        TransformationContext context,
        string routedEvent,
        string pseudoclass,
        List<AnimationInfo> animations)
    {
        var animationExamples = string.Join("\n            ", animations.Select(a =>
            $@"<KeyFrame Cue=""0%""><Setter Property=""{a.TargetProperty}"" Value=""{a.From ?? "0"}""/></KeyFrame>
            <KeyFrame Cue=""100%""><Setter Property=""{a.TargetProperty}"" Value=""{a.To ?? "1"}""/></KeyFrame>"));

        var duration = animations.FirstOrDefault()?.Duration ?? "0:0:0.5";

        var suggestion = $@"EventTrigger can be replaced with Avalonia Style Animation:

WPF EventTrigger (Event: {routedEvent}):
<EventTrigger RoutedEvent=""{routedEvent}"">
    <BeginStoryboard>
        <Storyboard>
            {animations.Count} animation(s)
        </Storyboard>
    </BeginStoryboard>
</EventTrigger>

Avalonia Style Animation:
<Style Selector=""Control{pseudoclass}"">
    <Style.Animations>
        <Animation Duration=""{duration}"">
            {animationExamples}
        </Animation>
    </Style.Animations>
</Style>

Note: Adjust Selector to match your control type. Use FillMode=""Forward"" to persist final values.";

        element.AddDiagnostic(
            "EVENTTRIGGER_STYLE_ANIMATION",
            suggestion,
            Core.Diagnostics.DiagnosticSeverity.Info);

        context.RecordTransformation(
            Name,
            "EventTrigger",
            $"EventTrigger on '{routedEvent}' can be replaced with Style Animation using '{pseudoclass}'");
    }

    private void AddCodeBehindSuggestion(
        UnifiedXamlElement element,
        TransformationContext context,
        string routedEvent,
        List<AnimationInfo> animations)
    {
        var transitionExamples = string.Join("\n        ", animations.Select(a =>
            $"new {GetAvaloniaTransitionType(a.AnimationType)} {{ Property = {a.TargetProperty}Property, Duration = {a.Duration ?? "TimeSpan.FromSeconds(0.5)"} }}"));

        var suggestion = $@"EventTrigger requires code-behind animation (Event: {routedEvent}):

WPF EventTrigger:
<EventTrigger RoutedEvent=""{routedEvent}"">
    <BeginStoryboard>
        <Storyboard>
            {animations.Count} animation(s)
        </Storyboard>
    </BeginStoryboard>
</EventTrigger>

Avalonia Approach 1 - Transitions (recommended):
Add to your control's Transitions property:
<Control.Transitions>
    <Transitions>
        {transitionExamples}
    </Transitions>
</Control.Transitions>

Avalonia Approach 2 - Code-behind animation:
Handle the {routedEvent} event in code-behind:
private void OnControl{routedEvent}(object sender, RoutedEventArgs e)
{{
    var animation = new Animation
    {{
        Duration = TimeSpan.FromMilliseconds(500),
        Children = {{ /* Add keyframes */ }}
    }};
    animation.RunAsync(control);
}}

Avalonia Approach 3 - Behaviors:
Install Avalonia.Xaml.Interactivity and use EventTriggerBehavior.";

        element.AddDiagnostic(
            "EVENTTRIGGER_CODE_ANIMATION",
            suggestion,
            Core.Diagnostics.DiagnosticSeverity.Info);

        context.RecordTransformation(
            Name,
            "EventTrigger",
            $"EventTrigger on '{routedEvent}' requires code-behind or Transitions ({animations.Count} animations)");
    }

    private void AddBasicWarning(UnifiedXamlElement element, TransformationContext context)
    {
        element.AddDiagnostic(
            "EVENTTRIGGER_NOT_SUPPORTED",
            "EventTrigger is not directly supported in Avalonia. Consider using: 1) Style Animations with pseudoclasses, 2) Transitions, 3) Code-behind animations, or 4) Avalonia.Xaml.Interactivity behaviors.",
            Core.Diagnostics.DiagnosticSeverity.Warning);

        context.RecordTransformation(
            Name,
            "EventTrigger",
            "EventTrigger requires manual conversion to Avalonia animations");
    }

    private string GetAvaloniaTransitionType(string wpfAnimationType)
    {
        return wpfAnimationType switch
        {
            "DoubleAnimation" => "DoubleTransition",
            "ColorAnimation" => "ColorTransition",
            "ThicknessAnimation" => "ThicknessTransition",
            "PointAnimation" => "PointTransition",
            _ => "Transition"
        };
    }

    private class AnimationInfo
    {
        public string? AnimationType { get; set; }
        public string? TargetProperty { get; set; }
        public string? From { get; set; }
        public string? To { get; set; }
        public string? Duration { get; set; }
        public string? EasingFunction { get; set; }
    }
}

/// <summary>
/// Post-processes Style elements to restructure triggers that have been converted to style selectors.
/// This rule removes the Style.Triggers container and adds the converted styles as siblings.
/// </summary>
public sealed class StyleTriggersRestructuringRule : ElementTransformationRuleBase
{
    public override string Name => "RestructureStyleTriggers";

    public override int Priority => 50; // Lower priority - run after trigger transformations

    public override bool CanTransformElement(UnifiedXamlElement element)
    {
        // Process Style elements that have converted trigger styles in metadata
        var canTransform = element.TypeName == "Style" &&
               element.Metadata.ContainsKey("ConvertedTriggerStyles");
        if (element.TypeName == "Style")
        {
        }
        return canTransform;
    }

    public override UnifiedXamlElement? TransformElement(UnifiedXamlElement element, TransformationContext context)
    {
        // Check if this style has converted trigger styles in metadata
        if (!element.Metadata.TryGetValue("ConvertedTriggerStyles", out var stylesObj) ||
            stylesObj is not List<UnifiedXamlElement> convertedStyles ||
            convertedStyles.Count == 0)
        {
            return element;
        }

        // Find the parent that can hold multiple styles
        // This could be a collection element (Window.Resources with multiple children) or a property value
        var (resourcesParent, needsCollectionConversion) = FindResourcesParentAndType(element);

        if (resourcesParent == null)
        {
            context.RecordTransformation(
                Name,
                "Style",
                "Cannot restructure triggers: no suitable parent found for new styles");
            return element;
        }

        // If the style is a direct property value (single style in resources), we need to convert to collection
        if (needsCollectionConversion)
        {
            ConvertPropertyValueToCollection(element, resourcesParent, convertedStyles, context);
        }
        else
        {
            // Add converted styles as siblings to the original style in existing collection
            var parentIndex = resourcesParent.Children.IndexOf(element);
            if (parentIndex >= 0)
            {
                for (int i = 0; i < convertedStyles.Count; i++)
                {
                    var convertedStyle = convertedStyles[i];
                    convertedStyle.Parent = resourcesParent;
                    resourcesParent.Children.Insert(parentIndex + 1 + i, convertedStyle);
                }

                context.RecordTransformation(
                    Name,
                    "Style",
                    $"Added {convertedStyles.Count} converted trigger style(s) as siblings to original style");
            }
        }

        // Remove the Style.Triggers child from the original style
        var triggersChild = element.Children.FirstOrDefault(c => c.TypeName == "Style.Triggers");
        if (triggersChild != null)
        {
            // Remove converted triggers from Style.Triggers
            var triggersToRemove = triggersChild.Children
                .Where(t => t.Metadata.ContainsKey("ConvertedToPseudoclass"))
                .ToList();

            foreach (var trigger in triggersToRemove)
            {
                triggersChild.Children.Remove(trigger);
            }

            // If all triggers were converted, remove the entire Style.Triggers container
            if (triggersChild.Children.Count == 0)
            {
                element.Children.Remove(triggersChild);

                context.RecordTransformation(
                    Name,
                    "Style",
                    "Removed empty Style.Triggers container after converting all triggers");
            }
        }

        return element;
    }

    private (UnifiedXamlElement? Parent, bool NeedsCollectionConversion) FindResourcesParentAndType(UnifiedXamlElement element)
    {
        var current = element.Parent;

        // First check if we're in a collection element (multiple styles case)
        while (current != null)
        {
            if (current is UnifiedXamlElement parentElement)
            {
                // Collection element for resources
                if (parentElement.TypeName == "Window.Resources" ||
                    parentElement.TypeName == "UserControl.Resources" ||
                    parentElement.TypeName == "ResourceDictionary")
                {
                    return (parentElement, false);
                }
            }
            current = current.Parent;
        }

        // If not in a collection, check if we're a direct property value (single style case)
        // The style's parent would be the Window/UserControl element
        if (element.Parent is UnifiedXamlElement immediateParent)
        {
            // Find the property that contains this style
            var containingProperty = immediateParent.Properties
                .FirstOrDefault(p => p.Value == element);

            if (containingProperty != null &&
                (containingProperty.PropertyName == "Resources" ||
                 containingProperty.PropertyName == "Window.Resources" ||
                 containingProperty.PropertyName == "UserControl.Resources"))
            {
                // We found it! The style is a direct property value, needs collection conversion
                return (immediateParent, true);
            }
        }

        return (null, false);
    }

    private void ConvertPropertyValueToCollection(
        UnifiedXamlElement originalStyle,
        UnifiedXamlElement ownerElement,
        List<UnifiedXamlElement> convertedStyles,
        TransformationContext context)
    {
        // Find the property that contains the style
        var resourcesProperty = ownerElement.Properties
            .FirstOrDefault(p => p.Value == originalStyle);

        if (resourcesProperty == null)
            return;

        // Create a collection element to hold all styles
        var collectionElement = new UnifiedXamlElement
        {
            #pragma warning disable CS0618
            TypeName = $"{ownerElement.TypeName}.Resources",
            #pragma warning restore CS0618
            Parent = ownerElement,
            SourceXmlElement = originalStyle.SourceXmlElement,
            XmlNode = originalStyle.XmlNode,
            Location = originalStyle.Location,
            Formatting = originalStyle.Formatting
        };

        // Add original style as first child
        originalStyle.Parent = collectionElement;
        collectionElement.AddChild(originalStyle);

        // Add converted styles as siblings
        foreach (var convertedStyle in convertedStyles)
        {
            convertedStyle.Parent = collectionElement;
            collectionElement.AddChild(convertedStyle);
        }

        // Replace the property value with the collection
        resourcesProperty.Value = collectionElement;

        context.RecordTransformation(
            Name,
            "Style",
            $"Converted single style property to collection with {convertedStyles.Count + 1} styles");
    }
}

/// <summary>
/// Transforms WPF MultiTrigger to Avalonia composite Style Selectors.
/// WPF MultiTriggers allow applying setters when multiple conditions are all true.
/// Avalonia supports composite selectors (e.g., :pointerover:pressed).
/// </summary>
public sealed class MultiTriggerTransformer : ElementTransformationRuleBase
{
    public override string Name => "MultiTriggerToCompositeSelector";
    public override int Priority => 200;

    public override bool CanTransformElement(UnifiedXamlElement element)
    {
        return element.TypeName == "MultiTrigger";
    }

    public override UnifiedXamlElement? TransformElement(UnifiedXamlElement element, TransformationContext context)
    {
        // Extract conditions - they might be direct children, under MultiTrigger.Conditions property element, or in Properties
        var conditions = element.Children.Where(c => c.TypeName == "Condition").ToList();

        // Check if conditions are under MultiTrigger.Conditions property element (as child element)
        if (conditions.Count == 0)
        {
            var conditionsProperty = element.Children.FirstOrDefault(c => c.TypeName == "MultiTrigger.Conditions");
            if (conditionsProperty != null)
            {
                conditions = conditionsProperty.Children.Where(c => c.TypeName == "Condition").ToList();
            }
        }

        // Check if conditions are stored as a property value
        if (conditions.Count == 0)
        {
            var conditionsProperty = element.Properties.FirstOrDefault(p => p.PropertyName == "Conditions");
            if (conditionsProperty?.Value is UnifiedXamlElement conditionsElement)
            {
                conditions = conditionsElement.Children.Where(c => c.TypeName == "Condition").ToList();
            }
        }

        var setters = element.Children.Where(c => c.TypeName == "Setter").ToList();

        if (conditions.Count == 0)
        {
            AddBasicWarning(element, context);
            return element;
        }

        // Try to map all conditions to pseudoclasses
        var pseudoclasses = new List<string>();
        var unmappableConditions = new List<string>();

        foreach (var condition in conditions)
        {
            var property = condition.Properties.FirstOrDefault(p => p.PropertyName == "Property")?.GetStringValue();
            var value = condition.Properties.FirstOrDefault(p => p.PropertyName == "Value")?.GetStringValue();

            if (string.IsNullOrEmpty(property) || string.IsNullOrEmpty(value))
            {
                unmappableConditions.Add("incomplete condition");
                continue;
            }

            var pseudoclass = MapPropertyValueToPseudoclass(property, value);
            if (pseudoclass != null)
            {
                pseudoclasses.Add(pseudoclass);
            }
            else
            {
                unmappableConditions.Add($"{property}={value}");
            }
        }

        // Generate suggestion based on whether all conditions can be mapped
        if (pseudoclasses.Count > 0 && unmappableConditions.Count == 0)
        {
            AddCompositeSelectorSuggestion(element, context, pseudoclasses, conditions, setters);
        }
        else if (pseudoclasses.Count > 0)
        {
            AddPartialMappingSuggestion(element, context, pseudoclasses, unmappableConditions, conditions, setters);
        }
        else
        {
            AddBehaviorSuggestion(element, context, conditions, setters);
        }

        return element;
    }

    private string? MapPropertyValueToPseudoclass(string property, string value)
    {
        var mapping = new Dictionary<(string Property, string Value), string>(
            new PropertyValueComparer())
        {
            { ("IsMouseOver", "True"), ":pointerover" },
            { ("IsPressed", "True"), ":pressed" },
            { ("IsFocused", "True"), ":focus" },
            { ("IsEnabled", "False"), ":disabled" },
            { ("IsSelected", "True"), ":selected" },
            { ("IsChecked", "True"), ":checked" },
        };

        return mapping.TryGetValue((property, value), out var pseudoclass) ? pseudoclass : null;
    }

    private void AddCompositeSelectorSuggestion(
        UnifiedXamlElement element,
        TransformationContext context,
        List<string> pseudoclasses,
        List<UnifiedXamlElement> conditions,
        List<UnifiedXamlElement> setters)
    {
        var compositeSelector = string.Join("", pseudoclasses);
        var conditionsText = string.Join(", ", conditions.Select(c =>
        {
            var prop = c.Properties.FirstOrDefault(p => p.PropertyName == "Property")?.GetStringValue();
            var val = c.Properties.FirstOrDefault(p => p.PropertyName == "Value")?.GetStringValue();
            return $"{prop}={val}";
        }));

        var setterExamples = string.Join("\n        ", setters.Select(s =>
        {
            var prop = s.Properties.FirstOrDefault(p => p.PropertyName == "Property")?.GetStringValue();
            var val = s.Properties.FirstOrDefault(p => p.PropertyName == "Value")?.GetStringValue();
            return $@"<Setter Property=""{prop}"" Value=""{val}"" />";
        }));

        var suggestion = $@"MultiTrigger can be converted to Avalonia composite Style Selector:

WPF MultiTrigger (Conditions: {conditionsText}):
<MultiTrigger>
    <MultiTrigger.Conditions>
        {conditions.Count} condition(s)
    </MultiTrigger.Conditions>
    {setters.Count} setter(s)
</MultiTrigger>

Avalonia Composite Selector Style:
<Style Selector=""Control{compositeSelector}"">
    {setterExamples}
</Style>

Note: Adjust 'Control' to match your target type. Composite selectors apply when ALL conditions are true.";

        element.AddDiagnostic(
            "MULTITRIGGER_COMPOSITE_SELECTOR",
            suggestion,
            Core.Diagnostics.DiagnosticSeverity.Info);

        context.RecordTransformation(
            Name,
            "MultiTrigger",
            $"MultiTrigger with {conditions.Count} conditions can be replaced with composite selector '{compositeSelector}'");
    }

    private void AddPartialMappingSuggestion(
        UnifiedXamlElement element,
        TransformationContext context,
        List<string> pseudoclasses,
        List<string> unmappableConditions,
        List<UnifiedXamlElement> conditions,
        List<UnifiedXamlElement> setters)
    {
        var compositeSelector = string.Join("", pseudoclasses);
        var unmappable = string.Join(", ", unmappableConditions);

        var suggestion = $@"MultiTrigger partially mappable to Avalonia:

WPF MultiTrigger has {conditions.Count} conditions.
- Mappable to pseudoclass: {pseudoclasses.Count} condition(s) → '{compositeSelector}'
- Requires alternative approach: {unmappableConditions.Count} condition(s) ({unmappable})

Avalonia Approaches:
1. Use composite selector for mappable conditions:
   <Style Selector=""Control{compositeSelector}"">
       <!-- Apply {setters.Count} setter(s) here -->
   </Style>

2. For unmappable conditions, consider:
   - Multi-value converters
   - Avalonia.Xaml.Interactivity behaviors
   - Custom attached properties with style selectors

Note: MultiTrigger with mixed conditions may require multiple Avalonia patterns.";

        element.AddDiagnostic(
            "MULTITRIGGER_PARTIAL_MAPPING",
            suggestion,
            Core.Diagnostics.DiagnosticSeverity.Warning);

        context.RecordTransformation(
            Name,
            "MultiTrigger",
            $"MultiTrigger partially mappable: {pseudoclasses.Count} of {conditions.Count} conditions can use composite selectors");
    }

    private void AddBehaviorSuggestion(
        UnifiedXamlElement element,
        TransformationContext context,
        List<UnifiedXamlElement> conditions,
        List<UnifiedXamlElement> setters)
    {
        var conditionsText = string.Join("\n        ", conditions.Select((c, i) =>
        {
            var prop = c.Properties.FirstOrDefault(p => p.PropertyName == "Property")?.GetStringValue();
            var val = c.Properties.FirstOrDefault(p => p.PropertyName == "Value")?.GetStringValue();
            return $"{i + 1}. {prop} = {val}";
        }));

        var suggestion = $@"MultiTrigger requires alternative Avalonia patterns:

WPF MultiTrigger:
<MultiTrigger>
    <MultiTrigger.Conditions>
        {conditionsText}
    </MultiTrigger.Conditions>
    {setters.Count} setter(s)
</MultiTrigger>

Avalonia Approaches:

1. Multi-binding with converter (recommended for data conditions):
   Install a multi-value converter that returns true when all conditions are met:
   <Control.IsVisible>
       <MultiBinding Converter=""{{StaticResource AllTrueConverter}}"">
           <Binding Path=""Condition1"" />
           <Binding Path=""Condition2"" />
       </MultiBinding>
   </Control.IsVisible>

2. Avalonia.Xaml.Interactivity (for complex logic):
   Install Avalonia.Xaml.Interactivity package and use DataTriggerBehavior with conditions.

3. Custom attached properties:
   Create an attached property that evaluates multiple conditions and applies a class.

Note: MultiTrigger with non-standard conditions has no direct Avalonia equivalent.";

        element.AddDiagnostic(
            "MULTITRIGGER_BEHAVIOR_PATTERN",
            suggestion,
            Core.Diagnostics.DiagnosticSeverity.Warning);

        context.RecordTransformation(
            Name,
            "MultiTrigger",
            $"MultiTrigger with {conditions.Count} unmappable conditions requires behavior pattern");
    }

    private void AddBasicWarning(UnifiedXamlElement element, TransformationContext context)
    {
        element.AddDiagnostic(
            "MULTITRIGGER_NOT_SUPPORTED",
            "MultiTrigger is not directly supported in Avalonia. Consider using composite Style Selectors for property-based conditions, or Avalonia.Xaml.Interactivity for data-based conditions.",
            Core.Diagnostics.DiagnosticSeverity.Warning);
    }

    private class PropertyValueComparer : IEqualityComparer<(string Property, string Value)>
    {
        public bool Equals((string Property, string Value) x, (string Property, string Value) y)
        {
            return string.Equals(x.Property, y.Property, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(x.Value, y.Value, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode((string Property, string Value) obj)
        {
            return HashCode.Combine(
                obj.Property.ToLowerInvariant(),
                obj.Value.ToLowerInvariant());
        }
    }
}

/// <summary>
/// Transforms WPF VisualStateManager to Avalonia-compatible patterns.
/// WPF uses VisualStateManager for complex state-based UI changes.
/// Avalonia uses style classes, pseudoclasses, and transitions for similar functionality.
/// </summary>
public sealed class VisualStateManagerTransformer : ElementTransformationRuleBase
{
    public override string Name => "VisualStateManagerToAvaloniaStyles";
    public override int Priority => 200;

    public override bool CanTransformElement(UnifiedXamlElement element)
    {
        return element.TypeName == "VisualStateManager.VisualStateGroups" ||
               element.TypeName == "VisualStateGroup" ||
               element.TypeName == "VisualState";
    }

    public override UnifiedXamlElement? TransformElement(UnifiedXamlElement element, TransformationContext context)
    {
        if (element.TypeName == "VisualStateManager.VisualStateGroups")
        {
            TransformVisualStateGroups(element, context);
        }
        else if (element.TypeName == "VisualStateGroup")
        {
            TransformVisualStateGroup(element, context);
        }
        else if (element.TypeName == "VisualState")
        {
            TransformVisualState(element, context);
        }

        return element;
    }

    private void TransformVisualStateGroups(UnifiedXamlElement element, TransformationContext context)
    {
        var groups = element.Children.Where(c => c.TypeName == "VisualStateGroup").ToList();

        if (groups.Count == 0)
        {
            AddBasicWarning(element, context);
            return;
        }

        // Analyze all groups and provide comprehensive migration guidance
        var allStates = groups.SelectMany(g => GetVisualStates(g)).ToList();
        var groupNames = groups.Select(g => g.XName ?? "Unknown").ToList();

        AddComprehensiveGuidance(element, context, groups, allStates, groupNames);
    }

    private void TransformVisualStateGroup(UnifiedXamlElement element, TransformationContext context)
    {
        var groupName = element.XName;

        // Try to find states in multiple locations (similar to MultiTrigger.Conditions pattern)
        var states = GetVisualStates(element);
        var transitions = GetVisualTransitions(element);

        if (states.Count == 0)
        {
            AddBasicWarning(element, context);
            return;
        }

        // Check if this is a common state group that can be mapped to pseudoclasses
        var pseudoclassMapping = TryMapToPseudoclasses(groupName, states);

        if (pseudoclassMapping != null)
        {
            AddPseudoclassSuggestion(element, context, groupName, states, pseudoclassMapping);
        }
        else
        {
            AddStyleClassSuggestion(element, context, groupName, states, transitions);
        }
    }

    private List<UnifiedXamlElement> GetVisualStates(UnifiedXamlElement element)
    {
        // Try direct children first
        var states = element.Children.Where(c => c.TypeName == "VisualState").ToList();

        // Check VisualStateGroup.States property element
        if (states.Count == 0)
        {
            var statesProperty = element.Children.FirstOrDefault(
                c => c.TypeName == "VisualStateGroup.States");
            if (statesProperty != null)
            {
                states = statesProperty.Children
                    .Where(c => c.TypeName == "VisualState").ToList();
            }
        }

        // Check States property value
        if (states.Count == 0)
        {
            var statesProperty = element.Properties
                .FirstOrDefault(p => p.PropertyName == "States");
            if (statesProperty?.Value is UnifiedXamlElement statesElement)
            {
                states = statesElement.Children
                    .Where(c => c.TypeName == "VisualState").ToList();
            }
        }

        return states;
    }

    private List<UnifiedXamlElement> GetVisualTransitions(UnifiedXamlElement element)
    {
        // Try direct children first
        var transitions = element.Children.Where(c => c.TypeName == "VisualTransition").ToList();

        // Check VisualStateGroup.Transitions property element
        if (transitions.Count == 0)
        {
            var transitionsProperty = element.Children.FirstOrDefault(
                c => c.TypeName == "VisualStateGroup.Transitions");
            if (transitionsProperty != null)
            {
                transitions = transitionsProperty.Children
                    .Where(c => c.TypeName == "VisualTransition").ToList();
            }
        }

        // Check Transitions property value
        if (transitions.Count == 0)
        {
            var transitionsProperty = element.Properties
                .FirstOrDefault(p => p.PropertyName == "Transitions");
            if (transitionsProperty?.Value is UnifiedXamlElement transitionsElement)
            {
                transitions = transitionsElement.Children
                    .Where(c => c.TypeName == "VisualTransition").ToList();
            }
        }

        return transitions;
    }

    private void TransformVisualState(UnifiedXamlElement element, TransformationContext context)
    {
        var stateName = element.XName;
        var storyboard = element.Children.FirstOrDefault(c => c.TypeName == "Storyboard");

        if (string.IsNullOrEmpty(stateName))
        {
            AddBasicWarning(element, context);
            return;
        }

        // Analyze the storyboard to understand what properties are being animated
        var setters = new List<string>();
        if (storyboard != null)
        {
            foreach (var animation in storyboard.Children)
            {
                var targetProperty = animation.Properties
                    .FirstOrDefault(p => p.PropertyName == "Storyboard.TargetProperty" || p.PropertyName == "TargetProperty")
                    ?.GetStringValue();

                if (!string.IsNullOrEmpty(targetProperty))
                {
                    setters.Add(targetProperty);
                }
            }
        }

        AddIndividualStateSuggestion(element, context, stateName, setters);
    }

    private Dictionary<string, string>? TryMapToPseudoclasses(string? groupName, List<UnifiedXamlElement> states)
    {
        if (string.IsNullOrEmpty(groupName))
            return null;

        // Common state groups that map to pseudoclasses
        var commonStateGroups = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            {
                "CommonStates", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "Normal", "default state (no pseudoclass)" },
                    { "MouseOver", ":pointerover" },
                    { "Pressed", ":pressed" },
                    { "Disabled", ":disabled" }
                }
            },
            {
                "FocusStates", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "Focused", ":focus" },
                    { "Unfocused", "default state (no pseudoclass)" },
                    { "FocusedDropDown", ":focus" }
                }
            },
            {
                "SelectionStates", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "Selected", ":selected" },
                    { "Unselected", "default state (no pseudoclass)" },
                    { "SelectedInactive", ":selected:not(:focus)" },
                    { "SelectedUnfocused", ":selected:not(:focus)" }
                }
            },
            {
                "CheckStates", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "Checked", ":checked" },
                    { "Unchecked", ":unchecked" },
                    { "Indeterminate", ":indeterminate" }
                }
            }
        };

        if (commonStateGroups.TryGetValue(groupName, out var mappings))
        {
            // Verify that the states in this group match the expected states
            var stateNames = states.Select(s => s.XName).Where(n => !string.IsNullOrEmpty(n)).ToList();

            // If most states match, return the mapping
            var matchingStates = stateNames.Count(name => mappings.ContainsKey(name!));
            if (matchingStates >= stateNames.Count * 0.5) // At least 50% match
            {
                return mappings;
            }
        }

        return null;
    }

    private void AddPseudoclassSuggestion(
        UnifiedXamlElement element,
        TransformationContext context,
        string? groupName,
        List<UnifiedXamlElement> states,
        Dictionary<string, string> pseudoclassMapping)
    {
        var stateExamples = string.Join("\n", states.Select(state =>
        {
            var stateName = state.XName;
            var pseudoclass = pseudoclassMapping.TryGetValue(stateName ?? "", out var pc) ? pc : "custom class";
            return $"  - {stateName} → {pseudoclass}";
        }));

        var suggestion = $@"VisualStateGroup '{groupName}' can be converted to Avalonia pseudoclass styles:

WPF VisualStateManager:
<VisualStateManager.VisualStateGroups>
    <VisualStateGroup x:Name=""{groupName}"">
        {states.Count} state(s)
    </VisualStateGroup>
</VisualStateManager.VisualStateGroups>

Avalonia Pseudoclass Styles:
{stateExamples}

Implementation approach:
1. Create Style with Selector using pseudoclasses
2. Use Setters for property changes (replaces Storyboard)
3. Use Transitions for smooth state changes

Example:
<Style Selector=""Control:pointerover"">
    <Setter Property=""Background"" Value=""LightBlue"" />
</Style>

<Style Selector=""Control"">
    <Style.Animations>
        <Animation Duration=""0:0:0.2"">
            <!-- Define transition animation -->
        </Animation>
    </Style.Animations>
</Style>

Note: WPF's VisualStateManager is more imperative, while Avalonia uses declarative pseudoclasses.";

        element.AddDiagnostic(
            "VSM_PSEUDOCLASS_PATTERN",
            suggestion,
            Core.Diagnostics.DiagnosticSeverity.Info);

        context.RecordTransformation(
            Name,
            "VisualStateGroup",
            $"VisualStateGroup '{groupName}' can be replaced with Avalonia pseudoclass styles");
    }

    private void AddStyleClassSuggestion(
        UnifiedXamlElement element,
        TransformationContext context,
        string? groupName,
        List<UnifiedXamlElement> states,
        List<UnifiedXamlElement> transitions)
    {
        var stateNames = string.Join(", ", states.Select(s => s.XName ?? "Unknown"));

        var suggestion = $@"VisualStateGroup '{groupName}' requires Avalonia style classes pattern:

WPF VisualStateManager:
<VisualStateGroup x:Name=""{groupName}"">
    States: {stateNames}
    Transitions: {transitions.Count}
</VisualStateGroup>

Avalonia Style Classes Approach:

1. Define styles for each state using class selectors:
   <Style Selector=""Control.state-name"">
       <Setter Property=""..."" Value=""..."" />
   </Style>

2. Programmatically add/remove classes in code-behind:
   control.Classes.Add(""state-name"");
   control.Classes.Remove(""old-state"");

3. For transitions, use Avalonia Transitions:
   <Control>
       <Control.Transitions>
           <Transitions>
               <DoubleTransition Property=""Opacity"" Duration=""0:0:0.3"" />
           </Transitions>
       </Control.Transitions>
   </Control>

4. Alternative: Use Avalonia.Xaml.Interactivity behaviors
   Install: Avalonia.Xaml.Interactivity
   Use DataTriggerBehavior to change classes based on data

Example code-behind for state changes:
private void GoToState(string stateName)
{{
    // Remove all state classes
    control.Classes.RemoveAll(c => c.StartsWith(""state-""));
    // Add new state class
    control.Classes.Add($""state-{{stateName}}"");
}}

Note: Avalonia doesn't have a direct VisualStateManager equivalent. Style classes provide similar functionality.";

        element.AddDiagnostic(
            "VSM_STYLECLASS_PATTERN",
            suggestion,
            Core.Diagnostics.DiagnosticSeverity.Warning);

        context.RecordTransformation(
            Name,
            "VisualStateGroup",
            $"VisualStateGroup '{groupName}' requires manual conversion to Avalonia style classes");
    }

    private void AddIndividualStateSuggestion(
        UnifiedXamlElement element,
        TransformationContext context,
        string stateName,
        List<string> setters)
    {
        var propertiesText = setters.Count > 0
            ? string.Join(", ", setters)
            : "no properties identified";

        var suggestion = $@"VisualState '{stateName}' transformation:

WPF VisualState animates: {propertiesText}

Avalonia approaches:
1. Style class: Create '.state-{stateName.ToLowerInvariant()}' class selector
2. Pseudoclass: Map to built-in pseudoclass if applicable
3. Code-behind: Programmatically set properties

Example style class:
<Style Selector=""Control.state-{stateName.ToLowerInvariant()}"">
    {string.Join("\n    ", setters.Select(p => $"<Setter Property=\"{p}\" Value=\"...\" />"))}
</Style>

To activate: control.Classes.Add(""state-{stateName.ToLowerInvariant()}"");";

        element.AddDiagnostic(
            "VSM_STATE_INFO",
            suggestion,
            Core.Diagnostics.DiagnosticSeverity.Info);
    }

    private void AddComprehensiveGuidance(
        UnifiedXamlElement element,
        TransformationContext context,
        List<UnifiedXamlElement> groups,
        List<UnifiedXamlElement> allStates,
        List<string> groupNames)
    {
        var groupSummary = string.Join("\n", groupNames.Select((name, i) =>
        {
            var stateCount = GetVisualStates(groups[i]).Count;
            return $"  - {name}: {stateCount} state(s)";
        }));

        var suggestion = $@"VisualStateManager migration guidance:

WPF VisualStateManager structure:
{groups.Count} group(s):
{groupSummary}

Total: {allStates.Count} visual state(s)

Avalonia Migration Strategy:

1. **Common State Groups** (CommonStates, FocusStates, etc.):
   → Convert to Avalonia pseudoclass styles (:pointerover, :focus, :pressed, etc.)
   → Use Style Selector with pseudoclasses
   → Add Transitions for smooth animations

2. **Custom State Groups**:
   → Use Avalonia style classes (.my-state-name)
   → Implement state changes in code-behind via Classes.Add/Remove
   → Consider Avalonia.Xaml.Interactivity for data-driven state changes

3. **Transitions**:
   → WPF VisualTransitions → Avalonia Transitions property
   → Define DoubleTransition, ColorTransition, etc.
   → Smoother than WPF Storyboards in many cases

4. **GoToState Actions**:
   → Replace VisualStateManager.GoToState() calls with:
     - control.Classes.Add(""state-name"") for style classes
     - Relies on pseudoclasses automatically for built-in states

Resources:
- Avalonia Styling: https://docs.avaloniaui.net/docs/styling/
- Avalonia Transitions: https://docs.avaloniaui.net/docs/animations/transitions
- Style Classes: https://docs.avaloniaui.net/docs/styling/styles#style-classes

Note: Manual review required. VisualStateManager has no direct Avalonia equivalent.
Each state group should be evaluated individually for the best migration approach.";

        element.AddDiagnostic(
            "VSM_COMPREHENSIVE_GUIDE",
            suggestion,
            Core.Diagnostics.DiagnosticSeverity.Warning);

        context.RecordTransformation(
            Name,
            "VisualStateManager.VisualStateGroups",
            $"VisualStateManager with {groups.Count} group(s) requires manual conversion to Avalonia patterns");
    }

    private void AddBasicWarning(UnifiedXamlElement element, TransformationContext context)
    {
        element.AddDiagnostic(
            "VSM_NOT_SUPPORTED",
            "VisualStateManager is not directly supported in Avalonia. Use pseudoclasses for common states, or style classes with code-behind for custom states. Consider Avalonia.Xaml.Interactivity for complex state management.",
            Core.Diagnostics.DiagnosticSeverity.Warning);
    }
}
/// <summary>
/// Cleanup rule that removes converted triggers after they've been transformed to style selectors.
/// This runs with the lowest priority to ensure it executes after all other transformations.
/// </summary>
public sealed class ConvertedTriggerCleanupRule : ElementTransformationRuleBase
{
    public override string Name => "CleanupConvertedTriggers";

    public override int Priority => 1; // Lowest priority - runs last

    public override bool CanTransformElement(UnifiedXamlElement element)
    {
        // Remove any element that was marked as converted to pseudoclass
        return element.Metadata.ContainsKey("ConvertedToPseudoclass");
    }

    public override UnifiedXamlElement? TransformElement(UnifiedXamlElement element, TransformationContext context)
    {
        context.RecordTransformation(
            Name,
            element.TypeName,
            $"Removed converted {element.TypeName} (already transformed to style selector)");

        // Return null to remove this element from the tree
        return null;
    }
}

/// <summary>
/// Transforms WPF Styles with ControlTemplates to Avalonia ControlThemes.
/// WPF uses Style elements with ControlTemplate setters, while Avalonia 11.0+ uses ControlTheme.
/// </summary>
public sealed class StyleToControlThemeTransformer : ElementTransformationRuleBase
{
    public override string Name => "StyleToControlTheme";
    public override int Priority => 150; // Lower priority than other transformers to run after basic style processing

    public override bool CanTransformElement(UnifiedXamlElement element)
    {
        // Only transform Style elements
        return element.TypeName == "Style";
    }

    public override UnifiedXamlElement? TransformElement(UnifiedXamlElement element, TransformationContext context)
    {
        // Analyze the style structure
        var targetType = GetTargetType(element);
        var key = element.XKey;
        var basedOn = GetBasedOn(element);
        var setters = GetSetters(element);
        var triggers = GetTriggers(element);

        // Check if this style has a ControlTemplate
        var hasControlTemplate = HasControlTemplate(setters);

        if (!hasControlTemplate)
        {
            // Not a theme-style Style, skip
            return element;
        }

        // Provide guidance for converting to ControlTheme
        AddControlThemeGuidance(element, context, targetType, key, basedOn, setters, triggers);

        return element;
    }

    private bool HasControlTemplate(List<UnifiedXamlElement> setters)
    {
        // Check if any setter has a Template property
        return setters.Any(setter =>
        {
            var propertyName = setter.Properties.FirstOrDefault(p => p.PropertyName == "Property")?.GetStringValue();
            if (propertyName == "Template")
            {
                // Check if the value is a ControlTemplate (direct child)
                var templateElement = setter.Children.FirstOrDefault(c => c.TypeName == "ControlTemplate");
                if (templateElement != null)
                    return true;

                // Check if the value is in a Setter.Value property element
                var setterValue = setter.Children.FirstOrDefault(c => c.TypeName == "Setter.Value");
                if (setterValue != null)
                {
                    templateElement = setterValue.Children.FirstOrDefault(c => c.TypeName == "ControlTemplate");
                    return templateElement != null;
                }

                // Check the Value property itself (might be stored as property value)
                var valueProp = setter.Properties.FirstOrDefault(p => p.PropertyName == "Value");
                if (valueProp?.Value is UnifiedXamlElement valueElement && valueElement.TypeName == "ControlTemplate")
                {
                    return true;
                }
            }
            return false;
        });
    }

    private string? GetTargetType(UnifiedXamlElement styleElement)
    {
        var targetTypeProp = styleElement.Properties.FirstOrDefault(p => p.PropertyName == "TargetType");
        if (targetTypeProp != null)
        {
            // Handle both direct string values and x:Type markup extensions
            if (targetTypeProp.HasMarkupExtension && targetTypeProp.MarkupExtension?.ExtensionName == "x:Type")
            {
                return targetTypeProp.MarkupExtension.PositionalArgument?.ToString();
            }
            return targetTypeProp.GetStringValue();
        }
        return null;
    }

    private string? GetBasedOn(UnifiedXamlElement styleElement)
    {
        var basedOnProp = styleElement.Properties.FirstOrDefault(p => p.PropertyName == "BasedOn");
        return basedOnProp?.GetStringValue();
    }

    private List<UnifiedXamlElement> GetSetters(UnifiedXamlElement styleElement)
    {
        var setters = new List<UnifiedXamlElement>();

        // Direct children
        setters.AddRange(styleElement.Children.Where(c => c.TypeName == "Setter"));

        // Style.Setters property element
        var settersProperty = styleElement.Children.FirstOrDefault(c => c.TypeName == "Style.Setters");
        if (settersProperty != null)
        {
            setters.AddRange(settersProperty.Children.Where(c => c.TypeName == "Setter"));
        }

        return setters;
    }

    private List<UnifiedXamlElement> GetTriggers(UnifiedXamlElement styleElement)
    {
        var triggers = new List<UnifiedXamlElement>();

        // Style.Triggers property element
        var triggersProperty = styleElement.Children.FirstOrDefault(c => c.TypeName == "Style.Triggers");
        if (triggersProperty != null)
        {
            triggers.AddRange(triggersProperty.Children);
        }

        return triggers;
    }

    private void AddControlThemeGuidance(
        UnifiedXamlElement element,
        TransformationContext context,
        string? targetType,
        string? key,
        string? basedOn,
        List<UnifiedXamlElement> setters,
        List<UnifiedXamlElement> triggers)
    {
        var hasBasedOn = !string.IsNullOrEmpty(basedOn);
        var hasTriggers = triggers.Count > 0;
        var nonTemplateSetters = setters.Where(s =>
        {
            var prop = s.Properties.FirstOrDefault(p => p.PropertyName == "Property")?.GetStringValue();
            return prop != "Template";
        }).ToList();

        var keyUsage = string.IsNullOrEmpty(key)
            ? "x:Key=\"{x:Type ControlType}\" (applies to all instances)"
            : $"x:Key=\"{key}\" (use Theme=\"{{StaticResource {key}}}\" to apply)";

        var setterExamples = string.Join("\n    ", nonTemplateSetters.Take(3).Select(s =>
        {
            var prop = s.Properties.FirstOrDefault(p => p.PropertyName == "Property")?.GetStringValue();
            var val = s.Properties.FirstOrDefault(p => p.PropertyName == "Value")?.GetStringValue();
            return $"<Setter Property=\"{prop}\" Value=\"{val}\" />";
        }));

        var suggestion = $@"WPF Style with ControlTemplate should be converted to Avalonia ControlTheme:

WPF Pattern:
<Style TargetType=""{targetType}""{(string.IsNullOrEmpty(key) ? "" : $" x:Key=\"{key}\"")}>
    {nonTemplateSetters.Count} property setter(s)
    1 ControlTemplate
    {triggers.Count} trigger(s){(hasTriggers ? " (will need migration)" : "")}
</Style>

Avalonia ControlTheme Pattern:
<ControlTheme {keyUsage}
              TargetType=""{targetType}""{(hasBasedOn ? $"\n              BasedOn=\"{basedOn}\"" : "")}>
    <!-- Property setters (same as WPF) -->
    {setterExamples}{(nonTemplateSetters.Count > 3 ? $"\n    <!-- ... {nonTemplateSetters.Count - 3} more setter(s) -->" : "")}
    
    <!-- Template setter -->
    <Setter Property=""Template"">
        <ControlTemplate>
            <!-- Template content (requires manual review) -->
            <!-- Use TemplateBinding for control properties -->
            <!-- Nested styles use ^ selector for control states -->
        </ControlTemplate>
    </Setter>
    
    {(hasTriggers ? @"<!-- Triggers must be converted to nested Style elements -->
    <Style Selector=""^:pointerover"">
        <Setter Property=""..."" Value=""..."" />
    </Style>" : "")}
</ControlTheme>

Key Differences:
1. ControlTheme instead of Style
2. No Selector property (use TargetType)
3. Stored in Resources, not Styles collection
4. Applied via Theme property: <Button Theme=""{{StaticResource MyTheme}}"" />
5. Triggers → Nested Style elements with ^ selector
6. Use x:Type as key to apply to all instances of the control

Migration Steps:
1. Change <Style> to <ControlTheme>
2. Remove any Selector attribute
3. Move to ResourceDictionary (not Styles collection)
4. Convert triggers to nested styles with pseudoclass selectors
5. Update control usages to use Theme property
6. Verify TemplateBinding usage in template

Note: ControlTheme is the recommended approach in Avalonia 11.0+";

        element.AddDiagnostic(
            "STYLE_TO_CONTROLTHEME",
            suggestion,
            Core.Diagnostics.DiagnosticSeverity.Info);

        context.RecordTransformation(
            Name,
            "Style",
            $"Style with ControlTemplate (TargetType={targetType}) should be converted to ControlTheme");
    }
}
