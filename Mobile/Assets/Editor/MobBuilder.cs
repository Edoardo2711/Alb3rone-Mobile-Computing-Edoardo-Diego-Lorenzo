#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Tool Editor per generare prefab, animazioni e AnimatorController di un nuovo mob
/// usando un mob esistente (di default lo Zombie) come template.
///
/// Cosa fa:
///   - Slice automatico delle sprite (se non gia' fatto) in modalita' "grid by cell size"
///     assumendo strip orizzontale di frame quadrati.
///   - Crea AnimationClip per ogni azione (Idle/Walk/Attack/Hit/Death) x direzione (Down/Left/Right/Up).
///   - Copia il controller dello Zombie (con i suoi Blend Tree) e rimpiazza le clip.
///   - Copia il prefab dello Zombie e gli assegna il nuovo controller + sprite di default.
///
/// Struttura cartelle richiesta:
///   Assets/Mob/&lt;NomeMob&gt;/Sprite/Idle/&lt;prefix&gt;_idle_&lt;dir&gt;.png
///   Assets/Mob/&lt;NomeMob&gt;/Sprite/Walk/&lt;prefix&gt;_walk_&lt;dir&gt;.png
///   Assets/Mob/&lt;NomeMob&gt;/Sprite/Attacco/&lt;prefix&gt;_attack_&lt;dir&gt;.png
///   Assets/Mob/&lt;NomeMob&gt;/Sprite/Hit/&lt;prefix&gt;_hit_&lt;dir&gt;.png
///   Assets/Mob/&lt;NomeMob&gt;/Sprite/Death/&lt;prefix&gt;_death_&lt;dir&gt;.png
///
/// Menu: Tools -> Mob Builder -> Slice & Crea Slime
/// </summary>
public static class MobBuilder
{
    // ---------------- Template di default ----------------
    private const string DEFAULT_TEMPLATE_PREFAB     = "Assets/Mob/Zombie/Zombie.prefab";
    private const string DEFAULT_TEMPLATE_CONTROLLER = "Assets/Mob/Zombie/Animazioni/Zombie.controller";

    private static readonly string[] DIRECTIONS = { "Down", "Left", "Right", "Up" };

    // Ogni azione ha il proprio frame rate: un valore unico per tutte appiattiva
    // i timing (un attacco vuole essere piu' rapido di una morte). I valori sono
    // quelli usati dalle clip dello Zombie, che fa da template.
    private static readonly (string action, string spriteSubfolder, bool loop, float fps)[] ACTIONS =
    {
        ("Idle",   "Idle",    true,   6f),
        ("Walk",   "Walk",    true,   8f),
        ("Attack", "Attacco", false, 10f),
        ("Hit",    "Hit",     false,  8f),
        ("Death",  "Death",   false,  5f),
    };

    // ============================================================
    //   MENU ITEMS
    // ============================================================

    [MenuItem("Tools/Mob Builder/Slice & Crea Slime")]
    public static void SliceAndBuildSlime()
    {
        SliceAndBuild("Slime", "Assets/Mob/Slime", "slime");
    }

    [MenuItem("Tools/Mob Builder/Slice & Crea Slime", true)]
    public static bool ValidateSlime() => AssetDatabase.IsValidFolder("Assets/Mob/Slime");

    [MenuItem("Tools/Mob Builder/Solo Slice/Slime Sprites")]
    public static void SliceOnlySlime()
    {
        SliceAllInMobFolder("Assets/Mob/Slime");
    }

    [MenuItem("Tools/Mob Builder/Solo Build (sprite gia' slicate)/Crea Slime")]
    public static void BuildOnlySlime()
    {
        BuildMob("Slime", "Assets/Mob/Slime", "slime");
    }

    // ============================================================
    //   API PUBBLICA
    // ============================================================

    /// <summary>
    /// Funzione che fa tutto: slice automatico + creazione anim/controller/prefab.
    /// </summary>
    public static void SliceAndBuild(string mobName, string mobFolder, string spritePrefix,
                                     string templatePrefab = DEFAULT_TEMPLATE_PREFAB,
                                     string templateController = DEFAULT_TEMPLATE_CONTROLLER)
    {
        SliceAllInMobFolder(mobFolder);
        BuildMob(mobName, mobFolder, spritePrefix, templatePrefab, templateController);
    }

    /// <summary>
    /// Slice automatico di tutte le PNG dentro le sottocartelle Sprite/* del mob.
    /// Assume strip orizzontale di frame quadrati (frameWidth = textureHeight,
    /// frameCount = textureWidth / textureHeight).
    /// </summary>
    public static void SliceAllInMobFolder(string mobFolder)
    {
        if (!AssetDatabase.IsValidFolder(mobFolder))
        {
            Debug.LogError($"[MobBuilder/Slicer] Cartella non trovata: {mobFolder}");
            return;
        }

        string spriteFolder = $"{mobFolder}/Sprite";
        if (!AssetDatabase.IsValidFolder(spriteFolder))
        {
            Debug.LogWarning($"[MobBuilder/Slicer] Manca {spriteFolder}");
            return;
        }

        Debug.Log($"[MobBuilder/Slicer] === Slicing PNG in {spriteFolder} ===");

        try
        {
            AssetDatabase.StartAssetEditing();
            string[] pngGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { spriteFolder });
            int sliced = 0, skipped = 0;
            foreach (var guid in pngGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (SliceTextureAutoGrid(path)) sliced++; else skipped++;
            }
            Debug.Log($"[MobBuilder/Slicer]  -> {sliced} texture slicate, {skipped} saltate (gia' slicate o errore).");
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    /// <summary>
    /// Importa la texture come Multiple Sprite e genera la grid di frame quadrati
    /// assumendo strip orizzontale (frame quadrati, lato = altezza texture).
    /// Ritorna true se ha modificato l'asset, false se era gia' a posto / errore.
    /// </summary>
    static bool SliceTextureAutoGrid(string texturePath)
    {
        var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer == null) return false;

        // Forza pixel art friendly
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 100;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        // Carica le dimensioni reali della texture
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (tex == null) return false;
        int texW = tex.width;
        int texH = tex.height;

        // Se gia' in modalita' Multiple con sprite definiti, controlla se serve re-slicing
        if (importer.spriteImportMode == SpriteImportMode.Multiple)
        {
            var dataProvider = new UnityEditor.U2D.Sprites.SpriteDataProviderFactories();
            dataProvider.Init();
            var dp = dataProvider.GetSpriteEditorDataProviderFromObject(importer);
            dp.InitSpriteEditorDataProvider();
            var rects = dp.GetSpriteRects();
            if (rects != null && rects.Length > 0)
                return false; // gia' slicato, non rifare
        }

        // Calcola la grid: assume frame quadrati, lato = altezza texture
        int frameSize = texH;
        if (frameSize <= 0 || texW < frameSize) return false;
        int frameCount = texW / frameSize;
        if (frameCount < 2) return false; // niente da slicare

        importer.spriteImportMode = SpriteImportMode.Multiple;

        // Costruisci i SpriteRect (uno per ogni frame, in orizzontale)
        var newRects = new List<SpriteRect>();
        string baseName = Path.GetFileNameWithoutExtension(texturePath);
        for (int i = 0; i < frameCount; i++)
        {
            newRects.Add(new SpriteRect
            {
                name = $"{baseName}_{i}",
                rect = new Rect(i * frameSize, 0, frameSize, frameSize),
                alignment = SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f),
                border = Vector4.zero,
            });
        }

        // Applica i rect via SpriteDataProvider (API moderna)
        var factory = new UnityEditor.U2D.Sprites.SpriteDataProviderFactories();
        factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        provider.SetSpriteRects(newRects.ToArray());
        provider.Apply();

        importer.SaveAndReimport();
        Debug.Log($"[MobBuilder/Slicer]  + Slice '{texturePath}': {frameCount} frame da {frameSize}x{frameSize}px");
        return true;
    }

    /// <summary>
    /// Crea il prefab, AnimationClip e AnimatorController per il mob, a partire
    /// dalle sprite gia' slicate. Usa il prefab/controller dello Zombie come template.
    /// </summary>
    public static void BuildMob(string mobName, string mobFolder, string spritePrefix,
                                string templatePrefab = DEFAULT_TEMPLATE_PREFAB,
                                string templateController = DEFAULT_TEMPLATE_CONTROLLER)
    {
        try
        {
            AssetDatabase.StartAssetEditing();

            Debug.Log($"[MobBuilder] === Costruzione mob '{mobName}' ===");

            if (!File.Exists(templatePrefab))
            {
                Debug.LogError($"[MobBuilder] Template prefab non trovato: {templatePrefab}");
                return;
            }
            if (!File.Exists(templateController))
            {
                Debug.LogError($"[MobBuilder] Template controller non trovato: {templateController}");
                return;
            }
            if (!AssetDatabase.IsValidFolder(mobFolder))
            {
                Debug.LogError($"[MobBuilder] Cartella del mob non trovata: {mobFolder}");
                return;
            }

            // 1. Crea le animation clip dalle sprite
            string animazioniFolder = $"{mobFolder}/Animazioni";
            EnsureFolder(animazioniFolder);

            Dictionary<string, AnimationClip> createdClips = new Dictionary<string, AnimationClip>();

            foreach (var (action, spriteSubfolder, loop, fps) in ACTIONS)
            {
                string actionAnimFolder = $"{animazioniFolder}/{spriteSubfolder}";
                EnsureFolder(actionAnimFolder);

                foreach (var dir in DIRECTIONS)
                {
                    string spritePath = $"{mobFolder}/Sprite/{spriteSubfolder}/{spritePrefix}_{action.ToLower()}_{dir.ToLower()}.png";
                    if (!File.Exists(spritePath))
                    {
                        Debug.LogWarning($"[MobBuilder] Sprite mancante: {spritePath} -> salto");
                        continue;
                    }

                    var sprites = AssetDatabase.LoadAllAssetsAtPath(spritePath)
                        .OfType<Sprite>()
                        .OrderBy(s => NaturalSortKey(s.name))
                        .ToArray();

                    if (sprites.Length == 0)
                    {
                        Debug.LogWarning($"[MobBuilder] Nessuna sprite slicata in {spritePath}. Usa 'Slice & Crea' o slica manualmente.");
                        continue;
                    }

                    string clipName = $"{action}_{dir}";
                    string clipPath = $"{actionAnimFolder}/{clipName}.anim";

                    var clip = CreateAnimationClip(sprites, clipName, loop, fps);
                    if (File.Exists(clipPath)) AssetDatabase.DeleteAsset(clipPath);
                    AssetDatabase.CreateAsset(clip, clipPath);
                    createdClips[clipName] = clip;

                    Debug.Log($"[MobBuilder]  + animazione {clipPath} ({sprites.Length} frame @ {fps}fps, loop={loop})");
                }
            }

            if (createdClips.Count == 0)
            {
                Debug.LogError($"[MobBuilder] Nessuna animazione creata. Sprite slicate? Cartelle corrette?");
                return;
            }

            // 2. Copia il controller template e rimpiazza le clip
            string controllerPath = $"{animazioniFolder}/{mobName}.controller";
            if (File.Exists(controllerPath)) AssetDatabase.DeleteAsset(controllerPath);
            if (!AssetDatabase.CopyAsset(templateController, controllerPath))
            {
                Debug.LogError($"[MobBuilder] Impossibile copiare il controller da {templateController}");
                return;
            }
            AssetDatabase.ImportAsset(controllerPath);

            var newController = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (newController == null)
            {
                Debug.LogError("[MobBuilder] Impossibile caricare il controller copiato.");
                return;
            }

            int remapped = RemapControllerClips(newController, createdClips);
            Debug.Log($"[MobBuilder]  + Controller '{controllerPath}': rimappate {remapped} clip nei BlendTree/state.");

            // 3. Copia il prefab template e aggiorna i riferimenti
            string prefabPath = $"{mobFolder}/{mobName}.prefab";
            if (File.Exists(prefabPath)) AssetDatabase.DeleteAsset(prefabPath);
            if (!AssetDatabase.CopyAsset(templatePrefab, prefabPath))
            {
                Debug.LogError($"[MobBuilder] Impossibile copiare il prefab da {templatePrefab}");
                return;
            }
            AssetDatabase.ImportAsset(prefabPath);

            var prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);
            if (prefabContents == null)
            {
                Debug.LogError($"[MobBuilder] Impossibile caricare il prefab {prefabPath}");
                return;
            }

            prefabContents.name = mobName;

            var animator = prefabContents.GetComponentInChildren<Animator>(true);
            if (animator != null)
                animator.runtimeAnimatorController = newController;
            else
                Debug.LogWarning("[MobBuilder] Nessun Animator trovato nel prefab template.");

            var sr = prefabContents.GetComponentInChildren<SpriteRenderer>(true);
            if (sr != null)
            {
                string defaultSpritePath = $"{mobFolder}/Sprite/Idle/{spritePrefix}_idle_down.png";
                if (File.Exists(defaultSpritePath))
                {
                    var idleSprites = AssetDatabase.LoadAllAssetsAtPath(defaultSpritePath)
                        .OfType<Sprite>()
                        .OrderBy(s => NaturalSortKey(s.name))
                        .ToArray();
                    if (idleSprites.Length > 0)
                        sr.sprite = idleSprites[0];
                }
            }

            PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);
            PrefabUtility.UnloadPrefabContents(prefabContents);

            Debug.Log($"[MobBuilder] === Mob '{mobName}' creato con successo: {prefabPath} ===");
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    // ============================================================
    //   HELPERS
    // ============================================================

    static AnimationClip CreateAnimationClip(Sprite[] sprites, string name, bool loop, float fps)
    {
        var clip = new AnimationClip { name = name, frameRate = Mathf.Max(1f, fps) };

        var binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            propertyName = "m_Sprite",
            path = ""
        };

        var keyframes = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = i / clip.frameRate,
                value = sprites[i]
            };
        }
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        return clip;
    }

    static int RemapControllerClips(AnimatorController controller, Dictionary<string, AnimationClip> newClips)
    {
        int remapped = 0;
        foreach (var layer in controller.layers)
            remapped += RemapStateMachine(layer.stateMachine, newClips);
        EditorUtility.SetDirty(controller);
        return remapped;
    }

    static int RemapStateMachine(AnimatorStateMachine sm, Dictionary<string, AnimationClip> newClips)
    {
        int remapped = 0;

        foreach (var childState in sm.states)
        {
            var state = childState.state;
            if (state.motion is AnimationClip oldClip)
            {
                if (oldClip != null && newClips.TryGetValue(oldClip.name, out var newClip))
                {
                    state.motion = newClip;
                    remapped++;
                }
            }
            else if (state.motion is BlendTree bt)
            {
                remapped += RemapBlendTree(bt, newClips);
            }
        }

        foreach (var subSm in sm.stateMachines)
            remapped += RemapStateMachine(subSm.stateMachine, newClips);

        return remapped;
    }

    static int RemapBlendTree(BlendTree bt, Dictionary<string, AnimationClip> newClips)
    {
        int remapped = 0;
        var children = bt.children;
        for (int i = 0; i < children.Length; i++)
        {
            var child = children[i];
            if (child.motion is AnimationClip oldClip)
            {
                if (oldClip != null && newClips.TryGetValue(oldClip.name, out var newClip))
                {
                    child.motion = newClip;
                    children[i] = child;
                    remapped++;
                }
            }
            else if (child.motion is BlendTree subBt)
            {
                remapped += RemapBlendTree(subBt, newClips);
            }
        }
        bt.children = children;
        return remapped;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string folderName = Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folderName);
    }

    static string NaturalSortKey(string name)
    {
        var result = new System.Text.StringBuilder();
        int i = 0;
        while (i < name.Length)
        {
            if (char.IsDigit(name[i]))
            {
                int start = i;
                while (i < name.Length && char.IsDigit(name[i])) i++;
                result.Append(name.Substring(start, i - start).PadLeft(6, '0'));
            }
            else
            {
                result.Append(name[i]);
                i++;
            }
        }
        return result.ToString();
    }
}
#endif
