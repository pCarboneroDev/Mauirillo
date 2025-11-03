using Cliente.ViewModels;
using CommunityToolkit.Maui.Core.Views;

namespace Cliente.Views;

public partial class PintorView : ContentPage
{
    private PartidaVM ViewModel => BindingContext as PartidaVM;
    public PintorView()
	{
		InitializeComponent();
	}

    private void DrawingView_DrawingLineCompleted(object sender, CommunityToolkit.Maui.Core.DrawingLineCompletedEventArgs e)
    {
        if (ViewModel == null || e.LastDrawingLine.Points == null || e.LastDrawingLine.Points.Count == 0)
            return;

        var lines = new DrawingLine();
        lines.Points = e.LastDrawingLine.Points;
        //ViewModel.Pizarra.Add(lines);
        ViewModel.OnDrawingChanged();
    }
}