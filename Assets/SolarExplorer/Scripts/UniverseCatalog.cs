using System;
using UnityEngine;

namespace SolarExplorer
{
    [Serializable]
    public sealed class AtlasEntry
    {
        public string id, name, category, summary, facts, sourceUrl;
        public ArticleSection[] sections;
    }

    [Serializable]
    public sealed class ExoplanetEntry
    {
        public AtlasEntry article;
        public float radiusEarth, orbitalDays;
        public string color, secondaryColor;
    }

    [Serializable]
    public sealed class StarSystemEntry
    {
        public AtlasEntry article;
        public ExoplanetEntry[] planets;
    }

    [Serializable]
    public sealed class UniverseCatalog
    {
        public string reviewed;
        public AtlasEntry localGroup, neighborhood;
        public AtlasEntry[] galaxies;
        public StarSystemEntry[] systems;

        public static UniverseCatalog Load()
        {
            var asset=Resources.Load<TextAsset>("UniverseCatalog");
            if(asset==null) throw new InvalidOperationException("UniverseCatalog is missing.");
            var data=JsonUtility.FromJson<UniverseCatalog>(asset.text);
            if(data==null || data.galaxies==null || data.galaxies.Length!=5 || data.systems==null || data.systems.Length!=2)
                throw new InvalidOperationException("Universe catalog must contain five galaxies and two featured exoplanet systems.");
            return data;
        }
    }

    public sealed class AtlasTarget : MonoBehaviour
    {
        public string Id;
        public float Radius;
        public Transform SpinningSurface;
    }
}
