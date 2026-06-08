using UnityEngine;

public class WeaponHolsterHandler : MonoBehaviour
{
    public static WeaponHolsterHandler Instance;

    [Header("Rychlost pohybu")]
    [Tooltip("Doba v sekundách, za kterou se animace dokončí (např. 0.3 = třetina vteřiny).")]
    public float transitionDuration = 0.4f;

    [Header("Nastavení schované pozice")]
    public Vector3 posOffset = new Vector3(0.1f, -1.2f, -0.2f);
    public Vector3 rotOffset = new Vector3(45f, -30f, 10f);

    [Header("UI k vypnutí při schování")]
    public GameObject[] uiElementsToHide; 

    private Vector3 originalPos;
    private Quaternion originalRot;

    private Vector3 holsteredPos;
    private Quaternion holsteredRot;

    // Proměnné pro animaci
    private Vector3 startPos;
    private Quaternion startRot;
    private Vector3 targetPos;
    private Quaternion targetRot;
    
    private float transitionProgress = 1f; // 1 znamená, že animace stojí (je hotová)
    private bool isHolstered = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        originalPos = transform.localPosition;
        originalRot = transform.localRotation;

        // Předpočítáme si schovanou pozici, abychom to nedělali pořád dokola
        holsteredPos = originalPos + posOffset;
        holsteredRot = originalRot * Quaternion.Euler(rotOffset);

        targetPos = originalPos;
        targetRot = originalRot;
        startPos = originalPos;
        startRot = originalRot;
    }

    void Update()
    {
        // Pokud animace ještě nedojela do konce (1.0)
        if (transitionProgress < 1f)
        {
            // Přičítáme čas. Pokud je duration 0.4s, za 0.4s to dojede na 1.0.
            transitionProgress += Time.deltaTime / transitionDuration;
            
            // Zastropujeme na 1
            if (transitionProgress > 1f) transitionProgress = 1f;

            // Vytvoříme hladkou křivku (pomalý rozjezd, rychlý střed, pomalý dojezd)
            float smoothT = Mathf.SmoothStep(0f, 1f, transitionProgress);

            transform.localPosition = Vector3.Lerp(startPos, targetPos, smoothT);
            transform.localRotation = Quaternion.Slerp(startRot, targetRot, smoothT);

            // Pokud animace PRÁVĚ teď dojela do konce A zbraň je vytažená
            if (transitionProgress == 1f && !isHolstered)
            {
                SetUIActive(true); // Zapneme texty
            }
        }
    }

    public void SetHolsterState(bool state)
    {
        // Pojistka, abychom animaci nespouštěli znovu, když už v tom stavu jsme
        if (isHolstered == state) return; 

        isHolstered = state;
        transitionProgress = 0f; // Vynulujeme časovač (spustíme animaci)

        // Od teď animujeme z aktuální pozice (umožňuje plynule otočit směr i v půlce animace)
        startPos = transform.localPosition;
        startRot = transform.localRotation;

        if (isHolstered)
        {
            targetPos = holsteredPos;
            targetRot = holsteredRot;
            
            // Okamžitě vypneme texty při začátku schovávání
            SetUIActive(false); 
        }
        else
        {
            targetPos = originalPos;
            targetRot = originalRot;
            
            // ZDE TEXTY NEZAPÍNÁME. Čekáme, až Update() dojede animaci do konce.
        }
    }

    private void SetUIActive(bool isActive)
    {
        if (uiElementsToHide != null)
        {
            foreach (GameObject uiElement in uiElementsToHide)
            {
                if (uiElement != null) uiElement.SetActive(isActive);
            }
        }
    }
}