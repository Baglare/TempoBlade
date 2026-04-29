#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class DirectionalAnimationClipBuilderWindow : EditorWindow
{
    private string characterId = "Character";
    private DirectionalAnimationState state = DirectionalAnimationState.Idle;
    private DirectionalFacing direction = DirectionalFacing.Down;
    private int frameRate = 12;
    private bool loop = true;
    private string outputFolder = "Assets/_Project/Art/Animations/Directional";

    [MenuItem("TempoBlade/Animation/Directional Clip Builder")]
    public static void Open()
    {
        GetWindow<DirectionalAnimationClipBuilderWindow>("Directional Clips");
    }

    [MenuItem("TempoBlade/Animation/Create Base Directional Animator Controller")]
    public static void CreateBaseDirectionalController()
    {
        const string folder = "Assets/_Project/Art/Animations/Directional/BaseController";
        EnsureFolder(folder);

        string controllerPath = folder + "/BaseDirectional.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        stateMachine.states = new ChildAnimatorState[0];

        int row = 0;
        foreach (DirectionalAnimationState animationState in DirectionalAnimationUtility.AllStates)
        {
            int column = 0;
            foreach (DirectionalFacing facing in DirectionalAnimationUtility.AllDirections)
            {
                string stateName = DirectionalAnimationUtility.GetStateName(animationState, facing);
                string clipPath = folder + "/" + stateName + ".anim";
                AnimationClip placeholder = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                if (placeholder == null)
                {
                    placeholder = new AnimationClip
                    {
                        name = stateName,
                        frameRate = 1f
                    };
                    AssetDatabase.CreateAsset(placeholder, clipPath);
                }

                AnimatorState animatorState = stateMachine.AddState(stateName, new Vector3(column * 180f, row * 70f, 0f));
                animatorState.motion = placeholder;
                animatorState.writeDefaultValues = true;
                if (row == 0 && column == 0)
                    stateMachine.defaultState = animatorState;

                column++;
            }

            row++;
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = controller;
        EditorGUIUtility.PingObject(controller);
        Debug.Log("[DirectionalAnimation] Base controller hazir: " + controllerPath);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Selected sliced Sprite assetlerinden AnimationClip uretir.", EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space();

        characterId = EditorGUILayout.TextField("Character", characterId);
        state = (DirectionalAnimationState)EditorGUILayout.EnumPopup("State", state);
        direction = (DirectionalFacing)EditorGUILayout.EnumPopup("Direction", direction);
        frameRate = Mathf.Max(1, EditorGUILayout.IntField("Frame Rate", frameRate));
        loop = EditorGUILayout.Toggle("Loop", loop);
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);

        EditorGUILayout.Space();
        if (GUILayout.Button("Create Clip From Selected Sprites"))
            CreateClipFromSelection();
    }

    private void CreateClipFromSelection()
    {
        Sprite[] sprites = Selection.objects
            .OfType<Sprite>()
            .OrderBy(sprite => sprite.name)
            .ToArray();

        if (sprites.Length == 0)
        {
            Debug.LogWarning("[DirectionalAnimation] Secimde sliced Sprite yok.");
            return;
        }

        EnsureFolder(outputFolder);

        string clipName = $"{Sanitize(characterId)}_{state}_{direction}";
        string clipPath = AssetDatabase.GenerateUniqueAssetPath(outputFolder.TrimEnd('/') + "/" + clipName + ".anim");
        AnimationClip clip = new AnimationClip
        {
            name = clipName,
            frameRate = frameRate
        };

        ObjectReferenceKeyframe[] frames = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            frames[i] = new ObjectReferenceKeyframe
            {
                time = i / (float)frameRate,
                value = sprites[i]
            };
        }

        EditorCurveBinding binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = string.Empty,
            propertyName = "m_Sprite"
        };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, frames);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        AssetDatabase.CreateAsset(clip, clipPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = clip;
        EditorGUIUtility.PingObject(clip);
        Debug.Log("[DirectionalAnimation] Clip olusturuldu: " + clipPath);
    }

    private static void EnsureFolder(string assetFolder)
    {
        string normalized = assetFolder.Replace("\\", "/").TrimEnd('/');
        if (AssetDatabase.IsValidFolder(normalized))
            return;

        string[] parts = normalized.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static string Sanitize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Character";

        foreach (char invalid in Path.GetInvalidFileNameChars())
            value = value.Replace(invalid, '_');

        return value.Replace(' ', '_');
    }
}
#endif
