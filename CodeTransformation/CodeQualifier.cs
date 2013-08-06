using System.Text;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.ControlFlow.ReflectionInspection.CodeTransformation
{
  public static class CodeQualifier
  {
    [NotNull] public static string RewriteDeclaration(
      [NotNull] IClassDeclaration declaration, [NotNull] out RangeTranslator translator)
    {
      var declarationRange = declaration.GetDocumentRange();



      translator = new RangeTranslator();
      translator.StartMapping(declarationRange.TextRange);

      var builder = new StringBuilder();

      // prolog
      var typeElement = declaration.DeclaredElement.NotNull();
      var nameSpace = typeElement.GetContainingNamespace();
      if (!nameSpace.IsRootNamespace)
      {
        builder.Append("namespace ").Append(nameSpace.QualifiedName).Append('{').AppendLine();
        translator.MapTextToText(
          declarationRange.TextRange.StartOffset, 0, 0, builder.Length);
      }

      // declaration text
      builder.Append(declaration.GetText());

      var processor = new RecursiveElementCollector<ITreeNode>(node =>
      {
        var referenceName = node as IReferenceName;
        if (referenceName != null && referenceName.Qualifier == null) return true;

        var expression = node as IReferenceExpression;
        if (expression != null && expression.QualifierExpression == null) return true;

        var attribute = node as IAttribute;
        if (attribute != null)
        {
          var typeReference = attribute.TypeReference;
          if (typeReference != null)
          {
            var typeElem = typeReference.Resolve().DeclaredElement as ITypeElement;
            if (typeElem != null)
            {
              if (typeElem.GetClrName().ShortName == "ReflectionInspectionAttribute")
              {
                return true;
              }
            }
          }
        }

        return false;
      });

      declaration.ProcessDescendants(processor);

      var attrRange = DocumentRange.InvalidRange;
      foreach (var reference in processor.GetResults())
      {
        IResolveResult result;
        ITreeNode nameIdentifier;
        bool hasTypeArguments;

        var referenceName = reference as IReferenceName;
        if (referenceName != null)
        {
          result = referenceName.Reference.Resolve().Result;
          nameIdentifier = referenceName.NameIdentifier;
          hasTypeArguments = (referenceName.TypeArgumentList != null);
        }
        else
        {
          var referenceExpression = reference as IReferenceExpression;
          if (referenceExpression != null)
          {
            result = referenceExpression.Reference.Resolve().Result;
            nameIdentifier = referenceExpression.NameIdentifier;
            hasTypeArguments = (referenceExpression.TypeArgumentList != null);
          }
          else
          {
            if (reference is IAttribute)
            {
              attrRange = reference.GetDocumentRange();
              var sourceRange = attrRange.TextRange;
              var target1 = translator.GetResultRange(sourceRange);
              builder.Remove(target1.StartOffset, target1.Length);
              translator.MapTextToText(
                sourceRange.StartOffset, sourceRange.Length, target1.StartOffset, 0);
            }

            continue;
          }
        }

        if (result.DeclaredElement == null) continue;

        var fqn = BuildLongName(result.DeclaredElement, result.Substitution, !hasTypeArguments);
        if (fqn == null) continue;

        var range = nameIdentifier.GetDocumentRange().TextRange;
        if (attrRange.TextRange.Contains(range)) continue;

        var target = translator.GetResultRange(range);

        builder.Remove(target.StartOffset, target.Length);
        builder.Insert(target.StartOffset, fqn);

        translator.MapTextToText(
          range.StartOffset, range.Length, target.StartOffset, fqn.Length);
      }

      translator.EndMapping(TextRange.FromLength(builder.Length));

      // epilog
      if (!nameSpace.IsRootNamespace)
      {
        builder.AppendLine().Append("}");
      }

      return builder.ToString();
    }

    [NotNull]
    private static string BuildLongName([NotNull] INamespace nameSpace)
    {
      if (nameSpace.IsRootNamespace) return "global::";

      return "global::" + nameSpace.QualifiedName + ".";
    }

    [CanBeNull]
    private static string BuildLongName(
      [NotNull] ITypeElement typeElement, [NotNull] ISubstitution substitution,
      bool emitTypeArguments = false)
    {
      var containing = typeElement.GetContainingNamespace();
      var fullNs = BuildLongName(containing, containing.IdSubstitution);
      var buf = new StringBuilder(fullNs).Append(typeElement.ShortName);

      var typeParameters = typeElement.TypeParameters;
      if (emitTypeArguments && typeParameters.Count > 0)
      {
        buf.Append('<');

        var first = true;
        foreach (var typeParameter in typeParameters)
        {
          if (first) first = false;
          else buf.Append(',');

          var typeFqn = BuildLongName(substitution[typeParameter], true);
          if (typeFqn == null) return null;

          buf.Append(typeFqn);
        }

        buf.Append('>');
      }

      return buf.ToString();
    }

    [CanBeNull]
    private static string BuildLongName([CanBeNull] IType type, bool emitTypeArguments)
    {
      var arrayType = type as IArrayType;
      if (arrayType != null)
      {
        var elementFqn = BuildLongName(arrayType.ElementType, emitTypeArguments);
        if (elementFqn == null) return null;

        return elementFqn + "[" + new string(',', arrayType.Rank - 1) + "]";
      }

      var declaredType = type as IDeclaredType;
      if (declaredType != null)
      {
        var typeElement = declaredType.GetTypeElement();
        if (typeElement == null) return null;

        return BuildLongName(typeElement, declaredType.GetSubstitution(), emitTypeArguments);
      }

      var pointerType = type as IPointerType;
      if (pointerType != null)
      {
        var elementFqn = BuildLongName(pointerType.ElementType, emitTypeArguments);
        if (elementFqn == null) return null;

        return elementFqn + "*";
      }

      return null;
    }

    [CanBeNull] private static string BuildLongName(
      [NotNull] IDeclaredElement element, [NotNull] ISubstitution substitution,
      bool emitTypeArguments = false)
    {
      var typeElement = element as ITypeElement;
      if (typeElement != null)
      {
        return BuildLongName(typeElement, substitution, emitTypeArguments);
      }

      var nameSpace = element as INamespace;
      if (nameSpace != null)
      {
        return BuildLongName(nameSpace);
      }

      return null;
    }
  }
}