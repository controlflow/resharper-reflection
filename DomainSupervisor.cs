using System;
using JetBrains.Annotations;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.Util;
using JetBrains.Util.Logging;

namespace JetBrains.ReSharper.ControlFlow.ReflectionInspection
{
  [SolutionComponent]
  public sealed class DomainSupervisor
  {
    [NotNull] private readonly object mySyncLock;
    [CanBeNull] private AppDomain myDomain;

    public DomainSupervisor([NotNull] Lifetime lifetime)
    {
      mySyncLock = new object();
      lifetime.AddAction(RecycleDomain);
    }

    [NotNull] public AppDomain GetOrCreateDomain()
    {
      lock (mySyncLock)
      {
        return myDomain ?? (myDomain = CreateDomain());
      }
    }

    public void RecycleDomain()
    {
      lock (mySyncLock)
      {
        var domain = myDomain;
        if (domain == null) return;

        try
        {
          AppDomain.Unload(domain);
          Logger.LogMessage(LoggingLevel.INFO, "ReflectionInspection.Domain unloaded");
        }
        catch (Exception exception)
        {
          Logger.LogExceptionSilently(exception);
        }

        myDomain = null;
      }
    }

    [NotNull] public static AppDomain CreateDomain()
    {
      var assemblyLocation = typeof(DomainSupervisor).Assembly.Location;
      var assemblyDirectory = FileSystemPath.Parse(assemblyLocation).Directory;

      var domainSetup = new AppDomainSetup();
      domainSetup.ApplicationBase = assemblyDirectory.FullPath;
      domainSetup.DisallowBindingRedirects = true;
      domainSetup.DisallowCodeDownload = true;

      var domain = AppDomain.CreateDomain(
        "ReflectionInspection.Domain", null, domainSetup);

      return domain;
    }
  }
}