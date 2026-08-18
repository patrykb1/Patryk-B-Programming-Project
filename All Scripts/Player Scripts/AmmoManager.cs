//DOCUMENTED CODE
using UnityEngine;
using System;

public class AmmoManager : MonoBehaviour
{
    public int maxAmmo = 15;
    public int currentAmmo;
    private Transform player;
    public event Action<int> OnAmmoChanged;
    public event Action OnReload;
    public GunScript gunScript;

    public void SetPlayer(Transform playerTransform)
    { // Set the player reference and initialize ammo
        player = playerTransform;
        player.GetComponent<PlayerUI>().SetAmmoManager(this);
        currentAmmo = maxAmmo;
    }

    public void GunFired()
    {   // Decrease ammo count and notify UI when the gun is fired
        currentAmmo--;
        OnAmmoChanged?.Invoke(currentAmmo);
    }
    public void Reload()
    {   // Start the reload process, disable shooting, and notify UI
        gunScript.canShoot = false;
        OnReload?.Invoke();
        Invoke(nameof(FinishReload), 2f);
    }
    private void FinishReload()
    {   // Refill ammo, enable shooting, and notify UI when reload is complete
        currentAmmo = maxAmmo;
        gunScript.canShoot = true;
        OnAmmoChanged?.Invoke(currentAmmo);
    }
}
