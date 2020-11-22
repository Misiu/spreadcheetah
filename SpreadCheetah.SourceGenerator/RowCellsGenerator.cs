using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpreadCheetah.SourceGenerator
{
    [Generator]
    public class RowCellsGenerator : ISourceGenerator
    {
        // TODO: Write summary on stub
        private const string AddAsRowStub =
@"// <auto-generated />
using System.Threading;
using System.Threading.Tasks;

namespace SpreadCheetah
{
    public static class SpreadsheetExtensions
    {
        public static async ValueTask AddAsRowAsync(this Spreadsheet spreadsheet, object obj, CancellationToken token = default)
        {
            // This will be filled in by the generator once you call AddAsRowAsync()
        }
    }
}
";

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxReceiver is not SyntaxReceiver syntaxReceiver)
                throw new InvalidOperationException("We were given the wrong syntax receiver.");

            var classesToValidate = GetClassPropertiesInfo(context.Compilation, syntaxReceiver.ArgumentsToValidate).ToList();
            if (classesToValidate.Any())
            {
                var sb = new StringBuilder();
                GenerateValidator(sb, classesToValidate);
                context.AddSource("SpreadsheetExtensions.cs", sb.ToString());
            }
            else
            {
                context.AddSource("SpreadsheetExtensions.cs", AddAsRowStub);
            }
        }

        private static void GenerateValidator(StringBuilder sb, IEnumerable<ClassPropertiesInfo> infos)
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

            foreach (var info in infos)
            {
                // TODO: Write summary
                sb.AppendLine($"{indent}public static async ValueTask AddAsRowAsync(this Spreadsheet spreadsheet, {info.ClassType} obj, CancellationToken token = default)");
                sb.AppendLine($"{indent}{{");

                GenerateValidateMethodBody(sb, info, indent + "    ");

                sb.AppendLine($"{indent}}}");
            }

            sb.AppendLine(
@"    }
}");
        }

        private static void GenerateValidateMethodBody(StringBuilder sb, ClassPropertiesInfo info, string indent)
        {
            if (info.PropertyNames.Count > 0)
            {
                sb.AppendLine($"{indent}var cells = new[]");
                sb.AppendLine($"{indent}{{");

                var innerIndent = indent + "    ";
                foreach (var propertyName in info.PropertyNames)
                {
                    sb.AppendLine($"{innerIndent}new DataCell(obj.{propertyName}),");
                }

                sb.AppendLine($"{indent}}};");
            }
            else
            {
                sb.AppendLine($"{indent}var cells = System.Array.Empty<DataCell>();");
            }

            sb.AppendLine($"{indent}await spreadsheet.AddRowAsync(cells, token).ConfigureAwait(false);");
        }

        private static IEnumerable<ClassPropertiesInfo> GetClassPropertiesInfo(Compilation compilation, List<SyntaxNode> argumentsToValidate)
        {
            var foundTypes = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

            foreach (var argument in argumentsToValidate)
            {
                var semanticModel = compilation.GetSemanticModel(argument.SyntaxTree);

                var classType = semanticModel.GetTypeInfo(argument).Type;
                if (classType is null || foundTypes.Contains(classType))
                {
                    continue;
                }

                foundTypes.Add(classType);

                yield return new ClassPropertiesInfo(classType);
            }
        }
    }
}
