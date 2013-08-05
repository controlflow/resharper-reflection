using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Modules;

namespace JetBrains.ReSharper.ControlFlow.ReflectionInspection
{
  [SolutionComponent]
  public class InspectionsCache : InvalidatingPsiCache
  {
    [NotNull] private readonly IPsiModules myPsiModules;
    [NotNull] private readonly DomainSupervisor myDomainSupervisor;
    [NotNull] private readonly Dictionary<IPsiModule, InspectionsCacheEntry> myCaches;
    [NotNull] private readonly object mySyncLock;

    public InspectionsCache(
      [NotNull] Lifetime lifetime,
      [NotNull] IPsiModules psiModules,
      [NotNull] ChangeManager changeManager,
      [NotNull] DomainSupervisor domainSupervisor)
    {
      myPsiModules = psiModules;
      myDomainSupervisor = domainSupervisor;
      myCaches = new Dictionary<IPsiModule, InspectionsCacheEntry>();
      mySyncLock = new object();

      changeManager.Changed2.Advise(lifetime, OnChangeManagerChanged);
    }

    [NotNull]
    public DomainSupervisor DomainSupervisor
    {
      get { return myDomainSupervisor; }
    }

    [NotNull]
    public InspectionsCacheEntry GetEntry([NotNull] IPsiModule module)
    {
      lock (mySyncLock)
      {
        InspectionsCacheEntry entry;
        if (myCaches.TryGetValue(module, out entry)) return entry;

        return myCaches[module] = new InspectionsCacheEntry(this, module);
      }
    }

    private void OnChangeManagerChanged([NotNull] ChangeEventArgs args)
    {
      var moduleChange = args.ChangeMap.GetChange<PsiModuleChange>(myPsiModules);
      if (moduleChange == null) return;

      if (moduleChange.ModuleChanges.Count > 0)
      {
        lock (mySyncLock)
        {
          foreach (var change in moduleChange.ModuleChanges)
            myCaches.Remove(change.Item);
        }
      }

      if (moduleChange.FileChanges.Count > 0)
      {
        // todo: a
      }
    }
  }
}