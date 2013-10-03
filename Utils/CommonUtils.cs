using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.ControlFlow.ReflectionInspection
{
  public static class CommonUtils
  {
    public static bool IsReflectionInspectionAttribute([NotNull] this IClrTypeName typeName)
    {
      switch (typeName.ShortName)
      {
        case "ReflectionInspection":
        case "ReflectionInspectionAttribute":
          return true;
      }

      return false;
    }
  }
}