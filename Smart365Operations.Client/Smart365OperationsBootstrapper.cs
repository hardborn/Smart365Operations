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
using Prism.Logging;
using Prism.Modularity;
using Smart365Operation.Modules.Log4NetLogger;
using Smart365Operation.Modules.VideoMonitoring;
using Smart365Operation.Modules.VideoMonitoring.Services;
using Smart365Operation.Modules.VideoMonitoring.ViewModels;
using Smart365Operations.Client.Services;
using Smart365Operations.Client.ViewModels;
using Smart365Operations.Client.Views;
using Smart365Operations.Common.Infrastructure.Interfaces;

namespace Smart365Operations.Client
{
    public class Smart365OperationsBootstrapper : UnityBootstrapper
    {
        private readonly Log4NetLogger _logger = new Log4NetLogger();
        protected override ILoggerFacade CreateLogger()
        {
            return _logger;
        }

        protected override DependencyObject CreateShell()
        {
            Shell view = this.Container.TryResolve<Shell>();
            return view;
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
            moduleCatalog.AddModule(typeof(VideoMonitoringModule));
        }

        public void Show()
        {
            App.Current.MainWindow = (Window)this.Shell;
            App.Current.MainWindow.Show();
        }

        protected override void ConfigureContainer()
        {
            base.ConfigureContainer();
            Container.RegisterType<IAuthenticationService, AuthenticationService>();
            Container.RegisterType<IDialogService, DialogService>(new ContainerControlledLifetimeManager());
            //Container.RegisterType<ICameraService, CameraService>();
            //Container.RegisterType<ICustomerService, CustomerService>();
        }
    }
}
