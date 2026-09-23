using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Infrastructure.Data.Repositories
{
    /// <summary>
    /// Generic repository interface for synchronous and asynchronous access to
    /// a single entity type. Implementers provide consistent CRUD semantics
    /// regardless of the underlying database provider.
    /// </summary>
    public interface IRepository<TEntity> where TEntity : class
    {
        /// <summary>
        /// Find an entity by identifier.
        /// </summary>
        Task<TEntity?> FindByIdAsync(int id);

        /// <summary>
        /// Find the first entity matching the predicate, or null if none found.
        /// </summary>
        Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate);

        /// <summary>
        /// Get all entities matching the predicate.
        /// </summary>
        Task<IEnumerable<TEntity>> WhereAsync(Expression<Func<TEntity, bool>> predicate);

        /// <summary>
        /// Check if any entity matches the predicate.
        /// </summary>
        Task<bool> AnyAsync(Expression<Func<TEntity, bool>>? predicate = null);

        /// <summary>
        /// Count entities matching the predicate.
        /// </summary>
        Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null);

        /// <summary>
        /// Add a new entity. Changes are not persisted until SaveChangesAsync is called on the store.
        /// </summary>
        void Add(TEntity entity);

        /// <summary>
        /// Remove an entity. Changes are not persisted until SaveChangesAsync is called on the store.
        /// </summary>
        void Remove(TEntity entity);

        /// <summary>
        /// Get all entities (unfiltered).
        /// </summary>
        Task<IEnumerable<TEntity>> GetAllAsync();
    }
}
