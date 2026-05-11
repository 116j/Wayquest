using DG.Tweening;
using UnityEngine;
using Zenject;

public class CameraFollow : MonoBehaviour
{
    [Inject]
    PlayerController m_player;

    float m_turnSpeed = 0.5f;
    Tween m_turnTween;

    void Update()
    {
        transform.position = m_player.transform.position;
    }

    public void Turn(int direction)
    {
        m_turnTween?.Kill();

        m_turnTween = transform.DORotate(new Vector3(0f, 180f * direction, 0f), m_turnSpeed).SetEase(Ease.InOutSine);
    }
}
