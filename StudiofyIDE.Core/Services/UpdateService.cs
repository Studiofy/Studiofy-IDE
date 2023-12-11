using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Windows.Management.Deployment;

namespace WindowsCode.Core.Services
{
    public class UpdateService
    {
        public enum Version
        {
            Canary,
            Beta,
            Official
        }

        public async Task GetUpdates(UIElement content)
        {
            GitHubClient GitClient = new(new ProductHeaderValue("Studiofy-IDE"));
            string Current = Windows.ApplicationModel.Package.Current.Id.Version.Major.ToString() + "."
                        + Windows.ApplicationModel.Package.Current.Id.Version.Minor.ToString() + "."
                        + Windows.ApplicationModel.Package.Current.Id.Version.Build.ToString() + "."
                        + Windows.ApplicationModel.Package.Current.Id.Version.Revision.ToString() + "-Canary";
            try
            {
                IReadOnlyList<Release> Release = await GitClient.Repository.Release.GetAll("Studiofy", "Studiofy-IDE");
                Release Latest = Release[0];
                if (!Latest.TagName.EndsWith("v" + Current))
                {
                    ContentDialog updateDialog = new()
                    {
                        Title = "Update Available!",
                        Content = $"{Latest.Name.Replace("-Canary", " for Canary Channel")} is Available.\nWould you like to Update now?",
                        PrimaryButtonText = "Yes, Update Now",
                        SecondaryButtonText = "No, Maybe Later",
                        DefaultButton = ContentDialogButton.Primary,
                        XamlRoot = content.XamlRoot
                    };

                    ContentDialogResult dialogResult = await updateDialog.ShowAsync();

                    if (dialogResult == ContentDialogResult.Primary)
                    {
                        IReadOnlyList<ReleaseAsset> Assets = Latest.Assets;
                        ReleaseAsset msix = null;
                        ReleaseAsset pfx = null;
                        foreach (ReleaseAsset Asset in Assets)
                        {
                            if (Asset.Name.EndsWith(".msix"))
                            {
                                msix = Asset;
                                break;
                            }
                            else if (Asset.Name.EndsWith(".pfx"))
                            {
                                pfx = Asset;
                                if (pfx != null)
                                {
                                    using HttpClient httpClient = new();
                                    string url = pfx.BrowserDownloadUrl;
                                    HttpResponseMessage response = httpClient.GetAsync(url).Result;
                                    Stream streamContent = response.Content.ReadAsStreamAsync().Result;
                                    string TempPath = Path.GetTempPath();
                                    string fileName = "SIDECertificate.cer";
                                    string filePath = Path.Combine(TempPath, fileName);

                                    using (FileStream fileStream = new(filePath, System.IO.FileMode.CreateNew))
                                    {
                                        streamContent.CopyTo(fileStream);
                                    }

                                    string certFilePath = filePath;
                                    X509Certificate2 cert = new(filePath);
                                    X509Store store = new(StoreName.TrustedPublisher, StoreLocation.LocalMachine);
                                    store.Open(OpenFlags.ReadWrite);
                                    store.Add(cert);
                                }
                                else
                                {
                                    ContentDialog errorDialog = new()
                                    {
                                        Title = "Certificate Not Found",
                                        Content = "A Security Certificate for this application is not available in\n" +
                                                Path.GetTempPath(),
                                        CloseButtonText = "OK",
                                        DefaultButton = ContentDialogButton.Close,
                                        XamlRoot = content.XamlRoot
                                    };
                                    _ = await errorDialog.ShowAsync();
                                }
                                break;
                            }
                        }
                        if (msix != null)
                        {
                            using HttpClient httpClient = new();
                            string url = msix.BrowserDownloadUrl;
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

                            ContentDialog userDialog = new()
                            {
                                Title = "Warning",
                                Content = "This application is required to shutdown when installing an update.\nWould you like to proceed?",
                                PrimaryButtonText = "OK",
                                DefaultButton = ContentDialogButton.Primary,
                                XamlRoot = content.XamlRoot
                            };

                            ContentDialogResult userDialogResult = await userDialog.ShowAsync();

                            if (userDialogResult == ContentDialogResult.Primary)
                            {
                                PackageManager packman = new();
                                DeploymentResult depRes = await packman.AddPackageAsync(new Uri(filePath), null, DeploymentOptions.ForceApplicationShutdown);
                                DeploymentResult curPack = await packman.RemovePackageAsync(Windows.ApplicationModel.Package.Current.Id.FullName);
                            }
                        }
                    }
                }
                else
                {
                    ContentDialog confirmDialog = new()
                    {
                        Title = "Studiofy IDE (Canary) is Up-to-Date",
                        Content = $"{Latest.Name.Replace("v", "version ").Replace("-Canary", " for Canary Channel")} is the Latest Version",
                        CloseButtonText = "OK",
                        DefaultButton = ContentDialogButton.Close,
                        XamlRoot = content.XamlRoot
                    };
                    _ = await confirmDialog.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                if (ex.GetType().Name.ToString() == "NotFoundException")
                {
                    ContentDialog exceptionDialog = new()
                    {
                        Title = "Error",
                        Content = ex.Message /*"Cannot find new updates for Studiofy IDE Canary Channel"*/,
                        SecondaryButtonText = "Help",
                        CloseButtonText = "OK",
                        DefaultButton = ContentDialogButton.Close,
                        XamlRoot = content.XamlRoot
                    };
                    _ = await exceptionDialog.ShowAsync();
                }
            }
        }
    }
}
