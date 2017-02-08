﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using MvvmDialogs;
using Prism.Commands;
using Prism.Mvvm;
using Smart365Operations.Common.Infrastructure.Interfaces;
using Smart365Operations.Common.Infrastructure.Models;

namespace Smart365Operations.Client.ViewModels
{
    public class AuthenticationViewModel : BindableBase, IViewModel
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly IDialogService _dialogService;
        public AuthenticationViewModel(IDialogService dialogService, IAuthenticationService authenticationService)
        {
            _dialogService = dialogService;
            _authenticationService = authenticationService;
        }

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

        private DelegateCommand _loginCommand;
        public DelegateCommand LoginCommand => _loginCommand ?? (_loginCommand = new DelegateCommand(Login, CanLogin));

        private bool CanLogin()
        {
            return !IsAuthenticated;
        }

        private void Login()
        {
            try
            {
                //Validate credentials(证书) through the authentication service
                User user = _authenticationService.AuthenticateUser(UserName, Password);

                //Get the current principal object
                CustomPrincipal customPrincipal = Thread.CurrentPrincipal as CustomPrincipal;
                if (customPrincipal == null)
                    throw new ArgumentException("The application's default thread principal must be set to a CustomPrincipal object on startup.");

                //Authenticate the user
                customPrincipal.Identity = new CustomIdentity(user.Username, user.Email, user.Roles);

                //Update UI
                //NotifyPropertyChanged("AuthenticatedUser");
                //NotifyPropertyChanged("IsAuthenticated");
                _loginCommand.RaiseCanExecuteChanged();
                var windows = System.Windows.Application.Current.Windows;
                //for (var i = 0; i < windows.Count; i++)
                //{
                //    if (windows[i].DataContext == this)
                //        _dialogService.Close(windows[i]);
                //}
                //_logoutCommand.RaiseCanExecuteChanged();
                //Username = string.Empty; //reset
                //passwordBox.Password = string.Empty; //reset
                //Status = string.Empty;
            }
            catch (UnauthorizedAccessException)
            {
                //Status = "Login failed! Please provide some valid credentials(证书).";
            }
            catch (Exception ex)
            {
               // Status = string.Format("ERROR: {0}", ex.Message);
            }
        }

        
    }
}