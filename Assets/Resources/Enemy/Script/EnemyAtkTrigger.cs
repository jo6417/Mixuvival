using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAtkTrigger : MonoBehaviour
{
    public GameObject explosionPrefab;
    public SpriteRenderer atkRangeSprite;
    public EnemyManager enemyManager;

    public bool atkTrigger; //범위내 플레이어 들어왔는지 여부

    private void Awake()
    {
        //공격 범위 인디케이터 스프라이트 찾기
        atkRangeSprite = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        //폭발 이펙트 있을때
        if (explosionPrefab)
        {
            //폭발 콜라이더 및 이펙트 사이즈 동기화
            explosionPrefab.transform.localScale = transform.localScale;
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        //  고스트 아닐때, 플레이어가 충돌하면
        if (other.CompareTag("Player") && !enemyManager.IsGhost)
        {
            atkTrigger = true;

            // 자폭형 몬스터일때
            if (enemyManager && enemyManager.selfExplosion && !enemyManager.isDead)
            {
                // 자폭하기
                StartCoroutine(enemyManager.Dead());
            }
        }

        // 고스트일때, 몬스터가 충돌하면
        if (other.CompareTag("Enemy") && enemyManager.IsGhost)
        {
            // 몬스터가 충돌했을때 히트박스 있을때
            if (other.TryGetComponent(out EnemyHitBox hitBox))
            {
                // 충돌 대상이 본인이면 리턴
                if (hitBox.enemyManager == enemyManager)
                    return;

                // 충돌 몬스터도 고스트일때 리턴
                if (hitBox.enemyManager.IsGhost)
                    return;
            }
            // 콜라이더가 히트박스를 갖고 있지 않을때 리턴
            else
                return;

            atkTrigger = true;

            // 자폭형 몬스터일때
            if (enemyManager && enemyManager.selfExplosion && !enemyManager.isDead)
            {
                // 자폭하기
                StartCoroutine(enemyManager.Dead());
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        //  고스트 아닐때, 플레이어가 나가면
        if (other.CompareTag("Player") && !enemyManager.IsGhost)
            atkTrigger = false;

        // 고스트일때, 몬스터가 나가면
        if (other.CompareTag("Enemy") && enemyManager.IsGhost)
            atkTrigger = false;
    }
}
