using System;
using System.Text;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.ControlFlow.ReflectionInspection.CodeTransformation;
using JetBrains.ReSharper.ControlFlow.ReflectionInspection.Compilation;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;
using JetBrains.Util.dataStructures.TypedIntrinsics;
using JetBrains.Util.Logging;

namespace JetBrains.ReSharper.ControlFlow.ReflectionInspection
{
  public sealed class InspectionsCacheEntry
  {
    [NotNull] private readonly IPsiModule myModule;
    [NotNull] private readonly InspectionsCache myCache;
    [NotNull] private readonly object mySyncLock;
    [CanBeNull] private CompilerController myController;

    //private readonly JetHashSet<IPsiSourceFile> myFiles;

    public InspectionsCacheEntry([NotNull] InspectionsCache cache, [NotNull] IPsiModule module)
    {
      myCache = cache;
      myModule = module;
      mySyncLock = new object();
      
    }

    [NotNull]
    private CompilerController GetOrCreateCompilerController()
    {
      lock (mySyncLock)
      {
        var controller = myController;
        if (controller != null) return controller;

        var domain = myCache.DomainSupervisor.GetOrCreateDomain();
        return myController = CompilerController.CreateInDomain(domain);
      }
    }

    [CanBeNull]
    public Action<ITextControl> Bar(IClassDeclaration declaration)
    {
      RangeTranslator translator;
      var text = CodeQualifier.RewriteDeclaration(declaration, out translator);

      MessageBox.ShowInfo(text);

      var controller = GetOrCreateCompilerController();

      var errors = controller.Compile(text);
      if (errors.Count == 0) return null;

      var sb = new StringBuilder();

      var factory = Shell.Instance.GetComponent<IInMemoryDocumentFactory>();
      var compiledDocument = factory.CreateSimpleDocumentFromText(text, "someMoniker");

      foreach (var compilationError in errors)
      {
        sb.Append(compilationError.ErrorText);

        try // I'm scary of exceptions from IDocument
        {
          var coords = new DocumentCoords(
            (Int32<DocLine>)compilationError.Line,
            (Int32<DocColumn>)compilationError.Column);

          if (coords.Line < compiledDocument.GetLineCount())
          {
            var length = compiledDocument.GetLineLength(coords.Line);
            if (length < coords.Column)
              coords = new DocumentCoords(coords.Line, length.Minus1());

            var offsetByCoords = compiledDocument.GetOffsetByCoords(coords);
            var sourceRange = translator.GetSourceRange(offsetByCoords);
            if (sourceRange.IsValid)
            {
              var document = declaration.GetDocumentRange().Document;
              var origCoords = document.GetCoordsByOffset(sourceRange.StartOffset);
              sb.AppendFormat(" (line: {0}, column: {1})", origCoords.Line.Plus1(), origCoords.Column.Plus1());
            }
          }
        }
        catch (Exception exception)
        {
          Logger.LogExceptionSilently(exception);
        }

        sb.AppendLine();
      }

      return _ => MessageBox.ShowInfo(sb.ToString());
    }
  }
}