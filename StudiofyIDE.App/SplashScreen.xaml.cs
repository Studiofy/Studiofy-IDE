using Microsoft.UI.Xaml;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using WinUIEx;

namespace WindowsCode.Studio
{
    public sealed partial class SplashScreen : WinUIEx.SplashScreen
    {

        private string[] splashScreenTips = new string[]
        {
            "Follow a style guide relevant to your programming language",
            "Use descriptive names for variables and functions",
            "Add comments to explain complex sections of code",
            "Break down your code into smaller functions. It makes it easier to understand and maintain",
            "Use Git to track changes, collaborate with others, and revert changes if needed",
            "Provide meaningful error message if you are trying to implement error handling",
            "Regularly participate in conduct code reviews",
            "Aim for small, focused functions that perform a single task",
            "Provide documentation for your code, especially for Public APIs",
            "Embrace feedback received during code reviews"
        };

        public SplashScreen(Type window) : base(window)
        {
            InitializeComponent();
            Title = string.Empty;
            this.SetIsShownInSwitchers(false);
            CheckWindowsVersion();
            VersionText.Text = "version " + Package.Current.Id.Version.Major + "."
                                                  + Package.Current.Id.Version.Minor + "."
                                                  + Package.Current.Id.Version.Build;
        }

        private void CheckWindowsVersion()
        {
            OperatingSystem WinOS = Environment.OSVersion;
            if (WinOS.Version.Build < 22000)
            {
                Backdrop = new AcrylicSystemBackdrop();
            }
            else if (WinOS.Version.Build >= 22000)
            {
                Backdrop = new MicaSystemBackdrop();
            }
        }

        protected override async Task OnLoading()
        {
            for (int i = 0; i <= 10; i++)
            {
                LoadingBar.IsIndeterminate = true;

                if (Application.Current.Resources?.Source != null)
                {
                    AssemblyName assemblyName = new(Application.Current.Resources.Source.AbsolutePath);
                    Assembly assembly = Assembly.Load(assemblyName);

                    // Get all loaded assemblies
                    Assembly[] loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();

                    foreach (Assembly loadedAssembly in loadedAssemblies)
                    {
                        // Display the name of each loaded assembly
                        ProgressText.Text = $"Loaded Assembly: {loadedAssembly.FullName}";

                        // You can also iterate over types in each assembly if needed
                        System.Collections.Generic.IEnumerable<Type> types = loadedAssembly.GetTypes()
                            .Where(t => typeof(UIElement).IsAssignableFrom(t));

                        foreach (Type type in types)
                        {
                            ProgressText.Text += $"\nLoaded Component: {type.Name}";
                        }

                        await Task.Delay(250); // Add a delay if needed
                    }
                }
                else
                {
                    ProgressText.Text = "Loading Core Libraries";

                    for (int j = 0; j < i % 4; j++)
                    {
                        ProgressText.Text += ".";
                    }
                }
                // Display a random tip or quote
                DisplayRandomTip();

                await Task.Delay(2500);
            }

            return;
        }

        private void DisplayRandomTip()
        {
            // Display a random tip or quote in ProgressText
            Random random = new();
            int randomIndex = random.Next(0, splashScreenTips.Length);
            TipText.Text = $"\nTip: {splashScreenTips[randomIndex]}";
        }
    }
}
