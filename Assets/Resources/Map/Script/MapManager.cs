using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Lean.Pool;
using System.Linq;
using System.Text;
using System.Diagnostics;

public class MapManager : MonoBehaviour
{
    #region Singleton
    private static MapManager instance;
    public static MapManager Instance
    {
        get
        {
            if (instance == null)
            {
                var obj = FindObjectOfType<MapManager>();
                if (obj != null)
                {
                    instance = obj;
                }
                // else
                // {
                //     var newObj = new GameObject().AddComponent<MapManager>();
                //     instance = newObj;
                // }
            }
            return instance;
        }
    }
    #endregion

    [Header("State")]
    public float portalRange = 100f; //포탈게이트 생성될 범위
    [SerializeField] float playerDistance = 5f; // 플레이어 이내 설치 금지 거리
    [SerializeField] int maxPropAttempt = 10; // 사물 생성 시도 최대 횟수
    [SerializeField] Vector3Int tilemapSize; // 설치할 사이즈
    [SerializeField] float boundRate = 0.5f; // 맵 생성 경계 비율
    float rightX, leftX, upY, downY; // 4방향 경계 좌표
    [SerializeField] List<Vector2> genPosList = new List<Vector2>();

    [Header("Tile")]
    [SerializeField] List<GameObject> tileSetPrefab = new List<GameObject>(); // 속성별 타일셋 리스트
    [SerializeField] List<TileLayer> tileBundle_Bottom = new List<TileLayer>(); // 속성별 타일 번들 리스트, 가장 아래층
    [SerializeField] List<TileLayer> tileBundle_Middle = new List<TileLayer>(); // 속성별 타일 번들 리스트, 중간층
    [SerializeField] List<TileLayer> tileBundle_Deco = new List<TileLayer>(); // 속성별 타일 번들 리스트, 장식 타일
    [SerializeField] TileMapGenerator[] tileGenList = new TileMapGenerator[3]; // 타일맵 생성기 리스트

    [Header("Refer")]
    [SerializeField] List<PropBundle> propBundleList = new List<PropBundle>(); // 속성별 장애물 번들 리스트
    public GameObject portalGate; //다음 맵 넘어가는 포탈게이트 프리팹
    public Transform[] background = null;
    [SerializeField] List<Vector3> initPos = new List<Vector3>(); // 초기화 된 맵위치 리스트

    [Header("Debug")]
    [SerializeField] bool blockTileShow = false; // 장애물 설치 금지 타일 표시
    [SerializeField] bool propTileShow = false; // 장애물 설치 타일 표시

    private void OnEnable()
    {
        // 초기화
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 설치할 타일맵 사이즈 절반만큼 경계 설정
        rightX = tilemapSize.x / 2f;
        leftX = -tilemapSize.x / 2f;
        upY = tilemapSize.y / 2f;
        downY = -tilemapSize.y / 2f;

        // print(rightX + " : " + leftX + " : " + upY + " : " + downY);

        //todo 타일셋 프리팹 소환하고 변수 기억하기
        GameObject tileSet = LeanPool.Spawn(tileSetPrefab[(int)SystemManager.Instance.nowMapElement], Vector2.zero, Quaternion.identity, transform);
        TileMapGenerator[] tileGens = tileSet.GetComponentsInChildren<TileMapGenerator>();
        for (int i = 0; i < tileGens.Length; i++)
        {
            tileGenList[i] = tileGens[i];
        }

        // 바닥 위치 초기화
        GenerateMap();

        // 플레이어 초기화 대기
        yield return new WaitUntil(() => PlayerManager.Instance);

        //다음맵으로 넘어가는 포탈게이트 생성하기
        SpawnPortalGate();

        yield return null;

        // 시간 초기화
        SystemManager.Instance.TimeScaleChange(1f);
    }

    void Update()
    {
        if (!SystemManager.Instance.loadDone
        || PlayerManager.Instance == null)
            return;

        // 플레이어가 경계 끝에 왔을때 배경 9개 전부 위치 변경
        if (PlayerManager.Instance.transform.position.x >= rightX)
        {
            // 경계 갱신
            rightX += tilemapSize.x;
            leftX += tilemapSize.x;

            // 새 위치에 타일 설치
            GenerateMap(new Vector3(tilemapSize.x, 0));
        }

        if (PlayerManager.Instance.transform.position.x <= leftX)
        {
            // 경계 갱신
            rightX -= tilemapSize.x;
            leftX -= tilemapSize.x;

            // 새 위치에 타일 설치
            GenerateMap(new Vector3(-tilemapSize.x, 0));
        }

        if (PlayerManager.Instance.transform.position.y >= upY)
        {
            // 경계 갱신
            upY += tilemapSize.y;
            downY += tilemapSize.y;

            // 새 위치에 타일 설치
            GenerateMap(new Vector3(0, tilemapSize.y));
        }

        if (PlayerManager.Instance.transform.position.y <= downY)
        {
            // 경계 갱신
            upY -= tilemapSize.y;
            downY -= tilemapSize.y;

            // 새 위치에 타일 설치
            GenerateMap(new Vector3(0, -tilemapSize.y));
        }
    }

    void GenerateMap(Vector3 movePos = default)
    {
        // 플레이어 서있는 타일셋 중심 위치 계산
        Vector2 setPos = new Vector2((leftX + rightX) / 2f, (upY + downY) / 2f);

        // 초기 설치 위치
        Vector2 defaultPos = setPos - new Vector2(tilemapSize.x, tilemapSize.y);

        // 총 9번 설치
        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                // 타일셋 사이즈만큼 중심위치 이동
                Vector2 genPos = defaultPos + new Vector2(tilemapSize.x * x, tilemapSize.y * y);

                // 이미 소환된 위치일때 넘기기
                if (genPosList.Exists(x => x == genPos))
                    continue;
                // 리스트에 없으면
                else
                    // 리스트에 저장
                    genPosList.Add(genPos);

                // 타일셋 사이즈 
                Vector3Int genSize = new Vector3Int(Mathf.FloorToInt(tilemapSize.x / 2f), Mathf.FloorToInt(tilemapSize.y / 2f), 0);
                // 타일맵 위치로 전환
                Vector3Int nowMapPos = new Vector3Int(Mathf.FloorToInt(genPos.x / 2f), Mathf.FloorToInt(genPos.y / 2f), 0);

                // 장애물이 들어갈 수 있는 빈 타일 리스트
                List<Vector2> emptyTileList = new List<Vector2>();
                // // 타일 전체 사이즈만큼 빈 타일의 좌표 리스트 만들기
                // for (int _x = 0; _x < tilemapSize.x / 2f; _x++)
                //     for (int _y = 0; _y < tilemapSize.y / 2f; _y++)
                //     {
                //         emptyTileList.Add(new Vector2(_x, _y) * 2);
                //     }

                // 각 타일맵마다 설치된 타일 리스트
                List<Vector2> tileSet = new List<Vector2>();

                // 하단 레이어 타일 설치
                tileSet = tileGenList[0].GenTile(genSize, nowMapPos, tileBundle_Bottom[(int)SystemManager.Instance.nowMapElement].tileBundle);

                // 빈 타일에 설치된 좌표 넣기
                if (tileGenList[0].includePropTile)
                    emptyTileList = tileSet.ToList();
                // 빈타일에서 설치된 좌표 빼기
                if (tileGenList[0].excludePropTile)
                    emptyTileList = EvadeTile(emptyTileList, tileSet, genPos);

                // 중간 레이어 타일 설치
                tileSet = tileGenList[1].GenTile(genSize, nowMapPos, tileBundle_Middle[(int)SystemManager.Instance.nowMapElement].tileBundle);

                // 빈 타일에 설치된 좌표 넣기
                if (tileGenList[1].includePropTile)
                    for (int i = 0; i < tileSet.Count; i++)
                        emptyTileList.Add(tileSet[i]);
                // 빈타일에서 설치된 좌표 빼기
                if (tileGenList[1].excludePropTile)
                    emptyTileList = EvadeTile(emptyTileList, tileSet, genPos);

                // 상단 레이어 타일 설치
                tileSet = tileGenList[2].GenTile(genSize, nowMapPos, tileBundle_Deco[(int)SystemManager.Instance.nowMapElement].tileBundle);

                // 빈 타일에 설치된 좌표 넣기
                if (tileGenList[2].includePropTile)
                    for (int i = 0; i < tileSet.Count; i++)
                        emptyTileList.Add(tileSet[i]);
                // 빈타일에서 설치된 좌표 빼기
                if (tileGenList[2].excludePropTile)
                    emptyTileList = EvadeTile(emptyTileList, tileSet, genPos);

                // 장애물 설치하기
                StartCoroutine(SpawnProp(genPos, emptyTileList));
            }
        }
    }

    List<Vector2> EvadeTile(List<Vector2> emptyTileList, List<Vector2> setList, Vector2 genPos)
    {
        // 상단 타일 설치된 좌표 빼기
        for (int i = 0; i < setList.Count; i++)
        {
            // setList 타일 위치 빼기
            emptyTileList.Remove(setList[i]);

            //! 뺀 타일 위치 표시
            if (blockTileShow)
                LeanPool.Spawn(SystemManager.Instance.targetPos_Red, setList[i] + genPos - new Vector2(tilemapSize.x, tilemapSize.y) / 2f + Vector2.one, Quaternion.identity, transform);
        }

        return emptyTileList;
    }

    IEnumerator SpawnProp(Vector2 groundPos, List<Vector2> emptyTileList)
    {
        //! 시간 측정
        // Stopwatch debugTime = new Stopwatch();
        // debugTime.Start();

        // 맵 속성에 따라 다른 장애물 번들 선택
        List<Prop> propBundle = propBundleList[(int)SystemManager.Instance.nowMapElement].props;

        //! 빈공간 모두 표시
        // for (int i = 0; i < emptyTileList.Count; i++)
        //     LeanPool.Spawn(SystemManager.Instance.targetPos_Blue, emptyTileList[i] + groundPos - new Vector2(tilemapSize.x, tilemapSize.y) / 2f, Quaternion.identity, transform);

        // 장애물 풀을 스택에 쌓고 사이즈 큰것부터 설치
        List<Prop> propStack = new List<Prop>();
        // 모든 사물 반복
        for (int i = 0; i < propBundle.Count; i++)
            // 해당 사물 개수만큼 반복
            for (int j = 0; j < propBundle[i].amount; j++)
            {
                // 생성 확률 통과 못하면 넘기기
                if (Random.value > propBundle[i].spawnRate)
                    continue;

                Prop nowProp = propBundle[i];

                // 사이즈 입력되어 있지 않으면
                if (nowProp.propSize == Vector2.zero)
                {
                    // 사이즈 알아내서 기억
                    nowProp.propSize = propBundle[i].propPrefab.GetComponentInChildren<SpriteRenderer>().sprite.bounds.size;
                    // 올림해서 정수로 만들기
                    nowProp.propSize = new Vector2(Mathf.CeilToInt(nowProp.propSize.x), Mathf.CeilToInt(nowProp.propSize.y)) / 2f;
                }

                // 스택에 장애물 넣기
                propStack.Add(nowProp);
            }

        // 사이즈 큰 순으로 정렬
        propStack = propStack.OrderBy(prop => prop.propSize.x * prop.propSize.y).Reverse().ToList();

        // 스택 개수만큼 반복
        for (int i = 0; i < propStack.Count; i++)
        {
            yield return null;

            // 임의 횟수만큼 반복
            for (int k = 0; k < maxPropAttempt; k++)
            {
                // 빈타일 없으면 끝내기
                if (emptyTileList.Count <= 0)
                    yield break;

                // 비어있는 랜덤 좌표 뽑기
                Vector2 randomPos = emptyTileList[Random.Range(0, emptyTileList.Count)];

                // 너무 커서 넣는데 실패한 사이즈를 산출
                Vector2 failSize = TileCheck(emptyTileList, propStack[i].propSize, randomPos);

                // print(failSize);

                // 자리가 한칸보다 작으면 넘기기
                if (failSize.x < 2 || failSize.y < 2)
                    continue;

                // 스택 개수만큼 반복
                for (int j = 0; j < propStack.Count - i; j++)
                {
                    // yield return null;
                    Prop prop = propStack[i + j];
                    Vector2 propSize = prop.propSize;

                    // 다음 장애물도 해당 사이즈보다 크면 넘기기
                    if (propSize.x * 2 > failSize.x
                    || propSize.y * 2 > failSize.y)
                        continue;

                    // 설치 좌표 계산
                    Vector2 spawnPos = randomPos + new Vector2(groundPos.x - tilemapSize.x / 2f, groundPos.y - tilemapSize.y / 2f);

                    // 해당 타일에 장애물 설치하고 끝내기
                    GameObject propObj = LeanPool.Spawn(prop.propPrefab, spawnPos, Quaternion.identity, ObjectPool.Instance.objectPool);

                    // 해당하는 모든 타일 리스트에서 빼기
                    for (int x = 0; x < propSize.x; x++)
                        for (int y = 0; y < propSize.y; y++)
                        {
                            emptyTileList.Remove(randomPos + new Vector2(x, y) * 2);

                            //! 장애물 차지 위치 표시
                            if (propTileShow)
                                LeanPool.Spawn(SystemManager.Instance.targetPos_Blue, spawnPos + new Vector2(x, y) * 2 + Vector2.one, Quaternion.identity, transform);
                        }

                    // 해당 사물 확률에 따라 뒤집기
                    TransformControl transformControl = propObj.GetComponentInChildren<TransformControl>();
                    TransformControl.Shuffle shuffle = transformControl.ShuffleTransform();

                    // 뒤집었을때
                    if (shuffle == TransformControl.Shuffle.MirrorX)
                        // 뒤집기 컴포넌트가 장애물 본인이면 (피벗이 왼쪽 구석에 있으므로)
                        if (transformControl.transform == propObj.transform)
                            // 오른쪽으로 X 좌표 절반만큼 이동
                            transformControl.transform.position += Vector3.right * propSize.x * 2;

                    // 성공시 다음 장애물로 넘어감
                    break;
                }

                // 성공시 다음 스택으로 넘어감
                break;
            }
        }

        // 걸린 시간 측정
        // debugTime.Stop();
        // print($"{new Vector2(groundPos.x / tilemapSize.x, groundPos.y / tilemapSize.y)} : {debugTime.ElapsedMilliseconds / 1000f}s");
    }

    Vector2 TileCheck(List<Vector2> emptyTileList, Vector2 propSize, Vector2 randomPos)
    {
        // 해당 타일 차지공간만큼 반복
        for (int x = 0; x < propSize.x; x++)
            for (int y = 0; y < propSize.y; y++)
                // 랜덤 좌표에 사이즈 좌표 반영한 타일 중에서 하나라도 빈타일 리스트에 없으면
                if (!emptyTileList.Exists(a => a == randomPos + new Vector2(x, y) * 2))
                {
                    // 해당 좌표는 이미 자리 있음
                    return new Vector2(x, y) * 2;
                }

        return new Vector2(100, 100);
    }

    void SpawnPortalGate()
    {
        //포탈이 생성될 위치
        Vector2 pos = (Vector2)PlayerManager.Instance.transform.position + UnityEngine.Random.insideUnitCircle.normalized * portalRange;

        //포탈 게이트 생성
        GameObject gate = LeanPool.Spawn(portalGate, pos, Quaternion.identity, ObjectPool.Instance.objectPool);
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        // 경계 모서리 표시
        Gizmos.DrawLine(new Vector2(leftX, downY), new Vector2(leftX, upY));
        Gizmos.DrawLine(new Vector2(rightX, downY), new Vector2(rightX, upY));
        Gizmos.DrawLine(new Vector2(leftX, downY), new Vector2(rightX, downY));
        Gizmos.DrawLine(new Vector2(leftX, upY), new Vector2(rightX, upY));
    }

    private void OnDrawGizmosSelected()
    {

        Gizmos.color = Color.blue;
        // 경계 모서리 표시
        Gizmos.DrawLine(new Vector2(leftX, downY), new Vector2(leftX, upY));
        Gizmos.DrawLine(new Vector2(rightX, downY), new Vector2(rightX, upY));
        Gizmos.DrawLine(new Vector2(leftX, downY), new Vector2(rightX, downY));
        Gizmos.DrawLine(new Vector2(leftX, upY), new Vector2(rightX, upY));
    }
}

[System.Serializable]
public class PropBundle
{
    public string name;
    public List<Prop> props = new List<Prop>();
}

[System.Serializable]
public class Prop
{
    public string name;
    public GameObject propPrefab; // 사물 프리팹
    public GameObject propObj; // 생성된 사물 인스턴스

    public Vector2 propSize; // 해당 장애물 크기
    public int amount = 1; // 소환 개수
    [Range(0f, 1f)]
    public float spawnRate = 1f; // 소환 될 확률
}

[System.Serializable]
public class TileLayer
{
    public string name;
    public List<TileBundle> tileBundle;
}

[System.Serializable]
public class TileBundle
{
    public string name;
    public RuleTile tile;
    public float weightRate; // 해당 레이어에서 타일 출현 가중치
    [Range(0, 1)] public float rate; // 해당 타일 출현 확률
}