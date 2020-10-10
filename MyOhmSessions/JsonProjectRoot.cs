namespace MyOhmStudioSessions

{

    public class ProjectsRoot
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
