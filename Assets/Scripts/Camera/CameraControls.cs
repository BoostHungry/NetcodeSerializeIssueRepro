using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControls : MonoBehaviour
{
    public float PanSpeed = 3;

    public float WASDPanSpeed = 100;
    public float ShiftMultiplier = 5;
    public float CtrlMultiplier = 2;
    
    private Camera _camera;
    private Transform _transform;
    
    private Vector3 dragOrigin;
    private Vector3 startingPos;
    
    private void Start()
    {
        _camera = Camera.main;
        _transform = _camera.gameObject.transform;
    }

    private void LateUpdate()
    {
        if (Input.GetMouseButtonDown(2)) {
            //isDragging = true;
            startingPos = _transform.position;
            dragOrigin = _camera.ScreenToViewportPoint(Input.mousePosition);
        }

        if (Input.GetMouseButton(2)) {
            Vector3 pos = dragOrigin - _camera.ScreenToViewportPoint(Input.mousePosition);
            _transform.position = startingPos + (pos * (PanSpeed * _camera.orthographicSize));
        }

        handleWASD();
    }

    private void handleWASD() {
        Vector3 direction = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) {
            direction += Vector3.up;
        }
        if (Input.GetKey(KeyCode.A)) {
            direction += Vector3.left;
        }
        if (Input.GetKey(KeyCode.S)) {
            direction += Vector3.down;
        }
        if (Input.GetKey(KeyCode.D)) {
            direction += Vector3.right;
        }

        if (direction == Vector3.zero) {
            return;
        }
        direction = direction.normalized;

        float multiplier = WASDPanSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.LeftShift)) {
            multiplier *= ShiftMultiplier;
        } else if (Input.GetKey(KeyCode.LeftControl)) {
            multiplier /= 2;
        }
        
        _transform.position += direction * multiplier;
    }
}
