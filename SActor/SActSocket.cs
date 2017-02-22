using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Threading;
using System.IO;
namespace SActor
{
    public class SActSocket
    {

        class WBuffer
        {
            public int Offset { get; set; }
            public byte[] Data { get; set; }

            public int Size { get; set; }
            public string FilePath  { get; set; }
        }

        enum SActSocketStatus
        {
            Invalid,
            Listened,
            Accepted,
            Connected,
            Connecting
        }

  

        Socket _fd;
        ConcurrentQueue<WBuffer> _wbuf = new ConcurrentQueue<WBuffer>();
        SActSocketStatus _status;
        bool _hasPostAccept;
        bool _hasPostRead;
        bool _hasPostSend;
        bool _hasClose;
        uint _recvSize = 4096;
        SActActor _act;
        WBuffer _currentWBuf;

        private SActSocket()
        {
            _status = SActSocketStatus.Invalid;
        }

        void CloseSocket()
        {
            if (_fd != null)
            {
                _fd.Close();
                _fd = null;
            }
            _hasClose = true;
        }
        void RetireSAEA(SocketAsyncEventArgs e)
        {
            e.AcceptSocket = null;
            e.SetBuffer(null, 0, 0);
            e.Completed -= OnCompleted;
            SActSAEAPool.Push(e);
        }

        void ReportMessage(SActSocketMessage msg)
        {
            if (_act != null)
            {
                SActor.Send(null, _act, (int)SActMessageType.Socket, 0, msg);
            }
            
        }

        void OnCompleted(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Accept:
                    OnAccepted(e);
                    break;
                case SocketAsyncOperation.Receive:
                    OnRecved(e);
                    break;
                case SocketAsyncOperation.Send:
                    OnSended(e);
                    break;
                case SocketAsyncOperation.Connect:
                    OnConnected(e);
                    break;
            }
        }

        void OnConnected(SocketAsyncEventArgs e)
        {
            RetireSAEA(e);
            SActSocketMessage msg = new SActSocketMessage();
            msg.Socket = this;
            if (e.SocketError == SocketError.Success)
            {
                msg.Type = SActSocketMessageType.Open;
                _status = SActSocketStatus.Connected;
                if (_hasClose)
                {
                    CloseSocket();
                }
            }
            else
            {
                msg.Type = SActSocketMessageType.Error;
                msg.Error = e.SocketError.ToString();
                CloseSocket();
            }
            ReportMessage(msg);
        }
        void OnAccepted(SocketAsyncEventArgs e)
        {
            _hasPostAccept = false;
            Socket client = e.AcceptSocket;
            e.AcceptSocket = null;
            if (client != null)
            {
                SActSocket s = new SActSocket();
                s._fd = client;
                s._status = SActSocketStatus.Accepted;
                SActSocketMessage msg = new SActSocketMessage();
                msg.Socket = this;
                msg.AcceptSocket = s;
                msg.Type = SActSocketMessageType.Accept;
                ReportMessage(msg);
            }
            if (!_hasClose)
            {
                DoAccept(e);
            }
            else
            {
                RetireSAEA(e);
                CloseSocket();
            }
        }

        void OnRecved(SocketAsyncEventArgs e)
        {
            _hasPostRead = false;
            SActSocketMessage msg = new SActSocketMessage();
            msg.Socket = this;
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                msg.Type = SActSocketMessageType.Data;
                msg.Data = e.Buffer;
                msg.Size = e.BytesTransferred;
                ReportMessage(msg);
                if (!_hasClose)
                {
                    DoRecv(e);
                }
                else if (!_hasPostSend)
                {
                    RetireSAEA(e);
                    CloseSocket();
                }
            }
            else
            {
                msg.Type = SActSocketMessageType.Close;
                RetireSAEA(e);
                CloseSocket();
                ReportMessage(msg);
            }
        }

        void OnSended(SocketAsyncEventArgs e)
        {
            _hasPostSend = false;
            if (_wbuf.Count > 0)
            {
                DoSend(e);
            }
            else if (_hasClose)
            {
                RetireSAEA(e);
                CloseSocket();
            }
            else
            {
                RetireSAEA(e);
            }
        }

        void DoSend(SocketAsyncEventArgs e)
        {
            if (_hasPostSend)
            {
                return;
            }
            if (e == null)
            {
                e = SActSAEAPool.Pop();
                e.Completed += OnCompleted;
            }
             _hasPostSend = true;
            WBuffer buf;
             _wbuf.TryDequeue(out buf);
            if (buf.FilePath == null)
            {
                e.SetBuffer(buf.Data, buf.Offset, buf.Size);
            }
            else
            {
                _fd.SendFile(buf.FilePath);
            }
            if (!_fd.SendAsync(e))
            {
                OnSended(e);
            }
        }

        void DoAccept(SocketAsyncEventArgs e)
        {
            if (_hasPostAccept)
            {
                return;
            }
            if (e == null)
            {
                e = SActSAEAPool.Pop();
                e.Completed += OnCompleted;
            }
            _hasPostAccept = true;
            if (!_fd.AcceptAsync(e))
            {
                OnAccepted(e);
            }
        }

        void DoRecv(SocketAsyncEventArgs e)
        {
            if (_hasPostRead)
            {
                return;
            }
            if (e == null)
            {
                e = SActSAEAPool.Pop();
                e.Completed += OnCompleted;
            }
            _hasPostRead = true;
            e.SetBuffer(new byte[_recvSize], 0, (int)_recvSize);
            if (!_fd.ReceiveAsync(e))
            {
                OnRecved(e);
            }
        }

        public static SActSocket Listen(int port, int bck,SActActor act)
        {
            try
            {
                SActSocket sock = new SActSocket();
                sock._fd = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                sock._fd.Bind(new IPEndPoint(IPAddress.Any, port));
                sock._status = SActSocketStatus.Listened;
                sock._fd.Listen(bck);
                sock._act = act;
                return sock;
            }
            catch (Exception e)
            {
                throw new SActException(e.Message);
            }
        }

        public static SActSocket Connect(string host, int port, SActActor act)
        {
            try
            {
                SActSocket sock = new SActSocket();
                IPEndPoint addr = new IPEndPoint(IPAddress.Parse(host), port);
                Socket fd = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                SocketAsyncEventArgs e = SActSAEAPool.Pop();
                e.RemoteEndPoint = addr;
                e.Completed += sock.OnCompleted;
                sock._fd = fd;
                sock._status = SActSocketStatus.Connecting;
                sock._act = act;
                Task.Run(delegate {
                    Thread.Sleep(100);
                    if (!fd.ConnectAsync(e))
                    {
                        sock.OnConnected(e);
                    }
                });
                return sock;
            }
            catch ( Exception e)
            {
                throw new SActException(e.Message);
            }
        }

        public void Bind(SActActor act)
        {
            _act = act;
        }

        public void Start()
        {
            Debug.Assert(_status == SActSocketStatus.Listened || _status == SActSocketStatus.Accepted || _status == SActSocketStatus.Connected);
            switch (_status)
            {
                case SActSocketStatus.Listened:
                   DoAccept(null);
                   break;
                case SActSocketStatus.Accepted:
                case SActSocketStatus.Connected:
                   DoRecv(null);
                   break;
            }
        }

        public void Send(byte[] data,int offset,int size)
        {
            Debug.Assert(data != null);
            WBuffer buf = new WBuffer() {Data = data,Offset=offset,Size =size };
            _wbuf.Enqueue(buf);
            if (!_hasPostSend)
            {
                DoSend(null);
            }
        }

        public void SendFile(string path)
        {
            if (File.Exists(path))
            {
                WBuffer buf = new WBuffer() { FilePath = path };
                _wbuf.Enqueue(buf);
                if (!_hasPostSend)
                {
                    DoSend(null);
                }
            }
        }

        public bool Connected()
        {
            return _fd == null;
        }

        public void Close()
        {
            if (_status == SActSocketStatus.Accepted || (_status == SActSocketStatus.Listened && !_hasPostAccept))
            {
                CloseSocket();
            }
            else
            {
                _hasClose = true;
            }
        }

        public string GetIP()
        {
            if (_fd != null)
            {
                return ((IPEndPoint)_fd.RemoteEndPoint).Address.ToString();
            }
            return null;
        }
    }
}
