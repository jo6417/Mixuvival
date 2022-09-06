using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ToolTipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    public enum ToolTipType { ProductTip, HasStuffTip };
    public ToolTipType toolTipType;
    private MagicInfo magic;
    public MagicInfo Magic
    {
        get { return magic; }
        set
        {
            magic = value;

            if (magic != null)
            {
                magicName = Magic.name;
            }
        }
    }
    private ItemInfo item;
    public ItemInfo Item
    {
        get { return item; }
        set
        {
            item = value;

            if (item != null)
            {
                itemName = Item.name;
            }
        }
    }
    public string magicName;
    public string itemName;

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        yield return null;

        // 마법,아이템 정보 들어올때까지 대기
        // yield return new WaitUntil(() => Magic != null || Item != null);

        // if (Magic != null)
        //     magicName = Magic.magicName;

        // if (Item != null)
        //     itemName = Item.itemName;

        // 마법 아이템 정보 없으면 컴포넌트 끄기
        if (Magic == null && Item == null)
            this.enabled = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 상품 구매 버튼일때
        if (toolTipType == ToolTipType.ProductTip)
        {
            // StartCoroutine(ProductToolTip.Instance.OpenTooltip(magic, item));
            ProductToolTip.Instance.OpenTooltip(Magic, Item);
        }

        // 소지품 아이콘일때
        if (toolTipType == ToolTipType.HasStuffTip)
        {
            HasStuffToolTip.Instance.OpenTooltip(Magic, Item);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        //마우스 잠겨있지 않으면
        if (Cursor.lockState == CursorLockMode.None)
            QuitTooltip();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        QuitTooltip();
    }

    void QuitTooltip()
    {
        // 상품 구매 버튼일때
        if (toolTipType == ToolTipType.ProductTip)
        {
            ProductToolTip.Instance.QuitTooltip();
        }

        // 소지품 아이콘일때
        if (toolTipType == ToolTipType.HasStuffTip)
        {
            HasStuffToolTip.Instance.QuitTooltip();
        }
    }
}
