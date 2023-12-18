using Markdig;
using Markdig.Syntax;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Windows.Management.Deployment;

namespace WindowsCode.Studio.Services
{
    public class UpdateService
    {
        public enum Version
        {
            Canary,
            Beta,
            Official
        }

        public async Task<string> GetUpdateDescription(XamlRoot root)
        {
            GitHubClient GitClient = new(new ProductHeaderValue("Studiofy-IDE"));

            try
            {
                IReadOnlyList<Release> Release = await GitClient.Repository.Release.GetAll("Studiofy", "Studiofy-IDE");
                return Release[0].Body;
            }
            catch (RateLimitExceededException ex)
            {
                await new ContentDialog()
                {
                    Title = ex.Source,
                    Content = ex.Message,
                    CloseButtonText = "Close",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = root
                }.ShowAsync();
                return null;
            }
        }

        public List<Block> ParseMarkdown(string markdownText)
        {
            MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            MarkdownDocument document = Markdown.Parse(markdownText, pipeline);
            return document.ToList();
        }

        public async Task GetUpdates(UIElement content)
        {
            Grid mainGrid = new()
            {
                Height = 100,
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
                IsIndeterminate = true,
                IsActive = true
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

            // Add StackPanel to the content of the page
            ContentDialog checkUpdateDialog = new()
            {
                Title = "Checking for Updates...",
                Content = mainGrid,
                XamlRoot = content.XamlRoot
            };

            GitHubClient GitClient = new(new ProductHeaderValue("Studiofy-IDE"));

            string Current = Windows.ApplicationModel.Package.Current.Id.Version.Major.ToString() + "."
                        + Windows.ApplicationModel.Package.Current.Id.Version.Minor.ToString() + "."
                        + Windows.ApplicationModel.Package.Current.Id.Version.Build.ToString() + "."
                        + Windows.ApplicationModel.Package.Current.Id.Version.Revision.ToString() + "-Canary";

            try
            {
                _ = checkUpdateDialog.ShowAsync();

                IReadOnlyList<Release> Release = await GitClient.Repository.Release.GetAll("Studiofy", "Studiofy-IDE");

                Release Latest = Release[0];

                await Task.Delay(2500);

                if (!Latest.TagName.EndsWith("v" + Current))
                {
                    checkUpdateDialog.Hide();

                    checkUpdateDialog = new()
                    {
                        Title = "New Version Available!",
                        Content = $"{Latest.Name.Replace("-Canary", " for Canary Channel")} is Available. Would you like to Update now?\n\nNote: Application will shutdown when installing the update",
                        PrimaryButtonText = "Yes, Update Now",
                        SecondaryButtonText = "No, Maybe Later",
                        DefaultButton = ContentDialogButton.Primary,
                        XamlRoot = content.XamlRoot
                    };

                    ContentDialogResult dialogResult = await checkUpdateDialog.ShowAsync();

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

                            try
                            {
                                PackageManager packman = new();

                                DeploymentResult depRes = await packman.AddPackageAsync(new Uri(filePath), null, DeploymentOptions.ForceTargetApplicationShutdown);

                                DeploymentResult curPack = await packman.RemovePackageAsync(Windows.ApplicationModel.Package.Current.Id.FullName);

                                if (string.IsNullOrEmpty(depRes.ErrorText))
                                {
                                    await Windows.ApplicationModel.Core.CoreApplication.RequestRestartAsync(string.Empty);
                                }
                                else
                                {
                                    ContentDialog errorDialog = new()
                                    {
                                        Title = "Error on Installing Update",
                                        Content = depRes.ErrorText,
                                        CloseButtonText = "OK",
                                        DefaultButton = ContentDialogButton.Close,
                                        XamlRoot = content.XamlRoot
                                    };

                                    await errorDialog.ShowAsync();
                                }
                            }
                            catch (Exception ex)
                            {
                                ContentDialog errorDialog = new()
                                {
                                    Title = "Error on Installing Update",
                                    Content = $"{ex.Message}\n\nIf you want to manually install the update yourself, find this path:\n{filePath}",
                                    CloseButtonText = "OK",
                                    DefaultButton = ContentDialogButton.Close,
                                    XamlRoot = content.XamlRoot
                                };

                                await errorDialog.ShowAsync();
                            }
                        }
                    }
                }
                else
                {
                    checkUpdateDialog.Hide();

                    checkUpdateDialog = new()
                    {
                        Title = "Studiofy IDE (Canary) is Up-to-Date",
                        Content = $"{Latest.Name.Replace("v", "version ").Replace("-Canary", " for Canary Channel")} is the Latest Version",
                        CloseButtonText = "OK",
                        DefaultButton = ContentDialogButton.Close,
                        XamlRoot = content.XamlRoot
                    };
                    _ = await checkUpdateDialog.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                if (ex.GetType().Name.ToString() == "NotFoundException")
                {
                    ContentDialog exceptionDialog = new()
                    {
                        Title = "Error",
                        Content = ex.Message,
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
