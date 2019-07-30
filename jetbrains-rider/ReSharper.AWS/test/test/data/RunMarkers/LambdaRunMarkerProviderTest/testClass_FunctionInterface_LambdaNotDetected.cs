using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;

namespace HelloWorld
{
    public interface Function
    {
        public APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context);
    }
}