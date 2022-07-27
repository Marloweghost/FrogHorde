using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField]
    private float _mouseSensitivity = 3.0f;

    private float _rotationY;
    private float _rotationX;

    [SerializeField]
    private Transform _target;

    private Transform _modifiedTarget;

    [SerializeField]
    private float _distanceFromTarget = 3.0f;
    public float minDistance = 1f;
    public float maxDistance = 4f;
    public float collisionOffset = 0.2f;

    private Vector3 _currentRotation;
    private Vector3 _smoothVelocity = Vector3.zero;

    [SerializeField]
    private float _smoothTime = 0.2f;

    [SerializeField]
    private Vector2 _rotationXMinMax = new Vector2(-40, 40);

    [SerializeField]
    private LayerMask collisionLayer;

    private bool _isAiming = false;
    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * _mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * _mouseSensitivity;

        _rotationY += mouseX;
        _rotationX += mouseY;

        // Apply clamping for x rotation 
        _rotationX = Mathf.Clamp(_rotationX, _rotationXMinMax.x, _rotationXMinMax.y);

        Vector3 nextRotation = new Vector3(_rotationX, _rotationY);

        // Apply damping between rotation changes
        _currentRotation = Vector3.SmoothDamp(_currentRotation, nextRotation, ref _smoothVelocity, _smoothTime);
        transform.localEulerAngles = _currentRotation;


        // Simple camera collision
        Vector3 desiredCameraPosition = _target.position - transform.forward * maxDistance;
        if (Physics.Linecast(_target.position, desiredCameraPosition, out RaycastHit hit, collisionLayer))
        {
            _distanceFromTarget = Vector3.Distance(_target.position, hit.point) - collisionOffset;
        }
        else
        {
            _distanceFromTarget = maxDistance;
        }

        // Set position + offset
        if (_isAiming)
        {
            _distanceFromTarget = 0f;
            transform.position = _target.position + transform.up * 0.8f - transform.forward * _distanceFromTarget;
        }
        else
        {
            transform.position = _target.position - transform.forward * _distanceFromTarget;
        }
    }

    public void SetAiming(bool isAiming)
    {
        if (_isAiming == false && isAiming == true)
        {
            _isAiming = isAiming;
        }
        else if (_isAiming == true && isAiming == false)
        {
            _isAiming = isAiming;
        }
    }
}
