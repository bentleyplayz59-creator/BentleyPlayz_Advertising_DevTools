namespace BentleyPlayz.Advertising;

/// <summary>
/// Public API used by companion mods such as Bentley's Advertising Dev Tools.
/// Keep this interface in sync with the copy in the Dev Tools project.
/// </summary>
public interface IBentleysAdvertisingApi
{
    IReadOnlyList<string> GetAdvertiserIds();
    string GetAdvertiserName(string advertiserId);
    int GetPopupCount(string advertiserId);
    int GetLetterCount(string advertiserId);
    string? ForcePopup(string advertiserId);
    string? ForceLetter(string advertiserId, bool deliverNow);
    void ClearAdFree();
    string GetStatusText();
}

/// <summary>
/// Explicit API adapter. Returning this interface implementation from GetApi keeps the
/// companion mod contract separate from the main mod's internal implementation.
/// </summary>
public sealed class BentleysAdvertisingApi : IBentleysAdvertisingApi
{
    private readonly ModEntry Mod;

    public BentleysAdvertisingApi(ModEntry mod)
    {
        this.Mod = mod;
    }

    public IReadOnlyList<string> GetAdvertiserIds() => this.Mod.GetAdvertiserIds();
    public string GetAdvertiserName(string advertiserId) => this.Mod.GetAdvertiserName(advertiserId);
    public int GetPopupCount(string advertiserId) => this.Mod.GetPopupCount(advertiserId);
    public int GetLetterCount(string advertiserId) => this.Mod.GetLetterCount(advertiserId);
    public string? ForcePopup(string advertiserId) => this.Mod.ForcePopup(advertiserId);
    public string? ForceLetter(string advertiserId, bool deliverNow) => this.Mod.ForceLetter(advertiserId, deliverNow);
    public void ClearAdFree() => this.Mod.ClearAdFree();
    public string GetStatusText() => this.Mod.GetStatusText();
}
