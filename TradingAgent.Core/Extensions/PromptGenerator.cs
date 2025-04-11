using System.Text.Json;
using System.Text.Json.Serialization;
using Json.Schema;
using Json.Schema.Generation;

namespace TradingAgent.Core.Extensions;

public static class PromptGenerator
{
    public static string GenerateSchemaPrompt(this object obj, string name)
    {
        var schema = new JsonSchemaBuilder()
            .FromType(obj.GetType())
            .Build();
        
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };
        
        var json = JsonSerializer.Serialize(schema, options);
        return $"[{name} Schema]:\n{json}";
    }
    
    public static string GenerateDataPrompt(this object obj, string name)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };
        
        var json = JsonSerializer.Serialize(obj, options);
        return $"[{name} Data]:\n{json}";
    }
    
    public static string GenerateSchemaAndDataPrompt(this object obj, string name)
    {
        var schema = new JsonSchemaBuilder()
            .FromType(obj.GetType())
            .Build();
        
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };
        
        var json = JsonSerializer.Serialize(schema, options);
        return $"[{name} Schema]:\n{json}\n\n[{name} Data]:\n{JsonSerializer.Serialize(obj, options)}";
    }

    public static JsonSchema GetSchema(this object obj)
    {
        var schema = new JsonSchemaBuilder()
            .FromType(obj.GetType())
            .Build();
        
        return schema;
    }
}