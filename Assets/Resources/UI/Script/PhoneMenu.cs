using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Lean.Pool;
using DG.Tweening;
using UnityEngine.EventSystems;
using DanielLochner.Assets.SimpleScrollSnap;

public class PhoneMenu : MonoBehaviour
{
    #region Singleton
    private static PhoneMenu instance;
    public static PhoneMenu Instance
    {
        get
        {
            if (instance == null)
            {
                var obj = FindObjectOfType<PhoneMenu>();
                if (obj != null)
                {
                    instance = obj;
                }
                // else
                // {
                //     var newObj = new GameObject().AddComponent<PhoneMenu>();
                //     instance = newObj;
                // }
            }
            return instance;
        }
    }
    #endregion

    [Header("Refer")]
    public SimpleScrollSnap screenScroll; // 화면 스크롤
    public GameObject phonePanel; // 핸드폰 화면 패널
    [SerializeField] GameObject invenScreen; // 머지 페이지
    [SerializeField] GameObject recipeScreen; // 레시피 페이지
    public Button recipeBtn;
    public Button backBtn;
    public Button homeBtn;
    public SpriteRenderer lightScreen; // 폰 스크린 전체 빛내는 HDR 이미지
    public Image blackScreen; // 폰 작아질때 검은 이미지로 화면 가리기
    public GameObject loadingPanel; //로딩 패널, 로딩중 뒤의 버튼 상호작용 막기
    bool btnsInteractable = true; // 버튼 상호작용 가능 여부

    [Header("Effect")]
    public Vector3 phonePosition; //핸드폰일때 위치 기억
    public Vector3 phoneRotation; //핸드폰일때 회전값 기억
    public Vector3 phoneScale; //핸드폰일때 고정된 스케일
    public Vector3 UIPosition; //팝업일때 위치
    public SlicedFilledImage backBtnFill; //뒤로가기 버튼
    float backBtnCount; //백버튼 더블클릭 카운트

    [Header("Chat List")]
    public GameObject chatPrefab; // 채팅 프리팹
    public ScrollRect chatScroll; // 채팅 스냅 스크롤
    [SerializeField] private RectTransform chatContentRect;
    public float sumChatHeights; // 채팅 스크롤 content의 높이

    [Header("Inventory")]
    public Transform invenParent; // 인벤토리 슬롯들 부모 오브젝트
    public List<InventorySlot> invenSlots = new List<InventorySlot>(); //각각 슬롯 오브젝트
    public InventorySlot nowSelectSlot; // 현재 선택된 슬롯
    public SlotInfo nowSelectSlotInfo; // 현재 선택된 슬롯 정보
    public Image nowSelectIcon; //마우스 따라다닐 아이콘
    RectTransform nowSelectIconRect;

    [Header("Merge Panel")]
    public Transform mergePanel;
    public RectTransform L_MergeSlotRect; // 왼쪽 재료 슬롯
    public RectTransform R_MergeSlotRect; // 오른쪽 재료 슬롯
    public InventorySlot mergedSlot; // 합성된 마법 슬롯
    public GameObject plusIcon; // 가운데 플러스 아이콘
    public ParticleSystem mergeBeforeEffect; // 합성 준비 이펙트
    public Image mergeAfterEffect; // 합성 완료 이펙트

    [Header("Recipe List")]
    public SimpleScrollSnap recipeScroll; // 레시피 슬롯 스크롤
    public GameObject recipePrefab; // 단일 레시피 프리팹
    public bool recipeInit = false;

    [Header("Random Panel")]
    // public TextMeshProUGUI usbAllNum; // USB 총 개수 표시 텍스트
    public SimpleScrollSnap usbScroll; // USB 슬롯 스크롤
    public Transform anim_USB_Slot; // 애니메이션용 usb 아이콘
    public CanvasGroup randomScreen; // 뽑기 스크린
    public SimpleScrollSnap randomScroll; // USB 뽑기 랜덤 스크롤
    public Transform magicSlotPrefab; // 랜덤 마법 아이콘 프리팹
    public Animator slotRayEffect; // 슬롯 팡파레 이펙트
    public ParticleSystem getMagicEffect; // 뽑은 마법 획득 이펙트
    // MagicInfo getMagic = null; // 랜덤 획득 마법 정보
    public float randomScrollSpeed = 15f; // 뽑기 스크롤 속도
    public float minScrollTime = 3f; // 슬롯머신 최소 시간
    public float maxScrollTime = 5f; // 슬롯머신 최대 시간

    private void Awake()
    {
        // 인벤토리 슬롯 컴포넌트 모두 저장
        for (int i = 0; i < invenParent.childCount; i++)
        {
            InventorySlot invenSlot = invenParent.GetChild(i).GetComponent<InventorySlot>();

            invenSlots.Add(invenSlot);
        }

        // // 액티브 슬롯 오브제트 모두 저장
        // for (int i = 0; i < PlayerManager.Instance.activeParent.childCount; i++)
        // {
        //     invenSlots.Add(PlayerManager.Instance.activeParent.GetChild(i).GetComponent<InventorySlot>());
        // }

        // 선택된 마법 rect 찾기
        nowSelectIconRect = nowSelectIcon.transform.parent.GetComponent<RectTransform>();

        // 키 입력 정리
        StartCoroutine(InputInit());

        // 스크롤 컨텐츠 모두 비우기
        SystemManager.Instance.DestroyAllChild(recipeScroll.Content);

        // 인벤토리 슬롯들 배열 참조
        // inventory = PlayerManager.Instance.inventory;
    }

    IEnumerator InputInit()
    {
        // 플레이어 초기화 대기
        yield return new WaitUntil(() => PlayerManager.Instance.initFinish);

        // 방향키 입력
        UIManager.Instance.UI_Input.UI.NavControl.performed += val => NavControl(val.ReadValue<Vector2>());
        // 마우스 위치 입력
        UIManager.Instance.UI_Input.UI.MousePosition.performed += val => MousePos();
        // 마우스 클릭
        UIManager.Instance.UI_Input.UI.Click.performed += val =>
        {
            if (gameObject.activeSelf)
                StartCoroutine(CancelMoveItem());
        };

        // 스마트폰 버튼 입력
        UIManager.Instance.UI_Input.UI.PhoneMenu.performed += val =>
        {
            // 로딩 패널 꺼져있을때, 머지 선택 모드 아닐때
            if (!loadingPanel.activeSelf)
            {
                //백 버튼 액션 실행
                StartCoroutine(BackBtnAction());
            }
        };

        // 핸드폰 패널 끄기
        phonePanel.SetActive(false);
        // 핸드폰 캔버스 끄기
        // gameObject.SetActive(false);
    }

    IEnumerator CancelMoveItem()
    {
        // 클릭시 select 오브젝트 바뀔때까지 1프레임 대기
        yield return new WaitForSeconds(Time.deltaTime);

        //마우스에 아이콘 들고 있을때
        if (nowSelectIcon.enabled)
        {
            // null 선택했을때, 메뉴버튼, 백버튼, 홈버튼 클릭했을때
            if (EventSystem.current.currentSelectedGameObject == null
            || EventSystem.current.currentSelectedGameObject == recipeBtn.gameObject
            || EventSystem.current.currentSelectedGameObject == backBtn.gameObject
            || EventSystem.current.currentSelectedGameObject == homeBtn.gameObject)
            {
                //커서 및 빈 스택 슬롯 초기화 하기
                CancelSelectSlot();
            }
        }
    }

    // 방향키 입력되면 실행
    void NavControl(Vector2 arrowDir)
    {
        // 머지 패널 꺼져있으면 리턴
        if (!gameObject.activeSelf)
            return;

        //마우스에 아이콘 들고 있을때
        if (nowSelectIcon.enabled)
            //커서 및 빈 스택 슬롯 초기화 하기
            CancelSelectSlot();
    }

    // 마우스 위치 입력되면 실행
    public void MousePos()
    {
        // 머지 패널 꺼져있으면 리턴
        if (!gameObject.activeSelf)
            return;

        // print(mousePosInput);

        if (nowSelectIcon.enabled)
        {
            // 캔버스 스케일을 해상도로 나눈 비율을 곱해서 마우스 위치값 보정
            Vector3 mousePos = UIManager.Instance.nowMousePos * (GetComponent<CanvasScaler>().referenceResolution.x / Screen.width);
            mousePos.z = 0;

            // 선택된 마법 아이콘 마우스 따라다니기
            nowSelectIconRect.anchoredPosition = mousePos;
        }
    }

    private void OnEnable()
    {
        //초기화
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        //시간 멈추기
        Time.timeScale = 0f;

        //마법 DB 로딩 대기
        yield return new WaitUntil(() => MagicDB.Instance.loadDone);

        // 인벤토리 세팅
        Set_Inventory();

        // 레시피 리스트 세팅
        Set_Recipe();

        // USB 애니메이션용 슬롯 위치 초기화
        anim_USB_Slot.position = usbScroll.transform.position;

        // 선택 아이콘 끄기
        nowSelectIcon.enabled = false;

        //레시피 버튼 켜기
        recipeBtn.interactable = true;
        //뒤로 버튼 켜기
        backBtn.interactable = true;
        //홈 버튼 켜기
        homeBtn.interactable = true;

        // 팡파레 이펙트 끄기
        slotRayEffect.gameObject.SetActive(false);

        // 뽑기 슬롯 이펙트 끄기
        getMagicEffect.gameObject.SetActive(false);

        // 뽑기 스크린 끄기
        randomScreen.gameObject.SetActive(false);

        // 합성 슬롯 비활성화
        // mergedSlot.gameObject.SetActive(false);
        mergedSlot.transform.localScale = Vector3.zero;

        // 각각 스크린 켜기
        invenScreen.SetActive(true);
        recipeScreen.SetActive(true);

        // 뽑기 스크린 투명하게 숨기기
        randomScreen.alpha = 0f;
    }

    public IEnumerator OpenPhone()
    {
        //초기화
        StartCoroutine(Init());

        // 휴대폰 로딩 화면으로 가리기
        LoadingToggle(true);
        blackScreen.color = new Color(70f / 255f, 70f / 255f, 70f / 255f, 1);

        //위치 기억하기
        phonePosition = CastMagic.Instance.transform.position;
        //회전값 기억하기
        phoneRotation = CastMagic.Instance.transform.rotation.eulerAngles;

        //카메라 위치
        Vector3 camPos = SystemManager.Instance.camParent.position;
        camPos.z = 0;

        // 화면 라이트 끄기
        lightScreen.DOColor(new Color(1f, 1f, 1f, 0f), 0.4f)
        .SetUpdate(true);

        float moveTime = 0.8f;

        // 팝업UI 위치,회전,스케일로 복구하기
        CastMagic.Instance.transform.DOMove(camPos + UIPosition, moveTime)
        .SetUpdate(true);
        CastMagic.Instance.transform.DOScale(Vector3.one, moveTime)
        .SetUpdate(true);
        CastMagic.Instance.transform.DORotate(new Vector3(0, 720f - phoneRotation.y, 0), moveTime, RotateMode.WorldAxisAdd)
        .SetUpdate(true);

        // 스마트폰 움직이는 트랜지션 끝날때까지 대기
        yield return new WaitUntil(() => CastMagic.Instance.transform.localScale == Vector3.one);

        // 핸드폰 화면 패널 켜기
        phonePanel.SetActive(true);

        // 검은화면 투명하게
        blackScreen.DOColor(Color.clear, 0.2f)
        .SetUpdate(true)
        .OnComplete(() =>
        {
            // 휴대폰 로딩 화면 끄기
            LoadingToggle(false);
        });

        #region Interact_On

        // 인벤 슬롯 모두 켜기
        foreach (InventorySlot invenSlot in invenSlots)
        {
            invenSlot.slotButton.interactable = true;
        }

        // 첫번째 인벤 슬롯 선택하기
        UIManager.Instance.lastSelected = invenSlots[0].slotButton;
        UIManager.Instance.targetOriginColor = invenSlots[0].GetComponent<Image>().color;

        // //선택된 슬롯 네비 설정
        // Navigation nav = selectedSlot.navigation;
        // nav.selectOnUp = stackObjSlots[3].GetComponent<Button>().FindSelectable(Vector3.up);
        // selectedSlot.navigation = nav;

        #endregion

        // TODO 게임 시작할때 액티브 슬롯에 스킬 이미 들어간채로 시작
    }

    void LoadingToggle(bool isLoading)
    {
        UIManager.Instance.phoneLoading = isLoading;

        loadingPanel.SetActive(isLoading);
    }

    void Set_Inventory()
    {
        // 머지 리스트에 있는 마법들 머지 보드에 나타내기
        for (int i = 0; i < invenSlots.Count; i++)
        {
            // 각 슬롯 세팅
            invenSlots[i].Set_Slot();
        }
    }

    public IEnumerator MergeMagic(MagicInfo mergedMagic, bool isNew = false)
    {
        //todo 상호작용 비활성화
        InteractBtnsToggle(false);

        // 좌측 슬롯 원래 위치 저장
        Vector3 L_originPos = L_MergeSlotRect.anchoredPosition;
        // 우측 슬롯 원래 위치 저장
        Vector3 R_originPos = R_MergeSlotRect.anchoredPosition;
        // 가운데 합성된 슬롯 위치
        Vector3 centerPos = mergedSlot.GetComponent<RectTransform>().anchoredPosition;
        // 슬롯 원래 사이즈 저장
        Vector3 originScale = L_MergeSlotRect.transform.localScale;

        // 합성 준비 이펙트 재생
        mergeBeforeEffect.Play();

        // 플러스 아이콘 줄이기
        plusIcon.transform.localScale = Vector3.zero;

        // 좌측 슬롯 가운데로 이동
        L_MergeSlotRect.DOAnchorPos(centerPos, 1f)
        .SetEase(Ease.InBack)
        .SetUpdate(true);
        // 우측 슬롯 가운데로 이동
        R_MergeSlotRect.DOAnchorPos(centerPos, 1f)
        .SetEase(Ease.InBack)
        .SetUpdate(true);

        // 좌측 슬롯 작아지기
        L_MergeSlotRect.DOScale(Vector3.zero, 1f)
        .SetDelay(0.4f)
        .SetUpdate(true);
        // 우측 슬롯 작아지기
        R_MergeSlotRect.DOScale(Vector3.zero, 1f)
        .SetDelay(0.4f)
        .SetUpdate(true);

        // 합성된 마법 아이콘 넣기
        mergedSlot.slotIcon.sprite = MagicDB.Instance.GetMagicIcon(mergedMagic.id);
        // 합성된 마법 프레임 색 넣기
        mergedSlot.slotFrame.color = MagicDB.Instance.GradeColor[mergedMagic.grade];
        // 합성된 마법 레벨 합산
        mergedSlot.slotLevel.GetComponentInChildren<TextMeshProUGUI>(true).text = "Lv. " + mergedMagic.magicLevel.ToString();

        // 새로운 마법일때
        if (isNew)
            // New 표시하기
            mergedSlot.newSign.SetActive(true);

        yield return new WaitForSecondsRealtime(0.5f);

        // 합성 준비 이펙트 정지
        mergeBeforeEffect.Stop();

        yield return new WaitForSecondsRealtime(0.5f);

        InventorySlot R_MergeSlot = R_MergeSlotRect.GetComponent<InventorySlot>();
        InventorySlot L_MergeSlot = L_MergeSlotRect.GetComponent<InventorySlot>();
        // 양쪽 Merge 슬롯 비우기
        L_MergeSlot.slotInfo = null;
        R_MergeSlot.slotInfo = null;
        // 양쪽 Merge 슬롯 UI 초기화
        L_MergeSlot.Set_Slot();
        R_MergeSlot.Set_Slot();

        // 합성된 슬롯 사이즈 제로
        mergedSlot.transform.localScale = Vector3.zero;

        // 합성된 슬롯 사이즈 키우기
        mergedSlot.transform.DOScale(originScale, 1f)
        .SetEase(Ease.OutBack)
        .SetUpdate(true);

        yield return new WaitForSecondsRealtime(1f);

        // 양쪽 슬롯 위치 초기화
        L_MergeSlotRect.anchoredPosition = L_originPos;
        R_MergeSlotRect.anchoredPosition = R_originPos;

        // 합성 완료 이펙트 켜기
        mergeAfterEffect.transform.localScale = Vector3.zero;
        mergeAfterEffect.GetComponent<Animator>().speed = 0.1f;
        mergeAfterEffect.gameObject.SetActive(true);
        mergeAfterEffect.transform.DOScale(Vector3.one, 0.2f)
        .SetUpdate(true);

        // 클릭, 확인 누르면 
        yield return new WaitUntil(() => UIManager.Instance.UI_Input.UI.Click.IsPressed() || UIManager.Instance.UI_Input.UI.Accept.IsPressed());

        //todo 인벤토리 빈칸에 합성된 마법 넣기
        GetMagic(mergedMagic);

        // 합성 완료 이펙트 끄기
        mergeAfterEffect.transform.DOScale(Vector3.one, 0.2f)
        .SetUpdate(true)
        .OnComplete(() =>
        {
            mergeAfterEffect.gameObject.SetActive(false);
        });

        // 합성 슬롯 작아지기
        mergedSlot.transform.DOScale(Vector3.zero, 0.5f)
        .SetEase(Ease.InBack)
        .SetUpdate(true);

        yield return new WaitForSecondsRealtime(0.5f);

        // 좌측 슬롯 커지기
        L_MergeSlotRect.DOScale(Vector3.one, 0.2f)
        .SetEase(Ease.OutBack)
        .SetUpdate(true);
        // 우측 슬롯 커지기
        R_MergeSlotRect.DOScale(Vector3.one, 0.2f)
        .SetEase(Ease.OutBack)
        .SetUpdate(true);
        // 플러스 아이콘 커지기
        plusIcon.transform.DOScale(Vector3.one, 0.2f)
        .SetEase(Ease.OutBack)
        .SetUpdate(true);

        //todo 상호작용 활성화
        InteractBtnsToggle(true);
    }

    public int GetEmptyInven()
    {
        int emptyIndex = -1;

        // 빈칸 찾기
        for (int i = 0; i < invenSlots.Count; i++)
        {
            // 빈칸 찾으면
            if (invenSlots[i].slotInfo == null)
            {
                // 빈칸 인덱스 기록
                emptyIndex = i;

                break;
            }
        }

        // 빈칸 인덱스 리턴
        return emptyIndex;
    }

    public void GetItem(ItemInfo getItem)
    {
        // print(getItem.itemType + " : " + getItem.itemName);

        // 아이템이 USB일때
        if (getItem.itemType == "USB"
        // 아이템이 SlotMagic 일때 (Merge 슬롯 조작 마법)
         || getItem.itemType == "SlotMagic")
        {
            // 인벤토리 빈칸 찾기
            int emptyIndex = GetEmptyInven();

            // 빈 슬롯에 해당 아이템 넣기
            invenSlots[emptyIndex].slotInfo = getItem;
        }
    }

    public void GetMagic(MagicInfo getMagic)
    {
        // MagicInfo 인스턴스 생성
        MagicInfo magic = new MagicInfo(getMagic);

        //마법의 레벨 초기화
        magic.magicLevel = 1;

        // touchedMagics에 해당 마법 id가 존재하지 않으면
        if (!MagicDB.Instance.savedMagics.Exists(x => x == magic.id))
        {
            // 보유했던 마법 리스트에 추가
            MagicDB.Instance.savedMagics.Add(magic.id);
        }

        // 인벤토리 빈칸 찾기
        int emptyIndex = GetEmptyInven();

        // 빈칸 있을때
        if (emptyIndex != -1)
        {
            // 해당 빈 슬롯에 마법 넣기
            invenSlots[emptyIndex].slotInfo = getMagic;

            // 해당 칸 UI 갱신
            invenSlots[emptyIndex].Set_Slot(true);
        }

        //플레이어 총 전투력 업데이트
        PlayerManager.Instance.PlayerStat_Now.playerPower = PlayerManager.Instance.GetPlayerPower();
    }

    private void Update()
    {
        //뒤로가기 시간 카운트
        if (backBtnCount > 0)
            backBtnCount -= Time.unscaledDeltaTime;
        else
        {
            DOTween.To(() => backBtnFill.fillAmount, x => backBtnFill.fillAmount = x, 0f, 0.2f)
            .SetUpdate(true);
        }

        //스택 스크롤 시간 카운트
        // if (scrollCoolCount > 0)
        //     scrollCoolCount -= Time.unscaledDeltaTime;

        //todo 채팅 스크롤의 컨텐츠 오브젝트 사이즈를 자식 모두의 높이 총합만큼 lerp로 설정

        // 채팅 패널 높이 계산
        if (sumChatHeights == 0)
            sumChatHeights = chatContentRect.sizeDelta.y;

        // 채팅패널 Lerp로 사이즈 반영        
        chatContentRect.sizeDelta = new Vector2(chatContentRect.sizeDelta.x, Mathf.Lerp(chatContentRect.sizeDelta.y, sumChatHeights, Time.unscaledDeltaTime * 5f));
    }

    public IEnumerator ChatAdd(string message)
    {
        // 메시지 생성
        GameObject chat = LeanPool.Spawn(chatPrefab,
        chatScroll.content.rect.position - new Vector2(0, -10f),
        Quaternion.identity, chatScroll.content);

        // 캔버스 그룹 찾고 알파값 0으로 낮춰서 숨기기
        CanvasGroup canvasGroup = chat.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0;

        // 채팅 컬러 및 메시지 적용
        chat.GetComponent<Image>().color = new Color(1, 50f / 255f, 50f / 255f, 1);
        chat.GetComponentInChildren<TextMeshProUGUI>().text = message;

        // 1프레임 대기
        yield return new WaitForSecondsRealtime(Time.unscaledDeltaTime);

        float sumHeights = 0;

        // 메시지들의 총합 높이 갱신
        for (int i = 0; i < chatScroll.content.childCount; i++)
        {
            RectTransform rect = chatScroll.content.GetChild(i).GetComponent<RectTransform>();

            // 여백 합산
            sumHeights += 10;
            // 높이 합산
            sumHeights += rect.sizeDelta.y;

            // print(i + " : " + rect.sizeDelta.y);
        }

        sumChatHeights = sumHeights;

        // 알파값 높여서 표시
        canvasGroup.alpha = 1;
    }

    public void CancelSelectSlot()
    {
        //마우스 꺼져있으면 리턴
        if (Cursor.lockState == CursorLockMode.Locked)
            return;

        // 마우스의 아이콘 끄기
        nowSelectIcon.enabled = false;

        // 선택된 슬롯에 슬롯 정보 넣기
        nowSelectSlot.slotInfo = nowSelectSlotInfo;
        // 선택된 슬롯 UI 갱신
        nowSelectSlot.Set_Slot();

        // 선택된 슬롯 shiny 이펙트 켜기
        nowSelectSlot.shinyEffect.gameObject.SetActive(false);
        nowSelectSlot.shinyEffect.gameObject.SetActive(true);

        // 현재 선택된 마법 인덱스 초기화
        // nowSelectIndex = -1;
        nowSelectSlot = null;

        // 폰 하단 버튼 상호작용 허용
        InteractBtnsToggle(true);

        //선택된 마법 아이콘 마우스 위치로 이동
        MousePos();
    }

    public void ShakeMouseIcon()
    {
        // 현재 트윈 멈추기
        nowSelectIcon.transform.DOPause();

        // 원래 위치 저장
        Vector2 originPos = nowSelectIcon.transform.localPosition;

        // 마우스 아이콘 흔들기
        nowSelectIcon.transform.DOPunchPosition(Vector2.right * 30f, 1f, 10, 1)
        .SetEase(Ease.Linear)
        .OnPause(() =>
        {
            nowSelectIcon.transform.localPosition = originPos;
        })
        .SetUpdate(true);
    }

    void Set_Recipe()
    {
        // 레시피 스크롤 컴포넌트 끄기
        recipeScroll.enabled = false;

        // 처음에만 오브젝트 생성
        if (!recipeInit)
            // 레시피 목록에 모든 마법 표시
            for (int i = 0; i < MagicDB.Instance.magicDB.Count; i++)
            {
                // 마법 정보 찾기
                MagicInfo magic = MagicDB.Instance.magicDB[i];

                // 0등급이면 넘기기
                if (magic.grade == 0)
                    continue;

                // 레시피 프리팹 생성
                LeanPool.Spawn(recipePrefab, recipeScroll.Content.transform);
            }

        // 레시피 목록에 모든 마법 표시
        for (int i = 0; i < recipeScroll.Content.childCount; i++)
        {
            // 마법 정보 찾기
            MagicInfo magic = MagicDB.Instance.magicDB[i + 6];

            // 레시피 아이템 찾기
            Transform recipe = recipeScroll.Content.GetChild(i);

            // 해당 마법 언락 여부 판단
            bool unlocked = MagicDB.Instance.unlockMagics.Exists(x => x == magic.id);

            // 재료들 정보 찾기
            MagicInfo elementA = MagicDB.Instance.GetMagicByName(magic.element_A);
            MagicInfo elementB = MagicDB.Instance.GetMagicByName(magic.element_B);

            // 메인 아이콘 및 프레임 찾기
            Image main_Icon = recipe.transform.Find("MagicSlot/Icon").GetComponent<Image>();
            Image main_Frame = recipe.transform.Find("MagicSlot/Frame").GetComponent<Image>();
            // 재료 아이콘 A,B 및 프레임 찾기
            Image elementA_Icon = recipe.transform.Find("Element_A/Icon").GetComponent<Image>();
            Image elementA_Frame = recipe.transform.Find("Element_A/Frame").GetComponent<Image>();
            Image elementB_Icon = recipe.transform.Find("Element_B/Icon").GetComponent<Image>();
            Image elementB_Frame = recipe.transform.Find("Element_B/Frame").GetComponent<Image>();

            // 메인 아이콘 컬러 초기화
            main_Icon.color = unlocked ? Color.white : Color.black;

            // 메인 아이콘 표시
            main_Icon.sprite = MagicDB.Instance.GetMagicIcon(magic.id);
            // 재료 아이콘 해금됬으면 표시, 아니면 물음표
            elementA_Icon.sprite = unlocked && elementA != null ? MagicDB.Instance.GetMagicIcon(elementA.id) : SystemManager.Instance.questionMark;
            elementB_Icon.sprite = unlocked && elementB != null ? MagicDB.Instance.GetMagicIcon(elementB.id) : SystemManager.Instance.questionMark;

            // 메인 아이콘 프레임 색 넣기
            main_Frame.color = MagicDB.Instance.GradeColor[magic.grade];
            // 재료 아이콘 해금됬으면 프레임 색 넣기
            elementA_Frame.color = unlocked && elementA != null ? MagicDB.Instance.GradeColor[elementA.grade] : Color.white;
            elementB_Frame.color = unlocked && elementB != null ? MagicDB.Instance.GradeColor[elementB.grade] : Color.white;

            // 메인 아이콘에 툴팁 넣기
            if (unlocked)
            {
                ToolTipTrigger main_tooltip = main_Icon.transform.parent.GetComponentInChildren<ToolTipTrigger>(true);
                ToolTipTrigger elementA_tooltip = elementA_Icon.transform.parent.GetComponentInChildren<ToolTipTrigger>(true);
                ToolTipTrigger elementB_tooltip = elementB_Icon.transform.parent.GetComponentInChildren<ToolTipTrigger>(true);

                main_tooltip.Magic = magic;
                main_tooltip.enabled = true;

                if (elementA != null && elementB != null)
                {
                    elementA_tooltip.Magic = elementA;
                    elementA_tooltip.enabled = true;
                    elementB_tooltip.Magic = elementB;
                    elementB_tooltip.enabled = true;
                }
            }
        }

        // 레시피 스크롤 컴포넌트 켜기
        recipeScroll.enabled = true;

        // 레시피 스크롤 위치 초기화
        recipeScroll.Content.localPosition = Vector2.zero;
        recipeScroll.GoToPanel(0);

        // 레시피 초기화 완료
        recipeInit = true;
    }

    public void Use_USB()
    {
        StartCoroutine(GetUSBMagic());
    }

    public IEnumerator GetUSBMagic()
    {
        // 아이콘 찾기
        Transform icon = usbScroll.Content.GetChild(usbScroll.CenteredPanel).Find("Icon");
        // 아이콘 이미지 찾기
        Image iconImage = icon.GetComponent<Image>();

        // usb 개수 부족하면 리턴
        if (PlayerManager.Instance.hasUSBList[usbScroll.CenteredPanel] <= 0)
        {
            // 아이콘 트윈 정지            
            icon.DOPause();

            // 원래 위치 저장
            Vector2 originPos = icon.localPosition;

            // usb 아이콘 떨림 트윈
            icon.DOPunchPosition(Vector2.right * 30f, 1f, 10, 1)
            .SetEase(Ease.Linear)
            .OnPause(() =>
            {
                icon.localPosition = originPos;
            })
            .SetUpdate(true);

            yield break;
        }

        //! todo 나중에 메뉴 버튼도 단축키 대응 되면 뽑기 도중에 화면 스크롤 못하게 막아야함
        // 메뉴, 백 버튼 상호작용 및 키입력 막기
        InteractBtnsToggle(false);

        // 뽑기 화면 전체 투명하게
        randomScreen.alpha = 0;
        // 뽑기 배경 활성화, 가려서 핸드폰 입력 막기
        randomScreen.gameObject.SetActive(true);

        // 뽑기 스크롤 그룹 투명하게
        CanvasGroup randomScrollGroup = randomScroll.GetComponent<CanvasGroup>();
        randomScrollGroup.alpha = 0;
        // 뽑기 스크롤 비활성화
        randomScroll.gameObject.SetActive(false);
        // 뽑기 스크롤 컴포넌트 비활성화
        randomScroll.enabled = false;

        // 해당 usb 개수 차감
        PlayerManager.Instance.hasUSBList[usbScroll.CenteredPanel]--;

        // 모든 자식 비우기
        SystemManager.Instance.DestroyAllChild(randomScroll.Content);

        // 사용된 usb 아이콘 숨기기
        Color usbColor = iconImage.color;
        usbColor.a = 0;
        iconImage.DOColor(usbColor, 0.5f)
        .SetUpdate(true);

        // 애니메이션용 아이콘 스크린 가운데로 올라가기
        anim_USB_Slot.gameObject.SetActive(true);
        anim_USB_Slot.DOMove(randomScroll.transform.position, 0.5f)
        .SetEase(Ease.OutSine)
        .SetUpdate(true);

        yield return new WaitForSecondsRealtime(0.5f);

        // 랜덤 마법 리스트
        List<MagicInfo> randomList = new List<MagicInfo>();

        // 모든 마법 정보 조회
        foreach (KeyValuePair<int, MagicInfo> value in MagicDB.Instance.magicDB)
        {
            // 선택된 usb와 등급이 같은 마법이면 
            if (value.Value.grade == usbScroll.CenteredPanel + 1)
            {
                // 랜덤 풀에 넣기
                randomList.Add(value.Value);
            }
        }

        //todo 해당 등급의 언락된 마법 리스트 불러오기
        // for (int i = 0; i < MagicDB.Instance.magicDB.Count; i++)
        // {
        // 언락된 마법의 id
        // int magicId = MagicDB.Instance.unlockMagics[i];

        // if (MagicDB.Instance.magicDB.TryGetValue(magicId, out MagicInfo magic))
        // {
        //     // 선택된 usb와 등급이 같은 마법이면 
        //     if (magic.grade == usbScroll.CenteredPanel)
        //     {
        //         // 랜덤 풀에 넣기
        //         randomList.Add(magic);
        //     }
        // }
        // }

        // 랜덤 마법 풀 개수만큼 반복
        for (int i = 0; i < randomList.Count; i++)
        {
            // 랜덤 스크롤 컨텐츠 자식으로 슬롯 넣기
            Transform magicSlot = LeanPool.Spawn(magicSlotPrefab, randomScroll.Content);

            // 마법 아이콘 넣기
            magicSlot.Find("Icon").GetComponent<Image>().sprite = MagicDB.Instance.GetMagicIcon(randomList[i].id);
            // 프레임 색 넣기
            magicSlot.Find("Frame").GetComponent<Image>().color = MagicDB.Instance.GradeColor[randomList[i].grade];
        }

        // 뽑기 화면 전체 나타내기
        DOTween.To(() => randomScreen.alpha, x => randomScreen.alpha = x, 1f, 0.5f)
        .SetUpdate(true);

        // 스냅 스크롤 컴포넌트 활성화
        randomScroll.enabled = true;
        // 뽑기 스크롤 활성화
        randomScroll.gameObject.SetActive(true);

        // 한번 굴려서 무한 스크롤 위치 초기화
        randomScroll.GoToNextPanel();

        // 애니메이션용 usb 슬롯 비활성화 및 위치 초기화
        anim_USB_Slot.gameObject.SetActive(false);
        anim_USB_Slot.position = usbScroll.transform.position;

        yield return new WaitForSecondsRealtime(0.5f);

        // 뽑기 스크롤 그룹 알파값 초기화
        DOTween.To(() => randomScrollGroup.alpha, x => randomScrollGroup.alpha = x, 1f, 0.5f)
        .SetUpdate(true);

        // 스크롤 끝나는 시간 계산
        float stopTime = Time.unscaledTime + Random.Range(minScrollTime, maxScrollTime);
        // 타이머 끝날때까지 빠르게 스크롤 반복 내리기
        while (stopTime > Time.unscaledTime)
        {
            // 끝날때쯤 점점 느려짐
            if (stopTime <= Time.unscaledTime + 1f)
            {
                // 스냅 스피드 계산
                float scrollSpeed = (stopTime - Time.unscaledTime) * randomScrollSpeed;
                scrollSpeed = Mathf.Clamp(scrollSpeed, 5f, randomScrollSpeed);

                randomScroll.SnapSpeed = scrollSpeed;
            }
            else
                randomScroll.SnapSpeed = randomScrollSpeed;

            randomScroll.GoToNextPanel();

            // 남은시간 1초 이상일때 1초로 만들기 (피지컬로 슬롯 맞추기 가능)
            if (UIManager.Instance.UI_Input.UI.Accept.triggered)
            {
                if (stopTime >= 1f)
                {
                    stopTime = Time.unscaledTime + 1f;
                    print("Skip : " + (stopTime - Time.unscaledTime));
                }
            }

            print(stopTime - Time.unscaledTime);

            yield return new WaitForSecondsRealtime(0.01f);
        }

        // 멈춘 후 딜레이
        yield return new WaitForSecondsRealtime(0.5f);

        // 랜덤풀에서 멈춘 시점 선택된 인덱스에 해당하는 마법 뽑기
        MagicInfo getMagic = randomList[randomScroll.CenteredPanel];
        // print(getMagic.name);

        // 획득한 마법 인벤에 넣기
        GetMagic(getMagic);

        // 팡파레 이펙트 켜기
        slotRayEffect.gameObject.SetActive(true);
        // 애니메이터 속도 느리게
        slotRayEffect.speed = 0.1f;
        // 팡파레 이펙트 등급색으로 변경
        Image raySprite = slotRayEffect.GetComponent<Image>();
        raySprite.color = MagicDB.Instance.GradeColor[getMagic.grade];
        // 사이즈 키우기
        slotRayEffect.transform.localScale = Vector2.zero;
        slotRayEffect.transform.DOScale(Vector3.one * 2f, 0.2f)
        .SetUpdate(true);

        // 획득 파티클 색 변경
        ParticleSystem.MainModule particleMain = getMagicEffect.main;
        particleMain.startColor = MagicDB.Instance.GradeColor[getMagic.grade];

        // 획득 마법 초기화
        getMagic = null;

        // 끝난 후 아무키 누르거나 클릭하면 트랜지션 종료
        yield return new WaitUntil(() => UIManager.Instance.UI_Input.UI.Click.IsPressed() || UIManager.Instance.UI_Input.UI.Accept.IsPressed());

        // 팡파레 이펙트 끄기
        slotRayEffect.gameObject.SetActive(false);

        // 뽑힌 슬롯 끄기
        GameObject getSlot = randomScroll.Content.GetChild(randomScroll.CenteredPanel).gameObject;
        getSlot.SetActive(false);

        // 마법 획득 이펙트 켜기
        getMagicEffect.gameObject.SetActive(true);

        // 사용된 usb 아이콘 색깔 초기화
        usbColor.a = 1;
        iconImage.DOColor(usbColor, 0.5f)
        .SetUpdate(true);

        yield return new WaitForSecondsRealtime(1f);

        //todo 아이템 개수 갱신
        // 스택 개수 UI 갱신
        // stackAllNum.text = StackAmount().ToString();

        // 뽑기 스크린 전체 투명하게
        DOTween.To(() => randomScreen.alpha, x => randomScreen.alpha = x, 0f, 1f)
        .SetUpdate(true);

        yield return new WaitForSecondsRealtime(1f);

        // 마법 획득 이펙트 끄기
        getMagicEffect.gameObject.SetActive(false);

        // 뽑기 스크린 비활성화
        randomScreen.gameObject.SetActive(false);

        // 뽑힌 슬롯 다시 켜기
        getSlot.SetActive(true);

        // 메뉴, 백 버튼 상호작용 및 키입력 막기 해제
        InteractBtnsToggle(true);
    }

    public void InteractBtnsToggle(bool able)
    {
        // 키 입력 막기 변수 토글
        btnsInteractable = able;

        // 메뉴 버튼 상호작용 토글
        recipeBtn.interactable = able;
        // 백 버튼 상호작용 토글
        backBtn.interactable = able;
    }

    public void ScreenScrollBtn()
    {
        // 키 입력 막기
        if (!btnsInteractable)
            return;

        // 선택된게 머지 스크린일때
        if (screenScroll.CenteredPanel == 0)
        {
            // // 총 USB 개수 활성화
            // usbAllNum.transform.parent.gameObject.SetActive(true);
            // // 총 스택 개수 비활성화
            // stackAllNum.transform.parent.gameObject.SetActive(false);
        }
        // 선택된게 레시피 스크린일때
        else
        {
            // // 총 USB 개수 비활성화
            // usbAllNum.transform.parent.gameObject.SetActive(false);
            // // 총 스택 개수 활성화
            // stackAllNum.transform.parent.gameObject.SetActive(true);

            // 레시피 갱신
            Set_Recipe();
        }
    }

    public void BackBtn()
    {
        //백 버튼 액션 실행
        StartCoroutine(BackBtnAction());
    }

    public IEnumerator BackBtnAction()
    {
        // 머지 패널 꺼져있으면 리턴
        if (!phonePanel.activeSelf)
            yield break;

        // 키 입력 막기
        if (!btnsInteractable)
            yield break;

        // 메인 인벤토리 화면일때
        if (backBtnCount <= 0)
        {
            //버튼 시간 카운트 시작
            backBtnCount = 1f;

            // 한번 누르면 시간 재면서 버튼 절반 색 채우기
            DOTween.To(() => backBtnFill.fillAmount, x => backBtnFill.fillAmount = x, 0.5f, 0.2f)
            .SetUpdate(true);
        }
        // 시간 내에 한번 더 누르면 팝업 종료
        else
        {
            // 인벤 슬롯 상호작용 모두 끄기
            foreach (InventorySlot invenSlot in invenSlots)
            {
                invenSlot.slotButton.interactable = false;
            }
            //레시피 버튼 끄기
            recipeBtn.interactable = false;
            //뒤로 버튼 끄기
            backBtn.interactable = false;
            //홈 버튼 끄기
            homeBtn.interactable = false;

            //마우스로 아이콘 들고 있으면 복귀시키기
            if (nowSelectIcon.enabled)
                CancelSelectSlot();

            // UI 커서 미리 끄기
            UIManager.Instance.UICursorToggle(false);

            // 로딩 패널 켜기
            loadingPanel.SetActive(true);

            // 백버튼 색 채우기
            DOTween.To(() => backBtnFill.fillAmount, x => backBtnFill.fillAmount = x, 1f, 0.2f)
            .SetUpdate(true);

            // 화면 검은색으로
            blackScreen.DOColor(new Color(70f / 255f, 70f / 255f, 70f / 255f, 1), 0.2f)
            .SetUpdate(true);

            // 화면 꺼지는 동안 대기
            yield return new WaitForSecondsRealtime(0.2f);

            // 핸드폰 화면 패널 끄기
            phonePanel.SetActive(false);

            float moveTime = 0.8f;

            // 매직폰 위치,회전,스케일로 복구하기
            CastMagic.Instance.transform.DOMove(phonePosition, moveTime)
            .SetUpdate(true);
            CastMagic.Instance.transform.DOScale(phoneScale, moveTime)
            .SetUpdate(true);
            CastMagic.Instance.transform.DORotate(new Vector3(0, 360f, 0), moveTime, RotateMode.WorldAxisAdd)
            .SetUpdate(true);

            // 절반쯤 이동했을때 화면 라이트 켜기
            lightScreen.DOColor(new Color(30f / 255f, 1f, 1f, 100f / 255f), moveTime / 2f)
            .SetDelay(moveTime / 2f)
            .SetUpdate(true);

            // 핸드폰 이동하는 동안 대기
            yield return new WaitForSecondsRealtime(moveTime);

            //백 버튼 변수 초기화
            backBtnCount = 0f;
            backBtnFill.fillAmount = 0f;

            // 끝나면 시간 복구하기
            Time.timeScale = 1f;

            //스마트폰 캔버스 종료
            UIManager.Instance.PopupUI(UIManager.Instance.phonePanel);

            // 인벤토리에서 마법 찾아 자동 시전하기
            CastMagic.Instance.CastCheck();
        }
    }
}
