#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

namespace BattleSystem.Editor
{
    [CustomEditor(typeof(BattleTestClient))]
    public class BattleTestClientEditor : EditorWindow
    {
        [MenuItem("BattleSystem/Create Test UI")]
        public static void CreateTestUI()
        {
            // Verificar se já existe um Canvas na cena
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                // Criar novo Canvas
                GameObject canvasObject = new GameObject("Battle Test Canvas");
                canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                // Adicionar componentes necessários ao Canvas
                canvasObject.AddComponent<CanvasScaler>();
                canvasObject.AddComponent<GraphicRaycaster>();

                // Verificar se existe EventSystem
                if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
                {
                    GameObject eventSystemObj = new GameObject("EventSystem");
                    eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                    eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                }
            }

            // Criar o painel principal
            GameObject panelObj = new GameObject("Battle Test Panel");
            panelObj.transform.SetParent(canvas.transform, false);

            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.1f, 0.1f);
            panelRect.anchorMax = new Vector2(0.9f, 0.9f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image panelImage = panelObj.AddComponent<Image>();
            panelImage.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

            // Adicionar o script do cliente de teste
            BattleTestClient client = panelObj.AddComponent<BattleTestClient>();

            // Criar as seções da UI e configurar as referências via SerializedObject
            SerializedObject serializedClient = new SerializedObject(client);

            CreateConnectionSection(panelObj.transform, serializedClient);
            CreateBattleCreationSection(panelObj.transform, serializedClient);
            CreateBattleControlSection(panelObj.transform, serializedClient);
            CreateLogSection(panelObj.transform, serializedClient);

            // Aplicar as mudanças ao objeto serializado
            serializedClient.ApplyModifiedProperties();

            Debug.Log("UI de teste de batalha criada com sucesso!");
        }

        private static void CreateConnectionSection(Transform parent, SerializedObject client)
        {
            GameObject sectionObj = CreateSection(parent, "Connection Section", new Vector2(0, 0.8f), new Vector2(1, 1));

            // Status da conexão
            GameObject statusTextObj = CreateTextObject(sectionObj.transform, "Connection Status", "Status: Desconectado", new Vector2(0.5f, 0.7f));
            AssignToSerializedProperty(client, "connectionStatusText", statusTextObj.GetComponent<TextMeshProUGUI>());

            // Botão conectar
            GameObject connectButtonObj = CreateButton(sectionObj.transform, "Connect Button", "Conectar", new Vector2(0.3f, 0.3f));
            AssignToSerializedProperty(client, "connectButton", connectButtonObj.GetComponent<Button>());

            // Botão desconectar
            GameObject disconnectButtonObj = CreateButton(sectionObj.transform, "Disconnect Button", "Desconectar", new Vector2(0.7f, 0.3f));
            AssignToSerializedProperty(client, "disconnectButton", disconnectButtonObj.GetComponent<Button>());
        }

        private static void CreateBattleCreationSection(Transform parent, SerializedObject client)
        {
            GameObject sectionObj = CreateSection(parent, "Battle Creation Section", new Vector2(0, 0.6f), new Vector2(1, 0.8f));

            // Botão criar batalha
            GameObject createBattleButtonObj = CreateButton(sectionObj.transform, "Create Battle Button", "Criar Batalha", new Vector2(0.2f, 0.7f));
            AssignToSerializedProperty(client, "createBattleButton", createBattleButtonObj.GetComponent<Button>());

            // Campo de texto para o ID da batalha
            GameObject battleIdInputObj = CreateInputField(sectionObj.transform, "Battle ID Input", "ID da Batalha", new Vector2(0.5f, 0.7f));
            AssignToSerializedProperty(client, "battleIdInputField", battleIdInputObj.GetComponent<TMP_InputField>());

            // Campo de texto para o nome do jogador
            GameObject playerNameInputObj = CreateInputField(sectionObj.transform, "Player Name Input", "Nome do Jogador", new Vector2(0.5f, 0.4f));
            AssignToSerializedProperty(client, "playerNameInputField", playerNameInputObj.GetComponent<TMP_InputField>());

            // Dropdown para a classe do personagem
            GameObject classDropdownObj = CreateDropdown(sectionObj.transform, "Class Dropdown", new Vector2(0.2f, 0.4f));
            TMP_Dropdown classDropdown = classDropdownObj.GetComponent<TMP_Dropdown>();
            classDropdown.options.Clear();
            classDropdown.options.Add(new TMP_Dropdown.OptionData("Warrior"));
            classDropdown.options.Add(new TMP_Dropdown.OptionData("Mage"));
            classDropdown.options.Add(new TMP_Dropdown.OptionData("Archer"));
            classDropdown.options.Add(new TMP_Dropdown.OptionData("Rogue"));
            classDropdown.value = 0;
            classDropdown.RefreshShownValue();
            AssignToSerializedProperty(client, "classDropdown", classDropdown);

            // Botão entrar na batalha
            GameObject joinBattleButtonObj = CreateButton(sectionObj.transform, "Join Battle Button", "Entrar na Batalha", new Vector2(0.8f, 0.4f));
            AssignToSerializedProperty(client, "joinBattleButton", joinBattleButtonObj.GetComponent<Button>());

            // Texto para mostrar o ID do jogador
            GameObject playerIdTextObj = CreateTextObject(sectionObj.transform, "Player ID Text", "Player ID: N/A", new Vector2(0.8f, 0.7f));
            AssignToSerializedProperty(client, "playerIdText", playerIdTextObj.GetComponent<TextMeshProUGUI>());
        }

        private static void CreateBattleControlSection(Transform parent, SerializedObject client)
        {
            GameObject sectionObj = CreateSection(parent, "Battle Control Section", new Vector2(0, 0.3f), new Vector2(1, 0.6f));

            // Dropdown para seleção de inimigos
            GameObject enemyDropdownObj = CreateDropdown(sectionObj.transform, "Enemy Dropdown", new Vector2(0.2f, 0.8f));
            TMP_Dropdown enemyDropdown = enemyDropdownObj.GetComponent<TMP_Dropdown>();
            enemyDropdown.options.Clear();
            enemyDropdown.options.Add(new TMP_Dropdown.OptionData("Goblin"));
            enemyDropdown.options.Add(new TMP_Dropdown.OptionData("Orc"));
            enemyDropdown.options.Add(new TMP_Dropdown.OptionData("Dragon"));
            enemyDropdown.value = 0;
            enemyDropdown.RefreshShownValue();
            AssignToSerializedProperty(client, "enemySelectionDropdown", enemyDropdown);

            // Botão adicionar inimigo
            GameObject addEnemyButtonObj = CreateButton(sectionObj.transform, "Add Enemy Button", "Adicionar", new Vector2(0.4f, 0.8f));
            AssignToSerializedProperty(client, "addEnemyButton", addEnemyButtonObj.GetComponent<Button>());

            // Texto para mostrar inimigos selecionados
            GameObject enemiesTextObj = CreateTextObject(sectionObj.transform, "Selected Enemies Text", "Inimigos selecionados:", new Vector2(0.7f, 0.8f));
            AssignToSerializedProperty(client, "selectedEnemiesText", enemiesTextObj.GetComponent<TextMeshProUGUI>());

            // Botão iniciar batalha
            GameObject startBattleButtonObj = CreateButton(sectionObj.transform, "Start Battle Button", "Iniciar Batalha", new Vector2(0.2f, 0.6f));
            AssignToSerializedProperty(client, "startBattleButton", startBattleButtonObj.GetComponent<Button>());

            // Botões de ação
            GameObject attackButtonObj = CreateButton(sectionObj.transform, "Attack Button", "Atacar", new Vector2(0.2f, 0.4f));
            AssignToSerializedProperty(client, "attackButton", attackButtonObj.GetComponent<Button>());

            GameObject skillButtonObj = CreateButton(sectionObj.transform, "Skill Button", "Usar Habilidade", new Vector2(0.5f, 0.4f));
            AssignToSerializedProperty(client, "skillButton", skillButtonObj.GetComponent<Button>());

            GameObject passButtonObj = CreateButton(sectionObj.transform, "Pass Button", "Passar", new Vector2(0.8f, 0.4f));
            AssignToSerializedProperty(client, "passButton", passButtonObj.GetComponent<Button>());

            // Dropdowns para alvo e habilidade
            GameObject targetDropdownObj = CreateDropdown(sectionObj.transform, "Target Dropdown", new Vector2(0.2f, 0.2f));
            AssignToSerializedProperty(client, "targetSelectionDropdown", targetDropdownObj.GetComponent<TMP_Dropdown>());

            GameObject skillDropdownObj = CreateDropdown(sectionObj.transform, "Skill Dropdown", new Vector2(0.5f, 0.2f));
            AssignToSerializedProperty(client, "skillSelectionDropdown", skillDropdownObj.GetComponent<TMP_Dropdown>());

            // Botão atualizar estado
            GameObject refreshButtonObj = CreateButton(sectionObj.transform, "Refresh Button", "Atualizar Estado", new Vector2(0.8f, 0.6f));
            AssignToSerializedProperty(client, "refreshStateButton", refreshButtonObj.GetComponent<Button>());
        }

        private static void CreateLogSection(Transform parent, SerializedObject client)
        {
            GameObject sectionObj = CreateSection(parent, "Log Section", new Vector2(0, 0), new Vector2(1, 0.3f));

            // Texto para mostrar o estado da batalha
            GameObject battleStateTextObj = CreateTextObject(sectionObj.transform, "Battle State Text", "Estado da Batalha: N/A", new Vector2(0.3f, 0.8f));
            AssignToSerializedProperty(client, "battleStateText", battleStateTextObj.GetComponent<TextMeshProUGUI>());

            // Texto para mostrar o turno atual
            GameObject currentTurnTextObj = CreateTextObject(sectionObj.transform, "Current Turn Text", "Turno: N/A", new Vector2(0.7f, 0.8f));
            AssignToSerializedProperty(client, "currentTurnText", currentTurnTextObj.GetComponent<TextMeshProUGUI>());

            // Área de rolagem para logs
            GameObject scrollViewObj = new GameObject("Log Scroll View");
            scrollViewObj.transform.SetParent(sectionObj.transform, false);

            RectTransform scrollRect = scrollViewObj.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.1f, 0.1f);
            scrollRect.anchorMax = new Vector2(0.9f, 0.7f);
            scrollRect.offsetMin = Vector2.zero;
            scrollRect.offsetMax = Vector2.zero;

            Image scrollBg = scrollViewObj.AddComponent<Image>();
            scrollBg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

            ScrollRect scroll = scrollViewObj.AddComponent<ScrollRect>();

            // Viewport
            GameObject viewportObj = new GameObject("Viewport");
            viewportObj.transform.SetParent(scrollViewObj.transform, false);

            RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            Mask mask = viewportObj.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            // Conteúdo
            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(viewportObj.transform, false);

            RectTransform contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.offsetMin = new Vector2(0, -500);
            contentRect.offsetMax = Vector2.zero;

            // Texto de log
            GameObject logTextObj = new GameObject("Log Text");
            logTextObj.transform.SetParent(contentObj.transform, false);

            RectTransform logTextRect = logTextObj.AddComponent<RectTransform>();
            logTextRect.anchorMin = Vector2.zero;
            logTextRect.anchorMax = Vector2.one;
            logTextRect.offsetMin = Vector2.zero;
            logTextRect.offsetMax = Vector2.zero;

            TextMeshProUGUI logTextComponent = logTextObj.AddComponent<TextMeshProUGUI>();
            logTextComponent.fontSize = 14;
            logTextComponent.color = Color.white;
            logTextComponent.alignment = TextAlignmentOptions.TopLeft;
            logTextComponent.text = "Logs aparecerão aqui...";
            AssignToSerializedProperty(client, "logText", logTextComponent);

            // Configurar scroll
            scroll.viewport = viewportRect;
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.verticalScrollbar = null;
            scroll.horizontalScrollbar = null;
        }

        // Método para atribuir componentes a propriedades serializadas
        private static void AssignToSerializedProperty(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
            else
            {
                Debug.LogWarning($"Propriedade '{propertyName}' não encontrada no SerializedObject");
            }
        }

        // Métodos auxiliares para criar elementos da UI
        private static GameObject CreateSection(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject sectionObj = new GameObject(name);
            sectionObj.transform.SetParent(parent, false);
            
            RectTransform rect = sectionObj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            return sectionObj;
        }

        private static GameObject CreateTextObject(Transform parent, string name, string text, Vector2 anchorPosition)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);
            
            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = anchorPosition;
            rect.anchorMax = anchorPosition;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(200, 30);
            
            TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = 14;
            textComponent.color = Color.white;
            
            return textObj;
        }

        private static GameObject CreateButton(Transform parent, string name, string text, Vector2 anchorPosition)
        {
            GameObject buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(parent, false);
            
            RectTransform rect = buttonObj.AddComponent<RectTransform>();
            rect.anchorMin = anchorPosition - new Vector2(0.1f, 0.05f);
            rect.anchorMax = anchorPosition + new Vector2(0.1f, 0.05f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = buttonObj.AddComponent<Image>();
            image.color = new Color(0.3f, 0.3f, 0.3f);

            Button button = buttonObj.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.3f, 0.3f, 0.3f);
            colors.highlightedColor = new Color(0.4f, 0.4f, 0.4f);
            colors.pressedColor = new Color(0.2f, 0.2f, 0.2f);
            button.colors = colors;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = 14;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.Center;

            return buttonObj;
        }

        private static GameObject CreateInputField(Transform parent, string name, string placeholder, Vector2 anchorPosition)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorPosition - new Vector2(0.15f, 0.05f);
            rect.anchorMax = anchorPosition + new Vector2(0.15f, 0.05f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = obj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f);

            TMP_InputField inputField = obj.AddComponent<TMP_InputField>();

            // Texto de entrada
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(obj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(5, 0);
            textRect.offsetMax = new Vector2(-5, 0);

            TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.fontSize = 14;
            textComponent.color = Color.white;

            // Placeholder
            GameObject placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(obj.transform, false);

            RectTransform placeholderRect = placeholderObj.AddComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = new Vector2(5, 0);
            placeholderRect.offsetMax = new Vector2(-5, 0);

            TextMeshProUGUI placeholderComponent = placeholderObj.AddComponent<TextMeshProUGUI>();
            placeholderComponent.text = placeholder;
            placeholderComponent.fontSize = 14;
            placeholderComponent.color = new Color(0.5f, 0.5f, 0.5f);

            // Configurar input field
            inputField.textComponent = textComponent;
            inputField.placeholder = placeholderComponent;

            return obj;
        }

        private static GameObject CreateDropdown(Transform parent, string name, Vector2 anchorPosition)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorPosition - new Vector2(0.1f, 0.05f);
            rect.anchorMax = anchorPosition + new Vector2(0.1f, 0.05f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = obj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f);

            TMP_Dropdown dropdown = obj.AddComponent<TMP_Dropdown>();

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(obj.transform, false);

            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(5, 0);
            labelRect.offsetMax = new Vector2(-30, 0);

            TextMeshProUGUI labelComponent = labelObj.AddComponent<TextMeshProUGUI>();
            labelComponent.fontSize = 14;
            labelComponent.color = Color.white;
            labelComponent.alignment = TextAlignmentOptions.MidlineLeft;

            // Arrow
            GameObject arrowObj = new GameObject("Arrow");
            arrowObj.transform.SetParent(obj.transform, false);

            RectTransform arrowRect = arrowObj.AddComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(1, 0.5f);
            arrowRect.anchorMax = new Vector2(1, 0.5f);
            arrowRect.sizeDelta = new Vector2(20, 20);
            arrowRect.anchoredPosition = new Vector2(-15, 0);

            Image arrowImage = arrowObj.AddComponent<Image>();
            arrowImage.color = Color.white;

            // Template
            GameObject templateObj = new GameObject("Template");
            templateObj.transform.SetParent(obj.transform, false);
            templateObj.SetActive(false);

            RectTransform templateRect = templateObj.AddComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0, 0);
            templateRect.anchorMax = new Vector2(1, 0);
            templateRect.pivot = new Vector2(0.5f, 1);
            templateRect.sizeDelta = new Vector2(0, 150);
            templateRect.anchoredPosition = new Vector2(0, 2);

            Image templateImage = templateObj.AddComponent<Image>();
            templateImage.color = new Color(0.2f, 0.2f, 0.2f);

            ScrollRect scrollRect = templateObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            // Viewport
            GameObject viewportObj = new GameObject("Viewport");
            viewportObj.transform.SetParent(templateObj.transform, false);

            RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            Mask mask = viewportObj.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            // Content
            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(viewportObj.transform, false);

            RectTransform contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 28);

            // Item
            GameObject itemObj = new GameObject("Item");
            itemObj.transform.SetParent(contentObj.transform, false);

            RectTransform itemRect = itemObj.AddComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0, 0);
            itemRect.anchorMax = new Vector2(1, 1);
            itemRect.offsetMin = Vector2.zero;
            itemRect.offsetMax = Vector2.zero;

            Toggle toggle = itemObj.AddComponent<Toggle>();
            toggle.targetGraphic = itemObj.AddComponent<Image>();
            toggle.isOn = true;

            // Item Label
            GameObject itemLabelObj = new GameObject("Item Label");
            itemLabelObj.transform.SetParent(itemObj.transform, false);

            RectTransform itemLabelRect = itemLabelObj.AddComponent<RectTransform>();
            itemLabelRect.anchorMin = Vector2.zero;
            itemLabelRect.anchorMax = Vector2.one;
            itemLabelRect.offsetMin = new Vector2(5, 0);
            itemLabelRect.offsetMax = new Vector2(-5, 0);

            TextMeshProUGUI itemLabelComponent = itemLabelObj.AddComponent<TextMeshProUGUI>();
            itemLabelComponent.fontSize = 14;
            itemLabelComponent.color = Color.white;
            itemLabelComponent.alignment = TextAlignmentOptions.MidlineLeft;

            // Configure dropdown
            dropdown.template = templateRect;
            dropdown.captionText = labelComponent;
            dropdown.itemText = itemLabelComponent;

            // Configure ScrollRect
            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;

            // Add default option
            dropdown.options.Add(new TMP_Dropdown.OptionData("Selecione..."));
            dropdown.value = 0;
            dropdown.RefreshShownValue();

            return obj;
        }
    }
}
#endif