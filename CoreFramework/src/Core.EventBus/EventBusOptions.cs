using System.Reflection;

namespace Core.EventBus
{
    public  class EventBusOptions
    {
        public Assembly[] AutoRegistrarHandlersAssemblies { get; set; }
    }
}
