using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Lean.Pool;

public class MagicFalling : MonoBehaviour
{
    [Header("Refer")]
    public Animator anim;
    float originAnimSpeed;
    public MagicInfo magic;
    public string magicName;
    public Collider2D col;
    public Vector2 originColScale; //원래 콜라이더 크기
    public Vector2 originScale; //원래 오브젝트 크기
    public Ease fallEase;
    public float fallSpeed = 1f; //마법 떨어지는데 걸리는 시간 (시전 딜레이)
    public float fallDistance = 2f; //마법 오브젝트 떨어지는 거리(시전 시간)
    public float coolTimeSet = 0.3f; //마법 쿨타임 임의 조정
    public bool isDespawn = false; //디스폰 여부
    public bool isExpand = false; //커지면서 등장 여부

    [Header("Effect")]
    public GameObject magicEffect;
    public Vector3 effectPos;

    private void Awake()
    {
        // 오브젝트 기본 사이즈 저장
        originScale = transform.localScale;

        //애니메이터 찾기
        anim = transform.GetComponent<Animator>();
        // 애니메이션 기본 속도 저장
        originAnimSpeed = anim.speed;

        // 콜라이더 기본 사이즈 저장
        if (TryGetComponent(out BoxCollider2D boxCol))
        {
            originColScale = boxCol.size;
        }
        else if (TryGetComponent(out CapsuleCollider2D capCol))
        {
            originColScale = capCol.size;
        }
        else if (TryGetComponent(out CircleCollider2D circleCol))
        {
            originColScale = Vector2.one * circleCol.radius;
        }

        //초기화 하기
        StartCoroutine(Initial());
    }

    IEnumerator Initial()
    {
        isDespawn = false;

        //시작할때 콜라이더 끄기
        MagicTrigger(false);

        //MagicDB 로드 될때까지 대기
        yield return new WaitUntil(() => MagicDB.Instance.loadDone);

        //프리팹 이름으로 아이템 정보 찾아 넣기
        if (magic == null)
        {
            magic = MagicDB.Instance.GetMagicByName(transform.name.Split('_')[0]);
            magicName = magic.magicName;
        }

        //magic 못찾으면 코루틴 종료
        if (magic == null)
            yield break;
    }

    private void OnEnable()
    {
        StartCoroutine(FallingMagicObj());
    }

    IEnumerator FallingMagicObj()
    {
        //초기화
        StartCoroutine(Initial());

        //magic이 null이 아닐때까지 대기
        yield return new WaitUntil(() => magic != null);

        // 속도 버프 계수
        float speedBuff = (magic.speed * 0.1f) + (PlayerManager.Instance.rateFire - 1f);
        speedBuff = Mathf.Clamp(speedBuff, 0.1f, 0.99f);

        // 마법 오브젝트 속도
        float magicSpeed = fallSpeed - fallSpeed * speedBuff;

        // 애니메이션 속도 계산
        anim.speed = originAnimSpeed + originAnimSpeed * speedBuff;

        // 팝업창 0,0에서 점점 커지면서 나타내기
        if (isExpand)
            transform.localScale = Vector2.zero;
        transform.DOScale(originScale, magicSpeed)
        .SetUpdate(true)
        .SetEase(Ease.OutBack);

        Vector2 startPos = (Vector2)transform.position + Vector2.up * fallDistance;
        Vector2 endPos = transform.position;

        //시작 위치로 올려보내기
        transform.position = startPos;
        //목표 위치로 떨어뜨리기        
        transform.DOMove(endPos, magicSpeed)
        .SetEase(fallEase)
        .OnComplete(() =>
        {
            //콜라이더 발동시키기
            MagicTrigger(true);

            // 속도 버프 계수
            float durationBuff = magic.range * (PlayerManager.Instance.duration - 1f);
            // 마법 오브젝트 속도
            float duration = magic.range - durationBuff;

            // 오브젝트 자동 디스폰하기
            if (!isDespawn)
                isDespawn = true;
            StartCoroutine(AutoDespawn(duration));
        });
    }

    void MagicTrigger(bool magicTrigger)
    {
        // magicInfo 데이터로 히트박스 크기 적용
        // if(magic != null)
        // col.size = originColliderSize * magic.range;

        // 마법 트리거 발동 됬을때 (적 데미지 입히기, 마법 효과 발동)
        if (magicTrigger)
        {
            col.enabled = true;

            // 이펙트 오브젝트 생성 (이펙트 있으면)
            if (magicEffect)
                LeanPool.Spawn(magicEffect, transform.position + effectPos, Quaternion.identity);
        }
        else
        {
            col.enabled = false;
        }
    }

    //애니메이션 끝날때 이벤트 함수
    public void AnimEndDespawn()
    {
        //디스폰 중이면 리턴
        if (isDespawn)
            return;

        // 속도 버프 계수
        float durationBuff = magic.range * (PlayerManager.Instance.duration - 1f);
        // 마법 오브젝트 속도
        float duration = magic.range - durationBuff;

        StartCoroutine(AutoDespawn(duration));
    }

    IEnumerator AutoDespawn(float duration)
    {
        //range 속성만큼 지속시간 부여
        yield return new WaitForSeconds(duration);

        //콜라이더 끄고 종료
        MagicTrigger(false);

        if (!isDespawn)
            isDespawn = true;
        // 오브젝트 디스폰하기
        LeanPool.Despawn(transform);
    }
}
