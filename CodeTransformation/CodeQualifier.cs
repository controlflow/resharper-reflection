using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Parsing;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.ControlFlow.ReflectionInspection.CodeTransformation
{
  public static class CodeQualifier
  {
    [NotNull]
    public static string RewriteDeclaration(
      [NotNull] IClassDeclaration declaration, [NotNull] out RangeTranslator translator)
    {
      var declarationRange = declaration.GetDocumentRange().TextRange;

      translator = new RangeTranslator();
      translator.StartMapping(declarationRange);

      var builder = new StringBuilder();

      InsertUsingsForUsedExtensionMethods(builder, declaration);

      var typeElement = declaration.DeclaredElement.NotNull();
      var nameSpace = typeElement.GetContainingNamespace();
      if (!nameSpace.IsRootNamespace)
        InsertNamespaceDeclaration(builder, nameSpace);

      if (builder.Length > 0)
      {
        translator.MapTextToText(
          declarationRange.StartOffset, 0, 0, builder.Length);
      }

      // copy whole declaration's text
      builder.Append(declaration.GetText());

      var removedRanges = new List<TextRange>(1);
      var elementsProcessor = new RecursiveElementCollector<ITreeNode>(IsElementToProcess);
      declaration.ProcessDescendants(elementsProcessor);

      foreach (var reference in elementsProcessor.GetResults())
      {
        QualifyReference(builder, reference, translator, removedRanges);
      }

      translator.EndMapping(TextRange.FromLength(builder.Length));

      if (!nameSpace.IsRootNamespace) builder.AppendLine().Append("}");

      return builder.ToString();
    }

    private static void QualifyReference(
      [NotNull] StringBuilder builder, [NotNull] ITreeNode reference,
      [NotNull] RangeTranslator translator, [NotNull] List<TextRange> removedRanges)
    {
      var attribute = reference as IAttribute;
      if (attribute != null)
      {
        var sourceRange = GetAttributeRemoveRange(attribute);
        var resultRange = translator.GetResultRange(sourceRange);

        builder.Remove(resultRange.StartOffset, resultRange.Length);
        translator.MapTextToText(
          sourceRange.StartOffset, sourceRange.Length, resultRange.StartOffset, 0);

        removedRanges.Add(sourceRange);
      }
      else
      {
        var typeofExpression = reference as ITypeofExpression;
        if (typeofExpression != null)
        {
          var fqn = typeofExpression.ArgumentType.ToString();
          var sourceRange = typeofExpression.GetDocumentRange().TextRange;
          var resultRange = translator.GetResultRange(sourceRange);

          var foo = "___FOO.BAR(\"" + fqn + "\")";

          builder.Remove(resultRange.StartOffset, resultRange.Length);
          builder.Insert(resultRange.StartOffset, foo);

          translator.MapTextToText(
            sourceRange.StartOffset, sourceRange.Length,
            resultRange.StartOffset, foo.Length);

          removedRanges.Add(sourceRange);
        }
        else
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
            else return;
          }

          if (result.DeclaredElement == null) return;

          var fullyQualifiedName = FqnUtil.Build(
            result.DeclaredElement, result.Substitution, !hasTypeArguments);
          if (fullyQualifiedName == null) return;

          var sourceRange = nameIdentifier.GetDocumentRange().TextRange;

          foreach (var removedRange in removedRanges)
            if (removedRange.Contains(sourceRange)) return;

          var resultRange = translator.GetResultRange(sourceRange);

          builder.Remove(resultRange.StartOffset, resultRange.Length);
          builder.Insert(resultRange.StartOffset, fullyQualifiedName);

          translator.MapTextToText(
            sourceRange.StartOffset, sourceRange.Length,
            resultRange.StartOffset, fullyQualifiedName.Length);
        }
      }
    }

    private static bool IsElementToProcess([NotNull] ITreeNode node)
    {
      var referenceName = node as IReferenceName;
      if (referenceName != null && referenceName.Qualifier == null)
        return true;

      var expression = node as IReferenceExpression;
      if (expression != null && expression.QualifierExpression == null)
        return true;

      var attribute = node as IAttribute;
      if (attribute != null)
      {
        var typeReference = attribute.TypeReference;
        if (typeReference != null)
        {
          var typeElement = typeReference.Resolve().DeclaredElement as ITypeElement;
          if (typeElement != null)
          {
            var attributeType = typeElement.GetClrName();
            if (attributeType.IsReflectionInspectionAttribute()) return true;
          }
        }
      }

      if (node is ITypeofExpression)
      {
        // only in method body
        var containingAttribute = node.GetContainingNode<IAttribute>();
        return (containingAttribute == null);
      }

      return false;
    }

    private static void InsertUsingsForUsedExtensionMethods(
      [NotNull] StringBuilder builder, [NotNull] ITreeNode declaration)
    {
      var processor = new RecursiveElementCollector<IReferenceExpression>(
        reference => reference.QualifierExpression != null && reference.IsExtensionMethod());

      declaration.ProcessDescendants(processor);

      var namespaces = new JetHashSet<INamespace>();
      foreach (var extensionMethod in processor.GetResults())
      {
        var method = extensionMethod.Reference.Resolve().DeclaredElement as IMethod;
        if (method == null) continue;

        var containingType = method.GetContainingType();
        if (containingType == null) continue;

        var nameSpace = containingType.GetContainingNamespace();
        if (!nameSpace.IsRootNamespace) namespaces.Add(nameSpace);
      }

      if (namespaces.Count == 0) return;

      foreach (var nameSpace in namespaces)
      {
        builder.Append("using ").Append(nameSpace.QualifiedName).AppendLine(";");
      }
    }

    private static void InsertNamespaceDeclaration(
      [NotNull] StringBuilder builder, [NotNull] INamespace nameSpace)
    {
      builder.Append("namespace ")
        .Append(nameSpace.QualifiedName).Append('{').AppendLine();
    }

    private static TextRange GetAttributeRemoveRange([NotNull] IAttribute attribute)
    {
      var attrRange = attribute.GetDocumentRange().TextRange;

      var section = AttributeSectionNavigator.GetByAttribute(attribute);
      if (section != null)
      {
        // <[RangeToRemove]> class C { }
        if (section.Attributes.Count == 1)
          return section.GetDocumentRange().TextRange;

        // [<RangeToRemove,> SomeOtherAttr] class C { }
        var token = attribute.GetNextMeaningfulToken();
        if (token != null && token.GetTokenType() == CSharpTokenType.COMMA)
        {
          return attrRange.SetEndTo(token.GetDocumentRange().TextRange.EndOffset);
        }
      }

      return attrRange;
    }
  }
}