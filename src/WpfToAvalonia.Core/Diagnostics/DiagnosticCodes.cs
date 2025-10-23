namespace WpfToAvalonia.Core.Diagnostics;

/// <summary>
/// Diagnostic codes used throughout the transformation process.
/// </summary>
public static class DiagnosticCodes
{
    // General codes (WA0001-WA0099)
    public const string GeneralError = "WA0001";
    public const string GeneralWarning = "WA0002";
    public const string GeneralInfo = "WA0003";

    // Namespace transformation codes (WA0100-WA0199)
    public const string NamespaceMappingNotFound = "WA0100";
    public const string NamespaceTransformed = "WA0101";
    public const string AmbiguousNamespaceMapping = "WA0102";

    // Type transformation codes (WA0200-WA0299)
    public const string TypeMappingNotFound = "WA0200";
    public const string TypeTransformed = "WA0201";
    public const string TypeRequiresManualReview = "WA0202";
    public const string TypeNotSupported = "WA0203";

    // Property transformation codes (WA0300-WA0399)
    public const string PropertyMappingNotFound = "WA0300";
    public const string PropertyTransformed = "WA0301";
    public const string PropertyRequiresManualReview = "WA0302";
    public const string PropertyTypeChanged = "WA0303";
    public const string PropertyValueConversionNeeded = "WA0304";

    // Event transformation codes (WA0400-WA0499)
    public const string EventMappingNotFound = "WA0400";
    public const string EventTransformed = "WA0401";
    public const string EventRequiresManualReview = "WA0402";

    // Dependency property codes (WA0500-WA0599)
    public const string DependencyPropertyFound = "WA0500";
    public const string DependencyPropertyTransformed = "WA0501";
    public const string DependencyPropertyRequiresManualReview = "WA0502";
    public const string AttachedPropertyFound = "WA0503";

    // XAML transformation codes (WA0600-WA0699)
    public const string XamlParseError = "WA0600";
    public const string XamlNamespaceTransformed = "WA0601";
    public const string XamlControlTransformed = "WA0602";
    public const string XamlPropertyTransformed = "WA0603";
    public const string XamlBindingTransformed = "WA0604";
    public const string XamlStyleTransformed = "WA0605";
    public const string XamlPropertyTypeChanged = "WA0606";
    public const string XamlTransformationError = "WA0607";

    // Project file codes (WA0700-WA0799)
    public const string ProjectFileTransformed = "WA0700";
    public const string ProjectFileError = "WA0701";
    public const string PackageReferenceAdded = "WA0702";
    public const string PackageReferenceRemoved = "WA0703";

    // File operation codes (WA0800-WA0899)
    public const string FileRenamed = "WA0800";
    public const string FileBackedUp = "WA0801";
    public const string FileTransformed = "WA0802";
    public const string FileSkipped = "WA0803";
    public const string FileError = "WA0804";

    // Compilation codes (WA0900-WA0999)
    public const string CompilationError = "WA0900";
    public const string CompilationWarning = "WA0901";
    public const string SemanticModelError = "WA0902";
}
