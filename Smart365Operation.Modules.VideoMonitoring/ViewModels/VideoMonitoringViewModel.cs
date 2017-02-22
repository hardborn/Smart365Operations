using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Mvvm;
using Smart365Operations.Common.Infrastructure.Interfaces;
using Smart365Operations.Common.Infrastructure.Models;

namespace Smart365Operation.Modules.VideoMonitoring.ViewModels
{
    public class VideoMonitoringViewModel:BindableBase
    {
        private readonly ICustomerService _customerService;
        private readonly ICameraService _cameraService;

        public VideoMonitoringViewModel(ICustomerService customerService, ICameraService cameraService)
        {
            _customerService = customerService;
            _cameraService = cameraService;
        }

        private ObservableCollection<CustomerViewModel> _customerList = new ObservableCollection<CustomerViewModel>();
        public ObservableCollection<CustomerViewModel> CustomerList
        {
            get { return _customerList; }
            set { SetProperty(ref _customerList, value); }
        }

        public DelegateCommand InitializeCommand => new DelegateCommand(Initialize, CanInitialize);

        private bool CanInitialize()
        {
            return true;
        }

        private void Initialize()
        {
            var customerList = _customerService.GetCustomersBy(1);
            foreach (var customer in customerList)
            {
                var cameraList = _cameraService.GetCamerasBy(customer.Id);

                CustomerViewModel customerViewModel = new CustomerViewModel(customer, cameraList);
                CustomerList.Add(customerViewModel);
            }
        }
    }
}
