using System.Windows.Controls;
using ASO.Desktop.ViewModels;

namespace ASO.Desktop.Views;

public partial class InventoryView : UserControl
{
    public InventoryView()
    {
        InitializeComponent();
        DataContext = new InventoryViewModel();
    }
}
