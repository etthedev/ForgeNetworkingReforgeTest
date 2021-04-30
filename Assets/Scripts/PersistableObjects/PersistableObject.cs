using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Objects that can be serialized would inherit from this class
/// </summary>
[DisallowMultipleComponent]
public class PersistableObject : MonoBehaviour
{
    protected bool _isBusy;

    public bool IsBusy { get { return _isBusy; } }

    public virtual void Save(DataWriter writer, bool singleFrameSave = false)
    {
        writer.Write(transform.localPosition);
        writer.Write(transform.localRotation);
        writer.Write(transform.localScale);
    }

    public virtual void Load(DataReader reader, bool singleFrameLoad = false)
    {
        transform.localPosition = reader.ReadVector3();
        transform.localRotation = reader.ReadQuaternion();
        transform.localScale = reader.ReadVector3();
    }

    /// <summary>
    /// Load the data without regards to the save version (works only if the data was serialized that way in the first place)
    /// </summary>
    /// <param name="reader"></param>
    public virtual void LoadNoVersion(DataReader reader)
    {
        transform.localPosition = reader.ReadVector3();
        transform.localRotation = reader.ReadQuaternion();
        transform.localScale = reader.ReadVector3();
    }
}
