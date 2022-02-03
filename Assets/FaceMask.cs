using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TensorFlowLite;
using NRKernal;
using NRKernal.Record;

public class FaceMask : MonoBehaviour
{
    [SerializeField, FilePopup("*.tflite")] string fileName;
    [SerializeField] RawImage cameraView = null;
    [SerializeField] Text framePrefabRed = null;
    [SerializeField] Text framePrefabGreen = null;
    [SerializeField, Range(0f, 1f)] float scoreThreshold = 0.5f;
    [SerializeField] TextAsset labelMap = null;

    SSD_facemask ssd;

    Text[] framesRed;
    Text[] framesGreen;

    public string[] labels;

    private NRRGBCamTexture RGBCamTexture { get; set; }

    public Text textLabel;
    public Text textInfo;
    Text[] text;

    public BlendMode blendMode;
    public ResolutionLevel resolutionLevel;
    public LayerMask cullingMask;
    public NRVideoCapture.AudioState audioState = NRVideoCapture.AudioState.ApplicationAudio;
    public bool useGreenBackGround;

    NRVideoCapture m_VideoCapture = null;

    public enum ResolutionLevel
    {
        High,
        Middle,
        Low,
    }
    public string VideoSavePath
    {
        get
        {
            string timeStamp = Time.time.ToString().Replace(".", "").Replace(":", "");
            string filename = string.Format("Nreal_Record_{0}.mp4", timeStamp);
            //return Path.Combine(Application.persistentDataPath, filename);
            return "rtp://192.168.1.156:8080";
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        if (m_VideoCapture == null)
        {
            CreateVideoCapture(() =>
            {
                StartVideoCapture();
            });
        }

        string path = Path.Combine(Application.streamingAssetsPath, fileName);
        ssd = new SSD_facemask(path);

        RGBCamTexture = new NRRGBCamTexture();
        cameraView.texture = RGBCamTexture.GetTexture();
        RGBCamTexture.Play();

        // Init frames
        text = new Text[10];
        framesRed = new Text[10];
        framesGreen = new Text[10];
        var parent = cameraView.transform;
        for (int i = 0; i < framesRed.Length; i++)
        {
            framesRed[i] = Instantiate(framePrefabRed, new Vector3(0, 0, 1f), Quaternion.identity, parent);
            framesGreen[i] = Instantiate(framePrefabGreen, new Vector3(0, 0, 1f), Quaternion.identity, parent);
            text[i] = Instantiate(textLabel, new Vector3(0, 0, 1f), Quaternion.identity, parent);
            framesRed[i].gameObject.SetActive(false);
            framesGreen[i].gameObject.SetActive(false);
            text[i].gameObject.SetActive(false);
        }

        // Labels
        labels = labelMap.text.Split('\n');
    }
    void Update()
    {
        for (int i = 0; i < framesRed.Length; i++)
        {
            framesRed[i].gameObject.SetActive(false);
            framesGreen[i].gameObject.SetActive(false);
            text[i].gameObject.SetActive(false);
        }

        ssd.Invoke(RGBCamTexture.GetTexture());

        var results = ssd.GetResults();

        int count = 0;
        int finalResult = 0;
        var size = cameraView.rectTransform.rect.size;
        for (int i = 0; i < framesRed.Length; i++)
        {
            if (results[i].score >= scoreThreshold)
            {
                count++;

                var lbt = text[i].transform as RectTransform;
                var position = results[i].rect.position * size - size * 0.5f;
                var sizeDelta = results[i].rect.size * size;
                if (results[i].classID.Equals(0))
                {
                    finalResult++;
                    SetFrame(framesGreen[i], results[i], size);
                    text[i].text = " " + $"{GetLabelName(results[i].classID)} : {(int)(results[i].score * 100)}%";
                    text[i].color = Color.green;
                    lbt.anchoredPosition = new Vector2(position.x, position.y);
                    lbt.sizeDelta = sizeDelta;
                    text[i].gameObject.SetActive(true);
                }
                else
                {
                    SetFrame(framesRed[i], results[i], size);
                    text[i].text = " " + $"{GetLabelName(results[i].classID)} : {(int)(results[i].score * 100)}%";
                    text[i].color = Color.red;
                    lbt.anchoredPosition = new Vector2(position.x, position.y);
                    lbt.sizeDelta = sizeDelta;
                    text[i].gameObject.SetActive(true);
                }
            }
        }

        //cameraView.material = ssd.transformMat;
        cameraView.texture = ssd.inputTex;
    }

    void SetFrame(Text frame, SSD_facemask.Result result, Vector2 size)
    {
        var rt = frame.transform as RectTransform;
        rt.anchoredPosition = result.rect.position * size - size * 0.5f;
        rt.sizeDelta = result.rect.size * size;
        frame.text = " " + $"{GetLabelName(result.classID)} : {(int)(result.score * 100)}%";
        frame.gameObject.SetActive(true);
    }

    string GetLabelName(int id)
    {
        if (id < 0 || id >= labels.Length - 1)
        {
            return "?";
        }
        return labels[id + 1];
    }
    void OnDestroy()
    {
        if (m_VideoCapture.IsRecording)
        {
            StopVideoCapture();
        }
        RGBCamTexture?.Stop();
        ssd?.Dispose();
        RGBCamTexture = null;
    }
    void CreateVideoCapture(Action callback)
    {
        NRVideoCapture.CreateAsync(false, delegate (NRVideoCapture videoCapture)
        {
            NRDebugger.Info("Created VideoCapture Instance!");
            if (videoCapture != null)
            {
                m_VideoCapture = videoCapture;
                callback?.Invoke();
            }
            else
            {
                NRDebugger.Error("Failed to create VideoCapture Instance!");
            }
        });
    }
    public void StartVideoCapture()
    {
        CameraParameters cameraParameters = new CameraParameters();
        Resolution cameraResolution = GetResolutionByLevel(resolutionLevel);
        cameraParameters.hologramOpacity = 0.0f;
        cameraParameters.frameRate = cameraResolution.refreshRate;
        cameraParameters.cameraResolutionWidth = cameraResolution.width;
        cameraParameters.cameraResolutionHeight = cameraResolution.height;
        cameraParameters.pixelFormat = CapturePixelFormat.BGRA32;
        // Set the blend mode.
        cameraParameters.blendMode = blendMode;
        // Set audio state, audio record needs the permission of "android.permission.RECORD_AUDIO",
        // Add it to your "AndroidManifest.xml" file in "Assets/Plugin".
        cameraParameters.audioState = audioState;

        m_VideoCapture.StartVideoModeAsync(cameraParameters, OnStartedVideoCaptureMode);
    }
    private Resolution GetResolutionByLevel(ResolutionLevel level)
    {
        var resolutions = NRVideoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height);
        Resolution resolution = new Resolution();
        switch (level)
        {
            case ResolutionLevel.High:
                resolution = resolutions.ElementAt(0);
                break;
            case ResolutionLevel.Middle:
                resolution = resolutions.ElementAt(1);
                break;
            case ResolutionLevel.Low:
                resolution = resolutions.ElementAt(2);
                break;
            default:
                break;
        }
        return resolution;
    }
    public void StopVideoCapture()
    {
        if (m_VideoCapture == null || !m_VideoCapture.IsRecording)
        {
            NRDebugger.Warning("Can not stop video capture!");
            return;
        }

        NRDebugger.Info("Stop Video Capture!");
        m_VideoCapture.StopRecordingAsync(OnStoppedRecordingVideo);
    }
    /// <summary> Executes the 'started video capture mode' action. </summary>
    /// <param name="result"> The result.</param>
    void OnStartedVideoCaptureMode(NRVideoCapture.VideoCaptureResult result)
    {
        if (!result.success)
        {
            NRDebugger.Info("Started Video Capture Mode faild!");
            return;
        }

        NRDebugger.Info("Started Video Capture Mode!");
        m_VideoCapture.StartRecordingAsync(VideoSavePath, OnStartedRecordingVideo);
    }

    /// <summary> Executes the 'started recording video' action. </summary>
    /// <param name="result"> The result.</param>
    void OnStartedRecordingVideo(NRVideoCapture.VideoCaptureResult result)
    {
        if (!result.success)
        {
            NRDebugger.Info("Started Recording Video Faild!");
            return;
        }

        NRDebugger.Info("Started Recording Video!");
        if (useGreenBackGround)
        {
            // Set green background color.
            m_VideoCapture.GetContext().GetBehaviour().SetBackGroundColor(Color.green);
        }
        m_VideoCapture.GetContext().GetBehaviour().CaptureCamera.cullingMask = cullingMask.value;
    }

    /// <summary> Executes the 'stopped recording video' action. </summary>
    /// <param name="result"> The result.</param>
    void OnStoppedRecordingVideo(NRVideoCapture.VideoCaptureResult result)
    {
        if (!result.success)
        {
            NRDebugger.Info("Stopped Recording Video Faild!");
            return;
        }

        NRDebugger.Info("Stopped Recording Video!");
        m_VideoCapture.StopVideoModeAsync(OnStoppedVideoCaptureMode);
    }
    /// <summary> Executes the 'stopped video capture mode' action. </summary>
    /// <param name="result"> The result.</param>
    void OnStoppedVideoCaptureMode(NRVideoCapture.VideoCaptureResult result)
    {
        NRDebugger.Info("Stopped Video Capture Mode!");

        // Release video capture resource.
        m_VideoCapture.Dispose();
        m_VideoCapture = null;
    }
}
