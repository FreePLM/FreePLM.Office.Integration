using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FreePLM.Office.Integration.Filters;

/// <summary>
/// Swagger operation filter to handle file upload parameters
/// </summary>
public class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var fileParameters = context.MethodInfo.GetParameters()
            .Where(p => p.ParameterType == typeof(IFormFile))
            .ToList();

        if (!fileParameters.Any())
            return;

        operation.RequestBody = new OpenApiRequestBody
        {
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = context.MethodInfo.GetParameters()
                            .ToDictionary(
                                p => p.Name ?? string.Empty,
                                p => p.ParameterType == typeof(IFormFile)
                                    ? new OpenApiSchema
                                    {
                                        Type = "string",
                                        Format = "binary"
                                    }
                                    : new OpenApiSchema
                                    {
                                        Type = GetSchemaType(p.ParameterType)
                                    }
                            ),
                        Required = context.MethodInfo.GetParameters()
                            .Where(p => !p.IsOptional && !p.HasDefaultValue)
                            .Select(p => p.Name ?? string.Empty)
                            .ToHashSet()
                    }
                }
            }
        };
    }

    private static string GetSchemaType(Type type)
    {
        if (type == typeof(string))
            return "string";
        if (type == typeof(int) || type == typeof(long))
            return "integer";
        if (type == typeof(bool))
            return "boolean";
        if (type == typeof(double) || type == typeof(float) || type == typeof(decimal))
            return "number";

        return "string";
    }
}
