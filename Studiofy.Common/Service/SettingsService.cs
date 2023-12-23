using Microsoft.UI.Xaml;
using Newtonsoft.Json;
using Studiofy.Common.Models;
using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace Studiofy.Common.Service
{
    public class SettingsService
    {
        private const string SETTINGSFILE = "Settings.json";

        public static async Task<SettingsModel> ReadSettingsFileAsync(UIElement element)
        {
            try
            {
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                StorageFile settingsFile = await localFolder.GetFileAsync(SETTINGSFILE);
                return JsonConvert.DeserializeObject<SettingsModel>(await FileIO.ReadTextAsync(settingsFile));
            }
            catch (Exception)
            {
                await new SettingsService().CreateSettingsFile(element);
            }
            return null;
        }

        public static async Task UpdateSettingsFileJson(SettingsModel updatedSettings)
        {
            try
            {
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                StorageFile settingsFile = await localFolder.GetFileAsync(SETTINGSFILE);

                if (settingsFile != null)
                {
                    string jsonSettings = await FileIO.ReadTextAsync(settingsFile);
                    SettingsModel existingSettings = JsonConvert.DeserializeObject<SettingsModel>(jsonSettings);

                    existingSettings.Id = updatedSettings.Id;
                    existingSettings.IsConfidentialInfoBarEnabled = updatedSettings.IsConfidentialInfoBarEnabled;
                    existingSettings.AppTheme = updatedSettings.AppTheme;
                    existingSettings.DisplayShowWelcomePageOnStartup = updatedSettings.DisplayShowWelcomePageOnStartup;

                    string updatedJsonSettings = JsonConvert.SerializeObject(existingSettings, Newtonsoft.Json.Formatting.Indented);
                    await FileIO.WriteTextAsync(settingsFile, updatedJsonSettings);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async void CreateSettingsFileJson(UIElement element)
        {
            try
            {
                StorageFolder storageFolder = ApplicationData.Current.LocalFolder;

                StorageFile settingsFile = await storageFolder.CreateFileAsync(SETTINGSFILE, CreationCollisionOption.ReplaceExisting);

                string jsonSettings = JsonConvert.SerializeObject(new SettingsModel()
                {
                    Id = Guid.NewGuid(),
                    IsConfidentialInfoBarEnabled = true,
                    AppTheme = SettingsModel.Theme.Default
                }, Newtonsoft.Json.Formatting.Indented);

                await FileIO.WriteTextAsync(settingsFile, jsonSettings);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> CreateSettingsFile(UIElement element)
        {
            try
            {
                StorageFolder settingsFolder = ApplicationData.Current.LocalFolder;
                StorageFile settingsFile = await settingsFolder.CreateFileAsync(SETTINGSFILE);
                return await Task.FromResult(settingsFile.IsAvailable);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
