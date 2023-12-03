using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class LauncherGame3 : MonoBehaviour
{
    private const string GitHubApiBaseUrl = "https://api.github.com";
    private const string RepositoryOwner = "Palito2222";
    private const string RepositoryName = "ShooterV";
    [SerializeField] private string token = "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";

    public GameObject errorGO;
    public UnityEngine.UI.Button downloadButton;
    public TextMeshProUGUI buttonText;
    public Image greenTick;
    public TextMeshProUGUI versionName;

    private bool gameDownloaded = false;
    private bool updateAvailable = false;
    private bool gameRunning = false;
    private string currentTag = "0.0.0";
    private string latestTag;
    private string downloadedFilePath = "";

    private void Start()
    {
        // Configurar el evento de clic del botón
        downloadButton.onClick.AddListener(HandleDownloadButtonClick);

        // Recuperar la ruta del archivo descargado al iniciar el launcher
        downloadedFilePath = PlayerPrefs.GetString("DownloadedFilePath3");

        // Recuperar el estado del juego descargado al iniciar el launcher
        gameDownloaded = PlayerPrefs.GetInt("GameDownloaded3", 0) == 1;

        // Actualizar el texto del botón según el estado del juego descargado
        UpdateButtonText();

        currentTag = GetCurrentTag(); // Obtén el tag actual de tu aplicación
        currentTag = PlayerPrefs.GetString("CurrentTag3", currentTag);

        // Verificar si hay una actualización disponible
        if (gameDownloaded && downloadButton.isActiveAndEnabled)
        {
            StartCoroutine(CheckUpdateCoroutine());
        }
        else
        {
            // Actualizar el texto del botón según el estado del juego descargado
            UpdateButtonText();
        }
    }

    #region Main Methods
    private IEnumerator GetReleaseByTag()
    {
        string apiUrl = $"{GitHubApiBaseUrl}/repos/{RepositoryOwner}/{RepositoryName}/releases/latest";

        UnityWebRequest www = UnityWebRequest.Get(apiUrl);
        www.SetRequestHeader("Accept", "application/vnd.github.v3+json");
        www.SetRequestHeader("Authorization", "token " + token);

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            string responseJson = www.downloadHandler.text;

            // Utilizamos JsonConvert.DeserializeObject para analizar el JSON
            ReleaseResponse releaseResponse = JsonConvert.DeserializeObject<ReleaseResponse>(responseJson);

            if (releaseResponse != null)
            {
                ReleaseResponse latestRelease = releaseResponse;

                if (latestRelease.assets != null && latestRelease.assets.Length > 0)
                {
                    ReleaseAsset asset = latestRelease.assets[0];

                    string downloadUrl = asset.browser_download_url;
                    string folderPath = SelectFolder();

                    if (string.IsNullOrEmpty(folderPath))
                    {
                        www.Abort();
                    }

                    if (folderPath != null)
                    {
                        string filePath = folderPath;

                        yield return StartCoroutine(DownloadFile(downloadUrl, filePath, asset.name));
                        Debug.Log("Se ha descargado: " + asset.name);
                    }
                }
                else
                {
                    Debug.LogError("La release no contiene archivos adjuntos.");
                }
            }
            else
            {
                Debug.LogError("No se encontraron releases.");
            }
        }
        else
        {
            Debug.LogError("Error al obtener la release: " + www.error);

            errorGO.SetActive(true);

            yield return new WaitForSeconds(2.5f);

            errorGO.SetActive(false);

            UpdateButtonText();
        }
    }

    private IEnumerator DownloadFile(string url, string filePath, string zipFileName)
    {
        buttonText.text = "Descargando...";

        // Si filePath es null, se utiliza la ubicación predeterminada en "Documentos"
        if (string.IsNullOrEmpty(filePath))
        {
            filePath = Path.Combine(SelectFolder(), zipFileName);
        }

        UnityWebRequest www = UnityWebRequest.Get(url);
        www.SetRequestHeader("Accept", "application/vnd.github.v3+json");
        www.SetRequestHeader("Authorization", "token " + token);

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            if (updateAvailable)
            {
                string downloadPath = Path.Combine(filePath, zipFileName);

                // Se modifica la variable extractPath a la carpeta designada
                string extractPath = filePath;

                // Verificar si el archivo ZIP ya existe en el escritorio
                if (File.Exists(downloadPath))
                {
                    // Borrar el archivo ZIP existente en el escritorio
                    File.Delete(downloadPath);
                    Debug.Log("Archivo ZIP duplicado eliminado: " + downloadPath);
                }

                if (!File.Exists(downloadPath))
                {
                    buttonText.text = "Descomprimiendo...";

                    // Guardar el archivo descargado en la ubicación especificada
                    if (!File.Exists(downloadPath))
                    {
                        File.WriteAllBytes(downloadPath, www.downloadHandler.data);
                        Debug.Log("Descarga completada: " + downloadPath);
                    }
                    else
                    {
                        Debug.Log("El archivo ya existe: " + downloadPath);
                    }

                    // Extraer el archivo ZIP en la carpeta de destino
                    if (File.Exists(downloadPath) && Directory.Exists(extractPath))
                    {
                        string extractDirectoryRevision = Path.Combine(extractPath, "CCTycoon");

                        if (Directory.Exists(extractDirectoryRevision))
                        {
                            // Eliminar el contenido de la carpeta de destino
                            foreach (string file in Directory.GetFiles(extractDirectoryRevision))
                            {
                                File.Delete(file);
                            }

                            foreach (string dir in Directory.GetDirectories(extractDirectoryRevision))
                            {
                                Directory.Delete(dir, true);
                            }
                        }
                        else if (!Directory.Exists(extractDirectoryRevision))
                        {
                            Debug.Log("El Directorio no existe");
                        }

                        if (File.Exists(downloadPath) && Directory.Exists(extractPath))
                        {
                            ZipFile.ExtractToDirectory(downloadPath, extractPath);
                            Debug.Log("Descompresión completada");
                        }
                    }
                    else
                    {
                        Debug.Log("No se pudo descomprimir el archivo: " + downloadPath);

                        gameDownloaded = true;
                        updateAvailable = true;
                        UpdateButtonText();
                        yield return null;
                    }

                    // Borrar el archivo ZIP
                    if (File.Exists(downloadPath))
                    {
                        File.Delete(downloadPath);
                        Debug.Log("Archivo ZIP eliminado: " + downloadPath);
                    }
                    else
                    {
                        Debug.Log("El archivo no existe: " + downloadPath);
                    }

                    // Actualizar el valor de currentTag solo cuando se haya descargado una nueva versión
                    currentTag = latestTag;
                    PlayerPrefs.SetString("CurrentTag3", currentTag);

                    // Actualizar el estado del juego descargado
                    gameDownloaded = true;
                    updateAvailable = false;
                    PlayerPrefs.SetInt("GameDownloaded3", 1);

                    // Actualizar el texto del botón según el estado del juego descargado
                    UpdateButtonText();

                    // Activar la visibilidad del objeto greenTick
                    greenTick.enabled = true;

                    yield return new WaitForSeconds(3f);

                    greenTick.enabled = false;
                }
                else
                {
                    Debug.LogError("Ya tienes la última versión descargada.");
                }
            }
            else
            {
                string downloadPath = Path.Combine(filePath, zipFileName);

                // Se modifica la variable extractPath a la carpeta designada
                string extractPath = filePath;

                // Verificar si el archivo ZIP ya existe en el escritorio
                if (File.Exists(downloadPath))
                {
                    // Borrar el archivo ZIP existente en el escritorio
                    File.Delete(downloadPath);
                    Debug.Log("Archivo ZIP duplicado eliminado: " + downloadPath);
                }

                if (!File.Exists(downloadPath))
                {
                    buttonText.text = "Descomprimiendo...";

                    // Guardar el archivo descargado en la ubicación especificada
                    if (!File.Exists(downloadPath))
                    {
                        File.WriteAllBytes(downloadPath, www.downloadHandler.data);
                        Debug.Log("Descarga completada: " + downloadPath);
                    }
                    else
                    {
                        Debug.Log("El archivo ya existe: " + downloadPath);
                    }

                    // Extraer el archivo ZIP en la carpeta de destino
                    if (File.Exists(downloadPath) && Directory.Exists(extractPath))
                    {
                        string extractDirectoryRevision = Path.Combine(extractPath, "CCTycoon");

                        if (Directory.Exists(Path.Combine(extractPath, "CCTycoon")))
                        {
                            // Eliminar el contenido de la carpeta de destino
                            foreach (string file in Directory.GetFiles(extractDirectoryRevision))
                            {
                                File.Delete(file);
                            }

                            foreach (string dir in Directory.GetDirectories(extractDirectoryRevision))
                            {
                                Directory.Delete(dir, true);
                            }
                        }

                        ZipFile.ExtractToDirectory(downloadPath, extractPath);
                        Debug.Log("Descompresión completada");
                    }
                    else
                    {
                        Debug.Log("No se pudo descomprimir el archivo: " + downloadPath);
                    }

                    // Borrar el archivo ZIP
                    if (File.Exists(downloadPath))
                    {
                        File.Delete(downloadPath);
                        Debug.Log("Archivo ZIP eliminado: " + downloadPath);
                    }
                    else
                    {
                        Debug.Log("El archivo no existe: " + downloadPath);
                    }

                    // Actualizar el estado del juego descargado
                    gameDownloaded = true;
                    PlayerPrefs.SetInt("GameDownloaded3", 1);

                    // Actualizar el texto del botón según el estado del juego descargado
                    UpdateButtonText();

                    // Activar la visibilidad del objeto greenTick
                    greenTick.enabled = true;

                    if (gameDownloaded)
                    {
                        StartCoroutine(CheckUpdateCoroutine());
                    }
                    else
                    {
                        UpdateButtonText();
                    }

                    yield return new WaitForSeconds(3f);

                    greenTick.enabled = false;
                }
                else
                {
                    Debug.LogError("Ya tienes la última versión descargada.");
                }
            }
        }
        else
        {
            Debug.LogError("Error al descargar el archivo: " + www.error);

            StartCoroutine(ErrorPopUp());
        }
    }

    private IEnumerator CheckUpdate()
    {
        Debug.Log("Verificando actualizaciones");

        string apiUrl = $"{GitHubApiBaseUrl}/repos/{RepositoryOwner}/{RepositoryName}/releases/latest";

        UnityWebRequest www = UnityWebRequest.Get(apiUrl);
        www.SetRequestHeader("Accept", "application/vnd.github.v3+json");
        www.SetRequestHeader("Authorization", "token " + token);

        yield return www.SendWebRequest();
        Debug.Log("Resultado de la solicitud: " + www.result);

        if (www.result == UnityWebRequest.Result.Success)
        {
            string responseJson = www.downloadHandler.text;

            ReleaseResponse latestRelease = JsonConvert.DeserializeObject<ReleaseResponse>(responseJson);
            Debug.Log("Último tag: " + latestRelease.tag_name);

            if (latestRelease != null)
            {
                latestTag = latestRelease.tag_name;
                Debug.Log("Tag actual: " + currentTag);

                if (currentTag != null && latestTag != currentTag)
                {
                    updateAvailable = true;
                    UpdateButtonText();
                }
                else
                {
                    updateAvailable = false;
                    UpdateButtonText();
                    Debug.Log("Versión Actual.");
                }
            }
            else
            {
                updateAvailable = false;
                UpdateButtonText();
                Debug.LogError("La release no contiene archivos adjuntos.");
            }
        }
        else
        {
            Debug.LogError("Error al obtener la última release: " + www.error);
        }
    }

    private IEnumerator PerformUpdate()
    {
        string apiUrl = $"{GitHubApiBaseUrl}/repos/{RepositoryOwner}/{RepositoryName}/releases";

        UnityWebRequest www = UnityWebRequest.Get(apiUrl);
        www.SetRequestHeader("Accept", "application/vnd.github.v3+json");
        www.SetRequestHeader("Authorization", "token " + token);

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            string responseJson = www.downloadHandler.text;

            // Check if the response is an array or an object
            if (responseJson.StartsWith("["))
            {
                // Response is an array of releases
                List<ReleaseResponse> releaseResponses = JsonConvert.DeserializeObject<List<ReleaseResponse>>(responseJson);

                if (releaseResponses.Count > 0)
                {
                    ReleaseResponse latestRelease = releaseResponses[0];
                    if (latestRelease.assets != null && latestRelease.assets.Length > 0)
                    {
                        ReleaseAsset asset = latestRelease.assets[0];
                        latestTag = latestRelease.tag_name;

                        if (latestTag != currentTag)
                        {
                            string downloadUrl = asset.browser_download_url;
                            string folderPath = SelectFolder();

                            if (folderPath != null)
                            {
                                string filePath = Path.Combine(folderPath, asset.name);

                                // Eliminar la versión anterior del juego
                                if (!string.IsNullOrEmpty(downloadedFilePath) && File.Exists(downloadedFilePath))
                                {
                                    File.Delete(downloadedFilePath);
                                    Debug.Log("Versión anterior del juego eliminada: " + downloadedFilePath);
                                }

                                yield return StartCoroutine(DownloadFile(downloadUrl, downloadedFilePath, asset.name));
                            }
                            else
                            {
                                Debug.Log("No hay Ruta de Carpeta");
                            }
                        }
                        else
                        {
                            updateAvailable = false;
                            UpdateButtonText();

                            Debug.LogError("Versión Actual.");
                        }
                    }
                    else
                    {
                        updateAvailable = false;
                        UpdateButtonText();

                        Debug.LogError("La release no contiene archivos adjuntos.");
                    }
                }
                else
                {
                    Debug.LogError("La respuesta no contiene ninguna versión.");
                }
            }
            else
            {
                // Response is a single release
                ReleaseResponse releaseResponse = JsonConvert.DeserializeObject<ReleaseResponse>(responseJson);

                if (releaseResponse.assets != null && releaseResponse.assets.Length > 0)
                {
                    ReleaseAsset asset = releaseResponse.assets[0];
                    latestTag = releaseResponse.tag_name;

                    if (latestTag != currentTag)
                    {
                        string downloadUrl = asset.browser_download_url;
                        string filePath = PlayerPrefs.GetString("DownloadedFilePath3");

                        // Eliminar la versión anterior del juego
                        if (!string.IsNullOrEmpty(downloadedFilePath) && File.Exists(downloadedFilePath))
                        {
                            File.Delete(downloadedFilePath);
                            Debug.Log("Versión anterior del juego eliminada: " + downloadedFilePath);
                        }

                        yield return StartCoroutine(DownloadFile(downloadUrl, filePath, asset.name));
                    }
                    else
                    {
                        updateAvailable = false;
                        UpdateButtonText();

                        Debug.LogError("Versión Actual.");
                    }
                }
                else
                {
                    updateAvailable = false;
                    UpdateButtonText();

                    Debug.LogError("La release no contiene archivos adjuntos.");
                }
            }
        }
        else
        {
            Debug.LogError("Error al obtener la release por el tag: " + www.error);
        }
    }

    private void RunGame()
    {
        if (gameRunning)
        {
            // El juego ya está en ejecución
            Debug.Log("El juego ya está en ejecución.");
            return; // Evitar ejecutar el juego múltiples veces
        }

        // Ruta del directorio donde se encuentra el juego descargado
        string gameDirectory = Path.Combine(downloadedFilePath, "CCTycoon");

        // Ruta del archivo ejecutable del juego
        string gameExecutablePath = Path.Combine(gameDirectory, "CCTycoon.exe");

        StartCoroutine(RunGameCoroutine());

        // Comprobar si el archivo ejecutable existe
        if (File.Exists(gameExecutablePath))
        {
            // Crear un proceso para ejecutar el archivo .exe del juego
            System.Diagnostics.Process gameProcess = new System.Diagnostics.Process();
            gameProcess.StartInfo.FileName = gameExecutablePath;

            if (!gameDownloaded)
            {
                // Actualizar el estado del juego a descargado
                gameDownloaded = true;
                PlayerPrefs.SetInt("GameDownloaded3", 1);
            }

            // Iniciar el proceso del juego
            gameProcess.Start();
        }
        else
        {
            Debug.LogError("El archivo ejecutable del juego no existe: " + gameExecutablePath + " GameDirectory: " + gameDirectory);

            // Actualizar el estado del juego descargado
            gameDownloaded = false;
            PlayerPrefs.SetInt("GameDownloaded3", 0);

            UpdateButtonText();
            StartCoroutine(ErrorPopUp());
            HandleDownloadButtonClick();
        }
    }

    private string GetCurrentTag()
    {
        string apiUrl = $"{GitHubApiBaseUrl}/repos/{RepositoryOwner}/{RepositoryName}/releases/latest";

        UnityWebRequest www = UnityWebRequest.Get(apiUrl);
        www.SetRequestHeader("Accept", "application/vnd.github.v3+json");
        www.SetRequestHeader("Authorization", "token " + token);

        www.SendWebRequest();

        while (!www.isDone)
        {
            // Espera hasta que la solicitud termine
        }

        if (www.result == UnityWebRequest.Result.Success)
        {
            string responseJson = www.downloadHandler.text;
            ReleaseResponse latestRelease = JsonConvert.DeserializeObject<ReleaseResponse>(responseJson);

            if (latestRelease != null)
            {
                return latestRelease.tag_name;
            }
            else
            {
                Debug.LogError("No se pudo obtener la información de la última release.");
            }
        }
        else
        {
            Debug.LogError("Error al obtener la última release: " + www.error);
        }

        return null;
    }
    #endregion

    #region Utility Methods
    private IEnumerator CheckUpdateCoroutine()
    {
        while (true)
        {
            yield return StartCoroutine(CheckUpdate());
            yield return new WaitForSeconds(10f);

            // Verificar la condición para salir del bucle
            if (!downloadButton.isActiveAndEnabled)
            {
                break; // Sale del bucle
            }
        }
    }

    public void ResetGameDownloaded()
    {
        gameDownloaded = false;
        PlayerPrefs.SetInt("GameDownloaded3", 0);
        buttonText.text = "Descargar";

        // Actualizar el texto del botón según el estado del juego descargado
        UpdateButtonText();
    }

    public void HandleDownloadButtonClick()
    {
        if (updateAvailable)
        {
            // Realizar la actualización
            StartCoroutine(PerformUpdate());
            return;
        }
        else if (gameDownloaded)
        {
            // Ejecutar el juego
            RunGame();
            return;
        }
        else
        {
            // Descargar el juego
            StartCoroutine(GetReleaseByTag());
        }
    }

    private void UpdateButtonText()
    {
        if (updateAvailable)
        {
            buttonText.text = "Actualizar";
        }
        else if (gameDownloaded)
        {
            buttonText.text = "Jugar";
        }
        else
        {
            buttonText.text = "Descargar";
        }
    }

    private IEnumerator RunGameCoroutine()
    {
        gameRunning = true;

        // Ruta y ejecución del juego...

        yield return new WaitForSeconds(3f);

        gameRunning = false;
    }

    public string SelectFolder()
    {
        var dlg = new FolderPicker();
        dlg.InputPath = @"C:\Program Files";
        if (dlg.ShowDialog(IntPtr.Zero) == true)
        {
            downloadedFilePath = dlg.ResultPath;
            PlayerPrefs.SetString("DownloadedFilePath3", Path.Combine(downloadedFilePath, "CCTycoon"));

            return dlg.ResultPath;
        }

        return null;
    }

    public IEnumerator ErrorPopUp()
    {
        errorGO.SetActive(true);

        yield return new WaitForSeconds(2.5f);

        errorGO.SetActive(false);
    }
    #endregion

    [System.Serializable]
    private class ReleaseResponse
    {
        public string tag_name;
        public ReleaseAsset[] assets;
    }

    [System.Serializable]
    private class ReleaseAsset
    {
        public string name;
        public string browser_download_url;
    }
}