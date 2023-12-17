using System;
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
            VersionText.Text = "version " +
                  Package.Current.Id.Version.Major + "." +
                  Package.Current.Id.Version.Minor + "." +
                  Package.Current.Id.Version.Build +
                  ((Package.Current.Id.Version.Revision != 0) ? "." + Package.Current.Id.Version.Revision.ToString() : string.Empty);
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
            for (int i = 0; i <= 5; i++)
            {
                LoadingBar.IsIndeterminate = true;
                DisplayRandomTip();
                await Task.Delay(5000);
            }
            return;
        }

        private void DisplayRandomTip()
        {
            Random random = new();
            int randomIndex = random.Next(0, splashScreenTips.Length);
            TipText.Text = $"\n{splashScreenTips[randomIndex]}";
        }
    }
}
