using UnityEngine;
using UnityEngine.UI;
using TMP;
using System;
using System.Collections;
using System.Collections.Generics;

public class CreateSaveUI {
    private GameObject CreateButton() {
        return DefaultControls.CreateButton(new DefaultControls.Resources())
    }

    public void CreateScrollMenu(List<SaveFile> _saveFiles) {
        // TODO: Create List and buttons for each
    }
}