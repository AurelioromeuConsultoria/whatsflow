using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace WhatsFlow.API.Swagger;

public class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var fileParameters = context.MethodInfo.GetParameters()
            .Where(p => IsFileParameter(p.ParameterType))
            .ToList();

        if (!fileParameters.Any())
            return;

        var properties = new Dictionary<string, OpenApiSchema>();
        var requiredProperties = new HashSet<string>();

        foreach (var param in fileParameters)
        {
            var paramName = param.Name ?? "file";
            var isList = param.ParameterType.IsGenericType && 
                        param.ParameterType.GetGenericTypeDefinition() == typeof(List<>) &&
                        param.ParameterType.GetGenericArguments()[0] == typeof(IFormFile);

            properties[paramName] = isList
                ? new OpenApiSchema
                {
                    Type = "array",
                    Items = new OpenApiSchema { Type = "string", Format = "binary" }
                }
                : new OpenApiSchema
                {
                    Type = "string",
                    Format = "binary"
                };

            if (!param.IsOptional)
            {
                requiredProperties.Add(paramName);
            }
        }

        // Verificar se já existe um RequestBody e mesclar ou substituir
        if (operation.RequestBody == null)
        {
            operation.RequestBody = new OpenApiRequestBody();
        }

        operation.RequestBody.Content = new Dictionary<string, OpenApiMediaType>
        {
            ["multipart/form-data"] = new OpenApiMediaType
            {
                Schema = new OpenApiSchema
                {
                    Type = "object",
                    Properties = properties,
                    Required = requiredProperties
                }
            }
        };

        // Remover apenas os parâmetros de IFormFile da lista de Parameters
        if (operation.Parameters != null)
        {
            var fileParamNames = fileParameters
                .Select(p => p.Name)
                .Where(name => !string.IsNullOrEmpty(name))
                .ToHashSet();

            operation.Parameters = operation.Parameters
                .Where(p => p.Name == null || !fileParamNames.Contains(p.Name))
                .ToList();
        }
    }

    private static bool IsFileParameter(Type parameterType)
    {
        if (parameterType == typeof(IFormFile))
            return true;

        if (parameterType.IsGenericType && 
            parameterType.GetGenericTypeDefinition() == typeof(List<>) &&
            parameterType.GetGenericArguments()[0] == typeof(IFormFile))
            return true;

        return false;
    }
}
