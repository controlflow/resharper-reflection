using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.ControlFlow.ReflectionInspection.Compilation;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.ControlFlow.ReflectionInspection
{
  public sealed class InspectionInfo
  {
    public InspectionInfo()
    {
      Errors = EmptyList<CompilationError>.InstanceList;
    }

    public InspectionInfo([NotNull] IList<CompilationError> errosList)
    {
      Errors = errosList;
    }

    [NotNull] public IList<CompilationError> Errors { get; private set; }
    
  }
}