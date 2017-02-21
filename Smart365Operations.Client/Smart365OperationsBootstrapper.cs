using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Unity;
using System.Windows;
using Microsoft.Practices.Unity;
using MvvmDialogs;
using Prism.Modularity;
using Smart365Operation.Modules.VideoMonitoring.Services;
using Smart365Operations.Client.Services;
using Smart365Operations.Common.Infrastructure.Interfaces;

namespace Smart365Operations.Client
{
    public class Smart365OperationsBootstrapper: UnityBootstrapper
    {
        protected override DependencyObject CreateShell()
        {
           return Container.Resolve<Shell>();
        }
        //protected override void InitializeShell()
        //{
        //    base.InitializeShell();

        //    App.Current.MainWindow = (Window)this.Shell;
        //    App.Current.MainWindow.Show();
        //}

        protected override void ConfigureModuleCatalog()
        {
            ModuleCatalog moduleCatalog = (ModuleCatalog)this.ModuleCatalog;
            //moduleCatalog.AddModule(typeof(EmployeeModule));
            //moduleCatalog.AddModule(typeof(NavigationModule));
            //moduleCatalog.AddModule(typeof(SoftwareModule), InitializationMode.OnDemand);
            //moduleCatalog.AddModule(typeof(HardwareModule), InitializationMode.OnDemand);
            //moduleCatalog.AddModule(typeof(RequestModule), InitializationMode.OnDemand);
        }

        public void Show()
        {
            App.Current.MainWindow = (Window)this.Shell;
            App.Current.MainWindow.Show();
        }

        protected override void ConfigureContainer()
        {
            Container.RegisterType<IAuthenticationService, AuthenticationService>();
            Container.RegisterType<IDialogService, DialogService>(new ContainerControlledLifetimeManager());
            Container.RegisterType<ICameraService, CameraService>();
            Container.RegisterType<ICustomerService, CustomerService>();
            base.ConfigureContainer();
        }
    }
}
