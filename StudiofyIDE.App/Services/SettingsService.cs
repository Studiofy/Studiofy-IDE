using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using WindowsCode.Studio.Models;

namespace WindowsCode.Studio.Services
{
    public class SettingsService
    {
        const string SETTINGSFILE = "Settings.json";

        public static async Task<SettingsModel> ReadSettingsFileAsync(UIElement element)
        {
            try
            {
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                StorageFile settingsFile = await localFolder.GetFileAsync(SETTINGSFILE);

                if (settingsFile.IsAvailable)
                {
                    string jsonSettings = await FileIO.ReadTextAsync(settingsFile);
                    return JsonConvert.DeserializeObject<SettingsModel>(jsonSettings);
                }
                else
                {
                    new SettingsService().CreateSettingsFileJson(element);
                    await ReadSettingsFileAsync(element);
                }
            }
            catch (Exception)
            {
                throw;
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
                    // Read existing settings from the file
                    string jsonSettings = await FileIO.ReadTextAsync(settingsFile);
                    SettingsModel existingSettings = JsonConvert.DeserializeObject<SettingsModel>(jsonSettings);

                    // Update the existing settings with the new values
                    existingSettings.Id = updatedSettings.Id;
                    existingSettings.IsConfidentialInfoBarEnabled = updatedSettings.IsConfidentialInfoBarEnabled;
                    existingSettings.AppTheme = updatedSettings.AppTheme;

                    // Serialize the updated settings and write back to the file
                    string updatedJsonSettings = JsonConvert.SerializeObject(existingSettings);
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
                });

                await FileIO.WriteTextAsync(settingsFile, jsonSettings);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
