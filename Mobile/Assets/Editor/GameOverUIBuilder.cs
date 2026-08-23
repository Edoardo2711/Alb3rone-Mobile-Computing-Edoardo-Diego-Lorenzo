using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

/// <summary>
/// Costruisce in scena il Canvas della schermata di game over e lo collega al player.
///
/// Perche' uno script di Editor invece di modificare la scena a mano: creare gli oggetti
/// tramite l'API di Unity e' l'unico modo sicuro con l'editor aperto (editare lo YAML
/// mentre Unity ha la scena in memoria fa sovrascrivere le modifiche al salvataggio), ed
/// e' ripetibile se la gerarchia va persa.
///
/// Usa UnityEngine.UI.Text e non TextMeshPro perche' le TMP Essential Resources non sono
/// importate nel progetto: il testo legacy usa il font builtin e non richiede alcun asset.
/// </summary>
public static class GameOverUIBuilder
{
    private const string CanvasName = "GameOverCanvas";

    [MenuItem("Tools/UI/Costruisci schermata Game Over")]
    public static void Build()
    {
        var existing = GameObject.Find(CanvasName);
        if (existing != null)
        {
            Selection.activeGameObject = existing;
            EditorUtility.DisplayDialog(
                "Game Over UI",
                $"'{CanvasName}' esiste gia' in scena.\n\nPer ricostruirlo, eliminalo e rilancia il comando.",
                "Ok");
            return;
        }

        EnsureEventSystem();

        // --- Canvas -------------------------------------------------------------
        var canvasGO = new GameObject(CanvasName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGO, "Crea Game Over UI");

        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        // 0.5: con l'autorotazione attiva l'UI scala in modo sensato sia in landscape
        // che in portrait, invece di seguire un solo asse.
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // --- Pannello a schermo intero -------------------------------------------
        var panel = CreateUIObject("Pannello", canvasGO.transform);
        Stretch(panel.rect);
        var panelImage = panel.go.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.78f);

        // --- Titolo ---------------------------------------------------------------
        var title = CreateUIObject("Titolo", panel.go.transform);
        Center(title.rect, new Vector2(0f, 150f), new Vector2(1400f, 260f));
        var titleText = title.go.AddComponent<Text>();
        titleText.text = "SEI MORTO";
        titleText.font = BuiltinFont();
        titleText.fontSize = 130;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(0.85f, 0.15f, 0.15f);
        titleText.horizontalOverflow = HorizontalWrapMode.Overflow;
        titleText.verticalOverflow = VerticalWrapMode.Overflow;

        // --- Pulsante Riprova -----------------------------------------------------
        var button = CreateUIObject("BottoneRiprova", panel.go.transform);
        Center(button.rect, new Vector2(0f, -110f), new Vector2(460f, 130f));
        var buttonImage = button.go.AddComponent<Image>();
        buttonImage.color = new Color(0.92f, 0.92f, 0.92f);
        var buttonComponent = button.go.AddComponent<Button>();
        buttonComponent.targetGraphic = buttonImage;

        var label = CreateUIObject("Testo", button.go.transform);
        Stretch(label.rect);
        var labelText = label.go.AddComponent<Text>();
        labelText.text = "RIPROVA";
        labelText.font = BuiltinFont();
        labelText.fontSize = 52;
        labelText.fontStyle = FontStyle.Bold;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.color = new Color(0.1f, 0.1f, 0.1f);
        labelText.horizontalOverflow = HorizontalWrapMode.Overflow;

        // --- Collegamenti ----------------------------------------------------------
        var health = Object.FindFirstObjectByType<PlayerHealth>();
        PlayerRespawn respawn = null;
        if (health != null)
        {
            respawn = health.GetComponent<PlayerRespawn>();
            if (respawn == null)
            {
                respawn = Undo.AddComponent<PlayerRespawn>(health.gameObject);
                Debug.Log($"[GameOverUIBuilder] Aggiunto PlayerRespawn a '{health.gameObject.name}'.");
            }
        }
        else
        {
            Debug.LogWarning("[GameOverUIBuilder] Nessun PlayerHealth in scena: i riferimenti del player vanno collegati a mano.");
        }

        var ui = canvasGO.AddComponent<GameOverUI>();
        var so = new SerializedObject(ui);
        so.FindProperty("panel").objectReferenceValue         = panel.go;
        so.FindProperty("retryButton").objectReferenceValue   = buttonComponent;
        so.FindProperty("playerHealth").objectReferenceValue  = health;
        so.FindProperty("playerRespawn").objectReferenceValue = respawn;
        so.ApplyModifiedPropertiesWithoutUndo();

        // Il pannello parte spento: GameOverUI.Awake lo rifarebbe comunque, ma cosi'
        // non copre la Game view mentre si lavora in editor.
        panel.go.SetActive(false);

        EditorSceneManager.MarkSceneDirty(canvasGO.scene);
        Selection.activeGameObject = canvasGO;

        Debug.Log("[GameOverUIBuilder] Schermata di game over creata. Ricordati di salvare la scena.");
    }

    // Crea un EventSystem se manca: senza, il pulsante non riceve ne' click ne' tocchi.
    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;

        var go = new GameObject("EventSystem", typeof(EventSystem));
        Undo.RegisterCreatedObjectUndo(go, "Crea EventSystem");

#if ENABLE_INPUT_SYSTEM
        // Il progetto usa il nuovo Input System (PlayerInput sul player): il modulo
        // corrispondente gestisce il tocco su mobile senza configurazione aggiuntiva.
        var module = go.AddComponent<InputSystemUIInputModule>();
        module.AssignDefaultActions();
#else
        go.AddComponent<StandaloneInputModule>();
#endif
        Debug.Log("[GameOverUIBuilder] Creato EventSystem (non ce n'era uno in scena).");
    }

    private struct UIObject
    {
        public GameObject go;
        public RectTransform rect;
    }

    private static UIObject CreateUIObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return new UIObject { go = go, rect = rect };
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void Center(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    // In Unity 2022+ il font builtin si chiama LegacyRuntime.ttf; Arial.ttf resta
    // come fallback per sicurezza.
    private static Font BuiltinFont()
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return font;
    }
}
