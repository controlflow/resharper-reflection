using System;
using JetBrains.Annotations;

namespace JetBrains.ReSharper.ControlFlow.ReflectionInspection.Domain
{
  public sealed class InspectionDomain
  {
    [NotNull] private readonly AppDomain myDomain;
    [NotNull] private readonly DomainRoot myRoot;

    public InspectionDomain([NotNull] AppDomain domain, [NotNull] DomainRoot root)
    {
      myDomain = domain;
      myRoot = root;
    }

    [NotNull] public AppDomain Domain
    {
      get { return myDomain; }
    }

    [NotNull] public DomainRoot Root
    {
      get { return myRoot; }
    }
  }
}