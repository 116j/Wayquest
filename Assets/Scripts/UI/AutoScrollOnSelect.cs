using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public class AutoScrollOnSelect : MonoBehaviour
{
    ScrollRect m_scroll;

    void Awake()
    {
        m_scroll = GetComponent<ScrollRect>();
    }

    public void OnElementSelected(RectTransform selected)
    {
        Canvas.ForceUpdateCanvases();

        //element's corners
        Vector3[] itemCorners = new Vector3[4];
        //scrollview's visible area corners
        Vector3[] viewCorners = new Vector3[4];
        selected.GetWorldCorners(itemCorners);
        m_scroll.viewport.GetWorldCorners(viewCorners);

        float viewTop = viewCorners[1].y;
        float viewBottom = viewCorners[0].y;

        float itemTop = itemCorners[1].y;
        float itemBottom = itemCorners[0].y;

        float norm = m_scroll.verticalNormalizedPosition;

        //if the element comes out from above - scrolls up
        if (itemTop > viewTop)
        {
            float delta = itemTop - viewTop;
            norm += delta / (m_scroll.content.rect.height - m_scroll.viewport.rect.height);
        }
        //if the element comes out from below - scrolls down
        else if (itemBottom < viewBottom)
        {
            float delta = viewBottom - itemBottom;
            norm -= delta / (m_scroll.content.rect.height - m_scroll.viewport.rect.height);
        }

        m_scroll.verticalNormalizedPosition = Mathf.Clamp01(norm);
    }
}
