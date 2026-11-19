namespace BentleyPlayz.AdvertisingDevTools;

/// <summary>
/// Contract provided by Bentley's Advertising 1.0.1. Keep method names and signatures
/// synchronized with the public API in the main mod's ModApi.cs.
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
