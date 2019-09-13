using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace CacheInvalidationRoute
{
    public class Function
    {
        public bool FunctionHandler(Request request, ILambdaContext context)
        {
            if (!string.IsNullOrWhiteSpace(request.url))
            {
                try
                {
                    CacheHelper.DeleteCacheEntityWithUrl(request.url);
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    return false;
                }
            }
            return false;
        }
    }
}
