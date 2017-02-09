using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using Prism.Modularity;
using Prism.Regions;

namespace Smart365Operation.Modules.Monitoring
{
    public class MonitoringModule:IModule
    {
        private readonly IRegionManager _regionManager;
        private readonly IUnityContainer _container;

        public MonitoringModule(IUnityContainer container, IRegionManager regionManager)
        {
            _container = container;
            _regionManager = regionManager;
        }

        public void Initialize()
        {
            throw new NotImplementedException();
        }
    }
}
