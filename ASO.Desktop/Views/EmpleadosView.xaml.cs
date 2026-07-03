using System.Windows.Controls;
using ASO.Desktop.ViewModels;

namespace ASO.Desktop.Views;

public partial class EmpleadosView : UserControl
{
    public EmpleadosView()
    {
        InitializeComponent();
        DataContext = new EmpleadosViewModel();
    }
}
