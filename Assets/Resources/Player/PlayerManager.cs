using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lean.Pool;
using DG.Tweening;

public class PlayerManager : MonoBehaviour
{
    #region Singleton
    private static PlayerManager instance;
    public static PlayerManager Instance
    {
        get
        {
            if (instance == null)
            {
                var obj = FindObjectOfType<PlayerManager>();
                if (obj != null)
                {
                    instance = obj;
                }
                else
                {
                    var newObj = new GameObject().AddComponent<PlayerManager>();
                    instance = newObj;
                }
            }
            return instance;
        }
    }
    #endregion

    [Header("<Refer>")]
    public GameObject mobSpawner;
    private Animator animator;
    private SpriteRenderer sprite;
    // public GameObject levelupPopup;
    public Rigidbody2D rigid;
    public Vector3 lastDir; //마지막 바라봤던 방향

    [Header("<Stat>")] //기본 스탯
    public float hpMax = 20; // 최대 체력
    public float hpNow = 20; // 체력
    public float Level = 1; //레벨
    public float ExpMax = 5; // 경험치 최대치
    public float ExpNow = 0; // 현재 경험치
    public float moveSpeed = 5; //이동속도

    public int projectileNum = 0; // 투사체 개수
    public int pierce = 0; // 관통 횟수
    public float power = 1; //마법 공격력
    public float armor = 1; //방어력
    public float knockbackForce = 1; //넉백 파워
    public float speed = 1; //마법 공격속도
    public float coolTime = 1; //마법 쿨타임
    public float duration = 1; //마법 지속시간
    public float range = 1; //마법 범위
    public float luck = 1; //행운
    public float expGain = 1; //경험치 획득량
    public float moneyGain = 1; //원소젬 획득량

    //원소 공격력
    public float earth_atk = 1;
    public float fire_atk = 1;
    public float life_atk = 1;
    public float lightning_atk = 1;
    public float water_atk = 1;
    public float wind_atk = 1;

    [Header("<Damaged>")]
    public float HitDelay = 0.1f; //피격 무적시간
    public float flickSpeed = 10f; //깜빡이는 속도
    public float ShakeTime;
    public float ShakeIntensity;
    private Color originColor;
    private bool isDamage = false;

    [Header("<Buff>")] // 능력치 추가 계수 (곱연산 기본값 : 1 / 합연산 기본값 : 0)
    public float hpMax_buff = 1; // 최대 체력 계수
    public float power_buff = 1; //마법 공격력
    public float armor_buff = 1; //방어력
    public float moveSpeed_buff = 0; // 이동 속도
    public int projectileNum_buff = 0; // 투사체 개수
    public float rateFire_buff = 1; //마법 공격속도
    public float coolTime_buff = 1; //마법 쿨타임
    public float duration_buff = 1; //마법 지속시간
    public float range_buff = 1; //마법 범위
    public float luck_buff = 1; //행운
    public float expGain_buff = 1; //경험치 획득량
    public float moneyGain_buff = 1; //원소젬 획득량

    //원소 공격력 버프
    public float earth_buff = 1;
    public float fire_buff = 1;
    public float life_buff = 1;
    public float lightning_buff = 1;
    public float water_buff = 1;
    public float wind_buff = 1;

    [Header("<Pocket>")]
    public List<MagicInfo> hasMagics = new List<MagicInfo>(); //플레이어가 가진 마법
    public List<ItemInfo> hasItems = new List<ItemInfo>(); //플레이어가 가진 아이템
    public List<int> hasGems = new List<int>(6); //플레이어가 가진 원소젬

    void Start()
    {
        rigid = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sprite = GetComponent<SpriteRenderer>();
        originColor = sprite.color;

        // 원소젬 UI 업데이트
        for (int i = 0; i < 6; i++)
        {
            // hasGems.Add(0);
            UIManager.Instance.UpdateGem(i);
        }

        //경험치 최대치 갱신
        ExpMax = Level * Level + 5;

        //능력치 초기화
        UIManager.Instance.InitialStat();
    }

    private void Update()
    {
        //카메라 따라오기
        Camera.main.transform.position = transform.position + new Vector3(0, 0, -10);

        //몬스터 스포너 따라오기
        mobSpawner.transform.position = transform.position;

        //이동
        Move();
    }

    void Move()
    {
        //이동 입력값 받기
        Vector2 dir = Vector2.zero;
        float horizonInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        // x축 이동
        if (horizonInput != 0)
        {
            dir.x = horizonInput;

            if (horizonInput > 0)
            {
                transform.rotation = Quaternion.Euler(0, 0, 0);
            }
            else
            {
                transform.rotation = Quaternion.Euler(0, 180, 0);
            }
        }

        // y축 이동
        if (verticalInput != 0)
        {
            dir.y = verticalInput;
        }

        // 애니메이터
        if (horizonInput == 0 && verticalInput == 0)
        {
            animator.SetBool("isWalking", false);
        }
        else
        {
            animator.SetBool("isWalking", true);
        }

        dir.Normalize();
        rigid.velocity = moveSpeed * dir;

        //마지막 방향 기억
        if(dir != Vector2.zero)
        lastDir = dir;
    }

    private void OnCollisionStay2D(Collision2D other)
    {
        //적에게 충돌
        if (other.gameObject.CompareTag("Enemy") && !isDamage)
        {
            // print("적 충돌");

            EnemyInfo enemy = other.gameObject.GetComponent<EnemyAI>().enemy;

            //피격 딜레이 무적
            IEnumerator hitDelay = HitDelayCoroutine();
            StartCoroutine(hitDelay);

            Damage(enemy.power);
        }
    }

    //HitDelay만큼 시간 지난후 피격무적시간 끝내기
    IEnumerator HitDelayCoroutine()
    {
        isDamage = true;

        sprite.color = new Color(255, 20, 20); //스프라이트 색 변환

        yield return new WaitForSeconds(HitDelay);

        sprite.color = originColor; //원래 색으로 복구

        isDamage = false;
    }

    void Damage(float damage)
    {
        // 체력 감소
        hpNow -= damage;

        //체력 0 이하가 되면 사망
        if (hpNow <= 0)
        {
            hpNow = 0;
            // print("Game Over");
            // Dead();
        }

        UIManager.Instance.UpdateHp(); //체력 UI 업데이트        
    }

    void Dead()
    {
        // 시간 멈추기
        Time.timeScale = 0;

        //TODO 게임오버 UI 띄우기
        // gameOverUI.SetActive(true);
    }

    public void GetItem(ItemInfo getItem)
    {
        // print(getItem.itemType + " : " + getItem.itemName);

        // 아이템이 스크롤일때
        if (getItem.itemType == "Scroll")
        {
            // print("아이템 합성");

            // 아이템 합성 메뉴 띄우기
            UIManager.Instance.PopupUI(UIManager.Instance.magicMixUI);
        }

        if (getItem.itemType == "Artifact")
        {
            // print("아티팩트 획득");

            //보유하지 않은 아이템일때
            if (!hasItems.Exists(x => x.id == getItem.id))
            {
                // 플레이어 보유 아이템에 해당 아이템 추가하기
                hasItems.Add(getItem);
            }

            //보유한 아이템의 개수만 늘려주기
            hasItems.Find(x => x.id == getItem.id).amount++;
            // getItem.hasNum++;

            // 보유한 모든 아이템 아이콘 갱신
            UIManager.Instance.UpdateItems();

            // 모든 아이템 버프 갱신
            buffUpdate();
        }
    }

    public void GetHeal(int amount)
    {
        //플레이어 체력 회복하기
        hpNow += amount;
        print(hpNow + ":" + amount);

        //초과 회복 방지
        if (hpNow > hpMax)
            hpNow = hpMax;

        //UI 업데이트
        UIManager.Instance.UpdateHp();
    }

    public void GetMagic(MagicInfo getMagic)
    {
        //보유하지 않은 마법일때
        if (!hasMagics.Exists(x => x.id == getMagic.id))
        {
            // 플레이어 보유 마법에 해당 마법 추가하기
            hasMagics.Add(getMagic);
        }

        //보유한 마법의 레벨 올리기
        hasMagics.Find(x => x.id == getMagic.id).magicLevel++;

        // 마법 캐스팅 다시 시작
        CastMagic.Instance.ReCastMagics();

        // 보유한 모든 마법 아이콘 갱신
        UIManager.Instance.UpdateMagics();
    }

    void buffUpdate()
    {
        //플레이어 능력치 기본값으로 초기화
        projectileNum = projectileNum - projectileNum_buff;
        hpMax = hpMax / hpMax_buff;
        power = power / power_buff;
        armor = armor / armor_buff;
        speed = speed / rateFire_buff;
        coolTime = coolTime / coolTime_buff;
        duration = duration / duration_buff;
        range = range / range_buff;
        luck = luck / luck_buff;
        expGain = expGain / expGain_buff;
        moneyGain = moneyGain / moneyGain_buff;

        earth_atk = earth_atk / earth_buff;
        fire_atk = fire_atk / fire_buff;
        life_atk = life_atk / life_buff;
        lightning_atk = lightning_atk / lightning_buff;
        water_atk = water_atk / water_buff;
        wind_atk = wind_atk / wind_buff;

        //버프 기본값으로 초기화
        projectileNum_buff = 0;
        hpMax_buff = 1;
        power_buff = 1;
        armor_buff = 1;
        rateFire_buff = 1;
        coolTime_buff = 1;
        duration_buff = 1;
        range_buff = 1;
        luck_buff = 1;
        expGain_buff = 1;
        moneyGain_buff = 1;

        earth_buff = 1;
        fire_buff = 1;
        life_buff = 1;
        lightning_buff = 1;
        water_buff = 1;
        wind_buff = 1;

        // 현재 소지한 아이템의 모든 버프 계수 합산하기
        foreach (var item in hasItems)
        {
            projectileNum_buff += item.projectileNum * item.amount; // 투사체 개수 버프
            hpMax_buff += item.hpMax * item.amount; //최대체력 버프
            power_buff += item.power * item.amount; //마법 공격력 버프
            armor_buff += item.armor * item.amount; //방어력 버프
            rateFire_buff += item.rateFire * item.amount; //마법 공격속도 버프
            coolTime_buff += item.coolTime * item.amount; //마법 쿨타임 버프
            duration_buff += item.duration * item.amount; //마법 지속시간 버프
            range_buff += item.range * item.amount; //마법 범위 버프
            luck_buff += item.luck * item.amount; //행운 버프
            expGain_buff += item.expGain * item.amount; //경험치 획득량 버프
            moneyGain_buff += item.moneyGain * item.amount; //원소젬 획득량 버프
            earth_buff += item.earth * item.amount;
            fire_buff += item.fire * item.amount;
            life_buff += item.life * item.amount;
            lightning_buff += item.lightning * item.amount;
            water_buff += item.water * item.amount;
            wind_buff += item.wind * item.amount;
        }

        //플레이어 능력치에 다시 합산
        projectileNum = projectileNum + projectileNum_buff;
        hpMax = hpMax * hpMax_buff;
        power = power * power_buff;
        armor = armor * armor_buff;
        speed = speed * rateFire_buff;
        coolTime = coolTime * coolTime_buff;
        duration = duration * duration_buff;
        range = range * range_buff;
        luck = luck * luck_buff;
        expGain = expGain * expGain_buff;
        moneyGain = moneyGain * moneyGain_buff;

        earth_atk = earth_atk * earth_buff;
        fire_atk = fire_atk * fire_buff;
        life_atk = life_atk * life_buff;
        lightning_atk = lightning_atk * lightning_buff;
        water_atk = water_atk * water_buff;
        wind_atk = wind_atk * wind_buff;

        // pause 메뉴 스탯UI에 스탯 반영하기
        UIManager.Instance.UpdateStat();
    }

    public void AddGem(ItemInfo item, int amount)
    {
        // 어떤 원소든지 젬 개수만큼 경험치 증가
        ExpNow += amount;

        //경험치 다 찼을때
        if (ExpNow == ExpMax)
        {
            //레벨업
            Levelup();
        }
        // print(item.itemName.Split(' ')[0]);

        // 가격 타입으로 젬 타입 인덱스로 반환
        int gemTypeIndex = System.Array.FindIndex(MagicDB.Instance.elementNames, x => x == item.priceType);

        //해당 젬 갯수 올리기
        hasGems[gemTypeIndex] = hasGems[gemTypeIndex] + amount;
        // print(hasGems[gemTypeIndex] + " : " + amount);

        //해당 젬 UI 인디케이터
        UIManager.Instance.GemIndicator(gemTypeIndex);

        // UI 업데이트
        UIManager.Instance.UpdateGem(gemTypeIndex);

        //경험치 및 레벨 갱신
        UIManager.Instance.UpdateExp();
    }

    void Levelup()
    {
        //레벨업
        Level++;

        //경험치 초기화
        ExpNow = 0;

        //경험치 최대치 갱신
        ExpMax = Level * Level + 5;

        // 시간 멈추기
        Time.timeScale = 0;

        // 팝업 선택메뉴 띄우기
        UIManager.Instance.PopupUI(UIManager.Instance.magicMixUI);
        // levelupPopup.SetActive(true);
    }

    //원소젬 지불하기
    public void PayGem(int gemIndex, int price)
    {

        hasGems[gemIndex] -= price;

        //젬 UI 업데이트
        UIManager.Instance.UpdateGem(gemIndex);
    }
}