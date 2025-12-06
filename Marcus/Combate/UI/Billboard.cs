using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera _mainCamera;

    void Start()
    {
        _mainCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (_mainCamera != null)
        {
            // Alinha a frente do objeto com a frente da câmera
            transform.forward = _mainCamera.transform.forward;
        }
    }
}