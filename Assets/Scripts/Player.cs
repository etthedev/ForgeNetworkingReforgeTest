using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeardedManStudios.Forge.Networking.Generated;
using BeardedManStudios.Forge.Networking;
using BeardedManStudios.Forge.Networking.Unity;

public class Player : PlayerBehavior
{
    /// <summary>
    /// Camera reference to work with relative movement direction
    /// </summary>
    [SerializeField] private Camera _displayCamera;
    [SerializeField] private bool _movementRelativeToCamera;
    [SerializeField] private float _moveSpeed;
    [SerializeField] private Rigidbody _rb;

    private Vector3 _moveDirection;

    #region Monobehaviour
    private void Awake()
    {
        if (_rb == null)
        {
            _rb = this.GetComponent<Rigidbody>();
        }
    }

    private void Start()
    {
        if (!networkObject.IsOwner)
        {
            _displayCamera.enabled = false;
        }
    }

    private void FixedUpdate()
    {
        if (networkObject.IsOwner)
        {
            Vector2 movementInput;

            movementInput.x = Input.GetAxisRaw("Horizontal");
            movementInput.y = Input.GetAxisRaw("Vertical");

            ReceiveMovementInput(movementInput, _displayCamera);

            UpdateNetworkLocation();
        }
        else
        {
            this.transform.position = networkObject.position;
            this.transform.rotation = networkObject.rotation;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        var item = collision.transform.GetComponent<WorldObject>();
        if (item != null)
        {
            CollidedWithWorldObject(item);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        var item = other.transform.GetComponent<WorldObject>();
        if (item != null)
        {
            CollidedWithWorldObject(item);
        }
    }
    #endregion

    /// <summary>
    /// Process movement input and move the player
    /// </summary>
    /// <param name="amount"></param>
    /// <param name="refCamera"></param>
    private void ReceiveMovementInput(Vector2 amount, Camera refCamera)
    {
        if (!networkObject.IsOwner) return;

        Vector3 pos = this.transform.position;

        amount.Normalize();

        pos.x += (amount.x * _moveSpeed * Time.deltaTime);
        pos.z += (amount.y * _moveSpeed * Time.deltaTime);

        _moveDirection = (pos - this.transform.position).normalized;

        if (_movementRelativeToCamera && refCamera != null)
        {
            _moveDirection = refCamera.transform.TransformDirection(_moveDirection);
            _moveDirection.y = 0;
            _moveDirection.Normalize();
        }

        _rb.AddForce(_moveDirection * _moveSpeed, ForceMode.Acceleration);
    }

    public void UpdateNetworkLocation()
    {
        networkObject.position = this.transform.position;
        networkObject.rotation = this.transform.rotation;
    }

    /// <summary>
    /// Handles collision with world objects, only run by the server host
    /// </summary>
    /// <param name="worldObject"></param>
    private void CollidedWithWorldObject(WorldObject worldObject)
    {
        if (!networkObject.IsServer) return;

        if (PlayerState.Instance.Inventory.IsFull)
        {
            Debug.Log("Player inventory full");
            return;
        }

        worldObject.PickUp(this);
    }

    /// <summary>
    /// Sends an RPC call to add item to the player's inventory
    /// </summary>
    /// <param name="item"></param>
    public void AddItemToInventory(SavingVariable item)
    {
        if (networkObject.IsServer)
        {
            using (var m = new MemoryStream())
            {
                using (var writer = new BinaryWriter(m))
                {
                    SavingVariableSaveLoad.SaveVariables(new DataWriter(writer), item);

                    MainThreadManager.Run(() => networkObject.SendRpc(RPC_ADD_ITEM_TO_INVENTORY, Receivers.All, m.ToArray()));
                }
            }
        }
    }

    #region RPC
    public override void AddItemToInventory(RpcArgs args)
    {
        if (!networkObject.IsOwner) return;

        var data = args.GetNext<byte[]>();

        SavingVariable itemData = null;

        using (var m = new MemoryStream(data))
        {
            using (var reader = new BinaryReader(m))
            {
                itemData = SavingVariableSaveLoad.LoadVariables(new DataReader(reader, 0));
            }
        }

        BMSLogger.Instance.Log(string.Format("{0} added to inventory via RPC call", itemData.variableName));
        PlayerState.Instance.AddItemToInventory(itemData);
    }
    #endregion
}
