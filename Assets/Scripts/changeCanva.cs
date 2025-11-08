using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class changeCanva : MonoBehaviour
{
    [Header("Elementos del Canvas")]
    public Button boton;          // Botón que se activará
    public GameObject textoNuevo; // Texto que se activará
    public GameObject textoViejo; // Texto que se desactivará

    [Header("Tiempo de espera (segundos)")]
    public float tiempoEspera = 3f;

    private bool iniciado = false;

    void OnEnable()
    {
        // Solo iniciar si el Canvas acaba de activarse
        if (!iniciado)
        {
            iniciado = true;
            StartCoroutine(ActivarDespuesDeTiempo());
        }
    }

    IEnumerator ActivarDespuesDeTiempo()
    {
        // Estado inicial al activarse el Canvas
        if (boton != null) boton.gameObject.SetActive(false);
        if (textoNuevo != null) textoNuevo.SetActive(false);
        if (textoViejo != null) textoViejo.SetActive(true);

        // Esperar el tiempo indicado
        yield return new WaitForSeconds(tiempoEspera);

        // Cambiar los estados
        if (boton != null) boton.gameObject.SetActive(true);
        if (textoNuevo != null) textoNuevo.SetActive(true);
        if (textoViejo != null) textoViejo.SetActive(false);
    }
}

