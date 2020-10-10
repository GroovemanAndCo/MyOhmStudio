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
namespace OhmstudioManager.Json
{

    public class Rootobject
    {
        public string jsonrpc { get; set; }
        public RpcResult[] result { get; set; }
        public string id { get; set; }
    }

    public class RpcResult
    {
        public int nid { get; set; }
        public string title { get; set; }
        public string url_web { get; set; }
        public bool online { get; set; }
        public string role { get; set; }
        public Mixdown[] mixdown { get; set; }
        public int date_created { get; set; }
        public int date_modified { get; set; }
        public bool _public { get; set; }
        public bool open { get; set; }
        public bool visible { get; set; }
        public bool cloneable { get; set; }
        public Member[] members { get; set; }
        public string short_desc { get; set; }
        public Style[] styles { get; set; }
        public Mood[] moods { get; set; }
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
}
