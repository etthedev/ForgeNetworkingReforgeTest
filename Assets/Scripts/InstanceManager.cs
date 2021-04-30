using BeardedManStudios.Forge.Networking;
using BeardedManStudios.Forge.Networking.Generated;
using BeardedManStudios.Forge.Networking.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstanceManager : InstanceManagerBehavior
{
    /// <summary>
    /// Has the host loaded the scene already?
    /// </summary>
    public bool SceneReady { get; private set; }

    /// <summary>
    /// Number of players connected
    /// </summary>
    public int PlayerCount { get; private set; } 

    public double RoundTripLatency { get; private set; }

    #region Singleton
    private static readonly object _instanceLock = new object();
    private static bool _shuttingDown = false;
    private static InstanceManager _instance;
    public static InstanceManager Instance
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
                lock (_instanceLock)
                {
                    if (_instance == null)
                    {
                        var obj = NetworkManager.Instance.InstantiateInstanceManager();
                        _instance = (InstanceManager)obj;
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
            DontDestroyOnLoad(this.gameObject);
        }
    }

    private void Start()
    {
        NetworkManager.Instance.objectInitialized += ObjectInitialized;
        NetworkManager.Instance.Networker.onPingPong += OnPingPong;
    }

    private void OnApplicationQuit()
    {
        _shuttingDown = true;
    }
    #endregion

    private void ObjectInitialized(INetworkBehavior behavior, NetworkObject obj)
    {
        if (!(obj is PlayerNetworkObject))
            return;
        
        PlayerCount++;
        
        obj.onDestroy += (sender) =>
        {
            PlayerCount--;
        };

        if (NetworkManager.Instance.Networker is IServer)
            obj.Owner.disconnected += (sender) => { obj.Destroy(); };
    }

    private void OnPingPong(double ping, NetWorker sender)
    {
        RoundTripLatency = ping;
    }

    private void Cleanup()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.Networker.onPingPong -= OnPingPong;
            NetworkManager.Instance.objectInitialized -= ObjectInitialized;
        }

        if (networkObject != null)
            networkObject.Destroy();
    }

    public void ToggleSceneReadyFlag(bool isReady)
    {
        if (networkObject.IsServer)
        {
            MainThreadManager.Run(() => networkObject.SendRpc(RPC_SCENE_READY_STATUS, true, Receivers.AllBuffered, isReady));
        }
    }

    #region RPC
    public override void SceneReadyStatus(RpcArgs args)
    {
        SceneReady = args.GetNext<bool>();
    }
    #endregion

    #region GUI
    private void WriteLabel(Rect rect, string message)
    {
        GUI.color = Color.black;
        GUI.Label(rect, message);

        // Do the same thing as above but make the above UI look like a solid
        // shadow so that the text is readable on any contrast screen
        GUI.color = Color.white;
        GUI.Label(rect, message);
    }

    private void OnGUI()
    {
        if (NetworkManager.Instance == null || NetworkManager.Instance.Networker == null)
            return;

        WriteLabel(new Rect(14, 28, 100, 25), "Time: " + NetworkManager.Instance.Networker.Time.Timestep);
        WriteLabel(new Rect(14, 42, 256, 25), "Bandwidth In: " + NetworkManager.Instance.Networker.BandwidthIn);
        WriteLabel(new Rect(14, 56, 256, 25), "Bandwidth Out: " + NetworkManager.Instance.Networker.BandwidthOut);
        WriteLabel(new Rect(14, 70, 256, 25), "Round Trip Latency (ms): " + RoundTripLatency);
    }
    #endregion
}
