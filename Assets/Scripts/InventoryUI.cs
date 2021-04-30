using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private Transform _container;
    [SerializeField] private Transform _slotPrefab;
    public KeyCode inventoryKey = KeyCode.I;

    private List<Transform> _spawnedSlots = new List<Transform>();
    private List<SavingVariable> _itemDatas = new List<SavingVariable>();
    private uint _slotCount;
    private int _takenSlots;
    private bool _inventoryOpen;
    private bool _isBusy;

    #region MonoBehaviour
    private void Awake()
    {
        PlayerState.Instance.Inventory.onInventoryUpdated += UpdateInventoryUI;
    }

    private void OnDestroy()
    {
        if(PlayerState.Instance != null)
            PlayerState.Instance.Inventory.onInventoryUpdated -= UpdateInventoryUI;
    }
    #endregion

    public void ToggleInventoryUI(bool forceClose = false)
    {
        _inventoryOpen = forceClose? false : !_inventoryOpen;
        this.gameObject.SetActive(_inventoryOpen);

        if (_inventoryOpen)
            UpdateInventoryUI();
    }

    private void UpdateInventoryUI()
    {
        if (_isBusy) return;

        StartCoroutine(GenerateInventorySlots());
    }

    private IEnumerator GenerateInventorySlots()
    {
        _isBusy = true;

        System.Diagnostics.Stopwatch periodicWatch = new System.Diagnostics.Stopwatch();
        periodicWatch.Start();

        Inventory playerInventory = PlayerState.Instance.Inventory;
        _slotCount = playerInventory.MaxSize;
        Debug.Log("MaxSize : " + _slotCount);
        _takenSlots = playerInventory.Items.Count;

        _itemDatas.Clear();
        _itemDatas.AddRange(playerInventory.Items);

        //! Disable all current slots
        for (int i = 0; i < _spawnedSlots.Count; ++i)
        {
            _spawnedSlots[i].gameObject.SetActive(false);
        }

        //! Find out how many more slots are needed, spawn them
        var spawnable = _slotCount - _spawnedSlots.Count;
        for (uint i = 0; i < spawnable; ++i)
        {
            if (periodicWatch.ElapsedMilliseconds > 5)
            {
                periodicWatch.Reset();
                yield return null;
                periodicWatch.Start();
            }

            var obj = Instantiate(_slotPrefab, _container);
            obj.gameObject.SetActive(false);
            _spawnedSlots.Add(obj);
        }

        //! Populate slots
        for (int i = 0; i < _slotCount; ++i)
        {
            if (periodicWatch.ElapsedMilliseconds > 5)
            {
                periodicWatch.Reset();
                yield return null;
                periodicWatch.Start();
            }
            _spawnedSlots[i].gameObject.SetActive(true);
        }

        periodicWatch.Stop();
        _isBusy = false;
    }
}
