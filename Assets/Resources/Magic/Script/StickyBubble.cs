using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class StickyBubble : MonoBehaviour
{
    [Header("Refer")]
    public MagicHolder magicHolder;
    public MagicInfo magic;
    public ParticleSystem particle;
    List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>(); //충돌한 파티클의 이벤트 정보들

    public LayerMask playerLayer;
    public LayerMask enemyLayer;

    private void Awake()
    {
        magicHolder = magicHolder == null ? GetComponent<MagicHolder>() : magicHolder;
        particle = particle == null ? GetComponent<ParticleSystem>() : particle;
    }

    private void OnEnable()
    {
        StartCoroutine(Initial());
    }

    IEnumerator Initial()
    {
        yield return new WaitUntil(() => magicHolder.magic != null);
        magic = magicHolder.magic;

        // 타겟에 따라 파티클 충돌 대상 레이어 바꾸기
        ParticleSystem.CollisionModule particleColl = particle.collision;

        if (magicHolder.GetTarget() == MagicHolder.Target.Enemy)
        {
            particleColl.collidesWith = enemyLayer;
        }

        if (magicHolder.GetTarget() == MagicHolder.Target.Player)
        {
            particleColl.collidesWith = playerLayer;
        }

        // 타겟 방향을 쳐다보기
        Vector2 targetDir = magicHolder.targetPos - transform.position;
        float angle = Mathf.Atan2(targetDir.y, targetDir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(Vector3.forward * angle);

        // 초기화 완료 되면 파티클 시작
        particle.Play();
    }

    private void OnParticleCollision(GameObject other)
    {
        ParticlePhysicsExtensions.GetCollisionEvents(particle, other, collisionEvents);

        for (int i = 0; i < collisionEvents.Count; i++)
        {
            //todo 충돌 지점에 거품 터진 스프라이트 남기기

            // 플레이어에 데미지 주기
            if (other.CompareTag("Player") && PlayerManager.Instance.hitCoolCount <= 0 && !PlayerManager.Instance.isDash)
            {
                StartCoroutine(PlayerManager.Instance.Hit(transform));
            }

            // 히트박스를 찾았으면 데미지 주기
            if (other.CompareTag("Enemy"))
            {
                print(other.tag);
                if (other.TryGetComponent(out EnemyHitBox enemyHitBox))
                {
                    print(other.tag);

                    StartCoroutine(enemyHitBox.enemyManager.Hit(gameObject));
                }
            }
        }
    }
}
