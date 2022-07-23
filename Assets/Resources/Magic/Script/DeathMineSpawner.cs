using System.Collections;
using System.Collections.Generic;
using Lean.Pool;
using UnityEngine;

public class DeathMineSpawner : MonoBehaviour
{
    MagicHolder mineMagicHolder;
    MagicInfo magic;
    public GameObject minePrefab; //지뢰 프리팹

    private void Awake()
    {
        mineMagicHolder = GetComponent<MagicHolder>();
    }

    IEnumerator Init()
    {
        yield return new WaitUntil(() => mineMagicHolder.magic != null);
        magic = mineMagicHolder.magic;

        // 적이 죽을때 함수를 호출하도록 델리게이트에 넣기
        SystemManager.Instance.globalEnemyDeadCallback += DropMine;

        //플레이어 자식으로 들어가기
        transform.SetParent(PlayerManager.Instance.transform);
        transform.localPosition = Vector3.zero;
    }

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    private void OnDisable()
    {
        // 해당 마법 장착 해제되면 델리게이트에서 함수 빼기
        SystemManager.Instance.globalEnemyDeadCallback -= DropMine;
    }

    // 지뢰 드랍하기
    public void DropMine(EnemyManager enemyManager)
    {
        // print(MagicDB.Instance.MagicCritical(magic));

        // 크리티컬 확률 = 드랍 확률
        bool isDrop = MagicDB.Instance.MagicCritical(magic);

        //크리티컬 데미지 = 회복량
        int healAmount = Mathf.RoundToInt(MagicDB.Instance.MagicCriticalPower(magic));
        healAmount = (int)Mathf.Clamp(healAmount, 1f, healAmount); //최소 회복량 1f 보장

        // 마법 크리티컬 확률에 따라 지뢰 생성
        if (isDrop)
        {
            // 지뢰 오브젝트 생성
            GameObject deathMine = LeanPool.Spawn(minePrefab, enemyManager.transform.position + Vector3.up * 2f, Quaternion.identity, SystemManager.Instance.magicPool);

            // 매직홀더 찾기
            MagicHolder mineMagicHolder = deathMine.GetComponentInChildren<MagicHolder>();

            // 마법 타겟 넣기
            mineMagicHolder.SetTarget(MagicHolder.Target.Enemy);

            // 마법 타겟 위치 넣기
            mineMagicHolder.targetPos = enemyManager.transform.position;
        }
    }
}
