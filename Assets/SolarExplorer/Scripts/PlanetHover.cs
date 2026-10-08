using UnityEngine;
using UnityEngine.EventSystems;

namespace SolarExplorer
{
    public sealed class PlanetHover : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
    {
        private SolarSystemBootstrap explorer;
        private int index;
        public void Initialize(SolarSystemBootstrap owner,int planetIndex) { explorer=owner; index=planetIndex; }
        public void OnPointerEnter(PointerEventData data) { explorer.SetUIHover(index); }
        public void OnPointerExit(PointerEventData data) { explorer.ClearUIHover(index); }
        private void OnDisable() { if(explorer!=null) explorer.ClearUIHover(index); }
    }
}
