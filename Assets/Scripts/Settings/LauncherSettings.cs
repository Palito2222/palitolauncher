using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Menu;

public class LauncherSettings : MonoBehaviour
{
    [Header("General Things")]
    public GameObject SettingsMenu;
    public Button closeSettingMenu;
    public Button openSettingMenu;
    public LauncherUIManager GameCS;

    [Header("Day/Night Mode")]
    public GameObject gameBG;
    public Sprite nightmodeBG;
    public Sprite daymodeBG;
    public Button changeMode;
    private bool nightModeBool = false;

    public Color NightTextColor;
    public Color NightIconsColor;
    public Color NightOptionsBGColor;
    public Color NightOptionsBorderColor;
    public Color DayTextColor;
    public Color DayIconsColor;
    public Color DayOptionsBGColor;
    public Color DayOptionsBorderColor;
    public TextMeshProUGUI gameName;
    public TextMeshProUGUI gameVersion;
    public Image settingsButton;
    public Image changeModeButton;
    public Image optionsBackground;
    public Image optionsBorderBackground;

    public TextMeshProUGUI lenguageDropdownText;

    [Header("Lenguage Settings")]

    public TMP_Dropdown lenguageDropdown;

    private void Start()
    {
        // Establecer nightModeBool basado en el valor recuperado
        nightModeBool = PlayerPrefs.GetInt("NightMode") == 0;

        // Cambiar la apariencia según el modo guardado
        ChangeDayNightMode();

        InitializeDropdown();
    }

    #region Change DayNight Mode
    public void ChangeDayNightMode()
    {
        nightModeBool = !nightModeBool;

        if (nightModeBool == true) 
        {
            PlayerPrefs.SetInt("NightMode", 1);
            ApplyNightMode();
        }
        else 
        {
            PlayerPrefs.SetInt("NightMode", 0);
            ApplyDayMode();
        }
    }

    private void ApplyNightMode()
    {
        gameBG.GetComponent<Image>().sprite = nightmodeBG;
        SetColors(NightTextColor, NightIconsColor, NightOptionsBGColor, NightOptionsBorderColor);
    }

    private void ApplyDayMode()
    {
        gameBG.GetComponent<Image>().sprite = daymodeBG;
        SetColors(DayTextColor, DayIconsColor, DayOptionsBGColor, DayOptionsBorderColor);
    }

    private void SetColors(Color textColor, Color iconsColor, Color optionsBGColor, Color optionsBorderColor)
    {
        gameName.color = textColor;
        gameVersion.color = textColor;
        settingsButton.color = iconsColor;
        changeModeButton.color = iconsColor;
        optionsBackground.color = optionsBGColor;
        optionsBorderBackground.color = optionsBorderColor;
        lenguageDropdownText.color = textColor;
    }
    #endregion

    #region Close Open Setting Window

    public void OpenSettingsWindow()
    {
        if (SettingsMenu.activeSelf) { return; }

        SettingsMenu.SetActive(true);
    }

    public void CloseSettingsWindow()
    {
        SettingsMenu.SetActive(false);
    }
    #endregion

    void InitializeDropdown()
    {
        // Obtener los idiomas disponibles desde las configuraciones de localización
        var availableLanguages = LocalizationSettings.AvailableLocales.Locales;

        // Crear una lista de nombres de idiomas para mostrar en el dropdown
        List<string> languageNames = new List<string>();

        foreach (var locale in availableLanguages)
        {
            languageNames.Add(locale.name);
        }

        // Limpiar las opciones actuales del dropdown
        lenguageDropdown.ClearOptions();

        // Agregar los nombres de idiomas al dropdown
        lenguageDropdown.AddOptions(languageNames);

        // Asignar la función que manejará el cambio de idioma al evento OnValueChanged del dropdown
        lenguageDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
    }

    void OnDropdownValueChanged(int index)
    {
        // Obtener los idiomas disponibles
        var availableLanguages = LocalizationSettings.AvailableLocales.Locales;

        // Obtener el idioma seleccionado del índice del dropdown
        var selectedLanguage = availableLanguages[index];

        // Establecer el idioma seleccionado en las configuraciones de localización
        LocalizationSettings.SelectedLocale = selectedLanguage;
    }
}
