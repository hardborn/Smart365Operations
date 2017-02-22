﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Smart365Operations.Client.ViewModels;
using Smart365Operations.Client.Views;
using Smart365Operations.Common.Infrastructure.Interfaces;
using Smart365Operations.Common.Infrastructure.Models;
using Microsoft.Practices.Unity;
using MvvmDialogs;
using Prism.Logging;

namespace Smart365Operations.Client
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            SystemPrincipal customPrincipal = new SystemPrincipal();
            AppDomain.CurrentDomain.SetThreadPrincipal(customPrincipal);

            base.OnStartup(e);

            Smart365OperationsBootstrapper bootStrapper = new Smart365OperationsBootstrapper();
            bootStrapper.Run();

            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            LoginScreen loginWindow = bootStrapper.Container.Resolve<LoginScreen>();
            //AuthenticationViewModel viewModel =
            //    new AuthenticationViewModel(bootStrapper.Container.Resolve<IAuthenticationService>());
            //loginWindow.DataContext = viewModel;
            bool? logonResult = loginWindow.ShowDialog();
            var viewModel = loginWindow.ViewModel as AuthenticationViewModel;
            if (logonResult.HasValue && viewModel != null && viewModel.IsAuthenticated)
            {
                bootStrapper.Show();
                Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
            }
            else
            {
                Application.Current.Shutdown(1);
            }
        }
    }
}
