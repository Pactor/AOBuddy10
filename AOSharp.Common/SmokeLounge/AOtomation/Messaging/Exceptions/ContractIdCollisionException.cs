using System;

namespace SmokeLounge.AOtomation.Messaging.Exceptions
{
    public class ContractIdCollisionException : Exception
    {
        public ContractIdCollisionException(string message) : base(message)
        {

        }
    }
}
