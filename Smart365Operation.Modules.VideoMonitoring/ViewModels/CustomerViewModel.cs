using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;
using Smart365Operations.Common.Infrastructure.Models;
using System.Collections.ObjectModel;

namespace Smart365Operation.Modules.VideoMonitoring.ViewModels
{
    public class CustomerViewModel : BindableBase
    {
        private Customer _customer;

        public CustomerViewModel(Customer customer, IList<Camera> cameraList)
        {
            this._customer = customer;
            this._name = _customer.Name;
            foreach (var camera in cameraList)
            {
                CameraList.Add(new CameraViewModel(camera));
            }
        }

        private string _name = string.Empty;
        public string Name
        {
            get { return _name; }
            set { SetProperty(ref _name, value); }
        }

        private ObservableCollection<CameraViewModel> _cameraList = new ObservableCollection<CameraViewModel>();
        public ObservableCollection<CameraViewModel> CameraList
        {
            get { return _cameraList; }
            set { SetProperty(ref _cameraList, value); }
        }
    }
}
