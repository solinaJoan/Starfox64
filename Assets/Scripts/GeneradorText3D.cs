using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GeneradorText3D : MonoBehaviour
{
    public enum AlineacioText
    {
        Esquerra,
        Centre,
        Dreta
    }

    [System.Serializable]
    public struct LletraPrefab
    {
        public char caracter;
        public GameObject prefab;
    }

    [Header("Contingut")]
    [TextArea(3, 10)]
    public string text = "PRIMERA LINIA\nSEGONA LINIA";

    [Header("Espaiat i Alineació")]
    public float espaiEntreLletres = 0.1f;
    public float ampladaEspai = 0.6f;
    public float espaiEntreLinies = 1.5f;
    public AlineacioText alineacio = AlineacioText.Centre;
    public bool centrarVerticalment = true;

    [Header("Correcció de Lletres")]
    [Tooltip("Gira cada lletra individualment (ex: 0, 180, 0 si miren enrere o 0, 90, 0 si estan de costat)")]
    public Vector3 rotacioIndividual = Vector3.zero;
    public bool ignorarMajuscules = true;

    [Header("Alfabet")]
    public List<LletraPrefab> alfabet = new List<LletraPrefab>();

    [ContextMenu("Generar Text")]
    public void GenerarText()
    {
        // 1. Esborra el text anterior
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying)
                Destroy(transform.GetChild(i).gameObject);
            else
                DestroyImmediate(transform.GetChild(i).gameObject);
        }

        if (string.IsNullOrEmpty(text)) return;

        // 2. Indexa l'alfabet
        Dictionary<char, GameObject> mapa = new Dictionary<char, GameObject>();
        foreach (var item in alfabet)
        {
            char clau = ignorarMajuscules ? char.ToUpper(item.caracter) : item.caracter;
            if (item.prefab != null && !mapa.ContainsKey(clau))
                mapa.Add(clau, item.prefab);
        }

        // 3. Processa el text línia per línia
        string textNormalitzat = text.Replace("\r", "");
        if (ignorarMajuscules) textNormalitzat = textNormalitzat.ToUpper();

        string[] linies = textNormalitzat.Split('\n');
        List<Transform> totesLesLletres = new List<Transform>();
        float cursorY = 0f;

        for (int indexLinia = 0; indexLinia < linies.Length; indexLinia++)
        {
            string linia = linies[indexLinia];
            float cursorX = 0f;
            List<Transform> lletresLinia = new List<Transform>();

            foreach (char c in linia)
            {
                if (c == ' ')
                {
                    cursorX += ampladaEspai;
                    continue;
                }

                if (mapa.TryGetValue(c, out GameObject prefab))
                {
                    GameObject instancia = Instantiate(prefab, transform);
                    instancia.name = $"L{indexLinia + 1}_{c}";
                    instancia.transform.localPosition = Vector3.zero;

                    // Combina la rotació original del prefab amb la correcció de l'Inspector
                    instancia.transform.localRotation = Quaternion.Euler(rotacioIndividual) * prefab.transform.localRotation;

                    // Calcula els límits reals en l'eix X del pare un cop girada la lletra
                    ObtenirLimitsLocalsX(instancia, out float minX, out float maxX);
                    float amplada = maxX - minX;

                    instancia.transform.localPosition = new Vector3(cursorX - minX, cursorY, 0f);
                    cursorX += amplada + espaiEntreLletres;

                    lletresLinia.Add(instancia.transform);
                    totesLesLletres.Add(instancia.transform);
                }
            }

            // Alinea horitzontalment la línia actual
            if (lletresLinia.Count > 0 && alineacio != AlineacioText.Esquerra)
            {
                float ampladaLinia = cursorX - espaiEntreLletres;
                float offsetX = alineacio == AlineacioText.Centre ? ampladaLinia * 0.5f : ampladaLinia;

                foreach (Transform t in lletresLinia)
                {
                    t.localPosition -= new Vector3(offsetX, 0f, 0f);
                }
            }

            cursorY -= espaiEntreLinies;
        }

        // 4. Centra verticalment tot el bloc si està activat
        if (centrarVerticalment && linies.Length > 1 && totesLesLletres.Count > 0)
        {
            float alcadaTotal = (linies.Length - 1) * espaiEntreLinies;
            float offsetY = alcadaTotal * 0.5f;

            foreach (Transform t in totesLesLletres)
            {
                t.localPosition += new Vector3(0f, offsetY, 0f);
            }
        }
    }

    private void ObtenirLimitsLocalsX(GameObject obj, out float minX, out float maxX)
    {
        minX = float.MaxValue;
        maxX = float.MinValue;
        bool trobat = false;

        MeshFilter[] meshFilters = obj.GetComponentsInChildren<MeshFilter>();
        foreach (MeshFilter mf in meshFilters)
        {
            if (mf.sharedMesh == null) continue;

            Bounds b = mf.sharedMesh.bounds;
            Vector3 c = b.center;
            Vector3 e = b.extents;

            // Projecta els 8 vèrtexs de la caixa contenidora a l'espai local del pare
            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 cantonadaLocal = c + new Vector3(e.x * x, e.y * y, e.z * z);
                        Vector3 puntMon = mf.transform.TransformPoint(cantonadaLocal);
                        Vector3 puntPare = transform.InverseTransformPoint(puntMon);

                        if (puntPare.x < minX) minX = puntPare.x;
                        if (puntPare.x > maxX) maxX = puntPare.x;
                        trobat = true;
                    }
                }
            }
        }

        if (!trobat)
        {
            minX = 0f;
            maxX = 1f;
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(GeneradorText3D))]
public class GeneradorText3DEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GeneradorText3D generador = (GeneradorText3D)target;
        GUILayout.Space(10);
        if (GUILayout.Button("Generar / Actualitzar Text 3D", GUILayout.Height(30)))
        {
            Undo.RegisterFullObjectHierarchyUndo(generador.gameObject, "Generar Text 3D");
            generador.GenerarText();
        }
    }
}
#endif