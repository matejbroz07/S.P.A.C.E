using UnityEngine;
using TMPro;

public class DrillController : MonoBehaviour
{
    [Header("Cameras")]
    public Camera mainCam;

    [Header("References")]
    public Transform laserStart;
    public LineRenderer line;
    public ParticleSystem particles;
    public ParticleSystem impactParticles;
    
    [Header("Rotation Settings")]
    public Transform drillVisual;
    public float idleRotationSpeed = 100f;
    public float firingRotationSpeed = 800f;
    private float currentRotationSpeed;

    [Header("UI")]
    public TMP_Text oreText;
    public TMP_Text oreLeftText;

    [Header("Settings")]
    public float maxDistance = 100f;
    public float sphereRadius = 0.2f;

    [Header("Layers")]
    public LayerMask depositMask;

    private DepositScript currentDeposit;

    void Start()
    {
        line.enabled = false;
        if (particles != null) particles.Stop();
        UpdateUI(null);
        currentRotationSpeed = idleRotationSpeed;
    }

    void Update()
    {
        CheckDepositUnderCrosshair();
        HandleRotation();

        if (Input.GetMouseButton(0) && currentDeposit != null && !InventoryHandler.Instance.IsInventoryOpen)
        {
            FireLaser();
            currentRotationSpeed = Mathf.Lerp(currentRotationSpeed, firingRotationSpeed, Time.deltaTime * 5f);
        }
        else
        {
            line.enabled = false;
            currentRotationSpeed = Mathf.Lerp(currentRotationSpeed, idleRotationSpeed, Time.deltaTime * 2f);
            
            if (particles != null) particles.Stop();
            if (impactParticles != null) 
            {
                impactParticles.Stop();
                impactParticles.Clear();
            }
        }
    }

    void HandleRotation()
    {
        if (drillVisual != null)
        {
            drillVisual.Rotate(Vector3.right * currentRotationSpeed * Time.deltaTime);
        }
    }

    void CheckDepositUnderCrosshair()
    {
        Ray ray = new Ray(mainCam.transform.position, mainCam.transform.forward);

        if (Physics.SphereCast(ray, sphereRadius, out RaycastHit hit, maxDistance, depositMask))
        {
            if (hit.collider.TryGetComponent(out DepositScript deposit))
            {
                currentDeposit = deposit;
                UpdateUI(deposit);
                return;
            }
        }

        currentDeposit = null;
        UpdateUI(null);
    }

    void FireLaser()
    {
        line.enabled = true;
        line.SetPosition(0, laserStart.position);

        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)); 
        Vector3 targetPoint;

        if (Physics.SphereCast(ray, sphereRadius, out RaycastHit hit, maxDistance, depositMask))
        {
            targetPoint = hit.point;

            if (hit.collider.TryGetComponent(out DepositScript deposit))
            {
                deposit.MineDamage(10f * Time.deltaTime);
                UpdateUI(deposit);
            }

            if (impactParticles != null)
            {
                impactParticles.transform.position = hit.point;
                Vector3 directionToPlayer = mainCam.transform.position - hit.point;
                impactParticles.transform.forward = directionToPlayer.normalized;
                if (!impactParticles.isPlaying) impactParticles.Play();
            }
        }
        else
        {
            targetPoint = ray.origin + ray.direction * maxDistance;
            if (impactParticles != null)
            {
                impactParticles.Stop();
                impactParticles.Clear();
            }
        }

        line.SetPosition(1, targetPoint);

        if (particles != null)
        {
            particles.transform.position = (laserStart.position + targetPoint) / 2f;
            particles.transform.LookAt(targetPoint);

            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            float actualDist = Vector3.Distance(laserStart.position, targetPoint);
            shape.scale = new Vector3(0.1f, 0.1f, actualDist);

            var main = particles.main;
            main.startSpeed = -actualDist / main.startLifetime.constant;

            if (!particles.isPlaying) particles.Play();
        }
    }

    void UpdateUI(DepositScript deposit)
    {
        if (deposit == null)
        {
            oreText.text = "Ore : None";
            oreLeftText.text = "Ore left : 0";
        }
        else
        {
            oreText.text = $"Ore : {deposit.itemName}";
            oreLeftText.text = $"Ore left : {deposit.GetOreLeft()}";
        }
    }
}