#region Using Directives

using StellarWolf;
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

#endregion

namespace StellarEditor
{
    [CustomPropertyDrawer(typeof(ChaosEngine))]
    internal sealed class ChaosEnginePropertyDrawer : PropertyDrawer
    {

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {

            EditorGUI.BeginChangeCheck();
            string seed = EditorGUI.DelayedTextField(position, label, property.FindPropertyRelative("m_Seed").stringValue);

            if (EditorGUI.EndChangeCheck())
                property.GetPropertyInstance<ChaosEngine>().Reseed(seed);
        }
    }
}
