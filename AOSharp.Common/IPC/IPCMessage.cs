using System;
using SmokeLounge.AOtomation.Messaging.Serialization;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

//This should really be AOSharp.IPC but that would murder every single plugin.
namespace AOSharp.Core.IPC
{
    [AoKnownType(9, IdentifierType.Int16)]
    public class IPCMessage
    {
        //No longer needed but left for compability.
        public virtual short Opcode { get; }
    }
}
