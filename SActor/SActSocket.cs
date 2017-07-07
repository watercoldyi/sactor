using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.Threading;
using System.IO;
namespace SActor
{
    public class SActSocket
    {

        enum SActSocketStatus
        {
            Invalid,
            Listened,
            Accepted,
            Connected,
            Connecting
        }

        Socket _fd;
        SActSocketStatus _status;
        bool _hasClose;
        bool _hasStart;
        uint _recvSize = 4096;
        SActActor _act;
        int _nsend;
        object _lock = new object();

        private SActSocket()
        {
            _status = SActSocketStatus.Invalid;
        }

        public bool IsServer()
        {
            return _status == SActSocketStatus.Listened;
        }

        public SActActor Actor { get { return _act; } }

        void CloseSocket( bool err)
        {
            lock (_lock)
            {
                if (_fd == null) { return; }
                _fd.Close();
                _fd = null;
                _status = SActSocketStatus.Invalid;
                if (!err)
                {
                    SActSocketMessage msg = new SActSocketMessage();
                    msg.Type = SActSocketMessageType.Close;
                    msg.Socket = this;
                    ReportMessage(msg);
                }
            }
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
            if (_hasClose)
            {
                CloseSocket(true);
                return;
            }
            SActSocketMessage msg = new SActSocketMessage();
            msg.Socket = this;
            if (e.SocketError == SocketError.Success)
            {
                msg.Type = SActSocketMessageType.Open;
                _status = SActSocketStatus.Connected;
                ReportMessage(msg);
                DoRecv(null);
            }
            else
            {
                msg.Type = SActSocketMessageType.Error;
                msg.Error = "connect fail";
                CloseSocket(true);
                ReportMessage(msg);
            }
        }
        void OnAccepted(SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                RetireSAEA(e);
                if (_status == SActSocketStatus.Invalid) { return; }
                SActSocketMessage msg = new SActSocketMessage();
                msg.Type = SActSocketMessageType.Error;
                msg.Error = "accept err " + e.SocketError.ToString();
                msg.Socket = this;
                ReportMessage(msg);
                return;
            }
            Socket client = e.AcceptSocket;
            e.AcceptSocket = null;
            if (client != null)
            {
                SActSocket s = new SActSocket();
                s._fd = client;
                s._fd.Blocking = false;
                s._fd.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
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
            }
        }

        void OnRecved(SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                SActSocketMessage msg = new SActSocketMessage();
                msg.Socket = this;
                msg.Type = SActSocketMessageType.Data;
                msg.Data = e.Buffer;
                msg.Size = e.BytesTransferred;
                ReportMessage(msg);
                if (!_hasClose)
                {
                    DoRecv(e);
                }
                else if (0 == _nsend)
                {
                    CloseSocket(true);
                    RetireSAEA(e);
                }
                else
                {
                    RetireSAEA(e);
                }
            }
            else
            {
                RetireSAEA(e);
                CloseSocket(false);
            }
        }

        void OnSended(SocketAsyncEventArgs e)
        {
            Interlocked.Decrement(ref _nsend);
            if (e.BytesTransferred <= 0 || e.SocketError != SocketError.Success)
            {
                RetireSAEA(e);
                CloseSocket(false);
            }
            else
            {
                RetireSAEA(e);
                if (_hasClose && _nsend == 0)
                {
                    CloseSocket(true);
                }
            }
        }


        void DoAccept(SocketAsyncEventArgs e)
        {
            if (e == null)
            {
                e = SActSAEAPool.Pop();
                e.Completed += OnCompleted;
            }
            try
            {
                if (!_fd.AcceptAsync(e))
                {
                    OnAccepted(e);
                }
            }
            catch (Exception ee)
            {
                RetireSAEA(e);
                if (_status == SActSocketStatus.Invalid) { return; }
                CloseSocket(true);
                SActSocketMessage msg = new SActSocketMessage();
                msg.Type = SActSocketMessageType.Error;
                msg.Socket = this;
                msg.Error = ee.Message;
                ReportMessage(msg);
            }
        }

        void DoRecv(SocketAsyncEventArgs e)
        {
            if (e == null)
            {
                e = SActSAEAPool.Pop();
                e.Completed += OnCompleted;
            }
            e.SetBuffer(null, 0, 0);
            e.SetBuffer(new byte[_recvSize], 0, (int)_recvSize);
            try
            {
                if (!_fd.ReceiveAsync(e))
                {
                    OnRecved(e);
                }
            }
            catch (Exception ee)
            {
                CloseSocket(true);
                RetireSAEA(e);
                SActSocketMessage msg = new SActSocketMessage();
                msg.Error = ee.Message;
                msg.Type = SActSocketMessageType.Error;
                msg.Socket = this;
                ReportMessage(msg);
            }
        }

        public static SActSocket Listen(int port, int bck, SActActor act)
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
                fd.Blocking = false;
                fd.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                SocketAsyncEventArgs e = SActSAEAPool.Pop();
                e.RemoteEndPoint = addr;
                e.Completed += sock.OnCompleted;
                sock._fd = fd;
                sock._status = SActSocketStatus.Connecting;
                sock._act = act;
                Task.Run(delegate
                {
                    Thread.Sleep(100);
                    if (!fd.ConnectAsync(e))
                    {
                        sock.OnConnected(e);
                    }
                });
                return sock;
            }
            catch (Exception e)
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
            if (_hasStart) { return; }
            _hasStart = true;
            switch (_status)
            {
                case SActSocketStatus.Listened:
                    DoAccept(null);
                    break;
                case SActSocketStatus.Accepted:
                    _status = SActSocketStatus.Connected;
                    DoRecv(null);
                    break;
            }
        }

        public void Send(byte[] data, int offset, int size)
        {
            if (_status == SActSocketStatus.Connected)
            {
                SocketAsyncEventArgs e = null;
                try
                {
                    e = SActSAEAPool.Pop();
                    e.Completed += OnCompleted;
                    e.SetBuffer(data, offset, size);
                    Interlocked.Increment(ref _nsend);
                    if (!_fd.SendAsync(e))
                    {
                        OnSended(e);
                    }
                }
                catch (Exception ee)
                {
                    Interlocked.Decrement(ref _nsend);
                    RetireSAEA(e);
                    CloseSocket(true);
                    SActSocketMessage msg = new SActSocketMessage();
                    msg.Error = ee.Message;
                    msg.Type = SActSocketMessageType.Error;
                    msg.Socket = this;
                    ReportMessage(msg);
                }
            }
        }

        public bool Connected()
        {
            return _fd == null ? false : _fd.Connected;
        }

        public void Close()
        {
            _hasClose = true;
            if (_nsend == 0)
            {
                CloseSocket(true);
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
