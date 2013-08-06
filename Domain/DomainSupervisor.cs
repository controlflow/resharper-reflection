using System;
using JetBrains.Annotations;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.Util;
using JetBrains.Util.Logging;

namespace JetBrains.ReSharper.ControlFlow.ReflectionInspection.Domain
{
  [SolutionComponent]
  public sealed class DomainSupervisor
  {
    [NotNull] private readonly object mySyncLock;
    [CanBeNull] private InspectionDomain myDomain;

    public DomainSupervisor([NotNull] Lifetime lifetime)
    {
      mySyncLock = new object();
      lifetime.AddAction(RecycleDomain);
    }

    private const string DomainName = "ReflectionInspection.Domain";

    [NotNull] public InspectionDomain GetOrCreateDomain()
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
          AppDomain.Unload(domain.Domain);
          Logger.LogMessage(LoggingLevel.INFO, DomainName + " unloaded");
        }
        catch (Exception exception)
        {
          Logger.LogExceptionSilently(exception);
        }

        myDomain = null;
      }
    }

    [NotNull] private static InspectionDomain CreateDomain()
    {
      var assemblyLocation = typeof(DomainSupervisor).Assembly.Location;
      var assemblyDirectory = FileSystemPath.Parse(assemblyLocation).Directory;

      var domainSetup = new AppDomainSetup {
        ApplicationBase = assemblyDirectory.FullPath,
        DisallowBindingRedirects = true,
        DisallowCodeDownload = true
      };

      var domain = AppDomain.CreateDomain(DomainName, null, domainSetup);
      var statistics = DomainRoot.CreateInDomain(domain);

      return new InspectionDomain(domain, statistics);
    }
  }
}