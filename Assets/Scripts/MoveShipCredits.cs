using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MoveShipCredits : MonoBehaviour
{
    public float forwardSpeed, horizontalSpeed;
    public InputAction moveAction;

    // Límits del pla XY
    public Vector2 limitX = new Vector2(-2f, 2f);
    public Vector2 limitY = new Vector2(-1.5f, 1.5f);

    // Apuntar amb el ratolí
    public float aimDistance = 10f;    // a quina distància (en Z) davant la nau es troba el punt d'apuntat
    public float maxAimAngle = 35f;    // gir màxim de la nau respecte l'eix Z
    public float rotationSpeed = 8f;   // com de ràpid gira cap al ratolí

    private Rigidbody rb;
    private Camera cam;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        cam = Camera.main;
    }

    void FixedUpdate()
    {
        // 1) Orientació: mira cap al punt del ratolí
        Quaternion targetRot = Quaternion.LookRotation(GetAimDirection(), Vector3.up);
        Quaternion rot = Quaternion.Slerp(rb.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
        rb.MoveRotation(rot);

        // 2) Moviment: endavant en la direcció on apunta la nau,
        //    fletxes al pla local de la nau (dreta / amunt)
        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 velocity = rot * Vector3.forward * forwardSpeed;
        velocity += (rot * Vector3.right * input.x + rot * Vector3.up * input.y) * horizontalSpeed;

        Vector3 pos = rb.position + velocity * Time.fixedDeltaTime;
        pos.x = Mathf.Clamp(pos.x, limitX.x, limitX.y);
        pos.y = Mathf.Clamp(pos.y, limitY.x, limitY.y);

        rb.MovePosition(pos);
    }

    // Direcció des de la nau fins al punt de la pantalla on hi ha el ratolí,
    // projectat en un pla perpendicular a Z situat aimDistance davant la nau.
    private Vector3 GetAimDirection()
    {
        if (cam == null || Mouse.current == null)
            return Vector3.forward;

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane aimPlane = new Plane(Vector3.forward, rb.position + Vector3.forward * aimDistance);
        if (!aimPlane.Raycast(ray, out float dist))
            return Vector3.forward;

        Vector3 dir = (ray.GetPoint(dist) - rb.position).normalized;

        // Limita l'angle perquè la nau no giri massa
        return Vector3.RotateTowards(Vector3.forward, dir, maxAimAngle * Mathf.Deg2Rad, 0f);
    }

    private void OnCollisionEnter(Collision collision)
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnEnable()
    {
        moveAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
    }
}
