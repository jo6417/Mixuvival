using System.Collections;
using System.Collections.Generic;
using Lean.Pool;
using UnityEngine;
using DG.Tweening;

public class WaterSlimeAtk : MonoBehaviour
{
    public float attackRange;
    Vector3 playerDir;
    public float activeAngleOffset; // 액티브 공격 오브젝트 방향 오프셋
    bool attackReady; //공격 준비중

    [Header("Refer")]
    public EnemyManager enemyManager;
    public string enemyName;
    public GameObject bubblePrefab; //거품 프리팹

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

        // 대쉬 범위 초기화
        attackRange = enemyManager.enemy.range;

        // 적 정보 들어오면 이름 표시
        enemyName = enemyManager.enemy.enemyName;

        // 공격 오브젝트 있으면 끄기
        if (bubblePrefab != null)
            bubblePrefab.SetActive(false);
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

        // 공격 준비중이면 리턴
        if (attackReady)
            return;

        //플레이어 방향 계산
        playerDir = PlayerManager.Instance.transform.position - transform.position;

        // 공격 범위 안에 들어오면 공격 시작
        if (playerDir.magnitude <= attackRange && attackRange > 0)
            StartCoroutine(ChooseAttack());
    }

    IEnumerator ChooseAttack()
    {
        //움직일 방향에따라 회전
        if (playerDir.x > 0)
            transform.rotation = Quaternion.Euler(0, 0, 0);
        else
            transform.rotation = Quaternion.Euler(0, 180, 0);

        // 이동 멈추기
        enemyManager.rigid.velocity = Vector3.zero;

        // 점프중이라면
        if (enemyManager.enemyAI.jumpCoolCount > 0)
        {
            //공격 준비로 전환
            attackReady = true;

            // Idle 상태 될때까지 대기
            yield return new WaitUntil(() => enemyManager.nowAction == EnemyManager.Action.Idle);

            //공격 준비 끝
            attackReady = false;
        }

        // 거품 공격 실행
        StartCoroutine(BubbleAttack());
    }

    public IEnumerator BubbleAttack()
    {
        // print("Active Attack");

        // 공격 액션으로 전환
        enemyManager.nowAction = EnemyManager.Action.Attack;

        //애니메이터 끄기
        enemyManager.animList[0].enabled = false;

        //스프라이트 길쭉해지기
        transform.DOScale(new Vector2(0.8f, 1.2f), 0.5f)
        .SetEase(Ease.InBack);
        yield return new WaitForSeconds(0.5f);

        //스프라이트 납작해지기
        transform.DOScale(new Vector2(1.2f, 0.8f), 0.5f)
        .SetEase(Ease.OutBack);
        yield return new WaitForSeconds(0.5f);

        // 부들거리며 떨기
        transform.DOShakeScale(1f, 0.1f, 20, 90, false)
        .OnComplete(() =>
        {
            // 스케일 초기화
            transform.DOScale(Vector3.one, 0.5f);
        });

        // 플레이어 방향 계산
        playerDir = PlayerManager.Instance.transform.position - transform.position;

        // 공격 오브젝트 각도 계산
        float angle = Mathf.Atan2(playerDir.y, playerDir.x) * Mathf.Rad2Deg;

        // 공격 오브젝트 생성
        LeanPool.Spawn(bubblePrefab, bubblePrefab.transform.position, Quaternion.AngleAxis(angle + activeAngleOffset, Vector3.forward), SystemManager.Instance.magicPool);

        // 쿨타임만큼 대기후 초기화
        yield return new WaitForSeconds(enemyManager.enemy.cooltime);

        //애니메이터 켜기
        enemyManager.animList[0].enabled = true;
        // Idle로 전환
        enemyManager.nowAction = EnemyManager.Action.Idle;
    }
}