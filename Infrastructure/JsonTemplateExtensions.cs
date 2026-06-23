using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;

namespace Adapter.AI.Extensions
{
    public static class JsonTemplateExtensions
    {
        /// <summary>
        /// Generates a JSON template string for the given type with example values and comments.
        /// </summary>
        /// <typeparam name="T">The type to generate a template for</typeparam>
        /// <param name="instance">The instance (can be null, used only for type inference)</param>
        /// <param name="indent">Current indentation level (for recursive calls)</param>
        /// <returns>A formatted JSON template string</returns>
        public static string ToJsonTemplate<T>(this T? instance, int indent = 0)
        {
            return ToJsonTemplate(typeof(T), indent);
        }

        /// <summary>
        /// Generates a JSON template string for the given type.
        /// </summary>
        /// <param name="type">The type to generate a template for</param>
        /// <param name="indent">Current indentation level</param>
        /// <returns>A formatted JSON template string</returns>
        public static string ToJsonTemplate(Type type, int indent = 0)
        {
            if(type.IsPrimitive || type == typeof(string)) {
                return string.Empty;
            }

            var sb = new StringBuilder();
            var indentStr = new string(' ', indent * 2);
            var nextIndentStr = new string(' ', (indent + 1) * 2);

            // Handle nullable types
            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            // Handle primitive types
            if (IsPrimitiveOrString(underlyingType))
            {
                return GetPrimitiveTemplate(underlyingType);
            }

            // Handle enums
            if (underlyingType.IsEnum)
            {
                var enumValues = Enum.GetNames(underlyingType);
                return $"\"{string.Join("|", enumValues)}\"";
            }

            // Handle collections
            if (IsCollectionType(underlyingType, out var elementType))
            {
                sb.AppendLine("[");
                if (elementType != null)
                {
                    var elementTemplate = ToJsonTemplate(elementType, indent + 1);
                    sb.Append(nextIndentStr);
                    
                    // For primitive types, show inline
                    if (IsPrimitiveOrString(elementType))
                    {
                        sb.AppendLine(elementTemplate);
                    }
                    else
                    {
                        sb.AppendLine(elementTemplate);
                    }
                }
                sb.Append(indentStr);
                sb.Append("]");
                return sb.ToString();
            }

            // Handle complex objects
            sb.AppendLine("{");

            var properties = GetSerializableProperties(underlyingType);
            var propertyCount = properties.Count;

            for (int i = 0; i < propertyCount; i++)
            {
                var prop = properties[i];
                var propName = GetJsonPropertyName(prop);
                var propType = prop.PropertyType;

                sb.Append(nextIndentStr);
                sb.Append($"\"{propName}\": ");

                var propTemplate = ToJsonTemplate(propType, indent + 1);
                
                // For objects and arrays, put on new line
                if (propTemplate.StartsWith("{") || propTemplate.StartsWith("["))
                {
                    sb.Append(propTemplate);
                }
                else
                {
                    sb.Append(propTemplate);
                }

                if (i < propertyCount - 1)
                {
                    sb.Append(",");
                }

                // Add comment with property description if available
                var description = GetPropertyDescription(prop);
                if (!string.IsNullOrEmpty(description))
                {
                    sb.Append($" // {description}");
                }

                sb.AppendLine();
            }

            sb.Append(indentStr);
            sb.Append("}");

            return sb.ToString();
        }

        private static bool IsPrimitiveOrString(Type type)
        {
            return type.IsPrimitive || 
                   type == typeof(string) || 
                   type == typeof(decimal) || 
                   type == typeof(DateTime) || 
                   type == typeof(DateTimeOffset) || 
                   type == typeof(TimeSpan) || 
                   type == typeof(Guid);
        }

        private static string GetPrimitiveTemplate(Type type)
        {
            if (type == typeof(string))
                return "\"string\"";
            if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte))
                return "0";
            if (type == typeof(decimal) || type == typeof(double) || type == typeof(float))
                return "0.0";
            if (type == typeof(bool))
                return "false";
            if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
                return "\"2024-01-01T00:00:00Z\"";
            if (type == typeof(TimeSpan))
                return "\"00:00:00\"";
            if (type == typeof(Guid))
                return "\"00000000-0000-0000-0000-000000000000\"";

            return "\"value\"";
        }

        private static bool IsCollectionType(Type type, out Type? elementType)
        {
            elementType = null;

            // Check for arrays
            if (type.IsArray)
            {
                elementType = type.GetElementType();
                return true;
            }

            // Check for generic collections
            if (type.IsGenericType)
            {
                var genericDef = type.GetGenericTypeDefinition();
                if (genericDef == typeof(List<>) || 
                    genericDef == typeof(IEnumerable<>) || 
                    genericDef == typeof(ICollection<>) || 
                    genericDef == typeof(IList<>))
                {
                    elementType = type.GetGenericArguments()[0];
                    return true;
                }
            }

            // Check if implements IEnumerable<T>
            var enumerableInterface = type.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            
            if (enumerableInterface != null)
            {
                elementType = enumerableInterface.GetGenericArguments()[0];
                return true;
            }

            return false;
        }

        private static List<PropertyInfo> GetSerializableProperties(Type type)
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && !HasJsonIgnoreAttribute(p))
                .OrderBy(p => p.Name)
                .ToList();
        }

        private static string GetJsonPropertyName(PropertyInfo property)
        {
            var jsonPropertyAttr = property.GetCustomAttribute<JsonPropertyNameAttribute>();
            if (jsonPropertyAttr != null)
            {
                return jsonPropertyAttr.Name;
            }

            // Default to camelCase
            var name = property.Name;
            return char.ToLowerInvariant(name[0]) + name.Substring(1);
        }

        private static bool HasJsonIgnoreAttribute(PropertyInfo property)
        {
            return property.GetCustomAttribute<JsonIgnoreAttribute>() != null;
        }

        private static string? GetPropertyDescription(PropertyInfo property)
        {
            // Try to get description from common documentation attributes
            // This is a simple implementation - could be extended to read XML doc comments
            
            // Check for Display attribute
            var displayAttr = property.GetCustomAttributes()
                .FirstOrDefault(a => a.GetType().Name == "DisplayAttribute");
            
            if (displayAttr != null)
            {
                var descProp = displayAttr.GetType().GetProperty("Description");
                if (descProp != null)
                {
                    var desc = descProp.GetValue(displayAttr) as string;
                    if (!string.IsNullOrEmpty(desc))
                        return desc;
                }
            }

            return null;
        }

        /// <summary>
        /// Generates a compact JSON template without whitespace and comments.
        /// </summary>
        public static string ToCompactJsonTemplate<T>(this T? instance)
        {
            var template = ToJsonTemplate(instance);
            // Remove comments
            var lines = template.Split('\n')
                .Select(line =>
                {
                    var commentIndex = line.IndexOf("//");
                    return commentIndex >= 0 ? line.Substring(0, commentIndex).TrimEnd() : line;
                })
                .Where(line => !string.IsNullOrWhiteSpace(line));

            return string.Join("", lines).Replace("  ", "").Replace("\r", "");
        }

        /// <summary>
        /// Generates a JSON template with example values based on property names.
        /// </summary>
        public static string ToJsonTemplateWithExamples<T>(this T? instance, Dictionary<string, object>? exampleValues = null)
        {
            var type = typeof(T);
            return ToJsonTemplateWithExamples(type, exampleValues ?? new Dictionary<string, object>());
        }

        private static string ToJsonTemplateWithExamples(Type type, Dictionary<string, object> exampleValues, int indent = 0)
        {
            var sb = new StringBuilder();
            var indentStr = new string(' ', indent * 2);
            var nextIndentStr = new string(' ', (indent + 1) * 2);

            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            if (IsPrimitiveOrString(underlyingType))
            {
                return GetPrimitiveTemplate(underlyingType);
            }

            if (underlyingType.IsEnum)
            {
                var firstValue = Enum.GetNames(underlyingType).FirstOrDefault() ?? "Unknown";
                return $"\"{firstValue}\"";
            }

            if (IsCollectionType(underlyingType, out var elementType))
            {
                sb.AppendLine("[");
                if (elementType != null)
                {
                    sb.Append(nextIndentStr);
                    sb.AppendLine(ToJsonTemplateWithExamples(elementType, exampleValues, indent + 1));
                }
                sb.Append(indentStr);
                sb.Append("]");
                return sb.ToString();
            }

            sb.AppendLine("{");

            var properties = GetSerializableProperties(underlyingType);
            var propertyCount = properties.Count;

            for (int i = 0; i < propertyCount; i++)
            {
                var prop = properties[i];
                var propName = GetJsonPropertyName(prop);
                var propType = prop.PropertyType;

                sb.Append(nextIndentStr);
                sb.Append($"\"{propName}\": ");

                // Check if we have an example value
                if (exampleValues.TryGetValue(propName, out var exampleValue))
                {
                    sb.Append(System.Text.Json.JsonSerializer.Serialize(exampleValue));
                }
                else
                {
                    var propTemplate = ToJsonTemplateWithExamples(propType, exampleValues, indent + 1);
                    sb.Append(propTemplate);
                }

                if (i < propertyCount - 1)
                {
                    sb.Append(",");
                }

                sb.AppendLine();
            }

            sb.Append(indentStr);
            sb.Append("}");

            return sb.ToString();
        }
    }
}
