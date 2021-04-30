using BeardedManStudios.Forge.Networking;
using BeardedManStudios.Forge.Networking.Generated;
using BeardedManStudios.Forge.Networking.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Teleporter : TeleporterBehavior
{
    [SerializeField] private int _targetSceneId;

    private int _playersInside;

    #region Monobehaviour
    private void OnTriggerEnter(Collider other)
    {
        var obj = other.GetComponent<Player>();
        if (obj != null && networkObject.IsServer)
        {
            _playersInside++;
            
            if (_playersInside == networkObject.Networker.Players.Count)
            {
                InitiateTeleport();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var obj = other.GetComponent<Player>();
        if (obj != null)
        {
            _playersInside--;
        }
    }
    #endregion

    private void InitiateTeleport()
    {
        if (networkObject.IsServer)
        {
            MainThreadManager.Run(() => networkObject.SendRpc(RPC_INITIATE_TELEPORT, true, Receivers.AllBuffered));
        }
        else
        {
            Debug.Log("I Not server");
        }
    }

    public override void InitiateTeleport(RpcArgs args)
    {
        LootingGameManager.Instance.SaveStates();

        LevelLoader.Instance.ChangeScene(_targetSceneId);
    }
}
