using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SActor;
using System.Security.Policy;
using System.IO;
namespace SActor.Channel
{
    /// <summary>
    /// 请求通道
    /// </summary>
    public class ReqCN
    {
        public delegate object UnPack(RingBuffer buf);

        SActActor _act;
        SActSocket _sock;
        bool _isSend = true;
        byte[] _data;
        Object _reply;
        string _ip;
        int _port;
        RingBuffer _buf = new RingBuffer(1024);
        UnPack _unpack;
        Task _wait;
        public ReqCN(SActActor act,UnPack decode)
        {
            _act = act;
            _unpack = decode;
            _act.SetMessageHandler((int)SActMessageType.Socket, OnSocketMessage);
        }

        public void Connect(string ip,int port)
        {
            _ip = ip;
            _port = port;
            _sock = SActSocket.Connect(ip, port, _act);
        }

        public void Send(byte[] data)
        {
            if (!_isSend)
            {
                throw new InvalidOperationException("not invoke Recv()");
            }
            _isSend = false;
            _data = data;
        }


        public Task<T> Recv<T>()
        {
            if (_isSend)
            {
                throw new InvalidOperationException("not invoke Send()");
            }
            Task<T> t = new Task<T>(delegate {
                _isSend = true;
                return (T)_reply;
            });
            _wait = t;
            if (_sock != null && _sock.Connected())
            {
                _sock.Send(_data,0,_data.Length);
                _data = null;
            }
            return t;
        }

        void OnSocketMessage(SActMessage msg)
        {
            SActSocketMessage m = msg.Data as SActSocketMessage;
            switch (m.Type)
            {
                case SActSocketMessageType.Open:
                    {
                        if (_data != null)
                        {
                            _sock.Send(_data, 0, _data.Length);
                            _data = null;
                        }
                    }
                    break;
                case SActSocketMessageType.Error:
                case SActSocketMessageType.Close:
                    {
                        if (m.Error != null)
                        {
                            _act.Log("socket err:" + m.Error);
                        }
                        _sock = SActSocket.Connect(_ip, _port, _act);
                    }
                    break;
                case SActSocketMessageType.Data:
                    {
                        _buf.Write(m.Data, 0, m.Size);
                        _reply = _unpack(_buf);
                        if (_reply != null)
                        {
                            _wait.RunSynchronously();
                        }
                    }
                    break;
            }
        }
    }
}
