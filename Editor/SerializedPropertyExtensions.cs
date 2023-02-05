using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace StellarEditor
{
    public static class SerializedPropertyExtensions
    {
        public static T GetPropertyInstance<T>(this SerializedProperty property)
        {
            string path = property.propertyPath;

            object obj = property.serializedObject.targetObject;
            var type = obj.GetType();
            var fieldNames = path.Split('.');
            for (int i = 0; i < fieldNames.Length; i++)
            {
                var info = type.GetField(fieldNames[i], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (info == null)
                    break;

                obj = info.GetValue(obj);
                type = info.FieldType;
            }

            return (T)obj;

        }
    }
}
