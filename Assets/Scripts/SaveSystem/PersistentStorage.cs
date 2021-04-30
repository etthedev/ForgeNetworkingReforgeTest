using System.IO;
using System.Collections;
using UnityEngine;

public class PersistentStorage : MonoBehaviour
{
    public bool _doNotDestroyOnLoad;
    [SerializeField] private string saveFileName = "saveFile";
    private string savePath;
    private bool _initialized;

    public string SaveFileName
    {
        get
        {
            return saveFileName;
        }
        set
        {
            if (value != null)
                saveFileName = value;
            else
                saveFileName = "saveFile";

            _initialized = false;

            InitStorage();
        }
    }

    private void Awake()
    {
        InitStorage();
    }

    private void InitStorage()
    {
        if (!_initialized)
        {
            savePath = Path.Combine(Application.persistentDataPath, saveFileName);
            _initialized = true;

            if (_doNotDestroyOnLoad)
                DontDestroyOnLoad(this.gameObject);
        }
    }

    public void Save(PersistableObject o, int version, bool singleFrameSave = false)
    {
        InitStorage();

        StartCoroutine(SaveInternal(o, version, singleFrameSave));
    }

    private IEnumerator SaveInternal(PersistableObject o, int version, bool singleFrameSave = false)
    {
        using (var writer = new BinaryWriter(File.Open(savePath, FileMode.Create)))
        {
            //! Writing the save file version
            writer.Write(-version);
            o.Save(new DataWriter(writer), singleFrameSave);

            while (o.IsBusy)
            {
                yield return null;
            }
        }
    }

    /// <summary>
    /// Attempt to load an object, returns false if the save file is not found
    /// </summary>
    /// <param name="o"></param>
    /// <returns></returns>
    public bool Load(PersistableObject o)
    {
        InitStorage();

        if (!File.Exists(savePath))
            return false;

        StartCoroutine(LoadInternal(o));

        return true;
    }

    private IEnumerator LoadInternal(PersistableObject o)
    {
        using (var reader = new BinaryReader(File.Open(savePath, FileMode.Open)))
        {
            o.Load(new DataReader(reader, -reader.ReadInt32()));

            while (o.IsBusy)
            {
                yield return null;
            }
        }
    }
}