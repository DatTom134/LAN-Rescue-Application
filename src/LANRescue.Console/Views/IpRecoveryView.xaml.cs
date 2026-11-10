using System.Windows;
using System.Windows.Controls;

namespace LANRescue.ConsoleApp.Views;

public partial class IpRecoveryView : UserControl
{
    private bool _isCompact;
    public IpRecoveryView() => InitializeComponent();

    private void RecoveryCardsGrid_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var compact = e.NewSize.Width < 920;
        if (compact == _isCompact) return;
        _isCompact = compact;
        if (compact)
        {
            RecoveryCardsGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            RecoveryCardsGrid.ColumnDefinitions[1].Width = new GridLength(0, GridUnitType.Pixel);
            RecoveryCardsGrid.ColumnDefinitions[2].Width = new GridLength(0, GridUnitType.Pixel);
            RecoveryCardsGrid.RowDefinitions[0].Height = GridLength.Auto;
            RecoveryCardsGrid.RowDefinitions[1].Height = GridLength.Auto;
            Grid.SetRow(RecoveryConfigCard, 1);
            Grid.SetColumn(RecoveryConfigCard, 0);
            RecoveryTargetCard.Margin = new Thickness(0, 0, 0, 12);
            RecoveryConfigCard.Margin = new Thickness(0);
        }
        else
        {
            RecoveryCardsGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            RecoveryCardsGrid.ColumnDefinitions[1].Width = new GridLength(16, GridUnitType.Pixel);
            RecoveryCardsGrid.ColumnDefinitions[2].Width = new GridLength(1, GridUnitType.Star);
            RecoveryCardsGrid.RowDefinitions[0].Height = GridLength.Auto;
            RecoveryCardsGrid.RowDefinitions[1].Height = new GridLength(0, GridUnitType.Pixel);
            Grid.SetRow(RecoveryConfigCard, 0);
            Grid.SetColumn(RecoveryConfigCard, 2);
            RecoveryTargetCard.Margin = new Thickness(0, 0, 0, 16);
            RecoveryConfigCard.Margin = new Thickness(0, 0, 0, 16);
        }
    }
}
