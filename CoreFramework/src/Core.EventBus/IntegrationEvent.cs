using System;

namespace Core.EventBus
{
    public class IntegrationEvent
    {
        public IntegrationEvent()
        {
            Id = Guid.NewGuid();
            UtcCreation = DateTime.UtcNow;
            CreationTime = DateTime.Now;
        }

        public IntegrationEvent(Guid id, DateTime createDate)
        {
            Id = id;
            CreationTime = createDate;
            UtcCreation = DateTime.UtcNow;
        }
        public Guid Id { get; }

        public DateTime UtcCreation { get; }

        public DateTime CreationTime { get; }

    }
}
