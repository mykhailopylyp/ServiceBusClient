using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

namespace TestServiceBusClient.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public async Task<ActionResult> Contact()
        {
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

            var serviceBusClient = new ServiceBusClient(fullyQualifiedNamespace, credentials, new ServiceBusClientOptions
            {
                RetryOptions = new ServiceBusRetryOptions
                {
                    MaxRetries = 5,    //The maximum number of retry attempts before giving up
                    Mode = ServiceBusRetryMode.Exponential,        //The approach to use for calculating retry delays
                    MaxDelay = TimeSpan.FromMilliseconds(90000)
                }
            });

            var administartionClient = new ServiceBusAdministrationClient(fullyQualifiedNamespace, credentials);

            if (await administartionClient.QueueExistsAsync(queueName, CancellationToken.None) == false)
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

                _ = await administartionClient.CreateQueueAsync(queueOptions, CancellationToken.None);
            }

            var processor = serviceBusClient.CreateProcessor(queueName, new ServiceBusProcessorOptions()
            {
                AutoCompleteMessages = true,
                MaxConcurrentCalls = Environment.ProcessorCount,
                PrefetchCount = Environment.ProcessorCount * 100,
                ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete
            });

            processor.ProcessMessageAsync += (args) =>
            {
                return Task.Run(() =>
                {
                    Thread.Sleep(1000);
                });
            };

            processor.ProcessErrorAsync += OnMessageProcessingException;
            await processor.StartProcessingAsync(CancellationToken.None);

            await Task.Run(() =>
            {
                Thread.Sleep(5 * 1000);
            });

            ViewBag.Message = "Your contact page.";

            return View();
        }

        private async Task OnMessageProcessingException(ProcessErrorEventArgs args)
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