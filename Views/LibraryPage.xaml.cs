using Yuzo.Services;
using Yuzo.ViewModels;

namespace Yuzo.Views;

public partial class LibraryPage : ContentPage
{
    private readonly LibraryViewModel _vm;
    private int _layoutSpan = 2;

    public LibraryPage(LibraryViewModel vm, PlaybackService playback)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
        PlaybackBarHost.Content = new PlaybackBarView(playback);
        SizeChanged += (_, _) => UpdateAlbumColumns();
        LibraryItems.SizeChanged += (_, _) => UpdateAlbumColumns();
        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(LibraryViewModel.SelectedView))
                UpdateAlbumColumns();
        };
    }

    private void UpdateAlbumColumns()
    {
        const double cardWidth = 150;
        const double itemSpacing = 12;
        var span = _vm.SelectedView == "Albums"
            ? Math.Max(1, (int)((LibraryItems.Width + itemSpacing) / (cardWidth + itemSpacing)))
            : 1;

        if (span == _layoutSpan)
            return;

        _layoutSpan = span;
        LibraryItems.ItemsLayout = new GridItemsLayout(ItemsLayoutOrientation.Vertical)
        {
            Span = span,
            HorizontalItemSpacing = itemSpacing,
            VerticalItemSpacing = 20,
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.OnAppearingAsync();
    }
}