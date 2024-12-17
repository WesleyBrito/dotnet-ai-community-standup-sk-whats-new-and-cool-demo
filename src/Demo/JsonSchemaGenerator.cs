// Ignore Spelling: json

using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Demo;

internal class JsonSchemaGenerator
{
    private static JsonSerializerOptions? DefaultJsonSerializerOptions;

    private static readonly AIJsonSchemaCreateOptions DefaultAIJsonSchemaCreateOptions = new()
    {
        IncludeSchemaKeyword = false,
        IncludeTypeInEnumSchemas = true,
        RequireAllProperties = false,
        DisallowAdditionalProperties = false,
    };

    private static readonly JsonElement TrueSchemaAsObject = JsonDocument.Parse("{}").RootElement;

    private static readonly JsonElement FalseSchemaAsObject = JsonDocument.Parse("""{"not":true}""").RootElement;

    public static string FromType<TSchemaType>()
    {
        JsonSerializerOptions options = new(JsonSerializerOptions.Default)
        {
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        };
        AIJsonSchemaCreateOptions config = new()
        {
            IncludeSchemaKeyword = false,
            DisallowAdditionalProperties = true,
        };

        var type = typeof(TSchemaType);

        var typeKernelJsonSchema = Build(type, type.Name, config);
        
        return JsonSerializer.Serialize(typeKernelJsonSchema, GetDefaultOptions());
    }

    public static KernelJsonSchema Build(Type type, string? description = null, AIJsonSchemaCreateOptions? configuration = null)
    {
        return Build(type, GetDefaultOptions(), description, configuration);
    }

    public static KernelJsonSchema Build(Type type, JsonSerializerOptions options, string? description = null, AIJsonSchemaCreateOptions? configuration = null)
    {
        configuration ??= DefaultAIJsonSchemaCreateOptions;
        
        var schemaDocument = AIJsonUtilities.CreateJsonSchema(type, description, serializerOptions: options, inferenceOptions: configuration);
        
        switch (schemaDocument.ValueKind)
        {
            case JsonValueKind.False:
                schemaDocument = FalseSchemaAsObject;
                break;

            case JsonValueKind.True:
                schemaDocument = TrueSchemaAsObject;
                break;
        }

        return KernelJsonSchema.Parse(schemaDocument.GetRawText());
    }

    private static JsonSerializerOptions GetDefaultOptions()
    {
        if (DefaultJsonSerializerOptions is null)
        {
            JsonSerializerOptions options = new()
            {
                TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
                Converters = { new JsonStringEnumConverter() },
            };
            options.MakeReadOnly();
            DefaultJsonSerializerOptions = options;
        }

        return DefaultJsonSerializerOptions;
    }
}
