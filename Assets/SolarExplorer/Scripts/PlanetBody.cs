using UnityEngine;

namespace SolarExplorer
{
    public sealed class PlanetBody : MonoBehaviour
    {
        public PlanetData Data { get; private set; }
        public int Index { get; private set; }
        public bool Rotating { get; set; } = true;
        public Transform Surface { get; private set; }

        public void Initialize(PlanetData data, int index, Transform surface)
        {
            Data = data;
            Index = index;
            Surface = surface;
        }

        // Driven by the same clock as the Sun and debris, including pause/resume.
        public void AdvanceAnimation(float deltaTime)
        {
            if (Rotating && Surface != null)
                Surface.Rotate(Vector3.up, Data.spinDegreesPerSecond * deltaTime, Space.Self);
        }
    }
}
