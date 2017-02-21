using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Mvvm;
using Smart365Operations.Common.Infrastructure.Models;

namespace Smart365Operation.Modules.VideoMonitoring.ViewModels
{
    public class CameraViewModel : BindableBase
    {
        private Camera _camera;
        private string _name;
        private string _id;
        private int _status;

        public CameraViewModel(Camera camera)
        {
            this._camera = camera;
            _name = camera.Name;
            _id = camera.Id;
            _status = camera.Status;
        }


        public string CameraName
        {
            get { return _name; }
            set { SetProperty(ref _name, value); }
        }

        public string CameraId
        {
            get { return _id; }
            set { SetProperty(ref _id, value); }
        }

        public int CameraStatus
        {
            get { return _status; }
            set { SetProperty(ref _status, value); }
        }

        public DelegateCommand PlayVideoCommand => new DelegateCommand(PlayVideo, CanPlayVideo);

        private void PlayVideo()
        {
      
        }

        private bool CanPlayVideo()
        {
            return Convert.ToBoolean(_status);
        }
    }
}
