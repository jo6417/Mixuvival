using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlotInfo
{
    public int id; //고유 아이디
    public string name; // 이름
    public int grade; // 등급
    public string description; //아이템 설명
    public string priceType; //지불 원소 종류
    public int price; // 가격
}

public class MagicInfo : SlotInfo
{
    //수정 가능한 변수들
    [Header("Configurable")]
    public int magicLevel = 1; //현재 마법 레벨
    public int amount = 0; // 해당 마법 개수 (스택 슬롯에서 사용)
    public bool exist = false; //현재 소환 됬는지 여부
    public float coolCount = 0f; //현재 마법의 남은 쿨타임

    [Header("Info")]
    // public int id; //고유 아이디
    // public int grade; //마법 등급
    // public string description; //마법 설명
    // public string priceType; //마법 구매시 화폐
    // public int price; //마법 구매시 가격
    // public string name; //마법 이름
    public string element_A; //해당 마법을 만들 재료 A
    public string element_B; //해당 마법을 만들 재료 B
    public string castType; //시전 타입
    public bool multiHit; //다단 히트 여부

    [Header("Spec")]
    public float power = 1; //데미지
    public float speed = 1; //투사체 속도 및 쿨타임
    public float range = 1; //범위
    public float duration = 1; //지속시간
    public float critical = 1f; //크리티컬 확률
    public float criticalPower = 1f; //크리티컬 데미지 증가율
    public int pierce = 0; //관통 횟수 및 넉백 계수
    public int atkNum = 0; //투사체 수
    public float coolTime = 0; //쿨타임

    [Header("LevUp")]
    public float powerPerLev;
    public float speedPerLev;
    public float rangePerLev;
    public float durationPerLev;
    public float criticalPerLev;
    public float criticalPowerPerLev;
    public float piercePerLev;
    public float atkNumPerLev;
    public float coolTimePerLev;

    public MagicInfo(MagicInfo magic)
    {
        this.id = magic.id;
        this.magicLevel = magic.magicLevel;
        this.grade = magic.grade;
        this.name = magic.name;
        this.element_A = magic.element_A;
        this.element_B = magic.element_B;
        this.castType = magic.castType;
        this.description = magic.description;
        this.priceType = magic.priceType;
        this.price = magic.price;
        this.multiHit = magic.multiHit;
        this.power = magic.power;
        this.speed = magic.speed;
        this.range = magic.range;
        this.duration = magic.duration;
        this.critical = magic.critical;
        this.criticalPower = magic.criticalPower;
        this.pierce = magic.pierce;
        this.atkNum = magic.atkNum;
        this.coolTime = magic.coolTime;
        this.powerPerLev = magic.powerPerLev;
        this.speedPerLev = magic.speedPerLev;
        this.rangePerLev = magic.rangePerLev;
        this.durationPerLev = magic.durationPerLev;
        this.criticalPerLev = magic.criticalPerLev;
        this.criticalPowerPerLev = magic.criticalPowerPerLev;
        this.piercePerLev = magic.piercePerLev;
        this.atkNumPerLev = magic.atkNumPerLev;
        this.coolTimePerLev = magic.coolTimePerLev;
    }

    public MagicInfo(int id, int grade, string magicName, string element_A, string element_B, string castType, string description, string priceType, int price, bool multiHit,
    float power, float speed, float range, float duration, float critical, float criticalPower, int pierce, int atkNum, float coolTime,
    float powerPerLev, float speedPerLev, float rangePerLev, float durationPerLev, float criticalPerLev, float criticalPowerPerLev, float piercePerLev, float atkNumPerLev, float coolTimePerLev)
    {
        this.id = id;
        this.grade = grade;
        this.name = magicName;
        this.element_A = element_A;
        this.element_B = element_B;
        this.castType = castType;
        this.description = description;
        this.priceType = priceType;
        this.price = price;
        this.multiHit = multiHit;

        this.power = power;
        this.speed = speed;
        this.range = range;
        this.duration = duration;
        this.critical = critical;
        this.criticalPower = criticalPower;
        this.pierce = pierce;
        this.atkNum = atkNum;
        this.coolTime = coolTime;

        this.powerPerLev = powerPerLev;
        this.speedPerLev = speedPerLev;
        this.rangePerLev = rangePerLev;
        this.durationPerLev = durationPerLev;
        this.criticalPerLev = criticalPerLev;
        this.criticalPowerPerLev = criticalPowerPerLev;
        this.piercePerLev = piercePerLev;
        this.atkNumPerLev = atkNumPerLev;
        this.coolTimePerLev = coolTimePerLev;
    }
}

public class ItemInfo : SlotInfo
{
    public int amount = 1; //몇개 갖고 있는지

    [Header("Info")]
    // public int id; //고유 아이디
    // public int grade; //아이템 등급
    // public string description; //아이템 설명
    // public string priceType; //마법 구매시 화폐
    // public int price; //아이템 가격
    // public string name; //아이템 이름
    public string itemType; //아이템 타입 (Gem, Heart, Scroll, Artifact, etc)

    [Header("Buff")] // 능력치 추가 계수 (곱연산 기본값 : 1 / 합연산 기본값 : 0)
    public int projectileNum = 0; // 투사체 개수
    public float hpMax = 1; //최대 체력
    public float power = 1; //마법 공격력
    public float armor = 1; //방어력
    public float moveSpeed = 1; //이동 속도
    public float rateFire = 1; //마법 공격속도
    public float coolTime = 1; //마법 쿨타임
    public float duration = 1; //마법 지속시간
    public float range = 1; //마법 범위
    public float luck = 1; //행운
    public float expGain = 1; //경험치 획득량
    public float moneyGain = 1; //원소젬 획득량

    // 원소 공격력 추가
    public float earth;
    public float fire;
    public float life;
    public float lightning;
    public float water;
    public float wind;

    public ItemInfo(int id, int grade, string itemName, string itemType, string description, string priceType, int price, int projectileNum, float hpMax, float power, float armor, float moveSpeed, float rateFire, float coolTime, float duration, float range, float luck, float expGain, float moneyGain, float earth, float fire, float life, float lightning, float water, float wind)
    {
        this.id = id;
        this.grade = grade;
        this.name = itemName;
        this.itemType = itemType;
        this.description = description;
        this.priceType = priceType;
        this.price = price;

        this.projectileNum = projectileNum;
        this.hpMax = hpMax;
        this.power = power;
        this.armor = armor;
        this.moveSpeed = moveSpeed;
        this.rateFire = rateFire;
        this.coolTime = coolTime;
        this.duration = duration;
        this.range = range;
        this.luck = luck;
        this.expGain = expGain;
        this.moneyGain = moneyGain;

        this.earth = earth;
        this.fire = fire;
        this.life = life;
        this.lightning = lightning;
        this.water = water;
        this.wind = wind;
    }

    public ItemInfo(ItemInfo item)
    {
        this.id = item.id;
        this.grade = item.grade;
        this.name = item.name;
        this.itemType = item.itemType;
        this.description = item.description;
        this.priceType = item.priceType;
        this.price = item.price;
        this.projectileNum = item.projectileNum;
        this.hpMax = item.hpMax;
        this.power = item.power;
        this.armor = item.armor;
        this.moveSpeed = item.moveSpeed;
        this.rateFire = item.rateFire;
        this.coolTime = item.coolTime;
        this.duration = item.duration;
        this.range = item.range;
        this.luck = item.luck;
        this.expGain = item.expGain;
        this.moneyGain = item.moneyGain;
        this.earth = item.earth;
        this.fire = item.fire;
        this.life = item.life;
        this.lightning = item.lightning;
        this.water = item.water;
        this.wind = item.wind;
    }
}