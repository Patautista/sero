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
    using System.Text.Json.Serialization;

    using System.Linq.Expressions;

    public static class SpecificationExpressionFactory
    {
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
                _ => throw new NotSupportedException($"Unsupported DTO: {dto.GetType().Name}")
            };
        }

        private static Expression BuildPropertyExpression(ParameterExpression param, PropertySpecificationDto dto)
        {
            // Navigate nested property path
            Expression prop = param;
            foreach (var part in dto.PropertyPath.Split('.'))
                prop = Expression.Property(prop, part);

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
    [JsonDerivedType(typeof(PropertySpecificationDto), "Property")]
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
        Contains
    }

    public record PropertySpecificationDto(string PropertyPath, Operator Operator, object? Value) : SpecificationDto
    {

    }

    public record AndSpecificationDto(SpecificationDto Left, SpecificationDto Right) : SpecificationDto;
    public record OrSpecificationDto(SpecificationDto Left, SpecificationDto Right) : SpecificationDto;
    public record NotSpecificationDto(SpecificationDto Inner) : SpecificationDto;


}
