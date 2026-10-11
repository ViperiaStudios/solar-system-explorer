using UnityEngine;
using UnityEngine.EventSystems;

namespace SolarExplorer
{
    public sealed class SolarSystemBootstrap : MonoBehaviour
    {
        [Header("Optional planet assets, in order from Mercury to Neptune")]
        [SerializeField] private PlanetAppearance[] appearances = new PlanetAppearance[8];
        [Header("Optional replacement Sun")]
        [Tooltip("Centered visual prefab with a surface radius of 1. Its root is scaled to the scene Sun radius. Use visual effects only, without cameras or input scripts.")]
        [SerializeField] private GameObject sunVisualPrefab;
        public PlanetBody[] Planets { get; private set; }
        public PlanetBody[] Bodies { get; private set; }
        public const int SunIndex = 8;
        public ExplorerCamera CameraRig { get; private set; }
        public ExplorerUI Interface { get; private set; }
        public UniverseExplorer Universe { get; private set; }
        public GameObject SolarWorld { get; private set; }
        public int SelectedIndex { get; private set; } = -1;
        public bool Paused { get; private set; }
        public bool OrbitsVisible { get; private set; } = true;
        public bool InteractionLocked => (CameraRig!=null && CameraRig.IsTransitioning) || (Universe!=null && Universe.IsCrossing);
        public int HoveredIndex => SelectedIndex>=0?-1:(uiHover>=0?uiHover:worldHover);
        public float OrbitOpacity => visuals.OrbitOpacity;
        private int uiHover=-1,worldHover=-1;
        private SpaceVisuals visuals;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            var catalog = PlanetCatalog.Load();
            SolarWorld=new GameObject("Solar System world"); SolarWorld.transform.SetParent(transform,false);
            Transform world=SolarWorld.transform;
            visuals = new SpaceVisuals();
            visuals.CreateStars(world);
            var sun = visuals.CreateSun(world, catalog.sun, sunVisualPrefab);
            visuals.CreateDebris(world);
            Planets = new PlanetBody[catalog.planets.Length];
            for (int i = 0; i < Planets.Length; i++)
            {
                var appearance = appearances != null && i < appearances.Length ? appearances[i] : null;
                Planets[i] = visuals.CreatePlanet(catalog.planets[i], i, SolarLayout.Position(i), world, appearance);
                visuals.CreateOrbit(SolarLayout.OrbitRadii[i], catalog.planets[i].Accent, world);
            }
            Bodies = new PlanetBody[9];
            System.Array.Copy(Planets, Bodies, Planets.Length);
            Bodies[SunIndex] = sun;

            var cameraObject = new GameObject("Explorer Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.transform.SetParent(transform, false);
            cameraObject.tag = "MainCamera";
            var view = cameraObject.GetComponent<Camera>();
            view.clearFlags = CameraClearFlags.Skybox;
            view.allowMSAA = true;
            view.allowHDR = true;
            cameraObject.AddComponent<CinematicBloom>();
            view.backgroundColor = new Color(0.006f,0.010f,0.021f);
            view.fieldOfView = 44;
            view.nearClipPlane = 0.05f;
            view.farClipPlane = 1400;
            CameraRig = cameraObject.AddComponent<ExplorerCamera>();
            CameraRig.Snap();
            if (EventSystem.current == null)
            {
                var events = new GameObject("Event System", typeof(EventSystem), typeof(StandaloneInputModule));
                events.transform.SetParent(transform, false);
            }
            Interface = gameObject.AddComponent<ExplorerUI>();
            Interface.Initialize(this, view);
            Universe=gameObject.AddComponent<UniverseExplorer>(); Universe.Initialize(this,view);
            gameObject.AddComponent<ExplorerInput>().Initialize(this);
        }

        public void SelectPlanet(int index)
        {
            if(InteractionLocked || (Universe!=null && Universe.Active)) return;
            if (index < 0 || index >= Bodies.Length) return;
            uiHover=worldHover=-1;
            SelectedIndex = index;
            CameraRig.Focus(Bodies[index]);
            Interface.ShowPlanet(Bodies[index]);
        }

        public void Overview()
        {
            if(InteractionLocked) return;
            if(Universe!=null && Universe.Active) { Universe.Back(); return; }
            uiHover=worldHover=-1;
            SelectedIndex = -1;
            CameraRig.Overview();
            visuals.SetOrbitsVisible(OrbitsVisible);
            Interface.ShowOverview();
        }

        public void TogglePause()
        {
            if(InteractionLocked) return;
            Paused = !Paused;
            foreach (var planet in Planets) planet.Rotating = !Paused;
            Interface.RefreshControls();
        }

        public void ToggleMotion()
        {
            if(InteractionLocked) return;
            CameraRig.ReduceMotion = !CameraRig.ReduceMotion;
            Interface.RefreshControls();
        }

        private void Update()
        {
            if(Universe!=null && Universe.Active)
            {
                if(Input.GetKeyDown(KeyCode.Escape)) Universe.Back();
                if(Input.GetKeyDown(KeyCode.Home)) Universe.Home();
                if(Input.GetKeyDown(KeyCode.Space)) TogglePause();
                AdvanceWorldAnimation(Time.unscaledDeltaTime); worldHover=-1; return;
            }
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Home)) Overview();
            if (Input.GetKeyDown(KeyCode.Space)) TogglePause();
            for (int i=0;i<8;i++)
                if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1+i))) SelectPlanet(i);
            if (Input.GetKeyDown(KeyCode.Alpha0)) SelectPlanet(SunIndex);
            if (Input.GetKeyDown(KeyCode.RightArrow)) SelectPlanet((SelectedIndex + 1) % Bodies.Length);
            if (Input.GetKeyDown(KeyCode.LeftArrow)) SelectPlanet(SelectedIndex < 0 ? SunIndex : (SelectedIndex + Bodies.Length - 1) % Bodies.Length);

            AdvanceWorldAnimation(Time.unscaledDeltaTime);
            worldHover=-1;
            if (InteractionLocked || Input.touchCount > 0 || ExplorerInput.OverInterface(Input.mousePosition)) return;
            RaycastHit hit;
            if (Physics.Raycast(CameraRig.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition), out hit, 1400))
            {
                var planet = hit.collider.GetComponentInParent<PlanetBody>();
                if (planet != null)
                {
                    worldHover=planet.Index;
                }
            }
        }

        public void AdvanceWorldAnimation(float deltaTime)
        {
            if (Paused || deltaTime <= 0) return;
            foreach (var planet in Planets) planet.AdvanceAnimation(deltaTime);
            visuals.AdvanceAmbience(deltaTime, false);
            Universe?.AdvanceAnimation(deltaTime);
        }

        public void ToggleView()
        {
            if(InteractionLocked || (Universe!=null && Universe.Active)) return;
            if (SelectedIndex >= 0) Overview();
            CameraRig.ToggleOverhead();
            Interface.RefreshControls();
        }

        public void ToggleOrbits()
        {
            if(InteractionLocked || (Universe!=null && Universe.Active)) return;
            OrbitsVisible = !OrbitsVisible;
            visuals.SetOrbitsVisible(OrbitsVisible);
            Interface.RefreshControls();
        }

        public void SetUIHover(int index) { if(!InteractionLocked) uiHover=index; }
        public void ClearUIHover(int index) { if(uiHover==index) uiHover=-1; }
        public void RefreshVisuals()
        {
            visuals.SetHoveredPlanet(HoveredIndex);
            visuals.SetOverviewVisibility(1-CameraRig.FocusBlend);
        }
        public void RestoreSolarOverview()
        {
            SelectedIndex=-1; uiHover=worldHover=-1;
            SolarWorld.SetActive(true); Interface.Canvas.gameObject.SetActive(true); Interface.ShowOverview();
            CameraRig.GetComponent<Camera>().clearFlags=CameraClearFlags.Skybox;
            CameraRig.GetComponent<Camera>().farClipPlane=1400;
            CameraRig.RevealSolarSystem();
        }
        private void LateUpdate() { RefreshVisuals(); }
        private void OnDestroy() { if (visuals != null) visuals.Dispose(); }
    }
}
