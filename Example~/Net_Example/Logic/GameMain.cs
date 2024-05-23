using Base.Net;
using Geek.Server.Proto;
using System.Threading.Tasks;
using ClientProto;
using UnityEngine;
using UnityEngine.UI;

namespace Demo.Net
{
    public class GameMain : MonoBehaviour
    {
        public string serverIp = "127.0.0.1";
        public int serverPort = 8899;
        public string userName = "123456";
        
        async void Start()
        {
            PolymorphicRegister.Load();
            GameClient.Instance.Init(() => new NetDisConnectMessage());
            
            DemoService.Singleton.RegisterEventListener();
            
            if (!await ConnectServer())
            {
                return;
            }
            await Login();
            await ReqBagInfo();
            await ReqComposePet();
        }
        
        void Update()
        {
            GameClient.Instance.Update(GED.NED);
        }

        private async Task<bool> ConnectServer()
        {
            return await GameClient.Instance.Connect(serverIp, serverPort);
        }

        private Task Login()
        {
            //登陆
            var req = new ReqLogin();
            req.SdkType = 0;
            req.SdkToken = "";
            req.UserName = userName;
            req.Device = SystemInfo.deviceUniqueIdentifier;
            if (Application.platform == RuntimePlatform.Android)
                req.Platform = "android";
            else if (Application.platform == RuntimePlatform.IPhonePlayer)
                req.Platform = "ios";
            else
                req.Platform = "unity";
            return DemoService.Singleton.SendMsg(req);
        }

        private Task ReqBagInfo()
        {
            ReqBagInfo req = new ReqBagInfo();
            return DemoService.Singleton.SendMsg(req);
        }

        private Task ReqComposePet()
        {
            ReqComposePet req = new ReqComposePet();
            req.FragmentId = 1000;
            return DemoService.Singleton.SendMsg(req);
        }


        private void OnApplicationQuit()
        {
            Debug.Log("OnApplicationQuit");
            GameClient.Instance.Close();
            MsgWaiter.DisposeAll();
        }

    }
}

