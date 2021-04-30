using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public enum ITEM_PERKS
{
    IncreasedDamage = 0,
    IncreasedDefense,
    IncreasedAttackSpeed,
    IncreasedMovementSpeed,
    IncreasedActionSpeed,
    IncreasedHealth,
    IncreasedExp,
    
    TotalPerks
}

public class WorldObject : PersistableObject
{
    public static Action<WorldObject> onWorldObjectCreated;
    public static Action<WorldObject> onWorldObjectDestroyed;

    static int _colorPropertyId = Shader.PropertyToID("_Color");
    static MaterialPropertyBlock sharedPropertyBlock;
    
    [SerializeField] private MeshRenderer _meshRenderer;

    [Tooltip("Item stats, randomized on spawn : \n" +
                "VariableName : name of item \n" +
                "IntVariable : itemRank \n" +
                "FloatVariable : someArbitaryNumber \n" +
                "BoolVariable : is item identified? \n" +
                "RandomPosition : some vector \n" +
                "Int list : list of perks, would be parsed as enum")]
    [SerializeField] private SavingVariable itemData;
    private Color _currentColor;
    private int wOId = int.MinValue;

    public int WorldObjectId
    {
        get
        {
            return wOId;
        }
        set
        {
            if (wOId == int.MinValue && value != int.MinValue)
            {
                wOId = value;
            }
            else
            {
                Debug.LogError("World Object ID cannot be changed");
            }
        }
    }

    public int MaterialId { get; private set; }

    public string ItemName { get { return itemData.variableName; } }

    public int ItemRank { get { return itemData.intVariable; } }

    public float SomeArbitaryNumber { get { return itemData.floatVariable; } }

    public bool Identified { get { return itemData.boolVariable; } }

    public Vector3 SomeVector { get { return itemData.randomPosition; } }

    #region Monobehaviour
    private void Awake()
    {
        if (_meshRenderer == null)
        {
            _meshRenderer = this.GetComponent<MeshRenderer>();
        }

        if (onWorldObjectCreated != null)
            onWorldObjectCreated.Invoke(this);
    }

    private void OnDestroy()
    {
        if (onWorldObjectDestroyed != null)
            onWorldObjectDestroyed.Invoke(this);
    }
    #endregion

    #region Initialization
    public void SetMaterial(Material material, int materialId)
    {
        GetComponent<MeshRenderer>().material = material;
        MaterialId = materialId;
    }

    public void SetColor(Color color)
    {
        _currentColor = color;
        if (sharedPropertyBlock == null)
        {
            sharedPropertyBlock = new MaterialPropertyBlock();
        }
        sharedPropertyBlock.SetColor(_colorPropertyId, color);
        _meshRenderer.SetPropertyBlock(sharedPropertyBlock);
    }

    public void SetValues(string name, int rank, float someNumber, bool identified, Vector3 someVector, List<int> listOfPerks)
    {
        itemData.variableName = name;
        itemData.intVariable = rank;
        itemData.floatVariable = someNumber;
        itemData.boolVariable = identified;

        itemData.intListVariable.Clear();
        itemData.intListVariable.AddRange(listOfPerks);
    }
    #endregion

    public void PickUp(Player player)
    {
        player.AddItemToInventory(itemData);

        Destroy(this.gameObject);
    }

    #region Save/Load
    public override void Save(DataWriter writer, bool singleFrameSave = false)
    {
        base.Save(writer);
        writer.Write(_currentColor);

        SavingVariableSaveLoad.SaveVariables(writer, itemData);
    }

    public override void Load(DataReader reader, bool singleFrameLoad = false)
    {
        base.Load(reader);

        SetColor(reader.Version > 0 ? reader.ReadColor() : Color.white);

        itemData = SavingVariableSaveLoad.LoadVariables(reader);
    }

    public override void LoadNoVersion(DataReader reader)
    {
        base.LoadNoVersion(reader);

        SetColor(reader.ReadColor());

        itemData = SavingVariableSaveLoad.LoadVariables(reader);
    }
    #endregion

}
