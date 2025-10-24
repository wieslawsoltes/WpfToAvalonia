using WpfToAvalonia.XamlParser.UnifiedAst;

namespace WpfToAvalonia.XamlParser.Transformation.Rules;

/// <summary>
/// Base class for animation transformation rules.
/// Implements tasks 2.5.7.2.1, 2.5.7.2.2, 2.5.7.2.3, 2.5.7.2.5:
/// - Parse WPF animation elements
/// - Transform to Avalonia animation syntax
/// - Convert storyboards
/// - Map easing functions
/// </summary>
public abstract class AnimationTransformationRuleBase : ElementTransformationRuleBase
{
    protected bool IsAnimationElement(string typeName)
    {
        return typeName.EndsWith("Animation") ||
               typeName.EndsWith("AnimationUsingKeyFrames") ||
               typeName == "Storyboard" ||
               typeName == "BeginStoryboard";
    }
}

/// <summary>
/// Task 2.5.7.2.1 &amp; 2.5.7.2.2: Parse and transform WPF animation elements.
/// Transforms DoubleAnimation, ColorAnimation, etc. to Avalonia equivalents.
/// </summary>
/// <remarks>
/// WPF Animation Types → Avalonia Equivalents:
/// - DoubleAnimation → Animation with DoubleTransition or KeyFrame
/// - ColorAnimation → Animation with ColorTransition or KeyFrame
/// - ThicknessAnimation → Animation with ThicknessTransition or KeyFrame
/// - PointAnimation → Animation with PointTransition or KeyFrame
/// - ObjectAnimationUsingKeyFrames → Animation with KeyFrames
/// - DoubleAnimationUsingKeyFrames → Animation with KeyFrames
/// - ColorAnimationUsingKeyFrames → Animation with KeyFrames
///
/// WPF Pattern:
/// &lt;DoubleAnimation Storyboard.TargetProperty="Opacity" From="0" To="1" Duration="0:0:0.5"/&gt;
///
/// Avalonia Approach 1 (Transitions - simpler):
/// &lt;Control.Transitions&gt;
///     &lt;Transitions&gt;
///         &lt;DoubleTransition Property="Opacity" Duration="0:0:0.5"/&gt;
///     &lt;/Transitions&gt;
/// &lt;/Control.Transitions&gt;
///
/// Avalonia Approach 2 (Animation with KeyFrames - more control):
/// &lt;Style.Animations&gt;
///     &lt;Animation Duration="0:0:0.5"&gt;
///         &lt;KeyFrame Cue="0%"&gt;&lt;Setter Property="Opacity" Value="0"/&gt;&lt;/KeyFrame&gt;
///         &lt;KeyFrame Cue="100%"&gt;&lt;Setter Property="Opacity" Value="1"/&gt;&lt;/KeyFrame&gt;
///     &lt;/Animation&gt;
/// &lt;/Style.Animations&gt;
/// </remarks>
public sealed class AnimationElementTransformationRule : AnimationTransformationRuleBase
{
    public override string Name => "TransformAnimationElement";

    public override int Priority => 150;

    public override bool CanTransformElement(UnifiedXamlElement element)
    {
        var typeName = element.TypeName;
        return typeName == "DoubleAnimation" ||
               typeName == "ColorAnimation" ||
               typeName == "ThicknessAnimation" ||
               typeName == "PointAnimation" ||
               typeName == "ObjectAnimationUsingKeyFrames" ||
               typeName == "DoubleAnimationUsingKeyFrames" ||
               typeName == "ColorAnimationUsingKeyFrames" ||
               typeName == "ThicknessAnimationUsingKeyFrames";
    }

    public override UnifiedXamlElement? TransformElement(UnifiedXamlElement element, TransformationContext context)
    {
        var animationType = element.TypeName;
        var targetProperty = GetTargetProperty(element);
        var from = element.Properties.FirstOrDefault(p => p.PropertyName == "From")?.GetStringValue();
        var to = element.Properties.FirstOrDefault(p => p.PropertyName == "To")?.GetStringValue();
        var duration = element.Properties.FirstOrDefault(p => p.PropertyName == "Duration")?.GetStringValue();
        var by = element.Properties.FirstOrDefault(p => p.PropertyName == "By")?.GetStringValue();

        // Check for easing function
        var easingElement = element.Children.FirstOrDefault(c => c.TypeName.Contains("Easing"));
        var easingFunction = easingElement?.TypeName;

        var avaloniaTransitionType = GetAvaloniaTransitionType(animationType);
        var avaloniaEasing = easingFunction != null ? MapEasingFunction(easingFunction) : null;

        // Build migration guidance
        var guidance = BuildAnimationGuidance(
            animationType, targetProperty, from, to, duration, by,
            avaloniaTransitionType, avaloniaEasing);

        element.AddDiagnostic(
            "WPF_ANIMATION_ELEMENT",
            guidance,
            Core.Diagnostics.DiagnosticSeverity.Info);

        context.RecordTransformation(
            Name,
            animationType,
            $"{animationType} on '{targetProperty}' needs conversion to Avalonia Animation or Transition");

        return element;
    }

    private string? GetTargetProperty(UnifiedXamlElement element)
    {
        return element.Properties.FirstOrDefault(p =>
            p.PropertyName == "Storyboard.TargetProperty" ||
            p.PropertyName == "TargetProperty")?.GetStringValue();
    }

    private string GetAvaloniaTransitionType(string wpfAnimationType)
    {
        return wpfAnimationType switch
        {
            "DoubleAnimation" => "DoubleTransition",
            "ColorAnimation" => "ColorTransition",
            "ThicknessAnimation" => "ThicknessTransition",
            "PointAnimation" => "PointTransition",
            "DoubleAnimationUsingKeyFrames" => "Animation (with KeyFrames)",
            "ColorAnimationUsingKeyFrames" => "Animation (with KeyFrames)",
            "ThicknessAnimationUsingKeyFrames" => "Animation (with KeyFrames)",
            "ObjectAnimationUsingKeyFrames" => "Animation (with KeyFrames)",
            _ => "Transition"
        };
    }

    private string BuildAnimationGuidance(
        string animationType, string? targetProperty, string? from, string? to,
        string? duration, string? by, string avaloniaType, string? easing)
    {
        var prop = targetProperty ?? "Property";
        var dur = duration ?? "0:0:0.5";
        var fromVal = from ?? "0";
        var toVal = to ?? (by != null ? $"CurrentValue + {by}" : "1");

        var guidance = $@"WPF {animationType} detected:
Target Property: {prop}
From: {fromVal}, To: {toVal}, Duration: {dur}
{(easing != null ? $"Easing: {easing}" : "")}

Avalonia Migration Options:

1. Transitions (Recommended for property changes):
<Control.Transitions>
    <Transitions>
        <{avaloniaType} Property=""{prop}"" Duration=""{dur}""{(easing != null ? $" Easing=\"{easing}\"" : "")}/>
    </Transitions>
</Control.Transitions>

2. Style Animations (For state-based animations):
<Style Selector=""Control:somestate"">
    <Style.Animations>
        <Animation Duration=""{dur}""{(easing != null ? $" Easing=\"{easing}\"" : "")}>
            <KeyFrame Cue=""0%""><Setter Property=""{prop}"" Value=""{fromVal}""/></KeyFrame>
            <KeyFrame Cue=""100%""><Setter Property=""{prop}"" Value=""{toVal}""/></KeyFrame>
        </Animation>
    </Style.Animations>
</Style>

3. Code-behind (For programmatic control):
var animation = new Animation
{{
    Duration = TimeSpan.Parse(""{dur}""),
    Children = {{
        new KeyFrame {{ Cue = new Cue(0), Setters = {{ new Setter({prop}Property, {fromVal}) }} }},
        new KeyFrame {{ Cue = new Cue(1), Setters = {{ new Setter({prop}Property, {toVal}) }} }}
    }}
}};
await animation.RunAsync(control);

Note: Avalonia uses different animation model than WPF. Transitions are automatic on property changes.";

        return guidance;
    }

    /// <summary>
    /// Task 2.5.7.2.5: Map WPF easing functions to Avalonia equivalents.
    /// </summary>
    private string MapEasingFunction(string wpfEasing)
    {
        // Remove namespace if present
        var easingName = wpfEasing.Contains('.') ? wpfEasing.Split('.').Last() : wpfEasing;

        return easingName switch
        {
            "QuadraticEase" => "QuadraticEaseInOut",
            "CubicEase" => "CubicEaseInOut",
            "QuarticEase" => "QuarticEaseInOut",
            "QuinticEase" => "QuinticEaseInOut",
            "SineEase" => "SineEaseInOut",
            "ExponentialEase" => "ExponentialEaseInOut",
            "CircleEase" => "CircularEaseInOut",
            "BackEase" => "BackEaseInOut",
            "ElasticEase" => "ElasticEaseInOut",
            "BounceEase" => "BounceEaseInOut",
            "PowerEase" => "CubicEaseInOut", // Approximate
            _ => "LinearEasing"
        };
    }
}

/// <summary>
/// Task 2.5.7.2.3: Convert WPF Storyboard elements to Avalonia animations.
/// </summary>
/// <remarks>
/// WPF Storyboards are complex animation containers that can target multiple properties
/// and multiple elements simultaneously. Avalonia has a simpler model:
///
/// WPF Storyboard:
/// &lt;Storyboard&gt;
///     &lt;DoubleAnimation Storyboard.TargetName="MyButton" Storyboard.TargetProperty="Opacity" To="0.5"/&gt;
///     &lt;ColorAnimation Storyboard.TargetName="MyButton" Storyboard.TargetProperty="Background.Color" To="Red"/&gt;
/// &lt;/Storyboard&gt;
///
/// Avalonia Equivalent (Style Animation):
/// &lt;Style Selector="Button#MyButton"&gt;
///     &lt;Style.Animations&gt;
///         &lt;Animation Duration="0:0:0.3"&gt;
///             &lt;KeyFrame Cue="100%"&gt;
///                 &lt;Setter Property="Opacity" Value="0.5"/&gt;
///                 &lt;Setter Property="Background" Value="Red"/&gt;
///             &lt;/KeyFrame&gt;
///         &lt;/Animation&gt;
///     &lt;/Style.Animations&gt;
/// &lt;/Style&gt;
/// </remarks>
public sealed class StoryboardTransformationRule : AnimationTransformationRuleBase
{
    public override string Name => "TransformStoryboard";

    public override int Priority => 140;

    public override bool CanTransformElement(UnifiedXamlElement element)
    {
        return element.TypeName == "Storyboard" || element.TypeName == "BeginStoryboard";
    }

    public override UnifiedXamlElement? TransformElement(UnifiedXamlElement element, TransformationContext context)
    {
        if (element.TypeName == "BeginStoryboard")
        {
            // BeginStoryboard is just a wrapper in WPF
            element.AddDiagnostic(
                "BEGIN_STORYBOARD_WRAPPER",
                "BeginStoryboard is a WPF-specific wrapper. In Avalonia, animations are defined directly in Style.Animations or used via code-behind.",
                Core.Diagnostics.DiagnosticSeverity.Info);

            context.RecordTransformation(
                Name,
                "BeginStoryboard",
                "BeginStoryboard wrapper needs to be removed for Avalonia");

            return element;
        }

        // Analyze storyboard contents
        var animations = AnalyzeStoryboardAnimations(element);
        var targetNames = animations.Select(a => a.TargetName).Distinct().Where(n => !string.IsNullOrEmpty(n)).ToList();
        var hasMultipleTargets = targetNames.Count > 1;
        var targetName = targetNames.FirstOrDefault();

        var guidance = BuildStoryboardGuidance(animations, targetName, hasMultipleTargets);

        element.AddDiagnostic(
            "STORYBOARD_TRANSFORMATION",
            guidance,
            Core.Diagnostics.DiagnosticSeverity.Info);

        context.RecordTransformation(
            Name,
            "Storyboard",
            $"Storyboard with {animations.Count} animation(s) needs conversion to Avalonia Animation");

        return element;
    }

    private List<StoryboardAnimationInfo> AnalyzeStoryboardAnimations(UnifiedXamlElement storyboard)
    {
        var animations = new List<StoryboardAnimationInfo>();

        foreach (var child in storyboard.Children)
        {
            if (!IsAnimationElement(child.TypeName)) continue;

            var info = new StoryboardAnimationInfo
            {
                AnimationType = child.TypeName,
                TargetName = child.Properties.FirstOrDefault(p => p.PropertyName == "Storyboard.TargetName")?.GetStringValue(),
                TargetProperty = child.Properties.FirstOrDefault(p =>
                    p.PropertyName == "Storyboard.TargetProperty" ||
                    p.PropertyName == "TargetProperty")?.GetStringValue(),
                From = child.Properties.FirstOrDefault(p => p.PropertyName == "From")?.GetStringValue(),
                To = child.Properties.FirstOrDefault(p => p.PropertyName == "To")?.GetStringValue(),
                Duration = child.Properties.FirstOrDefault(p => p.PropertyName == "Duration")?.GetStringValue(),
                BeginTime = child.Properties.FirstOrDefault(p => p.PropertyName == "BeginTime")?.GetStringValue(),
                RepeatBehavior = child.Properties.FirstOrDefault(p => p.PropertyName == "RepeatBehavior")?.GetStringValue(),
                AutoReverse = child.Properties.FirstOrDefault(p => p.PropertyName == "AutoReverse")?.GetStringValue()
            };

            animations.Add(info);
        }

        return animations;
    }

    private string BuildStoryboardGuidance(
        List<StoryboardAnimationInfo> animations,
        string? targetName,
        bool hasMultipleTargets)
    {
        var guidance = $@"WPF Storyboard detected with {animations.Count} animation(s):
{(targetName != null ? $"Target: {targetName}" : "Multiple or no targets")}

Avalonia Migration:

1. Style Animation (Recommended):";

        if (hasMultipleTargets)
        {
            guidance += @"
   Note: Multiple targets require separate Style definitions in Avalonia.
   Create one <Style> per target element.";
        }

        guidance += $@"

<Style Selector=""{(targetName != null ? $"#{targetName}" : "ControlType")}"">
    <Style.Animations>
        <Animation Duration=""{animations.FirstOrDefault()?.Duration ?? "0:0:0.5"}"">
{string.Join("\n", animations.Select(a => $"            <KeyFrame Cue=\"100%\"><Setter Property=\"{a.TargetProperty}\" Value=\"{a.To ?? "value"}\"/></KeyFrame>"))}
        </Animation>
    </Style.Animations>
</Style>

2. Code-behind Animation:
var storyboard = new Animation
{{
    Duration = TimeSpan.FromSeconds(0.5),
    Children = {{
{string.Join(",\n", animations.Select(a => $"        new KeyFrame {{ Cue = new Cue(1), Setters = {{ new Setter({a.TargetProperty}Property, {a.To ?? "value"}) }} }}"))}
    }}
}};
await storyboard.RunAsync(control);

Notes:
- Storyboard.TargetName is replaced with Style Selectors (e.g., #MyControl)
- Complex storyboards may need multiple animations or code-behind
- BeginTime/RepeatBehavior require additional configuration in Avalonia";

        return guidance;
    }

    private class StoryboardAnimationInfo
    {
        public string? AnimationType { get; set; }
        public string? TargetName { get; set; }
        public string? TargetProperty { get; set; }
        public string? From { get; set; }
        public string? To { get; set; }
        public string? Duration { get; set; }
        public string? BeginTime { get; set; }
        public string? RepeatBehavior { get; set; }
        public string? AutoReverse { get; set; }
    }
}

/// <summary>
/// Task 2.5.7.2.5: Transform WPF easing function elements to Avalonia equivalents.
/// </summary>
/// <remarks>
/// WPF Easing Functions → Avalonia Easing:
/// - QuadraticEase → QuadraticEaseInOut, QuadraticEaseIn, QuadraticEaseOut
/// - CubicEase → CubicEaseInOut, CubicEaseIn, CubicEaseOut
/// - QuarticEase → QuarticEaseInOut, QuarticEaseIn, QuarticEaseOut
/// - QuinticEase → QuinticEaseInOut, QuinticEaseIn, QuinticEaseOut
/// - SineEase → SineEaseInOut, SineEaseIn, SineEaseOut
/// - ExponentialEase → ExponentialEaseInOut, ExponentialEaseIn, ExponentialEaseOut
/// - CircleEase → CircularEaseInOut, CircularEaseIn, CircularEaseOut
/// - BackEase → BackEaseInOut, BackEaseIn, BackEaseOut
/// - ElasticEase → ElasticEaseInOut, ElasticEaseIn, ElasticEaseOut
/// - BounceEase → BounceEaseInOut, BounceEaseIn, BounceEaseOut
/// - PowerEase → Approximate with Cubic or custom
///
/// WPF uses EasingMode property (EaseIn, EaseOut, EaseInOut).
/// Avalonia uses separate easing types for each mode.
/// </remarks>
public sealed class EasingFunctionTransformationRule : AnimationTransformationRuleBase
{
    public override string Name => "TransformEasingFunction";

    public override int Priority => 130;

    public override bool CanTransformElement(UnifiedXamlElement element)
    {
        return element.TypeName.EndsWith("Ease") ||
               element.TypeName.Contains("Easing");
    }

    public override UnifiedXamlElement? TransformElement(UnifiedXamlElement element, TransformationContext context)
    {
        var easingType = element.TypeName;
        var easingMode = element.Properties.FirstOrDefault(p => p.PropertyName == "EasingMode")?.GetStringValue();

        var avaloniaEasing = MapEasingFunctionWithMode(easingType, easingMode);

        var guidance = $@"WPF Easing Function: {easingType}
{(easingMode != null ? $"Easing Mode: {easingMode}" : "")}

Avalonia Equivalent: {avaloniaEasing}

Usage in Avalonia:
1. In Animation:
   <Animation Easing=""{avaloniaEasing}"">...</Animation>

2. In Transition:
   <DoubleTransition Easing=""{avaloniaEasing}"" .../>

3. In Code:
   var animation = new Animation {{ Easing = Easing.Parse(""{avaloniaEasing}"") }};

Available Easing Functions in Avalonia:
- LinearEasing
- QuadraticEaseIn, QuadraticEaseOut, QuadraticEaseInOut
- CubicEaseIn, CubicEaseOut, CubicEaseInOut
- QuarticEaseIn, QuarticEaseOut, QuarticEaseInOut
- QuinticEaseIn, QuinticEaseOut, QuinticEaseInOut
- SineEaseIn, SineEaseOut, SineEaseInOut
- ExponentialEaseIn, ExponentialEaseOut, ExponentialEaseInOut
- CircularEaseIn, CircularEaseOut, CircularEaseInOut
- BackEaseIn, BackEaseOut, BackEaseInOut
- ElasticEaseIn, ElasticEaseOut, ElasticEaseInOut
- BounceEaseIn, BounceEaseOut, BounceEaseInOut
- SplineEasing (with custom control points)";

        element.AddDiagnostic(
            "EASING_FUNCTION_TRANSFORM",
            guidance,
            Core.Diagnostics.DiagnosticSeverity.Info);

        context.RecordTransformation(
            Name,
            easingType,
            $"{easingType} ({easingMode}) → {avaloniaEasing}");

        return element;
    }

    private string MapEasingFunctionWithMode(string wpfEasing, string? easingMode)
    {
        // Remove namespace if present
        var easingName = wpfEasing.Contains('.') ? wpfEasing.Split('.').Last() : wpfEasing;

        // Default to EaseInOut if no mode specified
        var mode = easingMode ?? "EaseInOut";

        var baseEasing = easingName switch
        {
            "QuadraticEase" => "Quadratic",
            "CubicEase" => "Cubic",
            "QuarticEase" => "Quartic",
            "QuinticEase" => "Quintic",
            "SineEase" => "Sine",
            "ExponentialEase" => "Exponential",
            "CircleEase" => "Circular",
            "BackEase" => "Back",
            "ElasticEase" => "Elastic",
            "BounceEase" => "Bounce",
            "PowerEase" => "Cubic", // Approximate
            _ => "Linear"
        };

        if (baseEasing == "Linear")
        {
            return "LinearEasing";
        }

        return $"{baseEasing}Ease{mode}";
    }
}
