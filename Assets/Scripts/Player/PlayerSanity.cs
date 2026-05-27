using UnityEngine;

public class PlayerSanity : MonoBehaviour
{
    float BarSanity;
    float BarSanityMin = 0f;
    float BarSanityMax = 0f;
    public float barSanityMax { get { return BarSanityMax; } set { BarSanityMax = value; } }
    float BarSanityCurrent;
    public float barSanityCurrent { get { return BarSanityCurrent; } set { BarSanityCurrent = value; } }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // La barra de cordura se va a ir llenando dependiendo de las acciones del jugador, por ejemplo, si el jugador se queda quieto o hace algo que no le gusta a la muñeca, la barra de cordura se va a ir llenando, si el jugador hace algo que le gusta a la muñeca, la barra de cordura se va a ir vaciando, si la barra de cordura llega al maximo, el jugador pierde la cordura se activa queste y empieza a ver cosas raras o se activa el estado de Terrified

        BarSanity = Mathf.Clamp(BarSanity, BarSanityMin, BarSanityMax);

        BarSanityCurrent = BarSanity;
    }
}
