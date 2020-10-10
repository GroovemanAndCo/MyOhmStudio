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

    public class JsonFullRoot
    {
        public int status { get; set; }
        public Result[] result { get; set; }
    }

    public class Mixdown
    {
        public string url_ogg { get; set; }
        public string url_m4a { get; set; }
        public string url_png { get; set; }
    }

    public class Member
    {
        public int uid { get; set; }
        public string name { get; set; }
        public string role { get; set; }
    }

}
