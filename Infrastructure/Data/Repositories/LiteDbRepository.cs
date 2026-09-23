using Domain.Shared.Models;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Infrastructure.Data.Repositories
{
    /// <summary>
    /// Repository adapter over a LiteDB collection.
    /// Wraps synchronous LiteDB methods in async Tasks to maintain compatibility
    /// with the application's async service contracts.
    /// </summary>
    public class LiteDbRepository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        private readonly ILiteCollection<TEntity> _collection;

        public LiteDbRepository(ILiteCollection<TEntity> collection)
        {
            _collection = collection;
        }

        public Task<TEntity?> FindByIdAsync(int id)
        {
            return Task.FromResult(_collection.FindById(id));
        }

        public Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return Task.FromResult(_collection.FindOne(predicate));
        }

        public Task<IEnumerable<TEntity>> WhereAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return Task.FromResult(_collection.Find(predicate));
        }

        public Task<bool> AnyAsync(Expression<Func<TEntity, bool>>? predicate = null)
        {
            var result = predicate == null 
                ? _collection.Count() > 0 
                : _collection.Exists(predicate);
            return Task.FromResult(result);
        }

        public Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null)
        {
            var count = predicate == null 
                ? _collection.Count() 
                : _collection.Count(predicate);
            return Task.FromResult(count);
        }

        public void Add(TEntity entity)
        {
            _collection.Insert(entity);
        }

        public void Update(TEntity entity)
        {
            _collection.Update(entity);
        }

        public void Remove(TEntity entity)
        {
            // LiteDB requires using BsonValue for ID-based deletion
            // For now, we'll use a workaround: try to find and delete by identity
            // This is a limitation of the abstraction layer - clients should prefer
            // using deletion by ID if possible, or provide a Remove(int id) overload.
            if (entity is IIdentifiable identifiable)
            {
                _collection.Delete(identifiable.GetId());
            }
        }

        public Task<IEnumerable<TEntity>> GetAllAsync()
        {
            return Task.FromResult(_collection.FindAll());
        }
    }

    /// <summary>
    /// Marker interface to identify entities with an Id property.
    /// </summary>
    public interface IIdentifiable
    {
        int GetId();
    }
}
