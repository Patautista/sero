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

    public static class SpecificationExpressionFactory
    {
        public static Expression<Func<T, bool>> FromJson<T, P>(string json)
        {
            var spec = JsonSerializer.Deserialize<PropertySpecificationDto<P>>(json);
            return ToExpression<T>(spec.ToGeneric());
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
                UntypedPropertySpecificationDto p => BuildPropertyExpression(param, p),
                AndSpecificationDto a => Expression.AndAlso(BuildBody(param, a.Left), BuildBody(param, a.Right)),
                OrSpecificationDto o => Expression.OrElse(BuildBody(param, o.Left), BuildBody(param, o.Right)),
                NotSpecificationDto n => Expression.Not(BuildBody(param, n.Inner)),
                _ => throw new NotSupportedException($"Unsupported DTO: {dto.GetType().Name}")
            };
        }

        private static Expression BuildPropertyExpression(ParameterExpression param, UntypedPropertySpecificationDto dto)
        {
            // Navigate nested property path
            Expression prop = param;
            foreach (var part in dto.PropertyPath.Split('.'))
                prop = Expression.Property(prop, part);

            /*
                 ParameterExpression parameter = Expression.Parameter(typeof(Product), "p");
                MemberExpression property = Expression.Property(parameter, "Price");
                ConstantExpression value = Expression.Constant(100m, typeof(decimal));
                BinaryExpression greaterThan = Expression.GreaterThan(property, value);
                Expression<Func<Product, bool>> predicate = Expression.Lambda<Func<Product, bool>>(greaterThan, parameter);
             */

            // Futuro: Passar tipo do JsonElement aqui
            var constant = Expression.Constant(dto.Value);

            return dto.Operator switch
            {
                Operator.Equals => Expression.Equal(prop, constant),
                Operator.Contains => Expression.Call(prop, nameof(string.Contains), Type.EmptyTypes, constant),
                _ => throw new NotSupportedException($"Operator {dto.Operator} not supported")
            };
        }
    }

    [JsonDerivedType(typeof(AndSpecificationDto), "And")]
    [JsonDerivedType(typeof(OrSpecificationDto), "Or")]
    [JsonDerivedType(typeof(NotSpecificationDto), "Not")]
    [JsonDerivedType(typeof(UntypedPropertySpecificationDto), "Property")]
    public abstract record SpecificationDto
    {
        public AndSpecificationDto And(SpecificationDto right) => new(this, right);
        public OrSpecificationDto Or(SpecificationDto right) => new(this, right);
        public NotSpecificationDto Not() => new(this);
    }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum Operator
    {
        Equals,
        Contains,
        GreaterThan,
        GreaterOrEqual,
        LesserThan,
        LesserOrEqual
    }

    // Used only for serialization
    public record UntypedPropertySpecificationDto(string PropertyPath, Operator Operator, object? Value) : SpecificationDto
    {
        public string ToJson() => JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true});
    }

    // Used only for de-serialization
    public record PropertySpecificationDto<T>(string PropertyPath, Operator Operator, T Value){
        public AndSpecificationDto And(SpecificationDto right) => new(this.ToGeneric(), right);
        public OrSpecificationDto Or(SpecificationDto right) => new(this.ToGeneric(), right);
        public NotSpecificationDto Not() => new(this.ToGeneric());
        public string ToJson() => ToGeneric().ToJson();
        public UntypedPropertySpecificationDto ToGeneric()
        {
            return new UntypedPropertySpecificationDto(PropertyPath, Operator, Value);
        }
        public (string sql, GenericDbParameter[] parameters) ToSQL()
        {
            var sqlBuilder = new StringBuilder();
            var parameters = new List<GenericDbParameter>();

            // This is a placeholder for the actual parameter name.
            // It's good practice to generate unique names to avoid collisions.
            var paramName = $"@p_{parameters.Count}";

            // The PropertyPath needs to be sanitized to prevent SQL injection.
            // For simplicity, this example assumes PropertyPath is a safe column name.
            // In a real-world scenario, you'd need a robust way to map PropertyPath to a valid column name.
            string sanitizedPath = PropertyPath;

            sqlBuilder.Append($"{sanitizedPath} ");

            object? value = Value;

            switch (Operator)
            {
                case Operator.Equals:
                    sqlBuilder.Append($"= {paramName}");
                    break;
                case Operator.Contains:
                    // Note: The '%' is part of the value, not the SQL string.
                    // This is important for proper parameterization.
                    sqlBuilder.Append($"LIKE {paramName}");
                    value = (T)(object)$"%{Value}%";
                    break;
                case Operator.GreaterThan:
                    sqlBuilder.Append($"> {paramName}");
                    break;
                case Operator.GreaterOrEqual:
                    sqlBuilder.Append($">= {paramName}");
                    break;
                case Operator.LesserThan:
                    sqlBuilder.Append($"< {paramName}");
                    break;
                case Operator.LesserOrEqual:
                    sqlBuilder.Append($"<= {paramName}");
                    break;
                default:
                    throw new ArgumentException("Unsupported MatchOperator", nameof(Operator));
            }

            // Add the parameter. The DbParameter class is abstract, so you'd need a specific implementation like SqliteParameter or SqlParameter.
            // For a generic solution, using DbParameter is fine, but you'll need to create the concrete type in the calling code.
            parameters.Add(new GenericDbParameter(paramName, value));

            return (sqlBuilder.ToString(), parameters.ToArray());
        }
    }
    public class GenericDbParameter
    {
        public string Name { get; set; }
        public object? Value { get; set; }
        public GenericDbParameter(string Name, object? Value)
        {
            
        }
    }

    public record AndSpecificationDto(SpecificationDto Left, SpecificationDto Right) : SpecificationDto;
    public record OrSpecificationDto(SpecificationDto Left, SpecificationDto Right) : SpecificationDto;
    public record NotSpecificationDto(SpecificationDto Inner) : SpecificationDto;


}
