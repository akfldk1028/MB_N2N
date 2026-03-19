using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

/// <summary>
/// Addressable 그룹에 누락된 프리팹을 등록하는 에디터 유틸리티.
/// Tools/Fix Addressables 메뉴에서 실행.
/// </summary>
public static class FixAddressables
{
    private const string UI_BRICKGAME_PATH = "Assets/@Resources/GameScene/UI/UI_BrickGameScene.prefab";
    private const string UI_BRICKGAME_ADDRESS = "UI_BrickGameScene";
    private const string BULLET_PATH = "Assets/@Resources/GameScene/Model/Bullet.prefab";
    private const string BULLET_ADDRESS = "bullet";

    [MenuItem("Tools/Fix Addressables")]
    public static void FixMissingAddressables()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("[FixAddressables] AddressableAssetSettings를 찾을 수 없음. Window > Asset Management > Addressables > Groups 에서 설정 확인");
            return;
        }

        // "Prefab" 그룹을 우선 사용, 없으면 "Prefabs", 없으면 DefaultGroup
        AddressableAssetGroup targetGroup = settings.FindGroup("Prefab")
                                         ?? settings.FindGroup("Prefabs")
                                         ?? settings.DefaultGroup;

        Debug.Log($"[FixAddressables] 대상 그룹: {targetGroup.Name}");

        int registered = 0;

        // 1. UI_BrickGameScene 등록
        registered += RegisterEntry(settings, targetGroup, UI_BRICKGAME_PATH, UI_BRICKGAME_ADDRESS);

        // 2. Bullet 등록 (이미 있으면 주소 소문자로 맞춤)
        registered += RegisterEntry(settings, targetGroup, BULLET_PATH, BULLET_ADDRESS);

        if (registered > 0)
        {
            AssetDatabase.SaveAssets();
            Debug.Log($"[FixAddressables] 완료: {registered}개 항목 등록/갱신됨");
        }
        else
        {
            Debug.Log("[FixAddressables] 변경 사항 없음 (이미 모두 올바르게 등록됨)");
        }
    }

    private static int RegisterEntry(AddressableAssetSettings settings, AddressableAssetGroup group, string assetPath, string address)
    {
        string guid = AssetDatabase.AssetPathToGUID(assetPath);
        if (string.IsNullOrEmpty(guid))
        {
            Debug.LogError($"[FixAddressables] 파일 없음: {assetPath}");
            return 0;
        }

        // 이미 등록된 항목인지 확인 (그룹 내 탐색)
        var existingEntry = settings.FindAssetEntry(guid);
        if (existingEntry != null && existingEntry.address == address)
        {
            Debug.Log($"[FixAddressables] 이미 등록됨: {address} (그룹: {existingEntry.parentGroup.Name})");
            return 0;
        }

        var entry = settings.CreateOrMoveEntry(guid, group, readOnly: false, postEvent: false);
        entry.address = address;

        Debug.Log($"[FixAddressables] 등록됨: '{address}' → {assetPath} (GUID: {guid})");
        return 1;
    }
}