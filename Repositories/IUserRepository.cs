using System.Linq.Expressions;
using WebApplication1.Repositories;

namespace WorldMusicJam.Repositories;

public interface IUserRepository
{
    public bool TryGetById<T>(int id, out T? data) where T : class;
    
    public T? Get<T>(Expression<Func<T, bool>>? predicate) where T : class;
    
    public List<T> GetAll<T>(Expression<Func<T, bool>>? predicate) where T : class;

    public bool Update<T1, T2>(int id, Func<T2, T2?> func) where T1 : class, ICopyable<T2>;
    
    public bool Add<T>(T? data) where T : class;

    public bool Remove<T>(int id) where T : class;

    public T[] ExecuteStoreProcedure<T>(string storedProcedure, params Tuple<string, object>[] parameters);

    public bool SaveChanges();
}