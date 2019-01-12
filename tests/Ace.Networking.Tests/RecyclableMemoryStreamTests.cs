using Ace.Networking.Memory;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Ace.Networking.Tests
{
    public class RecyclableMemoryStreamTests
    {
        internal RecyclableMemoryStream GetStream(float coeff, int baseSize)
        {
            return new Memory.RecyclableMemoryStreamManager(coeff, null, baseSize).GetStream();
        }
        internal byte[] RandomBuffer(int size)
        {
            var buf = new byte[size];
            for(int i = 0; i < size; i++)
            {
                buf[i] = (byte)i;
            }
            return buf;
        }

        [Fact]
        public void SingleBlockWriteTest()
        {
            var stream = GetStream(2.0f, 1024);

            Assert.Equal(0, stream.Length);
            var buf = RandomBuffer(1023);
            stream.Write(buf, 0, buf.Length);
            Assert.Equal(buf.Length, stream.Position);
            Assert.Equal(buf.Length, stream.Length);
        }

        [Fact]
        public void SingleBlockReadWriteTest()
        {
            var stream = GetStream(2.0f, 1024);

            Assert.Equal(0, stream.Length);
            var buf = RandomBuffer(1020);
            stream.Write(buf);
            stream.Position = 0;
            var buf2 = new byte[buf.Length];
            int read = stream.Read(buf2, 0, buf2.Length);
            Assert.Equal(buf, buf2);
            Assert.Equal(buf.Length, read);
        }

        [Fact]
        public void FourBlockReadWriteTest()
        {
            var stream = GetStream(2.0f, 128);
            Assert.Equal(0, stream.Length);
            var buf = RandomBuffer(1007);
            stream.Write(buf);
            stream.Position = 0;
            Assert.Equal(4, stream.Blocks.Count);
            var buf2 = new byte[buf.Length];
            int read = stream.Read(buf2, 0, buf2.Length);
            Assert.Equal(buf, buf2);
            Assert.Equal(buf.Length, read);
        }

        [Fact]
        public void SingleBlockShrinkZeroTest()
        {
            var stream = GetStream(2.0f, 128);
            stream.SetLength(127);
            Assert.Equal(1, stream.Blocks.Count);
            stream.SetLength(0);
            Assert.Equal(0, stream.Blocks.Count);
        }

        [Fact]
        public void FourBlockShrinkZeroTest()
        {
            var stream = GetStream(2.0f, 128);
            stream.SetLength(1009);
            Assert.Equal(4, stream.Blocks.Count);
            stream.SetLength(1);
            Assert.Equal(1, stream.Blocks.Count);
            stream.SetLength(0);
            Assert.Equal(0, stream.Blocks.Count);
        }

        [Fact]
        public void FourBlockRandomWriteReadTest()
        {
            var stream = GetStream(2.0f, 128);
            var buf = RandomBuffer(1012);
            stream.Write(buf, 0, buf.Length/4);
            stream.Write(buf, (int)stream.Position, buf.Length / 4);
            stream.Write(buf, (int)stream.Position, buf.Length / 4);
            stream.Write(buf, (int)stream.Position, buf.Length / 4);
            stream.Position = 0;
            var array = new byte[buf.Length];
            stream.Read(array, 0, buf.Length);
            Assert.Equal(buf, array);
        }
    }
}
