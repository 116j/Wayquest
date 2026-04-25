using UnityEngine;

[RequireComponent(typeof(SoundController))]
public class ShellScript : AttackListener
{
    [SerializeField]
    LayerMask m_collideLayers;
    readonly float m_lifeTime = 5f;

    float m_life = 0f;

    void Update()
    {
        //destroys a shell if it has lived more than m_lifeTime
        m_life += Time.deltaTime;
        if (m_life >= m_lifeTime)
        {
            Destroy(gameObject);
        }
    }

    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        if (m_collideLayers == (m_collideLayers | (1 << collision.gameObject.layer)))
        {
            base.OnTriggerEnter2D(collision);
            GetComponent<SoundController>().PlaySound(gameObject.name + "Hit");
            Destroy(gameObject);
        }
    }
}
