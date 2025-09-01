using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;

    public abstract class Specification<T>
    {
        public abstract Expression<Func<T, bool>> ToExpression();

        public bool IsSatisfiedBy(T entity) => ToExpression().Compile()(entity);

        public Specification<T> And(Specification<T> other) => new AndSpecification<T>(this, other);
        public Specification<T> Or(Specification<T> other) => new OrSpecification<T>(this, other);
        public Specification<T> Not() => new NotSpecification<T>(this);
    }

    public class AndSpecification<T> : Specification<T>
    {
        private readonly Specification<T> _left;
        private readonly Specification<T> _right;
        public AndSpecification(Specification<T> left, Specification<T> right) => (_left, _right) = (left, right);

        public override Expression<Func<T, bool>> ToExpression()
        {
            var leftExpr = _left.ToExpression();
            var rightExpr = _right.ToExpression();
            var param = Expression.Parameter(typeof(T));
            var body = Expression.AndAlso(
                Expression.Invoke(leftExpr, param),
                Expression.Invoke(rightExpr, param)
            );
            return Expression.Lambda<Func<T, bool>>(body, param);
        }
    }
    public class OrSpecification<T> : Specification<T>
    {
        private readonly Specification<T> _left;
        private readonly Specification<T> _right;
        public OrSpecification(Specification<T> left, Specification<T> right) => (_left, _right) = (left, right);

        public override Expression<Func<T, bool>> ToExpression()
        {
            var leftExpr = _left.ToExpression();
            var rightExpr = _right.ToExpression();
            var param = Expression.Parameter(typeof(T));
            var body = Expression.Or(
                Expression.Invoke(leftExpr, param),
                Expression.Invoke(rightExpr, param)
            );
            return Expression.Lambda<Func<T, bool>>(body, param);
        }
    }

    public class NotSpecification<T> : Specification<T>
    {
        private readonly Specification<T> _ex;
        public NotSpecification(Specification<T> ex) => _ex = ex;

        public override Expression<Func<T, bool>> ToExpression()
        {
            var expr = _ex.ToExpression();
            var param = Expression.Parameter(typeof(T));
            var body = Expression.Not(
                Expression.Invoke(expr, param)            );
            return Expression.Lambda<Func<T, bool>>(body, param);
        }
    }

    // Similarly define OrSpecification<T> and NotSpecification<T>

}
