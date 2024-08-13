using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Capture : MonoBehaviour
{
    [SerializeField] private Camera Cam;
    private void Start()
    {
        GameObject.Find("Canvas/SceneCapture/SceneCaptureButton").GetComponent<Button>().onClick.AddListener(CamCapture);    
    }

    public void CamCapture()
    {
        Cam.gameObject.SetActive(true);

        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = Cam.targetTexture;

        Cam.Render();

        Texture2D Image = new Texture2D(Cam.targetTexture.width, Cam.targetTexture.height);
        Image.ReadPixels(new Rect(0, 0, Cam.targetTexture.width, Cam.targetTexture.height), 0, 0);
        Image.Apply();
        RenderTexture.active = currentRT;

        var Bytes = Image.EncodeToPNG();
        Destroy(Image);

        string name = string.Format("{0}_{1}_{2}_{3}.png", Application.productName, SceneManager.GetActiveScene().name, NetworkManager.Singleton.LocalClientId, System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
        Debug.Log("Permission result: " + NativeGallery.SaveImageToGallery(Bytes, Application.productName + " Captures", name));

        Cam.gameObject.SetActive(false);
    }

}
