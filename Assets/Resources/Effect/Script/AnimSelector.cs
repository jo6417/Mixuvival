using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimSelector : MonoBehaviour
{
    [SerializeField] Animator anim;

    [SerializeField] List<float> patternWeight; // 패턴 가중치

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 애니메이터 끄기
        anim.enabled = false;

        // 1초 이내 랜덤 딜레이 대기
        yield return new WaitForSeconds(Random.value);

        // 패턴 고정
        int pattern = SystemManager.Instance.WeightRandom(patternWeight);

        // 고정 패턴 없으면 랜덤
        pattern = Random.Range(0, patternWeight.Count);

        anim.SetInteger("Pattern", pattern);

        // 애니메이터 켜기
        anim.enabled = true;
    }
}