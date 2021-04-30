using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SavingVariableSaveLoad
{
    public static void SaveVariables(DataWriter writer, SavingVariable savingVariable)
    {
        writer.Write(savingVariable.variableName);
        writer.Write(savingVariable.intVariable);
        writer.Write(savingVariable.floatVariable);
        writer.Write(savingVariable.boolVariable);
        writer.Write(savingVariable.randomPosition);

        var count = savingVariable.intListVariable.Count;
        writer.Write(count);
        for (int i = 0; i < count; ++i)
        {
            writer.Write(savingVariable.intListVariable[i]);
        }
    }

    public static SavingVariable LoadVariables(DataReader reader)
    {
        var variableName = reader.ReadString();
        var intVariable = reader.ReadInt();
        var floatVariable = reader.ReadFloat();
        var boolVariable = reader.ReadBool();
        var randomPosition = reader.ReadVector3();
        var count = reader.ReadInt();

        SavingVariable savingVariable = new SavingVariable(variableName, intVariable, floatVariable, boolVariable, randomPosition, count);
        savingVariable.intListVariable.Clear();
        for (int i = 0; i < count; ++i)
        {
            savingVariable.intListVariable.Add(reader.ReadInt());
        }
        
        return savingVariable;
    }
}
