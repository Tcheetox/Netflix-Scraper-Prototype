
using System.Collections.Generic;

namespace MediaSkraper.Media
{
    public class Movie : IMedia
    {
        public int Time { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Age { get; set; }
        public string Thumbnail { get; set; }
        public string Url { get; set; }
        public string ProviderId { get; set; }
        public List<string> Genres { get; set; }
        public List<string> Actors { get; set; }
        public Provider Provider { get; set; }

        public Movie()
        {
            Genres = new List<string>();
            Actors = new List<string>();
        }
    }
}
