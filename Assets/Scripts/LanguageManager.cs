using UnityEngine;
using UnityEngine.Localization.Settings;
using TMPro;
using System.Collections;

public class LanguageManager : MonoBehaviour
{
    public TMP_Dropdown dropDown;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        dropDown.onValueChanged.AddListener(OnLocaleChanged);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnLocaleChanged(int index)
    {
        StartCoroutine(ChangeLocale(index));
    }

    IEnumerator ChangeLocale(int index)
    {
        var selectedLocale = LocalizationSettings.AvailableLocales.Locales[index];
        yield return LocalizationSettings.InitializationOperation;
        LocalizationSettings.SelectedLocale = selectedLocale;
    }
}
