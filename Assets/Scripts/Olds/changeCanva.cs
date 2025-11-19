using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class changeCanva : MonoBehaviour
{
    public void cambiarEscena()
    {
        SceneManager.LoadScene("3dEnvironment");
    }
}

