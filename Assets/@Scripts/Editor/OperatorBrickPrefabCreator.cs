using UnityEngine;
using UnityEditor;
using Unity.Assets.Scripts.Objects;

/// <summary>
/// OperatorBrick 프리팹을 자동 생성하는 에디터 스크립트
/// Unity Editor 메뉴: Tools > BrickGame > Create OperatorBrick Prefab
/// </summary>
public class OperatorBrickPrefabCreator : EditorWindow
{
    [MenuItem("Tools/BrickGame/Create OperatorBrick Prefab")]
    public static void CreateOperatorBrickPrefab()
    {
        // 1. 기존 brick 프리팹 찾기
        string[] guids = AssetDatabase.FindAssets("brick t:Prefab");
        GameObject brickPrefab = null;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith("brick.prefab"))
            {
                brickPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Debug.Log($"[OperatorBrickCreator] brick 프리팹 발견: {path}");
                break;
            }
        }

        if (brickPrefab == null)
        {
            EditorUtility.DisplayDialog("Error",
                "brick.prefab을 찾을 수 없습니다!\nAssets 폴더에 brick.prefab이 있는지 확인하세요.",
                "OK");
            return;
        }

        // 2. 복사본 생성
        GameObject operatorBrickInstance = PrefabUtility.InstantiatePrefab(brickPrefab) as GameObject;
        if (operatorBrickInstance == null)
        {
            operatorBrickInstance = Object.Instantiate(brickPrefab);
        }
        operatorBrickInstance.name = "operatorBrick";

        // 3. 기존 Brick 컴포넌트 제거 (있으면)
        var existingBrick = operatorBrickInstance.GetComponent<Brick>();
        if (existingBrick != null)
        {
            DestroyImmediate(existingBrick);
            Debug.Log("[OperatorBrickCreator] 기존 Brick 컴포넌트 제거");
        }

        // 4. OperatorBrick 컴포넌트 추가
        var operatorBrick = operatorBrickInstance.AddComponent<OperatorBrick>();
        Debug.Log("[OperatorBrickCreator] OperatorBrick 컴포넌트 추가됨");

        // 5. 프리팹 저장 경로 결정
        string savePath = "Assets/@Resources/GameScene/operatorBrick.prefab";

        // 폴더가 없으면 생성
        string folderPath = System.IO.Path.GetDirectoryName(savePath);
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            string[] folders = folderPath.Split('/');
            string currentPath = folders[0];
            for (int i = 1; i < folders.Length; i++)
            {
                string nextPath = currentPath + "/" + folders[i];
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, folders[i]);
                }
                currentPath = nextPath;
            }
        }

        // 6. 프리팹으로 저장
        bool success = false;
        PrefabUtility.SaveAsPrefabAsset(operatorBrickInstance, savePath, out success);

        // 7. 임시 인스턴스 삭제
        DestroyImmediate(operatorBrickInstance);

        if (success)
        {
            Debug.Log($"[OperatorBrickCreator] ✅ operatorBrick 프리팹 생성 완료: {savePath}");

            // 8. Addressables 등록 안내
            EditorUtility.DisplayDialog("Success",
                $"operatorBrick.prefab이 생성되었습니다!\n\n" +
                $"경로: {savePath}\n\n" +
                "⚠️ Addressables 등록 필요:\n" +
                "1. 생성된 프리팹 선택\n" +
                "2. Inspector에서 'Addressable' 체크\n" +
                "3. Address를 'operatorBrick'으로 설정",
                "OK");

            // 생성된 프리팹 선택
            var createdPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(savePath);
            Selection.activeObject = createdPrefab;
            EditorGUIUtility.PingObject(createdPrefab);
        }
        else
        {
            EditorUtility.DisplayDialog("Error",
                "프리팹 저장 실패!",
                "OK");
        }
    }

    [MenuItem("Tools/BrickGame/Verify OperatorBrick Setup")]
    public static void VerifySetup()
    {
        // 프리팹 확인
        string[] guids = AssetDatabase.FindAssets("operatorBrick t:Prefab");

        if (guids.Length == 0)
        {
            Debug.LogError("[OperatorBrickVerify] ❌ operatorBrick 프리팹이 없습니다! Tools > BrickGame > Create OperatorBrick Prefab 실행");
            return;
        }

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab != null)
            {
                var opBrick = prefab.GetComponent<OperatorBrick>();
                if (opBrick != null)
                {
                    Debug.Log($"[OperatorBrickVerify] ✅ OperatorBrick 컴포넌트 확인됨: {path}");
                }
                else
                {
                    Debug.LogWarning($"[OperatorBrickVerify] ⚠️ OperatorBrick 컴포넌트 없음: {path}");
                }

                // Addressable 체크
                var addressableEntry = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings?
                    .FindAssetEntry(guid);

                if (addressableEntry != null)
                {
                    Debug.Log($"[OperatorBrickVerify] ✅ Addressable 등록됨: {addressableEntry.address}");
                }
                else
                {
                    Debug.LogWarning($"[OperatorBrickVerify] ⚠️ Addressable 미등록! Inspector에서 'Addressable' 체크하고 주소를 'operatorBrick'으로 설정하세요");
                }
            }
        }
    }
}
