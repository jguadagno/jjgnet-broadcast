using System.Reflection;
using System.Xml.Linq;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace JosephGuadagno.Broadcasting.Api;

/// <summary>
/// Transforms OpenAPI document to include XML documentation comments.
/// </summary>
public sealed class XmlDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

        if (!File.Exists(xmlPath))
        {
            return Task.CompletedTask;
        }

        try
        {
            var xmlDoc = XDocument.Load(xmlPath);
            var members = xmlDoc.Descendants("member")
                .Where(m => m.Attribute("name") != null)
                .GroupBy(m => m.Attribute("name")!.Value)
                .ToDictionary(
                    g => g.Key,
                    g => g.First().Element("summary")?.Value.Trim() ?? string.Empty
                );

            foreach (var path in document.Paths)
            {
                foreach (var operation in path.Value.Operations)
                {
                    // Try to find the controller method documentation
                    var operationId = operation.Value.OperationId;
                    if (!string.IsNullOrEmpty(operationId))
                    {
                        // Look for matching XML documentation with exact match or method prefix
                        var methodKey = members.Keys.FirstOrDefault(k => 
                            k.EndsWith($".{operationId}") || k.Contains($".{operationId}("));
                        if (methodKey != null && members.TryGetValue(methodKey, out var summary))
                        {
                            operation.Value.Summary = summary;
                        }
                    }
                }
            }
        }
        catch (Exception)
        {
            // If XML documentation cannot be loaded or parsed, continue without it
            // This prevents the application from failing to start due to malformed XML
        }

        return Task.CompletedTask;
    }
}
