using System.Text;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Resolve;

namespace JetBrains.ReSharper.ControlFlow.ReflectionInspection.CodeTransformation
{
  public static class FqnUtil
  {
    [NotNull] private static string Build(
      [NotNull] INamespace nameSpace, bool emitPrefix)
    {
      if (nameSpace.IsRootNamespace) return "global::";

      return "global::" + nameSpace.QualifiedName + (emitPrefix ? "." : null);
    }

    [CanBeNull] public static string Build(
      [NotNull] ITypeElement typeElement, [NotNull] ISubstitution substitution,
      bool emitTypeArguments = false)
    {
      var containing = typeElement.GetContainingNamespace();
      var longNamespaceName = Build(containing, true);
      var buf = new StringBuilder(longNamespaceName).Append(typeElement.ShortName);

      var typeParameters = typeElement.TypeParameters;
      if (emitTypeArguments && typeParameters.Count > 0)
      {
        buf.Append('<');

        var first = true;
        foreach (var typeParameter in typeParameters)
        {
          if (first) first = false;
          else buf.Append(',');

          var typeFqn = Build(substitution[typeParameter], true);
          if (typeFqn == null) return null;

          buf.Append(typeFqn);
        }

        buf.Append('>');
      }

      return buf.ToString();
    }

    [CanBeNull]
    public static string Build([CanBeNull] IType type, bool emitTypeArguments)
    {
      var arrayType = type as IArrayType;
      if (arrayType != null)
      {
        var elementFqn = Build(arrayType.ElementType, emitTypeArguments);
        if (elementFqn == null) return null;

        return elementFqn + "[" + new string(',', arrayType.Rank - 1) + "]";
      }

      var declaredType = type as IDeclaredType;
      if (declaredType != null)
      {
        var typeElement = declaredType.GetTypeElement();
        if (typeElement == null) return null;

        return Build(typeElement, declaredType.GetSubstitution(), emitTypeArguments);
      }

      var pointerType = type as IPointerType;
      if (pointerType != null)
      {
        var elementFqn = Build(pointerType.ElementType, emitTypeArguments);
        if (elementFqn == null) return null;

        return elementFqn + "*";
      }

      return null;
    }

    [CanBeNull] public static string Build(
      [NotNull] IDeclaredElement element, [NotNull] ISubstitution substitution,
      bool emitTypeArguments = false)
    {
      var typeElement = element as ITypeElement;
      if (typeElement != null)
      {
        return Build(typeElement, substitution, emitTypeArguments);
      }

      var nameSpace = element as INamespace;
      if (nameSpace != null)
      {
        return Build(nameSpace, false);
      }

      return null;
    }
  }
}