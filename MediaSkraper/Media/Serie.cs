
using System.Collections.Generic;

namespace MediaSkraper.Media
{
    public class Serie : IMedia
    {
        public string Seasons { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Age { get; set; }
        public string Thumbnail { get; set; }
        public string Url { get; set; }
        public string ProviderId { get; set; }
        public List<string> Genres { get; set; }
        public List<string> Actors { get; set; }
        public Provider Provider { get; set; }

        public Serie()
        {
            Genres = new List<string>();
            Actors = new List<string>();
        }
    }
}
