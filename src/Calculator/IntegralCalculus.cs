using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using MaximaSharp;
using System.Linq.Expressions;

namespace CalculusCalculator
{
    public static class IntegralCalculus
    {
        [FunctionName("IntegralCalculus")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            Config.FunctionDirectory = context.FunctionDirectory;

            string function = req.Query["f"];

            if (string.IsNullOrEmpty(function))
                return new BadRequestObjectResult("Please pass a function to be integrated");

            function = function ?? "";

            try
            {
                var result = function.Integrate();

                if (result != null)
                    return new OkObjectResult(result);
                else
                    return new BadRequestObjectResult("Erro on integration");
            }
            catch (Exception ex)
            {

                throw;
            }
        }
    }
}
