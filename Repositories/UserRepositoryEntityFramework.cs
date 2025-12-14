using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Repositories;
using WorldMusicJam.Contexts;

namespace WorldMusicJam.Repositories;

public class UserRepositoryEntityFramework(IConfiguration configuration) : IUserRepository
{
    private readonly DataContextEntityFramework context = new(configuration);

    public bool TryGetById<T>(int id, out T? data) where T : class
    {
        DbSet<T> dataSet = context.Set<T>();
        data = dataSet.Find(id);
        
        if(data is null)
            return false;

        return true;
    }

    public T? Get<T>(Expression<Func<T, bool>>? predicate) where T : class
    {
        var dbSet = context.Set<T>();
        if(predicate == null)
            throw new ArgumentNullException(nameof(predicate));
        
        return dbSet.FirstOrDefault(predicate);
    }

    public List<T> GetAll<T>(Expression<Func<T, bool>>? predicate) where T : class
    {
        var dbSet = context.Set<T>();
        if(predicate == null)
            return dbSet.ToList();
        
        return dbSet.Where(predicate).ToList();
    }

    public bool Update<T1, T2>(int id,  Func<T2, T2?> func) where T1 : class, ICopyable<T2>
    {
        DbSet<T1> dataSet = context.Set<T1>();
        var foundData = dataSet.Find(id);
        
        if (foundData == null)
            return false;
        
        var modifiedData = func(foundData.CopyTo());
        if(modifiedData == null)
            return false;
        
        foundData.CopyFrom(modifiedData);
        return context.Update(foundData).State == EntityState.Modified;
    }

    public bool Add<T>(T? data) where T : class
    {
        if (data is null)
            return false;

        if (context.Add(data).State != EntityState.Added)
            return false;
        
        return true;
    }

    public bool Remove<T>(int id) where T : class
    {
        DbSet<T> dataSet = context.Set<T>();
        var data = dataSet.Find(id);
        
        if (data != null)
            return context.Remove(data).State == EntityState.Deleted;
        
        return false;
    }

    public bool SaveChanges() => context.SaveChanges() > 0;
}