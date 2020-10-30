﻿using Core.Ddd.Domain.Events;
using Core.EventBus;

namespace EntityFrameworkCore.Api.Events
{
    [EventName("customer")]
    public class AddStudentEvent: AggregateRootEvent
    {
        public string AggregateRootId{ get; set; }
    }
}
