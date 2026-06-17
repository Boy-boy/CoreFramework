using Core.RabbitMQ;
using System;

namespace Core.EventBus.RabbitMQ
{
    /// <summary>
    /// RabbitMQ broker 模块的配置。在 appsettings.json 的 <c>EventBus:RabbitMq</c> 节点定义，
    /// 启动期由 <see cref="EventBusOptionsExtensions"/> 绑定到 IOptions 系统。
    /// </summary>
    /// <remarks>
    /// 配置项的语义：
    /// <list type="bullet">
    ///   <item><description><see cref="ExchangeName"/>：所有事件共用同一个 direct exchange，
    ///   按 routing key（即 <see cref="MessageNameAttribute"/> 值）路由到不同 queue。</description></item>
    ///   <item><description><see cref="Connection"/>：RabbitMQ 连接参数（host / port / 凭据）。</description></item>
    /// </list>
    /// </remarks>
    public class EventBusRabbitMqOptions
    {
        private string _defaultExchangeName = "event_bus_default_routing";

        /// <summary>
        /// 全局 exchange 名（direct 类型）。所有事件按 <see cref="MessageNameAttribute"/>
        /// 作为 routing key 绑定到这个 exchange。不允许为 null。
        /// </summary>
        public string ExchangeName
        {
            get => _defaultExchangeName;
            set => _defaultExchangeName = value ?? throw new Exception("exchange is not allowed to be null");
        }

        public EventBusRabbitMqOptions()
        {
            Connection = new RabbitMqConnectionConfigure();
        }

        /// <summary>RabbitMQ 连接配置（host、port、vhost、凭据等）。</summary>
        public RabbitMqConnectionConfigure Connection { get; set; }
    }
}
