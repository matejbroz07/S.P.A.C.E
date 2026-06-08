using System.Collections;
using UnityEngine;
using TMPro;

public class VictoryManager : MonoBehaviour
{
    public static VictoryManager Instance;

    [Header("Kamery")]
    public GameObject player; 
    public GameObject mainCamera; 
    public GameObject cutsceneCamera; 
    
    [Header("Animace a UI")]
    public Animator cameraAnimator;
    public GameObject victoryCanvas;
    public CanvasGroup victoryUIFade;
    
    [Header("Načasování")]
    public float timeBeforeUI = 3.5f; 
    public float uiFadeSpeed = 1f;
    [Tooltip("Jak dlouho nápis svítí, než se hra nadobro vypne")]
    public float timeBeforeQuit = 4f; // <--- NOVÉ

    private void Awake()
    {
        Instance = this;
    }

    public void PlayVictorySequence()
    {
        StartCoroutine(CutsceneRoutine());
    }

    private IEnumerator CutsceneRoutine()
    {
        // 1. Schováme inventář
        if (InventoryHandler.Instance != null && InventoryHandler.Instance.IsInventoryOpen)
        {
            InventoryHandler.Instance.ToggleInventory();
        }

        // 2. Vypneme ostatní UI
        Canvas[] allCanvases = FindObjectsOfType<Canvas>();
        foreach (Canvas canvas in allCanvases)
        {
            if (canvas.gameObject != victoryCanvas)
            {
                canvas.gameObject.SetActive(false);
            }
        }

        // 3. Zmrazíme hráče a přepneme kamery
        player.SetActive(false); 
        mainCamera.SetActive(false);
        cutsceneCamera.SetActive(true);

        // 4. Spustíme let kamery
        cameraAnimator.Play("Victory");

        // 5. Čekáme na správný moment pro text
        yield return new WaitForSeconds(timeBeforeUI);

        // 6. Zobrazíme text
        victoryCanvas.SetActive(true);
        victoryUIFade.alpha = 0f;

        while (victoryUIFade.alpha < 1f)
        {
            victoryUIFade.alpha += Time.deltaTime * uiFadeSpeed;
            yield return null;
        }

        Debug.Log("<color=green>[Victory] Text zobrazen, čekám na vypnutí hry...</color>");

        // --- 7. NOVÉ: ČEKÁNÍ A VYPNUTÍ HRY ---
        // Necháme text chvíli svítit, aby si ho hráč mohl vychutnat
        yield return new WaitForSeconds(timeBeforeQuit);

        Debug.Log("<color=red>[Victory] Vypínám hru!</color>");

        // Vypnutí hry (funguje v Editoru i v hotové hře)
        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #else
                Application.Quit();
        #endif
    }
}