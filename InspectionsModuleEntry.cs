using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.ControlFlow.ReflectionInspection.CodeTransformation;
using JetBrains.ReSharper.ControlFlow.ReflectionInspection.Compilation;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Impl;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.TextControl;
using JetBrains.Util;
using JetBrains.Util.dataStructures.TypedIntrinsics;
using JetBrains.Util.Logging;

namespace JetBrains.ReSharper.ControlFlow.ReflectionInspection
{
  public sealed class InspectionsModuleEntry
  {
    [NotNull] private readonly IPsiModule myModule;
    [NotNull] private readonly InspectionsCache myCache;
    [NotNull] private readonly object mySyncLock;
    [CanBeNull] private CompilerController myController; // do we need lazyness here?

    [NotNull] private readonly Dictionary<ITypeElement, InspectionInfo> myDict;

    //private readonly JetHashSet<IPsiSourceFile> myFiles;

    public InspectionsModuleEntry([NotNull] InspectionsCache cache, [NotNull] IPsiModule module)
    {
      myCache = cache;
      myModule = module;
      mySyncLock = new object();
      myDict = new Dictionary<ITypeElement, InspectionInfo>(
        DeclaredElementEqualityComparer.TypeElementComparer);
    }

    [CanBeNull]
    public InspectionInfo GetInfo(ITypeElement foo)
    {
      InspectionInfo info;
      lock (mySyncLock)
      {
        if (myDict.TryGetValue(foo, out info)) return info;
      }

      if (!(foo is IClass)) return null;
      if (!foo.IsAttribute()) return null;

      var declarations = foo.GetDeclarations();
      if (declarations.Count != 1) return null;

      var classDeclaration = declarations[0] as IClassDeclaration;
      if (classDeclaration == null) return null;

      RangeTranslator translator;
      var text = CodeQualifier.RewriteDeclaration(classDeclaration, out translator);

      var controller = GetOrCreateCompilerController();
      var results = controller.Compile(text);

      if (results.Count == 0)
      {
        info = new InspectionInfo();
      }
      else
      {
        var factory = Shell.Instance.GetComponent<IInMemoryDocumentFactory>();
        var compiledDocument = factory.CreateSimpleDocumentFromText(text, "someMoniker");
        var xs = new LocalList<CompilationError>();
        
        foreach (var compilationError in results)
        {
          int line = compilationError.Line, column = compilationError.Column, offset = -1;

          try // I'm too scary of exceptions from IDocument
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
                offset = sourceRange.StartOffset;

                var document = classDeclaration.GetDocumentRange().Document;
                var origCoords = document.GetCoordsByOffset(sourceRange.StartOffset);

                line = 1 + (int) origCoords.Line;
                column = 1 + (int) origCoords.Line;
              }
            }
          }
          catch (Exception exception)
          {
            Logger.LogExceptionSilently(exception);
          }

          xs.Add(new CompilationError(line, column, compilationError.ErrorText, offset));
        }

        info = new InspectionInfo(xs.ResultingList());
      }


      return info;
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

      int? range = null;
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
              if (!range.HasValue) range = sourceRange.StartOffset;

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

      return textControl =>
      {
        if (range.HasValue)
          textControl.Caret.MoveTo(range.Value, CaretVisualPlacement.Generic);

        MessageBox.ShowInfo(sb.ToString());
      };
    }

    public void Drop(IList<IPsiSourceFile> value)
    {
      
    }
  }
}