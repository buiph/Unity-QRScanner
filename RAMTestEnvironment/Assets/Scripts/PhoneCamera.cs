using System.Collections;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using ZXing;

//THIS CLASS IS USING THE DEVICE'S REAR CAMERA TO RECORD AND DISPLAY AN IMAGE ON SCREEN AND DECODING ANY QR CODE IN IT 
public class PhoneCamera : MonoBehaviour
{
    private bool camAvailable;
	private WebCamTexture cameraTexture;
	private Texture defaultBackground;
	public RawImage cameraImage;
	public AspectRatioFitter fit;

    //FROM ZXING UNITY DEMO
    private Thread qrThread;
    private Color32[] camTextureColor;
    private int width, height;
    private bool isQuit;

    public Button qRButton;
    public Button returnButton;
    public GameObject scanFrame;
    string resultText;

    #region For Testing Only
    public GameObject decodedPanel;
    #endregion

	// Use this for initialization
	void Start()
    {
        qRButton.gameObject.SetActive(true);
        returnButton.gameObject.SetActive(false);
        scanFrame.SetActive(false);

        #region For Testing Only
        decodedPanel.SetActive(false);
        #endregion

        isQuit = false;
		defaultBackground = cameraImage.texture;
        cameraImage.gameObject.SetActive(false); //Only show the cameraImage when in QR scanning mode
		WebCamDevice[] devices = WebCamTexture.devices;

        Screen.sleepTimeout = SleepTimeout.NeverSleep; // Set the device's screen to never go to sleep

        //Check if the device have at least 1 camera
		if (devices.Length == 0)
        {
            Debug.Log("No camera detected");
            camAvailable = false;
            return;
        }

        //Get the texture of the back camera to cameraTexture
		for (int i = 0; i < devices.Length; i++)
		{
			var curr = devices[i];

			if (!curr.isFrontFacing)
			{
				cameraTexture = new WebCamTexture(curr.name, Screen.width, Screen.height);
				break;
			}
		}
        
        //Check if cameraTexture is valid
		if (cameraTexture == null)
        {
            Debug.Log("No back camera detected");
            return;
        }
        else
        {
            camAvailable = true; // Set the camAvailable for future purposes.
        }

        //Start a thread for decoding the QR
        qrThread = new Thread(DecodeQR);
	}
	
	// Update is called once per frame
	void Update()
    {
        //Code for camAvailable, have no use currently
		if (!camAvailable)
        {
            return;
        }
        
        if (cameraTexture.isPlaying) // If phone is in QR scan mode
        {
            float ratio = (float)cameraTexture.width / (float)cameraTexture.height;
            fit.aspectRatio = ratio; // Set the aspect ratio

            float scaleY = cameraTexture.videoVerticallyMirrored ? -1f : 1f; // Find if the camera is mirrored or not
            cameraImage.rectTransform.localScale = new Vector3(1f, scaleY, 1f); // Swap the mirrored camera

            int orient = -cameraTexture.videoRotationAngle;
            cameraImage.rectTransform.localEulerAngles = new Vector3(0,0, orient); // rotate the camera to the new orientation

            if (camTextureColor == null)
            {
                camTextureColor = cameraTexture.GetPixels32();
            }

            #region For testing only
            decodedPanel.GetComponentInChildren<Text>().text = resultText;
            #endregion

            for (int i = 0; i < Input.touchCount; ++i)
            {
                if (Input.GetTouch(i).phase == TouchPhase.Began)
                {
                    cameraTexture.autoFocusPoint = new Vector3(0.5f, 0.5f);
                    StartCoroutine(SetFocusPointEnd(1f));
                }
            }
        }
	}

    /// <summary>
    /// This decodes the QR code detected in cameraTexture
    /// <summary>
    void DecodeQR()
    {
        // create a reader with a custom luminance source
        var barcodeReader = new BarcodeReader { AutoRotate = false, TryHarder = false };

        while (!isQuit)
        {
            try
            {
                // decode the current frame
                var result = barcodeReader.Decode(camTextureColor, width, height);
                if (result != null)
                {
                    #region For testing only
                    resultText = result.Text;
                    #endregion
                }

                // Sleep a little bit and set the signal to get the next frame
                Thread.Sleep(200);
                camTextureColor = null;
            }
            catch
            { }
        }
    }

    /// <summary>
    /// FROM ZXING UNITY DEMO
    /// This runs whenever the phone camera is enabled
    /// <summary>
    public void OnEnable()
    {
        if (cameraTexture != null)
        {
            cameraTexture.Play();
            width = cameraTexture.width;
            height = cameraTexture.height;
            cameraImage.texture = cameraTexture; // Set the texture
            cameraImage.gameObject.SetActive(true);

            if (!qrThread.IsAlive) //Start the thread if it has not been started
            {
                qrThread.Start();
                Debug.Log("Thread started");
            }

            qRButton.gameObject.SetActive(false);
            returnButton.gameObject.SetActive(true);
            scanFrame.SetActive(true);

            #region For testing only
            decodedPanel.SetActive(true);
            #endregion
        }
    }

    /// <summary>
    /// FROM ZXING UNITY DEMO
    /// This runs whenever the phone camera is disabled
    /// <summary>
    public void OnDisable()
    {
        if (cameraTexture != null)
        {
            if (cameraImage)
            {
                cameraImage.gameObject.SetActive(false);
                cameraImage.texture = defaultBackground;

                qRButton.gameObject.SetActive(true);
                returnButton.gameObject.SetActive(false);
                scanFrame.SetActive(false);

                #region For testing only
                decodedPanel.SetActive(false);
                #endregion
            }
            cameraTexture.Pause();
        }
    }

    void OnDestroy()
    {
        qrThread.Abort();
        cameraTexture.Stop();
    }

    // It's better to stop the thread by itself rather than abort it.
    void OnApplicationQuit()
    {
        isQuit = true;
        Screen.sleepTimeout = SleepTimeout.SystemSetting; // Set the device's aleep time to the default system setting
        Application.Quit();
    }

    public IEnumerator SetFocusPointEnd(float time)
    {
        yield return new WaitForSeconds(time);
        cameraTexture.autoFocusPoint = new Vector3(0.5f, 0.5f);
    }
}
