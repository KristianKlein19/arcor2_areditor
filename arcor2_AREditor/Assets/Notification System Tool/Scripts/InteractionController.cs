using System;
using System.Collections;
using Base;
using UnityEngine;
using RotaryHeart.Lib.SerializableDictionary;
[System.Serializable]
public class InteractionModeDictionary : SerializableDictionaryBase<string, GameObject> { }
public class InteractionController : Singleton<InteractionController>
{
    [SerializeField] InteractionModeDictionary interactionModes;
    private GameObject currentMode;

    protected void Awake()
    {
        ResetAllModes();
    }

    private void Start()
    {
        _EnableMode("Main");
    }

    private void ResetAllModes()
    {
        foreach (GameObject mode in interactionModes.Values)
        {
            mode.SetActive(false);
        }
    }

    public static void EnableMode(string name)
    {
        Instance?._EnableMode(name);
    }

    private void _EnableMode(string s)
    {
        GameObject modeObject;
        if (interactionModes.TryGetValue(name, out modeObject))
        {
            StartCoroutine(ChangeMode(modeObject));
        }
        else
        {
            Debug.Log("undefined mode named " + name);
        }
    }

    IEnumerator ChangeMode(GameObject mode)
    {
        if (mode == currentMode)
            yield break;

        if (currentMode)
        {
            currentMode.SetActive(false);
            yield return null;
        }
        currentMode = mode;
        mode.SetActive(true);
    }
}
