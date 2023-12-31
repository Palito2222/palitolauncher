using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.SmartFormat.Extensions;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using UnityEngine.Networking;
using UnityEngine.UI;

public class LauncherUIManager : MonoBehaviour
{
    private const string GitHubApiBaseUrl = "https://api.github.com";
    private const string RepositoryOwner = "Palito2222";
    private const string RepositoryName = "AnimeGame";
    [SerializeField] private string token = "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";

    public GameObject errorGO;
    public TextMeshProUGUI errorText;
    public Button downloadButton;
    public TextMeshProUGUI buttonText;
    public Image greenTick;
    public TextMeshProUGUI versionName;

    private bool gameDownloaded = false;
    private bool updateAvailable = false;
    private bool gameRunning = false;
    private string currentTag = "0.0.0";
    private string latestTag;
    private string downloadedFilePath = "";

    public LocalizeStringEvent downloadStringEvent;

    private void Start()
    {
        // Obtener el idioma del sistema operativo
        SystemLanguage systemLanguage = GetSystemLanguage();

        // Establecer el idioma predeterminado en tu juego
        SetDefaultLanguage(systemLanguage);

        // Configurar el evento de clic del bot�n
        downloadButton.onClick.AddListener(HandleDownloadButtonClick);

        // Recuperar la ruta del archivo descargado al iniciar el launcher
        downloadedFilePath = PlayerPrefs.GetString("DownloadedFilePath1");

        // Recuperar el estado del juego descargado al iniciar el launcher
        gameDownloaded = PlayerPrefs.GetInt("GameDownloaded1", 0) == 1;

        // Actualizar el texto del bot�n seg�n el estado del juego descargado
        UpdateButtonText();

        currentTag = GetCurrentTag(); // Obt�n el tag actual de tu aplicaci�n
        currentTag = PlayerPrefs.GetString("CurrentTag1", currentTag);

        StartCoroutine(FirstCheckUpdateCoroutine());

        // Verificar si hay una actualizaci�n disponible
        if (gameDownloaded && downloadButton.isActiveAndEnabled)
        {
            StartCoroutine(CheckUpdateCoroutine());
        }
        else
        {
            // Actualizar el texto del bot�n seg�n el estado del juego descargado
            UpdateButtonText();
        }
    }

    #region Localization and Lenguage settings
    SystemLanguage GetSystemLanguage()
    {
        // Obtener la configuraci�n regional del sistema operativo
        System.Globalization.CultureInfo culture = System.Globalization.CultureInfo.CurrentCulture;

        // Convertir la configuraci�n regional en un SystemLanguage de Unity
        // Aqu� se mapea una lista de configuraciones regionales a los idiomas de Unity
        switch (culture.TwoLetterISOLanguageName)
        {
            case "en":
                return SystemLanguage.English;
            case "es":
                return SystemLanguage.Spanish;
            case "vi":
                return SystemLanguage.Vietnamese;
            case "ja":
                return SystemLanguage.Japanese;
            // Agrega m�s casos seg�n los idiomas que desees admitir
            default:
                return SystemLanguage.English; // Idioma desconocido, puedes establecer un idioma por defecto
        }
    }

    void SetDefaultLanguage(SystemLanguage language)
    {
        Debug.Log("Idioma del sistema detectado: " + language.ToString());

        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale(language);
    }

    void UpdateGlobalStringValue(string variableName, string variableValue)
    {
        var source = LocalizationSettings.StringDatabase.SmartFormatter.GetSourceExtension<PersistentVariablesSource>();
        var mystring = source["global"][variableName] as StringVariable;
        mystring.Value = variableValue; // This will trigger an update
    }

    void UpdateGlobalStringReferenceValue(string variableName)
    {
        var source = LocalizationSettings.StringDatabase.SmartFormatter.GetSourceExtension<PersistentVariablesSource>();
        var localizedString = source["global"][variableName] as LocalizedString;

        // Aqu� es donde cambias el LocalizedString en el LocalizeStringEvent
        downloadStringEvent.StringReference.SetReference(localizedString.TableReference, localizedString.TableEntryReference);
    }

    void UpdateGlobalIntegerValue(string variableName, int variableValue)
    {
        var source = LocalizationSettings.StringDatabase.SmartFormatter.GetSourceExtension<PersistentVariablesSource>();
        var mystring = source["global"][variableName] as IntVariable;
        mystring.Value = variableValue; // This will trigger an update
    }
    #endregion

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

            UpdateGlobalIntegerValue("ErrorCode", 1);

            errorGO.SetActive(true);

            yield return new WaitForSeconds(2.5f);

            errorGO.SetActive(false);

            UpdateButtonText();
        }
    }

    private IEnumerator DownloadFile(string url, string filePath, string zipFileName)
    {
        UpdateGlobalStringReferenceValue("downloadingStage");

        // Si filePath es null, se utiliza la ubicaci�n predeterminada en "Documentos"
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
                    UpdateGlobalStringReferenceValue("unzipingStage");

                    // Guardar el archivo descargado en la ubicaci�n especificada
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
                        string extractDirectoryRevision = Path.Combine(extractPath, "Zenless.Copy.Zero");

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
                            Debug.Log("Descompresi�n completada");
                        }
                    }
                    else
                    {
                        Debug.LogError("No se pudo descomprimir el archivo: " + downloadPath);

                        UpdateGlobalIntegerValue("ErrorCode", 3);
                        errorGO.SetActive(true);

                        yield return new WaitForSeconds(2.5f);

                        errorGO.SetActive(false);

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

                    // Actualizar el valor de currentTag solo cuando se haya descargado una nueva versi�n
                    currentTag = latestTag;
                    PlayerPrefs.SetString("CurrentTag1", currentTag);

                    // Actualizar el estado del juego descargado
                    gameDownloaded = true;
                    updateAvailable = false;
                    PlayerPrefs.SetInt("GameDownloaded1", 1);

                    // Actualizar el texto del bot�n seg�n el estado del juego descargado
                    UpdateButtonText();

                    // Activar la visibilidad del objeto greenTick
                    greenTick.enabled = true;

                    yield return new WaitForSeconds(3f);

                    greenTick.enabled = false;
                }
                else
                {
                    Debug.LogError("Ya tienes la �ltima versi�n descargada.");

                    UpdateGlobalIntegerValue("ErrorCode", 2);
                    errorGO.SetActive(true);

                    yield return new WaitForSeconds(2.5f);

                    errorGO.SetActive(false);

                    UpdateButtonText();
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
                    UpdateGlobalStringReferenceValue("unzipingStage");

                    // Guardar el archivo descargado en la ubicaci�n especificada
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
                        string extractDirectoryRevision = Path.Combine(extractPath, "Zenless.Copy.Zero");

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
                            Debug.Log("Descompresi�n completada");
                        }
                    }
                    else
                    {
                        Debug.Log("No se pudo descomprimir el archivo: " + downloadPath);

                        UpdateGlobalIntegerValue("ErrorCode", 3);
                        errorGO.SetActive(true);

                        yield return new WaitForSeconds(2.5f);

                        errorGO.SetActive(false);

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

                    // Actualizar el estado del juego descargado
                    gameDownloaded = true;
                    PlayerPrefs.SetInt("GameDownloaded1", 1);

                    // Actualizar el texto del bot�n seg�n el estado del juego descargado
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
                    Debug.LogError("Ya tienes la �ltima versi�n descargada.");

                    UpdateGlobalIntegerValue("ErrorCode", 2);
                    errorGO.SetActive(true);

                    yield return new WaitForSeconds(2.5f);

                    errorGO.SetActive(false);

                    UpdateButtonText();
                }
            }
        }
        else
        {
            Debug.LogError("Error al descargar el archivo: " + www.error);

            UpdateGlobalIntegerValue("ErrorCode", 4);
            errorGO.SetActive(true);

            yield return new WaitForSeconds(2.5f);

            errorGO.SetActive(false);

            UpdateButtonText();
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
            Debug.Log("�ltimo tag: " + latestRelease.tag_name);

            if (latestRelease != null)
            {
                latestTag = latestRelease.tag_name;

                if (currentTag != null && latestTag != currentTag)
                {
                    updateAvailable = true;
                    UpdateButtonText();
                }
                else
                {
                    updateAvailable = false;
                    UpdateButtonText();
                    Debug.Log("Versi�n Actual.");
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
            Debug.LogError("Error al obtener la �ltima release: " + www.error);
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

                                // Eliminar la versi�n anterior del juego
                                if (!string.IsNullOrEmpty(downloadedFilePath) && File.Exists(downloadedFilePath))
                                {
                                    File.Delete(downloadedFilePath);
                                    Debug.Log("Versi�n anterior del juego eliminada: " + downloadedFilePath);
                                }

                                yield return StartCoroutine(DownloadFile(downloadUrl, downloadedFilePath, asset.name));
                            }
                            else
                            {
                                Debug.Log("No hay Ruta de Carpeta");

                                UpdateGlobalIntegerValue("ErrorCode", 5);
                                errorGO.SetActive(true);

                                yield return new WaitForSeconds(2.5f);

                                errorGO.SetActive(false);

                                UpdateButtonText();
                            }
                        }
                        else
                        {
                            updateAvailable = false;
                            UpdateButtonText();

                            Debug.LogError("Versi�n Actual.");
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
                    Debug.LogError("La respuesta no contiene ninguna versi�n.");
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
                        string filePath = PlayerPrefs.GetString("DownloadedFilePath1");

                        // Eliminar la versi�n anterior del juego
                        if (!string.IsNullOrEmpty(downloadedFilePath) && File.Exists(downloadedFilePath))
                        {
                            File.Delete(downloadedFilePath);
                            Debug.Log("Versi�n anterior del juego eliminada: " + downloadedFilePath);
                        }

                        yield return StartCoroutine(DownloadFile(downloadUrl, filePath, asset.name));
                    }
                    else
                    {
                        updateAvailable = false;
                        UpdateButtonText();

                        Debug.LogError("Versi�n Actual.");
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
            // El juego ya est� en ejecuci�n
            Debug.Log("El juego ya est� en ejecuci�n.");
            return; // Evitar ejecutar el juego m�ltiples veces
        }

        // Ruta del directorio donde se encuentra el juego descargado
        string gameDirectory = Path.Combine(downloadedFilePath, "Zenless Copy Zero");

        // Ruta del archivo ejecutable del juego
        string gameExecutablePath = Path.Combine(gameDirectory, "Zenless Copy Zero.exe");

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
                PlayerPrefs.SetInt("GameDownloaded1", 1);
            }

            // Iniciar el proceso del juego
            gameProcess.Start();
        }
        else
        {
            Debug.LogError("El archivo ejecutable del juego no existe: " + gameExecutablePath + " GameDirectory: " + gameDirectory);

            UpdateGlobalIntegerValue("ErrorCode", 6);

            // Actualizar el estado del juego descargado
            gameDownloaded = false;
            PlayerPrefs.SetInt("GameDownloaded1", 0);

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
                Debug.LogError("No se pudo obtener la informaci�n de la �ltima release.");
            }
        }
        else
        {
            Debug.LogError("Error al obtener la �ltima release: " + www.error);
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

            // Verificar la condici�n para salir del bucle
            if (!downloadButton.isActiveAndEnabled)
            {
                break; // Sale del bucle
            }
        }
    }

    private IEnumerator FirstCheckUpdateCoroutine()
    {
        yield return StartCoroutine(CheckUpdate());
        UpdateGlobalStringValue("GameVer", $"{latestTag}");
    }

    public void ResetGameDownloaded()
    {
        gameDownloaded = false;
        PlayerPrefs.SetInt("GameDownloaded1", 0);
        UpdateGlobalStringReferenceValue("downloadStage");

        // Actualizar el texto del bot�n seg�n el estado del juego descargado
        UpdateButtonText();
    }

    public void HandleDownloadButtonClick()
    {
        if (updateAvailable)
        {
            // Realizar la actualizaci�n
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
            UpdateGlobalStringReferenceValue("updateStage");
        }
        else if (gameDownloaded)
        {
            UpdateGlobalStringReferenceValue("playStage");
        }
        else
        {
            UpdateGlobalStringReferenceValue("downloadStage");
        }
    }

    private IEnumerator RunGameCoroutine()
    {
        gameRunning = true;

        // Ruta y ejecuci�n del juego...

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
            PlayerPrefs.SetString("DownloadedFilePath1", Path.Combine(downloadedFilePath, "Zenless Copy Zero"));

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