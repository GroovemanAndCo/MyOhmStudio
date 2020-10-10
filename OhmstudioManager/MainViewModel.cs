/* 
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
using CsvHelper;
using MoreLinq;
using MyOhmSessions;
using Newtonsoft.Json;
using NLog;
using OhmstudioManager.Json;
using OhmstudioManager.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OhmstudioManager

{
    public class MainViewModel : MediaFoundationPlaybackViewModel
    {
        public string DatabaseDir => Path.Combine(MyDocumentsFolder, ".myohmdb");
        public string ProjectsDir => Path.Combine(DatabaseDir, "projects");
        public string MyMusicFolder => Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        public string MyDocumentsFolder=> Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        private const string AvatarImages = @"sites/default/files/imagecache/avatar_pic/pictures";
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private StateOwner StateOwner { get; }

        public MainViewModel(StateOwner parent) : base(parent)
        {
            StateOwner = parent;
            UrlBase = "https://www.ohmstudio.com/v3/feed/my_projects_p?page=";
            CreateCacheFolders();
        }

        public string UrlBase { get; set; }

        public string Url(int page) => $"{UrlBase}{page}";

        public string ApplicationDirectory => AppDomain.CurrentDomain.BaseDirectory;
        /// <summary>
        /// On Mac unfortunately,  Mydocuments returns the home folder so get the right 'MyDocuments' folder
        /// </summary>
        /// <returns></returns>
        public string MyDocumentsFolderPath()
        {
            var perso = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var path = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            if (string.CompareOrdinal(perso.ToLowerInvariant(), path.ToLowerInvariant()) == 0)
            {
                path = Path.Combine(perso, "Documents");
            }
            return path;
        }

        private const string formatPaged = "{\"sessions\":[";
        private const string formatFull = "result\":[{\"nid\":";

        public void Reset()
        {
            Current.result = null;
        }

        // public ListView CurrentListView { get; private set; }

        private readonly List<Member> AllMembers = new List<Member>();

        /// <summary>
        /// Load a collection of json files from common folders
        /// </summary>
        /// <param name="fullpath"></param>
        public void LoadJson(string fullpath = null)
        {
            var ohmJsonFiles = new Dictionary<string, string>();
            var pagedJsonFiles = new Dictionary<string, string>();
            var FullJsonFile = new Dictionary<string, bool>();

            void AddJsonFile(string jsonFile)
            {
                try
                {
                    var jsonContent = File.ReadAllText(jsonFile);
                    var jsonHeader = jsonContent.Substring(0, 100);

                    var fullDetected = jsonHeader.Contains(formatFull);
                    var pagedDetected = jsonHeader.Contains(formatPaged);

                    if (!fullDetected && !pagedDetected)
                    {
                        Logger.Trace($"Skipping json file \"{jsonFile}\"");
                        return;
                    }
                    if (!FullJsonFile.ContainsKey(jsonFile))
                    {
                        FullJsonFile[jsonFile] = fullDetected;
                        if (fullDetected) ohmJsonFiles.Add(jsonFile, jsonContent);
                        else pagedJsonFiles.Add(jsonFile, jsonContent);
                    }
                    Logger.Info($"Found ohmstudio sessions json file: {jsonFile}.");

                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Failed to add an ohmstudio session json file, check your file content: \"{jsonFile}\"");
                }
            }

            if (fullpath == null)
            {
                var JsonFilesPath = new List<string> {
                    Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                    Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                    ApplicationDirectory
                };
                var AllJsonFiles = new List<string>();

                foreach (var path in JsonFilesPath)
                {
                    var jsonFiles = Directory.EnumerateFiles(path, "*.json", SearchOption.AllDirectories).ToArray();
                    if (jsonFiles.Length != 0)
                    {
                        AllJsonFiles.AddRange(jsonFiles);
                    }
                }
                AllJsonFiles.Sort();
                AllJsonFiles = AllJsonFiles.Distinct().ToList();

                foreach (var jsonFile in AllJsonFiles)
                {
                    AddJsonFile(jsonFile);
                }
            }
            else
            {
                AddJsonFile(fullpath);
            }

            if (pagedJsonFiles.Count > 0) ohmJsonFiles = ohmJsonFiles.Concat(pagedJsonFiles).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            foreach (var jsonKVP in ohmJsonFiles)
            {
                var jsonFile = jsonKVP.Key;
                var jsonContent = jsonKVP.Value;

                try
                {
                    var isFull = FullJsonFile[jsonFile];
                    if (isFull)
                    {
                        var json = JsonConvert.DeserializeObject<JsonFullRoot>(jsonContent);
                        AddJsonToCurrent(json);
                    }
                    else
                    {
                        var json = JsonConvert.DeserializeObject<JsonPagedRoot>(jsonContent);
                        AddJsonToCurrent(json);
                    }
                }

                catch (Exception ex)
                {

                    Logger.Error(ex, $"Failed to parse file {jsonFile}, please check file content.");
                }
            }

            if (Current?.result?.Length > 0)
            {
                foreach (var s in Current.result)
                {
                    s.title = FilterWebAmpersand(s.title);
                    s.short_desc = FilterWebAmpersand(s.short_desc);
                }

                Logger.Info($"Successfully loaded meta-data for {Current?.result?.Length} projects!");

                Member[] members = null;
                try
                {
                    members = Current?.result?.SelectMany(x => x.members).DistinctBy(x => x.uid)?.OrderBy(x => x.uid).ToArray();
                }
                catch (Exception)
                {
                    // ignored
                }

                AllMembers.AddRange((members ?? Array.Empty<Member>()).Where(x => !AllMembers.Contains(x)));
                //var membersPath = Path.Combine(ProjectsDir, $"members.json");
                //using (StreamWriter file = File.CreateText(membersPath))
                //{
                //    JsonSerializer serializer = new JsonSerializer();
                //    serializer.Serialize(file, AllMembers);
                //}

                //foreach (var m in members)
                //{
                //    Logger.Info($"Getting Projects for {m.name} [{m.uid}]");
                //    var storePath = Path.Combine(ProjectsDir, $"{m.uid:D6}_{FileUtils.FilterCharsFromFileName(m.name)}.json");
                //    if (File.Exists(storePath)) continue;
                //    var result = HttpUtils.GetProjects(m.uid);
                //    File.WriteAllText(storePath, result);
                //}
            }
            else
            {
                Logger.Info("Failed to find ohmstudio json file content at startup, please load a json file manually as explained in the About dialog.");
            }
        }

        public void AddJsonToCurrent(JsonPagedRoot json)
        {
            var toAdd = new List<Result>();
            if (json.pager!=null) Logger.Info($"Converting page {json.pager.current+1} of {json.pager.total} ...");

            foreach(var p in json.sessions)
            {
                var s = p.session;
                if (s == null) continue;
                toAdd.Add(new Result(s));
            }

            if (Current == null) Current = new JsonFullRoot();
            Current.result = AccumulateToResult(Current.result, toAdd);

        }

        private Result[] AccumulateToResult(Result[] src, ICollection<Result> arr)
        {
            if (src == null) return arr.ToArray();
            var z = new Result[src.Length + arr.Count];
            src.CopyTo(z, 0);
            arr.CopyTo(z, src.Length);
            return z.DistinctBy(x => x.nid).ToArray();
        }

        public void AddJsonToCurrent(JsonFullRoot json)
        {
            if (json == null)
            {
                Logger.Error("Can't add null json reference to current list.");
                return;
            }
            if (Current?.result == null)
            {
                Current = json;
            }
            else
            {
                Current.result = AccumulateToResult(Current.result, json.result);
            }
            var empty = json.result.Where(x => x == null).ToArray();
            Logger.Info($"{json.result?.Length} sessions added, with {empty.Length} empty sessions.");
        }

        string FilterWebAmpersand(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            s = s.Replace("&quot;", "\"");
            s = s.Replace("&lt;", "<");
            s = s.Replace("&gt;", ">");
            s = s.Replace("&#039;", "'");
            s = s.Replace("&amp;", "&");
            return s;
        }

        public JsonFullRoot Current { get; set; } = new JsonFullRoot();


        private readonly object AccessMixdownLock = new object();

        public async Task<int> DownloadFilesAsync(ICollection<Result> curSessions, Action<double> report, CancellationToken ct)
        {
            if (curSessions == null || curSessions.Count == 0)
            {
                Logger.Error("Nothing to download. Check your input json files and try again...");
                return 0;
            }

            var notfound = 0;
            var progressPct = 0.0;
            var count = 0;
            var Catalog = new List<Result>(2000);

            report(progressPct);
            var subFolderPath = StateOwner.DestinationFolder;

            Catalog.AddRange(curSessions.Where(x => x != null).ToList());

            // Write text catalog
            string catalogFileName = "";
            string catalogCsvName = "";
            try
            {
                catalogFileName = Path.Combine(subFolderPath, "sessions_catalog.txt");
                Logger.Info("Attempting to write sessions catalog ...");
                File.WriteAllText(catalogFileName, string.Join("\n", Catalog.Select(x => x.ToString()).ToArray()));

            }
            catch (Exception ex)
            {

                Logger.Error(ex, $"Failed to write txt file: {catalogFileName}");
            }

            // Write csv catalog
            try
            {
                catalogCsvName = Path.Combine(subFolderPath, "sessions_catalog.csv");
                using (TextWriter writer = new StreamWriter(catalogCsvName, false, Encoding.UTF8))
                {
                    var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

                    csv.Configuration.ShouldQuote = (field, context) => true;
                    await csv.WriteRecordsAsync(Catalog.Select(x => x.FilterForCsv()).ToList()); // where values implements IEnumerable
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to write csv file: {catalogCsvName}");
            }

            // Write Mixdowns
            await Task.Run(() =>
            {
                var mixdowns = curSessions.Where(x => x?.mixdown != null && x.mixdown.Length > 0 
                    && !string.IsNullOrEmpty(x.mixdown[0].url_m4a)).DistinctBy(x => x.mixdown[0].url_m4a).ToArray();
                var total = mixdowns.Length;

                Logger.Info($"Attempting to download up to {mixdowns.Length} mixdowns ...");

                foreach (var it in mixdowns)
                {
                    if (it == null) continue;

                    count++;
                    var uri = it.m4a;
                    try
                    {
                        if (string.IsNullOrWhiteSpace(uri))
                        {
                            Logger.Warn($"No mixdown was found for [{it.nid}] '{it.title}' at url: {it.url_web} ");
                            notfound++;
                            continue;
                        }
                        var mixdownName = it.MixdownFileName;
                        lock (AccessMixdownLock) it.CachedPath = FullMixdownPath(mixdownName);

                        if (!HttpUtils.DownloadFile(uri, mixdownName, MixdownFolder))
                        {
                            Logger.Warn($"Failed to download file from: {uri}");
                        }
                        
                        progressPct = ((double)count) / total * 100;
                        report(progressPct);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, $"Failed to download files with mixdownFileName {it.title} to destination directory {MixdownFolder}");
                    }

                    // check for a cancel
                    if (ct.IsCancellationRequested)
                    {
                        Logger.Info($"Cancelling download on user request, {count} files downloaded");
                        throw new OperationCanceledException(ct);
                    }

                }
                Logger.Info($"{total - notfound} sessions were successfully exported, {notfound} did not contain any mixdown");
            }, ct);
            Process.Start(MixdownFolder);
            return count;
        }

        static string GetProgramFilesx86()
        {
            if( 8 == IntPtr.Size 
                || (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"))))
            {
                return Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            }

            return Environment.GetEnvironmentVariable("ProgramFiles");
        }

        public bool LaunchSessionWithOhmstudio(long sessionid)
        {
            var args = $"ohmstudio:///session?id={sessionid}";
            var cmdPath = $"{GetProgramFilesx86()}\\Ohm Force\\Ohm Studio\\";
            var cmd = $"{cmdPath}ohm_studio_client_url_hook.exe";
            if (!File.Exists(cmd))
            {
                StateOwner.DisplayErrorDialog($"Could not find the ohmstudio process {cmd}", "Error while launching ohmstudio client.");
                return false;
            }
        try
        {
                if (!OhmLauncher.CheckIfLaunched(StateOwner)) return false;
                var startInfo = new ProcessStartInfo
                {
                    UseShellExecute = false
                    , FileName = cmd
                    , Arguments = args
                    , WindowStyle = ProcessWindowStyle.Hidden
                };

                Logger.Trace($"Attempting to launch ohmstudio with session {sessionid}, invocation: {cmd} {args} ");

                Process.Start(startInfo);
                return true;

            }

            catch (Exception ex)
            {
                var msg = $"Failed to launch ohm session {sessionid} from path: {cmdPath}. Exception{ex}";
                StateOwner.DisplayErrorDialog(msg, $"Error While trying to launch ohmstudio client with session {sessionid}");
                return false;
            }        
        }

        public void OnClosing()
        {
            PlayLink(null);
        }

        void CreateCacheFolders()
        {
            Directory.CreateDirectory(ProjectsDir);
            var subdir = AvatarImages.Replace('/', '\\');
            var images = Path.Combine(DatabaseDir, subdir);

            Directory.CreateDirectory(images);

        }

        public void PlayerControl(PlayerAction  playerAction)
        {
            switch (playerAction)
            {
                case PlayerAction.Play:
                    PlayResume();
                    break;
                case PlayerAction.Stop:
                    Stop();
                    break;
                case PlayerAction.Pause:
                    Pause();
                    break;
                case PlayerAction.Forward:
                    Forward();
                    break;
                case PlayerAction.Rewind:
                    Rewind();
                    break;
            }
        }
    }
}
