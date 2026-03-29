using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using Zenject;

public class ScrollSelectHandler : MonoBehaviour, ISelectHandler, IPointerClickHandler, ISubmitHandler
{
    [SerializeField]
    AutoScrollOnSelect m_autoScroller;
    [SerializeField]
    int ind;
    [SerializeField]
    UnityEvent m_buyFunc;

    [Inject]
    ShopLayout m_shop;

    public void OnPointerClick(PointerEventData eventData)
    {
        m_shop.ShowItemText(ind);
    }

    //Shows the text about the subject and adjusts the ScrollView borders for a specific element
    public void OnSelect(BaseEventData eventData)
    {
        m_shop.ShowItemText(ind);
        m_autoScroller.OnElementSelected(GetComponent<RectTransform>());
    }

    public void OnSubmit(BaseEventData eventData)
    {
        m_buyFunc.Invoke();
    }
}
