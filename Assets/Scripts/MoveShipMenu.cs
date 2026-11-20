using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MoveShipMenu : MonoBehaviour
{
    // Posicions de les opcions del menú (de dalt a baix)
    public Vector3[] positions = new Vector3[]
    {
        new Vector3(0f, 0f, 0f),
        new Vector3(0f, -1.5f, 0f),
        new Vector3(0f, -2f, 0f)
    };

    // Escena que es carrega per cada opció (mateix ordre que positions).
    // Deixa-la buida ("") per sortir del joc.
    public string[] scenes = new string[] { "SampleScene", "Credits", "" };

    // Paràmetres de l'enlairament
    public float acceleration = 20f;   // com de ràpid arriba a la velocitat màxima
    public float maxSpeed = 15f;       // velocitat final
    public float launchDuration = 1.5f; // segons abans de canviar d'escena

    // Fill SmokeEffect de la nau: desactivat fins que s'enlaira
    public GameObject smokeEffect;

    private int currentIndex = 0;
    private bool launching = false;

    void Start()
    {
        transform.position = positions[currentIndex];
        if (smokeEffect != null)
            smokeEffect.SetActive(false);
    }

    void Update()
    {
        if (launching)
            return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        // wasPressedThisFrame només és cert el frame en què es prem la tecla,
        // així mantenir-la premuda només mou una opció.
        if (keyboard.upArrowKey.wasPressedThisFrame)
        {
            currentIndex = (currentIndex - 1 + positions.Length) % positions.Length;
            transform.position = positions[currentIndex];
        }
        else if (keyboard.downArrowKey.wasPressedThisFrame)
        {
            currentIndex = (currentIndex + 1) % positions.Length;
            transform.position = positions[currentIndex];
        }
        else if (keyboard.enterKey.wasPressedThisFrame ||
                 keyboard.numpadEnterKey.wasPressedThisFrame ||
                 keyboard.spaceKey.wasPressedThisFrame)
        {
            StartCoroutine(Launch());
        }
    }

    private IEnumerator Launch()
    {
        launching = true;

        if (smokeEffect != null)
            smokeEffect.SetActive(true);

        float speed = 0f;
        float elapsed = 0f;

        while (elapsed < launchDuration)
        {
            // Accelera fins a maxSpeed i després manté la velocitat
            speed = Mathf.MoveTowards(speed, maxSpeed, acceleration * Time.deltaTime);
            transform.position += transform.forward * speed * Time.deltaTime;

            elapsed += Time.deltaTime;
            yield return null;
        }

        string scene = currentIndex < scenes.Length ? scenes[currentIndex] : "";
        if (string.IsNullOrEmpty(scene))
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
        else
        {
            SceneManager.LoadScene(scene);
        }
    }

    public int GetSelectedOption()
    {
        return currentIndex;
    }
}
