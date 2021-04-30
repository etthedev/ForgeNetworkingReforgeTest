using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private ObjectSpawner _objectSpawner;
    [SerializeField] private InventoryUI _inventoryUI;

    [Header("Controls")]
    public KeyCode spawnLargeQuantityKey = KeyCode.Alpha1;
    public KeyCode destroyAllItemsKey = KeyCode.Alpha2;
    public KeyCode saveObjectsKey = KeyCode.Alpha3;
    public KeyCode inventoryKey = KeyCode.I;
    public KeyCode forceCloseUI = KeyCode.Space;
    public KeyCode clearInventory = KeyCode.Alpha4;
    public KeyCode savePlayerState = KeyCode.Alpha5;

    #region Monobehaviour
    protected virtual void Start()
    {
        InitializeScene();
    }

    protected virtual void Update()
    {
        if (Input.GetKeyDown(spawnLargeQuantityKey))
        {
            _objectSpawner.ClearObjects();
            _objectSpawner.CreateManyWorldObjects();
        }
        else if (Input.GetKeyDown(destroyAllItemsKey))
        {
            _objectSpawner.ClearObjects();
        }
        else if (Input.GetKeyDown(saveObjectsKey))
        {
            _objectSpawner.SaveObjectSpawner();
        }
        else if (Input.GetKeyDown(inventoryKey))
        {
            _inventoryUI.ToggleInventoryUI();
        }
        else if (Input.GetKeyDown(forceCloseUI))
        {
            _inventoryUI.ToggleInventoryUI(true);
        }
        else if (Input.GetKeyDown(clearInventory))
        {
            PlayerState.Instance.Inventory.EmptyInventory();
        }
        else if (Input.GetKeyDown(savePlayerState))
        {
            PlayerState.Instance.SavePlayerState();
        }
    }
    #endregion

    protected virtual void InitializeScene()
    {
        PlayerState.Instance.LoadPlayerState();

        _objectSpawner.LoadObjectSpawner();
        if (_objectSpawner.TotalObjectsLeft == 0)
        {
            _objectSpawner.CreateManyWorldObjects();
        }
    }
}
