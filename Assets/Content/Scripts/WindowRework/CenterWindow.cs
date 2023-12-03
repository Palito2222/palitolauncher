using JetBrains.Annotations;
using UnityEngine;

public class CenterWindow : MonoBehaviour
{
    public float baseAspectRatio = 16f / 9f;  // Relación de aspecto base (16:9)
    public float widthPercentage = 0.666f;   // Porcentaje del ancho de la pantalla
    public float heightPercentage = 0.645f;  // Porcentaje del alto de la pantalla

    public int width = 1280;
    public int height = 700;

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
        //float aspectRatio = (float)Screen.currentResolution.width / Screen.currentResolution.height;
        //float aspectRatioPercentage = baseAspectRatio / aspectRatio;

        //int screenWidth = Mathf.RoundToInt(Screen.currentResolution.width * widthPercentage * aspectRatioPercentage);
        //int screenHeight = Mathf.RoundToInt(Screen.currentResolution.height * heightPercentage);

        Screen.SetResolution(width, height, Screen.fullScreenMode);
    }

    void CenterWindowW()
    {
        Screen.fullScreenMode = FullScreenMode.Windowed;  // Cambiar a modo de ventana para permitir el centrado
    }
}
