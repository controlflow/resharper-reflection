using System;

// TODO: DO NOT USE ANY OF TYPES FROM JETBRAINS.* NAMESPACES

namespace JetBrains.ReSharper.ControlFlow.ReflectionInspection.Domain
{
  public sealed class DomainRoot : MarshalByRefObject
  {
    private readonly object mySyncLock;
    private int myTotalAssemblies, myDeadAssemblies;

    public DomainRoot()
    {
      mySyncLock = new object();
    }

    public int AssembliesCount
    {
      get { lock (mySyncLock) return myTotalAssemblies; }
    }

    public int DeadAssemblies
    {
      get { lock (mySyncLock) return myDeadAssemblies; }
    }

    public void AddAssembly()
    {
      lock (mySyncLock) ++ myTotalAssemblies;
    }

    public void AddDeadAssembly(int count)
    {
      lock (mySyncLock) myDeadAssemblies += count;
    }

    public static DomainRoot CreateInDomain(AppDomain domain)
    {
      var type = typeof(DomainRoot);
      return (DomainRoot) domain.CreateInstanceAndUnwrap(type.Assembly.FullName, type.FullName);
    }
  }
}