using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BattleSystem.UI
{
    public class BattleUIManager : MonoBehaviour
    {
        [Header("Painéis principais")]
        public GameObject lobbyPanel;
        public GameObject battlePanel;
        public GameObject teamSelectionPanel;
        public GameObject battleResultPanel;
        public GameObject skillSelectionPanel;
        
        [Header("Animações e transições")]
        public float transitionSpeed = 0.3f;
        public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        // Estado atual e anterior (para voltar)
        private GameObject currentPanel;
        private Stack<GameObject> panelHistory = new Stack<GameObject>();
        
        // Singleton para acesso fácil
        public static BattleUIManager Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            
            // Inicialmente, esconder todos os painéis
            HideAllPanels();
            
            // Definir o painel inicial
            currentPanel = lobbyPanel;
            ShowPanel(currentPanel);
        }
        
        public void NavigateTo(GameObject targetPanel)
        {
            if (currentPanel == targetPanel) return;
            
            // Guardar histórico para navegação de volta
            panelHistory.Push(currentPanel);
            
            // Fazer a transição
            StartCoroutine(TransitionPanels(currentPanel, targetPanel));
            
            // Atualizar painel atual
            currentPanel = targetPanel;
        }
        
        public void NavigateBack()
        {
            if (panelHistory.Count == 0) return;
            
            GameObject previousPanel = panelHistory.Pop();
            StartCoroutine(TransitionPanels(currentPanel, previousPanel));
            
            currentPanel = previousPanel;
        }
        
        private IEnumerator TransitionPanels(GameObject fromPanel, GameObject toPanel)
        {
            // Configurar os painéis para a transição
            if (fromPanel != null)
            {
                CanvasGroup fromCanvasGroup = GetOrAddCanvasGroup(fromPanel);
                fromCanvasGroup.alpha = 1;
                fromCanvasGroup.interactable = false;
                fromCanvasGroup.blocksRaycasts = false;
            }
            
            // Mostrar o painel de destino
            toPanel.SetActive(true);
            CanvasGroup toCanvasGroup = GetOrAddCanvasGroup(toPanel);
            toCanvasGroup.alpha = 0;
            toCanvasGroup.interactable = false;
            toCanvasGroup.blocksRaycasts = false;
            
            // Executar transição
            float timeElapsed = 0;
            
            while (timeElapsed < transitionSpeed)
            {
                timeElapsed += Time.deltaTime;
                float normalizedTime = timeElapsed / transitionSpeed;
                float curveValue = transitionCurve.Evaluate(normalizedTime);
                
                if (fromPanel != null)
                {
                    CanvasGroup fromCG = fromPanel.GetComponent<CanvasGroup>();
                    fromCG.alpha = 1 - curveValue;
                }
                
                toCanvasGroup.alpha = curveValue;
                
                yield return null;
            }
            
            // Finalizar transição
            if (fromPanel != null)
            {
                fromPanel.SetActive(false);
            }
            
            toCanvasGroup.alpha = 1;
            toCanvasGroup.interactable = true;
            toCanvasGroup.blocksRaycasts = true;
            
            // Disparar evento de transição concluída
            OnTransitionComplete(toPanel);
        }
        
        private CanvasGroup GetOrAddCanvasGroup(GameObject panel)
        {
            var canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = panel.AddComponent<CanvasGroup>();
            }
            return canvasGroup;
        }
        
        private void HideAllPanels()
        {
            if (lobbyPanel) lobbyPanel.SetActive(false);
            if (battlePanel) battlePanel.SetActive(false);
            if (teamSelectionPanel) teamSelectionPanel.SetActive(false);
            if (battleResultPanel) battleResultPanel.SetActive(false);
            if (skillSelectionPanel) skillSelectionPanel.SetActive(false);
        }
        
        private void ShowPanel(GameObject panel)
        {
            panel.SetActive(true);
            CanvasGroup canvasGroup = GetOrAddCanvasGroup(panel);
            canvasGroup.alpha = 1;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        
        // Método chamado após a conclusão da transição para executar lógica específica
        private void OnTransitionComplete(GameObject activePanel)
        {
            // Lógica específica para cada painel (como atualizar dados ou estado)
            if (activePanel == battlePanel)
            {
                // Atualizar estado da batalha
                BattleTestClient client = FindObjectOfType<BattleTestClient>();
                if (client != null)
                {
                    client.GetBattleState();
                }
            }
            else if (activePanel == skillSelectionPanel)
            {
                // Atualizar lista de habilidades disponíveis
                BattleTestClient client = FindObjectOfType<BattleTestClient>();
                if (client != null)
                {
                    client.UpdateSkillsDropdown();
                }
            }
        }
    }
}