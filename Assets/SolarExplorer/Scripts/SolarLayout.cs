using UnityEngine;

namespace SolarExplorer
{
    // An explorable composition, not an ephemeris or a distance scale model.
    public static class SolarLayout
    {
        public static readonly float[] OrbitRadii = { 45, 65, 100, 120, 150, 178, 214, 252 };
        private static readonly Vector3[] Locations = {
            new Vector3(-50.86f,0,28.49f), new Vector3(23.10f,0,42.58f),
            new Vector3(54.75f,0,-55.17f), new Vector3(77.42f,0,-3.64f),
            new Vector3(137.30f,0,-44.37f), new Vector3(-28.15f,0,-71.74f),
            new Vector3(117.62f,0,48.44f), new Vector3(187.66f,0,25.76f)
        };
        public const float SunRadius = 22.7f;
        public const float DepthCompression = 0.55f;
        public const float OverviewPitch = 60;
        public const float OverviewDistance = 240;
        public static readonly Vector3 OverviewPivot = new Vector3(56.8869f,-17.4539f,-30.2311f);
        public static readonly Quaternion SaturnTilt = Quaternion.Euler(30, 0, -18);
        // The orbital plane is compressed for the approved cinematic composition.
        // These are browsing guides, not astronomical orbital eccentricities.
        public static Vector3 OrbitPoint(float radius,float angle)
        {
            return new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius*DepthCompression);
        }

        public static Vector3 Position(int index)
        {
            return Locations[index];
        }
    }
}
