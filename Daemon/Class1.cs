using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.ControlFlow.ReflectionInspection.Daemon
{
  [ShellComponent]
  public class Class1 : IUsageInspectionsSupressor
  {
    public bool SupressUsageInspectionsOnElement(
      IDeclaredElement element, out ImplicitUseKindFlags flags)
    {
      var typeMember = element as ITypeMember;
      if (typeMember != null)
      {
        var containingType = typeMember.GetContainingType();
        if (containingType != null)
        {
          if (InspectionDeclarationIssuesDaemonStage.IsReflectionInspectionAttribute(containingType))
          {
            flags = ImplicitUseKindFlags.Access;
            return true;
          }
        }
      }

      flags = ImplicitUseKindFlags.Default;
      return false;
    }
  }
}