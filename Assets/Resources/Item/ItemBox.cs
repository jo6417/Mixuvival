using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Lean.Pool;
using DG.Tweening;

public class ItemBox : MonoBehaviour
{
    [Header("Interact")]
    [SerializeField] Canvas uiCanvas; // 가격, 상호작용키 안내 UI 캔버스
    [SerializeField] Interacter interacter; //상호작용 콜백 함수 클래스
    [SerializeField] GameObject showKey; //상호작용 키 표시 UI
    [SerializeField] GameObject priceUI; // 가격 UI

    [Header("Refer")]
    [SerializeField] Sprite[] boxSpriteList = new Sprite[2];
    [SerializeField] Sprite[] doorSpriteList = new Sprite[2];
    [SerializeField] SpriteRenderer boxSprite;
    [SerializeField] SpriteRenderer doorSprite;
    [SerializeField] ParticleSystem lightParticle;
    public Collider2D coll;

    [Header("State")]
    int randomType; // 아이템 종류 (마법,샤드,아티팩트)
    public SlotInfo slotInfo;
    public float price;
    public int priceType;
    [SerializeField, ReadOnly] string productName;

    private void Awake()
    {
        // 콜라이더 찾기
        coll = coll != null ? coll : GetComponent<Collider2D>();

        // 상호작용 컴포넌트 찾기
        interacter = interacter != null ? interacter : GetComponent<Interacter>();
    }

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 캔버스 끄기
        uiCanvas.gameObject.SetActive(false);

        // 상호작용 키 UI 끄기
        showKey.SetActive(false);

        // 닫힌 상자 스프라이트로 초기화
        doorSprite.sprite = doorSpriteList[0];
        boxSprite.sprite = boxSpriteList[0];

        // 마법,아이템 DB 모두 로딩 될때까지 대기
        yield return new WaitUntil(() => MagicDB.Instance.loadDone && ItemDB.Instance.loadDone);

        // null 이 아닌 상품이 뽑힐때까지 반복
        while (slotInfo == null)
        {
            // 아티팩트 구현하면 0,3까지 랜덤
            // 상품 종류 아이템,마법,아티팩트 중 랜덤
            randomType = Random.Range(0, 2);

            // 등급별 랜덤으로 전환하기
            // 랜덤 등급 뽑기 
            int randomGrade = Random.Range(0, 7);

            // 해당 상품 내에서 랜덤 id
            switch (randomType)
            {
                // 마법일때
                case 0:
                    slotInfo = MagicDB.Instance.GetRandomMagic(randomGrade);
                    break;
                // 마법 샤드일때
                case 1:
                    slotInfo = ItemDB.Instance.GetRandomItem(ItemDB.ItemType.Shard, randomGrade);
                    break;
                // 아티팩트일때
                case 2:
                    slotInfo = ItemDB.Instance.GetRandomItem(ItemDB.ItemType.Artifact, randomGrade);
                    break;
            }

            yield return null;
        }

        // 해당 상품 이름 확인
        productName = slotInfo.name;

        yield return new WaitUntil(() => slotInfo != null);

        // 가격 타입 갱신
        priceType = MagicDB.Instance.ElementIndex(slotInfo);
        // 상품 가격 갱신
        price = slotInfo.price;

        // 상품 등급 색깔 찾기
        Color gradeColor = MagicDB.Instance.GradeColor[slotInfo.grade];
        // 상품 가격 타입 색깔 찾기
        Color elementColor = MagicDB.Instance.GetElementColor(priceType);

        // 금고 등급색으로 초기화
        boxSprite.color = gradeColor;
        doorSprite.color = gradeColor;
        // 파티클 켜기
        // lightParticle.Play();

        // 해당 아이템에 필요한 재화 종류, 가격 초기화
        priceUI.GetComponentInChildren<Image>().color = elementColor;
        priceUI.GetComponentInChildren<TextMeshProUGUI>().text = price.ToString();

        // 상호작용 트리거 함수 콜백에 연결 시키기
        interacter.interactTriggerCallback += InteractTrigger;
        // 상호작용 함수 콜백에 연결 시키기
        interacter.interactSubmitCallback += InteractSubmit;

        // 캔버스 켜기
        uiCanvas.gameObject.SetActive(true);
    }

    public void InteractTrigger(bool isClose)
    {
        // 상호작용 불가능하면 리턴
        if (!uiCanvas.gameObject.activeSelf)
            return;

        //todo 플레이어 상호작용 키가 어떤 키인지 표시
        // pressKey.text = 

        // 상호작용 가능 거리 접근했을때
        if (isClose)
            // 상호작용 키 UI 나타내기
            showKey.SetActive(true);
        else
            // 상호작용 키 UI 숨기기
            showKey.SetActive(false);
    }

    public void InteractSubmit()
    {
        // 상호작용 불가능하면 리턴
        if (!uiCanvas.gameObject.activeSelf)
            return;

        // 인디케이터 꺼져있으면 리턴
        if (!showKey.activeSelf)
            return;

        // 재화가 가격보다 많을때
        if (PlayerManager.Instance.hasGems[priceType] > price)
        {
            // 플레이어 젬 소모 및 UI 갱신
            PlayerManager.Instance.PayGem(priceType, (int)price);
            UIManager.Instance.UpdateGem(priceType);

            // 드랍할 아이템 오브젝트
            GameObject dropObj = null;

            // 해당 아이템 드랍 (인벤 빈칸 판단 필요 없음)
            switch (randomType)
            {
                // 마법일때
                case 0:
                    //todo 슬롯 아이템 만들기
                    // dropObj = LeanPool.Spawn(, transform.position, Quaternion.identity, SystemManager.Instance.itemPool);
                    break;
                // 마법 샤드일때
                case 1:
                    dropObj = LeanPool.Spawn(ItemDB.Instance.GetItemPrefab(slotInfo.id), transform.position, Quaternion.identity, SystemManager.Instance.itemPool);
                    break;
                // 아티팩트일때
                case 2:
                    dropObj = LeanPool.Spawn(ItemDB.Instance.GetItemPrefab(slotInfo.id), transform.position, Quaternion.identity, SystemManager.Instance.itemPool);
                    break;
            }

            //아이템 리지드 찾기
            Rigidbody2D itemRigid = dropObj.GetComponent<Rigidbody2D>();

            // 랜덤 방향, 랜덤 파워로 아이템 날리기
            itemRigid.velocity = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized * Random.Range(20f, 30f);

            // 랜덤으로 방향 및 속도 결정
            float randomRotate = Random.Range(1f, 3f);
            // 아이템 랜덤 속도로 회전 시키기
            itemRigid.angularVelocity = randomRotate < 2f ? 90f * randomRotate : -90f * randomRotate;

            // 금고 열린 스프라이트로 변경
            boxSprite.sprite = boxSpriteList[1];
            doorSprite.sprite = doorSpriteList[1];

            // 파티클 끄기
            lightParticle.Stop();

            //todo 캔버스 끄기
            // uiCanvas.gameObject.SetActive(false);

            //todo 스폰 콜라이더 벗어나면 디스폰하기
        }
        // 재화가 가격보다 적을때
        else
        {
            // 중복 트윈 방지
            priceUI.transform.DOKill();
            // 가격 좌우로 흔들기
            Vector2 originPos = priceUI.transform.localPosition;
            priceUI.transform.DOPunchPosition(Vector2.right * 30f, 0.5f, 10, 1)
            .OnKill(() =>
            {
                // 원래 위치로 복귀
                priceUI.transform.localPosition = originPos;
            })
            .OnComplete(() =>
            {
                // 원래 위치로 복귀
                priceUI.transform.localPosition = originPos;
            });

            //해당 젬 UI 인디케이터
            UIManager.Instance.GemIndicator(priceType, Color.red);
        }
    }
}