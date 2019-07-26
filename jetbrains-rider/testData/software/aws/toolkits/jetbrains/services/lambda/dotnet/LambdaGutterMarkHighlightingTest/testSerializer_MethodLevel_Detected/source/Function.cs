using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;

namespace HelloWorld
{
    public class Function
    {
        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]
        public Product DescribeProduct(DescribeProductRequest request)
        {
            var catalogService = new CatalogService();
            return catalogService.DescribeProduct(request.Id);
        }
    }

    public class DescribeProductRequest
    {
        public int Id { get; }

        public DescribeProductRequest(int id)
        {
            Id = id;
        }
    }

    public class Product
    {
        public int Id { get; }

        public Product(int id)
        {
            Id = id;
        }
    }

    public class CatalogService
    {
        public Product DescribeProduct(int id)
        {
            return new Product(id);
        }
    }
}