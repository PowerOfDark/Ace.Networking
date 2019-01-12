using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Ace.Networking.Memory
{
    public class RecyclableMemoryStream : Stream, IDisposable
    {
        public IList<byte[]> Blocks { get; } = new List<byte[]>();
        private long _maxLength = 0;
        private long _currentLength = 0;
        //private long _lastBlockLength = 0;
        private long _nextSize = 1024;
        private int _currentBlock = -1;
        private int _currentBlockOffset = 0;
        private long _currentOffset = 0;
        private int CurrentBlockLength => _currentBlock < 0 ? 0 : Blocks[_currentBlock].Length;
        private int CurrentBlockCapacity => CurrentBlockLength - _currentBlockOffset;
        private byte[] CurrentBlock => Blocks[_currentBlock];
        //private float _stepCoefficient = 2.0f;

        private RecyclableMemoryStreamManager _mgr;

        public RecyclableMemoryStream(RecyclableMemoryStreamManager mgr)
        {
            _mgr = mgr;
            _nextSize = _mgr.BaseSize;
        }

        public bool ReserveSingleBlock(int length)
        {
            SetLength(0);
            return TryExtend(length, true);
        }

        private bool TryExtend(int length, bool fixedSize = false)
        {
            if (_currentLength + length <= _maxLength) return false;
            while (_currentLength + length > _maxLength)
            {
                var top = _mgr.Pool.Rent(fixedSize ? length : (int)_nextSize);
                _nextSize = fixedSize ? _nextSize : FillNearest32((int)(_nextSize * _mgr.StepCoefficient));
                lock (Blocks)
                {
                    Blocks.Add(top);
                    if (Blocks.Count == 1)
                        _currentBlock = 0;
                    _maxLength += top.Length;
                    //length -= top.Length;
                }
            }
            return true;
        }

        private long FillNearest32(int length)
        {
            int mod = length % 32;
            if (mod != 0)
                return length + 32 - mod;
            return length;
        }

        private void TryShrink()
        {
            while(Blocks.Any() && _maxLength - Blocks.Last().Length >= _currentLength)
            {
                var back = Blocks.Last();
                Blocks.RemoveAt(Blocks.Count - 1);
                if (Blocks.Count == 0)
                    _currentBlock = -1;
                _mgr.Pool.Return(back);
                _maxLength -= back.Length;
                _nextSize = FillNearest32((int)(_nextSize / _mgr.StepCoefficient));
            }

        }
        

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => true;

        public override long Length => _currentLength;

        public override long Position { get => _currentOffset; set => Seek(value, SeekOrigin.Begin); }

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
            _currentBlockOffset = 0;
            return true;
        }
        public bool MovePrevious()
        {
            if (!HasPreviousBlock()) return false;
            _currentOffset -= _currentBlockOffset+1;
            _currentBlock--;
            _currentBlockOffset = CurrentBlock.Length - 1;
            return true;
        }

    
        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = 0;
            count = Math.Min(count, checked((int)(_currentLength - _currentOffset)));
            while(count > 0)
            {
                int toRead = Math.Min(count, CurrentBlockCapacity);
                Buffer.BlockCopy(CurrentBlock, _currentBlockOffset, buffer, offset, toRead);
                offset += toRead;
                count -= toRead;
                read += toRead;
                _currentBlockOffset += toRead;
                _currentOffset += toRead;
                if (count > 0)
                    if(!MoveNext())
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

            long diff = (offset - _currentOffset);
            
            if(diff > 0)
            {
                while (diff > CurrentBlockCapacity)
                {
                    diff -= CurrentBlockCapacity;
                    if (!MoveNext())
                        throw new IndexOutOfRangeException();
                }
                _currentBlockOffset = checked((int)diff);
            }
            else if (diff < 0)
            {
                diff = -diff;
                while(diff > 0)
                {
                    diff -= _currentBlockOffset;
                    _currentBlockOffset = 0;
                    if (diff > 0)
                    {
                        if (!MovePrevious())
                            throw new IndexOutOfRangeException();
                        diff--;
                    }
                }
                _currentBlockOffset -= checked((int)diff);
            }
           
            _currentOffset = offset;
            return _currentOffset;
        }

        public override void SetLength(long value)
        {
            if(value > _currentLength)
            {
                if(!TryExtend(checked((int)(value - _currentLength))))
                {
                    throw new OutOfMemoryException();
                    //nothing
                }
                _currentLength = value;
            }
            
            else if(value < _currentLength)
            {
                if(_currentOffset >= value)
                {
                    Seek(Math.Max(0, value - 1), SeekOrigin.Begin);
                }
                _currentLength = value;
                TryShrink();
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            TryExtend(count);

            while (count > 0)
            {
                int toWrite = Math.Min(count, CurrentBlockCapacity);
                Buffer.BlockCopy(buffer, offset, CurrentBlock, _currentBlockOffset, toWrite);
                offset += toWrite;
                count -= toWrite;
                _currentOffset += toWrite;
                _currentBlockOffset += toWrite;
                if (count > 0)
                    if (!MoveNext())
                        throw new IndexOutOfRangeException();
            }
            _currentLength = Math.Max(_currentLength, _currentOffset);
        }

        protected override void Dispose(bool disposing)
        {
            if(disposing)
            {
                SetLength(0);
            }
            base.Dispose(disposing);
        }
    }
}
