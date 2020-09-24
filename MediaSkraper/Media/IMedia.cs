
using System.Collections.Generic;

namespace MediaSkraper.Media
{
    public enum Provider
    {
        Netflix,
        AmazonVOD
    }

    public interface IMedia
    {
        string Name { get; set; }
        string Description { get; set; }
        string Age { get; set; }
        string Thumbnail { get; set; }
        string Url { get; set; }
        string ProviderId { get; set; }
        List<string> Genres { get; set; }
        List<string> Actors { get; set; }
        Provider Provider { get; set; }
    }
}
