using UnityEngine;

public class Doors : MonoBehaviour
{
    [Header("Části dveří")]
    public Transform leftDoor;
    public Transform rightDoor;

    [Header("Nastavení pohybu")]
    public float openDistance = 1.5f; // O kolik se odsunou do stran
    public float speed = 3f;

    private Vector3 leftClosedPos;
    private Vector3 rightClosedPos;
    private Vector3 leftOpenPos;
    private Vector3 rightOpenPos;

    private bool isPlayerNear = false;

    void Start()
    {
        // Zapamatujeme si pozice
        leftClosedPos = leftDoor.localPosition;
        rightClosedPos = rightDoor.localPosition;

        // Vypočítáme otevřené pozice (odsouvání po ose X)
        leftOpenPos = leftClosedPos + new Vector3(-openDistance, 0, 0);
        rightOpenPos = rightClosedPos + new Vector3(openDistance, 0, 0);
    }

    void Update()
    {
        // Plynulý pohyb pomocí Lerp
        if (isPlayerNear)
        {
            leftDoor.localPosition = Vector3.Lerp(leftDoor.localPosition, leftOpenPos, Time.deltaTime * speed);
            rightDoor.localPosition = Vector3.Lerp(rightDoor.localPosition, rightOpenPos, Time.deltaTime * speed);
        }
        else
        {
            leftDoor.localPosition = Vector3.Lerp(leftDoor.localPosition, leftClosedPos, Time.deltaTime * speed);
            rightDoor.localPosition = Vector3.Lerp(rightDoor.localPosition, rightClosedPos, Time.deltaTime * speed);
        }
    }

    // Detekce hráče
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) isPlayerNear = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) isPlayerNear = false;
    }
}