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
    public static class Function1
    {
        [FunctionName("IntegralCalculus")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string function = req.Query["f"];

            if (string.IsNullOrEmpty(function))
                return new BadRequestObjectResult("Please pass a function to be integrated");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            function = function ?? data?.function;

            var result = function.Integrate();

            return result != null
                ? (ActionResult)new OkObjectResult(result)
                : new BadRequestObjectResult("Erro on integration");
        }
    }
}
