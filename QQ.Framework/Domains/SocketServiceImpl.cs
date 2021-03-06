using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using QQ.Framework.Packets;
using QQ.Framework.Packets.Send.Login;
using QQ.Framework.Utils;

namespace QQ.Framework.Domains
{
    public class SocketServiceImpl : ISocketService
    {
        private readonly QQUser _user;

        /// <summary>
        ///     Socket连接
        /// </summary>
        private readonly Socket _server;

        /// <summary>
        ///     服务器地址
        /// </summary>
        private string _host;

        /// <summary>
        ///     登录端口
        /// </summary>
        private readonly int _port = 8000;

        private EndPoint _point;


        public SocketServiceImpl(QQUser user)
        {
            _user = user;
            _server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _host = Util.GetHostAddresses("sz6.tencent.com"); ////sz.tencent.com,sz{2-9}.tencent.com
            _user.TXProtocol.DwServerIP = _host;
            _port = _user.TXProtocol.WServerPort;
            _point = new IPEndPoint(IPAddress.Parse(_host), _port);
        }

        public ReceiveData Receive()
        {
            EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0); //用来保存发送方的ip和端口号
            var buffer = new byte[QQGlobal.QQPacketMaxSize];
            var len = _server.ReceiveFrom(buffer, ref endPoint);

            return new ReceiveData
            {
                Data = buffer,
                DataLength = len,
                From = endPoint
            };
        }

        public void Send(SendPacket packet)
        {
            _server.SendTo(packet.WriteData(), _point);
        }

        public void RefreshHost(string host)
        {
            _host = host;
            _point = new IPEndPoint(IPAddress.Parse(_host), _port);
        }

        public virtual void MessageLog(string content,MsgType type)
        {
            if (_user.LoggerHandler != null)
                _user.LoggerHandler.MessageLog(content, _user.QQ, type);
            else
                Console.WriteLine($"{DateTime.Now.ToString()}--{content}");
        }

        public void Login()
        {
            Send(new Send_0X0825(_user, false));
            MessageLog($"登录服务器{_host}",MsgType.INFO);
        }

        public virtual void LoginCallback(bool isSuccess, string message) {
            if(isSuccess) {
        	MessageLog($"登录成功: {message}", MsgType.INFO);
            } else {
        	MessageLog($"登录失败: {message}",MsgType.WARN);
            }
        }

        public virtual void ReceiveVerifyCode(byte[] data)
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "yanzhengma");
            var img = ImageHelper.CreateImageFromBytes(path, data);
            Console.Write($"请输入验证码({img}):",MsgType.INFO);
            var code = Console.ReadLine();
            if (!string.IsNullOrEmpty(code))
            {
                Send(new Send_0X00Ba(_user, code));
            }
        }
    }
}
