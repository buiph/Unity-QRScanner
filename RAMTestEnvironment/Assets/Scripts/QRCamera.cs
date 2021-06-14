using System.Collections;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Android;
using ZXing;

namespace Temp
{
    //THIS CLASS IS USING THE DEVICE'S REAR CAMERA TO RECORD AND DISPLAY AN IMAGE ON SCREEN AND DECODING ANY QR CODE IN IT 
    public class QRCamera : MonoBehaviour, ICamera
    {
        WebCamDevice[] devices;
        private WebCamTexture cameraTexture; // The texture to draw the camera live feed onto
        private Texture defaultBackground; // The default background when the camera is not on
        public RawImage cameraImage; // The image to display the textures on screen
        public AspectRatioFitter fit;

        //FROM ZXING UNITY DEMO
        private Thread qrThread;
        private Color32[] camTextureColor;
        private int width, height;
        private bool isQuit;

        public Button qRButton;
        public Button returnButton;
        public GameObject scanFrame;
        internal string resultText;

        #region For Testing Only
        public GameObject decodedPanel;
        #endregion

        // Use this for initialization
        void Start()
        {
            qRButton.gameObject.SetActive(true);
            returnButton.gameObject.SetActive(false);
            scanFrame.SetActive(false);

            isQuit = false;
            defaultBackground = cameraImage.texture;
            cameraImage.gameObject.SetActive(false); //Only show the cameraImage when in QR scanning mode

            qrThread = new Thread(DecodeQR); //Start a thread for decoding the QR
        }
        
        // Update is called once per frame
        void Update()
        {            
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
        /// Gets the camera feed and assigns it to a texture
        /// </summary>
        public void GetCameraTexture(WebCamTexture texture)
        {
            devices = WebCamTexture.devices;

            //Check if the device have at least 1 camera
            if (devices.Length == 0)
            {
                Debug.Log("No camera detected");
                return;
            }

            //Set the provided texture to the back camera feed
            for (int i = 0; i < devices.Length; i++)
            {
                var curr = devices[i];

                if (!curr.isFrontFacing)
                {
                    texture = new WebCamTexture(curr.name, Screen.width, Screen.height);
                    break;
                }
            }
            
            //Check if texture is valid
            if (texture == null)
            {
                Debug.Log("No back camera detected");
                return;
            }
        }

        /// <summary>
        /// FROM ZXING UNITY DEMO
        /// This runs whenever QR mode is entered
        /// <summary>
        public void ActivateQR()
        {
            // Check permission if it has not been granted already
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                Permission.RequestUserPermission(Permission.Camera);
            }

            // Set the cameraTexure to  the texture recorded by the device's camera
            // if it has not been done already 
            if (cameraTexture == null)
            {
                GetCameraTexture(cameraTexture);
            }

            if (cameraTexture != null)
            {
                Screen.sleepTimeout = SleepTimeout.NeverSleep; // Set the device's screen to never go to sleep

                cameraTexture.Play();
                width = cameraTexture.width;
                height = cameraTexture.height;
                cameraImage.texture = cameraTexture; // Set the texture displayed on screen to the live cameraTexture
                cameraImage.gameObject.SetActive(true);

                if (!qrThread.IsAlive) //Start the thread if it has not been started
                {
                    qrThread.Start();
                    Debug.Log("Thread started");
                }

                qRButton.gameObject.SetActive(false);
                returnButton.gameObject.SetActive(true);
                scanFrame.SetActive(true);
            }
        }

        /// <summary>
        /// FROM ZXING UNITY DEMO
        /// This runs whenever QR mode is exited
        /// <summary>
        public void DeactivateQR()
        {
            if (cameraTexture != null)
            {
                if (cameraImage)
                {
                    Screen.sleepTimeout = SleepTimeout.SystemSetting; // Set the device's aleep time to the default system setting

                    cameraImage.gameObject.SetActive(false);
                    cameraImage.texture = defaultBackground;

                    qRButton.gameObject.SetActive(true);
                    returnButton.gameObject.SetActive(false);
                    scanFrame.SetActive(false);
                }
                cameraTexture.Pause();
            }
        }

        void OnDestroy()
        {
            qrThread.Abort();
            if (cameraTexture != null)
            {
                cameraTexture.Stop();
            }
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
}
