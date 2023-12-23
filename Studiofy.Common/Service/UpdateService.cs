using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Management.Deployment;

namespace Studiofy.Common.Service
{
    public class UpdateService
    {
        private Enums.Version m_Version { get; set; }

        private XamlRoot m_XamlRoot { get; set; }

        public UpdateService(Enums.Version version, XamlRoot xamlRoot)
        {
            m_Version = version;
            m_XamlRoot = xamlRoot;
        }

        public async void CheckForUpdates(Windows.ApplicationModel.PackageVersion appVersion)
        {
            GitHubClient GitClient = new(new ProductHeaderValue("Studiofy-IDE"));

            string currentVersion = appVersion.Major + "."
                                  + appVersion.Minor + "."
                                  + appVersion.Build + "."
                                  + appVersion.Revision;

            if (m_Version == Enums.Version.Canary)
            {
                currentVersion += "-Canary";
            }
            else if (m_Version == Enums.Version.Beta)
            {
                currentVersion += "-Beta";
            }
            else if (m_Version == Enums.Version.Official)
            {
                // currentVersion += string.Empty;
            }

            IReadOnlyList<Release> Releases = await GitClient.Repository.Release.GetAll("Studiofy", "Studiofy-IDE");

            Grid mainGrid = new()
            {
                Width = 400
            };

            // Create StackPanel
            StackPanel stackPanel = new()
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Create ProgressRing
            ProgressRing progressRing = new()
            {
                Margin = new Thickness(10, 0, 10, 0),
                XamlRoot = stackPanel.XamlRoot
            };

            // Create TextBlock
            TextBlock statusTextBlock = new()
            {
                Name = "Status",
                Text = "Searching Studiofy IDE Releases",
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                HorizontalTextAlignment = TextAlignment.Center
            };

            stackPanel.Children.Add(progressRing);
            stackPanel.Children.Add(statusTextBlock);

            mainGrid.Children.Add(stackPanel);

            ContentDialog checkUpdatesDialog = new()
            {
                Title = "Checking for Updates",
                Content = mainGrid,
                XamlRoot = m_XamlRoot
            };

            _ = checkUpdatesDialog.ShowAsync();

            progressRing.IsIndeterminate = false;
            progressRing.IsActive = false;
            await Task.Delay(TimeSpan.FromMilliseconds(100));
            progressRing.IsActive = true;
            progressRing.IsIndeterminate = true;

            await Task.Delay(2500);

            if (Releases.Count != 0)
            {
                checkUpdatesDialog.Hide();

                Release latestRelease = Releases[0];

                bool isVersionPreRelease = latestRelease.Prerelease;

                string[] versionNumbers = latestRelease.TagName.Replace("v", string.Empty).
                    Replace("-Canary", string.Empty).
                    Split('.');

                Windows.ApplicationModel.PackageVersion newVersion = new();

                newVersion.Major = ushort.Parse(versionNumbers[0]);
                newVersion.Minor = ushort.Parse(versionNumbers[1]);
                newVersion.Build = ushort.Parse(versionNumbers[2]);
                newVersion.Revision = ushort.Parse(versionNumbers[3]);

                if (appVersion != newVersion)
                {
                    if (appVersion.Major < newVersion.Major &&
                        appVersion.Minor < newVersion.Minor &&
                        appVersion.Build < newVersion.Build &&
                        appVersion.Revision < newVersion.Revision ||
                        appVersion.Revision > newVersion.Revision)
                    {
                        if (isVersionPreRelease)
                        {
                            ContentDialog preReleaseWarningDialog = new()
                            {
                                Title = "Warning",
                                Content = $"{latestRelease.Name.Replace("-Canary", " for Canary Channel")} is marked as Pre-Release.\nYou can skip this update if you want to use only the beta or official versions.\n\nIf you want to use this Pre-Release update, click Continue.",
                                PrimaryButtonText = "Continue",
                                SecondaryButtonText = "Skip",
                                DefaultButton = ContentDialogButton.Secondary,
                                XamlRoot = m_XamlRoot
                            };

                            ContentDialogResult warningDialogResult = await preReleaseWarningDialog.ShowAsync();

                            if (warningDialogResult == ContentDialogResult.Primary)
                            {
                                IReadOnlyList<ReleaseAsset> releaseAssets = latestRelease.Assets;
                                ReleaseAsset msix_Asset = null;

                                foreach (ReleaseAsset releaseAsset in releaseAssets)
                                {
                                    if (releaseAsset.Name.EndsWith(".msix"))
                                    {
                                        msix_Asset = releaseAsset;
                                        break;
                                    }
                                }

                                if (msix_Asset != null)
                                {
                                    using HttpClient httpClient = new();
                                    string url = msix_Asset.BrowserDownloadUrl;
                                    HttpResponseMessage response = httpClient.GetAsync(url).Result;
                                    Stream streamContent = response.Content.ReadAsStreamAsync().Result;
                                    string TempPath = Path.GetTempPath();
                                    string fileName = "SIDECanaryUpdate.msix";
                                    string filePath = Path.Combine(TempPath, fileName);

                                    bool IsFileAlreadyAvailable = File.Exists(filePath);

                                    if (IsFileAlreadyAvailable)
                                    {
                                        File.Delete(filePath);
                                    }

                                    using (FileStream fileStream = new(filePath, System.IO.FileMode.CreateNew, FileAccess.ReadWrite))
                                    {
                                        streamContent.CopyTo(fileStream);
                                    }

                                    InstallUpdate(filePath, true);
                                }
                            }
                        }
                        else
                        {
                            ContentDialog updateFoundDialog = new()
                            {
                                Title = "New Version Available!",
                                Content = $"{latestRelease.Name.Replace("-Canary", " for Canary Channel")} is Available. Would you like to Update now?\n\nNote: Application will shutdown when installing the update",
                                PrimaryButtonText = "Yes, Update Now",
                                SecondaryButtonText = "No, Maybe Later",
                                DefaultButton = ContentDialogButton.Primary,
                                XamlRoot = m_XamlRoot
                            };

                            ContentDialogResult updateFoundDialogResult = await updateFoundDialog.ShowAsync();

                            if (updateFoundDialogResult == ContentDialogResult.Primary)
                            {
                                ContentDialog downloadStatusDialog = new()
                                {
                                    Title = "Update Status",
                                    Content = "Downloading update...",
                                    CloseButtonText = "Cancel",
                                    XamlRoot = m_XamlRoot
                                };

                                await downloadStatusDialog.ShowAsync();

                                IReadOnlyList<ReleaseAsset> releaseAssets = latestRelease.Assets;
                                ReleaseAsset msix_Asset = null;

                                foreach (ReleaseAsset releaseAsset in releaseAssets)
                                {
                                    if (releaseAsset.Name.EndsWith(".msix"))
                                    {
                                        msix_Asset = releaseAsset;
                                        break;
                                    }
                                }

                                if (msix_Asset != null)
                                {
                                    using HttpClient httpClient = new();
                                    string url = msix_Asset.BrowserDownloadUrl;
                                    HttpResponseMessage response = httpClient.GetAsync(url).Result;
                                    Stream streamContent = response.Content.ReadAsStreamAsync().Result;
                                    string TempPath = Path.GetTempPath();
                                    string fileName = "SIDECanaryUpdate.msix";
                                    string filePath = Path.Combine(TempPath, fileName);

                                    bool IsFileAlreadyAvailable = File.Exists(filePath);

                                    if (IsFileAlreadyAvailable)
                                    {
                                        File.Delete(filePath);
                                    }

                                    using (FileStream fileStream = new(filePath, System.IO.FileMode.CreateNew, FileAccess.ReadWrite))
                                    {
                                        streamContent.CopyTo(fileStream);
                                    }

                                    downloadStatusDialog.Content = "Update Downloaded";

                                    InstallUpdate(filePath, true);
                                }
                            }
                        }
                    }
                    else
                    {
                        ContentDialog updateDialog = new()
                        {
                            Title = "Build above from beyond",
                            Content = $"The application's version: {appVersion.Major}.{appVersion.Minor}.{appVersion.Build}.{appVersion.Revision} is not on the official release that is listed for the end-users.\nDo not distribute this application without the owner's permission.",
                            XamlRoot = m_XamlRoot,
                            CloseButtonText = "Close",
                            DefaultButton = ContentDialogButton.Close
                        };

                        await updateDialog.ShowAsync();
                    }
                }
                else
                {
                    ContentDialog updateNotFoundDialog = new()
                    {
                        Title = "Studiofy IDE (Canary) is Up-to-Date",
                        Content = $"version {appVersion.Major}.{appVersion.Minor}.{appVersion.Build}.{appVersion.Revision} is the Latest Version",
                        CloseButtonText = "OK",
                        DefaultButton = ContentDialogButton.Close,
                        XamlRoot = m_XamlRoot
                    };
                    _ = await updateNotFoundDialog.ShowAsync();
                }
            }
        }

        public async void InstallUpdate(string filePath)
        {
            try
            {
                PackageManager packman = new();

                DeploymentResult depRes = await packman.AddPackageAsync(new Uri(filePath), null, DeploymentOptions.ForceTargetApplicationShutdown);

                DeploymentResult curPack = await packman.RemovePackageAsync(Windows.ApplicationModel.Package.Current.Id.FullName);
            }
            catch (Exception ex)
            {
                ContentDialog exceptionDialog = new()
                {
                    Title = ex.Source,
                    Content = $"{ex.Message}",
                    DefaultButton = ContentDialogButton.Close,
                    CloseButtonText = "Close",
                    XamlRoot = m_XamlRoot
                };

                await exceptionDialog.ShowAsync();
            }
        }

        public async void InstallUpdate(string filePath, bool restartAfterUpdate)
        {
            try
            {
                PackageManager packman = new();

                DeploymentResult depRes = await packman.AddPackageAsync(new Uri(filePath), null, DeploymentOptions.ForceTargetApplicationShutdown);

                DeploymentResult curPack = await packman.RemovePackageAsync(Windows.ApplicationModel.Package.Current.Id.FullName);

                if (restartAfterUpdate)
                {
                    AppRestartFailureReason requestResult = await CoreApplication.RequestRestartAsync(string.Empty);
                    if (requestResult == AppRestartFailureReason.RestartPending ||
                        requestResult == AppRestartFailureReason.NotInForeground ||
                        requestResult == AppRestartFailureReason.Other)
                    {
                        ContentDialog resultDialog = new()
                        {
                            Title = "Failed to Restart",
                            Content = requestResult,
                            XamlRoot = m_XamlRoot,
                            CloseButtonText = "Close",
                            DefaultButton = ContentDialogButton.Close
                        };

                        await resultDialog.ShowAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                ContentDialog exceptionDialog = new()
                {
                    Title = ex.Source,
                    Content = $"{ex.Message}",
                    DefaultButton = ContentDialogButton.Close,
                    CloseButtonText = "Close",
                    XamlRoot = m_XamlRoot
                };

                await exceptionDialog.ShowAsync();
            }
        }
    }
}
