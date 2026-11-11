using Yuzo.Models;

namespace Yuzo.Views;

public sealed class LibraryItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate AlbumTemplate { get; set; } = new();
    public DataTemplate ArtistTemplate { get; set; } = new();
    public DataTemplate SongTemplate { get; set; } = new();

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        => item switch
        {
            AlbumTile => AlbumTemplate,
            ArtistTile => ArtistTemplate,
            Track => SongTemplate,
            _ => throw new InvalidOperationException($"Unsupported library item type: {item.GetType().FullName}")
        };
}
