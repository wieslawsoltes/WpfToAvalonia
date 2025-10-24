using WpfToAvalonia.XamlParser.UnifiedAst;

namespace WpfToAvalonia.XamlParser.Transformers;

/// <summary>
/// Transforms WPF event handlers to Avalonia event handlers.
/// </summary>
/// <remarks>
/// Maps WPF routed events to Avalonia events. Most events have similar names but some differences:
/// - Most UI events are the same: Click, MouseDown, MouseUp, KeyDown, KeyUp, etc.
/// - Some events have different names or don't exist in Avalonia
/// - Avalonia uses tunneling/bubbling events similar to WPF
/// - Attached events work similarly but may have different owners
/// </remarks>
public class EventTransformer : IXamlTransformer
{
    public string Name => "EventTransformer";
    public int Priority => 38; // Run after attached properties, before bindings

    // Maps WPF event names to Avalonia event names (only when different)
    private static readonly Dictionary<string, EventMapping> EventMappings = new()
    {
        // Window events
        { "Closing", new EventMapping("Closing", "System.ComponentModel.CancelEventArgs") },
        { "Closed", new EventMapping("Closed") },

        // Mouse events (mostly same, but some differences)
        { "MouseLeftButtonDown", new EventMapping("PointerPressed", "Avalonia.Input.PointerPressedEventArgs",
            "Use PointerPressed with e.GetCurrentPoint(this).Properties.IsLeftButtonPressed") },
        { "MouseLeftButtonUp", new EventMapping("PointerReleased", "Avalonia.Input.PointerReleasedEventArgs",
            "Use PointerReleased with e.GetCurrentPoint(this).Properties.IsLeftButtonPressed") },
        { "MouseRightButtonDown", new EventMapping("PointerPressed", "Avalonia.Input.PointerPressedEventArgs",
            "Use PointerPressed with e.GetCurrentPoint(this).Properties.IsRightButtonPressed") },
        { "MouseRightButtonUp", new EventMapping("PointerReleased", "Avalonia.Input.PointerReleasedEventArgs",
            "Use PointerReleased with e.GetCurrentPoint(this).Properties.IsRightButtonPressed") },
        { "MouseMove", new EventMapping("PointerMoved", "Avalonia.Input.PointerEventArgs") },
        { "MouseEnter", new EventMapping("PointerEntered", "Avalonia.Input.PointerEventArgs") },
        { "MouseLeave", new EventMapping("PointerExited", "Avalonia.Input.PointerEventArgs") },
        { "MouseWheel", new EventMapping("PointerWheelChanged", "Avalonia.Input.PointerWheelEventArgs") },

        // Touch events → Pointer events
        { "TouchDown", new EventMapping("PointerPressed", "Avalonia.Input.PointerPressedEventArgs",
            "Touch events use Pointer events in Avalonia") },
        { "TouchUp", new EventMapping("PointerReleased", "Avalonia.Input.PointerReleasedEventArgs",
            "Touch events use Pointer events in Avalonia") },
        { "TouchMove", new EventMapping("PointerMoved", "Avalonia.Input.PointerEventArgs",
            "Touch events use Pointer events in Avalonia") },

        // Keyboard events (mostly same)
        { "PreviewKeyDown", new EventMapping("KeyDown", note: "Avalonia uses tunneling events differently") },
        { "PreviewKeyUp", new EventMapping("KeyUp", note: "Avalonia uses tunneling events differently") },

        // Focus events (same names, potentially different behavior)
        { "GotFocus", new EventMapping("GotFocus") },
        { "LostFocus", new EventMapping("LostFocus") },
        { "GotKeyboardFocus", new EventMapping("GotFocus", note: "Avalonia doesn't distinguish keyboard focus") },
        { "LostKeyboardFocus", new EventMapping("LostFocus", note: "Avalonia doesn't distinguish keyboard focus") },

        // Drag and drop (similar but different APIs)
        { "DragEnter", new EventMapping("DragEnter", "Avalonia.Input.DragEventArgs",
            "Drag/drop API is different in Avalonia") },
        { "DragLeave", new EventMapping("DragLeave", "Avalonia.Input.DragEventArgs") },
        { "DragOver", new EventMapping("DragOver", "Avalonia.Input.DragEventArgs") },
        { "Drop", new EventMapping("Drop", "Avalonia.Input.DragEventArgs") },

        // Lifecycle events
        { "Loaded", new EventMapping("AttachedToVisualTree", "Avalonia.VisualTreeAttachmentEventArgs",
            "Loaded → AttachedToVisualTree in Avalonia") },
        { "Unloaded", new EventMapping("DetachedFromVisualTree", "Avalonia.VisualTreeAttachmentEventArgs",
            "Unloaded → DetachedFromVisualTree in Avalonia") },

        // Layout events
        { "SizeChanged", new EventMapping("SizeChanged", "Avalonia.SizeChangedEventArgs") },
        { "LayoutUpdated", new EventMapping("LayoutUpdated", note: "LayoutUpdated exists but behaves differently") },

        // Text events
        { "TextChanged", new EventMapping("TextChanged") },
        { "TextInput", new EventMapping("TextInput", "Avalonia.Input.TextInputEventArgs") },

        // Selection events (mostly same)
        { "SelectionChanged", new EventMapping("SelectionChanged") },

        // Scroll events
        { "ScrollChanged", new EventMapping("ScrollChanged") },

        // Context menu
        { "ContextMenuOpening", new EventMapping("ContextMenuOpening", note: "Similar but different event args") },
        { "ContextMenuClosing", new EventMapping("ContextMenuClosing", note: "Similar but different event args") },
    };

    private static readonly HashSet<string> RemovedEvents = new()
    {
        "PreviewMouseLeftButtonDown",
        "PreviewMouseLeftButtonUp",
        "PreviewMouseRightButtonDown",
        "PreviewMouseRightButtonUp",
        "PreviewTouchDown",
        "PreviewTouchUp",
        "PreviewTouchMove",
        "ManipulationStarting", // Different touch/manipulation API
        "ManipulationStarted",
        "ManipulationDelta",
        "ManipulationCompleted",
        "QueryCursor", // Different cursor handling
    };

    public void Transform(UnifiedXamlDocument document, TransformationContext context)
    {
        if (document.Root == null)
        {
            context.Diagnostics.AddWarning(
                "EVENT_TRANSFORM_NO_ROOT",
                "Document has no root element",
                null);
            return;
        }

        context.Diagnostics.AddInfo(
            "EVENT_TRANSFORM_START",
            "Starting event transformation",
            null);

        // Transform all elements
        TransformElementEvents(document.Root, context);

        foreach (var descendant in document.Root.Descendants())
        {
            TransformElementEvents(descendant, context);
        }

        context.Diagnostics.AddInfo(
            "EVENT_TRANSFORM_COMPLETE",
            $"Event transformation complete",
            null);
    }

    private void TransformElementEvents(UnifiedXamlElement element, TransformationContext context)
    {
        // Find all event handler properties
        // In XAML, events are represented as properties with handler method names as values
        var eventProperties = element.Properties
            .Where(p => p.Kind == PropertyKind.Attribute &&
                       IsEventProperty(p.PropertyName))
            .ToList();

        foreach (var eventProperty in eventProperties)
        {
            TransformEvent(eventProperty, element, context);
        }
    }

    private bool IsEventProperty(string propertyName)
    {
        // Events typically follow naming patterns:
        // - End with common event suffixes
        // - Start with common event prefixes
        // - Are well-known event names

        var eventSuffixes = new[] { "Click", "Changed", "Down", "Up", "Enter", "Leave", "Over" };
        var eventPrefixes = new[] { "On", "Preview" };
        var commonEvents = new[] { "Loaded", "Unloaded", "Closing", "Closed", "Opening", "Opened" };

        return eventSuffixes.Any(suffix => propertyName.EndsWith(suffix)) ||
               eventPrefixes.Any(prefix => propertyName.StartsWith(prefix)) ||
               commonEvents.Contains(propertyName) ||
               EventMappings.ContainsKey(propertyName) ||
               RemovedEvents.Contains(propertyName);
    }

    private void TransformEvent(UnifiedXamlProperty eventProperty, UnifiedXamlElement element, TransformationContext context)
    {
        var wpfEventName = eventProperty.PropertyName;
        var handlerName = eventProperty.Value?.ToString();

        if (string.IsNullOrEmpty(handlerName))
        {
            return;
        }

        // Check if this event is removed in Avalonia
        if (RemovedEvents.Contains(wpfEventName))
        {
            context.Diagnostics.AddWarning(
                "EVENT_REMOVED",
                $"Event '{wpfEventName}' is not available in Avalonia on {element.TypeName}. Handler '{handlerName}' needs to be removed or replaced.",
                null);
            context.Statistics.WarningsGenerated++;
            eventProperty.SetMetadata("Removed", $"No Avalonia equivalent for {wpfEventName}");
            return;
        }

        // Check if we have a mapping for this event
        if (EventMappings.TryGetValue(wpfEventName, out var mapping))
        {
            var avaloniaEventName = mapping.AvaloniaEventName;

            if (wpfEventName != avaloniaEventName)
            {
                // Event name changed
                context.Diagnostics.AddInfo(
                    "EVENT_MAPPED",
                    $"Transforming event: {wpfEventName} → {avaloniaEventName} on {element.TypeName}",
                    null);

                eventProperty.PropertyName = avaloniaEventName;

                if (!string.IsNullOrEmpty(mapping.Note))
                {
                    context.Diagnostics.AddWarning(
                        "EVENT_BEHAVIOR_CHANGE",
                        $"Event '{wpfEventName}' → '{avaloniaEventName}': {mapping.Note}",
                        null);
                }

                if (!string.IsNullOrEmpty(mapping.EventArgsType))
                {
                    context.Diagnostics.AddInfo(
                        "EVENT_ARGS_TYPE",
                        $"Event '{avaloniaEventName}' uses {mapping.EventArgsType}. Update handler signature: void {handlerName}(object sender, {mapping.EventArgsType} e)",
                        null);
                }
            }
            else
            {
                // Event name is the same, but there might be notes about behavior changes
                if (!string.IsNullOrEmpty(mapping.Note))
                {
                    context.Diagnostics.AddWarning(
                        "EVENT_BEHAVIOR_NOTE",
                        $"Event '{wpfEventName}' on {element.TypeName}: {mapping.Note}",
                        null);
                }

                context.Diagnostics.AddInfo(
                    "EVENT_COMPATIBLE",
                    $"Event '{wpfEventName}' is compatible with Avalonia on {element.TypeName}",
                    null);
            }
        }
        else
        {
            // Unknown event - might be compatible, might not be
            context.Diagnostics.AddWarning(
                "EVENT_UNKNOWN",
                $"Unknown event '{wpfEventName}' on {element.TypeName}. Verify if this event exists in Avalonia.",
                null);
        }

        context.Statistics.IncrementCount($"Event:{wpfEventName}");
    }

    private class EventMapping
    {
        public string AvaloniaEventName { get; }
        public string? EventArgsType { get; }
        public string? Note { get; }

        public EventMapping(string avaloniaEventName, string? eventArgsType = null, string? note = null)
        {
            AvaloniaEventName = avaloniaEventName;
            EventArgsType = eventArgsType;
            Note = note;
        }
    }
}
