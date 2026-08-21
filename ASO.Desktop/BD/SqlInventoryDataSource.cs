using ASO.Desktop.Models;
using ASO.Desktop.Services;

namespace ASO.Desktop.BD;

public class SqlInventoryDataSource : SqlCrudDataSource<InventoryItem, string>, IInventoryDataSource
{
}
