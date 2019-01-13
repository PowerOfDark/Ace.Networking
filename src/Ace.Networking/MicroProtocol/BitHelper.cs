namespace Ace.Networking.MicroProtocol
{
    public static class BitHelper
    {
        public static int WriteInt(byte[] target, int offset, params int[] args)
        {
            var written = 0;
            foreach (var arg in args)
            {
                BitConverter2.GetBytes(arg, target, offset + written);
                written += sizeof(int);
            }

            return written;
        }
    }
}