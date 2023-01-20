using Azure.Identity;
using Azure.Messaging.ServiceBus.Administration;
using Azure.Messaging.ServiceBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Threading.Tasks;
using System.Diagnostics;

namespace TestServiceBusClient
{
    public class MvcApplication : System.Web.HttpApplication
    {
        public static ServiceBusClient ServiceBusClient { get; set; }
        public static ServiceBusProcessor Processor { get; set; }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            var fullyQualifiedNamespace = "qcash-concert2-test.servicebus.windows.net";
            var queueName = "TestQueue";
            var credentials = new DefaultAzureCredential(new DefaultAzureCredentialOptions()
            {
                ExcludeAzureCliCredential = false,
                ExcludeEnvironmentCredential = false,
                ExcludeManagedIdentityCredential = false,
                ExcludeSharedTokenCacheCredential = false,
                ExcludeInteractiveBrowserCredential = true,
                ExcludeVisualStudioCodeCredential = false,
                ExcludeVisualStudioCredential = false,
            });

            ServiceBusClient = new ServiceBusClient(fullyQualifiedNamespace, credentials, new ServiceBusClientOptions
            {
                RetryOptions = new ServiceBusRetryOptions
                {
                    MaxRetries = 5,    //The maximum number of retry attempts before giving up
                    Mode = ServiceBusRetryMode.Exponential,        //The approach to use for calculating retry delays
                    MaxDelay = TimeSpan.FromMilliseconds(90000)
                }
            });

            var administartionClient = new ServiceBusAdministrationClient(fullyQualifiedNamespace, credentials);

            if (administartionClient.QueueExistsAsync(queueName, CancellationToken.None).Result == false)
            {
                var queueOptions = new CreateQueueOptions(queueName)
                {
                    DeadLetteringOnMessageExpiration = true,
                    EnableBatchedOperations = true,
                    EnablePartitioning = false,
                    MaxSizeInMegabytes = 1024,
                    RequiresDuplicateDetection = false,
                    RequiresSession = false,
                };

                queueOptions.AutoDeleteOnIdle = TimeSpan.FromMinutes(60);

                var _ = administartionClient.CreateQueueAsync(queueOptions, CancellationToken.None).Result;
            }

            Processor = ServiceBusClient.CreateProcessor(queueName, new ServiceBusProcessorOptions()
            {
                AutoCompleteMessages = true,
                MaxConcurrentCalls = Environment.ProcessorCount,
                PrefetchCount = Environment.ProcessorCount * 100,
                ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete
            });

            Processor.ProcessMessageAsync += OnMessageProcessingAsync;

            Processor.ProcessErrorAsync += OnMessageProcessingException;
        }

        private static Task OnMessageProcessingAsync(ProcessMessageEventArgs args)
        {
            return Task.Run(() =>
            {
                Thread.Sleep(1000);
            });
        }

        private static async Task OnMessageProcessingException(ProcessErrorEventArgs args)
        {
            if (args == null || args.CancellationToken.IsCancellationRequested)
                return;

            if (args.Exception is ServiceBusException serviceBusException
                && serviceBusException.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
            {
                // await ServiceBusFactory.CreateQueueAsync(_processor.FullyQualifiedNamespace, args.EntityPath, _queueTimeToLive, CancellationToken.None).ConfigureAwait(false);
            }
        }
    }
}
