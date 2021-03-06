﻿using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using JetBrains.ReSharper.ControlFlow.ReflectionInspection.Domain;

// TODO: DO NOT USE ANY OF TYPES FROM JETBRAINS.* NAMESPACES



namespace JetBrains.ReSharper.ControlFlow.ReflectionInspection.Compilation
{
  public static class ReflectionInspectionTypeResolver
  {
    [UsedImplicitly]
    public static Type ResolveTypeName(string fullQualifiedName)
    {
      GC.KeepAlive(fullQualifiedName);
      return null;
    }
  }

  public sealed class CompilerController : MarshalByRefObject
  {
    private readonly object mySyncLock;
    private readonly List<Assembly> myAssemblies;
    private readonly Dictionary<string, Type> myTypeToAssembly;

    public CompilerController()
    {
      mySyncLock = new object();
      myAssemblies = new List<Assembly>();
      myTypeToAssembly = new Dictionary<string, Type>();
      CompilerLanguage = "CSharp";
    }

    public string CompilerLanguage { get; set; }
    public DomainRoot DomainRoot { get; set; }

    public List<CompilationError> Compile(params string[] sources)
    {
      var errors = new List<CompilationError>();

      try
      {
        var provider = CodeDomProvider.CreateProvider(CompilerLanguage);

        var systemDllLocation = typeof(Uri).Assembly.Location;
        var systemCoreDllLocation = typeof(Enumerable).Assembly.Location;
        var me = typeof (CompilerController).Assembly.Location;

        var parameters = new CompilerParameters
        {
          ReferencedAssemblies =
          {
            systemDllLocation,
            systemCoreDllLocation,
            me,
          },

          GenerateInMemory = true
        };

        var results = provider.CompileAssemblyFromSource(parameters, sources);
        if (results.Errors.HasErrors)
        {
          foreach (CompilerError error in results.Errors)
          {
            if (error.IsWarning) continue;

            errors.Add(new CompilationError(
              error.Line - 1, error.Column - 1,
              error.ErrorNumber + ": " + error.ErrorText));
          }

          return errors;
        }

        var assembly = results.CompiledAssembly;
        if (assembly != null)
        {
          lock (mySyncLock)
          {
            myAssemblies.Add(assembly);

            var root = DomainRoot;
            if (root != null) root.AddAssembly();

            var maybeDead = new HashSet<Assembly>();
            foreach (var type in assembly.GetTypes())
            {
              Type value;
              if (myTypeToAssembly.TryGetValue(type.FullName, out value))
                maybeDead.Add(value.Assembly);

              myTypeToAssembly[type.FullName] = type;
            }

            if (maybeDead.Count > 0)
            {
              foreach (var type in myTypeToAssembly.Values)
                maybeDead.Remove(type.Assembly);

              if (root != null) root.AddDeadAssembly(maybeDead.Count);
            }
          }
        }
      }
      catch (Exception exception)
      {
        errors.Add(new CompilationError(
          0, 0, "Exception: " + exception));
      }

      return errors;
    }

    public static CompilerController CreateInDomain(InspectionDomain inspectionDomain)
    {
      var type = typeof (CompilerController);
      var controller = (CompilerController)
        inspectionDomain.Domain.CreateInstanceAndUnwrap(type.Assembly.FullName, type.FullName);
      controller.DomainRoot = inspectionDomain.Root;

      return controller;
    }
  }
}