using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SolarExplorer.Editor
{
    public static class ProjectChecks
    {
        public const string ScenePath = "Assets/SolarExplorer/Scenes/SolarSystem.unity";

        [MenuItem("Solar Explorer/Open Solar System")]
        public static void OpenScene()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("Solar Explorer/Validate Project")]
        public static void Validate()
        {
            var catalog = PlanetCatalog.Load();
            var expected = new[] { "Mercury","Venus","Earth","Mars","Jupiter","Saturn","Uranus","Neptune" };
            Require(catalog.planets.Select(p=>p.name).SequenceEqual(expected), "Planet order is incorrect.");
            for (int i=0;i<catalog.planets.Length;i++)
            {
                var planet=catalog.planets[i];
                Require(planet.diameterKm>0 && planet.gravity>0 && planet.orbitalDays>0 && planet.visualRadius>0, "Invalid values for "+planet.name);
                Require(!string.IsNullOrWhiteSpace(planet.description), "Missing description for "+planet.name);
                Require(planet.sourceUrl.StartsWith("https://science.nasa.gov/",StringComparison.Ordinal), "Unexpected source URL.");
                if(i>0) Require(planet.distanceMillionKm>catalog.planets[i-1].distanceMillionKm, "Distances must increase in solar order.");
            }
            foreach(var body in catalog.planets.Concat(new[]{catalog.sun}))
            {
                Require(body.sections!=null && body.sections.Length>=6,"Missing article sections: "+body.name);
                Require(body.sections.All(s=>!string.IsNullOrWhiteSpace(s.heading) && s.body.Length>100),"Incomplete article: "+body.name);
                Require(Uri.TryCreate(body.sourceUrl,UriKind.Absolute,out var source) && source.Scheme=="https" && source.Host=="science.nasa.gov","Unexpected article source.");
            }
            Require(catalog.sun.statistics!=null && catalog.sun.statistics.Length==4,"Sun needs its own statistics.");
            foreach(var name in new[] { "PlanetSurface","SpaceUnlit","SunGlow","PlanetRings","GalaxySky","Atmosphere","EarthClouds","OrbitGlow","AsteroidSurface","CinematicBloom","SolarFilament","StarGlint" })
            {
                var shader=Resources.Load<Shader>(name);
                Require(shader!=null,"Missing shader: "+name);
                Require(!ShaderUtil.ShaderHasError(shader),"Shader compilation failed: "+name);
            }
            Require(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)!=null,"Missing scene.");
            Require(EditorBuildSettings.scenes.Any(s=>s.enabled && s.path==ScenePath),"Scene missing from build settings.");
            Require(Resources.Load<Font>("Fonts/Barlow-Regular")!=null,"Missing UI font.");
            Require(Resources.Load<Font>("Fonts/Rajdhani-SemiBold")!=null,"Missing HUD font.");
            foreach(var name in SpaceVisuals.TextureNames.Concat(new[]{"8k_earth_clouds","8k_earth_nightmap","8k_saturn_ring_alpha","eso0932a_milky_way","2k_earth_normal_map","2k_earth_specular_map"}))
                Require(Resources.Load<Texture2D>("Textures/"+name)!=null,"Missing texture: "+name);
            Debug.Log("SOLAR_VALIDATION_PASSED: catalog, shaders, scene asset and build list.");
        }

        private static void Require(bool condition,string message)
        {
            if(!condition) throw new InvalidOperationException(message);
        }
    }
}
