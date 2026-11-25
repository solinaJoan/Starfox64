using UnityEngine;
using UnityEngine.InputSystem;

public class MoveShip : MonoBehaviour
{
    public float forwardSpeed, horizontalSpeed;
    public float verticalSpeed = 7f;
    public float minX = -2f, maxX = 2f, minY = 0f, maxY = 4f;
    public InputAction moveAction;

    // Apuntar amb el ratolí
    public Transform model;            // el model "ship" (fill). Si és buit es busca automàticament
    public float aimDistance = 15f;    // distància en Z davant la nau on es projecta el ratolí
    public float maxAimAngle = 40f;    // gir màxim respecte l'eix Z
    public float rotationSpeed = 10f;  // com de ràpid gira cap al ratolí

    // Dany que rep l'escut en xocar contra un obstacle
    public int collisionDamage = 30;

    private Rigidbody rb;
    private Camera cam;
    private PlayerStats stats;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        cam = Camera.main;
        if (model == null)
            model = transform.Find("ship");

        stats = GetComponent<PlayerStats>();
        if (stats == null)
            stats = gameObject.AddComponent<PlayerStats>();
    }

    void FixedUpdate()
    {
        // Eixos del món (no transform.forward/right/up), així el moviment
        // no depèn de cap rotació: sempre endavant en Z i fletxes al pla XY.
        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 velocity = Vector3.forward * forwardSpeed;
        velocity += input.x * horizontalSpeed * Vector3.right;
        velocity += input.y * verticalSpeed * Vector3.up;

        Vector3 pos = rb.position + velocity * Time.fixedDeltaTime;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);

        CurrentVelocity = (pos - rb.position) / Time.fixedDeltaTime;
        rb.MovePosition(pos);
    }

    // Velocitat real de la nau (la fa servir ShipShooter per als projectils)
    public Vector3 CurrentVelocity { get; private set; }

    void Update()
    {
        // Només gira el model, no el Rigidbody: el collider i la càmera no giren.
        if (model == null)
            return;

        Quaternion target = Quaternion.LookRotation(GetAimDirection(), Vector3.up);
        model.rotation = Quaternion.Slerp(model.rotation, target, rotationSpeed * Time.deltaTime);
    }

    // Direcció des de la nau fins al punt on apunta el ratolí, projectat
    // en un pla perpendicular a Z situat aimDistance davant la nau.
    private Vector3 GetAimDirection()
    {
        if (cam == null || Mouse.current == null)
            return Vector3.forward;

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        Vector3 origin = model.position;
        Plane aimPlane = new Plane(Vector3.forward, origin + Vector3.forward * aimDistance);
        if (!aimPlane.Raycast(ray, out float dist))
            return Vector3.forward;

        Vector3 dir = (ray.GetPoint(dist) - origin).normalized;
        return Vector3.RotateTowards(Vector3.forward, dir, maxAimAngle * Mathf.Deg2Rad, 0f);
    }

    // En xocar es perd escut; PlayerStats s'encarrega de treure una vida
    // i reiniciar el nivell quan l'escut arriba a 0.
    private void OnCollisionEnter(Collision collision)
    {
        stats.TakeDamage(collisionDamage);
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
