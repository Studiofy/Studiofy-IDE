using Octokit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WindowsCode.Core.Services
{
    public class UpdateService
    {
        private int CURRENT_VERSION_MAJOR = 0, CURRENT_VERSION_MINOR = 0, CURRENT_VERSION_BUILD = 0, CURRENT_VERSION_REVISION = 0;
        private int LATEST_VERSION_MAJOR = 0, LATEST_VERSION_MINOR = 0, LATEST_VERSION_BUILD = 0, LATEST_VERSION_REVISION = 0;
        private char VERSION_SEPARATOR = '-';
        private string VERSION_IDENTIFIER = "";

        public UpdateService()
        {

            if (CURRENT_VERSION_MAJOR == 0 && CURRENT_VERSION_MINOR >= 1 && CURRENT_VERSION_MINOR <= 9)
            {
                new UpdateService(Version.Canary);
            }
            else if (CURRENT_VERSION_MAJOR == 0 && CURRENT_VERSION_MINOR >= 10 && CURRENT_VERSION_MINOR <= 19)
            {
                new UpdateService(Version.Beta);
            }
            else if (CURRENT_VERSION_MAJOR == 1 && CURRENT_VERSION_MINOR >= 0 && CURRENT_VERSION_BUILD <= 19 && CURRENT_VERSION_REVISION >= 0)
            {
                new UpdateService(Version.Official);
            }
        }

        public UpdateService(Version version)
        {
            switch (version)
            {
                case Version.Canary:
                    VERSION_IDENTIFIER = VERSION_SEPARATOR + "Canary";
                    break;
                case Version.Beta:
                    VERSION_IDENTIFIER = VERSION_SEPARATOR + "Beta";
                    break;
                case Version.Official:
                    VERSION_IDENTIFIER = VERSION_SEPARATOR + "Official";
                    break;
            }
        }

        public enum Version
        {
            Canary,
            Beta,
            Official
        }

        public async Task CheckUpdates()
        {
            GitHubClient GitClient = new(new ProductHeaderValue("Studiofy-IDE"));
            try
            {
                IReadOnlyList<Release> Releases = await GitClient.Repository.Release.GetAll("Studiofy", "Studiofy-IDE");
                foreach (Release Release in Releases)
                {
                    string TagName = Release.TagName.Replace("-Canary", "");
                    string[] versionParts = TagName.Split('.');
                    LATEST_VERSION_MAJOR = int.Parse(versionParts[0]);
                    LATEST_VERSION_MINOR = int.Parse(versionParts[1]);
                    LATEST_VERSION_BUILD = int.Parse(versionParts[2]);
                    LATEST_VERSION_REVISION = int.Parse(versionParts[3]);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
