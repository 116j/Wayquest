using UnityEngine;

public class Outline : MonoBehaviour
{
    [SerializeField]
    bool m_setOutline = true;
    [SerializeField]
    Color m_outlibeColor;
    [Range(0f,0.1f)]
    [SerializeField]
    float m_outlineThickness;

    MaterialPropertyBlock m_propertyBlock;

    void Start()
    {
        m_propertyBlock ??= new MaterialPropertyBlock();

        if (m_setOutline)
        {
            SetOutline(m_outlibeColor, m_outlineThickness);
        }
    }

    public void SetOutline(Color color, float thickness)
    {
        m_propertyBlock ??= new MaterialPropertyBlock();
        m_propertyBlock.SetColor("_OutlineColor", color);
        m_propertyBlock.SetFloat("_OutlineThickness", thickness);
        GetComponent<SpriteRenderer>().SetPropertyBlock(m_propertyBlock);
    }
}
