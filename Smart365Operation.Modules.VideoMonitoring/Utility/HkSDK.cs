using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Smart365Operation.Modules.VideoMonitoring.Utility
{
    public class HkSDK
    {
        [DllImport("OpenNetStream.dll")]//SDK启动
        public static extern int OpenSDK_InitLib(string AuthAddr, string Platform, string AppId);

        [DllImport("OpenNetStream.dll")]//SDK关闭
        public static extern int OpenSDK_FiniLib();

        [DllImport("OpenNetStream.dll")]//SDK第三方登陆
        public static extern int OpenSDK_Mid_Login(ref string pToken, ref int TokenLth);

        [DllImport("OpenNetStream.dll")]
        public static extern int OpenSDK_HttpSendWithWait(string szUri, string szHeaderParam, string szBody, out IntPtr iMessage, out int iLength);

        [DllImport("OpenNetStream.dll")]//SDK申请会话
        public static extern int OpenSDK_AllocSession(MsgHandler CallBack, IntPtr UserID, ref IntPtr pSID, ref int SIDLth, bool bSync, uint timeout);
        //及其回调函数格式
        public delegate int MsgHandler(IntPtr SID, uint MsgType, uint Error, string Info, IntPtr pUser);

        //回调实例
        public static int HandlerWork(IntPtr SID, uint MsgType, uint Error, string Info, IntPtr pUser)
        {
            return 0;
        }

        [DllImport("OpenNetStream.dll")]//SDK关闭会话
        public static extern int OpenSDK_FreeSession(string SID);

        [DllImport("OpenNetStream.dll")]//SDK开始播放
        public static extern int OpenSDK_StartRealPlay(IntPtr SID, IntPtr PlayWnd, string CameraId, string Token, int VideoLevel, string SafeKey, string AppKey, uint pNSCBMsg);

        [DllImport("OpenNetStream.dll")]//SDK关闭播放
        public static extern int OpenSDK_StopRealPlay(IntPtr SID, uint pNSCBMsg);

        [DllImport("OpenNetStream.dll")]//截屏
        public static extern int OpenSDK_CapturePicture(IntPtr SID, string szFileName);

        [DllImport("OpenNetStream.dll")]//设置数据回调窗口
        public static extern int OpenSDK_SetDataCallBack(IntPtr sessionId, OpenSDK_DataCallBack pDataCallBack, string pUser);

        [DllImport("OpenNetStream.dll")]//SDK获取所有设备摄像机列表
        public static extern int OpenSDK_Data_GetDevList(string accessToken, int iPageStart, int iPageSize, out IntPtr iMessage, out int iLength);

        [DllImport("OpenNetStream.dll")]//SDK获取设备告警列表
        public static extern int OpenSDK_Data_GetAlarmList(string szAccessToken, string szCameraId, string szStartTime, string szEndTime, int iAlarmType, int iStatus, int iPageStart, int iPageSize, out IntPtr pBuf, out int iLength);

        //回调函数格式
        public delegate void OpenSDK_DataCallBack(CallBackDateType dateType, IntPtr dateContent, int dataLen, string pUser);
        public static void DataCallBackHandler(CallBackDateType dataType, IntPtr dataContent, int dataLen, string pUser)
        {
        }

        //数据回调设置
        public enum CallBackDateType
        {
            NET_DVR_SYSHEAD = 0,
            NET_DVR_STREAMDATA = 1,
            NET_DVR_RECV_END = 2,
        };

        //云台控制命令
        public enum PTZCommand
        {
            UP,
            DOWN,
            LEFT,
            RIGHT,
            UPLEFT,
            DOWNLEFT,
            UPRIGHT,
            DOWNRIGHT,
            ZOOMIN,
            ZOOMOUT,
            FOCUSNEAR,
            FOCUSFAR,
            IRISSTARTUP,
            IRISSTOPDOWN,
            LIGHT,
            WIPER,
            AUTO
        };

        //云台操作命令
        public enum PTZAction
        {
            START,
            STOP
        };
        
        public struct NSCBMsg
        {
            public ushort iErrorCode;
            public string pMessageInfo;
        }

        [DllImport(@"OpenNetStream.dll")]//SDK云台控制
        public static extern int OpenSDK_PTZCtrl(IntPtr szSessionId, string szAccessToken, string szCameraId, PTZCommand enCommand, PTZAction enAction, int iSpeed, IntPtr pNSCBMsg);

        [DllImport(@"OpenNetStream.dll")]//SDK云台控制
        public static extern uint OpenSDK_GetLastErrorCode();
    }
}
