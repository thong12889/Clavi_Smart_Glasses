﻿using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using NRKernal;
using NRKernal.Record;

public class StreamScript : MonoBehaviour
{
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
    }

    private void OnDestroy()
    {
        if (m_VideoCapture.IsRecording)
        {
            StopVideoCapture();
        }
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