using System.Collections.Generic;
using Core.EventBus;

namespace Core.Ddd.Domain.Events
{
    public class EntityEventReport
    {
        public List<IMessage> DomainEvents { get; }

        public List<IMessage> DistributedEvents { get; }

        public EntityEventReport()
        {
            DomainEvents = new List<IMessage>();
            DistributedEvents = new List<IMessage>();
        }
    }
}
