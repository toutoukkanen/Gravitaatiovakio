using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    // Start is called before the first frame update
    private GameObject _player;
    private Transform _enemyTransform;
    private GameObject _enemy;
    private Camera _mainCamera;
    private readonly Vector3 _offset = new Vector3(0,0,-10);

    [SerializeField] private float zoomInSpeed = 1f;
    [SerializeField] private float zoomOutSpeed = 1f;

    [SerializeField] private float zoomInTimer = 3f;
    private float currentZoomInTimer = 0f;

    [SerializeField] private float maxCameraSize = 25f;
    
    private float _initialCameraSize;
    
    public float followSpeed = 1f;

    public Vector3 recoil = new Vector3();
    
    void Start()
    {
        _player = GameObject.FindWithTag("Player");
        
        _enemy = GameObject.FindWithTag("Enemy");

        _mainCamera = Camera.main;

        _initialCameraSize = _mainCamera.orthographicSize;

        _enemyTransform = _enemy.transform;
    }

    // Update is called once per frame
    void Update()
    {
        var orthographicSize = _mainCamera.orthographicSize;
        
        // Zoom out if enemy is not seen by the camera and zoom in if it is
        var viewPos = _mainCamera.WorldToViewportPoint(_enemyTransform.position);
        
        if (viewPos.x > 0 && viewPos.x < 1 && viewPos.y > 0 && viewPos.y < 1)  // If on screen, zoom in
        {
            if(currentZoomInTimer > zoomInTimer)
                _mainCamera.orthographicSize = Mathf.Lerp(orthographicSize, _initialCameraSize, Time.deltaTime * zoomInSpeed);
            else
                currentZoomInTimer += Time.deltaTime;
            
        }
        else if(_mainCamera.orthographicSize < maxCameraSize) // Zoom out until max camera size is reached
        {
            _mainCamera.orthographicSize = Mathf.Lerp(orthographicSize, orthographicSize + 1,
                Time.deltaTime * zoomOutSpeed);

            if (currentZoomInTimer > zoomInTimer)
                currentZoomInTimer = 0f;
        }
        
        // Move the  camera
        var playerPos = _player.transform.position;
        
        var desiredPosition = new Vector3(playerPos.x, playerPos.y, playerPos.z);
        desiredPosition += _offset;
        
        // Add a recoil factor
        desiredPosition += recoil;
        
        recoil = Vector3.Lerp(recoil, Vector3.zero, Time.deltaTime * 10); // Reduce recoil
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * followSpeed);

    }

   //Vector3 CalculateRecoil()
   //{
   //    
   //    
   //    
   //}
}
