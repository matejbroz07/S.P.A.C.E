using UnityEngine;
using TMPro;
using System.Collections;

public class NotificationItem : MonoBehaviour
{
    public TMP_Text tmpText;
    public float lifetime = 3f;
    public float fadeDuration = 1f;

    // Data pro "stackování"
    [HideInInspector] public string itemName;
    [HideInInspector] public int currentQuantity;
    
    private Coroutine fadeCoroutine; // Uložíme si běžící odpočet

    public void Initialize(string name, int quantity, Color color)
    {
        itemName = name;
        currentQuantity = quantity;
        tmpText.color = color;
        UpdateText();

        // Spustíme odpočet zmizení
        fadeCoroutine = StartCoroutine(FadeAndDestroy());
    }

    // Tuto metodu zavolá Manager, když chce jen zvednout číslo
    public void AddAmount(int amount)
    {
        currentQuantity += amount;
        UpdateText();

        // RESET ČASOVAČE: Zastavíme staré mizení a spustíme nové
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        
        // Vrátíme textu plnou viditelnost (alpha 1)
        Color c = tmpText.color;
        tmpText.color = new Color(c.r, c.g, c.b, 1f);
        
        fadeCoroutine = StartCoroutine(FadeAndDestroy());
    }

    private void UpdateText()
    {
        // Pokud je množství větší než 0, vypíšeme "+1 Item"
        if (currentQuantity > 0)
        {
            tmpText.text = $"+ {currentQuantity} {itemName}";
        }
        // Pokud je množství 0 (nebo méně), vypíšeme jen TEXT bez čísla
        else
        {
            tmpText.text = itemName;
        }
    }

    private IEnumerator FadeAndDestroy()
    {
        yield return new WaitForSeconds(lifetime - fadeDuration);

        float timer = 0f;
        Color startColor = tmpText.color;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            tmpText.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        Destroy(gameObject);
    }
}