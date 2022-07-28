using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Attach settings")]
    public float attachCheckRadius = 0.52f;
    public float attachCooldown = 1f;

    [Header("Jump settings")]
    public float minJumpForce = 10f;
    public float maxJumpForce = 50f;
    public float jumpChargeSpeed = 10f;

    [Header("Clap settings")]
    public float clapExplosionRadius = 5f;
    public float clapExplosionForce = 25f;
    public float clapCooldown = 2f;

    [Header("Stun settings")]
    public float stunThreshold = 50f;
    public float stunDuration = 2f;

    [Header("Hookshot settings")]
    public float hookshotMaxRange = 25f;
    public float hookSpeed = 8f;
    public float hookPullForce = 10f;
    public float hookPullPlayerForce = 10f;

    [Header("Misc")]
    public float midAirRotationTime = 1.5f;
    public float trajectoryLength = 3f;

    private Collider[] _attachColliders = new Collider[10];

    private float _defaultRotationX;
    private float _defaultRotationZ;
    private float _currentRotationX;
    private float _currentRotationZ;
    private bool _hookshotDestinationReached;

    // Refs init
    [Header("References")]
    [SerializeField] 
    private LayerMask _attachMask;
    private Rigidbody _rb;
    [SerializeField]
    private Camera _camera;
    private CameraController _cameraController;
    [SerializeField]
    private GameObject _trajArrow;
    [SerializeField]
    private GameObject _hookPrefab;
    [SerializeField]
    private Transform _hookStart;
    [SerializeField]
    private Transform _hookEnd;
    private GameObject _hookshotInstance;
    private HookshotHandler _hookshotHandler;
    private LineRenderer _lr;
    [SerializeField]
    private GameObject _stunEffectGameObject;
    private ParticleSystem _stunEffect;


    // Counters
    [Header("Timers [READ ONLY]")]
    [SerializeField]
    private float currentJumpForce = 0f;
    [SerializeField]
    private float attachCooldownTimer = 0f;
    [SerializeField]
    private float rotationTimer = 0f;
    [SerializeField]
    private float clapCooldownTimer = 0f;
    [SerializeField]
    private float stunDurationTimer = 0f;

    #region State machine
    // State machine init
    private enum State
    {
        Flying,
        Attached,
        Aiming,
        FlyingOnHookshot,
        Hookshotting,
        Stunned,
        Dead
    }
    
    private State _currentState;

    /// <summary>
    /// Изменяет текущее состояние на новое состояние, определённое параметром newState
    /// </summary>
    /// <param name="newState"></param>
    private void ChangeState(State newState)
    {
        if (newState == _currentState)
        {
            //Debug.Log("State change to " + _currentState + " is failed!");
            return;
        }

        OnStateChanged(_currentState, newState);
        _currentState = newState;
    }

    /// <summary>
    /// Вызывается при каждой смене состояния
    /// </summary>
    /// <param name="oldState"></param>
    /// <param name="newState"></param>
    void OnStateChanged(State oldState, State newState)
    {
        Debug.Log(oldState + " -> " + newState);
        
        if (oldState == State.Flying && newState == State.Attached && !Input.GetMouseButton(1))
        {
            _cameraController.SetAiming(false);
        }

        if (oldState == State.Hookshotting && newState == State.Flying && _hookshotHandler.colliderFound == true)
        {
            _cameraController.SetAiming(false);
        } 

        if (newState == State.Stunned)
        {
            _cameraController.SetAiming(false);
            _stunEffect.Play();
            stunDurationTimer = 0f;
        }

        if (oldState == State.Stunned)
        {
            _stunEffect.Clear();
            _stunEffect.Stop();
        }

        if (!Input.GetMouseButton(1) && oldState == State.Hookshotting)
        {
            _cameraController.SetAiming(true);
        }

        if (newState == State.Flying && oldState == State.Hookshotting)
        {
            attachCooldownTimer = attachCooldown - 0.2f;
        }

        if (oldState == State.Hookshotting)
        {
            _lr.positionCount = 0;
        }

        // Смерть
        if (newState == State.Dead)
        {
            Destroy(gameObject);
            GameManager.instance.ReloadScene();
        }
    }
    #endregion

    void Start()
    {
        ChangeState(State.Flying);
        //_currentState = State.Flying;

        // Refs
        _rb = GetComponent<Rigidbody>();
        _cameraController = _camera.GetComponent<CameraController>();
        _lr = GetComponent<LineRenderer>();
        _stunEffect = _stunEffectGameObject.GetComponent<ParticleSystem>();

        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Стандартные углы для поворота в воздухе
        _defaultRotationX = transform.rotation.eulerAngles.x;
        _defaultRotationZ = transform.rotation.eulerAngles.z;

        // Передвинуть Tongue End на максимальную дистанцию крюка
        _hookEnd.transform.localPosition = transform.forward * hookshotMaxRange;

        _lr.positionCount = 0;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Death"))
        {
            ChangeState(State.Dead);
        }
    }

    void UpdateParticleTransform()
    {
        _stunEffectGameObject.transform.position = transform.position + Vector3.up * 0.7f;
        _stunEffectGameObject.transform.rotation = Quaternion.Euler(new Vector3(-90, 0, 0));
    }
    void UpdateTimers()
    {
        // Timers
        rotationTimer += Time.deltaTime;

        if (attachCooldownTimer <= attachCooldown)
        {
            attachCooldownTimer += Time.deltaTime;
        }
        if (clapCooldownTimer <= clapCooldown)
        {
            clapCooldownTimer += Time.deltaTime;
        }
        if (stunDurationTimer <= stunDuration)
        {
            stunDurationTimer += Time.deltaTime;
        }
    }
    void Update()
    {
        UpdateParticleTransform();

        UpdateTimers();

        // State machine
        switch (_currentState)
        {
            case State.Flying:
                HandleFlyingRotation();
                LookUpForAttaching();
                break;
            case State.Attached:
                HandleGroundRotation();
                HandleClap();
                HandleStartAiming();
                HandleJump();
                break;
            case State.Aiming:
                HandleAiming();
                HandleGroundRotation();
                break;
            case State.Hookshotting:
                HandleHookshotMovement();
                break;
            case State.FlyingOnHookshot:
                break;
            case State.Stunned:
                HandleStun();
                break;
            case State.Dead:
                break;
            default:
                break;
        }


        // Always return hookshot if exists (if needed)
        ReturnAndDestroyHookshot();
    }
    private void LateUpdate()
    {
        DrawHookshotLine();
    }


    #region Attaching...
    void LookUpForAttaching()
    {
        if (attachCooldownTimer < attachCooldown) return;

        if (Physics.CheckSphere(transform.position, attachCheckRadius, _attachMask))
        {
            //Debug.Log("Attaching...");
            Physics.OverlapSphereNonAlloc(transform.position, attachCheckRadius, _attachColliders, _attachMask);
            HandleAttach(_attachColliders[0]);
        }
    }
    void HandleAttach(Collider collider)
    {
        Vector3 _colliderNormal = (transform.position - collider.ClosestPoint(transform.position)).normalized;

        // Условие наклона поверхности > 90
        if (Vector3.Dot(Vector3.up, _colliderNormal) < 0) return;

        _rb.isKinematic = true;
        transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, _colliderNormal), _colliderNormal);

        ChangeState(State.Attached);
        //_currentState = State.Attached;
        rotationTimer = 0f;
    }
    #endregion

    void HandleJump()
    {
        // Удержание "Jump"
        if (Input.GetButton("Jump"))
        {
            if (currentJumpForce < maxJumpForce)
            {
                currentJumpForce += Time.deltaTime * jumpChargeSpeed;
            }
            else
            {
                currentJumpForce = maxJumpForce;
            }

            DrawTrajectory();
        }
        // Отпускание "Jump"
        else
        {
            DrawTrajectoryStop();

            if (currentJumpForce > 0f)
            {
                // Запуск
                currentJumpForce += minJumpForce;
                _rb.isKinematic = false;
                _rb.AddForce((transform.up/3 + transform.forward).normalized * currentJumpForce, ForceMode.Impulse);
                currentJumpForce = 0f;

                ChangeState(State.Flying);
                //_currentState = State.Flying;
                attachCooldownTimer = 0f;
                rotationTimer = 0f;
            }
        }
    }
    void HandleFlyingRotation()
    {
        if (rotationTimer < midAirRotationTime)
        {
            _currentRotationX = transform.rotation.eulerAngles.x;
            _currentRotationZ = transform.rotation.eulerAngles.z;

            transform.rotation = Quaternion.Lerp(transform.rotation,
                    Quaternion.Euler(new Vector3(_defaultRotationX, transform.rotation.eulerAngles.y, _defaultRotationZ)),
                    rotationTimer / midAirRotationTime);
        }
        else
        {
            transform.rotation = Quaternion.Euler(new Vector3(_defaultRotationX, transform.rotation.eulerAngles.y, _defaultRotationZ));
        }
    }
    void HandleGroundRotation()
    {
        transform.rotation = _camera.transform.rotation;
    }

    #region Trajectory
    void DrawTrajectory()
    {
        _trajArrow.SetActive(true);
    }
    void DrawTrajectoryStop()
    {
        _trajArrow.SetActive(false);
    }
    #endregion

    void HandleClap()
    {
        if (Input.GetMouseButtonDown(0) && clapCooldownTimer > clapCooldown)
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, clapExplosionRadius);

            foreach (Collider nearbyObject in colliders)
            {
                Rigidbody rigidbody = nearbyObject.GetComponent<Rigidbody>();
                if (rigidbody != null && rigidbody != _rb)
                {
                    rigidbody.AddExplosionForce(clapExplosionForce, transform.position, clapExplosionRadius);
                }
            }

            clapCooldownTimer = 0f;
        }
    }

    #region Aiming...
    void HandleStartAiming()
    {
        if (Input.GetMouseButton(1))
        {
            ChangeState(State.Aiming);
        }
    }
    void HandleAiming()
    {
        // TODO: Каждый кадр меняется переменная, сделать присваивание только при изменении значения
        // UPD: Вроде как сделано в функции камеры
        if (Input.GetMouseButton(1))
        {
            _cameraController.SetAiming(true);
            if (Input.GetMouseButtonDown(0) && (!_hookshotInstance || _hookshotInstance.activeSelf == false))
            { 
                // Launch hookshot
                LaunchHookshot();
            }
        }
        else
        {
            _cameraController.SetAiming(false);
            ChangeState(State.Attached);
        }
    }
    #endregion

    private void OnCollisionEnter(Collision collision)
    {
        
        if (collision.gameObject.TryGetComponent(out Rigidbody rigidbody))
        {
            Debug.Log("Velocity = " + (rigidbody.velocity - _rb.velocity).magnitude);
            if (rigidbody.velocity.magnitude - _rb.velocity.magnitude > 0 && (rigidbody.velocity - _rb.velocity).magnitude > stunThreshold)
            {
                _rb.AddForce((rigidbody.velocity - _rb.velocity) * 0.3f * rigidbody.mass, ForceMode.Impulse);
                stunDurationTimer = 0f;
                ChangeState(State.Stunned);
            } 
        }
    }
    private void HandleStun()
    {
        _rb.isKinematic = false;
        _rb.constraints = RigidbodyConstraints.None;
        
        if (stunDurationTimer > stunDuration)
        {
            _rb.constraints = RigidbodyConstraints.FreezeRotation;
            ChangeState(State.Flying);
            stunDurationTimer = 0f;
            rotationTimer = 0f;
        }
    }

    // Вызывается при нажатии ЛКМ при прицеливании
    private void LaunchHookshot()
    {
        if (!_hookshotInstance)
        {
            _hookshotInstance = Instantiate(_hookPrefab, _hookStart.position, Quaternion.identity);
        }
        else
        {
            _hookshotInstance.transform.position = _hookStart.position;
            _hookshotInstance.transform.rotation = Quaternion.identity;
        }
        _hookshotInstance.SetActive(true);

        if (!_hookshotHandler)
        {
            _hookshotHandler = _hookshotInstance.GetComponent<HookshotHandler>();
        }

        ChangeState(State.Hookshotting);
        _hookshotDestinationReached = false;

        //HandleHookshotMovement();
    }

    // State: Hookshotting, каждый кадр
    private void HandleHookshotMovement()
    {
        if (_hookshotHandler.colliderFound == true)
        {
            //Debug.Log("Collider found!");
            _hookshotDestinationReached = true;
            switch (_hookshotHandler.colliderLayerName)
            {
                case "DynEnv":
                    Debug.Log("Hit dynamic environment!");
                    Rigidbody colliderRigidbody = _hookshotHandler.colliderObject.GetComponent<Rigidbody>();
                    colliderRigidbody.velocity = Vector3.zero;
                    colliderRigidbody.AddForce((transform.position - _hookshotHandler.colliderObject.transform.position).normalized * hookPullForce, ForceMode.VelocityChange);
                    _hookshotHandler.colliderLayerName = "";
                    break;
                case "Ground":
                    Debug.Log("Hit ground!");
                    ChangeState(State.Flying);
                    _rb.isKinematic = false;
                    _rb.AddForce((_hookshotInstance.transform.position - transform.position).normalized * hookPullPlayerForce, ForceMode.VelocityChange);
                    _hookshotHandler.colliderLayerName = "";
                    break;
                default:
                    break;
            }
        }
        else if (Vector3.Distance(_hookshotInstance.transform.position, _hookEnd.position) < 0.1f)
        {
            _hookshotDestinationReached = true;
        }

        if (_hookshotDestinationReached && Vector3.Distance(_hookshotInstance.transform.position, _hookStart.position) < 0.1f)
        {
            Debug.Log("Returned!");
            ChangeState(State.Flying);
            _hookshotHandler.HookshotReset();
            _hookshotInstance.SetActive(false);
        }

    }

    private void ReturnAndDestroyHookshot()
    {
        if (_hookshotInstance && _hookshotInstance.activeSelf == true)
        {
            if (!_hookshotDestinationReached)
            {
                _hookshotInstance.transform.position = Vector3.MoveTowards(_hookshotInstance.transform.position, _hookEnd.position, hookSpeed * Time.deltaTime);
            }
            else
            {
                _hookshotInstance.transform.position = Vector3.MoveTowards(_hookshotInstance.transform.position, _hookStart.position, hookSpeed * Time.deltaTime);
            }

            if (_hookshotDestinationReached && Vector3.Distance(_hookshotInstance.transform.position, _hookStart.position) < 0.1f)
            {
                Debug.Log("Returned!");
                if (!(_currentState == State.FlyingOnHookshot))
                {
                    ChangeState(State.Flying);
                }
                _hookshotHandler.HookshotReset();
                _hookshotInstance.SetActive(false);
            }
        }
    }

    private void DrawHookshotLine()
    {
        if (!_hookshotInstance || _hookshotInstance.activeSelf == false)
        {
            if (_lr.positionCount == 0) return;
            _lr.positionCount = 0;
            return;
        }

        _lr.positionCount = 2;
        _lr.SetPosition(0, transform.position);
        _lr.SetPosition(1, _hookshotInstance.transform.position);
    }

}
