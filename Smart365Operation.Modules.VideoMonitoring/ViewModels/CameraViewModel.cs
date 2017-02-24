using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Mvvm;
using Smart365Operation.Modules.VideoMonitoring.Services;
using Smart365Operations.Common.Infrastructure.Models;

namespace Smart365Operation.Modules.VideoMonitoring.ViewModels
{
    public class CameraViewModel : BindableBase
    {
        private readonly IVideoService _videoService;
        private Camera _camera;
        private string _name;
        private string _id;
        private int _status;

        public CameraViewModel(IVideoService videoService, Camera camera)
        {
            _videoService = videoService;
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

        private DelegateCommand _playVideoCommand;

        public DelegateCommand PlayVideoCommand
        {
            get
            {
                if (_playVideoCommand == null)
                {
                    _playVideoCommand = new DelegateCommand(PlayVideo, CanPlayVideo);
                }
                return _playVideoCommand;
            }
        }

        private void PlayVideo()
        {
            _videoService.Play(CameraId);
        }

        private bool CanPlayVideo()
        {
            return true;
        }
    }
}
