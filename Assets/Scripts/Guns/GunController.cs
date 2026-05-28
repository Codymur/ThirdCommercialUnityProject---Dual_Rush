using UnityEngine;
using CartoonFX;
using System.Collections;
using System.Collections.Generic;

public class GunController : MonoBehaviour
{
    public float damage = 10f;
    public float range = 100f;
    public float impactForce = 30f;

    public float fireRate = 15f;
    private float nextTimeToFire = 0f;

    public KeyCode firingKey;

    public Camera FPScam;

    // References
    public CameraRecoil CameraRecoilScript;
    private WeaponRecoil _gunRecoil;
    private InstantiateUselessMag _uselessMagScript;

    public GameObject DecalBulletHole;
    public GameObject impactEffect;

    public bool itsPistol = false;
    public bool itsUzi = false;

    public ParticleSystem muzzleFlash;
    private CFXR_Effect _cfxrEffect;

    public Animator GunAnimator;

    public bool LeftHanded = false;
    public bool RightHanded = false;

    private float pickupTime = -999f;
    public float pickupShootDelay = 0.2f;

    [Header("Shot Sounds")]
    public AudioSource shotAudioSource;
    public AudioClip[] shotSoundClips;
    public float shotVolumeMin = 0.9f;
    public float shotVolumeMax = 1.0f;
    public float shotPitchMin = 0.95f;
    public float shotPitchMax = 1.05f;
    private int lastShotSoundIndex = -1;

    [Header("Layer Detection")]
    public LayerMask shootableLayers;

    [Header("Decal Pool")]
    [SerializeField] private int decalPoolSize = 20;
    [SerializeField] private float decalLifetime = 60f;

    [Header("Impact Effect Pool")]
    [SerializeField] private int impactPoolSize = 10;

    // Decal pool
    private readonly Queue<GameObject> _decalPool = new Queue<GameObject>();
    private Transform _decalPoolParent;

    // Impact effect pool
    private readonly Queue<GameObject> _impactPool = new Queue<GameObject>();
    private Transform _impactPoolParent;

    public PerkSelectionUI PerkSelectionUIScript;

    private void Awake()
    {
        Transform root = transform.root;

        _decalPoolParent = new GameObject($"{gameObject.name}_DecalPool").transform;
        _decalPoolParent.SetParent(root);

        _impactPoolParent = new GameObject($"{gameObject.name}_ImpactPool").transform;
        _impactPoolParent.SetParent(root);

        for (int i = 0; i < decalPoolSize; i++)
            _decalPool.Enqueue(CreateDecal());

        for (int i = 0; i < impactPoolSize; i++)
            _impactPool.Enqueue(CreateImpact());
    }

    private void Start()
    {
        _uselessMagScript = GetComponent<InstantiateUselessMag>();
        GunAnimator = GetComponent<Animator>();
        _cfxrEffect = muzzleFlash.GetComponent<CFXR_Effect>();
        muzzleFlash.gameObject.SetActive(false);
    }

    /// <summary>Caches handedness and WeaponRecoil when the gun is (re)enabled.</summary>
    private void OnEnable()
    {
        LeftHanded = false;
        RightHanded = false;
        _gunRecoil = null;

        if (transform.parent == null) return;

        string parentName = transform.parent.name;
        if (parentName == "LeftGunForKnowingJustRotationAndPosition")
            LeftHanded = true;
        else if (parentName == "RightGunForKnowingJustRotationAndPosition")
            RightHanded = true;

        _gunRecoil = transform.parent.GetComponentInParent<WeaponRecoil>();
    }

    private void Update()
    {
        if (transform.parent == null) return;
        if (Time.time - pickupTime < pickupShootDelay) return;

        if (LeftHanded && Input.GetMouseButton(0) && Time.time >= nextTimeToFire && !PerkSelectionUIScript.PerkChoosing)
        {
            nextTimeToFire = Time.time + 1f / fireRate;
            Shoot();
        }
        else if (RightHanded && Input.GetMouseButton(1) && Time.time >= nextTimeToFire && !PerkSelectionUIScript.PerkChoosing)
        {
            nextTimeToFire = Time.time + 1f / fireRate;
            Shoot();
        }
    }

    private void Shoot()
    {
        _uselessMagScript.InstantiatingBullet();
        GunAnimator.SetTrigger("Shoot");

        muzzleFlash.gameObject.SetActive(true);
        _cfxrEffect.ResetState();
        muzzleFlash.Stop();
        muzzleFlash.Play();

        PlayShotSound();

        if (_gunRecoil != null)
        {
            if (itsPistol) _gunRecoil.Fire();
            else if (itsUzi) _gunRecoil.FireUziGun();
        }

        CameraRecoilScript.Fire();

        if (Physics.Raycast(FPScam.transform.position, FPScam.transform.forward, out RaycastHit hit, range, shootableLayers))
        {
            Target target = hit.transform.GetComponent<Target>();

            if (target != null)
            {
                target.TakeDamage(damage, FPScam.transform.forward);
            }

            if (hit.rigidbody != null)
            {
                hit.rigidbody.AddForce(-hit.normal * impactForce);
            }

            if (target == null)
            {
                SpawnImpact(hit.point);
                SpawnDecal(hit.point, hit.normal, hit.transform);
            }
        }
    }

    // -------------------------------------------------------------------------
    // Decal pool
    // -------------------------------------------------------------------------

    private GameObject CreateDecal()
    {
        GameObject decal = Instantiate(DecalBulletHole, _decalPoolParent);
        decal.SetActive(false);
        return decal;
    }

    private void SpawnDecal(Vector3 point, Vector3 normal, Transform surface)
    {
        GameObject decal = _decalPool.Count > 0 ? _decalPool.Dequeue() : CreateDecal();

        Quaternion rot = Quaternion.LookRotation(-normal)
                         * Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

        decal.transform.SetPositionAndRotation(point + normal * 0.01f, rot);
        decal.transform.SetParent(surface);
        decal.SetActive(true);

        StartCoroutine(ReturnDecalToPool(decal, decalLifetime));
    }

    private IEnumerator ReturnDecalToPool(GameObject decal, float delay)
    {
        yield return new WaitForSeconds(delay);

        // The decal's parent surface may have been destroyed during the wait,
        // which takes the decal with it. Skip pooling if the reference is gone.
        if (decal == null) yield break;

        decal.SetActive(false);
        decal.transform.SetParent(_decalPoolParent);
        _decalPool.Enqueue(decal);
    }

    // -------------------------------------------------------------------------
    // Impact effect pool
    // -------------------------------------------------------------------------

    private GameObject CreateImpact()
    {
        GameObject impact = Instantiate(impactEffect, _impactPoolParent);
        impact.SetActive(false);
        return impact;
    }

    private void SpawnImpact(Vector3 point)
    {
        GameObject impact = _impactPool.Count > 0 ? _impactPool.Dequeue() : CreateImpact();

        impact.transform.SetPositionAndRotation(point, transform.rotation);
        impact.transform.SetParent(_impactPoolParent);
        impact.SetActive(true);

        ParticleSystem ps = impact.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play();
            StartCoroutine(ReturnImpactToPool(impact, ps.main.duration + ps.main.startLifetime.constantMax));
        }
        else
        {
            // Fallback: return after a fixed delay if no ParticleSystem is found.
            StartCoroutine(ReturnImpactToPool(impact, 3f));
        }
    }

    private IEnumerator ReturnImpactToPool(GameObject impact, float delay)
    {
        yield return new WaitForSeconds(delay);
        impact.SetActive(false);
        _impactPool.Enqueue(impact);
    }

    // -------------------------------------------------------------------------
    // Audio
    // -------------------------------------------------------------------------

    private void PlayShotSound()
    {
        if (shotSoundClips == null || shotSoundClips.Length == 0) return;

        int index;
        do
        {
            index = Random.Range(0, shotSoundClips.Length);
        } while (index == lastShotSoundIndex && shotSoundClips.Length > 1);

        lastShotSoundIndex = index;

        shotAudioSource.volume = Random.Range(shotVolumeMin, shotVolumeMax);
        shotAudioSource.pitch = Random.Range(shotPitchMin, shotPitchMax);
        shotAudioSource.PlayOneShot(shotSoundClips[index]);
    }

    public void NotifyPickedUp()
    {
        pickupTime = Time.time;
    }
}
