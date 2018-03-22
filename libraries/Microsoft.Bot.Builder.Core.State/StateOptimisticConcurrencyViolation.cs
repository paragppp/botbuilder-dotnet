using System;
using System.Runtime.Serialization;

namespace Microsoft.Bot.Builder.Core.State
{
    [Serializable]
    internal class StateOptimisticConcurrencyViolation : Exception
    {
        public StateOptimisticConcurrencyViolation(string message) : base(message)
        {
        }

        public StateOptimisticConcurrencyViolation(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected StateOptimisticConcurrencyViolation(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}