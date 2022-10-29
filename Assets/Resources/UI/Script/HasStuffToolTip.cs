using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HasStuffToolTip : MonoBehaviour
{
    #region Singleton
    private static HasStuffToolTip instance;
    public static HasStuffToolTip Instance
    {
        get
        {
            if (instance == null)
            {
                //비활성화된 오브젝트도 포함
                var obj = FindObjectOfType<HasStuffToolTip>(true);
                if (obj != null)
                {
                    instance = obj;
                }
                else
                {
                    print("new obj");
                    var newObj = new GameObject().AddComponent<HasStuffToolTip>();
                    instance = newObj;
                }
            }
            return instance;
        }
    }
    #endregion

    public TextMeshProUGUI stuffName;
    public TextMeshProUGUI stuffDescription;
    public MagicInfo magic;
    public ItemInfo item;
    float halfCanvasWidth;
    private RectTransform rect;
    [SerializeField] CanvasGroup canvasGroup;

    private void Awake()
    {
        //마우스 클릭 입력
        UIManager.Instance.UI_Input.UI.Click.performed += val =>
        {
            QuitTooltip();
        };
        //마우스 위치 입력
        UIManager.Instance.UI_Input.UI.MousePosition.performed += val => FollowMouse(val.ReadValue<Vector2>());

        halfCanvasWidth = GetComponentInParent<CanvasScaler>().referenceResolution.x * 0.5f;
        rect = GetComponent<RectTransform>();

        //처음엔 끄기
        // gameObject.SetActive(false);
        canvasGroup.alpha = 0f;
    }

    void Update()
    {
        // 마우스 커서 따라다니기
        // FollowMouse();
    }

    void FollowMouse(Vector2 nowMousePos)
    {
        // 패널이 화면밖으로 안나가게 피벗 수정
        if (rect == null)
            return;

        if (rect.anchoredPosition.x + rect.sizeDelta.x > halfCanvasWidth)
        {
            rect.pivot = new Vector2(1, 0);
        }
        else
        {
            rect.pivot = new Vector2(0, 0);
        }

        Vector3 mousePos = nowMousePos;
        mousePos.z = 0;
        transform.position = mousePos;
    }

    //툴팁 켜기
    public void OpenTooltip(SlotInfo slotInfo = null)
    {
        //마우스 위치로 이동 후 활성화
        FollowMouse(UIManager.Instance.nowMousePos);
        gameObject.SetActive(true);
        canvasGroup.alpha = 0.7f;

        //마법 or 아이템 정보 넣기
        this.magic = slotInfo as MagicInfo;
        this.item = slotInfo as ItemInfo;

        string name = "";
        string description = "";

        // 마법 정보가 있을때
        if (slotInfo != null)
        {
            name = slotInfo.name;
            description = slotInfo.description;
        }

        // 아이템 정보가 있을때
        if (item != null)
        {
            name = item.name;
            description = item.description;
        }

        //이름, 설명 넣기
        stuffName.text = name;
        stuffDescription.text = description;
    }

    //툴팁 끄기
    public void QuitTooltip()
    {
        // print("QuitTooltip");
        magic = null;
        item = null;

        // gameObject.SetActive(false);
        canvasGroup.alpha = 0f;
    }
}
