using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float lookSpeed = 3f;
    public float jumpForce = 5f;

    private Camera playerCamera;
    private Rigidbody rb;
    private bool isGrounded;
    private float rotationX = 0f;

    void Start()
    {
        playerCamera = GetComponentInChildren<Camera>();
        rb = GetComponent<Rigidbody>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * lookSpeed;
        float mouseY = Input.GetAxis("Mouse Y") * lookSpeed;

        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -90f, 90f);
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);

        if (Input.GetButtonDown("Jump") && isGrounded)
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = (Cursor.lockState == CursorLockMode.Locked) ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = (Cursor.lockState == CursorLockMode.None);
        }
    }

    void FixedUpdate()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 movement = transform.right * horizontal + transform.forward * vertical;
        rb.MovePosition(rb.position + movement.normalized * moveSpeed * Time.fixedDeltaTime);

        isGrounded = Physics.Raycast(transform.position, Vector3.down, 1.1f);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.contacts[0].normal.y > 0.5f)
            isGrounded = true;
    }

    void OnCollisionExit(Collision collision)
    {
        isGrounded = false;
    }
}
