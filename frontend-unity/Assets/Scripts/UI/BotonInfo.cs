using UnityEngine;
using UnityEngine.EventSystems;

namespace Sme.UI
{
    public class BotonInfo : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private GameObject panelInfo;

        private void Awake()
        {
            panelInfo.SetActive(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            panelInfo.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            panelInfo.SetActive(false);
        }

        private void OnDisable()
        {
            panelInfo.SetActive(false);
        }
    }
}
