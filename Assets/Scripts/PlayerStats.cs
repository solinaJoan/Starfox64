using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// Estat del jugador: escut, vides i bombes.
// L'escut es reinicia a cada intent; les vides i les bombes són estàtiques
// perquè es conservin quan es recarrega l'escena després de perdre una vida.
public class PlayerStats : MonoBehaviour
{
    public const int MaxShield = 150;

    // Valors inicials de la partida
    public static int startLives = 3;
    public static int startBombs = 3;

    [Header("Dany")]
    [Tooltip("Segons d'invulnerabilitat després de rebre un impacte")]
    public float invulnerableTime = 1f;
    [Tooltip("Escena que es carrega quan s'acaben les vides")]
    public string gameOverScene = "Menu";

    public int Shield { get; private set; } = MaxShield;
    public static int Lives { get; private set; } = 3;
    public static int Bombs { get; private set; } = 3;
    public bool IsInvulnerable => Time.time < invulnerableUntil;

    // Esdeveniments per a la interfície
    public event Action<int, int> ShieldChanged;   // (actual, màxim)
    public event Action<int> LivesChanged;
    public event Action<int> BombsChanged;
    public event Action<int> Damaged;              // quantitat de dany rebut

    private float invulnerableUntil;
    private bool dead;

    // Reinicia els estàtics quan s'entra al Play Mode (per si el domain reload està desactivat)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => ResetRun();

    // Crida-ho quan comenci una partida nova (des del menú)
    public static void ResetRun()
    {
        Lives = startLives;
        Bombs = startBombs;
    }

    void Awake()
    {
        Shield = MaxShield;

        // Crea la interfície automàticament si l'escena no en té cap
        GameHUD hud = FindFirstObjectByType<GameHUD>();
        if (hud == null)
            hud = new GameObject("GameHUD").AddComponent<GameHUD>();
        hud.Bind(this);
    }

    public void TakeDamage(int amount)
    {
        if (dead || amount <= 0 || IsInvulnerable)
            return;

        invulnerableUntil = Time.time + invulnerableTime;
        Shield = Mathf.Max(0, Shield - amount);
        Damaged?.Invoke(amount);
        ShieldChanged?.Invoke(Shield, MaxShield);

        if (Shield == 0)
            Die();
    }

    public void RestoreShield(int amount)
    {
        if (dead || amount <= 0)
            return;
        Shield = Mathf.Min(MaxShield, Shield + amount);
        ShieldChanged?.Invoke(Shield, MaxShield);
    }

    public void AddLife()
    {
        Lives++;
        LivesChanged?.Invoke(Lives);
    }

    public void AddBomb()
    {
        Bombs++;
        BombsChanged?.Invoke(Bombs);
    }

    // Retorna true si quedava alguna bomba i s'ha gastat
    public bool UseBomb()
    {
        if (dead || Bombs <= 0)
            return false;
        Bombs--;
        BombsChanged?.Invoke(Bombs);
        return true;
    }

    private void Die()
    {
        dead = true;
        Lives--;
        LivesChanged?.Invoke(Lives);

        if (Lives < 0)
        {
            ResetRun();
            SceneManager.LoadScene(gameOverScene);
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
