using System;
using UnityEngine;

namespace SolarExplorer
{
    [Serializable]
    public sealed class PlanetAppearance
    {
        [Tooltip("Optional equirectangular planet texture. Keeps the generated sphere and lighting.")]
        public Texture2D albedoTexture;
        [Tooltip("Optional Built-in Render Pipeline material. Takes priority over the texture slot.")]
        public Material surfaceMaterial;
    }
}
