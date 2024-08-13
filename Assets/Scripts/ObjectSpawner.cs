using System;
using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine.XR.Interaction.Toolkit.Utilities;
using Random = UnityEngine.Random;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Behavior with an API for spawning objects from a given set of prefabs.
/// </summary>
public class ObjectSpawner : MonoBehaviour
{
    /// <summary>
    /// The active ARPlaneManager used in the example.
    /// </summary>
    public ARPlaneManager PlaneManager;

    [SerializeField]
    [Tooltip("The camera that objects will face when spawned. If not set, defaults to the main camera.")]
    Camera m_CameraToFace;

    /// <summary>
    /// The camera that objects will face when spawned. If not set, defaults to the <see cref="Camera.main"/> camera.
    /// </summary>
    public Camera cameraToFace
    {
        get
        {
            EnsureFacingCamera();
            return m_CameraToFace;
        }
        set => m_CameraToFace = value;
    }

    [SerializeField]
    [Tooltip("The list of prefabs available to spawn.")]
    List<GameObject> m_ObjectPrefabs = new List<GameObject>();

    [SerializeField]
    public GameObject m_GroundPrefab;

    /// <summary>
    /// The list of prefabs available to spawn.
    /// </summary>
    public List<GameObject> objectPrefabs
    {
        get => m_ObjectPrefabs;
        set => m_ObjectPrefabs = value;
    }

    [SerializeField]
    [Tooltip("Optional prefab to spawn for each spawned object. Use a prefab with the Destroy Self component to make " +
        "sure the visualization only lives temporarily.")]
    GameObject m_SpawnVisualizationPrefab;

    /// <summary>
    /// Optional prefab to spawn for each spawned object.
    /// </summary>
    /// <remarks>Use a prefab with <see cref="DestroySelf"/> to make sure the visualization only lives temporarily.</remarks>
    public GameObject spawnVisualizationPrefab
    {
        get => m_SpawnVisualizationPrefab;
        set => m_SpawnVisualizationPrefab = value;
    }

    [SerializeField]
    [Tooltip("The index of the prefab to spawn. If outside the range of the list, this behavior will select " +
        "a random object each time it spawns.")]
    public int m_SpawnOptionIndex = -1;

    /// <summary>
    /// The index of the prefab to spawn. If outside the range of <see cref="objectPrefabs"/>, this behavior will
    /// select a random object each time it spawns.
    /// </summary>
    /// <seealso cref="isSpawnOptionRandomized"/>
    public int spawnOptionIndex
    {
        get => m_SpawnOptionIndex;
        set => m_SpawnOptionIndex = value;
    }

    /// <summary>
    /// Whether this behavior will select a random object from <see cref="objectPrefabs"/> each time it spawns.
    /// </summary>
    /// <seealso cref="spawnOptionIndex"/>
    /// <seealso cref="RandomizeSpawnOption"/>
    public bool isSpawnOptionRandomized => m_SpawnOptionIndex < 0 || m_SpawnOptionIndex >= m_ObjectPrefabs.Count;

    [SerializeField]
    [Tooltip("Whether to only spawn an object if the spawn point is within view of the camera.")]
    bool m_OnlySpawnInView = true;

    /// <summary>
    /// Whether to only spawn an object if the spawn point is within view of the <see cref="cameraToFace"/>.
    /// </summary>
    public bool onlySpawnInView
    {
        get => m_OnlySpawnInView;
        set => m_OnlySpawnInView = value;
    }

    [SerializeField]
    [Tooltip("The size, in viewport units, of the periphery inside the viewport that will not be considered in view.")]
    float m_ViewportPeriphery = 0.15f;

    /// <summary>
    /// The size, in viewport units, of the periphery inside the viewport that will not be considered in view.
    /// </summary>
    public float viewportPeriphery
    {
        get => m_ViewportPeriphery;
        set => m_ViewportPeriphery = value;
    }

    [SerializeField]
    [Tooltip("When enabled, the object will be rotated about the y-axis when spawned by Spawn Angle Range, " +
        "in relation to the direction of the spawn point to the camera.")]
    bool m_ApplyRandomAngleAtSpawn = true;

    /// <summary>
    /// When enabled, the object will be rotated about the y-axis when spawned by <see cref="spawnAngleRange"/>
    /// in relation to the direction of the spawn point to the camera.
    /// </summary>
    public bool applyRandomAngleAtSpawn
    {
        get => m_ApplyRandomAngleAtSpawn;
        set => m_ApplyRandomAngleAtSpawn = value;
    }

    [SerializeField]
    [Tooltip("The range in degrees that the object will randomly be rotated about the y axis when spawned, " +
        "in relation to the direction of the spawn point to the camera.")]
    float m_SpawnAngleRange = 45f;

    /// <summary>
    /// The range in degrees that the object will randomly be rotated about the y axis when spawned, in relation
    /// to the direction of the spawn point to the camera.
    /// </summary>
    public float spawnAngleRange
    {
        get => m_SpawnAngleRange;
        set => m_SpawnAngleRange = value;
    }

    [SerializeField]
    [Tooltip("Whether to spawn each object as a child of this object.")]
    bool m_SpawnAsChildren;

    /// <summary>
    /// Whether to spawn each object as a child of this object.
    /// </summary>
    public bool spawnAsChildren
    {
        get => m_SpawnAsChildren;
        set => m_SpawnAsChildren = value;
    }

    /// <summary>
    /// Event invoked after an object is spawned.
    /// </summary>
    /// <seealso cref="TrySpawnObject"/>
    public event Action<int, Vector3, Quaternion, ulong> objectSpawned;

    /// <summary>
    /// Event invoked after an object is spawned.
    /// </summary>
    /// <seealso cref="TrySpawnObject"/>
    public event Action<Vector3, Quaternion> groundSpawned;

    public bool isGroundSpawned = false;

    /// <summary>
    /// See <see cref="MonoBehaviour"/>.
    /// </summary>
    void Awake()
    {
        EnsureFacingCamera();
    }

    private void Update()
    {
        foreach(var obj in FindObjectsOfType<NetworkParticipant>())
        {
            isGroundSpawned = isGroundSpawned || obj.m_isGroundSpawned.Value;
        }

        if( isGroundSpawned)
        {
            UpdatePlaneVisibility(false);
        }
    }

    void EnsureFacingCamera()
    {
        if (m_CameraToFace == null)
            m_CameraToFace = Camera.main;
    }

    /// <summary>
    /// Sets this behavior to select a random object from <see cref="objectPrefabs"/> each time it spawns.
    /// </summary>
    /// <seealso cref="spawnOptionIndex"/>
    /// <seealso cref="isSpawnOptionRandomized"/>
    public void RandomizeSpawnOption()
    {
        m_SpawnOptionIndex = -1;
    }

    /// <summary>
    /// Attempts to spawn an object from <see cref="objectPrefabs"/> at the given position. The object will have a
    /// yaw rotation that faces <see cref="cameraToFace"/>, plus or minus a random angle within <see cref="spawnAngleRange"/>.
    /// </summary>
    /// <param name="spawnPoint">The world space position at which to spawn the object.</param>
    /// <param name="spawnNormal">The world space normal of the spawn surface.</param>
    /// <returns>Returns <see langword="true"/> if the spawner successfully spawned an object. Otherwise returns
    /// <see langword="false"/>, for instance if the spawn point is out of view of the camera.</returns>
    /// <remarks>
    /// The object selected to spawn is based on <see cref="spawnOptionIndex"/>. If the index is outside
    /// the range of <see cref="objectPrefabs"/>, this method will select a random prefab from the list to spawn.
    /// Otherwise, it will spawn the prefab at the index.
    /// </remarks>
    /// <seealso cref="objectSpawned"/>
    public bool TrySpawnObject(Vector3 spawnPoint, Vector3 spawnNormal)
    {
        if (m_OnlySpawnInView)
        {
            var inViewMin = m_ViewportPeriphery;
            var inViewMax = 1f - m_ViewportPeriphery;
            var pointInViewportSpace = cameraToFace.WorldToViewportPoint(spawnPoint);
            if (pointInViewportSpace.z < 0f || pointInViewportSpace.x > inViewMax || pointInViewportSpace.x < inViewMin ||
                pointInViewportSpace.y > inViewMax || pointInViewportSpace.y < inViewMin)
            {
                return false;
            }
        }

        var objectIndex = isSpawnOptionRandomized ? Random.Range(0, m_ObjectPrefabs.Count) : m_SpawnOptionIndex;

        if (GameObject.Find("ConjureKitManager"))
        {
            m_ObjectPrefabs[objectIndex].GetComponent<ConjureKitEntity>().OwnerID = GameObject.Find("ConjureKitManager").GetComponent<ConjureKitManager>().GetClientID();
        }
        var newObject = Instantiate(m_ObjectPrefabs[objectIndex]);
        if (m_SpawnAsChildren)
            newObject.transform.parent = transform;

        newObject.transform.position = spawnPoint;
        EnsureFacingCamera();
                
        var facePosition = m_CameraToFace.transform.position;
        var forward = facePosition - spawnPoint;
        BurstMathUtility.ProjectOnPlane(forward, spawnNormal, out var projectedForward);
        newObject.transform.rotation = Quaternion.LookRotation(projectedForward, spawnNormal);

        if (m_ApplyRandomAngleAtSpawn)
        {
            var randomRotation = UnityEngine.Random.Range(-m_SpawnAngleRange, m_SpawnAngleRange);
            newObject.transform.Rotate(Vector3.up, randomRotation);
        }

        if (m_SpawnVisualizationPrefab != null)
        {
            var visualizationTrans = Instantiate(m_SpawnVisualizationPrefab).transform;
            visualizationTrans.position = spawnPoint;
            visualizationTrans.rotation = newObject.transform.rotation;
        }



        if(NetworkManager.Singleton.IsConnectedClient && GameObject.Find("ConjureKitManager") == null)
        {
            objectSpawned?.Invoke(spawnOptionIndex, spawnPoint, newObject.transform.rotation, NetworkManager.Singleton.LocalClientId);
            Destroy(newObject);
        }
        return true;
    }

    public bool TrySpawnGround(Vector3 spawnPoint, Vector3 spawnNormal)
    {
        if (isGroundSpawned)
            return false;

        if (m_OnlySpawnInView)
        {
            var inViewMin = m_ViewportPeriphery;
            var inViewMax = 1f - m_ViewportPeriphery;
            var pointInViewportSpace = cameraToFace.WorldToViewportPoint(spawnPoint);
            if (pointInViewportSpace.z < 0f || pointInViewportSpace.x > inViewMax || pointInViewportSpace.x < inViewMin ||
                pointInViewportSpace.y > inViewMax || pointInViewportSpace.y < inViewMin)
            {
                return false;
            }
        }

        var newObject = Instantiate(m_GroundPrefab);
        isGroundSpawned = true;

        if (m_SpawnAsChildren)
            newObject.transform.parent = transform;

        newObject.transform.position = spawnPoint;
        EnsureFacingCamera();

        var facePosition = m_CameraToFace.transform.position;
        var forward = facePosition - spawnPoint;
        BurstMathUtility.ProjectOnPlane(forward, spawnNormal, out var projectedForward);
        newObject.transform.rotation = Quaternion.LookRotation(projectedForward, spawnNormal);

        if (m_ApplyRandomAngleAtSpawn)
        {
            var randomRotation = UnityEngine.Random.Range(-m_SpawnAngleRange, m_SpawnAngleRange);
            newObject.transform.Rotate(Vector3.up, randomRotation);
        }

        if (m_SpawnVisualizationPrefab != null)
        {
            var visualizationTrans = Instantiate(m_SpawnVisualizationPrefab).transform;
            visualizationTrans.position = spawnPoint;
            visualizationTrans.rotation = newObject.transform.rotation;
        }

        if (NetworkManager.Singleton.IsConnectedClient && GameObject.Find("ConjureKitManager") == null)
        {
            groundSpawned?.Invoke(spawnPoint, newObject.transform.rotation);
            Destroy(newObject);
        }
        return true;
    }

    private void UpdatePlaneVisibility(bool visible)
    {
        foreach (var plane in PlaneManager.trackables)
        {
            plane.gameObject.SetActive(visible);
        }
    }
}