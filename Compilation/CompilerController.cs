using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

// TODO: DO NOT USE ANY OF TYPES FROM JETBRAINS.* NAMESPACES

namespace JetBrains.ReSharper.ControlFlow.ReflectionInspection.Compilation
{
  internal sealed class CompilerController : MarshalByRefObject
  {
    private readonly List<Assembly> myAssemblies;
    private readonly Dictionary<string, Type> myTypeToAssembly;

    public CompilerController()
    {
      myAssemblies = new List<Assembly>();
      myTypeToAssembly = new Dictionary<string, Type>();
      CompilerLanguage = "CSharp";
    }

    public string CompilerLanguage { get; set; }

    public bool TooManyAssemblies
    {
      get { return myAssemblies.Count > 42; }
    }

    private List<CompilationError> CompileUnits(params string[] sources)
    {
      var provider = CodeDomProvider.CreateProvider(CompilerLanguage);

      var systemDllLocation = typeof(Uri).Assembly.Location;
      var systemCoreDllLocation = typeof(Enumerable).Assembly.Location;

      var parameters = new CompilerParameters {
        GenerateInMemory = true,
        ReferencedAssemblies = {
          systemDllLocation,
          systemCoreDllLocation,
        },
      };

      var errors = new List<CompilationError>();

      var results = provider.CompileAssemblyFromSource(parameters, sources);
      if (results.Errors.HasErrors)
      {
        foreach (CompilerError error in results.Errors)
        {
          errors.Add(new CompilationError(
            error.Line, error.Column,
            error.ErrorNumber, error.ErrorText,
            error.IsWarning));
        }

        return errors;
      }

      try
      {
        var assembly = results.CompiledAssembly;
        if (assembly != null)
        {
          myAssemblies.Add(assembly);

          foreach (var type in assembly.GetTypes())
            myTypeToAssembly[type.FullName] = type;
        }
      }
      catch { }

      return errors;
    }

    public IList<CompilationError> Compile(params string[] sources)
    {
      var results = CompileUnits(sources);
      return results;
    }

    public static CompilerController CreateInDomain(AppDomain domain)
    {
      var type = typeof (CompilerController);
      return (CompilerController)
        domain.CreateInstanceAndUnwrap(type.Assembly.FullName, type.FullName);
    }
  }
}