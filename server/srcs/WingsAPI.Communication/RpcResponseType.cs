using ProtoBuf;

namespace WingsAPI.Communication
{
    [ProtoContract]
    public enum RpcResponseType
    {
        UNKNOWN_ERROR,
        SUCCESS,
        GENERIC_SERVER_ERROR,
        MAINTENANCE_MODE
    }
}