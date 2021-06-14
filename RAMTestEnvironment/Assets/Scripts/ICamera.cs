using UnityEngine;

namespace Temp
{
    public interface ICamera
    {
        /// <summary> 
        /// Gets the camera feed and assigns it to a texture
        /// </summary>
        /// <param name="cameraTexture"></param>
        void GetCameraTexture(WebCamTexture texture);
    }
}
