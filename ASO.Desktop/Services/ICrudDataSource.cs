using System.Collections.Generic;
using ASO.Desktop.Models;

namespace ASO.Desktop.Services;

/// <summary>
/// Fuente de datos con operaciones CRUD para una entidad maestra. Hoy la implementa un
/// mock en memoria; mañana la implementará un repositorio EF Core sin cambiar la UI ni
/// el ViewModel.
/// </summary>
public interface ICrudDataSource<T, TId> where T : IEntidad<TId>
{
    IEnumerable<T> GetAll();
    T? GetById(TId id);

    /// <returns>El ítem guardado, con el <c>Id</c> ya asignado.</returns>
    T Add(T item);

    void Update(T item);
    void Delete(TId id);
}
