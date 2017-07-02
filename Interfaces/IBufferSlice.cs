namespace Ace.Networking.Interfaces
{
    public interface IBufferSlice
    {
        /// <summary>
        ///     Where this slice starts
        /// </summary>
        int Offset { get; }

        /// <summary>
        ///     Number of bytes allocated for this slice
        /// </summary>
        int Capacity { get; }

        /// <summary>
        ///     Buffer that this is a slice of.
        /// </summary>
        byte[] Buffer { get; }
    }
}