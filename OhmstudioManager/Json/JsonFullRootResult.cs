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
using System;
using System.Globalization;
using System.Linq;
using MoreLinq;
using OhmstudioManager.Utils;

namespace OhmstudioManager.Json
{
    public class Result
    {
        public Result() { }

        public Result(SessionProperties s)
        {
            nid = !long.TryParse(s.nid, out var n) ? 0 : n;
            title = s.title;
            url_web = s.url;
            short_desc = s.short_desc;
            mixdown = new [] { new Mixdown()};
            mixdown[0].url_m4a = s.Mixdown;
            role           = s.role.Contains("1002") ? "Owner" : "Contributor";
            if (!string.IsNullOrEmpty(s.created))      date_created = int.Parse(s.created);
            if (!string.IsNullOrEmpty(s.updated_date)) date_modified  = int.Parse(s.updated_date);
            hidden = s.hidden?.ToLower().Contains("true") ?? false;
            closed = s.closed?.ToLower().Contains("true") ?? false;
            cloneable = s.Remixable?.ToLower().Contains("true") ?? false;
            @public = s.session_type?.Contains("ublic") ?? false;
            // no members in this structure
        }

        public long nid { get; set; }
        public string title { get; set; }
        public string url_web { get; set; }
        public bool online { get; set; }
        public string role { get; set; }
        public Mixdown[] mixdown { get; set; }
        public int date_created { get; set; }
        public int date_modified { get; set; }
        public bool @public { get; set; }
        public bool closed { get; set; }
        public bool hidden { get; set; }
        public bool cloneable { get; set; }
        public Member[] members { get; set; }
        public string short_desc { get; set; }
        public Style[] styles { get; set; }
        public Mood[] moods { get; set; }
        internal string CachedPath { get; set; }

        public string MixdownFileName => $"{nid:D7}_{(FileUtils.FilterCharsFromFileName(title) ?? "unknown_title")}.m4a";

        public string m4a => mixdown?.Length>0 ?  mixdown?[0]?.url_m4a : null;
        public bool HasMixdown => mixdown?.Length>0;
        public string Image => mixdown?.Length>0 ? mixdown?[0]?.url_png : null;
        private DateTime EpochToDateTime(long epoch)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(epoch);
        }

        public string Modified => EpochToDateTime(date_modified).ToString("yyyy-MM-dd HH:mm:ss");
        public string Created => EpochToDateTime(date_created).ToString("yyyy-MM-dd HH:mm:ss");
        public string ModifiedPretty => EpochToDateTime(date_modified).ToString(CultureInfo.CurrentCulture);
        public string CreatedPretty => EpochToDateTime(date_created).ToString(CultureInfo.CurrentCulture);

        public string escapeQuotes(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = s.Replace("\r", "");
            s = s.Replace("\n", " ");
            s = s.Replace("\"", "\"\"");
            return s;
        }
        public string Public => @public ? "True" : "False";

        public string Attributes
        {
            get
            {
                const string Sep = ", ";
                return $"{(@public   ? "public" : "private")}" + 
                 Sep + $"{(closed    ? "closed" : "open")}" + 
                       $"{(cloneable ? Sep + "cloneable" : "")}" +
                       $"{(hidden    ? Sep + "hidden" : "")}";
            }
        }

        public Result FilterForCsv()
        {
            var r = new Result
            {
                nid = nid,
                title = escapeQuotes(title),
                url_web = url_web,
                online = online,
                role = role,
                mixdown = mixdown,
                date_created = date_created,
                date_modified = date_modified,
                @public = @public,
                closed = closed,
                hidden = hidden,
                cloneable = cloneable,
                members = members,
                short_desc = escapeQuotes(short_desc),
                styles = styles,
                moods = moods
            };
            return r;
        }

        public string Owners
        {
            get
            {
                var s = "";
                if (members == null) return s;
                var all = members.Where(x => x.role.Contains("wner")).ToList();
                foreach (var m in all)
                {
                    if (s.Length > 0) s += ", ";
                    if (m.role.Contains("ontrib")) s += $"{m.name}";
                    else s += $"{m.name}";
                }
                return s;
            }
        }
        public string Contributors
        {
            get
            {
                var s = "";
                if (members == null) return s;
                var all = members.Where(x => !x.role.Contains("wner")).ToList();
                foreach (var m in all)
                {
                    if (s.Length > 0) s += ", ";
                    s += $"{m.name}";
                }
                return s;
            }
        }

        public class Style
        {
            public int tid { get; set; }
            public int vid { get; set; }
            public string name { get; set; }
        }

        public class Mood
        {
            public int tid { get; set; }
            public int vid { get; set; }
            public string name { get; set; }
        }


        public string Styles
        {
            get
            {
                var s = "";
                if (styles == null) return s;
                var styleNames = styles.DistinctBy(x => x?.name).Select(x => x.name).ToArray();
                foreach (var m in styleNames)
                {
                    if (s.Length > 0) s += " | ";
                    s += $"{m}";
                }
                return s;
            }
        }
        public string Moods
        {
            get
            {
                var s = "";
                if (moods == null) return s;
                var moodNames = moods.DistinctBy(x => x?.name).Select(x => x.name).ToArray();
                foreach (var m in moodNames)
                {
                    if (s.Length > 0) s += " | ";
                    s += $"{m}";
                }
                return s;
            }
        }

        public override string ToString()
        {
            return
                $"- Title:\t{title}"
                + $"\n    NID           :\t{nid}"
                + $"\n    URL           :\t{url_web}"
                + $"\n    Created       :\t{Created}"
                + $"\n    Last updated  :\t{Modified}"
                + $"\n    Short desc.   :\t{short_desc}"
                + $"\n    Owners        :\t{Owners}"
                + $"\n    Contributors  :\t{Contributors}"
                + $"\n    Public:        \t{@public}"
                + $"\n    Hidden        :\t{hidden}"
                + $"\n    Closed        :\t{closed}"
                + $"\n    Styles        :\t{Styles}"
                + $"\n    Moods         :\t{Moods}"
                + "\n"
                ;
        }
        public string ExtractMixdown()
        {
            var m = mixdown;
            if (m == null || m.Length==0) return null;
            return m[0].url_m4a;
        }

    }
}