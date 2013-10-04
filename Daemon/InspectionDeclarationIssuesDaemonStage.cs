using System;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.ControlFlow.ReflectionInspection.CodeTransformation;
using JetBrains.ReSharper.ControlFlow.ReflectionInspection.Daemon.Highlighting;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Daemon.Stages;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.ControlFlow.ReflectionInspection.Daemon
{
  [DaemonStage(StagesBefore = new[] {typeof(LanguageSpecificDaemonStage)})]
  internal sealed class InspectionDeclarationIssuesDaemonStage : CSharpDaemonStageBase
  {
    protected override IDaemonStageProcess CreateProcess(
      IDaemonProcess process, IContextBoundSettingsStore settings,
      DaemonProcessKind processKind, ICSharpFile file)
    {
      return new InspectionDeclarationIssuesStageProcess(process, file, settings);
    }

    private sealed class InspectionDeclarationIssuesStageProcess : CSharpDaemonStageProcessBase
    {
      public InspectionDeclarationIssuesStageProcess([NotNull] IDaemonProcess process,
        [NotNull] ICSharpFile file, [NotNull] IContextBoundSettingsStore settingsStore)
        : base(process, file)
      {
        SettingsStore = settingsStore;
      }

      [NotNull] private IContextBoundSettingsStore SettingsStore { get; set; }

      public override bool InteriorShouldBeProcessed(ITreeNode element, IHighlightingConsumer context)
      {
        if (element is IClassDeclaration) return true;
        return !(element is ITypeMemberDeclaration);
      }

      public override void Execute(Action<DaemonStageResult> committer)
      {
        HighlightInFile(
          (file, consumer) => file.ProcessDescendants(this, consumer),
          committer, SettingsStore);
      }

      public override void ProcessBeforeInterior(ITreeNode element, IHighlightingConsumer consumer)
      {
        var declaration = element as IClassDeclaration;
        if (declaration == null) return;

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

        // todo: check any public Validate()-like methods with suitable signature

        var inspectionsCache = File.GetSolution().GetComponent<InspectionsCache>();
        var entry = inspectionsCache.GetEntry(classType.Module);

        var inspectionInfo = entry.GetInfo(classType);
        if (inspectionInfo == null) return;

        var errors = inspectionInfo.Errors;
        if (errors.Count == 0) return;

        var builder = new StringBuilder()
          .AppendFormat("Inspection compilation produces {0} {1}:",
            errors.Count, NounUtil.ToPluralOrSingular("error", errors.Count))
          .AppendLine();

        // todo: remove
        RangeTranslator translator;
        builder.AppendLine(CodeQualifier.RewriteDeclaration(declaration, out translator));

        foreach (var error in errors)
        {
          var line = error.ErrorText;

          if (error.Line > 0 && error.Column > 0)
            line += string.Format(" [line {0} column {1}]", error.Line, error.Column);

          if (error.Offset >= 0)
          {
            var containingFile = declaration.GetContainingFile().NotNull();
            var treeOffset = containingFile.Translate(
              declaration.GetDocumentRange().Document, error.Offset);

            var node = containingFile.FindNodeAt(new TreeTextRange(treeOffset, 1));
            if (node != null && !node.IsFiltered())
            {
              consumer.AddHighlighting(
                new InspectionDeclarationErrorHighlighting(
                  "Inspection compilation error: " + line),
                node.GetDocumentRange());
            }
          }

          builder.Append(line).AppendLine();
        }

        if (errors.Any(x => x.ErrorText.StartsWith("CS0234")))
        {
          builder.AppendLine().AppendLine(
            "Please, make shure you are using only BCL types in inspection body or " +
            "every project type refereces occurs only in typeof(T) expressions");
        }

        consumer.AddHighlighting(
          new InspectionDeclarationErrorHighlighting(builder.ToString()),
          declaration.NameIdentifier.GetDocumentRange());
      }
    }

    public static bool IsReflectionInspectionAttribute([NotNull] ITypeElement typeElement)
    {
      foreach (var attribute in typeElement.GetAttributeInstances(false))
      {
        var attributeType = attribute.GetAttributeType();
        if (attributeType.IsUnknown) continue;

        var clrTypeName = attributeType.GetClrName();
        if (clrTypeName.ShortName == "ReflectionInspectionAttribute" ||
            clrTypeName.ShortName == "ReflectionIspection") return true;
      }

      return false;
    }
  }
}