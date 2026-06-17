using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.EventBus;
using Core.EventBus.Inbox;
using Core.EventBus.Integration;
using Core.EventBus.Local;
using Core.EventBus.Outbox;
using Core.Modularity;
using Core.Modularity.Attribute;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test
{
    [TestClass]
    public class EventBusUsageTest
    {
        [TestMethod]
        public void AddEventBus_ShouldRegisterCoreInfrastructure()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddEventBus(_ => { });

            var serviceProvider = services.BuildServiceProvider();

            var invoker = serviceProvider.GetRequiredService<IMessageHandlerInvoker>();
            Assert.IsInstanceOfType<DefaultMessageHandlerInvoker>(invoker);

            var hostedServices = serviceProvider.GetServices<IHostedService>().ToList();
            Assert.IsTrue(hostedServices.Any(x => x is EventBusBackgroundService));
        }

        [TestMethod]
        public void AddEventBus_WithLocalMq_ShouldRegisterLocalServices()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddEventBus(options => options.AddLocalMq());

            var serviceProvider = services.BuildServiceProvider();

            Assert.IsInstanceOfType<LocalMessagePublisher>(
                serviceProvider.GetRequiredService<ILocalMessagePublisher>());
            Assert.IsInstanceOfType<LocalMessageSubscribe>(
                serviceProvider.GetRequiredService<ILocalMessageSubscribe>());
            Assert.IsInstanceOfType<LocalMessageHandlerManager>(
                serviceProvider.GetRequiredService<ILocalMessageHandlerManager>());
        }

        [TestMethod]
        public void AddEventBus_NullArguments_ShouldThrow()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
                ((IServiceCollection)null).AddEventBus(_ => { }));

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new ServiceCollection().AddEventBus(null));
        }

        [TestMethod]
        public async Task PublishLocal_ShouldDeliverMessageToRegisteredHandler()
        {
            var serviceProvider = BuildLocalEventBus();
            await InitializeSubscribesAsync(serviceProvider);

            var publisher = serviceProvider.GetRequiredService<ILocalMessagePublisher>();
            var recorder = serviceProvider.GetRequiredService<TestRecorder>();

            await publisher.PublishAsync(new CustomerCreatedEvent { CustomerName = "Alice" });

            CollectionAssert.AreEqual(new[] { "Alice" }, recorder.Customers);
        }

        [TestMethod]
        public async Task PublishLocal_ShouldInvokeHandlersInPriorityDescendingOrder()
        {
            var serviceProvider = BuildLocalEventBus();
            await InitializeSubscribesAsync(serviceProvider);

            var publisher = serviceProvider.GetRequiredService<ILocalMessagePublisher>();
            var recorder = serviceProvider.GetRequiredService<TestRecorder>();

            await publisher.PublishAsync(new OrderPlacedEvent { OrderId = "ORD-1" });

            CollectionAssert.AreEqual(
                new[] { nameof(OrderPlacedHighPriorityHandler), nameof(OrderPlacedLowPriorityHandler) },
                recorder.Order);
        }

        [TestMethod]
        public async Task PublishLocal_NoSubscriber_ShouldNotThrow()
        {
            var serviceProvider = BuildLocalEventBus();
            await InitializeSubscribesAsync(serviceProvider);

            var publisher = serviceProvider.GetRequiredService<ILocalMessagePublisher>();

            await publisher.PublishAsync(new OrphanEvent());
        }

        [TestMethod]
        public async Task PublishLocal_HandlerThrows_ShouldNotStopOtherHandlers()
        {
            var serviceProvider = BuildLocalEventBus();
            await InitializeSubscribesAsync(serviceProvider);

            var publisher = serviceProvider.GetRequiredService<ILocalMessagePublisher>();
            var recorder = serviceProvider.GetRequiredService<TestRecorder>();

            await publisher.PublishAsync(new BrittleEvent());

            CollectionAssert.Contains(recorder.Order, nameof(SafeBrittleHandler));
        }

        [TestMethod]
        public async Task DefaultMessageHandlerInvoker_ShouldUnwrapTargetInvocationException()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddTransient<ThrowingHandler>();

            var serviceProvider = services.BuildServiceProvider();
            var invoker = new DefaultMessageHandlerInvoker(
                serviceProvider.GetRequiredService<IServiceScopeFactory>());

            await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
                await invoker.InvokeAsync(
                    typeof(ThrowingEvent),
                    typeof(ThrowingHandler),
                    new ThrowingEvent()));
        }

        [TestMethod]
        public void Message_Constructor_ShouldInitializeIdTimestampAndItems()
        {
            var message = new Message();

            Assert.AreNotEqual(Guid.Empty, message.Id);
            Assert.AreNotEqual(default, message.Timestamp);
            Assert.IsNotNull(message.Items);
            Assert.AreEqual(0, message.Items.Count);
        }

        [TestMethod]
        public void Message_AddItems_ShouldNotOverwriteExistingKey()
        {
            var message = new Message();
            message.Items["traceId"] = "original";

            message.AddItems(new Dictionary<string, string>
            {
                ["traceId"] = "overwritten",
                ["userId"] = "u-1",
            });

            Assert.AreEqual("original", message.Items["traceId"]);
            Assert.AreEqual("u-1", message.Items["userId"]);
        }

        [TestMethod]
        public void Message_RemoveItem_ShouldBeNoOpForMissingKey()
        {
            var message = new Message();
            message.Items["k"] = "v";

            message.RemoveItem("absent");
            message.RemoveItem("k");

            Assert.AreEqual(0, message.Items.Count);
        }

        [TestMethod]
        public void MessageHandlerManager_AddHandler_DuplicateShouldThrow()
        {
            var manager = new LocalMessageHandlerManager();
            manager.AddHandler(typeof(CustomerCreatedEvent), typeof(CustomerCreatedHandler));

            Assert.ThrowsExactly<ArgumentException>(() =>
                manager.AddHandler(typeof(CustomerCreatedEvent), typeof(CustomerCreatedHandler)));
        }

        [TestMethod]
        public void MessageHandlerManager_RemoveHandler_LastHandlerShouldRaiseOnEventRemoved()
        {
            var manager = new LocalMessageHandlerManager();
            manager.AddHandler(typeof(CustomerCreatedEvent), typeof(CustomerCreatedHandler));

            Type removedMessageType = null;
            manager.OnEventRemoved += (_, t) => removedMessageType = t;

            manager.RemoveHandler(typeof(CustomerCreatedEvent), typeof(CustomerCreatedHandler));

            Assert.AreEqual(typeof(CustomerCreatedEvent), removedMessageType);
            Assert.AreEqual(0, manager.MessageHandlerWrappers.Count);
        }

        [TestMethod]
        public void MessageHandlerManager_RemoveHandler_NonexistentShouldBeNoOp()
        {
            var manager = new LocalMessageHandlerManager();
            var raised = false;
            manager.OnEventRemoved += (_, _) => raised = true;

            manager.RemoveHandler(typeof(CustomerCreatedEvent), typeof(CustomerCreatedHandler));

            Assert.IsFalse(raised);
        }

        [TestMethod]
        public void MessageNameAttribute_ShouldReadAttributeValueOrFallbackToFullName()
        {
            Assert.AreEqual("test.customer-created",
                MessageNameAttribute.GetNameOrDefault(typeof(CustomerCreatedEvent)));
            Assert.AreEqual(typeof(OrphanEvent).FullName,
                MessageNameAttribute.GetNameOrDefault(typeof(OrphanEvent)));
        }

        [TestMethod]
        public void MessageGroupAttribute_ShouldReadAttributeValue()
        {
            Assert.AreEqual("orders",
                MessageGroupAttribute.GetGroupOrDefault(typeof(OrderPlacedEvent)));
        }

        [TestMethod]
        public void MessageHandlerPriorityAttribute_ShouldReadClassLevelPriority()
        {
            Assert.AreEqual(10,
                MessageHandlerPriorityAttribute.GetPriority(
                    typeof(OrderPlacedEvent), typeof(OrderPlacedHighPriorityHandler)));
            Assert.AreEqual(1,
                MessageHandlerPriorityAttribute.GetPriority(
                    typeof(OrderPlacedEvent), typeof(OrderPlacedLowPriorityHandler)));
            Assert.AreEqual(0,
                MessageHandlerPriorityAttribute.GetPriority(
                    typeof(CustomerCreatedEvent), typeof(CustomerCreatedHandler)));
        }

        [TestMethod]
        public void MessageHandlerExtensions_GetHandlerTypes_ShouldExcludeAbstractAndInterface()
        {
            var handlerTypes = MessageHandlerExtensions
                .GetHandlerTypes(typeof(EventBusUsageTest).Assembly)
                .ToList();

            CollectionAssert.Contains(handlerTypes, typeof(CustomerCreatedHandler));
            CollectionAssert.DoesNotContain(handlerTypes, typeof(IMessageHandler));
            Assert.IsFalse(handlerTypes.Any(t => t.IsAbstract));
        }

        [TestMethod]
        public void MessageHandlerExtensions_GetBaseHandlerTypes_ShouldReturnGenericInterfaces()
        {
            var baseTypes = MessageHandlerExtensions
                .GetBaseHandlerTypes(typeof(MultiMessageHandler))
                .ToList();

            CollectionAssert.Contains(baseTypes, typeof(IMessageHandler<CustomerCreatedEvent>));
            CollectionAssert.Contains(baseTypes, typeof(IMessageHandler<OrphanEvent>));
        }

        [TestMethod]
        public void MessageEnvelope_Constructor_ShouldPopulateMetadata()
        {
            var customer = new CustomerCreatedEvent { CustomerName = "Bob" };

            var envelope = new MessageEnvelope(customer);

            Assert.AreNotEqual(Guid.Empty, envelope.Id);
            Assert.AreEqual(1, envelope.Version);
            Assert.AreEqual(typeof(CustomerCreatedEvent).Assembly.GetName().Name, envelope.AssemblyName);
            Assert.AreEqual(typeof(CustomerCreatedEvent).FullName, envelope.MessageName);
            Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.MessageData));
            Assert.AreEqual(0, envelope.RetryCount);
            Assert.IsNull(envelope.NextRetryAt);
            Assert.IsNull(envelope.LastError);
        }

        [TestMethod]
        public void OutboxBackoff_RetryCountZero_ShouldReturnNow()
        {
            var now = new DateTime(2026, 6, 17, 12, 0, 0, DateTimeKind.Utc);

            var next = OutboxBackoff.CalculateNextRetry(0, new OutboxOptions(), now);

            Assert.AreEqual(now, next);
        }

        [TestMethod]
        public void OutboxBackoff_FirstRetry_ShouldRoughlyMatchInitialBackoff()
        {
            var now = new DateTime(2026, 6, 17, 12, 0, 0, DateTimeKind.Utc);
            var options = new OutboxOptions
            {
                InitialBackoff = TimeSpan.FromSeconds(5),
                MaxBackoff = TimeSpan.FromMinutes(10),
            };

            var next = OutboxBackoff.CalculateNextRetry(1, options, now);

            var delay = (next - now).TotalMilliseconds;
            Assert.IsTrue(delay >= 5000 * 0.9 && delay <= 5000 * 1.1,
                $"expected ~5000ms with ±10% jitter, actual={delay}ms");
        }

        [TestMethod]
        public void OutboxBackoff_HighRetry_ShouldNotExceedMaxBackoffPlusJitter()
        {
            var now = new DateTime(2026, 6, 17, 12, 0, 0, DateTimeKind.Utc);
            var options = new OutboxOptions
            {
                InitialBackoff = TimeSpan.FromSeconds(5),
                MaxBackoff = TimeSpan.FromMinutes(10),
            };

            var next = OutboxBackoff.CalculateNextRetry(20, options, now);

            var delay = (next - now).TotalMilliseconds;
            var maxMs = options.MaxBackoff.TotalMilliseconds;
            Assert.IsTrue(delay >= maxMs * 0.9 && delay <= maxMs * 1.1,
                $"expected ~{maxMs}ms with ±10% jitter, actual={delay}ms");
        }

        [TestMethod]
        public async Task ConfigureServiceCollection_WithModule_ShouldRegisterEventBusServices()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.Configure<EventBusOptions>(options =>
            {
                options.AddConsumers(typeof(EventBusUsageTest).Assembly);
            });
            services.AddSingleton<TestRecorder>();
            services.ConfigureServiceCollection<EventBusModuleTestStartup>();

            var serviceProvider = services.BuildServiceProvider();
            await InitializeSubscribesAsync(serviceProvider);

            var publisher = serviceProvider.GetRequiredService<ILocalMessagePublisher>();
            var recorder = serviceProvider.GetRequiredService<TestRecorder>();

            await publisher.PublishAsync(new CustomerCreatedEvent { CustomerName = "Charlie" });

            CollectionAssert.AreEqual(new[] { "Charlie" }, recorder.Customers);
        }

        // ============= helpers =============

        private static ServiceProvider BuildLocalEventBus()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<TestRecorder>();
            services.AddEventBus(options =>
            {
                options.AddLocalMq();
                options.AddConsumers(typeof(EventBusUsageTest).Assembly);
            });

            return services.BuildServiceProvider();
        }

        private static async Task InitializeSubscribesAsync(IServiceProvider serviceProvider)
        {
            foreach (var hostedService in serviceProvider.GetServices<IHostedService>())
            {
                if (hostedService is EventBusBackgroundService)
                {
                    await hostedService.StartAsync(CancellationToken.None);
                }
            }
        }

        // ============= shared test recorder =============

        public sealed class TestRecorder
        {
            public List<string> Customers { get; } = new();
            public List<string> Order { get; } = new();
        }

        // ============= test events =============

        [MessageName("test.customer-created")]
        public class CustomerCreatedEvent : Message
        {
            public string CustomerName { get; set; }
        }

        [MessageName("test.order-placed")]
        [MessageGroup("orders")]
        public class OrderPlacedEvent : Message
        {
            public string OrderId { get; set; }
        }

        public class OrphanEvent : Message
        {
        }

        public class BrittleEvent : Message
        {
        }

        public class ThrowingEvent : Message
        {
        }

        // ============= test handlers =============

        public class CustomerCreatedHandler : IMessageHandler<CustomerCreatedEvent>
        {
            private readonly TestRecorder _recorder;

            public CustomerCreatedHandler(TestRecorder recorder)
            {
                _recorder = recorder;
            }

            public Task HandAsync(CustomerCreatedEvent message, CancellationToken cancellationToken = default)
            {
                _recorder.Customers.Add(message.CustomerName);
                return Task.CompletedTask;
            }
        }

        [MessageHandlerPriority(10)]
        public class OrderPlacedHighPriorityHandler : IMessageHandler<OrderPlacedEvent>
        {
            private readonly TestRecorder _recorder;

            public OrderPlacedHighPriorityHandler(TestRecorder recorder)
            {
                _recorder = recorder;
            }

            public Task HandAsync(OrderPlacedEvent message, CancellationToken cancellationToken = default)
            {
                _recorder.Order.Add(nameof(OrderPlacedHighPriorityHandler));
                return Task.CompletedTask;
            }
        }

        [MessageHandlerPriority(1)]
        public class OrderPlacedLowPriorityHandler : IMessageHandler<OrderPlacedEvent>
        {
            private readonly TestRecorder _recorder;

            public OrderPlacedLowPriorityHandler(TestRecorder recorder)
            {
                _recorder = recorder;
            }

            public Task HandAsync(OrderPlacedEvent message, CancellationToken cancellationToken = default)
            {
                _recorder.Order.Add(nameof(OrderPlacedLowPriorityHandler));
                return Task.CompletedTask;
            }
        }

        [MessageHandlerPriority(5)]
        public class FaultyBrittleHandler : IMessageHandler<BrittleEvent>
        {
            public Task HandAsync(BrittleEvent message, CancellationToken cancellationToken = default)
            {
                throw new InvalidOperationException("intentional");
            }
        }

        [MessageHandlerPriority(1)]
        public class SafeBrittleHandler : IMessageHandler<BrittleEvent>
        {
            private readonly TestRecorder _recorder;

            public SafeBrittleHandler(TestRecorder recorder)
            {
                _recorder = recorder;
            }

            public Task HandAsync(BrittleEvent message, CancellationToken cancellationToken = default)
            {
                _recorder.Order.Add(nameof(SafeBrittleHandler));
                return Task.CompletedTask;
            }
        }

        public class ThrowingHandler : IMessageHandler<ThrowingEvent>
        {
            public Task HandAsync(ThrowingEvent message, CancellationToken cancellationToken = default)
            {
                throw new InvalidOperationException("unwrap me");
            }
        }

        public class MultiMessageHandler
            : IMessageHandler<CustomerCreatedEvent>, IMessageHandler<OrphanEvent>
        {
            public Task HandAsync(CustomerCreatedEvent message, CancellationToken cancellationToken = default)
                => Task.CompletedTask;

            public Task HandAsync(OrphanEvent message, CancellationToken cancellationToken = default)
                => Task.CompletedTask;
        }

        // ============= test startup module =============

        [DependsOn(typeof(CoreEventBusModule), typeof(CoreEventBusLocalModule))]
        private sealed class EventBusModuleTestStartup : CoreModuleBase
        {
        }
    }
}
