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
namespace OhmstudioManager

{

    public class JsonPagedRoot
    {
        public Session[] sessions { get; set; }
        public Pager pager { get; set; }
    }

    public class Pager
    {
        public int total { get; set; }
        public int current { get; set; }
    }

    public class Session
    {
        public SessionProperties session { get; set; }
    }

    public class SessionProperties
    {
        public string nid { get; set; }
        public string title { get; set; }
        public string url { get; set; }
        public string role { get; set; }
        public string Mixdown { get; set; }
        public string created { get; set; }
        public string updated_date { get; set; }
        public string session_type { get; set; }
        public string closed { get; set; }
        public string hidden { get; set; }
        public string Remixable { get; set; }
        public string short_desc { get; set; }
        public string styles { get; set; }
        public string moods { get; set; }

        public override string ToString()
        {
            return 
                  $"- Title:\t{title}"
                + $"\n    NID:\t{nid}"
                + $"\n    URL:\t{url}"
                + $"\n    Created:\t{created}"
                + $"\n    Last updated:\t{updated_date}"
                + $"\n    Session type:\t{session_type}"
                + $"\n    Closed:\t{closed}"
                + $"\n    Hidden:\t{hidden}"
                + $"\n    Remixable:\t{Remixable}"
                + $"\n    Short desc.:\t{short_desc}"
                + $"\n    Styles:\t{styles}"
                + $"\n    moods:\t{moods}" 
                +"\n"
                ;
        }
    }

}
