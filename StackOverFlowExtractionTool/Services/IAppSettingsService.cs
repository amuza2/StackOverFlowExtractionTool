namespace StackOverFlowExtractionTool.Services;

public interface IAppSettingsService
{
    T GetSetting<T>(string key, T defaultValue = default);
    void SetSetting<T>(string key, T value);
    bool ContainsKey(string key);
    void RemoveSetting(string key);
    void Save();
}