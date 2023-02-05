#region Using Directives

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

#endregion

namespace StellarWolf
{

    [Serializable]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {

        #region Fields

        [SerializeField] private List<TKey> m_Keys;
        [SerializeField] private List<TValue> m_Values;

        #endregion

        #region Methods

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            m_Keys.Clear();
            m_Values.Clear();
            m_Keys.Capacity = Count;
            m_Values.Capacity = Count;

            foreach (KeyValuePair<TKey, TValue> pair in this)
            {
                m_Keys.Add(pair.Key);
                m_Values.Add(pair.Value);
            }
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            Clear();
            _ = EnsureCapacity(m_Keys.Count);

            if (m_Keys.Count != m_Values.Count)
                throw new SerializationException($"There are {m_Keys.Count} key and {m_Values.Count} values after deserialization. Make sure that both key and value types are serializable.");

            for (int i = 0; i < m_Keys.Count; i++)
                this.Add(m_Keys[i], m_Values[i]);
        }

        #endregion
    }
}
