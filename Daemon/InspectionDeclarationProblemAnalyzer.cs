using System.Text;
using JetBrains.Annotations;
using JetBrains.ReSharper.ControlFlow.ReflectionInspection.Daemon.Highlighting;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Daemon.Stages;
using JetBrains.ReSharper.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.ControlFlow.ReflectionInspection.Daemon
{
  [ElementProblemAnalyzer(typeof(IClassDeclaration),
    HighlightingTypes = new[] { typeof(InspectionDeclarationErrorHighlighting) })]
  public class InspectionDeclarationProblemAnalyzer : ElementProblemAnalyzer<IClassDeclaration>
  {
    [NotNull] private readonly InspectionsCache myInspectionsCache;

    public InspectionDeclarationProblemAnalyzer([NotNull] InspectionsCache inspectionsCache)
    {
      myInspectionsCache = inspectionsCache;
    }

    protected override void Run(
      IClassDeclaration declaration, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
    {
      var classType = declaration.DeclaredElement as IClass;
      if (classType == null) return;

      if (!IsReflectionInspectionAttribute(classType)) return;

      var predefinedType = classType.Module.GetPredefinedType(classType.ResolveContext);

      var baseClass = classType.GetBaseClassType();
      if (baseClass == null
        || !baseClass.Equals(predefinedType.Attribute)
        || classType.IsAbstract
        || CSharpDeclaredElementUtil.IsStaticClass(classType))
      {
        var anchor = declaration.ExtendsList ?? (ITreeNode) declaration.NameIdentifier;
        consumer.AddHighlighting(
          new InspectionDeclarationErrorHighlighting(
            "Inspection should be declared as non-abstract non-static " +
            "attribute class type, inherited directly from 'System.Attribute'"),
          anchor.GetDocumentRange());
        return;
      }

      if (declaration.IsPartial)
      {
        consumer.AddHighlighting(
          new InspectionDeclarationErrorHighlighting(
            "Inspection can't be declared as partial class"),
          declaration.ClassKeyword.GetDocumentRange());
        return;
      }

      var entry = myInspectionsCache.GetEntry(classType.Module);

      var inspectionInfo = entry.GetInfo(classType);
      if (inspectionInfo != null)
      {
        var errors = inspectionInfo.Errors;
        if (errors.Count > 0)
        {
          var builder = new StringBuilder();
          builder
            .AppendFormat(
              "Inspection compilation produces {0} {1}:",
              errors.Count, NounUtil.ToPluralOrSingular("error", errors.Count))
            .AppendLine().AppendLine();

          foreach (var error in errors)
          {
            var line = error.ErrorText;

            if (error.Line > 0 && error.Column > 0)
              line += string.Format(" (line: {0}, column: {1})", error.Line, error.Offset);

            if (error.Offset >= 0)
            {
              var containingFile = declaration.GetContainingFile().NotNull();
              var treeOffset = containingFile.Translate(declaration.GetDocumentRange().Document, error.Offset);
              var node = declaration.FindNodeAt(new TreeTextRange(treeOffset, 1));
              if (node != null && node.IsFiltered())
              {
                consumer.AddHighlighting(
                  new InspectionDeclarationErrorHighlighting(
                    "Inspection compilation error: " + line),
                  node.GetDocumentRange());
              }
            }

            builder.Append(line);
          }

          consumer.AddHighlighting(
            new InspectionDeclarationErrorHighlighting(builder.ToString()),
            declaration.NameIdentifier.GetDocumentRange());
        }
      }
    }

    // todo: move to common
    private static bool IsReflectionInspectionAttribute([NotNull] ITypeElement typeElement)
    {
      foreach (var attribute in typeElement.GetAttributeInstances(inherit: false))
      {
        var attributeType = attribute.GetAttributeType();
        if (attributeType.IsUnknown) continue;

        var clrTypeName = attributeType.GetClrName();
        if (clrTypeName.ShortName == "ReflectionInspectionAttribute")
          return true;
      }

      return false;
    }
  }
}