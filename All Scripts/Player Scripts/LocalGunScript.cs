using System;
using System.Globalization;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Events;

public class LocalGunScript : MonoBehaviour
{
    [SerializeField] private Transform hipPos;
    [SerializeField] private Transform adsPos;
    private PlayerInputHandler playerInput;
    private Transform gunModel;
    [SerializeField] private Transform worldHipPos;
    [SerializeField] private Transform worldADSPos;
    [SerializeField] private Transform gunHolder;
    public GameObject tracerPrefab;
    public float tracerDuration = 0.1f;
    public UnityEvent OnFire;


    private void Awake()
    {
        hipPos = transform.Find("HipPos");
        adsPos = transform.Find("ADSPos");
        gunModel = transform.Find("Gun Model");
        playerInput = GetComponentInParent<PlayerInputHandler>();
    }

    private void LateUpdate()
    {
        if (playerInput == null) return;
        bool isAiming = playerInput.isAiming.Value;
        bool shootPressed = playerInput.ShootInput; 
        Vector3 gunPos = isAiming ? adsPos.localPosition : hipPos.localPosition;
        Vector3 worldGunPos = isAiming ? worldADSPos.position : worldHipPos.position;

        gunModel.localPosition = Vector3.Lerp(gunModel.localPosition, gunPos, Time.deltaTime * 15f);
        gunHolder.position = Vector3.Lerp(gunHolder.position, worldGunPos, Time.deltaTime * 15f);
    }

}
