using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Lean.Pool;
using DG.Tweening;

public class MergeMagic : MonoBehaviour
{
    //초기 화면 로딩 여부
    public bool loadDone = false;
    public bool selectStack; // 스택 슬롯을 선택했는지

    [Header("Merge Board")]
    public Transform mergeSlots;
    public GameObject slotPrefab;

    [Header("Stack List")]
    public Transform stackSlots;
    List<GameObject> stackList = new List<GameObject>(); //각각 슬롯 오브젝트
    Vector2[] stackSlotPos = new Vector2[7]; //각각 슬롯의 초기 위치
    float scrollCoolCount; //스크롤 쿨타임 카운트
    float scrollCoolTime = 0.1f; //스크롤 쿨타임
    MagicInfo selectedMagic; //현재 선택된 마법
    public Image selectedIcon; //마우스 따라다닐 아이콘

    private void Awake()
    {
        for (int i = 0; i < 7; i++)
        {
            // 모든 슬롯 오브젝트 넣기
            stackList.Add(stackSlots.transform.GetChild(i).gameObject);
            // 슬롯들의 초기 위치 넣기
            stackSlotPos[i] = stackList[i].GetComponent<RectTransform>().anchoredPosition;
            // print(slotPos[i]);
        }
    }

    private void OnEnable()
    {
        //초기화
        StartCoroutine(Initial());
    }

    IEnumerator Initial()
    {
        //시간 멈추기
        Time.timeScale = 0f;

        //TODO 휴대폰 로딩 화면으로 가리기
        loadDone = false;

        yield return new WaitUntil(() => MagicDB.Instance.loadDone);

        // 머지 리스트에 있는 마법들 머지 보드에 나타내기
        for (int i = 0; i < mergeSlots.childCount; i++)
        {
            Transform mergeSlot = mergeSlots.GetChild(i);

            // 마법 정보 찾기
            MagicInfo magic = PlayerManager.Instance.hasMergeMagics[i];

            //프레임 찾기
            Image frame = mergeSlot.Find("Frame").GetComponent<Image>();
            //아이콘 찾기
            Image icon = mergeSlot.Find("Icon").GetComponent<Image>();
            //레벨 찾기
            TextMeshProUGUI level = mergeSlot.Find("Level").GetComponent<TextMeshProUGUI>();
            //버튼 찾기
            Button button = mergeSlot.GetComponent<Button>();
            //툴팁 컴포넌트 찾기
            ToolTipTrigger tooltip = mergeSlot.Find("Button").GetComponent<ToolTipTrigger>();

            //마법 정보 없으면 넘기기
            if (magic == null)
            {
                //프레임 색 초기화
                frame.color = Color.white;

                //아이콘 및 레벨 비활성화
                icon.enabled = false;
                level.enabled = false;

                continue;
            }
            else
            {
                //아이콘 및 레벨 활성화
                icon.enabled = true;
                level.enabled = true;
            }

            //등급 프레임 색 넣기
            frame.color = MagicDB.Instance.gradeColor[magic.grade];
            //아이콘 넣기
            icon.sprite = MagicDB.Instance.GetMagicIcon(magic.id);
            //레벨 넣기
            level.text = "Lv. " + magic.magicLevel.ToString();
            //TODO 슬롯에 툴팁 정보 넣기
            tooltip.magic = magic;

            // TODO 슬롯에 마우스 오버시 select
            // TODO 슬롯 마우스 오버시 해당 슬롯에 반투명하게 아이콘 표시
        }

        //TODO 첫번째 머지 슬롯 선택하기
        mergeSlots.GetChild(0).GetComponent<Button>().Select();

        // 스택 슬롯 세팅
        SetSlots();
        // 스택 슬롯 사이즈 및 위치 정렬
        ScrollSlots(true);

        //TODO 휴대폰 로딩 화면 끄기
        loadDone = true;

        //TODO 게임 시작할때는 기본 마법 1개 어느 슬롯에 놓을지 선택
        //TODO 선택하면 배경 사라지고, 휴대폰 플레이어 위로 작아지며 날아간 후에 게임 시작
    }

    private void Update()
    {
        // 좌,우 방향키로 스택 리스트 스크롤하기
        StackScroll();

        //선택된 마법 아이콘 마우스 따라가기
        FollowMouse();
    }

    void FollowMouse()
    {
        //TODO 스택 슬롯이 select 되어있으면 진행 아니면 아이콘 끄기

        // 방향키 누르면
        if (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0)
        {
            //아이콘 끄기
            selectedIcon.gameObject.SetActive(false);
        }

        // 마우스 움직이면
        if (Input.GetAxisRaw("Mouse X") != 0 || Input.GetAxisRaw("Mouse Y") != 0)
        {
            //아이콘 켜기
            selectedIcon.gameObject.SetActive(true);
        }

        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 0;
        selectedIcon.transform.position = mousePos;
    }

    void StackScroll()
    {
        if (scrollCoolCount <= 0f)
        {
            if (Input.GetKey(KeyCode.A))
            {
                ScrollSlots(false);
                scrollCoolCount = scrollCoolTime;
            }
            if (Input.GetKey(KeyCode.D))
            {
                ScrollSlots(true);
                scrollCoolCount = scrollCoolTime;
            }
        }
        else
        {
            scrollCoolCount -= Time.unscaledDeltaTime;
        }
    }

    public void ScrollSlots(bool isLeft)
    {
        //모든 슬롯 domove 강제 즉시 완료
        foreach (var slot in stackList)
        {
            slot.transform.DOComplete();
        }

        int startSlotIndex = -1;
        int endSlotIndex = -1;

        // 처음 로딩이 아닐때는 슬롯 인덱스 이동
        if (loadDone)
        {
            //슬롯 오브젝트 리스트 인덱스 계산
            startSlotIndex = isLeft ? stackList.Count - 1 : 0;
            endSlotIndex = isLeft ? 0 : stackList.Count - 1;

            // 마지막 슬롯을 첫번째 인덱스 자리에 넣기
            GameObject targetSlot = stackList[startSlotIndex]; //타겟 오브젝트 얻기
            stackList.RemoveAt(startSlotIndex); //타겟 마법 삭제
            stackList.Insert(endSlotIndex, targetSlot); //타겟 마법 넣기

            // 마지막 슬롯은 slotPos[0] 으로 이동
            targetSlot.GetComponent<RectTransform>().anchoredPosition = stackSlotPos[endSlotIndex];
        }

        // 모든 슬롯 오브젝트들을 slotPos 초기위치에 맞게 domove
        for (int i = 0; i < stackList.Count; i++)
        {
            RectTransform rect = stackList[i].GetComponent<RectTransform>();

            //이미 domove 중이면 빠르게 움직이기
            // float moveTime = Vector2.Distance(rect.anchoredPosition, slotPos[i]) != 120f ? 0.1f : 0.5f;
            float moveTime = 0.2f;

            //한칸 옆으로 위치 이동
            if (endSlotIndex != i && endSlotIndex != -1)
                rect.DOAnchorPos(stackSlotPos[i], moveTime)
                .SetUpdate(true);

            //자리에 맞게 사이즈 바꾸기
            float scale = i == 3 ? 1f : 0.5f;
            rect.DOScale(scale, moveTime)
            .SetUpdate(true);

            //아이콘 알파값 바꾸기
            stackList[i].GetComponent<CanvasGroup>().alpha = i == 3 ? 1f : 0.5f;

            //버튼 컴포넌트 찾기
            Button btn = stackList[i].GetComponent<Button>();

            //TODO Stack 슬롯 선택 되어있으면 3번 오브젝트 select 후 이동
            // btn.Select();

            if (i == 3)
            {
                //버튼 네비 설정
                Navigation nav = btn.navigation;
                nav.mode = Navigation.Mode.Automatic;
                btn.navigation = nav;
            }
            else
            {
                //버튼 네비 끄기
                Navigation nav = btn.navigation;
                nav.mode = Navigation.Mode.None;
                btn.navigation = nav;
            }
        }

        //마법 데이터 리스트 인덱스 계산
        int startIndex = isLeft ? PlayerManager.Instance.hasStackMagics.Count - 1 : 0;
        int endIndex = isLeft ? 0 : PlayerManager.Instance.hasStackMagics.Count - 1;

        // 실제 데이터 hasStackMagics도 마지막 슬롯을 첫번째 인덱스 자리에 넣기
        MagicInfo targetMagic = PlayerManager.Instance.hasStackMagics[startIndex]; //타겟 마법 얻기
        PlayerManager.Instance.hasStackMagics.RemoveAt(startIndex); //타겟 마법 삭제
        PlayerManager.Instance.hasStackMagics.Insert(endIndex, targetMagic); //타겟 마법 넣기

        // 선택된 마법 입력
        selectedMagic = PlayerManager.Instance.hasStackMagics[0];
        // 선택된 마법 아이콘 이미지 넣기
        selectedIcon.sprite = MagicDB.Instance.GetMagicIcon(selectedMagic.id);

        // 모든 아이콘 다시 넣기
        SetSlots();
    }

    void SetSlots()
    {
        //마지막 이전 마법
        SetIcon(0, 4, PlayerManager.Instance.hasStackMagics.Count - 3);
        //마지막 마법
        SetIcon(1, 3, PlayerManager.Instance.hasStackMagics.Count - 2);
        //0번째 마법
        SetIcon(2, 2, PlayerManager.Instance.hasStackMagics.Count - 1);
        //1번째 마법
        SetIcon(3, 1, 0);
        //2번째 마법
        SetIcon(4, 2, 1);
        //3번째 마법
        SetIcon(5, 3, 2);
        //4번째 마법
        SetIcon(6, 4, 3);
    }

    void SetIcon(int objIndex, int num, int magicIndex)
    {
        // hasStackMagics의 보유 마법이 num 보다 많을때
        if (PlayerManager.Instance.hasStackMagics.Count >= num)
        {
            stackList[objIndex].transform.Find("Icon").gameObject.SetActive(true);
            Sprite sprite = MagicDB.Instance.GetMagicIcon(PlayerManager.Instance.hasStackMagics[magicIndex].id);
            stackList[objIndex].transform.Find("Icon").GetComponent<Image>().sprite = sprite == null ? SystemManager.Instance.questionMark : sprite;
            stackList[objIndex].transform.Find("Frame").GetComponent<Image>().color = MagicDB.Instance.gradeColor[PlayerManager.Instance.hasStackMagics[magicIndex].grade];
        }
        //넣을 마법 없으면 아이콘 및 프레임 숨기기
        else
        {
            stackList[objIndex].transform.Find("Icon").gameObject.SetActive(false);
            stackList[objIndex].transform.Find("Frame").GetComponent<Image>().color = Color.white;
        }
    }

    public void MouseOverSlot()
    {
        //TODO 함수 실행한 버튼이 몇번째 인덱스 슬롯인지?
        print(this.transform.GetSiblingIndex());
        //TODO 슬롯에 마우스 오버시 select
        //TODO 슬롯 마우스 오버시 해당 슬롯에 반투명하게 아이콘 표시
    }

    //TODO 슬롯에 마우스 오버시 select
    //TODO 슬롯 마우스 오버시 해당 슬롯에 반투명하게 아이콘 표시
    //TODO 4방향에 있는 마법 검사해서 합성 가능하면 인디케이터 (슬롯 사이 화살표, 선택된 슬롯 방향으로 조금씩 움직이려는 트윈)

    //TODO 클릭하면 마법 합성
    //TODO 클릭한 슬롯으로 합쳐짐
    //TODO 주변에 합성 가능성이 여러개일때, 선택지 띄우기
}