using UnityEngine;

// Càmera que avança en Z al ritme de la nau, però amb X, Y i rotació fixes.
// Funciona tant si la càmera és filla de Ship com si no (fixa la posició al món).
public class CameraRail : MonoBehaviour
{
    public Transform target;       // la nau (Ship). Si és buit, s'agafa el pare
    public float offsetZ = -2f;    // distància darrere la nau

    private Vector3 fixedXY;
    private Quaternion fixedRotation;

    void Start()
    {
        if (target == null)
            target = transform.parent;

        // Guarda la posició X/Y i la rotació inicials (les que tens a l'escena)
        fixedXY = transform.position;
        fixedRotation = transform.rotation;
        if (target != null)
            offsetZ = transform.position.z - target.position.z;
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        transform.SetPositionAndRotation(
            new Vector3(fixedXY.x, fixedXY.y, target.position.z + offsetZ),
            fixedRotation);
    }
}
