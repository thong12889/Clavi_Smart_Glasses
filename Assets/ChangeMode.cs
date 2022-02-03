using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Button))]
public class ChangeMode : MonoBehaviour
{
    public string sceneName = "";
    public LoadSceneMode mode = LoadSceneMode.Single;

    void OnEnable()
    {
        GetComponent<Button>().onClick.AddListener(OnButtonClick);
    }

    void OnDisable()
    {
        GetComponent<Button>().onClick.RemoveListener(OnButtonClick);
    }

    void OnButtonClick()
    {
        SceneManager.LoadScene(sceneName, mode);
    }
}
