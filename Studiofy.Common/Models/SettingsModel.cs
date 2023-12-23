using System;

namespace Studiofy.Common.Models
{
    public class SettingsModel
    {
        public enum Theme
        {
            Default,
            Light,
            Dark
        }

        public Guid Id { get; set; }

        public bool IsConfidentialInfoBarEnabled { get; set; }

        public bool DisplayShowWelcomePageOnStartup { get; set; }

        public Theme AppTheme { get; set; }
    }
}
