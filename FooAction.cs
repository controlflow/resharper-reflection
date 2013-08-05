using System;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.CSharp.Bulbs;
using JetBrains.ReSharper.Intentions.CSharp.ContextActions;
using JetBrains.ReSharper.Intentions.Extensibility;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.ControlFlow.ReflectionInspection
{
  [ContextAction(
    Name = "Fooo Foo",
    Description = "Foo action",
    Group = CSharpContextActions.GroupID)]
  class FooAction : ContextActionBase
  {
    [NotNull] private readonly ICSharpContextActionDataProvider myProvider;
    
    public FooAction([NotNull] ICSharpContextActionDataProvider provider)
    {
      myProvider = provider;
    }

    public override string Text
    {
      get { return "Compile attribute"; }
    }

    public override bool IsAvailable(IUserDataHolder cache)
    {
      var declaration = myProvider.GetSelectedElement<IClassDeclaration>(true, true);
      if (declaration == null) return false;

      var classType = declaration.DeclaredElement as IClass;
      return classType != null && classType.IsAttribute();
    }

    protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
    {
      var declaration = myProvider.GetSelectedElement<IClassDeclaration>(true, true);
      if (declaration == null) return null;

      var classType = declaration.DeclaredElement as IClass;
      if (classType == null || !classType.IsAttribute()) return null;

      var supervisor = solution.GetComponent<DomainSupervisor>();
      var inspectionsCache = solution.GetComponent<InspectionsCache>();

      var entry = inspectionsCache.GetEntry(classType.Module);

      return entry.Bar(declaration);
      
      {
        //var error = new HeapAllocationHighlighting(list[0].ErrorText);
        //var nameDocumentRange = declaration.GetNameDocumentRange();

        

        //var errorText = list[0].ErrorText;
        //
        //
        //MessageBox.ShowError(errorText);
      }


      return null;
    }
  }

  [StaticSeverityHighlighting(Severity.ERROR,
    "CSharpErrors",
    OverlapResolve = OverlapResolveKind.ERROR,
    ShowToolTipInStatusBar = true)]
  public class HeapAllocationHighlighting : IHighlighting
  {
    public HeapAllocationHighlighting(string toolTip)
    {
      ToolTip = toolTip;
    }

    public bool IsValid()
    {
      return true;
    }

    public string ToolTip { get; private set; }
    public string ErrorStripeToolTip { get { return ToolTip; } }
    public int NavigationOffsetPatch { get { return 0; } }
  }
}
