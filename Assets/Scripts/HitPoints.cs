using UnityEngine;
using TMPro;

public class HitPoints : MonoBehaviour
{
    [SerializeField] private int _hitPoints = 100;
    [SerializeField] private TMP_Text _text;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _text.text = _hitPoints.ToString();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void TakeDamage(int value)
    {
        _hitPoints -= value;
        _text.text = _hitPoints.ToString();
        if (_hitPoints <= 0){
            // Debug.log("I Died");
        }
    }
}
