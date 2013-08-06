using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.ControlFlow.ReflectionInspection.Domain;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;

namespace JetBrains.ReSharper.ControlFlow.ReflectionInspection
{
  [SolutionComponent]
  public class InspectionsCache : InvalidatingPsiCache
  {
    [NotNull] private readonly IPsiModules myPsiModules;
    [NotNull] private readonly DomainSupervisor myDomainSupervisor;
    [NotNull] private readonly Dictionary<IPsiModule, InspectionsModuleEntry> myCaches;
    [NotNull] private readonly object mySyncLock;

    public InspectionsCache(
      [NotNull] Lifetime lifetime,
      [NotNull] IPsiModules psiModules,
      [NotNull] ChangeManager changeManager,
      [NotNull] DomainSupervisor domainSupervisor)
    {
      myPsiModules = psiModules;
      myDomainSupervisor = domainSupervisor;
      myCaches = new Dictionary<IPsiModule, InspectionsModuleEntry>();
      mySyncLock = new object();

      changeManager.Changed2.Advise(lifetime, OnChangeManagerChanged);
    }

    [NotNull]
    public DomainSupervisor DomainSupervisor
    {
      get { return myDomainSupervisor; }
    }

    [NotNull]
    public InspectionsModuleEntry GetEntry([NotNull] IPsiModule module)
    {
      lock (mySyncLock)
      {
        InspectionsModuleEntry entry;
        if (myCaches.TryGetValue(module, out entry)) return entry;

        return myCaches[module] = new InspectionsModuleEntry(this, module);
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
        var filesPerCache = new OneToListMap<InspectionsModuleEntry, IPsiSourceFile>();
        lock (mySyncLock)
        {
          foreach (var change in moduleChange.FileChanges)
          {
            InspectionsModuleEntry entry;
            if (myCaches.TryGetValue(change.Item.PsiModule, out entry))
              filesPerCache.Add(entry, change.Item);
          }
        }

        foreach (var cacheFilesPair in filesPerCache)
          cacheFilesPair.Key.Drop(cacheFilesPair.Value);
      }
    }
  }
}