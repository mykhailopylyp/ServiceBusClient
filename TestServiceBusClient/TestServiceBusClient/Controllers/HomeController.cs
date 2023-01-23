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
        public static bool StartedProcessing { get; set; } = false;
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
            if (!StartedProcessing)
            {
                try
                {
                    await MvcApplication.Processor.StartProcessingAsync(CancellationToken.None);
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                
                StartedProcessing = false;
            }
            

            await Task.Run(() =>
            {
                Thread.Sleep(5 * 1000);
            });

            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}