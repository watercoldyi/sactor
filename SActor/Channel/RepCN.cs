using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SActor.Channel
{
    public class RepCN
    {
        public delegate object UnPack(RingBuffer buf);

        SActActor _act;
        SActSocket _sock;
        bool _isSend = false;
        Object _reply;
        RingBuffer _buf = new RingBuffer(1024);
        UnPack _unpack;
        Task _wait;
        public RepCN(SActActor act,SActSocket sock, UnPack decode)
        {
            _act = act;
            _sock = sock;
            _unpack = decode;
            _act.SetMessageHandler((int)SActMessageType.Socket, OnSocketMessage);
            _sock.Bind(act);
            _sock.Start();
        }

        public bool Send(byte[] data)
        {
            if (_sock == null || !_sock.Connected()) { return false; }
            if (!_isSend)
            {
                throw new InvalidOperationException("not invoke Recv()");
            }
            _isSend = false;
            _sock.Send(data, 0, data.Length);
            return true;
        }


        public Task<T> Recv<T>()
        {
            if (_sock == null || !_sock.Connected()) { return null; }
            if (_isSend)
            {
                throw new InvalidOperationException("not invoke Send()");
            }
            Task<T> t = new Task<T>(delegate
            {
                var r = _reply;
                _reply = null;
                _isSend = true;
                return (T)r;
            });
            _wait = t;
            return t;
        }

        void OnSocketMessage(SActMessage msg)
        {
            SActSocketMessage m = msg.Data as SActSocketMessage;
            switch (m.Type)
            {
                case SActSocketMessageType.Error:
                case SActSocketMessageType.Close:
                    {
                        if (m.Error != null)
                        {
                            _act.Log("socket err:" + m.Error);
                        }
                        if (_wait != null)
                        {
                            var t = _wait;
                            _wait = null;
                            t.RunSynchronously();
                        }
                    }
                    break;
                case SActSocketMessageType.Data:
                    {
                        _buf.Write(m.Data, 0, m.Size);
                        _reply = _unpack(_buf);
                        if (_reply != null)
                        {
                            var t = _wait;
                            _wait = null;
                            t.RunSynchronously();
                        }
                    }
                    break;
            }
        }
    }
}
