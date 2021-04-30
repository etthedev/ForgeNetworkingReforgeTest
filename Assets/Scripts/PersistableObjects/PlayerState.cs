using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerState : PersistableObject
{
    const int SAVE_VERSION = 1;
    const int MAX_MILLISECONDS = 5;
    [SerializeField] private bool _doNotDestroyOnLoad = true;
    [SerializeField] private uint _playerInventoryMaxSize = 40;
    [SerializeField] private PersistentStorage _playerStateStorage;

    [SerializeField] private Inventory _playerInventory;
    public Inventory Inventory
    {
        get
        {
            if (_playerInventory == null)
                _playerInventory = new Inventory(_playerInventoryMaxSize);

            return _playerInventory;
        }
    }

    #region Singleton
    private static readonly object _instanceLock = new object();
    private static bool _shuttingDown = false;
    private static PlayerState _instance;
    public static PlayerState Instance
    {
        get
        {
            if (_shuttingDown)
            {
                Debug.Log("shutting down");
                return null;
            }

            if (_instance == null)
            {
                lock(_instanceLock)
                {
                    if (_instance == null)
                    {
                        var obj = new GameObject("PlayerState");
                        _instance = obj.AddComponent<PlayerState>();
                    }
                }
            }

            return _instance;
        }
    }
    #endregion

    #region Monobehaviour
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;

            if (_doNotDestroyOnLoad)
                DontDestroyOnLoad(this.gameObject);
        }
    }

    private void OnApplicationQuit()
    {
        _shuttingDown = true;
    }
    #endregion

    public void AddItemToInventory(SavingVariable itemdata)
    {
        Inventory.AddItem(itemdata);
    }

    #region Save/Load
    public void SavePlayerState(bool singleFrameSave = false)
    {
        if (_playerStateStorage == null)
        {
            var obj = new GameObject("PlayerStatePersistentStorage");
            _playerStateStorage = obj.AddComponent<PersistentStorage>();
            _playerStateStorage.SaveFileName = "PlayerState";
            _playerStateStorage._doNotDestroyOnLoad = true;
        }

        _playerStateStorage.Save(this, SAVE_VERSION, singleFrameSave);
    }

    public bool LoadPlayerState()
    {
        if (_playerStateStorage == null)
        {
            Debug.Log("No player storage, creating new one");
            var obj = new GameObject("PlayerStatePersistentStorage");
            _playerStateStorage = obj.AddComponent<PersistentStorage>();
            _playerStateStorage.SaveFileName = "PlayerState";
            _playerStateStorage._doNotDestroyOnLoad = true;
        }
        return _playerStateStorage.Load(this);
    }

    public override void Save(DataWriter writer, bool singleFrameSave = false)
    {
        if (_isBusy) return;

        if (singleFrameSave)
        {
            Inventory.SaveInventory(writer);
        }
        else
        {
            StartCoroutine(SavePlayerStateInternal(writer));
        }
    }

    private IEnumerator SavePlayerStateInternal(DataWriter writer)
    {
        _isBusy = true;

        yield return StartCoroutine(Inventory.SaveInventoryCoroutine(writer, MAX_MILLISECONDS));

        _isBusy = false;
    }

    public override void Load(DataReader reader, bool singleFrameLoad = false)
    {
        if (_isBusy) return;

        //! Start loading
        int saveVersion = reader.Version;

        //! if the version is too new, don't load
        if (saveVersion > SAVE_VERSION)
        {
            Debug.LogError("PlayerState save file is from the future, cannot be read");
            _isBusy = false;
            return;
        }

        if (_playerInventory == null)
        {
            _playerInventory = new Inventory(_playerInventoryMaxSize);
        }

        if (singleFrameLoad)
        {
            Inventory.LoadInventory(reader);
        }
        else
        {
            StartCoroutine(LoadPlayerStateInternal(reader));
        }
    }

    private IEnumerator LoadPlayerStateInternal(DataReader reader)
    {
        _isBusy = true;

        yield return StartCoroutine(Inventory.LoadInventoryCoroutine(reader, MAX_MILLISECONDS));

        _isBusy = false;
    }
    #endregion
}
