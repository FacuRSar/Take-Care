using UnityEngine;

/* "abstract" significa que esta clase no se usa sola directamente,
*  sino que sirve como base para otros scripts concretos
*  como puertas, notas, objetos especiales, etc.
*/

public abstract class Interactable : MonoBehaviour
{
    [Header("Configuracion de interaccion")]
    [SerializeField] private string promptMessage = "E - Interactuar";
    /* mensaje que se va a mostrar en pantalla cuando el jugador mire este objeto
    *  lo dejo en SerializeField para poder cambiarlo desde el Inspector mas comodo y reusable
    *  sin hacerlo público para otros scripts.
    */

    public string PromptMessage => promptMessage;
    // Para que el resto lo pueda usar, lo asigno asi. No lo pueden cambiar

    public virtual void OnFocus()
    {
        // Sirve para ejecutar algo cuando el jugador empieza a mirar el objeto. (Lo dejo aca por si pinta despues usarlo)
        // Medio que ya meti la logica en otras partes, pero lo sigo dejando por si acaso
    }

    public virtual void OnLoseFocus()
    {
        // Sirve para ejecutar algo cuando el jugador deja de mirar el objeto. (Lo mismo, lo meto para no olvidarme y por si sirve)
    }

    public abstract void Interact(PlayerInteraction player);
    // Recbe como parametro al jugador que interactua por si mas adelante pinta usar info del jugador para la interaccion (ej si tiene algo)
}