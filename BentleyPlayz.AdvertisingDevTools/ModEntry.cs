using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace BentleyPlayz.AdvertisingDevTools;

public sealed class ModEntry : Mod
{
    private const string MainModId = "BentleyPlayz.Advertising";

    private DevConfig Config = new();
    private IBentleysAdvertisingApi? Api;

    public override void Entry(IModHelper helper)
    {
        Config = helper.ReadConfig<DevConfig>();

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.Display.RenderedHud += OnRenderedHud;
        helper.Events.Input.ButtonPressed += OnButtonPressed;

        Monitor.Log($"Bentley's Advertising Dev Tools {ModManifest.Version} loaded.", LogLevel.Info);
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        Api = Helper.ModRegistry.GetApi<IBentleysAdvertisingApi>(MainModId);
        if (Api is null)
            Monitor.Log("Couldn't connect to Bentley's Advertising. Make sure it is installed and up to date (the version that includes the Dev Tools API).", LogLevel.Warn);
        else
            Monitor.Log("Connected to Bentley's Advertising.", LogLevel.Debug);
    }

    private bool CanUseButton() =>
        Config.ShowButton && Context.IsWorldReady && Game1.activeClickableMenu == null && !Game1.eventUp && Game1.displayHUD;

    /// <summary>One rectangle for both drawing and tapping, kept fully on screen.</summary>
    private Rectangle GetButtonBounds()
    {
        int size = Math.Clamp(Config.ButtonSize, 40, 96);
        int x = Math.Clamp(Config.ButtonOffsetX, 0, Math.Max(0, Game1.uiViewport.Width - size));
        int y = Math.Clamp(Config.ButtonOffsetY, 0, Math.Max(0, Game1.uiViewport.Height - size));
        return new Rectangle(x, y, size, size);
    }

    private void OpenDevMenu()
    {
        if (!Context.IsWorldReady)
            return;

        if (Api is null)
        {
            Game1.addHUDMessage(new HUDMessage("Dev Tools: Bentley's Advertising was not found or is too old.", HUDMessage.error_type));
            return;
        }

        try
        {
            Game1.playSound("bigSelect");
            Game1.activeClickableMenu = new DevMenu(Api);
        }
        catch (Exception ex)
        {
            Monitor.Log($"Couldn't open the dev menu: {ex}", LogLevel.Error);
            Game1.addHUDMessage(new HUDMessage("Dev Tools couldn't open. See the SMAPI log.", HUDMessage.error_type));
        }
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady || Game1.activeClickableMenu != null || Game1.eventUp)
            return;

        if (Config.OpenMenuKey != SButton.None && e.Button == Config.OpenMenuKey)
        {
            if (Game1.chatBox is not null && Game1.chatBox.isActive())
                return;

            Helper.Input.Suppress(e.Button);
            OpenDevMenu();
            return;
        }

        if (!CanUseButton())
            return;

        if (e.Button != SButton.MouseLeft && !e.Button.IsUseToolButton())
            return;

        // The button is drawn in UI space; accept either UI-scaled coordinate source so touch and mouse both work.
        Rectangle hit = GetButtonBounds();
        hit.Inflate(10, 10);
        Point scaled = e.Cursor.GetScaledScreenPixels().ToPoint();
        Point game = new(Game1.getMouseX(true), Game1.getMouseY(true));
        if (!hit.Contains(scaled) && !hit.Contains(game))
            return;

        Helper.Input.Suppress(e.Button);
        OpenDevMenu();
    }

    private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
    {
        if (!CanUseButton())
            return;

        SpriteBatch b = e.SpriteBatch;
        Rectangle area = GetButtonBounds();

        b.Draw(Game1.staminaRect, new Rectangle(area.X + 4, area.Y + 6, area.Width, area.Height), Color.Black * 0.35f);
        StardewValley.Menus.IClickableMenu.drawTextureBox(
            b, Game1.mouseCursors, new Rectangle(432, 439, 9, 9),
            area.X, area.Y, area.Width, area.Height, Color.MediumPurple, 4f, false);

        const string label = "DEV";
        SpriteFont font = Game1.dialogueFont;
        Vector2 size = font.MeasureString(label);
        if (size.X > area.Width - 14)
        {
            font = Game1.smallFont;
            size = font.MeasureString(label);
        }

        Utility.drawTextWithShadow(b, label, font,
            new Vector2(area.X + (area.Width - size.X) / 2f, area.Y + (area.Height - size.Y) / 2f),
            Color.White);
    }
}
