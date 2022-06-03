using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lean.Pool;

public class Heist : MonoBehaviour
{
    private MagicInfo magic;
    public MagicHolder magicHolder;
    float speed = 0;
    float ghostCount = 0;

    private void OnEnable()
    {
        //초기화
        StartCoroutine(Initial());
    }

    // 마법 레벨업 할때 새로 초기화 하기
    IEnumerator Initial()
    {
        yield return new WaitUntil(() => magicHolder.magic != null);
        magic = magicHolder.magic;

        //원래 속도 변수가 있으면 버프 빼기
        if (speed != 0)
            PlayerManager.Instance.PlayerStat_Now.moveSpeed = PlayerManager.Instance.PlayerStat_Now.moveSpeed / speed;

        //버프할 스피드 불러오기
        speed = MagicDB.Instance.MagicSpeed(magic, true);
        //플레이어 이동속도 버프하기
        PlayerManager.Instance.PlayerStat_Now.moveSpeed = PlayerManager.Instance.PlayerStat_Now.moveSpeed * speed;

        //속도에 따라 사이즈 변화
        transform.localScale = Vector3.one * speed;

        //플레이어 위치로 이동
        transform.position = PlayerManager.Instance.transform.position;

        // 플레이어를 부모로 지정
        transform.parent = PlayerManager.Instance.transform;
    }

    private void Update()
    {
        //잔상 남기기
        GhostTrail();
    }

    void GhostTrail()
    {
        if (ghostCount <= 0)
        {
            //고스트 오브젝트 소환
            GameObject ghostObj = LeanPool.Spawn(SystemManager.Instance.ghostPrefab, PlayerManager.Instance.transform.position, PlayerManager.Instance.transform.rotation);

            //스프라이트 렌더러 찾기
            SpriteRenderer ghostSprite = ghostObj.GetComponent<SpriteRenderer>();

            //플레이어 현재 스프라이트 넣기
            ghostSprite.sprite = PlayerManager.Instance.sprite.sprite;

            // 플레이어 레이어 넣기
            ghostSprite.sortingLayerID = PlayerManager.Instance.sprite.sortingLayerID;
            // 플레이어보다 한단계 낮게
            ghostSprite.sortingOrder = PlayerManager.Instance.sprite.sortingOrder - 1;

            //쿨타임 갱신
            ghostCount = 0.005f * PlayerManager.Instance.PlayerStat_Now.moveSpeed;
        }
        else
        {
            ghostCount -= Time.deltaTime;
        }
    }
}
