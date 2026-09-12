using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class MobileCatController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float moveDelay = 0.15f;

    [Header("Tilt")]
    public float tiltAmount = 15f;
    public float tiltSpeed = 8f;

    private Rigidbody2D rb;
    private Camera cam;
    private Vector2 targetPosition;
    private Vector2 smoothVelocity;
    private bool isTouching;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        cam = Camera.main;
        rb.gravityScale = 0f;
        targetPosition = rb.position;
    }

    private void Update()
    {
        HandleTouchInput();
    }

    private void FixedUpdate()
    {
        if (isTouching)
        {
            Vector2 newPosition = Vector2.SmoothDamp(
                rb.position,
                targetPosition,
                ref smoothVelocity,
                moveDelay,
                moveSpeed,
                Time.fixedDeltaTime);

            rb.MovePosition(newPosition);
        }

        ApplyTilt();
    }

    private void HandleTouchInput()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                StopMoving();
                return;
            }

            UpdateTargetPosition(touch.position);
            return;
        }

        if (Input.GetMouseButton(0))
        {
            UpdateTargetPosition(Input.mousePosition);
            return;
        }

        StopMoving();
    }

    private void UpdateTargetPosition(Vector2 screenPosition)
    {
        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null)
            {
                return;
            }
        }

        Vector3 screenPoint = screenPosition;
        screenPoint.z = Mathf.Abs(cam.transform.position.z - transform.position.z);
        Vector3 worldPos = cam.ScreenToWorldPoint(screenPoint);
        targetPosition = new Vector2(worldPos.x, worldPos.y);
        isTouching = true;
    }

    private void StopMoving()
    {
        isTouching = false;
        smoothVelocity = Vector2.zero;
        targetPosition = rb.position;
    }

    private void ApplyTilt()
    {
        float targetZ = -smoothVelocity.x * tiltAmount;
        targetZ = Mathf.Clamp(targetZ, -tiltAmount, tiltAmount);

        Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetZ);
        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            targetRotation,
            tiltSpeed * Time.fixedDeltaTime);
    }
}
