//DOCUMENTED CODE
using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

public class CameraScript : NetworkBehaviour
{

    [SerializeField] private PlayerInputHandler playerInput;
    private Vector2 lookInput;
    private bool isAiming;
    public float sensX, sensY = 50f;
    private float xRotation = 0f;
    private float yRotation = 0f;
    [SerializeField] private Transform orientation;
    [SerializeField] private Transform camHolder;
    [SerializeField] Camera cam;
    [SerializeField] float hipFOV = 75f;
    [SerializeField] float adsFOV = 60f;
    [SerializeField] float zoomSpeed = 12f;
    [SerializeField] private Transform gunHolder;
    public bool invertCamera = false;
    [SerializeField] private Transform headBone;
    private NetworkVariable<float> netYRotation = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    private NetworkVariable<float> netXRotation = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    public PauseManager pauseManager;
    public override void OnNetworkSpawn()
    {   // Disable the camera and audio listener for non-owners
        cam = GetComponentInChildren<Camera>();
        AudioListener audioListener = GetComponentInChildren<AudioListener>();
        if (!IsOwner)
        {
            if (cam != null) cam.enabled = false;
            if (audioListener != null) audioListener.enabled = false;
            return;
        }
        UnityEngine.Cursor.lockState = CursorLockMode.Locked; // Lock and hide the cursor for the local player
        UnityEngine.Cursor.visible = false;
    }

    private void Update()
    {   
        if (pauseManager.isPaused) return; // Don't process input or update camera when the game is paused
        if (IsOwner)
        {   // Only the owner should process input and update field of view
            float deltaYRotation = lookInput.x * sensX * Time.deltaTime;
            yRotation += invertCamera ? (-1f * deltaYRotation) : deltaYRotation ;
            float deltaXRotation = lookInput.y * sensY * Time.deltaTime;
            xRotation -= invertCamera ? (-1f) * deltaXRotation : deltaXRotation;
            xRotation = Mathf.Clamp(xRotation, -70f, 70f);
            netYRotation.Value = yRotation; //sync rotation across the network
            netXRotation.Value = xRotation;
            //ternary operator to determine target FOV in a single line
            float targetFOV = isAiming ? adsFOV : hipFOV;
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * zoomSpeed);
        }
        // Apply the networked rotation values to the camera and gun holders for all clients
        transform.rotation = Quaternion.Euler(0f, netYRotation.Value, 0f);
        camHolder.localRotation = Quaternion.Euler(netXRotation.Value, 0f, 0f);
        gunHolder.localRotation = Quaternion.Euler(netXRotation.Value, 0f, 0f);
    }

    private void LateUpdate()
    {   // Only the owner should take in inputs and update the camera position based on the head bone
        if (!IsOwner) return;
        lookInput = playerInput.LookInput;
        isAiming = playerInput.AimInput;
        Vector3 targetPos = headBone.position;
        camHolder.position = targetPos; 
    }


}
