using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SActor
{
    /// <summary>
    /// 线性的请求回应模式客户端
    /// </summary>
    public class LineRRClient
    {
        class Req
        {
            public byte[] data;
            public Task task;
        }

        public delegate object UnPack(RingBuffer buf);
        Queue<Req> _reqs = new Queue<Req>();
        UnPack _unpack;
        SActSocket _sock;
        object _result;
        Task _wait;
        string _ip;
        int _port;
        SActActor _act;
        RingBuffer _buf = new RingBuffer(1024);

        public LineRRClient(string ip,int port,SActActor act,UnPack unpack)
        {
            _unpack = unpack;
            _sock = SActSocket.Connect(ip, port, act);
            
        }

        void ProcessData(SActSocketMessage msg)
        {
            _buf.Write(msg.Data, 0, msg.Size);
            object r = _unpack(_buf);
            if (r != null)
            {
                _result = r;
                Task t = _wait;
                _wait = null;
                t.RunSynchronously();
            }
        }

        void ProcessClose()
        {
            _sock = SActSocket.Connect(_ip, _port, _act);
            _result = null;
            if (_wait != null)
            {
                Task t = _wait;
                _wait = null;
                t.RunSynchronously();
            }
        }

        void ProcessSocketMessage(SActSocketMessage msg)
        {
            switch (msg.Type)
            {
                case SActSocketMessageType.Open:
                    break;
                case SActSocketMessageType.Data:
                    break;
                case SActSocketMessageType.Close:
                    break;
                case SActSocketMessageType.Error:
                    break;
            }
        }

        void OnSocketMessage(SActMessage msg)
        {

        }

        public Task<T> Request<T>(byte[] data, int timeout = -1)
        {
            Task<T> t = new Task<T>(() => { return (T)_result; });
            if (_reqs.Count == 0)
            {
                _sock.Send(data,0,data.Length);
                _wait = t;
            }
            else
            {
                Req req = new Req();
                req.data = data;
                req.task = t;
                _reqs.Enqueue(req);
            }
            return t;
        }

       
    }
}
