using StardewModdingAPI;

namespace BentleyPlayz.AdvertisingDevTools;

public sealed class DevConfig
{
    /// <summary>Show the small DEV button on the HUD (the only way to open the menu on a touch screen).</summary>
    public bool ShowButton { get; set; } = true;

    /// <summary>Keyboard shortcut that opens the dev menu (set to None to disable).</summary>
    public SButton OpenMenuKey { get; set; } = SButton.F8;

    /// <summary>Width and height of the DEV button in UI pixels (40-96).</summary>
    public int ButtonSize { get; set; } = 64;

    /// <summary>Distance in UI pixels from the left edge of the screen to the DEV button.</summary>
    public int ButtonOffsetX { get; set; } = 16;

    /// <summary>Distance in UI pixels from the top edge of the screen to the DEV button.</summary>
    public int ButtonOffsetY { get; set; } = 16;
}
