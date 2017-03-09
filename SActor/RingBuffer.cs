using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SActor
{
    /// <summary>
    /// 环形缓冲区
    /// </summary>
    public class RingBuffer
    {
        byte[] _buf;
        int _head;
        int _tail;

        public readonly int MAXSIZE = 0x7fffffff;

        public RingBuffer(int n)
        {
            if (n <= 0)
            {
                n = 65535;
            }
            _buf = new byte[n];
            _head = 0;
            _tail = 0;
        }

       public int Length()
        {
            return (_tail - _head + _buf.Length) % _buf.Length;
        }

        void Expand()
        {
            int len = _buf.Length * 2;
            if (len > MAXSIZE)
            {
                len = MAXSIZE;
            }
            byte[] buf = new byte[len];
            int size = Length();
            if (_head <= _tail)
            {
                Array.Copy(_buf, _head, buf, 0, size);
            }
            else
            {
                int copy1 = _buf.Length - _head;
                Array.Copy(_buf, _head, buf, 0, copy1);
                Array.Copy(_buf, 0, buf, copy1, _tail);
            }
            _buf = buf;
            _head = 0;
            _tail = size;
        }

        public int Read(byte[] dest, int offset, int n)
        {
            int len = Length();
            if (n <= 0 || len == 0 || len < n)
            {
                return 0;
            }
            if (_head < _tail)
            {
                Array.Copy(_buf, _head, dest, offset, n);
            }
            else
            {
                int copy1 = _buf.Length - _head;
                if (copy1 > n)
                {
                    copy1 = n;
                }
                Array.Copy(_buf, _head, dest, offset, copy1);
                if (copy1 < n)
                {
                    Array.Copy(_buf, 0, dest, offset + copy1, n - copy1);
                }
            }
            _head = (_head + n) % _buf.Length;
            return n;
        }

        public int Write(byte[] source, int offset, int n)
        {
            if (n <= 0)
            {
                return 0;
            }
            int len = Length() + n;
            if (len > MAXSIZE)
            {
                return 0;
            }
            for (; ; )
            {
                if ((_buf.Length - Length() - 1) <= n)
                {
                    Expand();
                }
                else
                {
                    if (_tail >= _head)
                    {
                        int copy1 = _buf.Length - _tail;
                        if (copy1 > n)
                        {
                            copy1 = n;
                        }
                        Array.Copy(source, offset, _buf, _tail, copy1);
                        if (copy1 < n)
                        {
                            Array.Copy(source, offset + copy1, _buf, 0, n - copy1);
                        }
                    }
                    else
                    {
                        Array.Copy(source, offset, _buf, _tail, n);
                    }
                    _tail = (_tail + n) % _buf.Length;
                    return n;
                }
            }
        }

        public int Peek(byte[] dest, int offset, int n)
        {
            int len = Length();
            if (n <= 0 || len == 0 || len < n)
            {
                return 0;
            }
            if (_head < _tail)
            {
                Array.Copy(_buf, _head, dest, offset, n);
            }
            else
            {
                int copy1 = _buf.Length - _head;
                if (copy1 > n)
                {
                    copy1 = n;
                }
                Array.Copy(_buf, _head, dest, offset, copy1);
                if (copy1 < n)
                {
                    Array.Copy(_buf, 0, dest, offset + copy1, n - copy1);
                }
            }
            return n; ;
        }

        public void Clear()
        {
            _tail = 0;
            _head = 0;
        }

    }
}
