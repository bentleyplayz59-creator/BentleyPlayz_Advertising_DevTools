using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;

namespace BentleyPlayz.AdvertisingDevTools;

/// <summary>
/// The developer menu: pick an advertiser (or Random) from the list, then press a button to force its popup or letter.
/// Built as a real vanilla menu so touch input and UI scaling behave like the rest of the game, and sized from the
/// screen and font so nothing clips on a short phone screen (the advertiser list scrolls).
/// </summary>
internal sealed class DevMenu : IClickableMenu
{
    private const int Side = 32;
    private const int FeedbackMs = 4000;
    private const int FeedbackRowHeight = 52;

    private static readonly string[] ButtonLabels =
    {
        "Show ad popup",
        "Letter: mailbox now",
        "Letter: tomorrow",
        "Clear ad-free"
    };

    private readonly IBentleysAdvertisingApi Api;
    private readonly List<string> Ids = new();
    private readonly List<string> Names = new();
    private readonly List<int> PopupCounts = new();
    private readonly List<int> LetterCounts = new();

    private int RowHeight;
    private int RowGap;
    private int HeaderHeight;
    private int ButtonHeight;
    private const int ButtonGap = 8;
    private bool Compact;
    private int Columns;

    private int FirstRow;
    private int VisibleRows;

    /// <summary>Row 0 is "Random advertiser"; rows 1..N are the advertisers.</summary>
    private int SelectedRow;

    private readonly Rectangle[] Buttons = new Rectangle[ButtonLabels.Length];
    private ClickableTextureComponent? UpArrow;
    private ClickableTextureComponent? DownArrow;

    private string? Feedback;
    private bool FeedbackIsError;
    private int FeedbackTimerMs;

    private int DragStartY = int.MinValue;
    private int DragStartFirstRow;

    private int RowCount => this.Ids.Count + 1;

    public DevMenu(IBentleysAdvertisingApi api)
        : base(0, 0, 0, 0, true)
    {
        this.Api = api;

        foreach (string id in api.GetAdvertiserIds())
        {
            this.Ids.Add(id);
            this.Names.Add(api.GetAdvertiserName(id));
            this.PopupCounts.Add(api.GetPopupCount(id));
            this.LetterCounts.Add(api.GetLetterCount(id));
        }

        this.Layout();
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
    {
        base.gameWindowSizeChanged(oldBounds, newBounds);
        this.Layout();
    }

    private void Layout()
    {
        int viewWidth = Game1.uiViewport.Width;
        int viewHeight = Game1.uiViewport.Height;

        this.Compact = viewHeight < 700;
        this.RowHeight = Game1.smallFont.LineSpacing + 22;
        this.RowGap = this.Compact ? 4 : 6;
        this.HeaderHeight = this.Compact ? 104 : 124;
        this.ButtonHeight = this.Compact ? 46 : 56;

        this.width = Math.Min(860, viewWidth - 32);
        int inner = this.width - Side * 2;
        this.Columns = inner >= 640 ? 4 : 2;
        int buttonRows = (ButtonLabels.Length + this.Columns - 1) / this.Columns;
        int buttonsBlock = buttonRows * this.ButtonHeight + (buttonRows - 1) * ButtonGap;
        int footerHeight = FeedbackRowHeight + buttonsBlock + 16;

        int perRow = this.RowHeight + this.RowGap;
        int maxHeight = viewHeight - 16;
        int fit = Math.Max(1, (maxHeight - this.HeaderHeight - footerHeight) / perRow);
        this.VisibleRows = Math.Max(1, Math.Min(this.RowCount, fit));
        this.height = this.HeaderHeight + this.VisibleRows * perRow + footerHeight;

        this.xPositionOnScreen = (viewWidth - this.width) / 2;
        this.yPositionOnScreen = Math.Max(8, (viewHeight - this.height) / 2);
        this.FirstRow = Math.Clamp(this.FirstRow, 0, Math.Max(0, this.RowCount - this.VisibleRows));

        this.upperRightCloseButton = new ClickableTextureComponent(
            "exit",
            new Rectangle(this.xPositionOnScreen + this.width - 36, this.yPositionOnScreen - 8, 48, 48),
            null,
            null,
            Game1.mouseCursors,
            new Rectangle(337, 494, 12, 12),
            4f);

        int footerTop = this.yPositionOnScreen + this.HeaderHeight + this.VisibleRows * perRow;
        int buttonsTop = footerTop + FeedbackRowHeight;
        int buttonWidth = (inner - (this.Columns - 1) * ButtonGap) / this.Columns;
        for (int i = 0; i < ButtonLabels.Length; i++)
        {
            int column = i % this.Columns;
            int row = i / this.Columns;
            this.Buttons[i] = new Rectangle(
                this.xPositionOnScreen + Side + column * (buttonWidth + ButtonGap),
                buttonsTop + row * (this.ButtonHeight + ButtonGap),
                buttonWidth,
                this.ButtonHeight);
        }

        if (this.RowCount > this.VisibleRows)
        {
            int right = this.xPositionOnScreen + this.width - Side;
            int arrowY = footerTop + (FeedbackRowHeight - 48) / 2;
            this.DownArrow = new ClickableTextureComponent(
                new Rectangle(right - 44, arrowY, 44, 48),
                Game1.mouseCursors,
                new Rectangle(421, 472, 11, 12),
                4f);
            this.UpArrow = new ClickableTextureComponent(
                new Rectangle(right - 96, arrowY, 44, 48),
                Game1.mouseCursors,
                new Rectangle(421, 459, 11, 12),
                4f);
        }
        else
        {
            this.UpArrow = null;
            this.DownArrow = null;
        }
    }

    private Rectangle GetRowBounds(int visibleIndex) =>
        new(
            this.xPositionOnScreen + Side,
            this.yPositionOnScreen + this.HeaderHeight + visibleIndex * (this.RowHeight + this.RowGap),
            this.width - Side * 2,
            this.RowHeight);

    private Rectangle GetListArea() =>
        new(
            this.xPositionOnScreen + Side,
            this.yPositionOnScreen + this.HeaderHeight,
            this.width - Side * 2,
            this.VisibleRows * (this.RowHeight + this.RowGap));

    private void Say(string text, bool isError)
    {
        this.Feedback = text;
        this.FeedbackIsError = isError;
        this.FeedbackTimerMs = FeedbackMs;
    }

    /*********
    ** Actions
    *********/
    private void Press(int button)
    {
        try
        {
            if (button == 3)
            {
                this.Api.ClearAdFree();
                Game1.playSound("bigSelect");
                this.Say("Ad-free time cleared. Ads are active again.", false);
                return;
            }

            if (this.Ids.Count == 0)
            {
                Game1.playSound("cancel");
                this.Say("No advertisers were found.", true);
                return;
            }

            int index = this.SelectedRow <= 0 ? Game1.random.Next(this.Ids.Count) : this.SelectedRow - 1;
            string id = this.Ids[index];
            string name = this.Names[index];

            string? error = button switch
            {
                0 => this.Api.ForcePopup(id),
                1 => this.Api.ForceLetter(id, true),
                _ => this.Api.ForceLetter(id, false)
            };

            if (error is not null)
            {
                Game1.playSound("cancel");
                this.Say(error, true);
                return;
            }

            Game1.playSound("bigSelect");
            if (button == 0)
            {
                // The popup is drawn on the HUD, which is hidden behind menus, so close this menu to show it.
                this.exitThisMenu(false);
                return;
            }

            this.Say(
                button == 1 ? $"A letter from {name} is in your mailbox." : $"A letter from {name} arrives tomorrow.",
                false);
        }
        catch (Exception ex)
        {
            Game1.playSound("cancel");
            this.Say($"Dev Tools error: {ex.Message}", true);
        }
    }

    /*********
    ** Input
    *********/
    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        // The base class handles the upper-right close button.
        base.receiveLeftClick(x, y, playSound);

        if (this.UpArrow?.containsPoint(x, y) == true)
        {
            this.Scroll(-1);
            return;
        }

        if (this.DownArrow?.containsPoint(x, y) == true)
        {
            this.Scroll(1);
            return;
        }

        for (int i = 0; i < this.Buttons.Length; i++)
        {
            if (this.Buttons[i].Contains(x, y))
            {
                this.Press(i);
                return;
            }
        }

        if (this.GetListArea().Contains(x, y))
        {
            // Remember where the touch started so dragging the list scrolls it.
            this.DragStartY = y;
            this.DragStartFirstRow = this.FirstRow;
        }

        for (int i = 0; i < this.VisibleRows; i++)
        {
            int row = this.FirstRow + i;
            if (row >= this.RowCount)
                break;

            if (this.GetRowBounds(i).Contains(x, y))
            {
                if (this.SelectedRow != row)
                    Game1.playSound("smallSelect");
                this.SelectedRow = row;
                this.Feedback = null;
                return;
            }
        }
    }

    public override void leftClickHeld(int x, int y)
    {
        base.leftClickHeld(x, y);

        if (this.DragStartY == int.MinValue || this.RowCount <= this.VisibleRows)
            return;

        int steps = (this.DragStartY - y) / (this.RowHeight + this.RowGap);
        int max = Math.Max(0, this.RowCount - this.VisibleRows);
        this.FirstRow = Math.Clamp(this.DragStartFirstRow + steps, 0, max);
    }

    public override void releaseLeftClick(int x, int y)
    {
        base.releaseLeftClick(x, y);
        this.DragStartY = int.MinValue;
    }

    public override void receiveScrollWheelAction(int direction)
    {
        base.receiveScrollWheelAction(direction);
        this.Scroll(direction > 0 ? -1 : 1);
    }

    private void Scroll(int delta)
    {
        int max = Math.Max(0, this.RowCount - this.VisibleRows);
        int next = Math.Clamp(this.FirstRow + delta, 0, max);
        if (next != this.FirstRow)
        {
            this.FirstRow = next;
            Game1.playSound("shwip");
        }
    }

    public override void update(GameTime time)
    {
        base.update(time);

        if (this.FeedbackTimerMs > 0)
        {
            this.FeedbackTimerMs -= (int)time.ElapsedGameTime.TotalMilliseconds;
            if (this.FeedbackTimerMs <= 0)
                this.Feedback = null;
        }
    }

    /*********
    ** Drawing
    *********/
    public override void draw(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.5f);

        IClickableMenu.drawTextureBox(
            b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
            this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height, Color.White);

        // Title: vanilla pixel font, falling back to the small font on very narrow screens.
        const string title = "Advertising Dev Tools";
        int maxTitleWidth = this.width - 140;
        if (SpriteText.getWidthOfString(title) <= maxTitleWidth)
        {
            SpriteText.drawStringHorizontallyCenteredAt(b, title, this.xPositionOnScreen + this.width / 2, this.yPositionOnScreen + 28);
        }
        else
        {
            string fitted = TrimToWidth(title, Game1.dialogueFont, maxTitleWidth);
            Vector2 size = Game1.dialogueFont.MeasureString(fitted);
            Utility.drawTextWithShadow(b, fitted, Game1.dialogueFont,
                new Vector2(this.xPositionOnScreen + (this.width - size.X) / 2f, this.yPositionOnScreen + 28), Game1.textColor);
        }

        int textWidth = this.width - Side * 2;
        int statusY = this.yPositionOnScreen + (this.Compact ? 70 : 84);
        string status;
        try
        {
            status = this.Api.GetStatusText();
        }
        catch
        {
            status = "";
        }

        if (status.Length > 0)
        {
            Utility.drawTextWithShadow(b, TrimToWidth(status, Game1.smallFont, textWidth), Game1.smallFont,
                new Vector2(this.xPositionOnScreen + Side, statusY), new Color(60, 110, 40));
        }

        int mouseX = Game1.getMouseX(true);
        int mouseY = Game1.getMouseY(true);

        // Advertiser rows (row 0 is Random).
        for (int i = 0; i < this.VisibleRows; i++)
        {
            int rowIndex = this.FirstRow + i;
            if (rowIndex >= this.RowCount)
                break;

            Rectangle row = this.GetRowBounds(i);
            bool selected = rowIndex == this.SelectedRow;
            bool hovered = row.Contains(mouseX, mouseY);
            Color tint = selected ? Color.LightGreen : hovered ? Color.Wheat : Color.White;

            IClickableMenu.drawTextureBox(
                b, Game1.mouseCursors, new Rectangle(432, 439, 9, 9),
                row.X, row.Y, row.Width, row.Height, tint, 4f, false);
            if (selected)
                DrawOutline(b, row, 4, Color.DarkGreen);

            string left;
            string right;
            if (rowIndex == 0)
            {
                left = "Random advertiser";
                right = "any";
            }
            else
            {
                left = this.Names[rowIndex - 1];
                right = $"{this.PopupCounts[rowIndex - 1]} ads, {this.LetterCounts[rowIndex - 1]} letters";
            }

            int innerLeft = row.X + 20;
            int innerRight = row.Right - 20;
            float rightWidth = Game1.smallFont.MeasureString(right).X;
            float textY = row.Y + (row.Height - Game1.smallFont.LineSpacing) / 2f;

            // Keep the counts on narrow screens only if there is room for the name too.
            bool showRight = rightWidth < (innerRight - innerLeft) * 0.5f;
            if (showRight)
            {
                Utility.drawTextWithShadow(b, right, Game1.smallFont,
                    new Vector2(innerRight - rightWidth, textY), Game1.textColor * 0.85f);
            }

            int nameWidth = Math.Max(40, innerRight - innerLeft - (showRight ? (int)rightWidth + 16 : 0));
            Utility.drawTextWithShadow(b, TrimToWidth(left, Game1.smallFont, nameWidth), Game1.smallFont,
                new Vector2(innerLeft, textY), Game1.textColor);
        }

        // Feedback line (left) and scroll arrows (right).
        int footerTop = this.yPositionOnScreen + this.HeaderHeight + this.VisibleRows * (this.RowHeight + this.RowGap);
        string? footerText = this.Feedback;
        Color footerColor = this.FeedbackIsError ? Color.Firebrick : Color.DarkGreen;
        if (footerText is null)
        {
            footerText = this.SelectedRow <= 0
                ? "Pick an advertiser, or use Random."
                : $"Selected: {this.Names[this.SelectedRow - 1]}";
            footerColor = Game1.textColor;
        }

        int footerMaxWidth = textWidth - (this.UpArrow is not null ? 112 : 0);
        Utility.drawTextWithShadow(b, TrimToWidth(footerText, Game1.smallFont, footerMaxWidth), Game1.smallFont,
            new Vector2(this.xPositionOnScreen + Side, footerTop + (FeedbackRowHeight - Game1.smallFont.LineSpacing) / 2f), footerColor);

        this.UpArrow?.draw(b, this.FirstRow > 0 ? Color.White : Color.Gray, 0.88f);
        this.DownArrow?.draw(b, this.FirstRow < this.RowCount - this.VisibleRows ? Color.White : Color.Gray, 0.88f);

        // Action buttons.
        for (int i = 0; i < this.Buttons.Length; i++)
        {
            Rectangle area = this.Buttons[i];
            bool hovered = area.Contains(mouseX, mouseY);
            Color tint = i == 3
                ? (hovered ? Color.LightSalmon : Color.Salmon)
                : (hovered ? Color.Wheat : Color.LightGreen);

            IClickableMenu.drawTextureBox(
                b, Game1.mouseCursors, new Rectangle(432, 439, 9, 9),
                area.X, area.Y, area.Width, area.Height, tint, 4f, false);

            string label = TrimToWidth(ButtonLabels[i], Game1.smallFont, area.Width - 20);
            Vector2 size = Game1.smallFont.MeasureString(label);
            Utility.drawTextWithShadow(b, label, Game1.smallFont,
                new Vector2(area.X + (area.Width - size.X) / 2f, area.Y + (area.Height - size.Y) / 2f),
                Game1.textColor);
        }

        this.upperRightCloseButton?.draw(b);
        this.drawMouse(b);
    }

    private static void DrawOutline(SpriteBatch b, Rectangle area, int thickness, Color color)
    {
        b.Draw(Game1.staminaRect, new Rectangle(area.X, area.Y, area.Width, thickness), color);
        b.Draw(Game1.staminaRect, new Rectangle(area.X, area.Bottom - thickness, area.Width, thickness), color);
        b.Draw(Game1.staminaRect, new Rectangle(area.X, area.Y, thickness, area.Height), color);
        b.Draw(Game1.staminaRect, new Rectangle(area.Right - thickness, area.Y, thickness, area.Height), color);
    }

    /// <summary>Shortens text with an ellipsis so it can never spill past its box.</summary>
    private static string TrimToWidth(string text, SpriteFont font, int maxWidth)
    {
        if (font.MeasureString(text).X <= maxWidth)
            return text;

        string trimmed = text;
        while (trimmed.Length > 1 && font.MeasureString(trimmed + "...").X > maxWidth)
            trimmed = trimmed.Substring(0, trimmed.Length - 1);
        return trimmed.TrimEnd() + "...";
    }
}
