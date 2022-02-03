using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TensorFlowLite;
using NRKernal;
using NRKernal.Record;

public class FaceDetectScene : MonoBehaviour
{
    [SerializeField, FilePopup("*.tflite")] string fileName;
    [SerializeField] RawImage cameraView = null;
    [SerializeField] Text framePrefabRed = null;
    [SerializeField, Range(0f, 1f)] float scoreThreshold = 0.5f;
    [SerializeField] TextAsset labelMap = null;
    SSD ssd;

    Text[] framesRed;

    public string[] labels;

    private NRRGBCamTexture RGBCamTexture { get; set; }

    public Text textLabel;
    //public Text textInfo;
    Text[] text;

    // Start is called before the first frame update
    void Start()
    {
        string path = Path.Combine(Application.streamingAssetsPath, fileName);
        ssd = new SSD(path);

        RGBCamTexture = new NRRGBCamTexture();
        cameraView.texture = RGBCamTexture.GetTexture();
        RGBCamTexture.Play();

        // Init frames
        text = new Text[10];
        framesRed = new Text[10];
        var parent = cameraView.transform;
        for (int i = 0; i < framesRed.Length; i++)
        {
            framesRed[i] = Instantiate(framePrefabRed, new Vector3(0, 0, (float)1.5), Quaternion.identity, parent);
            text[i] = Instantiate(textLabel, new Vector3(0, 0, (float)1.5), Quaternion.identity, parent);
            framesRed[i].gameObject.SetActive(false);
            text[i].gameObject.SetActive(false);
        }

        // Labels
        labels = labelMap.text.Split('\n');
    }
    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < framesRed.Length; i++)
        {
            framesRed[i].gameObject.SetActive(false);
            text[i].gameObject.SetActive(false);
        }

        ssd.Invoke(RGBCamTexture.GetTexture());

        var results = ssd.GetResults();

        int count = 0;
        var size = cameraView.rectTransform.rect.size;
        for (int i = 0; i < framesRed.Length; i++)
        {
            if (results[i].score >= scoreThreshold)
            {
                count++;

                var lbt = text[i].transform as RectTransform;
                var position = results[i].rect.position * size - size * 0.5f;
                var sizeDelta = results[i].rect.size * size;
                SetFrame(framesRed[i], results[i], size);
                /*text[i].text = " " + $"{GetLabelName(results[i].classID)} : {(int)(results[i].score * 100)}%";
                text[i].color = Color.red;
                lbt.anchoredPosition = new Vector2(position.x, position.y);
                lbt.sizeDelta = sizeDelta;
                text[i].gameObject.SetActive(true);*/
            }
        }

        cameraView.material = ssd.transformMat;
        //textInfo.text = count.ToString();
    }
    void SetFrame(Text frame, SSD.Result result, Vector2 size)
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
        RGBCamTexture?.Stop();
        ssd?.Dispose();
        RGBCamTexture = null;
    }
}
