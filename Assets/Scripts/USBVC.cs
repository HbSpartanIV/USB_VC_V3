using System.Collections;
using System.Collections.Generic;
using Adrenak.UniMic;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class USBVC : MonoBehaviour {
    [SerializeField] private TMP_Dropdown _menu;
    [SerializeField] private RawImage _image;
    [SerializeField] private MicAudioSource _audio;
    private WebCamDevice[] devices;
    private List<string> currentDevices;
    private List<WebCamTexture> webcamTextures;
    private Mic.Device[] micsList;

    private void Awake() {
        Mic.Init();
        devices = WebCamTexture.devices;
        currentDevices = new List<string>();
        webcamTextures = new List<WebCamTexture>();
    }

    private void Start() {
        InitializeAll();
    }

    public void ToggleMenu() {
        if (_menu.gameObject.activeSelf)
            _menu.gameObject.SetActive(false);
        else
            _menu.gameObject.SetActive(true);
    }

    public void ExitApp() {
        Application.Quit();
    }

    private void InitializeAll() {
        foreach (var item in devices) {
            currentDevices.Add(item.name);
            webcamTextures.Add(new WebCamTexture(item.name, 1920, 1080, 60));
        }

        micsList = new Mic.Device[currentDevices.Count];
        for (var i = 0; i < micsList.Length; i++)
            foreach (var mic in Mic.AvailableDevices)
                if (mic.Name.Contains(currentDevices[i]))
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
        if (_audio.Device != null)
            _audio.Device.StopRecording();
        _audio.Device = micsList[device];
        if (_audio.Device != null) _audio.Device.StartRecording();
    }

    public void SelectDevice() {
        InitializeCamera(_menu.value);
        PlayerPrefs.SetInt("ID", _menu.value);
        ToggleMenu();
    }
}