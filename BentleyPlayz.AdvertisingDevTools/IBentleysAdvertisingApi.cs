namespace BentleyPlayz.AdvertisingDevTools;

/// <summary>
/// The API exposed by Bentley's Advertising. SMAPI matches this interface to the real one by method name and
/// signature, so it must be kept in sync with ModApi.cs in the main mod. Methods that can fail return an error
/// message, or null on success.
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
