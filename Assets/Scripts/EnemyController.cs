using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Distancia Frontal")]
    [SerializeField] private float targetDistanceInFront = 25f; // Distancia a mantener por delante de ti
    [SerializeField] private float forwardSpeed = 5f;

    [Header("Patrón de Movimiento (Serpenteo)")]
    [SerializeField] private float sideAmplitude = 8f;   // Cuánto se mueve a los lados (izquierda/derecha)
    [SerializeField] private float sideFrequency = 2f;   // Velocidad del movimiento lateral
    [SerializeField] private float upAmplitude = 3f;     // Cuánto sube y baja
    [SerializeField] private float upFrequency = 1.5f;   // Velocidad del movimiento vertical

    [Header("Disparo")]
    [SerializeField] private GameObject enemyLaserPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 1.5f;
    private float nextFireTime;

    [Header("Apunto")]
    [SerializeField] private float rotationSpeed = 3f;
    private Transform player;
    private Vector3 initialOffset;
    private float timeCounter;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        
        // Guardar el tiempo inicial con un desfase aleatorio para que varios enemigos no se muevan idéntico
        timeCounter = Random.Range(0f, 100f);
    }

    void Update()
    {
        if (player == null) return;

        timeCounter += Time.deltaTime;

        // 1. Calcular el punto base frente a la nave del jugador
        Vector3 targetPosition = player.position + (player.forward * targetDistanceInFront);

        // 2. Aplicar la oscilación sinusoidal a los lados y arriba/abajo
        float offsetX = Mathf.Sin(timeCounter * sideFrequency) * sideAmplitude;
        float offsetY = Mathf.Cos(timeCounter * upFrequency) * upAmplitude;

        // Sumar la oscilación a la posición objetivo
        targetPosition += player.right * offsetX + player.up * offsetY;

        // 3. Mover suavemente al enemigo hacia esa posición objetivo
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * forwardSpeed);

        // 4. Hacer que la nave enemiga mire siempre hacia tu nave para dispararte
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        if (directionToPlayer != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
        }

        // 5. Disparar
        if (Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + 1f / fireRate;
        }
    }

    void Shoot()
    {
        if (enemyLaserPrefab != null && firePoint != null)
        {
            Instantiate(enemyLaserPrefab, firePoint.position, firePoint.rotation);
        }
    }
}