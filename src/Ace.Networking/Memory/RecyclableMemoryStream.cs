using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ace.Networking.Memory
{
    public class RecyclableMemoryStream : Stream, IDisposable
    {
        public byte[] _buffer;
        private int _currentBlock = -1;
        private long _currentLength;
        private long _currentOffset;

        private long _maxLength;
        //private float _stepCoefficient = 2.0f;

        private readonly RecyclableMemoryStreamManager _mgr;

        //private long _lastBlockLength = 0;
        private long _nextSize = 1024;

        public RecyclableMemoryStream(RecyclableMemoryStreamManager mgr)
        {
            _mgr = mgr;
            _nextSize = _mgr.BaseSize;
            _buffer = mgr.Pool.Rent(8);
        }

        public List<byte[]> Blocks { get; } = new List<byte[]>();
        public int CurrentBlockOffset { get; private set; }

        public int CurrentBlockLength => _currentBlock < 0 ? 0 : Blocks[_currentBlock].Length;
        public int CurrentBlockCapacity => CurrentBlockLength - CurrentBlockOffset;
        public byte[] CurrentBlock => Blocks[_currentBlock];

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => true;

        public override long Length => _currentLength;

        public override long Position
        {
            get => _currentOffset;
            set => Seek(value, SeekOrigin.Begin);
        }

        public bool ReserveSingleBlock(int length)
        {
            if (length > _maxLength)
            {
                SetLength(0);
                return TryExtend(length, true);
            }

            SetLength(length);
            return true;
        }

        public bool TryExtend(int length, bool fixedSize = false)
        {
            if (_currentLength + length <= _maxLength) return false;
            while (_currentLength + length > _maxLength)
            {
                var top = _mgr.Pool.Rent(fixedSize ? length : (int) _nextSize);
                _nextSize = fixedSize ? _nextSize : FillNearest32(checked((int) (_nextSize * _mgr.StepCoefficient)));
                lock (Blocks)
                {
                    Blocks.Add(top);
                    if (Blocks.Count == 1)
                        _currentBlock = 0;
                    _maxLength += top.Length;
                }
            }

            return true;
        }

        private long FillNearest32(int length)
        {
            var mod = length % 32;
            if (mod != 0)
                return length + 32 - mod;
            return length;
        }


        private void PopBlock()
        {
            var block = Blocks.Last();
            Blocks.RemoveAt(Blocks.Count - 1);
            if (Blocks.Count == 0)
                _currentBlock = -1;
            _mgr.Pool.Return(block);
            _maxLength -= block.Length;
            _nextSize = FillNearest32((int) (_nextSize / _mgr.StepCoefficient));
        }


        private void TryShrink()
        {
            while (Blocks.Any() && _maxLength - Blocks.Last().Length >= _currentLength
                                && _maxLength - Blocks.Last().Length >= _mgr.MinimumSize)
                PopBlock();
        }

        public void ShrinkTo(long length)
        {
            if (_currentOffset >= length)
                Seek(length - 1, SeekOrigin.Begin);
            TryShrink();
        }

        public void ShrinkToBase()
        {
            ShrinkTo(_mgr.BaseSize);
        }

        public override void Flush()
        {
            //throw new NotImplementedException();
        }

        public bool HasNextBlock()
        {
            return _currentBlock + 1 < Blocks.Count;
        }

        public bool HasPreviousBlock()
        {
            return _currentBlock > 0;
        }

        public bool MoveNext()
        {
            if (!HasNextBlock()) return false;
            _currentOffset += CurrentBlockCapacity;
            _currentBlock++;
            CurrentBlockOffset = 0;
            return true;
        }

        public bool MovePrevious()
        {
            if (!HasPreviousBlock()) return false;
            _currentOffset -= CurrentBlockOffset + 1;
            _currentBlock--;
            CurrentBlockOffset = CurrentBlock.Length - 1;
            return true;
        }


        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = 0;
            count = Math.Min(count, checked((int) (_currentLength - _currentOffset)));
            while (count > 0)
            {
                var toRead = Math.Min(count, CurrentBlockCapacity);
                Buffer.BlockCopy(CurrentBlock, CurrentBlockOffset, buffer, offset, toRead);
                offset += toRead;
                count -= toRead;
                read += toRead;
                CurrentBlockOffset += toRead;
                _currentOffset += toRead;
                if (count > 0)
                    if (!MoveNext())
                        break;
            }

            return read;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin == SeekOrigin.Current) offset += _currentOffset;
            if (origin == SeekOrigin.End) offset = _currentLength - offset;

            if (offset < 0 || offset > _maxLength)
                throw new InvalidOperationException();

            var diff = offset - _currentOffset;

            if (diff > 0)
            {
                while (diff > CurrentBlockCapacity)
                {
                    diff -= CurrentBlockCapacity;
                    if (!MoveNext())
                        throw new IndexOutOfRangeException();
                }

                CurrentBlockOffset += checked((int) diff);
            }
            else if (diff < 0)
            {
                diff = -diff;
                while (diff > 0)
                {
                    diff -= CurrentBlockOffset;
                    CurrentBlockOffset = 0;
                    if (diff > 0)
                    {
                        if (!MovePrevious())
                            throw new IndexOutOfRangeException();
                        diff--;
                    }
                }

                CurrentBlockOffset -= checked((int) diff);
            }

            _currentOffset = offset;
            return _currentOffset;
        }

        public override void SetLength(long value)
        {
            /*if (value == 0 && _mgr.MinimumSize == 0)
            {
                _currentLength = _maxLength = _currentOffset = 0;
                CurrentBlockOffset = 0;
                _currentBlock = -1;
                foreach (var b in Blocks)
                    _mgr.Pool.Return(b);
                _nextSize = _mgr.BaseSize;
                Blocks.Clear();
                return;
            }*/

            if (value > _currentLength)
            {
                if (value > _maxLength)
                    if (!TryExtend(checked((int) (value - _currentLength))))
                        throw new OutOfMemoryException();
                _currentLength = value;
            }

            else if (value < _currentLength)
            {
                if (_currentOffset >= value) Seek(Math.Max(0, value - 1), SeekOrigin.Begin);
                _currentLength = value;
                TryShrink();
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            TryExtend(count);

            while (count > 0)
            {
                var toWrite = Math.Min(count, CurrentBlockCapacity);
                Buffer.BlockCopy(buffer, offset, CurrentBlock, CurrentBlockOffset, toWrite);
                offset += toWrite;
                count -= toWrite;
                _currentOffset += toWrite;
                CurrentBlockOffset += toWrite;
                if (count > 0)
                    if (!MoveNext())
                        throw new IndexOutOfRangeException();
            }

            _currentLength = Math.Max(_currentLength, _currentOffset);
        }

        public void Write(bool value)
        {
            _buffer[0] = (byte) (value ? 1 : 0);
            Write(_buffer, 0, 1);
        }

        public void Write(byte value)
        {
            WriteByte(value);
        }

        public void Write(sbyte value)
        {
            WriteByte((byte) value);
        }

        public void Write(short value)
        {
            _buffer[0] = (byte) value;
            _buffer[1] = (byte) (value >> 8);
            Write(_buffer, 0, 2);
        }

        public void Write(ushort value)
        {
            _buffer[0] = (byte) value;
            _buffer[1] = (byte) (value >> 8);
            Write(_buffer, 0, 2);
        }

        public void Write(int value)
        {
            _buffer[0] = (byte) value;
            _buffer[1] = (byte) (value >> 8);
            _buffer[2] = (byte) (value >> 16);
            _buffer[3] = (byte) (value >> 24);
            Write(_buffer, 0, 4);
        }

        public void Write(uint value)
        {
            _buffer[0] = (byte) value;
            _buffer[1] = (byte) (value >> 8);
            _buffer[2] = (byte) (value >> 16);
            _buffer[3] = (byte) (value >> 24);
            Write(_buffer, 0, 4);
        }

        public void Write(long value)
        {
            _buffer[0] = (byte) value;
            _buffer[1] = (byte) (value >> 8);
            _buffer[2] = (byte) (value >> 16);
            _buffer[3] = (byte) (value >> 24);
            _buffer[4] = (byte) (value >> 32);
            _buffer[5] = (byte) (value >> 40);
            _buffer[6] = (byte) (value >> 48);
            _buffer[7] = (byte) (value >> 56);
            Write(_buffer, 0, 8);
        }

        public void Write(ulong value)
        {
            _buffer[0] = (byte) value;
            _buffer[1] = (byte) (value >> 8);
            _buffer[2] = (byte) (value >> 16);
            _buffer[3] = (byte) (value >> 24);
            _buffer[4] = (byte) (value >> 32);
            _buffer[5] = (byte) (value >> 40);
            _buffer[6] = (byte) (value >> 48);
            _buffer[7] = (byte) (value >> 56);
            Write(_buffer, 0, 8);
        }

        public bool ReadBoolean()
        {
            Read(_buffer, 0, 1);
            return _buffer[0] != 0;
        }

        public short ReadInt16()
        {
            Read(_buffer, 0, 2);
            return (short) (_buffer[0] | (_buffer[1] << 8));
        }

        public ushort ReadUInt16()
        {
            Read(_buffer, 0, 2);
            return (ushort) (_buffer[0] | (_buffer[1] << 8));
        }

        public int ReadInt32()
        {
            Read(_buffer, 0, 4);
            return _buffer[0] | (_buffer[1] << 8) | (_buffer[2] << 16) | (_buffer[3] << 24);
        }

        public uint ReadUInt32()
        {
            Read(_buffer, 0, 4);
            return (uint) (_buffer[0] | (_buffer[1] << 8) | (_buffer[2] << 16) | (_buffer[3] << 24));
        }

        public long ReadInt64()
        {
            Read(_buffer, 0, 8);
            return _buffer[0] | (_buffer[1] << 8) | (_buffer[2] << 16) | (_buffer[3] << 24) | (_buffer[4] << 32) |
                   (_buffer[5] << 40) | (_buffer[6] << 48) | (_buffer[7] << 56);
        }

        public ulong ReadUInt64()
        {
            Read(_buffer, 0, 8);
            return (ulong) (_buffer[0] | (_buffer[1] << 8) | (_buffer[2] << 16) | (_buffer[3] << 24) |
                            (_buffer[4] << 32) | (_buffer[5] << 40) | (_buffer[6] << 48) | (_buffer[7] << 56));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                SetLength(0);
                _mgr.Pool.Return(_buffer);
            }

            base.Dispose(disposing);
        }
    }
}