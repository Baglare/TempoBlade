#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class EnemyMeleeBoxerAnimationSetup
{
    private const string MenuPath = "TempoBlade/Animation/Build/Enemy Melee Boxer Animation Setup";
    private const string PreferredSourceRoot = "Assets/_Project/Art/Animations/Directional/Enemies/Melee";
    private const string FallbackSourceRoot = "Assets/_Project/Art/Assets/MeleeCharacter";
    private const string OutputFolder = "Assets/_Project/Art/Animations/Directional/Enemies/Melee/Boxer";
    private const string DirectionalSetPath = "Assets/_Project/ScriptableObjects/Animations/EnemyMelee_DirectionalAnimationSet.asset";
    private const string PrefabPath = "Assets/_Project/Prefabs/Enemies/Enemy_Melee.prefab";
    private const string BaseControllerPath = "Assets/_Project/Art/Animations/Directional/BaseController/BaseDirectional.controller";
    private const string ClipBindingPath = "";
    private const int CellWidth = 126;
    private const int CellHeight = 132;
    private const float PixelsPerUnit = 64f;

    private static readonly DirectionSpec[] Directions =
    {
        new DirectionSpec(DirectionalFacing.DownLeft, 1),
        new DirectionSpec(DirectionalFacing.Left, 2),
        new DirectionSpec(DirectionalFacing.UpLeft, 3),
        new DirectionSpec(DirectionalFacing.Up, 4),
        new DirectionSpec(DirectionalFacing.UpRight, 5),
        new DirectionSpec(DirectionalFacing.Right, 6),
        new DirectionSpec(DirectionalFacing.DownRight, 7),
        new DirectionSpec(DirectionalFacing.Down, 8),
    };

    private static readonly StateSpec[] States =
    {
        new StateSpec("Idle", DirectionalAnimationState.Idle, "FightIdle", "Boxer__FightIdle", 24f, true),
        new StateSpec("Move", DirectionalAnimationState.Move, "Run1", "Boxer__Run1", 24f, true),
        new StateSpec("Attack", DirectionalAnimationState.Attack, "StraightRight", "Boxer__StraightRight", 60f, false),
        new StateSpec("Hit", DirectionalAnimationState.Hit, "HitHeadLight", "Boxer__HitHeadLight", 60f, false),
        new StateSpec("Death", DirectionalAnimationState.Death, "Die1", "Boxer__Die1", 60f, false),
    };

    [MenuItem(MenuPath)]
    public static void Build()
    {
        List<string> createdClips = new List<string>();
        List<string> warnings = new List<string>();
        Dictionary<string, AnimationClip> clipLookup = new Dictionary<string, AnimationClip>();

        EnsureFolder(OutputFolder);

        foreach (StateSpec state in States)
        {
            foreach (DirectionSpec direction in Directions)
            {
                string pngPath = ResolveSourcePngPath(state, direction, warnings);
                if (string.IsNullOrEmpty(pngPath))
                    continue;

                if (!ConfigureSpriteSheetImport(pngPath, warnings))
                    continue;

                List<Sprite> sprites = LoadOrderedSprites(pngPath);
                if (sprites.Count == 0)
                {
                    warnings.Add($"Sprite slice bos: {pngPath}");
                    continue;
                }

                string clipName = $"EnemyMelee_{state.outputLabel}_{direction.facing}";
                string clipPath = $"{OutputFolder}/{clipName}.anim";
                AnimationClip clip = CreateOrUpdateClip(clipPath, clipName, state.frameRate, state.loop, sprites);
                createdClips.Add(clipPath);
                clipLookup[BuildKey(state.animationState, direction.facing)] = clip;
            }
        }

        DirectionalAnimationSetSO setAsset = CreateOrUpdateDirectionalSet(clipLookup, warnings);
        RuntimeAnimatorController baseController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(BaseControllerPath);
        if (baseController == null)
            warnings.Add($"Base controller bulunamadi: {BaseControllerPath}");

        UpdateEnemyMeleePrefab(setAsset, baseController, warnings);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string summary = "[EnemyMeleeBoxerSetup]\n" +
                         $"Olusturulan/Guncellenen clip sayisi: {createdClips.Count}\n" +
                         $"Directional set: {DirectionalSetPath}\n" +
                         $"Prefab: {PrefabPath}\n";

        if (createdClips.Count > 0)
            summary += "Clipler:\n - " + string.Join("\n - ", createdClips) + "\n";

        if (warnings.Count > 0)
            summary += "Uyarilar:\n - " + string.Join("\n - ", warnings);

        Debug.Log(summary);
    }

    private static string ResolveSourcePngPath(StateSpec state, DirectionSpec direction, List<string> warnings)
    {
        string fileName = $"{state.filePrefix}_dir{direction.dirIndex}.png";
        string preferredPath = $"{PreferredSourceRoot}/{state.sourceFolder}/{fileName}";
        if (File.Exists(Path.GetFullPath(preferredPath)))
            return preferredPath.Replace("\\", "/");

        string fallbackPath = $"{FallbackSourceRoot}/{state.sourceFolder}/{fileName}";
        if (File.Exists(Path.GetFullPath(fallbackPath)))
            return fallbackPath.Replace("\\", "/");

        warnings.Add($"Kaynak PNG eksik: {state.sourceFolder} / dir{direction.dirIndex} ({fileName})");
        return null;
    }

    private static bool ConfigureSpriteSheetImport(string assetPath, List<string> warnings)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            warnings.Add($"TextureImporter bulunamadi: {assetPath}");
            return false;
        }

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (texture == null)
        {
            warnings.Add($"Texture bulunamadi: {assetPath}");
            return false;
        }

        bool changed = false;

        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            changed = true;
        }

        if (importer.spriteImportMode != SpriteImportMode.Multiple)
        {
            importer.spriteImportMode = SpriteImportMode.Multiple;
            changed = true;
        }

        if (!Mathf.Approximately(importer.spritePixelsPerUnit, PixelsPerUnit))
        {
            importer.spritePixelsPerUnit = PixelsPerUnit;
            changed = true;
        }

        if (importer.filterMode != FilterMode.Point)
        {
            importer.filterMode = FilterMode.Point;
            changed = true;
        }

        if (importer.textureCompression != TextureImporterCompression.Uncompressed)
        {
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            changed = true;
        }

        if (importer.mipmapEnabled)
        {
            importer.mipmapEnabled = false;
            changed = true;
        }

        if (importer.alphaIsTransparency == false)
        {
            importer.alphaIsTransparency = true;
            changed = true;
        }

        int columns = texture.width / CellWidth;
        int rows = texture.height / CellHeight;
        if (columns <= 0 || rows <= 0)
        {
            warnings.Add($"Grid slice olusmadi: {assetPath} ({texture.width}x{texture.height})");
            return false;
        }

        SpriteMetaData[] spriteSheet = BuildGridSliceMeta(assetPath, texture.width, texture.height, columns, rows);
        if (!SpriteSheetEquals(importer.spritesheet, spriteSheet))
        {
            importer.spritesheet = spriteSheet;
            changed = true;
        }

        if (changed)
            importer.SaveAndReimport();

        return true;
    }

    private static SpriteMetaData[] BuildGridSliceMeta(string assetPath, int textureWidth, int textureHeight, int columns, int rows)
    {
        List<SpriteMetaData> meta = new List<SpriteMetaData>(columns * rows);
        string baseName = Path.GetFileNameWithoutExtension(assetPath);
        int index = 0;

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                meta.Add(new SpriteMetaData
                {
                    name = $"{baseName}_{index:D3}",
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    rect = new Rect(column * CellWidth, textureHeight - ((row + 1) * CellHeight), CellWidth, CellHeight)
                });
                index++;
            }
        }

        return meta.ToArray();
    }

    private static bool SpriteSheetEquals(SpriteMetaData[] left, SpriteMetaData[] right)
    {
        if (left == null || right == null)
            return left == right;

        if (left.Length != right.Length)
            return false;

        for (int i = 0; i < left.Length; i++)
        {
            if (left[i].rect != right[i].rect)
                return false;
        }

        return true;
    }

    private static List<Sprite> LoadOrderedSprites(string assetPath)
    {
        return AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath)
            .OfType<Sprite>()
            .OrderByDescending(sprite => sprite.rect.y)
            .ThenBy(sprite => sprite.rect.x)
            .ThenBy(sprite => sprite.name)
            .ToList();
    }

    private static AnimationClip CreateOrUpdateClip(string clipPath, string clipName, float frameRate, bool loop, List<Sprite> sprites)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        bool isNew = clip == null;
        if (isNew)
        {
            clip = new AnimationClip();
            clip.name = clipName;
        }

        clip.frameRate = frameRate;

        EditorCurveBinding binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = ClipBindingPath,
            propertyName = "m_Sprite"
        };

        ObjectReferenceKeyframe[] frames = new ObjectReferenceKeyframe[sprites.Count];
        for (int i = 0; i < sprites.Count; i++)
        {
            frames[i] = new ObjectReferenceKeyframe
            {
                time = i / frameRate,
                value = sprites[i]
            };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, binding, null);
        AnimationUtility.SetObjectReferenceCurve(clip, binding, frames);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        if (isNew)
            AssetDatabase.CreateAsset(clip, clipPath);
        else
            EditorUtility.SetDirty(clip);

        return clip;
    }

    private static DirectionalAnimationSetSO CreateOrUpdateDirectionalSet(Dictionary<string, AnimationClip> clipLookup, List<string> warnings)
    {
        EnsureFolder(Path.GetDirectoryName(DirectionalSetPath)?.Replace("\\", "/") ?? "Assets");

        DirectionalAnimationSetSO setAsset = AssetDatabase.LoadAssetAtPath<DirectionalAnimationSetSO>(DirectionalSetPath);
        if (setAsset == null)
        {
            setAsset = ScriptableObject.CreateInstance<DirectionalAnimationSetSO>();
            AssetDatabase.CreateAsset(setAsset, DirectionalSetPath);
        }

        setAsset.characterId = "EnemyMelee";
        setAsset.displayName = "Enemy Melee";
        setAsset.supportsEightDirections = true;
        setAsset.defaultFacingMode = DirectionalFacingMode.EightDirection;
        setAsset.defaultFacing = DirectionalFacing.Down;
        setAsset.useSpriteFlip = false;
        setAsset.leftRightClipsAreSeparate = true;
        setAsset.flipWhenFacingLeft = false;

        List<DirectionalAnimationClipSlot> slots = new List<DirectionalAnimationClipSlot>();
        foreach (StateSpec state in States)
        {
            foreach (DirectionSpec direction in Directions)
            {
                string key = BuildKey(state.animationState, direction.facing);
                if (!clipLookup.TryGetValue(key, out AnimationClip clip) || clip == null)
                {
                    warnings.Add($"Clip eksik: {state.outputLabel} / {direction.facing}");
                    continue;
                }

                slots.Add(new DirectionalAnimationClipSlot
                {
                    state = state.animationState,
                    direction = direction.facing,
                    clip = clip
                });
            }
        }

        setAsset.clips = slots.ToArray();
        if (clipLookup.TryGetValue(BuildKey(DirectionalAnimationState.Idle, DirectionalFacing.Down), out AnimationClip defaultClip))
            setAsset.defaultClip = defaultClip;

        EditorUtility.SetDirty(setAsset);
        return setAsset;
    }

    private static void UpdateEnemyMeleePrefab(DirectionalAnimationSetSO animationSet, RuntimeAnimatorController baseController, List<string> warnings)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
        if (prefabRoot == null)
        {
            warnings.Add($"Prefab yuklenemedi: {PrefabPath}");
            return;
        }

        try
        {
            Sprite idlePreview = GetFirstSpriteFromClip(animationSet != null ? animationSet.defaultClip : null);

            SpriteRenderer rootRenderer = prefabRoot.GetComponent<SpriteRenderer>();
            if (rootRenderer != null)
                rootRenderer.enabled = false;

            Transform visuals = GetOrCreateChild(prefabRoot.transform, "Visuals");
            Transform body = GetOrCreateChild(visuals, "Body");
            body.localPosition = Vector3.zero;
            body.localRotation = Quaternion.identity;
            body.localScale = Vector3.one;
            body.gameObject.layer = prefabRoot.layer;

            SpriteRenderer bodyRenderer = body.GetComponent<SpriteRenderer>();
            if (bodyRenderer == null)
                bodyRenderer = body.gameObject.AddComponent<SpriteRenderer>();
            bodyRenderer.enabled = true;
            bodyRenderer.sortingLayerName = "Characters";
            bodyRenderer.sortingOrder = 0;
            bodyRenderer.color = Color.white;
            if (idlePreview != null)
                bodyRenderer.sprite = idlePreview;

            Animator bodyAnimator = body.GetComponent<Animator>();
            if (bodyAnimator == null)
                bodyAnimator = body.gameObject.AddComponent<Animator>();
            bodyAnimator.runtimeAnimatorController = baseController;

            SpriteRenderer visualsRenderer = visuals.GetComponent<SpriteRenderer>();
            if (visualsRenderer != null)
                visualsRenderer.enabled = false;

            Animator visualsAnimator = visuals.GetComponent<Animator>();
            if (visualsAnimator != null)
                visualsAnimator.enabled = false;

            SpriteRenderer weaponRenderer = FindNamedChild(prefabRoot.transform, "WeaponVisual")?.GetComponent<SpriteRenderer>();
            if (weaponRenderer == null)
                weaponRenderer = FindNamedChild(prefabRoot.transform, "WeaponSprite")?.GetComponent<SpriteRenderer>();
            if (weaponRenderer != null)
                weaponRenderer.enabled = false;

            IsoVisualRoot visualRoot = prefabRoot.GetComponent<IsoVisualRoot>();
            if (visualRoot == null)
                visualRoot = prefabRoot.AddComponent<IsoVisualRoot>();
            visualRoot.visualRoot = visuals;
            visualRoot.bodyRenderer = bodyRenderer;
            visualRoot.weaponRenderer = weaponRenderer;

            Rigidbody2D rigidbody = prefabRoot.GetComponent<Rigidbody2D>();
            EnemyBase enemy = prefabRoot.GetComponent<EnemyBase>();

            IsoFacingController facingController = prefabRoot.GetComponent<IsoFacingController>();
            if (facingController == null)
                facingController = prefabRoot.AddComponent<IsoFacingController>();
            facingController.preferAimDirection = false;
            facingController.facingPriority = DirectionalFacingPriority.PreferMovementDirection;
            facingController.useEightWayFacing = true;
            facingController.applySpriteFlip = false;
            facingController.body = rigidbody;
            facingController.playerController = null;
            facingController.playerCombat = null;
            facingController.animator = null;
            facingController.spriteFlipTarget = bodyRenderer;

            CharacterDirectionalAnimator directionalAnimator = prefabRoot.GetComponent<CharacterDirectionalAnimator>();
            if (directionalAnimator == null)
                directionalAnimator = prefabRoot.AddComponent<CharacterDirectionalAnimator>();
            directionalAnimator.animationSet = animationSet;
            directionalAnimator.baseController = baseController;
            directionalAnimator.useRuntimeOverrideController = true;
            directionalAnimator.visualRoot = visualRoot;
            directionalAnimator.facingController = facingController;
            directionalAnimator.animator = bodyAnimator;
            directionalAnimator.bodyRenderer = bodyRenderer;
            directionalAnimator.body = rigidbody;
            directionalAnimator.playAutomatically = true;
            directionalAnimator.warnWhenMissingSetup = true;

            DirectionalAnimationStateBridge bridge = prefabRoot.GetComponent<DirectionalAnimationStateBridge>();
            if (bridge == null)
                bridge = prefabRoot.AddComponent<DirectionalAnimationStateBridge>();
            bridge.mode = DirectionalAnimationBridgeMode.Enemy;
            bridge.directionalAnimator = directionalAnimator;
            bridge.playerController = null;
            bridge.playerCombat = null;
            bridge.parrySystem = null;
            bridge.enemy = enemy;

            visualRoot.Resolve();
            directionalAnimator.ResolveReferences();
            directionalAnimator.RebuildOverrideController();
            bridge.ResolveReferences();

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static Sprite GetFirstSpriteFromClip(AnimationClip clip)
    {
        if (clip == null)
            return null;

        EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
        for (int i = 0; i < bindings.Length; i++)
        {
            if (bindings[i].propertyName != "m_Sprite")
                continue;

            ObjectReferenceKeyframe[] frames = AnimationUtility.GetObjectReferenceCurve(clip, bindings[i]);
            if (frames != null && frames.Length > 0)
                return frames[0].value as Sprite;
        }

        return null;
    }

    private static Transform GetOrCreateChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null)
            return child;

        GameObject go = new GameObject(childName);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static Transform FindNamedChild(Transform root, string childName)
    {
        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] != null && children[i].name == childName)
                return children[i];
        }

        return null;
    }

    private static void EnsureFolder(string assetFolder)
    {
        if (string.IsNullOrWhiteSpace(assetFolder))
            return;

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

    private static string BuildKey(DirectionalAnimationState state, DirectionalFacing facing)
    {
        return $"{state}_{facing}";
    }

    private readonly struct StateSpec
    {
        public readonly string outputLabel;
        public readonly DirectionalAnimationState animationState;
        public readonly string sourceFolder;
        public readonly string filePrefix;
        public readonly float frameRate;
        public readonly bool loop;

        public StateSpec(string outputLabel, DirectionalAnimationState animationState, string sourceFolder, string filePrefix, float frameRate, bool loop)
        {
            this.outputLabel = outputLabel;
            this.animationState = animationState;
            this.sourceFolder = sourceFolder;
            this.filePrefix = filePrefix;
            this.frameRate = frameRate;
            this.loop = loop;
        }
    }

    private readonly struct DirectionSpec
    {
        public readonly DirectionalFacing facing;
        public readonly int dirIndex;

        public DirectionSpec(DirectionalFacing facing, int dirIndex)
        {
            this.facing = facing;
            this.dirIndex = dirIndex;
        }
    }
}
#endif
