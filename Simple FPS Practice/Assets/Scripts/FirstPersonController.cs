
using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonController : MonoBehaviour
{
    //Settings
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float lookSpeed = 1f;
    [SerializeField] private float interactionDistance = 1.5f;
    
    //Input System
    private InputSystem_Actions playerControls;
    private Vector2 movementInput;
    private Vector2 lookInput;
    
    //Component
    private Rigidbody myRigidbody;
    [SerializeField] private Camera MyCamera;
    [SerializeField] private GameObject MyHand;
    
    //Tracking
    private float verticalRotation = 0f;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    
    private void Awake()
    {
        playerControls = new InputSystem_Actions();
        myRigidbody = GetComponent<Rigidbody>();
        
        playerControls.Enable();
    }
    
    void Start()
    {
        // Set the Input actions
        playerControls.Player.Move.performed += OnMovementPerformed;
        playerControls.Player.Move.canceled += OnMovementCanceled;
        
        playerControls.Player.Look.performed += OnLookPerformed;
        playerControls.Player.Look.canceled += OnLookCanceled;

        playerControls.Player.Interact.performed += Interact;
        
        playerControls.Player.Attack.performed += Shoot;
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    private void OnMovementPerformed(InputAction.CallbackContext context)
    {
        // Read the movement value when the action is performed
        movementInput = context.ReadValue<Vector2>();
    }
    
    private void OnMovementCanceled(InputAction.CallbackContext context)
    {
        // Reset movement when the action is canceled
        movementInput = Vector2.zero;
    }
    
    private void OnLookPerformed(InputAction.CallbackContext context)
    {
        // Read the movement value when the action is performed
        lookInput = context.ReadValue<Vector2>();
    }
    
    private void OnLookCanceled(InputAction.CallbackContext context)
    {
        // Reset movement when the action is canceled
        lookInput = Vector2.zero;
    }
    
    private void FixedUpdate()
    {
        // Apply movement in FixedUpdate for physics consistency
        if (movementInput != Vector2.zero) Move();
    }

    void Update()
    {
        if (lookInput != Vector2.zero) Look();
    }

    void Move()
    {
        Vector3 movement = new Vector3((movementInput.x * moveSpeed), 0, (movementInput.y * moveSpeed));
        transform.Translate(movement * Time.deltaTime, Space.Self);
    }

    void Look()
    {
        float mouseX = lookInput.x * lookSpeed * Time.deltaTime;
        float mouseY = lookInput.y * lookSpeed * Time.deltaTime;

        // Rotate the player (horizontal rotation)
        Vector3 yRotation = new Vector3(0, 1 * mouseX, 0);
        transform.Rotate(yRotation);

        // Rotate the camera (vertical rotation), with clamping
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -40f, 40f);

        MyCamera.transform.localEulerAngles = new Vector3(verticalRotation, 0f, 0f);
    }

    private void Interact(InputAction.CallbackContext context)
    {
       
        Vector3 rayStart = MyCamera.transform.position;
        Vector3 rayDirection = MyCamera.transform.forward;
        LayerMask rayLayer= LayerMask.GetMask("Interactable");
        
        //Cast a ray to see if we hit something interactable
        Ray raycast = new Ray(rayStart, rayDirection);
        RaycastHit hitInteractable;
        
        if (Physics.Raycast(raycast, out hitInteractable, interactionDistance, rayLayer))
        {
            GameObject interactableObject = hitInteractable.collider.gameObject;
        //    interactableObject.GetComponent<Interactable>().Interact();
        }
        else
        {
            Debug.Log("No interactable");
        }
    }

    void Shoot(InputAction.CallbackContext context)
    {
        if (MyHand.transform.childCount == 0) return;
        
        GameObject gun =  MyHand.transform.GetChild(0).gameObject;
      //  gun.GetComponent<Gun>().Shoot();
    }
    
}
