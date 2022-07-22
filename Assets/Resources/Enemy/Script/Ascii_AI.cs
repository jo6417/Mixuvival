using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Lean.Pool;
using System.Linq;

public class Ascii_AI : MonoBehaviour
{
    [Header("State")]
    bool initialDone = false;
    // public NowState nowState;
    // public enum NowState { Idle, Walk, Attack, Rest, Hit, Dead, TimeStop, SystemStop }
    public float farDistance = 10f;
    public float closeDistance = 5f;
    float speed;
    List<int> atkList = new List<int>(); //공격 패턴 담을 변수

    [Header("Refer")]
    public EnemyManager enemyManager;
    EnemyInfo enemy;
    public Image angryGauge; //분노 게이지 이미지
    public TextMeshProUGUI faceText;
    public Transform canvasChildren;
    public Animator anim;
    public Rigidbody2D rigid;
    public SpriteRenderer shadow;

    public LineRenderer leftCable;
    public LineRenderer rightCable;
    public Animator handsAnim;
    public Transform leftHand;
    public Transform rightHand;

    [Header("FallAtk")]
    public Collider2D fallAtkColl; // 해당 컴포넌트를 켜야 fallAtk 타격 가능
    public EnemyAtkTrigger fallRangeTrigger; //엎어지기 범위 내에 플레이어가 들어왔는지 보는 트리거
    public SpriteRenderer fallRangeBackground;
    public SpriteRenderer fallRangeIndicator;
    public ParticleSystem fallDustEffect; //엎어질때 발생할 먼지 이펙트
    public bool fallAtkDone = false; //방금 폴어택 했을때 true, 다른공격 하면 취소

    [Header("LaserAtk")]
    public GameObject LaserPrefab; //발사할 레이저 마법 프리팹
    public GameObject pulseEffect; //laser stop 할때 펄스 이펙트
    MagicInfo laserMagic = null; //발사할 레이저 마법 데이터
    SpriteRenderer laserRange;
    public EnemyAtkTrigger LaserRangeTrigger; //레이저 범위 내에 플레이어가 들어왔는지 보는 트리거
    public TextMeshProUGUI laserText;

    [Header("Cooltime")]
    public float coolCount;
    public float fallCooltime = 1f; //
    public float laserCooltime = 3f; //무궁화꽃 쿨타임
    public float groundPunchCooltime = 5f; // 그라운드 펀치 쿨타임
    public float earthGroundCooltime = 8f; //접지 패턴 쿨타임

    //! 테스트
    [Header("Debug")]
    public TextMeshProUGUI stateText;
    public bool fallAtkAble;
    public bool laserAtkAble;

    private void Awake()
    {
        enemyManager = enemyManager == null ? GetComponentInChildren<EnemyManager>() : enemyManager;
        anim = GetComponent<Animator>();
        rigid = GetComponent<Rigidbody2D>();

        fallRangeBackground = fallRangeTrigger.GetComponent<SpriteRenderer>();
        laserRange = LaserRangeTrigger.GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        StartCoroutine(Initial());
    }

    IEnumerator Initial()
    {
        // 초기화 안됨
        initialDone = false;

        //표정 초기화
        faceText.text = "...";

        rigid.velocity = Vector2.zero; //속도 초기화

        //EnemyDB 로드 될때까지 대기
        yield return new WaitUntil(() => enemyManager.enemy != null);

        //프리팹 이름으로 아이템 정보 찾아 넣기
        if (enemy == null)
            enemy = enemyManager.enemy;

        // fallAtk 공격 비활성화
        fallAtkColl.enabled = false;

        transform.DOKill();

        //애니메이션 스피드 초기화
        if (anim != null)
            anim.speed = 1f;

        // 위치 고정 해제
        rigid.constraints = RigidbodyConstraints2D.FreezeRotation;

        //스피드 초기화
        speed = enemy.speed;

        //공격범위 오브젝트 초기화
        fallRangeBackground.enabled = false;
        laserRange.enabled = false;

        //그림자 초기화
        shadow.gameObject.SetActive(true);

        //EnemyDB 로드 될때까지 대기
        yield return new WaitUntil(() => MagicDB.Instance.loadDone);

        // 레이저 마법 데이터 찾기
        if (laserMagic == null)
        {
            //찾은 마법 데이터로 MagicInfo 새 인스턴스 생성
            laserMagic = new MagicInfo(MagicDB.Instance.GetMagicByName("Explosion"));

            // 강력한 데미지로 고정
            laserMagic.power = 20f;
        }

        // 초기화 완료
        initialDone = true;
    }

    private void Update()
    {
        if (enemy == null || laserMagic == null)
            return;

        // 상태 이상 있으면 리턴
        if (!enemyManager.ManageState())
            return;

        // AI 초기화 완료 안됬으면 리턴
        if (!initialDone)
            return;

        // 행동 관리
        ManageAction();
    }

    void ManageAction()
    {
        // Idle 아니면 리턴
        if (enemyManager.nowAction != EnemyManager.Action.Idle)
            return;

        // 시간 멈추면 리턴
        if (SystemManager.Instance.globalTimeScale == 0f)
            return;

        // 공격 쿨타임 차감
        if (coolCount > 0)
            coolCount -= Time.deltaTime;

        // 플레이어 방향
        Vector2 playerDir = PlayerManager.Instance.transform.position - transform.position;

        // 플레이어와의 거리
        float playerDistance = playerDir.magnitude;

        // 폴어택 가능할때, 쿨타임 가능할때, 마지막 공격이 폴어택이 아닐때
        if (fallRangeTrigger.atkTrigger && coolCount <= 0 && !fallAtkDone)
        {
            //! 거리 확인용
            stateText.text = "Fall : " + playerDistance;

            // 속도 초기화
            enemyManager.rigid.velocity = Vector3.zero;

            // 현재 액션 변경
            enemyManager.nowAction = EnemyManager.Action.Attack;

            // 폴어택 공격
            FalldownAttack();

            // 연속 폴어택 방지
            fallAtkDone = true;

            // 쿨타임 갱신
            coolCount = fallCooltime;

            return;
        }

        // 공격 범위내에 있고 공격 쿨타임 됬을때
        if (playerDistance <= farDistance && playerDistance >= closeDistance && coolCount <= 0)
        {
            //! 거리 확인용
            // stateText.text = "Attack : " + playerDistance;

            // // 속도 초기화
            // enemyManager.rigid.velocity = Vector3.zero;

            // // 현재 액션 변경
            // enemyManager.nowAction = EnemyManager.Action.Attack;

            // //공격 패턴 결정하기
            // ChooseAttack();
        }
        else
        {
            //! 거리 확인용
            stateText.text = "Move : " + playerDistance;

            // 공격 범위 내 위치로 이동
            Move();
        }
    }

    void Move()
    {
        // 걷기 상태로 전환
        enemyManager.nowAction = EnemyManager.Action.Walk;

        //걸을때 표정
        faceText.text = "● ▽ ●";

        // 걷기 애니메이션 시작
        anim.SetBool("isWalk", true);

        //움직일 방향
        Vector2 dir = PlayerManager.Instance.transform.position - transform.position;

        //해당 방향으로 가속
        rigid.velocity = dir.normalized * speed * SystemManager.Instance.globalTimeScale;

        //움직일 방향에따라 회전
        if (dir.x > 0)
        {
            //내부 텍스트 오브젝트들 좌우반전
            if (transform.rotation == Quaternion.Euler(0, 0, 0))
            {
                canvasChildren.rotation = Quaternion.Euler(0, 180, 0);
            }

            transform.rotation = Quaternion.Euler(0, 180, 0);
        }
        else
        {
            //내부 텍스트 오브젝트들 좌우반전
            if (transform.rotation == Quaternion.Euler(0, 180, 0))
            {
                canvasChildren.rotation = Quaternion.Euler(0, 0, 0);
            }

            transform.rotation = Quaternion.Euler(0, 0, 0);
        }

        // idle 상태로 전환
        enemyManager.nowAction = EnemyManager.Action.Idle;
    }

    void ChooseAttack()
    {
        // 공격 상태로 전환
        enemyManager.nowAction = EnemyManager.Action.Attack;

        // 걷기 애니메이션 끝내기
        anim.SetBool("isWalk", false);

        // 이제 폴어택 가능
        fallAtkDone = false;

        //공격 리스트 비우기
        atkList.Clear();

        // fall 콜라이더에 플레이어 있으면 리스트에 fall 공격패턴 담기
        // if (fallRangeTrigger.atkTrigger)
        //     atkList.Add(0);

        // Laser 콜라이더에 플레이어 있으면 리스트에 Laser 공격패턴 담기
        if (LaserRangeTrigger.atkTrigger)
            atkList.Add(1);

        // 가능한 공격 중에서 랜덤 뽑기
        int randAtk = -1;
        if (atkList.Count > 0)
        {
            randAtk = atkList[Random.Range(0, atkList.Count)];
        }

        //! 디버그용 숫자 고정
        // randAtk = 0;

        // 결정된 공격 패턴 실행
        switch (randAtk)
        {
            // case 0:
            //     FalldownAttack();
            //     break;

            case 1:
                StartCoroutine(LaserAtk());
                break;
        }

        // 랜덤 쿨타임 입력
        coolCount = Random.Range(1f, 5f);

        //패턴 리스트 비우기
        atkList.Clear();
    }

    void ToggleCable(bool isPutIn)
    {
        // 케이블 집어넣기
        if (isPutIn)
        {
            // 케이블 머리 부유 애니메이션 끄기
            handsAnim.enabled = false;
            // 양쪽 손을 시작부분으로 빠르게 domove
            leftHand.DOMove(leftCable.transform.GetChild(0).position, 0.2f);
            rightHand.DOMove(rightCable.transform.GetChild(0).position, 0.2f)
            .OnComplete(() =>
            {
                // 케이블 라인 렌더러 끄기
                leftCable.enabled = false;
                rightCable.enabled = false;

                // 케이블 헤드 끄기
                leftCable.transform.GetChild(leftCable.transform.childCount - 1).GetComponent<SpriteRenderer>().enabled = false;
                rightCable.transform.GetChild(leftCable.transform.childCount - 1).GetComponent<SpriteRenderer>().enabled = false;
            });
        }
        // 케이블 꺼내기
        else
        {
            // 케이블 헤드 켜기
            leftCable.transform.GetChild(leftCable.transform.childCount - 1).GetComponent<SpriteRenderer>().enabled = true;
            rightCable.transform.GetChild(leftCable.transform.childCount - 1).GetComponent<SpriteRenderer>().enabled = true;

            // 케이블 라인 렌더러 켜기
            leftCable.enabled = true;
            rightCable.enabled = true;

            // 양쪽 손을 시작부분으로 빠르게 domove
            leftHand.DOMove(new Vector2(-12, 10), 0.2f);
            leftHand.DOMove(new Vector2(12, 10), 0.2f)
            .OnComplete(() =>
            {
                // 케이블 머리 부유 애니메이션 시작
                handsAnim.enabled = true;
            });
        }

    }

    void FalldownAttack()
    {
        // 걷기 애니메이션 종료
        anim.SetBool("isWalk", false);

        // 앞뒤로 흔들려서 당황하는 표정
        faceText.text = "◉ Д ◉";

        // 엎어질 준비 애니메이션 시작
        anim.SetTrigger("FallReady");

        // 케이블 집어넣기
        ToggleCable(true);

        // 엎어질 범위 활성화
        fallRangeBackground.enabled = true;
        fallRangeIndicator.enabled = true;

        // 인디케이터 사이즈 초기화
        fallRangeIndicator.transform.localScale = Vector3.zero;

        // 인디케이터 사이즈 늘리기
        fallRangeIndicator.transform.DOScale(Vector3.one, 1f)
        .SetEase(Ease.Linear)
        .OnComplete(() =>
        {
            //넘어질때 표정
            faceText.text = "> ︿ <";

            //엎어지는 애니메이션
            anim.SetBool("isFallAtk", true);
        });
    }

    void FallAtkEnable()
    {
        // 엎어질 범위 비활성화
        fallRangeBackground.enabled = false;
        fallRangeIndicator.enabled = false;

        // fallAtk 공격 활성화
        fallAtkColl.enabled = true;

        // 먼지 파티클 활성화
        fallDustEffect.gameObject.SetActive(true);

        //일어서기, 휴식 애니메이션 재생
        StartCoroutine(GetUpAnim());
    }

    void FallAtkDisable()
    {
        // fallAtk 공격 비활성화
        fallAtkColl.enabled = false;
    }

    IEnumerator GetUpAnim()
    {
        //일어날때 표정
        faceText.text = "x  _  x";

        // 엎어진채로 1초 대기
        yield return new WaitForSeconds(1f);

        // 일어서기, 휴식 애니메이션 시작
        anim.SetBool("isFallAtk", false);

        StartCoroutine(RestAnim());
    }

    IEnumerator LaserAtk()
    {
        // print("laser atk");

        // 레이저 준비 애니메이션 시작
        anim.SetTrigger("LaserReady");

        // 모니터에 화를 참는 얼굴
        faceText.text = "◣` ︿ ´◢"; //TODO 얼굴 바꾸기

        // 동시에 점점 빨간색 게이지가 차오름
        angryGauge.fillAmount = 0f;
        DOTween.To(() => angryGauge.fillAmount, x => angryGauge.fillAmount = x, 1f, 2f);

        // 동시에 공격 범위 표시
        // 엎어질 범위 활성화 및 반짝거리기
        // laserRange.enabled = true;
        // Color originColor = new Color(1, 0, 0, 0.2f);
        // laserRange.color = originColor;

        // laserRange.DOColor(new Color(1, 0, 0, 0f), 1f)
        // .SetEase(Ease.InOutFlash, 5, 0)
        // .OnComplete(() =>
        // {
        //     laserRange.color = originColor;
        // });

        //펄스 이펙트 활성화
        pulseEffect.SetActive(true);

        //게이지 모두 차오르면 
        yield return new WaitUntil(() => angryGauge.fillAmount >= 1f);

        // 무궁화 애니메이션 시작
        anim.SetTrigger("LaserSet");

        //채워질 글자
        string targetText = "무궁화꽃이\n피었습니다";

        //텍스트 비우기
        laserText.text = "";

        //공격 준비 글자 채우기
        float delay = 0.2f;
        while (laserText.text.Length < targetText.Length)
        {
            laserText.text = targetText.Substring(0, laserText.text.Length + 1);

            //글자마다 랜덤 딜레이 갱신 
            delay = Random.Range(0.2f, 0.5f);
            delay = 0.1f;

            yield return new WaitForSeconds(delay);
        }

        //펄스 이펙트 활성화
        pulseEffect.SetActive(true);

        // 공격 준비 끝나면 stop 띄우기
        laserText.text = "STOP";

        // Stop 표시될때까지 대기
        yield return new WaitUntil(() => laserText.text == "STOP");

        // 감시 애니메이션 시작
        anim.SetBool("isLaserWatch", true);

        // 노려보는 얼굴
        faceText.text = "⚆`  ︿  ´⚆";

        //몬스터 스폰 멈추기
        EnemySpawn.Instance.spawnSwitch = false;
        // 모든 몬스터 멈추기
        List<EnemyManager> enemys = SystemManager.Instance.enemyPool.GetComponentsInChildren<EnemyManager>().ToList();
        foreach (EnemyManager enemyManager in enemys)
        {
            // 보스 본인이 아닐때
            if (enemyManager != this.enemyManager)
                enemyManager.stopCount = 3f;
        }

        //감시 시간
        float watchTime = Time.time;
        //플레이어 현재 위치
        Vector3 playerPos = PlayerManager.Instance.transform.position;

        print("watch start : " + Time.time);

        // 플레이어 움직이는지 감시
        while (Time.time - watchTime < 3)
        {
            //플레이어 위치 변경됬으면 레이저 발사
            if (playerPos != PlayerManager.Instance.transform.position)
            {
                //움직이면 레이저 발사 애니메이션 재생
                anim.SetBool("isLaserAtk", true);

                // 레이저 쏠때 얼굴
                faceText.text = "◣` ︿ ´◢";

                // 플레이어에게 양쪽눈에서 레이저 여러번 발사
                int whitchEye = 0;
                for (int i = 0; i < 10; i++)
                {
                    ShotLaser(LaserRangeTrigger.transform.GetChild(whitchEye));

                    //쏘는 눈 변경
                    whitchEye = whitchEye == 0 ? 1 : 0;

                    //레이저 쏘는 시간 대기
                    yield return new WaitForSeconds(0.3f);
                }

                //레이저 쏘는 시간 대기
                yield return new WaitForSeconds(2f);

                //레이저 발사 종료
                anim.SetBool("isLaserAtk", false);

                // while문 탈출
                break;
            }

            yield return new WaitForSeconds(Time.deltaTime);
        }

        print("watch end : " + Time.time);

        // 감시 종료
        anim.SetBool("isLaserWatch", false);

        //몬스터 스폰 재개
        EnemySpawn.Instance.spawnSwitch = true;
        // 모든 몬스터 움직임 재개
        SystemManager.Instance.globalTimeScale = 1f;

        //휴식 시작
        StartCoroutine(RestAnim());
    }

    void ShotLaser(Transform shotter)
    {
        // print("레이저 발사");

        //레이저 생성
        GameObject magicObj = LeanPool.Spawn(LaserPrefab, shotter.position, Quaternion.identity, SystemManager.Instance.magicPool);

        Explosion laser = magicObj.GetComponent<Explosion>();
        // 레이저 발사할 오브젝트 넣기
        laser.startObj = shotter;

        MagicHolder magicHolder = magicObj.GetComponent<MagicHolder>();
        // magic 데이터 넣기
        magicHolder.magic = laserMagic;

        // 타겟을 플레이어로 전환
        magicHolder.SetTarget(MagicHolder.Target.Player);
        // 레이저 목표지점 targetPos 넣기
        magicHolder.targetPos = PlayerManager.Instance.transform.position;
    }

    IEnumerator RestAnim()
    {
        //휴식 시작
        anim.SetBool("isRest", true);
        //휴식할때 표정
        faceText.text = "x  _  x";

        // 쿨타임 0 될때까지 대기
        yield return new WaitForSeconds(2f);
        // yield return new WaitUntil(() => coolCount <= 0);

        //휴식 끝
        anim.SetBool("isRest", false);

        // 쿨타임 끝나면 idle로 전환, 쿨타임 차감 시작
        enemyManager.nowAction = EnemyManager.Action.Idle;

        // 케이블 꺼내기
        ToggleCable(false);
    }

    // void SetIdle()
    // {
    //     //로딩중 텍스트 애니메이션
    //     StartCoroutine(LoadingText());
    // }

    // IEnumerator LoadingText()
    // {
    //     string targetText = "...";

    //     faceText.text = targetText;

    //     // 쿨타임 중일때
    //     while (coolCount > 0)
    //     {
    //         if (faceText.text.Length < targetText.Length)
    //             faceText.text = targetText.Substring(0, faceText.text.Length + 1);
    //         else
    //             faceText.text = targetText.Substring(0, 0);

    //         yield return new WaitForSeconds(0.2f);
    //     }

    //     // 쿨타임 끝나면 idle로 전환
    //     enemyManager.nowAction = EnemyManager.Action.Idle;
    // }
}
