using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using Microsoft.Practices.Unity;
using MvvmDialogs;
using Prism.Commands;
using Prism.Logging;
using Prism.Mvvm;
using Smart365Operations.Common.Infrastructure.Interfaces;
using Smart365Operations.Common.Infrastructure.Models;

namespace Smart365Operations.Client.ViewModels
{
    public class AuthenticationViewModel : BindableBase, IViewModel
    {

        private readonly IAuthenticationService _authenticationService;
        public AuthenticationViewModel(IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        [Dependency]
        public ILoggerFacade Logger { get; set; }

        private string _userName;
        public string UserName
        {
            get { return _userName; }
            set { SetProperty(ref _userName, value); }
        }

        private string _password;
        public string Password
        {
            get { return _password; }
            set { SetProperty(ref _password, value); }
        }

        private string _authenticatedUser;
        public string AuthenticatedUser
        {
            get { return _authenticatedUser; }
            set { SetProperty(ref _authenticatedUser, value); }
        }

        private bool _isAuthenticated;
        public bool IsAuthenticated
        {
            get { return _isAuthenticated = Thread.CurrentPrincipal.Identity.IsAuthenticated; }
            private set { SetProperty(ref _isAuthenticated, value); }
        }

        private DelegateCommand<object> _loginCommand;
        public DelegateCommand<object> LoginCommand => _loginCommand ?? (_loginCommand = new DelegateCommand<object>(Login, view => CanLogin()));

        private bool CanLogin()
        {
            return !IsAuthenticated;
        }

        private void Login(object view)
        {
            try
            {
                User user = _authenticationService.AuthenticateUser(UserName, Password);
                Logger.Log($"User:{UserName} Password:{Password}", Category.Debug, Priority.Medium);
                //Get the current principal object
                SystemPrincipal customPrincipal = Thread.CurrentPrincipal as SystemPrincipal;
                if (customPrincipal == null)
                {
                    Logger.Log(
                        $"The application\'s default thread principal must be set to a CustomPrincipal object on startup",
                        Category.Exception, Priority.High);
                    throw new ArgumentException("The application's default thread principal must be set to a CustomPrincipal object on startup.");
                }

                //Authenticate the user
                customPrincipal.Identity = new SystemIdentity(user.Id, user.Username, user.Email, user.Roles);

                _loginCommand.RaiseCanExecuteChanged();
                IView loginView = view as IView;
                loginView.Close();
            }
            catch (UnauthorizedAccessException)
            {
                Logger.Log($"Login failed! Please provide some valid credentials", Category.Exception, Priority.High);
            }
            catch (Exception ex)
            {
                Logger.Log($"ERROR: {ex.Message}", Category.Exception, Priority.High);
            }
        }


    }
}
