using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class A_800Atk : MonoBehaviour
{
    public float attackRange;
    Vector3 playerDir;

    [Header("Refer")]
    public EnemyManager enemyManager;
    public string enemyName;
    public Collider2D meleeColl; // 공격 이펙트

    private void Awake()
    {
        enemyManager = enemyManager == null ? GetComponentInChildren<EnemyManager>() : enemyManager;
    }

    private void OnEnable()
    {
        StartCoroutine(Initial());
    }

    IEnumerator Initial()
    {
        yield return new WaitUntil(() => enemyManager.enemy != null);

        // 공격 범위 초기화
        attackRange = enemyManager.enemy.range;

        // 적 정보 들어오면 이름 표시
        enemyName = enemyManager.enemy.enemyName;

        //공격 콜라이더 끄기
        meleeColl.gameObject.SetActive(false);
    }

    private void Update()
    {
        // 몬스터 정보 없으면 리턴
        if (enemyManager.enemy == null)
            return;

        // 이미 공격중이면 리턴
        if (enemyManager.nowAction == EnemyManager.Action.Attack)
        {
            //속도 멈추기
            enemyManager.rigid.velocity = Vector3.zero;
            return;
        }

        //플레이어 방향 계산
        playerDir = PlayerManager.Instance.transform.position - transform.position;

        // 공격 범위 안에 들어오면 공격 시작
        if (playerDir.magnitude <= attackRange && attackRange > 0)
            StartCoroutine(MeleeAttack());
    }

    IEnumerator MeleeAttack()
    {
        // print("Melee Attack");

        // 공격 액션으로 전환
        enemyManager.nowAction = EnemyManager.Action.Attack;

        //움직일 방향에따라 회전
        if (playerDir.x > 0)
            transform.rotation = Quaternion.Euler(0, 0, 0);
        else
            transform.rotation = Quaternion.Euler(0, 180, 0);

        // 이동 멈추기
        enemyManager.rigid.velocity = Vector3.zero;

        // 공격 애니메이션 실행
        enemyManager.animList[0].SetBool("isAttack", true);

        // 쿨타임만큼 대기
        yield return new WaitForSeconds(enemyManager.enemy.cooltime);

        // Idle 애니메이션으로 초기화
        enemyManager.animList[0].SetBool("isAttack", false);

        // Idle 상태로 초기화
        enemyManager.nowAction = EnemyManager.Action.Idle;
    }

    public void OnMeleeEffect()
    {
        //공격 이펙트 켜기
        meleeColl.gameObject.SetActive(true);

        //플레이어 방향 계산
        playerDir = PlayerManager.Instance.transform.position - transform.position;

        //플레이어 방향으로 회전
        float rotation = Mathf.Atan2(playerDir.y, playerDir.x) * Mathf.Rad2Deg;
        meleeColl.transform.rotation = Quaternion.Euler(Vector3.forward * (rotation - 90f));
    }
}