using UnityEngine.InputSystem;
using UnityEngine;

public class LaserBeam : MonoBehaviour
{

    [SerializeField] private LineRenderer _lineRenderer;
    [SerializeField] private Transform _firePoint;
    [SerializeField] private Transform _target;
    [SerializeField] private ParticleSystem _particleBlast;
    [SerializeField] private float _maxDistance = 50f;
    [SerializeField] private int _damage = 10;
    [SerializeField] private float _beamDuration = 0.2f;

    // Llargada del raig actual (fins a l'impacte o _maxDistance)
    private float _beamLength;

    void Start()
    {
        _lineRenderer.enabled = false;
    }

    void Update()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            return;

        Vector3 origin = _firePoint.position;
        // Direcció del model de la nau (aquest script hi està posat), que és
        // el que gira cap al ratolí. _firePoint és fill de "Ship", que no gira.
        Vector3 direction = transform.forward;
        _beamLength = _maxDistance;

        // PlayerManager.Instance.CamShake();
        // AudioManager.Instance.PlaySFXClip(3);

        if (Physics.Raycast(origin, direction, out RaycastHit hitInfo, _maxDistance))
        {
            _beamLength = hitInfo.distance;
            _target.position = hitInfo.point;

            HitPoints hp = hitInfo.collider.GetComponent<HitPoints>();
            if (hp != null)
                hp.TakeDamage(_damage);

            _particleBlast.transform.position = hitInfo.point;
            _particleBlast.Play();
        }

        _lineRenderer.enabled = true;
        UpdateBeam();

        // Si es torna a disparar abans que s'apagui, que no l'apagui l'anterior
        CancelInvoke(nameof(TurnOffLaser));
        Invoke(nameof(TurnOffLaser), _beamDuration);
    }

    // Cada frame, mentre el raig és visible, es redibuixa des de la posició
    // actual del canó: així segueix la nau en lloc de quedar-se enrere.
    void LateUpdate()
    {
        if (_lineRenderer.enabled)
            UpdateBeam();
    }

    private void UpdateBeam()
    {
        Vector3 start = _firePoint.position;
        _lineRenderer.SetPosition(0, start);
        _lineRenderer.SetPosition(1, start + transform.forward * _beamLength);
    }

    private void TurnOffLaser()
    {
        _lineRenderer.enabled = false;
    }
}
