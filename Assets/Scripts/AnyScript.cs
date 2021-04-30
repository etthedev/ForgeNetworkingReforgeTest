using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Threading;

[System.Serializable]
public class SavingVariable
{
    public string variableName;
    public int intVariable;
    public float floatVariable;
    public bool boolVariable;
    public Vector3 randomPosition;
    public List<int> intListVariable;

    public SavingVariable(string _string, int _int, float _float, bool _bool, Vector3 _vector, int numberOfArray)
    {
        variableName = _string;
        intVariable = _int;
        floatVariable = _float;
        boolVariable = _bool;
        randomPosition = _vector;
        intListVariable = new List<int>();
        for (int i = 0; i < numberOfArray; i++) 
        {
            intListVariable.Add(Random.Range(0, 100));
        }
    }
}

public class AnyScript : MonoBehaviour
{
    const int MAX_MILLISECONDS = 5;

    public List<SavingVariable> ListsOfVariable;
    public string saveFileName ="savingVariables";
    private string _savePath;
    private bool _isSaving;
    private bool _isLoading;

    #region Monobehaviour
    private void Awake()
    {
        ListsOfVariable = new List<SavingVariable>();
        _savePath = Path.Combine(Application.persistentDataPath, saveFileName);
        Debug.Log(string.Format("Save path : {0}", _savePath));
    }

    private void Start()
    {
        for (int i = 0; i < 10; i++)
        {
            ListsOfVariable.Add(new SavingVariable("string" + i, Random.Range(0, 100), Random.Range(0.0f, 50.0f), (Random.Range(0, 2) == 0) ? true : false, new Vector3(i * 2, i * 3, i * 4), 3));
        }
    }

    #endregion

    public void SaveVariables()
    {
        if (_isSaving) return;

        Debug.Log("Saving...");
        _isSaving = true;
        StartCoroutine(SaveVariablesInternal());
    }

    private IEnumerator SaveVariablesInternal()
    {
        Debug.Log("Saving...");
        using (var writer = new BinaryWriter(File.Open(_savePath, FileMode.OpenOrCreate)))
        {
            var dataWriter = new DataWriter(writer);
            var variableList = new List<SavingVariable>(ListsOfVariable);

            //! Using stopwatching instead of thread because there's a List.Add in SavingVariable class
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();

            dataWriter.Write(variableList.Count); //! store how many items are there

            for (int i = 0; i < variableList.Count; ++i)
            {
                if (watch.ElapsedMilliseconds > MAX_MILLISECONDS)
                {
                    watch.Reset();
                    yield return null;
                    watch.Start();
                }

                SavingVariableSaveLoad.SaveVariables(dataWriter, variableList[i]);
            }

            Debug.Log("Saved!");
            _isSaving = false;
        }
    }

    public void LoadVariables()
    {
        if (_isLoading) return;

        _isLoading = true;
        if (File.Exists(_savePath))
        {
            StartCoroutine(LoadVariablesInternal());
        }
    }

    private IEnumerator LoadVariablesInternal()
    {
        Debug.Log("Loading...");
        using (var reader = new BinaryReader(File.Open(_savePath, FileMode.OpenOrCreate)))
        {
            var dataReader = new DataReader(reader, 0);
            var readData = new List<SavingVariable>();

            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();

            var collectionSize = dataReader.ReadInt();

            for (int i = 0; i < collectionSize; ++i)
            {
                if (watch.ElapsedMilliseconds > MAX_MILLISECONDS)
                {
                    watch.Reset();
                    yield return null;
                    watch.Start();
                }

                var savedVariable = SavingVariableSaveLoad.LoadVariables(dataReader);
                readData.Add(savedVariable);
            }

            Debug.Log("Loaded!");
            ListsOfVariable = readData;
            _isLoading = false;
        }
    }

    public void DeleteSavedFile()
    {
        if (File.Exists(_savePath))
        {
            File.Delete(_savePath);
            Debug.Log("Save file deleted");
        }
    }
}
