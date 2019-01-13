namespace Ace.Networking.MicroProtocol.Interfaces
{
    public interface IDynamicPayload
    {
        void Construct(object[] payload);
        object[] Deconstruct();
    }
}