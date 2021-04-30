using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class WorldObjectFactory : ScriptableObject
{
    [SerializeField] private WorldObject[] _worldObjects;
    [SerializeField] private Material[] _materials;
    [SerializeField] private string[] _namePrefix;
    [SerializeField] private string[] _nameSuffix;

    public WorldObject MakeWorldObject(int wOIndex = 0, int materialIndex = 0)
    {
        WorldObject wO = Instantiate(_worldObjects[wOIndex]);
        wO.WorldObjectId = wOIndex;
        wO.SetMaterial(_materials[materialIndex], materialIndex);
        wO.SetValues(GenerateRandomName(), GenerateRandomRank(), GenerateRandomFloat(), false, Vector3.zero, GenerateRandomPerks());
        return wO;
    }

    private string GenerateRandomName()
    {
        string prefix;
        string suffix;

        prefix = (_namePrefix.Length == 0) ? "UNDISPUTED" : _namePrefix[Random.Range(0, _namePrefix.Length)];
        suffix = (_nameSuffix.Length == 0) ? "AXE" : _nameSuffix[Random.Range(0, _nameSuffix.Length)];

        return string.Format("{0} {1}", prefix, suffix).ToUpper();
    }

    private int GenerateRandomRank()
    {
        return Random.Range(1, 11);
    }

    private float GenerateRandomFloat()
    {
        return Random.Range(0f, 100f);
    }

    private List<int> GenerateRandomPerks()
    {
        var perkCount = Random.Range(1, 7);
        var perks = new List<int>();

        for(int i = 0; i<perkCount; ++i)
        {
            var perk = Random.Range(0, (int)ITEM_PERKS.TotalPerks);

            perks.Add(perk);
        }

        return perks;
    }

    public WorldObject MakeRandomObject()
    {
        return MakeWorldObject(Random.Range(0, _worldObjects.Length), Random.Range(0, _materials.Length));
    }
}
