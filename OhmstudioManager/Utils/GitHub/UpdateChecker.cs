﻿/* 
 * This file is part of the MyOhmSessions distribution (https://github.com/GroovemanAndCo/MyOhmStudio).
 * Copyright (c) 2020 Fabien (https://github.com/fab672000)
 * 
 * This program is free software: you can redistribute it and/or modify  
 * it under the terms of the GNU General Public License as published by  
 * the Free Software Foundation, version 3.
 *
 * This program is distributed in the hope that it will be useful, but 
 * WITHOUT ANY WARRANTY; without even the implied warranty of 
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU 
 * General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License 
 * along with this program. If not, see <http://www.gnu.org/licenses/>.
 */
using System;
using System.Linq;
using System.Reflection;
using Octokit;
using Semver;
using System.Threading.Tasks;

namespace GitHubUpdate
{
    public enum UpdateType
    {
        None,
        Major,
        Minor,
        Patch
    }

    public class UpdateChecker
    {
        private IReleasesClient _releaseClient;
        internal GitHubClient Github;

        internal SemVersion CurrentVersion;
        internal string RepositoryOwner;
        internal string RepostoryName;
        internal Release LatestRelease;

        void Init(string owner, string name, SemVersion version)
        {
            Github = new GitHubClient(new ProductHeaderValue(name + @"-UpdateCheck"));
            _releaseClient = Github.Release;

            RepositoryOwner = owner;
            RepostoryName = name;
            CurrentVersion = version;
        }

        public UpdateChecker(string owner, string name)
        {
            Helper.ArgumentNotNullOrEmptyString(owner, @"owner");
            Helper.ArgumentNotNullOrEmptyString(name, @"name");

            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            version = version.Substring(0, version.LastIndexOf('.'));

            Init(owner, name, version);
        }

        public UpdateChecker(string owner, string name, string version)
        {
            Helper.ArgumentNotNullOrEmptyString(owner, @"owner");
            Helper.ArgumentNotNullOrEmptyString(name, @"name");
            Helper.ArgumentNotNullOrEmptyString(version, @"version");

            Init(owner, name, Helper.StripInitialV(version));
        }

        public async Task<UpdateType> CheckUpdate(UpdateType locked = UpdateType.None)
        {
            var releases = await _releaseClient.GetAll(RepositoryOwner, RepostoryName);

            SemVersion lockedVersion;
            switch (locked)
            {
                case UpdateType.Major:
                    lockedVersion = new SemVersion(CurrentVersion.Major + 1);
                    LatestRelease = releases.FirstOrDefault(
                        release => !release.Prerelease &&
                        Helper.StripInitialV(release.TagName) > CurrentVersion &&
                        Helper.StripInitialV(release.TagName) < lockedVersion
                    );
                    break;
                case UpdateType.Minor:
                    lockedVersion = new SemVersion(CurrentVersion.Major, CurrentVersion.Minor + 1);
                    LatestRelease = releases.FirstOrDefault(
                        release => !release.Prerelease &&
                        Helper.StripInitialV(release.TagName) > CurrentVersion &&
                        Helper.StripInitialV(release.TagName) < lockedVersion
                    );
                    break;
                default:
                    LatestRelease = releases.FirstOrDefault(
                        release => !release.Prerelease &&
                        Helper.StripInitialV(release.TagName) > CurrentVersion
                    );
                    break;
            }

            if (LatestRelease == null) return UpdateType.None;

            var tagName = LatestRelease.TagName;
            var latestVersion = Helper.StripInitialV(tagName);

            if (latestVersion.Major != CurrentVersion.Major)
                return UpdateType.Major;
            if (latestVersion.Minor != CurrentVersion.Minor)
                return UpdateType.Minor;
            if (latestVersion.Patch != CurrentVersion.Patch)
                return UpdateType.Patch;

            return UpdateType.None;
        }

        public async Task<string> RenderReleaseNotes()
        {
            if (LatestRelease == null)
                throw new InvalidOperationException();

            return await Github.Miscellaneous.RenderRawMarkdown(LatestRelease.Body);
        }

        public /*async*/ void DownloadAsset(string assetname)
        {
            // asset.Url is some api wizardry that we'll maybe use later
            //var assets = await _releaseClient.GetAssets(RepositoryOwner, RepostoryName, LatestRelease.Id);
            //var asset = assets.First(n => n.Name == assetname);
            
            // for now, do some ugly shit
            const string template = "https://github.com/{0}/{1}/releases/download/{2}/{3}";
            var url = string.Format(template, RepositoryOwner, RepostoryName, LatestRelease.TagName, assetname);

            System.Diagnostics.Process.Start(url);
        }
    }
}
