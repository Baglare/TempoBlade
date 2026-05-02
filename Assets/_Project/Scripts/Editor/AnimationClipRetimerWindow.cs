#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

public class AnimationClipRetimerWindow : EditorWindow
{
    private const string WindowTitle = "Animation Retimer";

    private int targetFps = 12;
    private bool loopTime = true;
    private AnimationClip[] selectedClips = new AnimationClip[0];

    [MenuItem("TempoBlade/Animation/Retimer/Retiming Selected Clips")]
    public static void Open()
    {
        AnimationClipRetimerWindow window = GetWindow<AnimationClipRetimerWindow>(true, WindowTitle);
        window.minSize = new Vector2(420f, 220f);
        window.RefreshSelection();
        window.Show();
    }

    [MenuItem("TempoBlade/Animation/Retimer/Retiming Selected Clips", true)]
    private static bool ValidateOpen()
    {
        return Selection.objects.OfType<AnimationClip>().Any();
    }

    private void OnFocus()
    {
        RefreshSelection();
    }

    private void OnSelectionChange()
    {
        RefreshSelection();
        Repaint();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Project panelde secili AnimationClip assetlerini yeniden zamanlar.", EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space();

        targetFps = Mathf.Max(1, EditorGUILayout.IntField("Target FPS", targetFps));
        loopTime = EditorGUILayout.Toggle("Loop Time", loopTime);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Secili Clip Sayisi", selectedClips.Length.ToString());

        if (selectedClips.Length == 0)
        {
            EditorGUILayout.HelpBox("Project panelde en az bir AnimationClip secili olmali.", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.LabelField("Secili Clipler", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                foreach (AnimationClip clip in selectedClips)
                    EditorGUILayout.LabelField("- " + clip.name);
            }
        }

        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(selectedClips.Length == 0))
        {
            if (GUILayout.Button("Retime Selected Clips", GUILayout.Height(32f)))
                RetimingSelectedClips();
        }
    }

    private void RefreshSelection()
    {
        selectedClips = Selection.objects
            .OfType<AnimationClip>()
            .Where(AssetDatabase.Contains)
            .Distinct()
            .ToArray();
    }

    private void RetimingSelectedClips()
    {
        if (selectedClips.Length == 0)
        {
            EditorUtility.DisplayDialog(WindowTitle, "Project panelde secili AnimationClip bulunamadi.", "Tamam");
            return;
        }

        int processedClipCount = 0;
        int retimedBindingCount = 0;

        foreach (AnimationClip clip in selectedClips)
        {
            if (clip == null)
                continue;

            Undo.RegisterCompleteObjectUndo(clip, "Retime Animation Clip");

            bool clipChanged = false;
            EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
            foreach (EditorCurveBinding binding in bindings)
            {
                if (!IsSpriteBinding(binding))
                    continue;

                ObjectReferenceKeyframe[] frames = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                if (frames == null || frames.Length == 0)
                    continue;

                for (int i = 0; i < frames.Length; i++)
                    frames[i].time = i / (float)targetFps;

                AnimationUtility.SetObjectReferenceCurve(clip, binding, frames);
                clipChanged = true;
                retimedBindingCount++;
            }

            if (!Mathf.Approximately(clip.frameRate, targetFps))
            {
                clip.frameRate = targetFps;
                clipChanged = true;
            }

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (settings.loopTime != loopTime)
            {
                settings.loopTime = loopTime;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
                clipChanged = true;
            }

            if (clipChanged)
            {
                EditorUtility.SetDirty(clip);
                processedClipCount++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            WindowTitle,
            $"Islem tamamlandi.\n\nGuncellenen clip: {processedClipCount}\nRetimed sprite binding: {retimedBindingCount}",
            "Tamam");
    }

    private static bool IsSpriteBinding(EditorCurveBinding binding)
    {
        return binding.type == typeof(SpriteRenderer) && binding.propertyName == "m_Sprite";
    }
}
#endif
