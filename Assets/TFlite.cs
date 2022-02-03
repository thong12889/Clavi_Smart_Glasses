using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TensorFlowLite;
using NRKernal;
using NRKernal.Record;

public class TFlite : MonoBehaviour
{
    [SerializeField, FilePopup("*.tflite")] string fileName;
    [SerializeField] RawImage cameraView = null;
    [SerializeField] Text framePrefab = null;
    [SerializeField, Range(0f, 1f)] float scoreThreshold = 0.5f;
    [SerializeField] TextAsset labelMap = null;
    public string[] labels;
    public Image imageP;
    public List<string> n; 
    public Text textLabel;
    SSD ssd;
    WebCamTexture webcamTexture;

    Text[] text;
    Text[] frames;

    // Start is called before the first frame update
    void Start()
    {

        string path = Path.Combine(Application.streamingAssetsPath, fileName);
        ssd = new SSD(path);

        string cameraName = WebCamUtil.FindName();
        webcamTexture = new WebCamTexture(cameraName, 1280, 720, 30);
        cameraView.texture = webcamTexture;
        webcamTexture.Play();

        text = new Text[10];
        frames = new Text[10];
        var parent = cameraView.transform;
        for (int i = 0; i < frames.Length; i++)
        {
            frames[i] = Instantiate(framePrefab, new Vector3(0, 0, 150f), Quaternion.identity, parent);
            text[i] = Instantiate(textLabel, new Vector3(0, 0, 150f), Quaternion.identity, parent);
            /*frames[i] = Instantiate(framePrefab, new Vector3(0, 0, 0), Quaternion.identity, parent);
            text[i] = Instantiate(textLabel, new Vector3(0, 0, 0), Quaternion.identity, parent);*/
            //frames[i] = Instantiate(framePrefab, new Vector3(cameraTransform.position.x, cameraTransform.position.y, cameraTransform.position.z + 10), cameraTransform.rotation, parent);
        }

        // Labels
        labels = labelMap.text.Split('\n');
    }
    void Update()
    {

        ssd.Invoke(cameraView.texture);

        var results = ssd.GetResults();

        n = new List<string>(); 
        var size = cameraView.rectTransform.rect.size;
        for (int i = 0; i < 10; i++)
        {
            
            SetFrame(frames[i], results[i], size, text[i]);
            if (results[i].score >= scoreThreshold)
            {
                Debug.Log(GetLabelName(results[i].classID));
                
                n.Add(results[i].classID.ToString());

            }
                
        }

        Debug.Log(n.Count());
        cameraView.material = ssd.transformMat;
        //cameraView.texture = ssd.inputTex;
        //textfps.text =  cameraTransform.forward.ToString() + canvas.transform.position.ToString();
    }

    void SetFrame(Text frame, SSD.Result result, Vector2 size, Text label)
    {
        /*if (result.score < scoreThreshold)
        {
            frame.gameObject.SetActive(false);
            label.gameObject.SetActive(false);
            return;
        }*/
        if (result.score >= scoreThreshold)
        {
            frame.gameObject.SetActive(true);
            label.gameObject.SetActive(true);

            //frame.text = $"{GetLabelName(result.classID)} : {(int)(result.score * 100)}%"; 
            var rt = frame.transform as RectTransform;
            var lt = label.transform as RectTransform;

            /*if (result.classID == 0)
            {
                label.color = Color.green;
                rt.anchoredPosition = result.rect.position * size - size * 0.5f;
                rt.sizeDelta = result.rect.size * size;
                label.text = " " + $"{GetLabelName(result.classID)} : {(int)(result.score * 100)}%";
                lt.anchoredPosition = new Vector2(rt.anchoredPosition.x, rt.anchoredPosition.y);
                lt.sizeDelta = rt.sizeDelta;
            }
            if (result.classID == 1)
            {
                label.color = Color.red;
                rt.anchoredPosition = result.rect.position * size - size * 0.5f;
                rt.sizeDelta = result.rect.size * size;
                label.text = " " + $"{GetLabelName(result.classID)} : {(int)(result.score * 100)}%";
                lt.anchoredPosition = new Vector2(rt.anchoredPosition.x, rt.anchoredPosition.y);
                lt.sizeDelta = rt.sizeDelta;
            }*/
            rt.anchoredPosition = result.rect.position * size - size * 0.5f; 
            rt.sizeDelta = result.rect.size * size;
            label.text = " " + $"{GetLabelName(result.classID)} : {(int)(result.score * 100)}%";
            lt.anchoredPosition = new Vector2(rt.anchoredPosition.x, rt.anchoredPosition.y);
            lt.sizeDelta = rt.sizeDelta;
        }
        else
        {
            frame.gameObject.SetActive(false);
            label.gameObject.SetActive(false);
        }
    }

    string GetLabelName(int id)
    {
        if (id < 0 || id >= labels.Length - 1)
        {
            return "?";
        }
        return labels[id + 1];
    }

}
