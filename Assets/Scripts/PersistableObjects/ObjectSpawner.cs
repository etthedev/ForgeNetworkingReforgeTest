using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectSpawner : PersistableObject
{
    private static bool _shuttingDown = false;

    const int MAX_MILLISECONDS = 5;

    //! Save file version for object spawner
    const int SAVE_VERSION = 1;

    [Header("Object Spawning")]
    public WorldObjectFactory wOFactory;
    public int volume = 30;
    public float spawnRadius = 48f;

    [Header("Storage")]
    public PersistentStorage storage;

    [Header("UI stuff")]
    public UnityEngine.UI.Text _spawnObjectDisplay;

    [SerializeField] private List<WorldObject> _spawnedObjects;

    public int TotalObjectsLeft { get { return _spawnedObjects.Count; } }

    #region Monobehaviour
    private void Awake()
    {
        _spawnedObjects = new List<WorldObject>();
        WorldObject.onWorldObjectCreated += WorldObjectCreated;
        WorldObject.onWorldObjectDestroyed += WorldObjectDestroyed;
    }

    private void OnDestroy()
    {
        WorldObject.onWorldObjectCreated -= WorldObjectCreated;
        WorldObject.onWorldObjectDestroyed -= WorldObjectDestroyed;
    }

    private void OnApplicationQuit()
    {
        _shuttingDown = true;
    }
    #endregion

    public void CreateWorldObject()
    {
        WorldObject wO = wOFactory.MakeRandomObject();

        Transform t = wO.transform;
        var unitInsideCircle = UnityEngine.Random.insideUnitCircle * spawnRadius;
        t.localPosition = new Vector3(unitInsideCircle.x, this.transform.position.y, unitInsideCircle.y);
        t.localRotation = UnityEngine.Random.rotation;

        wO.SetColor(UnityEngine.Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.25f, 1f, 1f, 1f));
    }

    public void CreateManyWorldObjects()
    {
        if (_isBusy) return;

        StartCoroutine(CreateManyObjectsInternal());
    }

    private IEnumerator CreateManyObjectsInternal()
    {
        Debug.Log(string.Format("Creating {0} objects...", volume));
        _isBusy = true;
        System.Diagnostics.Stopwatch periodicWatch = new System.Diagnostics.Stopwatch();
        periodicWatch.Start();

        for (int i = 0; i < volume; ++i)
        {
            CreateWorldObject();

            if (_spawnObjectDisplay != null)
                _spawnObjectDisplay.text = string.Format("Creating Objects : {0}/{1}", i + 1, volume);

            if (periodicWatch.ElapsedMilliseconds > MAX_MILLISECONDS)
            {
                periodicWatch.Reset();
                yield return null;
                periodicWatch.Start();
            }
        }

        Debug.Log("Objects created");
        _isBusy = false;
    }

    public void ClearObjects()
    {
        int itemCount = _spawnedObjects.Count;

        for (int i = itemCount - 1; i >= 0; --i)
        {
            Destroy(_spawnedObjects[i].gameObject);
        }
    }

    public void DestroyObjectByIndex(int index)
    {
        var wO = _spawnedObjects[index];
        _spawnedObjects.RemoveAt(index);
        Destroy(wO.gameObject);
    }

    #region Events
    private void WorldObjectCreated(WorldObject worldObject)
    {
        _spawnedObjects.Add(worldObject);
    }

    private void WorldObjectDestroyed(WorldObject worldObject)
    {
        if (_shuttingDown) return;

        if (_spawnObjectDisplay != null)
            _spawnObjectDisplay.text = string.Format("Objects Left : {0}", _spawnedObjects.Count);

        if (LootingGameManager.Instance != null)
            LootingGameManager.Instance.SendWorldObjectDestroyedRPC(_spawnedObjects.IndexOf(worldObject));

        _spawnedObjects.Remove(worldObject);
    }
    #endregion

    #region Save/Load
    
    public void SaveObjectSpawner(bool singleFrameSave = false)
    {
        if (storage == null)
        {
            var obj = new GameObject("ObjectSpawnerStorage");
            storage = obj.AddComponent<PersistentStorage>();
            storage.SaveFileName = "WorldObjects";
            storage._doNotDestroyOnLoad = true;
        }

        storage.Save(this, SAVE_VERSION, singleFrameSave);
    }

    public bool LoadObjectSpawner()
    {
        if (storage == null)
        {
            Debug.Log("No Object Storage, creating new one");
            var obj = new GameObject("ObjectSpawnerStorage");
            storage = obj.AddComponent<PersistentStorage>();
            storage.SaveFileName = "WorldObjects";
            storage._doNotDestroyOnLoad = true;
        }

        return storage.Load(this);
    }

    public override void Save(DataWriter writer, bool singleFrameSave = false)
    {
        if (_isBusy) return;
        
        Debug.Log("Saving World Objects...");

        if (singleFrameSave)
        {
            writer.Write(_spawnedObjects.Count);

            for (int i = 0; i < _spawnedObjects.Count; ++i)
            {
                writer.Write(_spawnedObjects[i].WorldObjectId);
                writer.Write(_spawnedObjects[i].MaterialId);
                _spawnedObjects[i].Save(writer);
            }

            Debug.Log("World objects saved");
        }
        else
        {
            StartCoroutine(SaveVariablesInternal(writer));
        }
    }

    private IEnumerator SaveVariablesInternal(DataWriter writer)
    {
        Debug.Log(string.Format("World Object count : {0}", _spawnedObjects.Count));
        _isBusy = true;

        System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
        System.Diagnostics.Stopwatch periodicWatch = new System.Diagnostics.Stopwatch();
        watch.Start();
        periodicWatch.Start();

        //! Start Saving
        writer.Write(_spawnedObjects.Count);

        for (int i = 0; i < _spawnedObjects.Count; ++i)
        {
            writer.Write(_spawnedObjects[i].WorldObjectId);
            writer.Write(_spawnedObjects[i].MaterialId);
            _spawnedObjects[i].Save(writer);

            if (_spawnObjectDisplay != null)
                _spawnObjectDisplay.text = string.Format("Saving Objects... : {0}/{1}", i + 1, _spawnedObjects.Count);

            if (periodicWatch.ElapsedMilliseconds > MAX_MILLISECONDS)
            {
                periodicWatch.Reset();
                yield return null;
                periodicWatch.Start();
            }
        }

        //! Saving ends
        watch.Stop();
        periodicWatch.Stop();

        if (_spawnObjectDisplay != null)
            _spawnObjectDisplay.text = string.Format("Objects saved : {0}",_spawnedObjects.Count);
        Debug.Log("World Objects save completed, total time taken : " + watch.Elapsed);

        _isBusy = false;
    }

    public override void Load(DataReader reader, bool singleFrameLoad = false)
    {
        if (_isBusy) return;

        Debug.Log("Loading world objects...");

        //! Start loading
        int saveVersion = reader.Version;

        //! if the version is too new, don't load
        if (saveVersion > SAVE_VERSION)
        {
            Debug.LogError("Save file is from the future, cannot be read");
            return;
        }

        int count = saveVersion <= 0 ? -saveVersion : reader.ReadInt();

        if (singleFrameLoad)
        {
            for (int i = 0; i < count; ++i)
            {
                int worldObjectId = saveVersion > 0 ? reader.ReadInt() : 0;
                int materialId = saveVersion > 0 ? reader.ReadInt() : 0;
                WorldObject wO = wOFactory.MakeWorldObject(worldObjectId, materialId);
                wO.Load(reader);
            }

            Debug.Log("World Objects load completed");

        }
        else
        {
            StartCoroutine(LoadObjectsInternal(reader, count, saveVersion));
        }
    }

    private IEnumerator LoadObjectsInternal(DataReader reader, int count, int saveVersion)
    {
        _isBusy = true;

        System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
        System.Diagnostics.Stopwatch periodicWatch = new System.Diagnostics.Stopwatch();
        watch.Start();
        periodicWatch.Start();

        Debug.Log(string.Format("World Object count : {0}", count));

        for (int i = 0; i < count; ++i)
        {
            int worldObjectId = saveVersion > 0 ? reader.ReadInt() : 0;
            int materialId = saveVersion > 0 ? reader.ReadInt() : 0;
            WorldObject wO = wOFactory.MakeWorldObject(worldObjectId, materialId);
            wO.Load(reader);

            if (_spawnObjectDisplay != null)
                _spawnObjectDisplay.text = string.Format("Loading Objects : {0}/{1}", i + 1, count);

            if (periodicWatch.ElapsedMilliseconds > MAX_MILLISECONDS)
            {
                periodicWatch.Reset();
                yield return null;
                periodicWatch.Start();
            }
        }

        //! End loading
        watch.Stop();
        periodicWatch.Stop();

        if (_spawnObjectDisplay != null)
            _spawnObjectDisplay.text = string.Format("Objects Loaded : {0}", count);
        Debug.Log("World Objects load completed, total time taken : " + watch.Elapsed);

        _isBusy = false;
    }

    public override void LoadNoVersion(DataReader reader)
    {
        Debug.Log("Deserializing object spawner");

        int count = reader.ReadInt();

        for (int i = 0; i < count; ++i)
        {
            int worldObjectId = reader.ReadInt();
            int materialId = reader.ReadInt();
            WorldObject wO = wOFactory.MakeWorldObject(worldObjectId, materialId);
            wO.LoadNoVersion(reader);
        }

        Debug.Log("Object spawner deserialization completed");
    }
    #endregion

    #region Gizmo
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(this.transform.position, spawnRadius);
    }
    #endregion
}
