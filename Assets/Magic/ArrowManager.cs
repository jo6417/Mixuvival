using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Lean.Pool;

public class ArrowManager : MonoBehaviour
{
    public enum State { Ready, Attack };
    public State state = State.Ready; //화살의 상태

    [Header("Refer")]
    public GameObject atkRange; //공격 범위 표시
    public GameObject atkMark; //공격 위치 표시 마커
    public GameObject tail; //화살 꼬리 trail

    // public float atkDuration = 1; //공격 진행 시간
    Vector3[] atkPos = null;
    GameObject[] marks = null;
    List<GameObject> marker = null;
    Vector3 arrowLastPos;
    Rigidbody2D rigidArrow;
    Collider2D col;
    SpriteRenderer sprite;
    public MagicInfo magic;

    private void OnEnable()
    {
        tail.SetActive(true); //꼬리 켜기

        //비활성화 되고 다시 스폰 될때는 Enable에서 공격 실행
        if (magic != null)
            StartCoroutine(shotArrow());
    }

    void Start()
    {
        marker = new List<GameObject>();
        rigidArrow = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sprite = GetComponent<SpriteRenderer>();
        tail.SetActive(true); //꼬리 켜기
        magic = MagicDB.Instance.GetMagicByID(GetComponent<MagicProjectile>().magicID);

        //처음 스폰 될때는 Start에서 공격 실행
        if (magic != null)
            StartCoroutine(shotArrow());
    }

    void Update()
    {
        // 날아가는 방향으로 화살 돌리기
        ArrowDirection();

        // 공격 할때만 콜라이더 활성화
        if (state == State.Attack)
            col.enabled = true;

        if (state == State.Ready)
            col.enabled = false;
    }

    //화살 발사
    IEnumerator shotArrow()
    {
        // 플레이어 중심 범위 안의 적 배열에 담기
        float range = magic.range * PlayerManager.Instance.range;
        // 범위 사이즈 반영해서 보여주기
        PlayerManager.Instance.transform.Find("BasicMagicRange").transform.localScale = new Vector2(range / 2, range / 2);

        // 관통 횟수 만큼 공격
        int atkNum = PlayerManager.Instance.projectileNum;

        for (int i = 0; i < atkNum; i++)
        {
            // 마크한 적의 위치
            Vector2 enemyPos = markEnemyPos(range);

            //마크 위치가 (0,0)이면 공격 취소
            if (enemyPos != Vector2.zero)
            {
                //적의 위치에 마커 생성
                GameObject mark = LeanPool.Spawn(atkMark, enemyPos, Quaternion.identity);

                //공격하는데 걸리는 시간 = 거리 / 속력
                float atkDuration = Vector2.Distance(transform.position, enemyPos) / magic.speed;

                // 마크 포지션 방향으로 domove
                transform.DOMove(enemyPos, atkDuration / atkNum)
                .OnStart(() =>
                {
                    state = State.Attack; //공격 상태로 전환
                })
                .OnComplete(() =>
                {
                    state = State.Ready; //준비 상태로 전환

                // 도착하면 마크 오브젝트 삭제
                LeanPool.Despawn(mark);
                });

                //공격 시간동안 대기
                yield return new WaitForSeconds(atkDuration / atkNum);
            }
        }

        //꼬리 끄기
        tail.SetActive(false);

        // 끝나면 화살 디스폰
        LeanPool.Despawn(transform);
        yield return null;
    }

    // 플레이어 주변 랜덤 적 위치에 마크하기
    Vector2 markEnemyPos(float range)
    {
        Vector2 enemyPos = Vector2.zero;

        if (magic == null)
            magic = MagicDB.Instance.GetMagicByID(GetComponent<MagicProjectile>().magicID);

        //캐릭터 주변의 적들
        Collider2D[] colls = null;
        colls = Physics2D.OverlapCircleAll(PlayerManager.Instance.transform.position, range, 1 << LayerMask.NameToLayer("Enemy"));

        //랜덤한 적 하나 뽑기
        if (colls != null && colls.Length != 0)
        {
            enemyPos = colls[Random.Range(0, colls.Length)].transform.position;
        }

        //적의 위치 리턴
        return enemyPos;

        // int Num = magic.pierceNum; //마법 관통 횟수만큼 공격

        // //범위 안의 적이 공격횟수보다 많으면 공격횟수만큼 반복, 아니면 범위 안의 적 갯수만큼 반복
        // int atkNum = colls.Length > Num ? Num : colls.Length;

        // // 공격 위치 배열 초기화
        // atkPos = colls.Length == 0 ? null : new Vector3[atkNum];
        // marks = colls.Length == 0 ? null : new GameObject[atkNum];

        // // 중복된 적 마크 방지
        // List<int> indexList = new List<int>();
        // for (int i = 0; i < colls.Length; i++)
        // {
        //     //리스트에 모든 인덱스 넣기
        //     indexList.Add(i);
        // }

        // for (int i = 0; i < atkNum; i++)
        // {
        //     //인덱스 리스트에서 랜덤한 난수 생성
        //     int index = Random.Range(0, indexList.Count);

        //     // 공격 위치
        //     Vector3 pos = colls[indexList[index]].transform.position;

        //     //이미 선택된 인덱스 제거
        //     indexList.RemoveAt(index);

        //     // 공격 위치에 마커 생성
        //     GameObject mark = LeanPool.Spawn(atkMark, pos, Quaternion.identity);
        //     marks[i] = mark;

        //     // 공격 위치 배열에 추가
        //     atkPos[i] = pos;
        // }
    }

    //화살이 날아가는 방향
    void ArrowDirection()
    {
        // 날아가는 방향 바라보기
        if (transform.position != arrowLastPos)
        {
            Vector3 returnDir = (transform.position - arrowLastPos).normalized;
            float rotation = Mathf.Atan2(returnDir.y, returnDir.x) * Mathf.Rad2Deg;

            rigidArrow.rotation = rotation - 90;
            arrowLastPos = transform.position;
        }

    }
}