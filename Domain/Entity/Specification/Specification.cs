using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entity.Specification
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Linq.Expressions;
    using System.Reflection.Metadata;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    [JsonDerivedType(typeof(AndSpecificationDto), "And")]
    [JsonDerivedType(typeof(OrSpecificationDto), "Or")]
    [JsonDerivedType(typeof(NotSpecificationDto), "Not")]
    [JsonDerivedType(typeof(PropertySpecificationDto), "Property")]
    
    public abstract record SpecificationDto
    {
        public string ToJson() => JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        public AndSpecificationDto And(SpecificationDto right) => new(this, right);
        public OrSpecificationDto Or(SpecificationDto right) => new(this, right);
        public NotSpecificationDto Not() => new(this);
    }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum MatchOperator
    {
        Equals,
        Contains,
        GreaterThan,
        GreaterOrEqual,
        LesserThan,
        LesserOrEqual
    }

    public record PropertySpecificationDto(string PropertyPath, MatchOperator Operator, JsonElement Value) : SpecificationDto;

    public record AndSpecificationDto(SpecificationDto Left, SpecificationDto Right) : SpecificationDto;
    public record Tautology() : SpecificationDto;
    public record OrSpecificationDto(SpecificationDto Left, SpecificationDto Right) : SpecificationDto;
    public record NotSpecificationDto(SpecificationDto Inner) : SpecificationDto;

    public static class SpecificationExpressionFactory
    {
        public static Expression<Func<T, bool>> FromJson<T>(string json)
        {
            var spec = JsonSerializer.Deserialize<PropertySpecificationDto>(json);
            return ToExpression<T>(spec);
        }
        public static Expression<Func<T, bool>> ToExpression<T>(SpecificationDto dto)
        {
            var param = Expression.Parameter(typeof(T), "x");
            var body = BuildBody(param, dto);
            return Expression.Lambda<Func<T, bool>>(body, param);
        }

        private static Expression BuildBody(ParameterExpression param, SpecificationDto dto)
        {
            return dto switch
            {
                PropertySpecificationDto p => BuildPropertyExpression(param, p),
                AndSpecificationDto a => Expression.AndAlso(BuildBody(param, a.Left), BuildBody(param, a.Right)),
                OrSpecificationDto o => Expression.OrElse(BuildBody(param, o.Left), BuildBody(param, o.Right)),
                NotSpecificationDto n => Expression.Not(BuildBody(param, n.Inner)),
                Tautology n => Expression.Constant(true),
                _ => throw new NotSupportedException($"Unsupported DTO: {dto.GetType().Name}")
            };
        }

        private static Expression BuildPropertyExpression(ParameterExpression param, PropertySpecificationDto dto)
        {
            // Navigate nested property path
            Expression prop = param;
            foreach (var part in dto.PropertyPath.Split('.'))
                prop = Expression.Property(prop, part);

            object convertedValue = null;

            switch (dto.Value.ValueKind)
            {
                case JsonValueKind.String:
                    // Attempt to convert the string to the original type (e.g., DateTime, Guid)
                    convertedValue = Convert.ChangeType(dto.Value.GetString(), typeof(string));
                    break;

                case JsonValueKind.Number:
                    // Check if the number has a fractional part.
                    // GetDouble() is used because it can represent both integers and decimals.
                    double doubleValue = dto.Value.GetDouble();

                    // Use a small epsilon for floating-point comparisons to avoid precision issues
                    if (Math.Abs(doubleValue - Math.Truncate(doubleValue)) < 0.000001)
                    {
                        // If the number is an integer (no fractional part), we can use Int64 to avoid overflow.
                        convertedValue = Convert.ChangeType(dto.Value.GetInt32(), typeof(Int32));
                    }
                    else
                    {
                        // If the number has a fractional part, it's a double.
                        convertedValue = Convert.ChangeType(dto.Value.GetDouble(), typeof(Double));
                    }
                    break;

                case JsonValueKind.True:
                case JsonValueKind.False:
                    convertedValue = dto.Value.GetBoolean();
                    break;

                case JsonValueKind.Null:
                    convertedValue = null;
                    break;

                // Add cases for Object, Array, etc.
                default:
                    throw new InvalidOperationException("Unsupported JsonValueKind.");
            }

            // Futuro: Passar tipo do JsonElement aqui
            var constant = Expression.Constant(convertedValue);

            return dto.Operator switch
            {
                MatchOperator.Equals => Expression.Equal(prop, constant),
                MatchOperator.Contains => Expression.Call(prop, nameof(string.Contains), Type.EmptyTypes, constant),
                MatchOperator.GreaterThan => Expression.GreaterThan(prop, constant),
                MatchOperator.LesserThan => Expression.LessThan(prop, constant),
                _ => throw new NotSupportedException($"Operator {dto.Operator} not supported")
            };
        }
    }
}
