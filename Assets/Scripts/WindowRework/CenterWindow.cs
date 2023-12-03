using UnityEngine;

public class CenterWindow : MonoBehaviour
{
    public float widthPercentage = 0.7f;   // Porcentaje del ancho de la pantalla
    public float heightPercentage = 0.7f;  // Porcentaje del alto de la pantalla

    void Awake()
    {
        SetScreenResolution();
        CenterWindowW();
    }

    private void Start()
    {
        // Establece el objetivo de FPS a 60
        Application.targetFrameRate = 60;
    }

    void SetScreenResolution()
    {
        int screenWidth = Mathf.RoundToInt(Screen.currentResolution.width * widthPercentage);
        int screenHeight = Mathf.RoundToInt(Screen.currentResolution.height * heightPercentage);

        Screen.SetResolution(screenWidth, screenHeight, Screen.fullScreenMode);
    }

    void CenterWindowW()
    {
        Screen.fullScreenMode = FullScreenMode.Windowed;  // Cambiar a modo de ventana para permitir el centrado
    }
}
