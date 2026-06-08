using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ColorPicker : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [Header("UI Reference")]
    public RawImage paletteImage;
    public RectTransform cursor;

    [Header("Výsledek")]
    public Color selectedColor = Color.white;

    public System.Action<Color> OnColorChanged;

    private void PickColor(PointerEventData eventData)
    {
        RectTransform rectTransform = paletteImage.rectTransform;
        Vector2 localPoint;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out localPoint))
        {
            float xPercent = (localPoint.x / rectTransform.rect.width) + 0.5f;
            float yPercent = (localPoint.y / rectTransform.rect.height) + 0.5f;

            xPercent = Mathf.Clamp01(xPercent);
            yPercent = Mathf.Clamp01(yPercent);

            cursor.localPosition = new Vector2(
                (xPercent - 0.5f) * rectTransform.rect.width,
                (yPercent - 0.5f) * rectTransform.rect.height
            );

            Texture2D texture = (Texture2D)paletteImage.texture;
            int texX = Mathf.FloorToInt(xPercent * texture.width);
            int texY = Mathf.FloorToInt(yPercent * texture.height);

            selectedColor = texture.GetPixel(texX, texY);

            OnColorChanged?.Invoke(selectedColor);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        PickColor(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        PickColor(eventData);
    }
}

