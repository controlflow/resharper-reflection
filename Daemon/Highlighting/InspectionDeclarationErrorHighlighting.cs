using JetBrains.Annotations;
using JetBrains.ReSharper.Daemon;

namespace JetBrains.ReSharper.ControlFlow.ReflectionInspection.Daemon.Highlighting
{
  [StaticSeverityHighlighting(
    Severity.ERROR, "CSharpErrors",
    OverlapResolve = OverlapResolveKind.ERROR,
    ShowToolTipInStatusBar = false)]
  public class InspectionDeclarationErrorHighlighting : IHighlighting
  {
    public InspectionDeclarationErrorHighlighting([NotNull] string toolTip)
    {
      ToolTip = toolTip;
    }

    public bool IsValid() { return true; }
    public string ToolTip { get; private set; }
    public string ErrorStripeToolTip { get { return ToolTip; } }
    public int NavigationOffsetPatch { get { return 0; } }
  }
}