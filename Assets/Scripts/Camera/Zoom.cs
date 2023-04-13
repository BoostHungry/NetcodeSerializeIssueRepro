using System;
using UnityEngine;
using UnityEngine.Serialization;

public class Zoom : MonoBehaviour {
    
    public float MaxZoom = 1;
    public float MinZoom = 2500;
    public float Sensitivity = 1;
    public float SpeedMultiplier = 1;
    
    private Camera _camera;
    private float _targetZoom;
    
    
    private void Start()
    {
        _camera = Camera.main;
        _targetZoom = _camera.orthographicSize;
    }
    private void Update() {
        if (MathF.Abs(Input.mouseScrollDelta.y) > 0.1f) {
            float zoomAmount = 1 + (_camera.orthographicSize * 0.1f);
            _targetZoom -= Input.mouseScrollDelta.y * zoomAmount * Sensitivity;
            _targetZoom = Mathf.Clamp(_targetZoom, MaxZoom, MinZoom);
        }

        // Let the speed be relative to the current zoom level
        float cameraSpeed = SpeedMultiplier * _camera.orthographicSize;
        
        float newSize = Mathf.MoveTowards(_camera.orthographicSize, _targetZoom, cameraSpeed * Time.deltaTime);
        _camera.orthographicSize = newSize;
    }
}