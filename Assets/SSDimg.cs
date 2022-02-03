using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NRKernal.Record;
using NRKernal;
using System;
using System.Linq;
using TensorFlowLite;
using System.IO;
using OpenCvSharp;

public class SSDimg : MonoBehaviour
{
    [SerializeField, FilePopup("*.tflite")] string fileName;
    [SerializeField, Range(0f, 1f)] float scoreThreshold = 0.5f;
    [SerializeField] Text framePrefabRed = null;
    [SerializeField] Text framePrefabGreen = null;
    [SerializeField] TextAsset labelMap = null;
    public string[] labels;
    public RawImage captureView;
    public Text showResult;
    public RawImage frameFit;
    public RawImage resultLabel;
    public Text textLabel;

    Text[] framesRed;
    Text[] framesGreen;
    Text[] labelText;

    private NRPhotoCapture m_PhotoCaptureObject;
    private Resolution m_CameraResolution;
    private bool isOnPhotoProcess = false;

    SSD ssd;

    GameObject quad;

    // Start is called before the first frame update
    void Start()
    {
        framesRed = new Text[10];
        framesGreen = new Text[10];
        labelText = new Text[10];
        var parent = captureView.transform;
        for (int i = 0; i < framesRed.Length; i++)
        {
            framesRed[i] = Instantiate(framePrefabRed, new Vector3(0, 0, 3), Quaternion.identity, parent);
            framesGreen[i] = Instantiate(framePrefabGreen, new Vector3(0, 0, 3), Quaternion.identity, parent);
            labelText[i] = Instantiate(textLabel, new Vector3(0, 0, 3), Quaternion.identity, parent);
            framesRed[i].gameObject.SetActive(false);
            framesGreen[i].gameObject.SetActive(false);
            labelText[i].gameObject.SetActive(false);
        }
        captureView.gameObject.SetActive(false);
        string path = Path.Combine(Application.streamingAssetsPath, fileName);
        ssd = new SSD(path);
        labels = labelMap.text.Split('\n');
    }

    // Update is called once per frame
    void Update()
    {
        if (NRInput.GetButtonDown(ControllerButton.TRIGGER))
        {
            var headTran = NRSessionManager.Instance.NRHMDPoseTracker.centerAnchor;
            captureView.transform.position = headTran.position + headTran.forward * 3f;
            captureView.transform.rotation = headTran.rotation;
            if (quad != null)
            {
                quad.SetActive(false);
            } 
            for (int i = 0; i < framesRed.Length; i++)
            {
                framesRed[i].gameObject.SetActive(false);
                framesGreen[i].gameObject.SetActive(false);
                labelText[i].gameObject.SetActive(false);
            }
            captureView.gameObject.SetActive(false);
            frameFit.gameObject.SetActive(false);
            showResult.gameObject.SetActive(false);
            resultLabel.gameObject.SetActive(false); 
            TakeAPhoto();
        }
    }
    void Create(Action<NRPhotoCapture> onCreated)
    {
        if (m_PhotoCaptureObject != null)
        {
            NRDebugger.Info("The NRPhotoCapture has already been created.");
            return;
        }

        // Create a PhotoCapture object
        NRPhotoCapture.CreateAsync(false, delegate (NRPhotoCapture captureObject)
        {
            m_CameraResolution = NRPhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();

            if (captureObject == null)
            {
                NRDebugger.Error("Can not get a captureObject.");
                return;
            }

            m_PhotoCaptureObject = captureObject;

            CameraParameters cameraParameters = new CameraParameters();
            cameraParameters.cameraResolutionWidth = m_CameraResolution.width;
            cameraParameters.cameraResolutionHeight = m_CameraResolution.height;
            cameraParameters.pixelFormat = CapturePixelFormat.BGRA32;
            cameraParameters.blendMode = BlendMode.Blend;

            // Activate the camera
            m_PhotoCaptureObject.StartPhotoModeAsync(cameraParameters, delegate (NRPhotoCapture.PhotoCaptureResult result)
            {
                NRDebugger.Info("Start PhotoMode Async");
                if (result.success)
                {
                    onCreated?.Invoke(m_PhotoCaptureObject);
                }
                else
                {
                    isOnPhotoProcess = false;
                    NRDebugger.Error("Start PhotoMode faild." + result.resultType);
                }
            });
        });
    }
    void TakeAPhoto()
    {
        if (isOnPhotoProcess)
        {
            NRDebugger.Warning("Currently in the process of taking pictures, Can not take photo .");
            return;
        }

        isOnPhotoProcess = true;
        if (m_PhotoCaptureObject == null)
        {
            this.Create((capture) =>
            {
                capture.TakePhotoAsync(OnCapturedPhotoToMemory);
            });
        }
        else
        {
            m_PhotoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
        }
    }
    /// <summary> Executes the 'captured photo memory' action. </summary>
    /// <param name="result">            The result.</param>
    /// <param name="photoCaptureFrame"> The photo capture frame.</param>
    void OnCapturedPhotoToMemory(NRPhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
    {
        var targetTexture = new Texture2D(m_CameraResolution.width, m_CameraResolution.height);
        // Copy the raw image data into our target texture
        photoCaptureFrame.UploadImageDataToTexture(targetTexture);
        captureView.material = ssd.transformMat;
        captureView.texture = ssd.inputTex;
        captureView.texture = targetTexture;
        /*OpenCvSharp.Unity.TextureConversionParams param = new OpenCvSharp.Unity.TextureConversionParams
        {
            FlipVertically = false
        };
        var targetmat = OpenCvSharp.Unity.TextureToMat(targetTexture, param);
        var matTex = OpenCvSharp.Unity.MatToTexture(targetmat);*/

        ssd.Invoke(captureView.texture);
        var results = ssd.GetResults();
        /*foreach (var result0 in results)
        {
            if (result0.score >= scoreThreshold)
            {
                OpenCvSharp.Rect cvRect = new OpenCvSharp.Rect(new OpenCvSharp.Point(result0.rect.position.x * (float)targetmat.Cols, result0.rect.position.y * (float)targetmat.Rows), new OpenCvSharp.Size(result0.rect.width * (float)targetmat.Cols, result0.rect.height * (float)targetmat.Rows));
                //UnityEngine.Rect rect = new UnityEngine.Rect(result0.rect.position * size - size * 0.5f, result0.rect.size * size);
                
                NRDebugger.Info(cvRect.ToString());

                Cv2.Rectangle(targetmat, cvRect, Scalar.Red, 3);

            }
        }
        var targetTextureNew = OpenCvSharp.Unity.MatToTexture(targetmat);
        
        // Create a gameobject that we can apply our texture to
        quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Renderer quadRenderer = quad.GetComponent<Renderer>() as Renderer;
        //quadRenderer.material = new Material(Resources.Load<Shader>("Record/Shaders/CaptureScreen"));
        quadRenderer.material = new Material(Resources.Load<Material>("Record/Shaders/UVCTex"));

        var headTran = NRSessionManager.Instance.NRHMDPoseTracker.centerAnchor;
        quad.name = "picture";
        quad.transform.localPosition = headTran.position + headTran.forward * 3f;
        quad.transform.forward = headTran.forward;
        quad.transform.localScale = new Vector3(1.6f, 0.9f, 0);
        quadRenderer.material.SetTexture("_MainTex", targetTextureNew);
        quad.SetActive(true);*/

        int finalResult = 0;
        int count = 0;
        var sizeImg = captureView.rectTransform.rect.size;
        for (int i = 0; i < 10; i++)
        {
            if (results[i].score >= scoreThreshold)
            {
                var lbt = labelText[i].transform as RectTransform;
                var position = results[i].rect.position * sizeImg - sizeImg * 0.5f; 
                var sizeDelta = results[i].rect.size * sizeImg;
                count++;
                if (GetLabelName(results[i].classID).Contains("OK USB") || GetLabelName(results[i].classID).Contains("OK CAP") || GetLabelName(results[i].classID).Contains("OK PIN") || GetLabelName(results[i].classID).Contains("OK LED"))
                {
                    Setframe(framesGreen[i], results[i], sizeImg);
                    labelText[i].text = " " + $"{GetLabelName(results[i].classID)} : {(int)(results[i].score * 100)}%";
                    labelText[i].color = Color.green;
                    lbt.anchoredPosition = new Vector2(position.x, position.y);
                    lbt.sizeDelta = sizeDelta;
                    labelText[i].gameObject.SetActive(true); 
                }
                else
                {
                    Setframe(framesRed[i], results[i], sizeImg);
                    labelText[i].text = " " + $"{GetLabelName(results[i].classID)} : {(int)(results[i].score * 100)}%";
                    labelText[i].color = Color.red;
                    lbt.anchoredPosition = new Vector2(position.x, position.y);
                    lbt.sizeDelta = sizeDelta;
                    labelText[i].gameObject.SetActive(true);
                }

                if (results[i].classID == 1 || results[i].classID == 2 || results[i].classID == 5 || results[i].classID == 7)
                {
                    finalResult++;
                }
            }
        }
        if (count == 4)
        {
            if (finalResult != 0)
            {
                showResult.text = "NG";
                showResult.color = Color.red;
            }
            else
            {
                showResult.text = "OK";
                showResult.color = Color.green;
            }
        }
        else
        {
            showResult.text = "Try again!";
            showResult.color = Color.white;
        }
        captureView.gameObject.SetActive(true);
        frameFit.gameObject.SetActive(true);
        showResult.gameObject.SetActive(true);
        resultLabel.gameObject.SetActive(true);
        // Release camera resource after capture the photo.
        this.Close();
    }
    void Setframe(Text frame, SSD.Result result, Vector2 size)
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
    /// <summary> Closes this object. </summary>
    void Close()
    {
        if (m_PhotoCaptureObject == null)
        {
            NRDebugger.Error("The NRPhotoCapture has not been created.");
            return;
        }
        // Deactivate our camera
        m_PhotoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
    }

    /// <summary> Executes the 'stopped photo mode' action. </summary>
    /// <param name="result"> The result.</param>
    void OnStoppedPhotoMode(NRPhotoCapture.PhotoCaptureResult result)
    {
        if (m_PhotoCaptureObject == null)
        {
            NRDebugger.Error("The NRPhotoCapture has not been created.");
            return;
        }
        // Shutdown our photo capture resource
        m_PhotoCaptureObject.Dispose();
        m_PhotoCaptureObject = null;
        isOnPhotoProcess = false;
    }

    /// <summary> Executes the 'destroy' action. </summary>
    void OnDestroy()
    {
        if (m_PhotoCaptureObject == null)
        {
            return;
        }
        // Shutdown our photo capture resource
        m_PhotoCaptureObject?.Dispose();
    }
}
