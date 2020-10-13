using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Octokit;

namespace OhmstudioManager.Utils
{
    public static class GitHubReleasesManager
    {
        public static void CheckForReleases(string api_key)
        {
            var client = new GitHubClient(new ProductHeaderValue("MyOhmSessions", "v1.3.2"));
            client.Credentials = new Credentials(api_key);
            var result = Task.Run(() => client.Repository.Release.GetAll("GroovemanAndCo", "MyOhmStudio"));
            if (result.Wait(5000))
            {
                var releases = result.Result;
                foreach (var r in releases)
                {
                    Console.WriteLine($"Name: {r.Name} TagName: {r.TagName}");
                }
            }
        }
    }
}
