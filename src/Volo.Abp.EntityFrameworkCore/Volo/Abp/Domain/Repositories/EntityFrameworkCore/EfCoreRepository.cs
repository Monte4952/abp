﻿using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Domain.Entities;
using Volo.Abp.EntityFrameworkCore;

namespace Volo.Abp.Domain.Repositories.EntityFrameworkCore
{
    public class EfCoreRepository<TDbContext, TEntity> : EfCoreRepository<TDbContext, TEntity, string>, IEfCoreRepository<TEntity>
        where TDbContext : AbpDbContext<TDbContext>
        where TEntity : class, IEntity<string>
    {
        public EfCoreRepository(IDbContextProvider<TDbContext> dbContextProvider)
            : base(dbContextProvider)
        {
        }
    }

    public class EfCoreRepository<TDbContext, TEntity, TPrimaryKey> : QueryableRepositoryBase<TEntity, TPrimaryKey>, IEfCoreRepository<TEntity, TPrimaryKey>
        where TDbContext : AbpDbContext<TDbContext>
        where TEntity : class, IEntity<TPrimaryKey>
    {
        public virtual DbSet<TEntity> DbSet => DbContext.Set<TEntity>();

        DbContext IEfCoreRepository<TEntity, TPrimaryKey>.DbContext => DbContext;

        protected virtual TDbContext DbContext => _dbContextProvider.GetDbContext();

        private readonly IDbContextProvider<TDbContext> _dbContextProvider;

        public EfCoreRepository(IDbContextProvider<TDbContext> dbContextProvider)
        {
            _dbContextProvider = dbContextProvider;
        }

        protected override IQueryable<TEntity> GetQueryable()
        {
            return DbSet.AsQueryable();
        }

        public override async Task<TEntity> GetAsync(TPrimaryKey id, CancellationToken cancellationToken = default(CancellationToken))
        {
            var entity = await FindAsync(id, cancellationToken);

            if (entity == null)
            {
                throw new EntityNotFoundException(typeof(TEntity), id);
            }

            return entity;
        }

        public override TEntity Find(TPrimaryKey id)
        {
            return DbSet.Find(id);
        }

        public override Task<TEntity> FindAsync(TPrimaryKey id, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DbSet.FindAsync(new object[] { id }, cancellationToken);
        }

        public override TEntity Insert(TEntity entity, bool autoSave = false)
        {
            var savedEntity = DbSet.Add(entity).Entity;

            if (autoSave)
            {
                DbContext.SaveChanges();
            }

            return savedEntity;
        }

        public override async Task<TEntity> InsertAsync(TEntity entity, bool autoSave = false, CancellationToken cancellationToken = new CancellationToken())
        {
            var savedEntity = DbSet.Add(entity).Entity;

            if (autoSave)
            {
                await DbContext.SaveChangesAsync(cancellationToken);
            }

            return savedEntity;
        }

        public override TEntity Update(TEntity entity)
        {
            //TODO: This code is got from UserStore.UpdateAsync and revised Update method based on that, but we should be sure that it's valid
            //Context.Attach(user);
            //user.ConcurrencyStamp = Guid.NewGuid().ToString();
            //Context.Update(user);
            
            DbContext.Attach(entity); //TODO: What is different for DbSet.Attach(entity)?

            if (entity is IHasConcurrencyStamp)
            {
                (entity as IHasConcurrencyStamp).ConcurrencyStamp = Guid.NewGuid().ToString(); //TODO: Use IGuidGenerator!
            }

            return DbContext.Update(entity).Entity; //TODO: or DbSet.Update(entity) ?
        }

        public override void Delete(TEntity entity)
        {
            DbSet.Remove(entity);
        }

        public override async Task DeleteAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default(CancellationToken))
        {
            var entities = await GetQueryable().Where(predicate).ToListAsync(cancellationToken);
            foreach (var entity in entities)
            {
                DbSet.Remove(entity);
            }
        }
    }
}
