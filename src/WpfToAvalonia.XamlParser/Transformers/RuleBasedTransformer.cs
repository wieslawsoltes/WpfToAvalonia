using WpfToAvalonia.XamlParser.UnifiedAst;
using WpfToAvalonia.Core.Diagnostics;
using LegacyTransformationContext = WpfToAvalonia.XamlParser.Transformation.TransformationContext;
using LegacyTransformationOptions = WpfToAvalonia.XamlParser.Transformation.TransformationOptions;
using ITransformationRule = WpfToAvalonia.XamlParser.Transformation.ITransformationRule;

namespace WpfToAvalonia.XamlParser.Transformers;

/// <summary>
/// An IXamlTransformer adapter that applies ITransformationRule-based transformations.
/// This bridges the legacy rule-based transformation system with the modern transformer pipeline.
/// </summary>
public class RuleBasedTransformer : IXamlTransformer
{
    private readonly List<ITransformationRule> _rules;
    private readonly string _name;
    private readonly int _priority;

    /// <summary>
    /// Initializes a new instance of the <see cref="RuleBasedTransformer"/> class.
    /// </summary>
    /// <param name="name">The name of this transformer.</param>
    /// <param name="priority">The execution priority.</param>
    /// <param name="rules">The transformation rules to apply.</param>
    public RuleBasedTransformer(string name, int priority, IEnumerable<ITransformationRule> rules)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _priority = priority;
        _rules = rules?.ToList() ?? throw new ArgumentNullException(nameof(rules));

        // Sort rules by their own priority (higher priority first)
        _rules.Sort((a, b) => b.Priority.CompareTo(a.Priority));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RuleBasedTransformer"/> class.
    /// </summary>
    /// <param name="name">The name of this transformer.</param>
    /// <param name="priority">The execution priority.</param>
    /// <param name="rules">The transformation rules to apply.</param>
    public RuleBasedTransformer(string name, int priority, params ITransformationRule[] rules)
        : this(name, priority, (IEnumerable<ITransformationRule>)rules)
    {
    }

    /// <inheritdoc/>
    public string Name => _name;

    /// <inheritdoc/>
    public int Priority => _priority;

    /// <inheritdoc/>
    public void Transform(UnifiedXamlDocument document, TransformationContext context)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (document.Root == null)
        {
            context.Diagnostics.AddWarning(
                "TRANSFORM_NO_ROOT",
                $"{Name}: Document has no root element",
                document.FilePath);
            return;
        }

        // Apply rules using a visitor pattern
        var visitor = new RuleApplicationVisitor(_rules, document, context);
        visitor.VisitElement(document.Root);
    }

    /// <summary>
    /// Visitor that applies transformation rules to elements and properties.
    /// </summary>
    private class RuleApplicationVisitor
    {
        private readonly List<ITransformationRule> _rules;
        private readonly LegacyTransformationContext _legacyContext;
        private readonly TransformationContext _modernContext;

        public RuleApplicationVisitor(List<ITransformationRule> rules, UnifiedXamlDocument document, TransformationContext modernContext)
        {
            _rules = rules;
            _modernContext = modernContext;
            // Create a legacy context adapter - pass the document being transformed
            var options = new LegacyTransformationOptions();
            _legacyContext = new LegacyTransformationContext(document, options);
        }

        public void VisitElement(UnifiedXamlElement element)
        {
            // Apply rules to the element itself
            ApplyRulesToNode(element);

            // Apply rules to properties (visit property elements and attributes)
            foreach (var property in element.Properties.ToList()) // ToList to avoid modification during iteration
            {
                ApplyRulesToNode(property);

                // If property has a markup extension, apply rules to it
                if (property.MarkupExtension != null)
                {
                    ApplyRulesToNode(property.MarkupExtension);
                }

                // If property value is an element, visit it recursively
                if (property.Value is UnifiedXamlElement propertyValueElement)
                {
                    VisitElement(propertyValueElement);
                }
            }

            // Recursively visit child elements
            foreach (var child in element.Children.ToList()) // ToList to avoid modification during iteration
            {
                VisitElement(child);
            }
        }

        private void ApplyRulesToNode(UnifiedXamlNode node)
        {
            foreach (var rule in _rules)
            {
                if (rule.CanTransform(node))
                {
                    try
                    {
                        var transformed = rule.Transform(node, _legacyContext);
                        // Note: The transform method modifies the node in-place
                        // The return value can be used for node replacement scenarios
                    }
                    catch (Exception ex)
                    {
                        _modernContext.Diagnostics.AddError(
                            "RULE_APPLICATION_ERROR",
                            $"Rule '{rule.Name}' failed: {ex.Message}",
                            null);
                    }
                }
            }
        }
    }
}
