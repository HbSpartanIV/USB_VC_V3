using System;
using System.Collections.Generic;
using System.Linq;
using Adrenak.UniMic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class USBVC : MonoBehaviour {
    [SerializeField] private TMP_Dropdown _menu;
    [SerializeField] private RawImage _image;
    [SerializeField] private MicAudioSource _audio;
    private WebCamDevice[] devices;
    private List<string> currentDevices;
    private List<WebCamTexture> webcamTextures;
    private Mic.Device[] micsList;
    private bool firstStart;

    private void Awake() {
        Application.targetFrameRate = -1;
        QualitySettings.vSyncCount = 0;
        Mic.Init();
        devices = WebCamTexture.devices;
        currentDevices = new List<string>();
        webcamTextures = new List<WebCamTexture>();
    }

    private void Update() {
        Cursor.visible = _menu.gameObject.activeSelf;
    }

    private void Start() {
        InitializeAll();
    }

    public void ToggleMenu() {
        _menu.gameObject.SetActive(!_menu.gameObject.activeSelf);
    }

    public void ExitApp() {
        Application.Quit();
    }

    private void InitializeAll() {
        foreach (var item in devices)
            //Debug.Log(item.name);
            if (item.name.Contains("Camera (NVIDIA Broadcast)") || item.name.Contains("OBS Virtual Camera")) {
                //Debug.Log("Virtual Camera detected");
            }
            else {
                currentDevices.Add(item.name);
                webcamTextures.Add(new WebCamTexture(item.name, 1920 * 2, 1080 * 2, 60));
            }

        micsList = new Mic.Device[currentDevices.Count];
        for (var i = 0; i < micsList.Length; i++)
            foreach (var mic in Mic.AvailableDevices.Where(mic => mic.Name.Contains(currentDevices[i])))
                micsList[i] = mic;

        _menu.ClearOptions();
        _menu.AddOptions(currentDevices);
        if (PlayerPrefs.HasKey("ID")) _menu.value = PlayerPrefs.GetInt("ID");
        SelectDevice();
        _menu.gameObject.SetActive(false);
    }

    private void InitializeCamera(int device) {
        _image.color = Color.white;
        _image.texture = webcamTextures[device];
        webcamTextures[device].Play();
        _audio.Device?.StopRecording();
        _audio.Device = micsList[device];
        _audio.Device?.StartRecording(32);
    }

    public void SelectDevice() {
        Invoke(nameof(ReloadDevice), .1f);
        InitializeCamera(_menu.value);
        PlayerPrefs.SetInt("ID", _menu.value);
        ToggleMenu();
    }

    private void ReloadDevice() {
        if (firstStart) return;
        //Debug.Log("Reloading device");
        firstStart = true;
        SelectDevice();
        _menu.gameObject.SetActive(false);
    }
}