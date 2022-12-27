using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using System.Linq;
using UnityEngine.Networking;
using SimpleJSON;
using TMPro;
using UnityEngine.UI;
using System.Text;

[System.Serializable]
public class SaveData : System.IDisposable
{
    public int[] unlockMagics; //해금된 마법 리스트
    public string magicDBJson; //마법 DB json
    public string enemyDBJson; //몬스터 DB json
    public string itemDBJson; //아이템 DB json

    public float[] volumes = { 0, 0, 0 }; // 전체볼륨, 배경음, 효과음

    public SaveData()
    {
        //해금 마법 목록
        // unlockMagics = MagicDB.Instance.unlockMagics.ToArray();
        //TODO 해금 캐릭터 목록
        //TODO 소지금(원소젬)
    }

    public void Dispose()
    {
        throw new System.NotImplementedException();
    }
}

public class SaveManager : MonoBehaviour
{
    #region Singleton
    private static SaveManager instance;
    public static SaveManager Instance
    {
        get
        {
            if (instance == null)
            {
                // 비활성화된 오브젝트도 포함 찾기
                var obj = FindObjectOfType<SaveManager>(true);
                if (obj != null)
                {
                    instance = obj;
                }
                else
                {
                    // print("new obj");
                    var newObj = new GameObject().AddComponent<SaveManager>();
                    instance = newObj;
                }
            }
            return instance;
        }
    }
    #endregion

    [Header("State")]
    [ReadOnly] public bool nowSaving = false; //저장 중 여부

    public SaveData webSaveData; // 웹에서 불러온 세이브 데이터
    public SaveData localSaveData; // 로컬에서 불러온 세이브 데이터

    public IEnumerator Save()
    {
        //저장 시작
        nowSaving = true;
        //저장 중일때 저장 아이콘 띄우기
        StartCoroutine(Saving());

        #region SaveData

        // 해금 마법 목록 가져오기
        localSaveData.unlockMagics = MagicDB.Instance.unlockMagics.ToArray();

        // 오디오 옵션값 가져오기
        localSaveData.volumes[0] = SoundManager.Instance.masterVolume;
        localSaveData.volumes[1] = SoundManager.Instance.bgmVolume;
        localSaveData.volumes[2] = SoundManager.Instance.sfxVolume;

        #endregion

        #region Saving

        // SaveData 형태의 데이터를 json 문자열로 변환
        string jsonData = JsonConvert.SerializeObject(localSaveData);

        // 해당 파일 경로에 저장
        File.WriteAllText(Application.persistentDataPath + "/save.json", jsonData);

        print("저장 완료 - " + Application.persistentDataPath + "/save.json");

        nowSaving = false; //저장 끝

        #endregion

        yield return null;
    }

    IEnumerator Saving()
    {
        // 저장 아이콘 켜기
        UIManager.Instance.saveIcon.SetActive(true);

        // 최소 저장 시간 대기
        yield return new WaitForSecondsRealtime(1f);
        // 저장 끝날때까지 대기
        yield return new WaitUntil(() => !nowSaving);

        //저장 아이콘 끄기
        UIManager.Instance.saveIcon.SetActive(false);
    }

    public IEnumerator LoadData()
    {
        // 세이브 파일이 없으면 리턴
        if (!File.Exists(Application.persistentDataPath + "/save.json"))
            yield break;

        string loadData = File.ReadAllText(Application.persistentDataPath + "/save.json");
        // print(loadData);

        // 불러온 json 문자열을 SaveData 형태로 변환해서 변수에 넣기
        localSaveData = JsonConvert.DeserializeObject<SaveData>(loadData);

        // 오디오 옵션값 불러오기
        SoundManager.Instance.Set_MasterVolume(localSaveData.volumes[0]);
        SoundManager.Instance.Set_BGMVolume(localSaveData.volumes[1]);
        SoundManager.Instance.Set_SFXVolume(localSaveData.volumes[2]);

        yield return null;
    }

    public void LoadSet()
    {
        //TODO 해금 캐릭터 목록
        //TODO 소지금 불러오기
    }

    public IEnumerator DBSyncCheck(SystemManager.DBType dbType, Button syncBtn, string uri)
    {
        // 동기화 버튼 노랑불
        if (syncBtn != null)
            syncBtn.targetGraphic.color = Color.yellow;

        // 로컬에 저장된 세이브 불러와 변수에 넣기
        yield return StartCoroutine(LoadData());
        // 웹에서 세이브 불러와 변수에 넣기
        yield return StartCoroutine(WebDataLoad(dbType, uri));

        string localDBJson = "";
        string webDBJson = "";
        // DB 종류에 따라 다른 DB json 불러오기
        switch (dbType)
        {
            case SystemManager.DBType.Magic:
                localDBJson = localSaveData.magicDBJson;
                webDBJson = webSaveData.magicDBJson;
                break;
            case SystemManager.DBType.Enemy:
                localDBJson = localSaveData.enemyDBJson;
                webDBJson = webSaveData.enemyDBJson;
                break;
            case SystemManager.DBType.Item:
                localDBJson = localSaveData.itemDBJson;
                webDBJson = webSaveData.itemDBJson;
                break;
        }

        // print($"{string.Equals(localDBJson, webDBJson)}, {localDBJson.Length} : {webDBJson.Length}");

        if (syncBtn != null)
        {
            // 해당 DB 종류의 로컬 json 문자열과 웹 json 문자열 비교
            if (string.Equals(localDBJson, webDBJson))
            {
                // 동기화 버튼 초록불
                syncBtn.targetGraphic.color = Color.green;

                // 버튼 상호작용 끄기
                // syncBtn.interactable = false;
            }
            else
            {
                // 동기화 버튼 빨간불
                syncBtn.targetGraphic.color = Color.red;

                // 버튼 상호작용 켜기
                // syncBtn.interactable = true;
            }

            // 동기화 아이콘 애니메이션 멈추고 각도 초기화
            syncBtn.transform.Find("SyncIcon").GetComponent<Animator>().enabled = false;
            syncBtn.transform.Find("SyncIcon").rotation = Quaternion.Euler(Vector3.zero);
        }
    }

    public IEnumerator WebDataLoad(SystemManager.DBType dbType, string uri)
    {
        //Apps Script에서 가공된 json 데이터 문서 주소
        UnityWebRequest www = UnityWebRequest.Get(uri);
        // 해당 주소에 요청
        yield return www.SendWebRequest();

        //에러 뜰 경우 에러 표시
        if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.Log("Error : " + www.error);
        }
        else
        {
            // DB 종류에 따라 받아온 json string값 넣어주기
            switch (dbType)
            {
                case SystemManager.DBType.Magic:
                    webSaveData.magicDBJson = www.downloadHandler.text;
                    break;
                case SystemManager.DBType.Enemy:
                    webSaveData.enemyDBJson = www.downloadHandler.text;
                    break;
                case SystemManager.DBType.Item:
                    webSaveData.itemDBJson = www.downloadHandler.text;
                    break;
            }

            yield return null;
        }
    }

    public IEnumerator DBtoEnum()
    {
        string filePath = "Assets/System/Script/" + "DBEnums.cs";

        // enum 파일이 없으면 리턴
        if (!File.Exists(filePath))
            yield break;

        // 입력될 코드 스크립트
        StringBuilder DBEnumScript = new StringBuilder();

        DBEnumScript.Append("using System.Collections;\n");
        DBEnumScript.Append("using System.Collections.Generic;\n");
        DBEnumScript.Append("using UnityEngine;\n");
        DBEnumScript.Append("public class DBEnums : MonoBehaviour\n");
        DBEnumScript.Append("{\n");

        // 마법 enum 작성
        DBEnumScript.Append("public enum MagicDBEnum {");
        foreach (var value in MagicDB.Instance.magicDB)
        {
            // 공백 제거
            string magicName = value.Value.name;
            magicName = magicName.Replace(" ", "");

            // 마법 이름으로 enum에 추가
            DBEnumScript.Append(magicName);

            // 마지막 값이 아닐때만
            if (value.Value.id < MagicDB.Instance.magicDB.Count - 1)
                DBEnumScript.Append(", ");

            yield return null;
        }
        DBEnumScript.Append("};" + "\n\n");

        // 아이템 enum 작성
        DBEnumScript.Append("public enum ItemDBEnum {");
        foreach (var value in ItemDB.Instance.itemDB)
        {
            // 공백 제거
            string itemName = value.Value.name;
            itemName = itemName.Replace(" ", "");

            // 마법 이름으로 enum에 추가
            DBEnumScript.Append(itemName);

            // 마지막 값이 아닐때만
            if (value.Value.id < ItemDB.Instance.itemDB.Count - 1)
                DBEnumScript.Append(", ");

            yield return null;
        }
        DBEnumScript.Append("};" + "\n\n");

        // 몬스터 enum 작성
        DBEnumScript.Append("public enum EnemyDBEnum {");
        foreach (var value in EnemyDB.Instance.enemyDB)
        {
            // 공백 제거
            string enemyName = value.Value.name;
            enemyName = enemyName.Replace(" ", "");

            // 마법 이름으로 enum에 추가
            DBEnumScript.Append(enemyName);

            // 마지막 값이 아닐때만
            if (value.Value.id < EnemyDB.Instance.enemyDB.Count - 1)
                DBEnumScript.Append(", ");

            yield return null;
        }
        DBEnumScript.Append("};" + "\n");
        DBEnumScript.Append("}\n");

        // 해당 파일 경로에 저장
        File.WriteAllText(filePath, DBEnumScript.ToString());
    }

    // public void DBSynchronize(SystemManager.DBType dbType)
    // {
    //     // 데이터 동기화 코루틴 실행
    //     StartCoroutine(DBSync(dbType));
    // }

    // IEnumerator DBSync(SystemManager.DBType dbType)
    // {
    //     // SaveData 형태로 로컬 데이터 로드하기
    //     SaveData saveData = LoadData();

    //     //todo 로컬 json에 웹 json 넣고 저장, 완료시까지 대기
    //     yield return StartCoroutine(SaveManager.Instance.Save());

    //     // DB 타입에따라 데이터 넣어주기
    //     switch (dbType)
    //     {
    //         case SystemManager.DBType.Magic:
    //             StartCoroutine(MagicDB.Instance.GetMagicDB());
    //             break;
    //         case SystemManager.DBType.Enemy:
    //             break;
    //         case SystemManager.DBType.Item:
    //             break;
    //     }

    //     //todo 마법,몬스터,아이템 DB에 로컬 json 데이터로 파싱해서 넣기

    //     //todo 동기화 여부 다시 검사
    //     StartCoroutine(DBSyncCheck(webMagicDBJson, SystemManager.Instance.magicDBSyncBtn, "https://script.googleusercontent.com/macros/echo?user_content_key=7V2ZVIq0mlz0OyEVM8ULXo0nlLHXKPuUIJxFTqfLhj4Jsbg3SVZjnSH4X9KTiksN02j7LG8xCj8EgELL1uGWpX0Tg3k2TlLvm5_BxDlH2jW0nuo2oDemN9CCS2h10ox_1xSncGQajx_ryfhECjZEnD_xj3pGHBsYNBHTy1qMO9_iBmRB6zvsbPv4uu5dqbk-3wD3VcpY-YvftUimQsCyzKs3JAsCIlkQoFkByun7M-8F5ap6m-tpCA&lib=MlJXL_oXznex1TzTWlp6olnqzQVRJChSp"));
    // }
}
