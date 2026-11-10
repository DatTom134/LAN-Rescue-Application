using System.Windows;
using System.Windows.Controls;
using LANRescue.ConsoleApp.Controls;

namespace LANRescue.ConsoleApp.Views;

public partial class DashboardView : UserControl
{
    private bool _isCompactDetails;

    public DashboardView() => InitializeComponent();

    private void DashboardKpiGrid_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var width = e.NewSize.Width;
        DashboardKpiGrid.Columns = width < 560 ? 1 : width < 920 ? 2 : 4;
        foreach (UIElement child in DashboardKpiGrid.Children)
            if (child is FrameworkElement element)
                element.Margin = width < 560 ? new Thickness(0, 0, 0, 10) : new Thickness(6, 0, 6, 0);
    }

    private void DashboardDetailsGrid_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var compact = e.NewSize.Width < 920;
        if (compact == _isCompactDetails) return;
        _isCompactDetails = compact;
        if (compact)
        {
            DashboardDetailsGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            DashboardDetailsGrid.ColumnDefinitions[1].Width = new GridLength(0);
            DashboardDetailsGrid.ColumnDefinitions[2].Width = new GridLength(0, GridUnitType.Pixel);
            DashboardDetailsGrid.RowDefinitions.Clear();
            DashboardDetailsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            DashboardDetailsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Grid.SetRow(RecentEventsCard, 1);
            Grid.SetColumn(RecentEventsCard, 0);
            PriorityScenarioCard.Margin = new Thickness(0, 0, 0, 12);
            RecentEventsCard.Margin = new Thickness(0);
        }
        else
        {
            DashboardDetailsGrid.ColumnDefinitions[0].Width = new GridLength(1.2, GridUnitType.Star);
            DashboardDetailsGrid.ColumnDefinitions[1].Width = new GridLength(16);
            DashboardDetailsGrid.ColumnDefinitions[2].Width = new GridLength(0.8, GridUnitType.Star);
            DashboardDetailsGrid.RowDefinitions.Clear();
            DashboardDetailsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Grid.SetRow(RecentEventsCard, 0);
            Grid.SetColumn(RecentEventsCard, 2);
            PriorityScenarioCard.Margin = new Thickness(0);
            RecentEventsCard.Margin = new Thickness(0);
        }
    }
}
