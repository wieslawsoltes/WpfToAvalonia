using Xunit;
using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.XamlParser;
using WpfToAvalonia.XamlParser.SemanticEnrichment;

namespace WpfToAvalonia.Tests;

/// <summary>
/// Tests for semantic enrichment functionality (Task 2.5.4.2).
/// </summary>
public class SemanticEnrichmentTests
{
    [Fact]
    public void SemanticEnricher_CanBeInstantiated()
    {
        // Arrange & Act
        var diagnostics = new DiagnosticCollector();
        var enricher = new SemanticEnricher(diagnostics);

        // Assert
        Assert.NotNull(enricher);
    }

    [Fact]
    public void SemanticModel_CalculatesEnrichmentPercentage()
    {
        // Arrange
        var model = new SemanticModel
        {
            TotalElements = 10,
            EnrichedElements = 7
        };

        // Act
        var percentage = model.EnrichmentPercentage;

        // Assert
        Assert.Equal(70.0, percentage);
    }

    [Fact]
    public void SemanticModel_HandlesZeroElements()
    {
        // Arrange
        var model = new SemanticModel
        {
            TotalElements = 0,
            EnrichedElements = 0
        };

        // Act
        var percentage = model.EnrichmentPercentage;

        // Assert
        Assert.Equal(0.0, percentage);
    }
}
