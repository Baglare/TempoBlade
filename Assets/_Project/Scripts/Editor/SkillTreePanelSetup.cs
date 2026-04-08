#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Aktif sahneye SkillTreePanelUI bileşenli bir GameObject oluşturur.
/// Menü: TempoBlade > Skill Tree > Sahneye Panel Ekle
/// </summary>
public static class SkillTreePanelSetup
{
    [MenuItem("TempoBlade/Skill Tree/Sahneye Panel Ekle")]
    public static void AddPanelToScene()
    {
        // Zaten var mı?
        var existing = Object.FindObjectOfType<SkillTreePanelUI>();
        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            Debug.Log("[SkillTreePanelSetup] Panel zaten sahnede mevcut. Seçildi.");
            return;
        }

        // Yeni GameObject oluştur
        var go = new GameObject("SkillTreePanel");
        Undo.RegisterCreatedObjectUndo(go, "Skill Tree Panel Oluştur");

        var panel = go.AddComponent<SkillTreePanelUI>();

        // DashAxis SO'yu bul ve ata
        string[] guids = AssetDatabase.FindAssets("DashAxis t:ProgressionAxisSO");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            panel.dashAxis = AssetDatabase.LoadAssetAtPath<ProgressionAxisSO>(path);
            Debug.Log($"[SkillTreePanelSetup] DashAxis atandı: {path}");
        }
        else
        {
            Debug.LogWarning("[SkillTreePanelSetup] DashAxis SO bulunamadı! Önce 'Dash Ağacı Asset'leri Oluştur' menüsünü çalıştırın.");
        }

        // EventSystem yoksa ekle
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem");
            Undo.RegisterCreatedObjectUndo(esGO, "EventSystem Oluştur");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            Debug.Log("[SkillTreePanelSetup] EventSystem oluşturuldu.");
        }

        Selection.activeGameObject = go;
        Debug.Log("<color=green>[SkillTreePanelSetup] Skill Tree Panel sahneye eklendi!</color>");
        Debug.Log("  Tab tuşuyla açılır/kapanır. DashAxis SO'yu Inspector'dan atamayı unutmayın.");
    }
}
#endif
