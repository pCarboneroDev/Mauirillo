using Cliente.ViewModels;

namespace Cliente.Views;

public partial class HomeView : ContentPage
{
    HomeVM vm;
    public HomeView()
	{
		InitializeComponent();
	}

    //private void ContentPage_Appearing(object sender, EventArgs e)
    //{
    //    //try
    //    //{
    //    //    vm = new HomeVM();
    //    //    vm.reconectar();

    //    //}
    //    //catch (Exception ex) 
    //    //{
    //    //    Application.Current.MainPage.DisplayAlert("",ex.ToString(), "Aceptar");
    //    //}

    //}

    private bool _isActive = false;

    private async void ContentPage_Appearing(object sender, EventArgs e)
    {
        if (_isActive) return;
        _isActive = true;

        try
        {
            // Asegurar que el BindingContext es HomeVM
            vm = BindingContext as HomeVM;
            if (vm != null)
            {
                await vm.reconectar(); // Llamada segura
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", ex.ToString(), "Aceptar");
        }
    }
}