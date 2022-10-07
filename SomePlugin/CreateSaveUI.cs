using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

[DefaultExecutionOrder(1)] // After main canvas
public class CreateSaveUI : MonoBehaviour {
    public static Canvas ModCanvas { private set; get; }
    private Dropdown m_dropdownMenu;

    private void Start()
    {
        Transform mainCanvas = FindObjectOfType<Canvas>().rootCanvas.transform;
        ModCanvas = new GameObject("ModCanvas", typeof(Canvas), typeof(RectTransform)).GetComponent<Canvas>();
        ModCanvas.gameObject.AddComponent<GraphicRaycaster>();
        ModCanvas.transform.SetParent(mainCanvas, false);
        ModCanvas.GetComponent<RectTransform>().sizeDelta = mainCanvas.GetComponent<RectTransform>().sizeDelta;
    }

    [ContextMenu("CreateDropdownMenu")]
    public void CreateDropdownMenu() {
        //GameObject newButton = DefaultControls.CreateButton(new DefaultControls.Resources());
        m_dropdownMenu = DefaultControls.CreateDropdown(new DefaultControls.Resources()).GetComponent<Dropdown>();
        m_dropdownMenu.transform.SetParent(ModCanvas.transform);
        m_dropdownMenu.ClearOptions();
        m_dropdownMenu.AddOptions(new List<String>() { "Save 1", "Save 2", "Save 3" });
        m_dropdownMenu.value = 0;
        //m_dropdownMenu.onValueChanged +=
    }

    public void CreateScrollMenuEntry()
    {

    }
}