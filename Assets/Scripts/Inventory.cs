using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Inventory
{
    public Action onInventoryUpdated;

    private List<SavingVariable> items;
    private uint maxSize;

    public bool IsFull { get { return maxSize < items.Count; } }
    public uint MaxSize { get { return maxSize; } }
    public IList<SavingVariable> Items { get { return items.AsReadOnly(); } }

    public Inventory(uint size)
    {
        maxSize = size;
        items = new List<SavingVariable>();
    }

    public void AddItem(SavingVariable itemData)
    {
        if (!IsFull && itemData != null)
        {
            items.Add(itemData);
            Debug.Log(string.Format("Item {0} added to inventory!", itemData.variableName));

            if (onInventoryUpdated != null)
                onInventoryUpdated();
        }
    }

    public void RemoveItem(SavingVariable itemData)
    {
        items.Remove(itemData);
        Debug.Log(string.Format("Item {0} removed from inventory!", itemData.variableName));

        if (onInventoryUpdated != null)
            onInventoryUpdated();
    }

    public void EmptyInventory()
    {
        items.Clear();
        Debug.Log("Inventory cleared");

        if (onInventoryUpdated != null)
            onInventoryUpdated();
    }

    //! Save inventory into writer within a frame
    public void SaveInventory(DataWriter writer)
    {
        Debug.Log("Saving Inventory...");
        System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
        watch.Start();

        //! Start Saving
        writer.Write(maxSize);
        writer.Write(items.Count);

        for (int i = 0; i < items.Count; ++i)
        {
            SavingVariableSaveLoad.SaveVariables(writer, items[i]);
        }

        //! Saving ends
        watch.Stop();
        Debug.Log("Inventory save completed, total time taken : " + watch.Elapsed);
    }

    //! Save inventory with frame offloading
    public IEnumerator SaveInventoryCoroutine(DataWriter writer, int maxMilliseconds)
    {
        Debug.Log("Saving Inventory...");

        System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
        System.Diagnostics.Stopwatch periodicWatch = new System.Diagnostics.Stopwatch();
        watch.Start();
        periodicWatch.Start();

        //! Start Saving
        writer.Write(maxSize);
        writer.Write(items.Count);

        for (int i = 0; i < items.Count; ++i)
        {
            SavingVariableSaveLoad.SaveVariables(writer, items[i]);

            if (periodicWatch.ElapsedMilliseconds > maxMilliseconds)
            {
                periodicWatch.Reset();
                yield return null;
                periodicWatch.Start();
            }
        }

        //! Saving ends
        watch.Stop();
        periodicWatch.Stop();
        Debug.Log("Inventory save completed, total time taken : " + watch.Elapsed);
    }

    //! Load inventory form reader within a frame
    public void LoadInventory(DataReader reader)
    {
        Debug.Log("Loading inventory...");

        System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
        watch.Start();

        items.Clear();

        //!Start loading
        maxSize = reader.ReadUInt();
        var count = reader.ReadInt();

        for (int i = 0; i < count; ++i)
        {
            var data = SavingVariableSaveLoad.LoadVariables(reader);

            AddItem(data);
        }

        //! End loading
        watch.Stop();
        Debug.Log("Inventory load completed, total time taken : " + watch.Elapsed);
    }

    //! Load inventory with frame offloading
    public IEnumerator LoadInventoryCoroutine(DataReader reader, int maxMilliseconds)
    {
        Debug.Log("Loading inventory...");

        System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
        System.Diagnostics.Stopwatch periodicWatch = new System.Diagnostics.Stopwatch();
        watch.Start();
        periodicWatch.Start();

        items.Clear();

        //!Start loading
        maxSize = reader.ReadUInt();
        var count = reader.ReadInt(); 

        for (int i = 0; i < count; ++i)
        {
            var data = SavingVariableSaveLoad.LoadVariables(reader);

            AddItem(data);

            if (periodicWatch.ElapsedMilliseconds > maxMilliseconds)
            {
                periodicWatch.Reset();
                yield return null;
                periodicWatch.Start();
            }
        }

        //! End loading
        watch.Stop();
        periodicWatch.Stop();
        Debug.Log("Inventory load completed, total time taken : " + watch.Elapsed);
    }
}
