using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Enable.Extensions.Queuing.Abstractions;
using Microsoft.Extensions.Logging;

namespace Enable.Extensions.Queuing.Discovery
{
    // TODO Console WriteLine on discover message handler.
    public static class MessageHandlerDiscovery
    {
        public static async Task RegisterMessageHandler<TService, TMessage>(
            this IServiceProvider services,
            string entityPath,
            Func<TService, TMessage, Task> handleMessage)
            where TService : class
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (string.IsNullOrWhiteSpace(entityPath))
            {
                throw new ArgumentException(nameof(entityPath));
            }

            if (handleMessage == null)
            {
                throw new ArgumentNullException(nameof(handleMessage));
            }

            var queueManager = (QueueManager)services.GetService(typeof(QueueManager));

            if (queueManager == null)
            {
                throw new Exception("Service of type " + typeof(QueueManager).FullName + " not found.");
            }

            var queueClient = queueManager.GetOrAddQueueClient(entityPath);

            async Task handler(IQueueMessage message, CancellationToken ct)
            {
                var service = (TService)services.GetService(typeof(TService));

                if (service == null)
                {
                    throw new Exception("Service of type " + typeof(TService).FullName + " not found.");
                }

                TMessage payload;

                if (typeof(TMessage) == typeof(IQueueMessage))
                {
                    payload = (TMessage)message;
                }
                else
                {
                    payload = message.GetBody<TMessage>();
                }

                await handleMessage(service, payload).ConfigureAwait(false);
            }

            var messageHandlerOptions = (MessageHandlerOptions)services.GetService(typeof(MessageHandlerOptions));

            await queueClient.RegisterMessageHandler(handler, messageHandlerOptions ?? new MessageHandlerOptions()).ConfigureAwait(false);
        }

        public static async Task DiscoverMessageHandlers(
            this IServiceProvider services,
            Assembly assembly)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            var loggerFactory = (ILoggerFactory)services.GetService(typeof(ILoggerFactory));
            var logger = loggerFactory?.CreateLogger(typeof(MessageHandlerDiscovery).FullName);

            var handlers = from t in assembly.GetTypes()
                           from m in t.GetMethods()
                           let a = m.GetCustomAttribute<MessageHandlerAttribute>()
                           where a != null
                           select new
                           {
                               Type = t,
                               Method = m,
                               Attribute = a
                           };

            foreach (var handler in handlers)
            {
                await services.RegisterDiscoveredMessageHandler(handler.Type, handler.Method, handler.Attribute, logger).ConfigureAwait(false);
            }
        }

        private static async Task RegisterDiscoveredMessageHandler(
            this IServiceProvider services,
            Type serviceType,
            MethodInfo method,
            MessageHandlerAttribute attribute,
            ILogger logger)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (serviceType == null)
            {
                throw new ArgumentNullException(nameof(serviceType));
            }

            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            if (attribute == null)
            {
                throw new ArgumentNullException(nameof(attribute));
            }

            var methodParameters = method.GetParameters();

            if (methodParameters.Length != 1)
            {
                throw new Exception("Message handler method must have one parameter.");
            }

            var entityPath = attribute.EntityPath;
            var messageType = methodParameters[0].ParameterType;

            if (logger != null)
            {
                var logMessage = $"Registering discovered message handler {serviceType.Name}.{method.Name}({messageType.Name}) for queue \"{entityPath}\"";
                logger.LogInformation(logMessage);
            }

            var exprService = Expression.Parameter(serviceType, "service");
            var exprMessage = Expression.Parameter(messageType, "message");

            var exprHandler = Expression.Call(exprService, method, new[] { exprMessage });
            var exprHandlerLambda = Expression.Lambda(exprHandler, exprService, exprMessage);

            var handler = exprHandlerLambda.Compile();

            var register = typeof(MessageHandlerDiscovery)
                .GetMethod(nameof(RegisterMessageHandler))
                .MakeGenericMethod(serviceType, messageType);

            var registerTask = (Task)register.Invoke(null, new object[] { services, entityPath, handler });

            await registerTask.ConfigureAwait(false);
        }
    }
}
