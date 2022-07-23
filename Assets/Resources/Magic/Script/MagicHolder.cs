using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagicHolder : MonoBehaviour
{
    [Header("Refer")]
    public MagicInfo magic; //보유한 마법 데이터
    public Collider2D coll;
    public GameObject targetObj = null; //목표 오브젝트

    [Header("Status")]
    public string magicName; //마법 이름 확인
    public Vector3 targetPos = default(Vector3); //목표 위치
    public enum Target { None, Enemy, Player, Both };
    public Target targetType; //마법의 목표 타겟
    private float addDuration; // 추가 유지 시간
    public float AddDuration // 추가 유지 시간
    {
        get { return Mathf.Clamp(addDuration, 0f, 100f); }
        set { addDuration = value; }
    }
    private float multipleSpeed; // 추가 스피드
    public float MultipleSpeed // 추가 스피드
    {
        get { return Mathf.Clamp(multipleSpeed, 1f, 100f); }
        set { multipleSpeed = value; }
    }
    public bool initDone = false; //초기화 완료 여부

    [Header("After Effect")]
    public float setPower = 0f; // 고정된 데미지
    public float knockbackForce = 0; //넉백 파워
    public bool isStop; //정지 여부
    public float poisonTime = 0; // 독 도트 데미지 지속시간
    public float slowTime = 0; //슬로우 지속시간
    public float burnTime = 0; //화상 지속시간
    public float wetTime = 0; //젖음 지속시간
    public float bleedTime = 0; //출혈 지속시간
    public float shockTime = 0; //감전 지속시간
    public float freezeTime = 0; //빙결 지속시간

    private void Awake()
    {
        coll = coll == null ? GetComponentInChildren<Collider2D>() : coll;
    }

    private void OnEnable()
    {
        //초기화
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 초기화 완료 안됨
        initDone = false;

        // 마법 정보 알기 전까지 콜라이더 끄기
        if (coll != null)
            coll.enabled = false;

        yield return new WaitUntil(() => MagicDB.Instance.loadDone);

        //프리팹 이름으로 마법 정보 찾아 넣기
        if (magic == null)
            magic = MagicDB.Instance.GetMagicByName(transform.name.Split('_')[0]);

        //타겟 임의 지정되면 마법 정보에 반영
        if (targetType != Target.None)
            SetTarget(targetType);

        // 마법 정보 찾은 뒤 콜라이더 활성화
        if (coll != null)
            coll.enabled = true;

        //! 마법 이름 확인
        magicName = magic.magicName;

        // 초기화 완료
        initDone = true;
    }

    private void OnDisable()
    {
        //변수 초기화
        addDuration = 0;
        multipleSpeed = 1;
        setPower = 0f;
    }

    public Target GetTarget()
    {
        return targetType;
    }

    public void SetTarget(Target changeTarget)
    {
        //입력된 타겟에 따라 오브젝트 태그 및 레이어 변경
        switch (changeTarget)
        {
            case Target.Enemy:
                gameObject.layer = SystemManager.Instance.layerList.PlayerAttack_Layer;
                break;

            case Target.Player:
                gameObject.layer = SystemManager.Instance.layerList.EnemyAttack_Layer;
                break;

            case Target.Both:
                gameObject.layer = SystemManager.Instance.layerList.AllAttack_Layer;
                break;
        }

        //해당 마법의 타겟 변경
        targetType = changeTarget;

        StartCoroutine(MagicTarget(changeTarget));
    }

    IEnumerator MagicTarget(Target changeTarget)
    {
        // 마법 정보 들어올때까지 대기
        yield return new WaitUntil(() => magic != null);

        //해당 마법의 타겟 변경
        targetType = changeTarget;
    }
}
