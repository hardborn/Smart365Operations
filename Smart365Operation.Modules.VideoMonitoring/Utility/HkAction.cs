using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Smart365Operation.Modules.VideoMonitoring.Utility
{
    public class HkAction
    {
        //访问令牌，通过接口调用获得
        public static string AccessToken = null;
        private static string ApiUrl = "https://open.ys7.com/api/method";
        private static string AppKey = "b42937c70f3d491ca2083a03b97d3bec";//ConfigurationManager.AppSettings["AppKey"].ToString();
        private static string AuthAddr = "https://auth.ys7.com";
        public static bool IsLoaded = false;//记录登录状态
        private static string PhoneNumber = "15249295796";//ConfigurationManager.AppSettings["PhoneNumber"].ToString();
        private static string PlatformAddr = "https://open.ys7.com";
        private static string SecretKey = "ae59e1e816e77391f6ed5349836fc024";// ConfigurationManager.AppSettings["SecretKey"].ToString();
        public static IntPtr SessionId;
        public static int SessionIdLth;
        public static string SessionIdstr;
        private static int TokenLth = 0;
        private static int level = 2;//清晰度，0流畅，1标清，2高清
        public static string userId = "1b14cf56-b61d-43f5-a2c8-62b9626cbaaf";//自定义用户名，用guid防止重复
        private static int ptz_speed = 5; //默认使用云台控制速度为7
        private static string _SafeKey = "FRXQJH";

        //分配会话后，调用方法之后执行的回调函数
        static HkSDK.MsgHandler callBack = new HkSDK.MsgHandler(HkAction.HandlerWork);

        //初始化库
        public static bool Start()
        {
            return (HkSDK.OpenSDK_InitLib(AuthAddr, PlatformAddr, AppKey) == 0);
        }

        //反初始化库
        public static bool Close()
        {
            return (HkSDK.OpenSDK_FiniLib() == 0);
        }

        //获取accesstoken
        public static string GetAccessToken()
        {
            IntPtr iMessage;
            int iLength;

            string jsonStr = BuildParams("token");
            int iResult = HkSDK.OpenSDK_HttpSendWithWait(ApiUrl, jsonStr, "", out iMessage, out iLength);
            string strResult = Marshal.PtrToStringAnsi(iMessage, iLength);
            if (iResult == 0)
            {
                JObject jsonResult = (JObject)JsonConvert.DeserializeObject(strResult);
                if (jsonResult["result"]["code"].ToString() == "200")
                {
                    AccessToken = jsonResult["result"]["data"]["accessToken"].ToString();
                    Debug.WriteLine(AccessToken);
                }
                else
                {
                    Debug.WriteLine(jsonResult["result"]["code"].ToString());
                }
            }
            return AccessToken;
        }

        //处理json获取token
        public static string BuildParams(string _type)
        {
            string str = string.Empty;
            string str6 = _type.ToLower();
            if (str6 != null)
            {
                if (!(str6 == "token"))
                {
                    if (str6 == "msg")
                    {
                        str = "msg/get";
                    }
                    else
                    {
                        str = "msg/get";
                    }
                }
                else
                {
                    str = "token/getAccessToken";
                }
            }
            TimeSpan span = (TimeSpan)(DateTime.Now - TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(0x7b2, 1, 1, 0, 0, 0)));
            string str2 = Math.Round(span.TotalSeconds, 0).ToString();
            string str3 = MD5Encrypt("phone:" + PhoneNumber + ",method:" + str + ",time:" + str2 + ",secret:" + SecretKey, 0x20).ToLower();
            return ("{\"id\":\"100\",\"system\":{\"key\":\"" + AppKey + "\",\"sign\":\"" + str3 + "\",\"time\":\"" + str2 + "\",\"ver\":\"1.0\"},\"method\":\"" + str + "\",\"params\":{\"phone\":\"" + PhoneNumber + "\"}}");
        }

        //MD5加密
        public static string MD5Encrypt(string str, int code)
        {
            MD5 md = new MD5CryptoServiceProvider();
            Encoding encoding = Encoding.UTF8;
            byte[] buffer = md.ComputeHash(encoding.GetBytes(str));
            StringBuilder builder = new StringBuilder(code);
            for (int i = 0; i < buffer.Length; i++)
            {
                builder.Append(buffer[i].ToString("x").PadLeft(2, '0'));
            }
            return builder.ToString();
        }

        //第三方登录
        public static bool Login()
        {
            return (HkSDK.OpenSDK_Mid_Login(ref AccessToken, ref TokenLth) == 0);
        }

        //申请会话
        public static IntPtr AllocSession()
        {
            IntPtr userID = Marshal.StringToHGlobalAnsi(userId);
            bool flag = HkSDK.OpenSDK_AllocSession(callBack, userID, ref SessionId, ref SessionIdLth, false, uint.MaxValue) == 0;
            SessionIdstr = Marshal.PtrToStringAnsi(SessionId, SessionIdLth);
            return SessionId;
        }

        /*
        public static bool allocion()
        {
            HkSDK.MsgHandler Handler = new HkSDK.MsgHandler(HkSDK.HandlerWork);
            IntPtr UserID =Marshal.StringToHGlobalAnsi(userId);
            bool result = (HkSDK.OpenSDK_AllocSession(Handler, UserID, ref SessionId, ref SessionIdLth, false, 0xEA60) == 0);
            SessionIdstr = Marshal.PtrToStringAnsi(SessionId, SessionIdLth);
            return SessionIdstr;
        }*/

        //结束会话
        public static bool closeAllocion(IntPtr sid)
        {
            string sid1 = sid.ToString();
            return (HkSDK.OpenSDK_FreeSession(sid1) == 0);
        }

        //结束会话重载无参
        public static bool closeAllocionAll()
        {
            string sid1 = SessionId.ToString();
            return (HkSDK.OpenSDK_FreeSession(sid1) == 0);
        }

        //申请设备列表
        public static string playList()
        {
            IntPtr iMessage;
            int iLength;
            string strResult = string.Empty;

            int iResult = HkSDK.OpenSDK_Data_GetDevList(AccessToken, 0, 50, out iMessage, out iLength);
            if (iResult == 0)
            {
                strResult = Marshal.PtrToStringAnsi(iMessage);
                JObject jsonResult = (JObject)JsonConvert.DeserializeObject(strResult);
            }
            //Marshal.FreeHGlobal(iMessage); 
            return strResult;
        }

        //申请告警列表
        public static string AlarmList(string CameraId, string StartTime, string EndTime)
        {
            IntPtr iMessage;
            int iLength;
            int iResult = HkSDK.OpenSDK_Data_GetAlarmList(AccessToken, CameraId, StartTime, EndTime, -1, 2, 0, 50, out iMessage, out iLength);
            string strResult = Marshal.PtrToStringAnsi(iMessage);
            if (iResult == 0)
            {
                JObject result = (JObject)JsonConvert.DeserializeObject(strResult);
            }
            //Marshal.FreeHGlobal(iMessage); 
            return strResult;
        }

        //播放视频（预览）
        public static bool Play(IntPtr PlayWnd, string CameraId, IntPtr SessionId)
        {
            //MessageBox.Show("AAAAA：\r\n" + SessionId.ToString() + "\r\n" + PlayWnd.ToString() + "\r\n" + CameraId + "\r\n" + AccessToken + "\r\n" + level + "\r\n" + _SafeKey + "\r\n" + AppKey, "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return (HkSDK.OpenSDK_StartRealPlay(SessionId, PlayWnd, CameraId, AccessToken, level, _SafeKey, AppKey, 0) == 0);
        }

        //停止播放（预览）
        public static bool Stop(IntPtr SessionId)
        {
            closeAllocion(SessionId);//每次播放结束会话            
            return HkSDK.OpenSDK_StopRealPlay(SessionId, 0) == 0;
        }

        //停止播放（预览）重载无参
        public static bool StopAll()
        {
            closeAllocionAll();//每次播放结束会话
            if (HkSDK.OpenSDK_StopRealPlay(SessionId, 0) == 0)
                return HkSDK.OpenSDK_StopRealPlay(SessionId, 0) == 0;
            else
                return false;
        }

        //截图
        public static bool CapturePicture(string fileName)
        {
            if (!String.IsNullOrWhiteSpace(fileName))
            {
                return HkSDK.OpenSDK_CapturePicture(SessionId, fileName) == 0 ? true : false;
            }
            return false;
        }

        //回调函数
        public static int HandlerWork(IntPtr SessionId, uint MsgType, uint Error, string Info, IntPtr pUser)
        {
            switch (MsgType)
            {
                case 20:
                    JObject obj = (JObject)JsonConvert.DeserializeObject(Info);
                    if (Error == 0)
                    {
//                        PlayMainWindow.jObjInfo = obj;
//                        PlayMainWindow.isOpertion = 2;
                    }
                    break;
                case 3:// 播放开始
                    break;
                case 4:// 播放终止
                    break;
                case 5:// 播放结束，回放结束时会有此消息
//                    PlayMainWindow.mType = PlayMainWindow.MessageType.INS_PLAY_ARCHIVE_END;
                    break;
                default:
                    break;
            }
            return 0;
        }

        public int MsgHandler1(IntPtr SessionId, uint MsgType, uint Error, string Info, IntPtr pUser)
        {
            return 1;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LP_NSCBMsg
        {
            public ushort iErrorCode;
            public string pMessageInfo;
        }

        public static bool IsOpertion { get; set; }//1已处理  0未处理  2正在处理

        public static void on_insPtzStart(IntPtr SessionId, string CameraId, HkSDK.PTZCommand emDirect)
        {
            if (SessionId == null)
            {
                MessageBox.Show("请先开启预览才能进行云台控制！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int iRet = 0;
            HkSDK.NSCBMsg cbMsg;
            cbMsg.iErrorCode = 0;
            cbMsg.pMessageInfo = "";
            IntPtr pMsg = Marshal.AllocHGlobal(Marshal.SizeOf(cbMsg));

            try
            {
                Marshal.StructureToPtr(cbMsg, pMsg, false);
                switch (emDirect)
                {
                    case HkSDK.PTZCommand.UP:
                        iRet = HkSDK.OpenSDK_PTZCtrl(SessionId, AccessToken, CameraId, HkSDK.PTZCommand.UP, HkSDK.PTZAction.START, ptz_speed, pMsg);
                        break;
                    case HkSDK.PTZCommand.DOWN:
                        iRet = HkSDK.OpenSDK_PTZCtrl(SessionId, AccessToken, CameraId, HkSDK.PTZCommand.DOWN, HkSDK.PTZAction.START, ptz_speed, pMsg);
                        break;
                    case HkSDK.PTZCommand.LEFT:
                        iRet = HkSDK.OpenSDK_PTZCtrl(SessionId, AccessToken, CameraId, HkSDK.PTZCommand.LEFT, HkSDK.PTZAction.START, ptz_speed, pMsg);
                        break;
                    case HkSDK.PTZCommand.RIGHT:
                        iRet = HkSDK.OpenSDK_PTZCtrl(SessionId, AccessToken, CameraId, HkSDK.PTZCommand.RIGHT, HkSDK.PTZAction.START, ptz_speed, pMsg);
                        break;
                    case HkSDK.PTZCommand.UPLEFT:
                        iRet = HkSDK.OpenSDK_PTZCtrl(SessionId, AccessToken, CameraId, HkSDK.PTZCommand.UPLEFT, HkSDK.PTZAction.START, ptz_speed, pMsg);
                        break;
                    case HkSDK.PTZCommand.DOWNLEFT:
                        iRet = HkSDK.OpenSDK_PTZCtrl(SessionId, AccessToken, CameraId, HkSDK.PTZCommand.DOWNLEFT, HkSDK.PTZAction.START, ptz_speed, pMsg);
                        break;
                    case HkSDK.PTZCommand.UPRIGHT:
                        iRet = HkSDK.OpenSDK_PTZCtrl(SessionId, AccessToken, CameraId, HkSDK.PTZCommand.UPRIGHT, HkSDK.PTZAction.START, ptz_speed, pMsg);
                        break;
                    case HkSDK.PTZCommand.DOWNRIGHT:
                        iRet = HkSDK.OpenSDK_PTZCtrl(SessionId, AccessToken, CameraId, HkSDK.PTZCommand.DOWNRIGHT, HkSDK.PTZAction.START, ptz_speed, pMsg);
                        break;
                    case HkSDK.PTZCommand.ZOOMIN:
                        iRet = HkSDK.OpenSDK_PTZCtrl(SessionId, AccessToken, CameraId, HkSDK.PTZCommand.ZOOMIN, HkSDK.PTZAction.START, ptz_speed, pMsg);
                        break;
                    case HkSDK.PTZCommand.ZOOMOUT:
                        iRet = HkSDK.OpenSDK_PTZCtrl(SessionId, AccessToken, CameraId, HkSDK.PTZCommand.ZOOMOUT, HkSDK.PTZAction.START, ptz_speed, pMsg);
                        break;
                    case HkSDK.PTZCommand.FOCUSNEAR:
                        iRet = HkSDK.OpenSDK_PTZCtrl(SessionId, AccessToken, CameraId, HkSDK.PTZCommand.FOCUSNEAR, HkSDK.PTZAction.START, ptz_speed, pMsg);
                        break;
                    case HkSDK.PTZCommand.FOCUSFAR:
                        iRet = HkSDK.OpenSDK_PTZCtrl(SessionId, AccessToken, CameraId, HkSDK.PTZCommand.FOCUSFAR, HkSDK.PTZAction.START, ptz_speed, pMsg);
                        break;
                    case HkSDK.PTZCommand.IRISSTARTUP:
                        iRet = HkSDK.OpenSDK_PTZCtrl(SessionId, AccessToken, CameraId, HkSDK.PTZCommand.IRISSTARTUP, HkSDK.PTZAction.START, ptz_speed, pMsg);
                        break;
                    case HkSDK.PTZCommand.IRISSTOPDOWN:
                        iRet = HkSDK.OpenSDK_PTZCtrl(SessionId, AccessToken, CameraId, HkSDK.PTZCommand.IRISSTOPDOWN, HkSDK.PTZAction.START, ptz_speed, pMsg);
                        break;
                    case HkSDK.PTZCommand.LIGHT:
                        iRet = HkSDK.OpenSDK_PTZCtrl(SessionId, AccessToken, CameraId, HkSDK.PTZCommand.LIGHT, HkSDK.PTZAction.START, ptz_speed, pMsg);
                        break;
                    case HkSDK.PTZCommand.WIPER:
                        iRet = HkSDK.OpenSDK_PTZCtrl(SessionId, AccessToken, CameraId, HkSDK.PTZCommand.WIPER, HkSDK.PTZAction.START, ptz_speed, pMsg);
                        break;
                    case HkSDK.PTZCommand.AUTO:
                        iRet = HkSDK.OpenSDK_PTZCtrl(SessionId, AccessToken, CameraId, HkSDK.PTZCommand.AUTO, HkSDK.PTZAction.START, ptz_speed, pMsg);
                        break;
                    default:
                        break;
                }
            }
            finally
            {
                Marshal.FreeHGlobal(pMsg);
            }

            if (iRet != 0)
            {
                uint uError = HkSDK.OpenSDK_GetLastErrorCode();
                MessageBox.Show("PTZ控制失败！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public static void on_insPtzStop(IntPtr SessionId, string CameraId, HkSDK.PTZCommand emDirect)
        {
            if (SessionId == null)
            {
                MessageBox.Show("请先开启预览才能进行云台控制！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int iRet = 0;
            HkSDK.NSCBMsg cbMsg;
            cbMsg.iErrorCode = 0;
            cbMsg.pMessageInfo = "";
            IntPtr pMsg = Marshal.AllocHGlobal(Marshal.SizeOf(cbMsg));

            try
            {
                Marshal.StructureToPtr(cbMsg, pMsg, false);
                switch (emDirect)
                {
                    case HkSDK.PTZCommand.UP:
                        iRet = HkSDK.OpenSDK_PTZCtrl(SessionId, AccessToken, CameraId, HkSDK.PTZCommand.UP, HkSDK.PTZAction.STOP, ptz_speed, pMsg);
                        break;
                    case HkSDK.PTZCommand.DOWN:
                        iRet = HkSDK.OpenSDK_PTZCtrl(SessionId, AccessToken, CameraId, HkSDK.PTZCommand.DOWN, HkSDK.PTZAction.STOP, ptz_speed, pMsg);
                        break;
                    case HkSDK.PTZCommand.LEFT:
                        iRet = HkSDK.OpenSDK_PTZCtrl(SessionId, AccessToken, CameraId, HkSDK.PTZCommand.LEFT, HkSDK.PTZAction.STOP, ptz_speed, pMsg);
                        break;
                    case HkSDK.PTZCommand.RIGHT:
                        iRet = HkSDK.OpenSDK_PTZCtrl(SessionId, AccessToken, CameraId, HkSDK.PTZCommand.RIGHT, HkSDK.PTZAction.STOP, ptz_speed, pMsg);
                        break;
                    case HkSDK.PTZCommand.UPLEFT:
                        iRet = HkSDK.OpenSDK_PTZCtrl(SessionId, AccessToken, CameraId, HkSDK.PTZCommand.UPLEFT, HkSDK.PTZAction.STOP, ptz_speed, pMsg);
                        break;
                    case HkSDK.PTZCommand.DOWNLEFT:
                        iRet = HkSDK.OpenSDK_PTZCtrl(SessionId, AccessToken, CameraId, HkSDK.PTZCommand.DOWNLEFT, HkSDK.PTZAction.STOP, ptz_speed, pMsg);
                        break;
                    case HkSDK.PTZCommand.UPRIGHT:
                        iRet = HkSDK.OpenSDK_PTZCtrl(SessionId, AccessToken, CameraId, HkSDK.PTZCommand.UPRIGHT, HkSDK.PTZAction.STOP, ptz_speed, pMsg);
                        break;
                    case HkSDK.PTZCommand.DOWNRIGHT:
                        iRet = HkSDK.OpenSDK_PTZCtrl(SessionId, AccessToken, CameraId, HkSDK.PTZCommand.DOWNRIGHT, HkSDK.PTZAction.STOP, ptz_speed, pMsg);
                        break;
                    case HkSDK.PTZCommand.ZOOMIN:
                        iRet = HkSDK.OpenSDK_PTZCtrl(SessionId, AccessToken, CameraId, HkSDK.PTZCommand.ZOOMIN, HkSDK.PTZAction.STOP, ptz_speed, pMsg);
                        break;
                    case HkSDK.PTZCommand.ZOOMOUT:
                        iRet = HkSDK.OpenSDK_PTZCtrl(SessionId, AccessToken, CameraId, HkSDK.PTZCommand.ZOOMOUT, HkSDK.PTZAction.STOP, ptz_speed, pMsg);
                        break;
                    case HkSDK.PTZCommand.FOCUSNEAR:
                        iRet = HkSDK.OpenSDK_PTZCtrl(SessionId, AccessToken, CameraId, HkSDK.PTZCommand.FOCUSNEAR, HkSDK.PTZAction.STOP, ptz_speed, pMsg);
                        break;
                    case HkSDK.PTZCommand.FOCUSFAR:
                        iRet = HkSDK.OpenSDK_PTZCtrl(SessionId, AccessToken, CameraId, HkSDK.PTZCommand.FOCUSFAR, HkSDK.PTZAction.STOP, ptz_speed, pMsg);
                        break;
                    case HkSDK.PTZCommand.IRISSTARTUP:
                        iRet = HkSDK.OpenSDK_PTZCtrl(SessionId, AccessToken, CameraId, HkSDK.PTZCommand.IRISSTARTUP, HkSDK.PTZAction.STOP, ptz_speed, pMsg);
                        break;
                    case HkSDK.PTZCommand.IRISSTOPDOWN:
                        iRet = HkSDK.OpenSDK_PTZCtrl(SessionId, AccessToken, CameraId, HkSDK.PTZCommand.IRISSTOPDOWN, HkSDK.PTZAction.STOP, ptz_speed, pMsg);
                        break;
                    case HkSDK.PTZCommand.LIGHT:
                        iRet = HkSDK.OpenSDK_PTZCtrl(SessionId, AccessToken, CameraId, HkSDK.PTZCommand.LIGHT, HkSDK.PTZAction.STOP, ptz_speed, pMsg);
                        break;
                    case HkSDK.PTZCommand.WIPER:
                        iRet = HkSDK.OpenSDK_PTZCtrl(SessionId, AccessToken, CameraId, HkSDK.PTZCommand.WIPER, HkSDK.PTZAction.STOP, ptz_speed, pMsg);
                        break;
                    case HkSDK.PTZCommand.AUTO:
                        iRet = HkSDK.OpenSDK_PTZCtrl(SessionId, AccessToken, CameraId, HkSDK.PTZCommand.AUTO, HkSDK.PTZAction.STOP, ptz_speed, pMsg);
                        break;
                    default:
                        break;
                }
            }
            finally
            {
                Marshal.FreeHGlobal(pMsg);
            }

            if (iRet != 0)
            {
                MessageBox.Show("PTZ控制失败！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
