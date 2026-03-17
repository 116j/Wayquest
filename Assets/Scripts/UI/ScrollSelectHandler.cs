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

    //Показывает текст про предмет и регулирует границы ScrollView для конкретного элемента
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
