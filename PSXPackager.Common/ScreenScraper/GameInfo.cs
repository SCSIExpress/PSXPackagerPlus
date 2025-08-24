using System.Collections.Generic;

namespace PSXPackager.Common.ScreenScraper
{
    public class GameInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Synopsis { get; set; }
        public string Publisher { get; set; }
        public string Developer { get; set; }
        public string Players { get; set; }
        public string Rating { get; set; }
        public string ReleaseDate { get; set; }
        public List<string> Genres { get; set; } = new List<string>();
        public MediaInfo Media { get; set; } = new MediaInfo();
    }

    public class MediaInfo
    {
        public string Screenshot { get; set; }
        public string Fanart { get; set; }
        public string Video { get; set; }
        public string Marquee { get; set; }
        public string WheelUs { get; set; }
        public string WheelJp { get; set; }
        public string BoxTextureUs { get; set; }
        public string BoxTextureJp { get; set; }
        public string Box2dUs { get; set; }
        public string Box2dJp { get; set; }
        public string Box3dUs { get; set; }
        public string Box3dJp { get; set; }
        public string Icon0Url { get; set; }
    }
}