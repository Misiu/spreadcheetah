using Microsoft.CodeAnalysis;

namespace SpreadCheetah.SourceGenerator;

internal static class Diagnostics
{
    private const string Category = "SpreadCheetah.SourceGenerator";

    public static readonly DiagnosticDescriptor NoPropertiesFound = new DiagnosticDescriptor(
        id: "SPCH1001",
        title: "Missing properties with public getters",
        messageFormat: "The type '{0}' has no properties with public getters. This will cause an empty row to be added.",
        category: Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnsupportedTypeForCellValue = new DiagnosticDescriptor(
        id: "SPCH1002",
        title: "Unsupported type for cell value",
        messageFormat: "The type '{0}' has a property of type '{1}' which is not supported as a cell value. The property will be ignored when creating the row.",
        category: Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
