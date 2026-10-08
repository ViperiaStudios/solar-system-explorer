using System;
using UnityEngine;

namespace SolarExplorer
{
    [Serializable]
    public sealed class PlanetData
    {
        public string name;
        public string category;
        public string description;
        public float diameterKm;
        public float gravity;
        public float orbitalDays;
        public float distanceMillionKm;
        public string highlight;
        public string sourceUrl;
        public float visualRadius;
        public float spinDegreesPerSecond;
        public string color;
        public string secondaryColor;
        public ArticleSection[] sections;
        public BodyStatistic[] statistics;

        public Color Accent
        {
            get { Color value; return ColorUtility.TryParseHtmlString(color, out value) ? value : Color.white; }
        }
    }

    [Serializable]
    public sealed class ArticleSection { public string heading; public string body; }

    [Serializable]
    public sealed class BodyStatistic { public string label; public string value; public string unit; }

    [Serializable]
    public sealed class PlanetCatalog
    {
        public string source;
        public string verified;
        public PlanetData[] planets;
        public PlanetData sun;

        public static PlanetCatalog Load()
        {
            var asset = Resources.Load<TextAsset>("PlanetCatalog");
            if (asset == null) throw new InvalidOperationException("Missing Resources/PlanetCatalog.json.");
            var catalog = JsonUtility.FromJson<PlanetCatalog>(asset.text);
            if (catalog == null || catalog.planets == null || catalog.planets.Length != 8)
                throw new InvalidOperationException("The catalog must contain the eight planets in solar order.");
            if (catalog.sun == null || catalog.sun.name != "Sun") throw new InvalidOperationException("The Sun entry is missing.");
            return catalog;
        }
    }
}
