using BeardedManStudios.Forge.Networking;
using BeardedManStudios.Forge.Networking.Generated;
using BeardedManStudios.Forge.Networking.Unity;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LootingGameManager : LootingGameBehavior
{
    /// <summary>
    /// Responsible for spawning and keeping track of world objects in scene
    /// </summary>
    [SerializeField] private ObjectSpawner _objectSpawner = null;

    /// <summary>
    /// Where the players should be spawned in the scene
    /// </summary>
    [SerializeField] private Vector3 _spawnLocation = Vector3.zero;

    /// <summary>
    /// Reference to the owned player
    /// </summary>
    [SerializeField] private PlayerBehavior _playerRef = null;

    /// <summary>
    /// Singleton, assigned on Awake()
    /// </summary>
    public static LootingGameManager Instance { get; private set; }
    
    #region Monobehaviour
    private void Awake()
    {
        if (Instance !=null)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        NetworkManager.Instance.objectInitialized += ObjectInitialized;

        CreatePlayer();

        if (NetworkManager.Instance.Networker.IsServer)
        {
            InitializeLevel();
            NetworkManager.Instance.Networker.playerAccepted += PlayerAccepted;
        }
        else
        {
            NetworkManager.Instance.Networker.disconnected += DisconnectedFromServer;
        }
    }
    
    private void OnDestroy()
    {
        Cleanup();
    }
    #endregion

    #region Networking

    private void ObjectInitialized(INetworkBehavior behaviour, NetworkObject obj)
    {
        //! Ignore if it's not players being initialized
        if (!(obj is PlayerNetworkObject))
            return;

        //! When the host is disconnected, destroy the client's players as well
        if (NetworkManager.Instance.Networker.IsServer)
            obj.Owner.disconnected += (sender) => { obj.Destroy(); };
    }

    private void PlayerAccepted(NetworkingPlayer player, NetWorker sender)
    {
        MainThreadManager.Run(() =>
        {
            networkObject.SendRpc(player, RPC_INITIALIZE_MAP, SerializeMap());
        });
    }

    /// <summary>
    /// Disconnect client when the server connection is lost
    /// </summary>
    /// <param name="sender"></param>
    private void DisconnectedFromServer(NetWorker sender)
    {
        NetworkManager.Instance.Networker.disconnected -= DisconnectedFromServer;

        MainThreadManager.Run(() =>
        {
            if (NetworkManager.Instance != null) NetworkManager.Instance.Disconnect();

            //!Boot back to menu screen
            SceneManager.LoadScene(0);
        });
    }

    /// <summary>
    /// Unsubscribe from events
    /// </summary>
    private void Cleanup()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.Networker.playerAccepted -= PlayerAccepted;
            NetworkManager.Instance.objectInitialized -= ObjectInitialized;

            NetworkManager.Instance.Networker.disconnected -= DisconnectedFromServer;
        }

        if (networkObject != null)
            networkObject.Destroy();
    }
    #endregion
    
    /// <summary>
    /// Spawn owned player into scene
    /// </summary>
    private void CreatePlayer()
    {
        if (_playerRef == null)
        {
            PlayerBehavior pb = NetworkManager.Instance.InstantiatePlayer();
            _playerRef = pb;
        }
        var unitInsideCircle = UnityEngine.Random.insideUnitSphere * 5f;
        _playerRef.transform.position = _spawnLocation + unitInsideCircle;
        ((Player)_playerRef).UpdateNetworkLocation();

        PlayerState.Instance.LoadPlayerState();
    }

    /// <summary>
    /// Try to load scene state from save files
    /// </summary>
    private void InitializeLevel()
    {
        if (_objectSpawner != null)
        {
            bool hasSave = _objectSpawner.LoadObjectSpawner();

            if (!hasSave)
            {
                _objectSpawner.CreateManyWorldObjects();
            }
        }
    }

    /// <summary>
    /// Sends an RPC call to destroy the world object using the list index in object spawner as reference
    /// </summary>
    /// <param name="index"></param>
    public void SendWorldObjectDestroyedRPC(int index)
    {
        if (networkObject.IsServer)
        {
            MainThreadManager.Run(() => networkObject.SendRpc(RPC_WORLD_OBJECT_DESTROYED, Receivers.All, index));
        }
    }

    #region Save/Load
    public void SaveStates()
    {
        if (networkObject.IsServer)
        {
            _objectSpawner.SaveObjectSpawner(true);
        }

        PlayerState.Instance.SavePlayerState(true);
    }

    //! Uses functions that involves coroutine
    public void SaveStatesGentle()
    {
        if (networkObject.IsServer)
        {
            _objectSpawner.SaveObjectSpawner();
        }

        PlayerState.Instance.SavePlayerState();
    }
    #endregion

    #region Serialization
    private byte[] SerializeMap()
    {
        using (var m = new MemoryStream())
        {
            using (var writer = new BinaryWriter(m))
            {
                DataWriter dataWriter = new DataWriter(writer);

                _objectSpawner.Save(dataWriter);
            }

            return m.ToArray();
        }
    }

    private void DeserializeMap(byte[] data)
    {
        if (_objectSpawner == null)
        {
            Debug.LogError("There are no object spawner in scene");
            return;
        }

        using (var m = new MemoryStream(data))
        {
            using (var reader = new BinaryReader(m))
            {
                _objectSpawner.LoadNoVersion(new DataReader(reader, 0));
            }
        }
    }
    #endregion

    #region RPC
    public override void InitializeMap(RpcArgs args)
    {
        Debug.Log("InitMap");
        MainThreadManager.Run(() =>
        {
            DeserializeMap(args.GetNext<byte[]>());
        });
    }

    public override void WorldObjectDestroyed(RpcArgs args)
    {
        //! Object was initially destroyed at server, shouldn't try to destroy again
        if (networkObject.IsServer) return;

        if (_objectSpawner != null)
        {
            BMSLogger.Instance.Log("World object destroyed via RPC");
            _objectSpawner.DestroyObjectByIndex(args.GetNext<int>());
        }
    }

    public override void WorldObjectCreated(RpcArgs args)
    {
        
    }
    #endregion
}
