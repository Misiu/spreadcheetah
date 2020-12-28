using Microsoft.CodeAnalysis;
using SpreadCheetah.SourceGenerator.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpreadCheetah.SourceGenerator
{
    [Generator]
    public class RowCellsGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxReceiver is not SyntaxReceiver syntaxReceiver)
                throw new InvalidOperationException("We were given the wrong syntax receiver.");

            var classesToValidate = GetClassPropertiesInfo(context.Compilation, syntaxReceiver.ArgumentsToValidate);

            var sb = new StringBuilder();
            GenerateValidator(sb, classesToValidate);
            context.AddSource("SpreadsheetExtensions.cs", sb.ToString());

            ReportDiagnostics(context, classesToValidate);
        }

        private static void ReportDiagnostics(GeneratorExecutionContext context, IEnumerable<ClassPropertiesInfo> infos)
        {
            foreach (var info in infos)
            {
                if (info.PropertyNames.Count == 0)
                    context.ReportDiagnostics(Diagnostics.NoPropertiesFound, info.Locations, info.ClassType);

                if (info.UnsupportedPropertyNames.FirstOrDefault() is { } unsupportedProperty)
                    context.ReportDiagnostics(Diagnostics.UnsupportedTypeForCellValue, info.Locations, info.ClassType, unsupportedProperty);
            }
        }

        private static void GenerateValidator(StringBuilder sb, ICollection<ClassPropertiesInfo> infos)
        {
            sb.AppendLine(
@"// <auto-generated />
using System.Threading;
using System.Threading.Tasks;

namespace SpreadCheetah
{
    public static class SpreadsheetExtensions
    {");

            const string indent = "        ";

            if (infos.Count == 0)
                WriteStub(sb, indent);
            else
                WriteMethods(sb, indent, infos);

            sb.AppendLine(
@"    }
}
");
        }

        private static void WriteSummary(StringBuilder sb, string indent)
        {
            sb.Append(indent).AppendLine("/// <summary>");
            sb.Append(indent).AppendLine("/// Add object as a row in the active worksheet.");
            sb.Append(indent).AppendLine("/// Each property with a public getter on the object will be added as a cell in the row.");
            sb.Append(indent).AppendLine("/// The method is generated by a source generator.");
            sb.Append(indent).AppendLine("/// </summary>");
        }

        private static void WriteStub(StringBuilder sb, string indent)
        {
            WriteSummary(sb, indent);
            sb.Append(indent).AppendLine("public static ValueTask AddAsRowAsync(this Spreadsheet spreadsheet, object obj, CancellationToken token = default)");
            sb.Append(indent).AppendLine("{");
            sb.Append(indent).AppendLine("    // This will be filled in by the generator once you call AddAsRowAsync()");
            sb.Append(indent).AppendLine("    return new ValueTask();");
            sb.Append(indent).AppendLine("}");
        }

        private static void WriteMethods(StringBuilder sb, string indent, IEnumerable<ClassPropertiesInfo> infos)
        {
            foreach (var info in infos)
            {
                WriteSummary(sb, indent);
                sb.Append(indent).AppendLine($"public static async ValueTask AddAsRowAsync(this Spreadsheet spreadsheet, {info.ClassType} obj, CancellationToken token = default)");
                sb.Append(indent).AppendLine("{");

                GenerateValidateMethodBody(sb, info, indent + "    ");

                sb.Append(indent).AppendLine("}");
            }
        }

        private static void GenerateValidateMethodBody(StringBuilder sb, ClassPropertiesInfo info, string indent)
        {
            if (info.PropertyNames.Count > 0)
            {
                sb.Append(indent).AppendLine("var cells = new[]");
                sb.Append(indent).AppendLine("{");

                var innerIndent = indent + "    ";
                foreach (var propertyName in info.PropertyNames)
                {
                    sb.Append(innerIndent).AppendLine($"new DataCell(obj.{propertyName}),");
                }

                sb.Append(indent).AppendLine("};");
            }
            else
            {
                sb.Append(indent).AppendLine("var cells = System.Array.Empty<DataCell>();");
            }

            sb.Append(indent).AppendLine("await spreadsheet.AddRowAsync(cells, token).ConfigureAwait(false);");
        }

        private static ICollection<ClassPropertiesInfo> GetClassPropertiesInfo(Compilation compilation, List<SyntaxNode> argumentsToValidate)
        {
            var foundTypes = new Dictionary<ITypeSymbol, ClassPropertiesInfo>(SymbolEqualityComparer.Default);

            foreach (var argument in argumentsToValidate)
            {
                var semanticModel = compilation.GetSemanticModel(argument.SyntaxTree);

                var classType = semanticModel.GetTypeInfo(argument).Type;
                if (classType is null)
                    continue;

                if (!foundTypes.TryGetValue(classType, out var info))
                {
                    info = ClassPropertiesInfo.CreateFrom(compilation, classType);
                    foundTypes.Add(classType, info);
                }

                info.Locations.Add(argument.GetLocation());
            }

            return foundTypes.Values;
        }
    }
}
