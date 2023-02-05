using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace StellarWolf
{
    public static class TypeExtensions
    {

        private static readonly List<Type> s_Types = new List<Type>()
        {
            typeof(sbyte),
            typeof(byte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(float),
            typeof(double),
            typeof(decimal)
        };

        private static readonly Dictionary<KeyValuePair<Type, Type>, Func<object, object>> s_ConversionMethods = new Dictionary<KeyValuePair<Type, Type>, Func<object, object>>();

        private static readonly Dictionary<Type, string> s_PrimitiveTypes = new Dictionary<Type, string>()
        {
            { typeof(bool), "bool" },
            { typeof(byte), "byte" },
            { typeof(sbyte), "sbyte" },
            { typeof(char), "char" },
            { typeof(decimal), "decimal" },
            { typeof(double), "double" },
            { typeof(float), "float" },
            { typeof(int), "int" },
            { typeof(uint), "uint" },
            { typeof(nint), "nint" },
            { typeof(nuint), "nuint" },
            { typeof(long), "long" },
            { typeof(ulong), "ulong" },
            { typeof(short), "short" },
            { typeof(ushort), "ushort" },
            { typeof(object), "object" },
            { typeof(string), "string" },

        };

        /// <summary>
        /// Does the type represent a number?
        /// </summary>
        /// <remarks>This method returns <see langword="false"/> for <see cref="bool"/>, <see cref="nint">nint</see>, and <see cref="nuint">nuint</see>.</remarks>
        public static bool IsNumericType(this Type type)
        {
            return s_Types.Contains(type);
        }

        /// <summary>
        /// Return a string representation of types, displaying generic parameters.
        /// </summary>
        /// <param name="primitiveNames">Should 'primitive' types be displayed using their keywords instead of explicit types?</param>
        /// <example>a dictionary containing a string key and an integer value would be returned as Dictionary<String, Int32>.</String></example>
        public static string CSharpName(this Type type, bool primitiveNames = false)
        {
            string name = type.Name;

            if (!type.IsGenericType)
            {
                if (type.IsArray)
                {
                    Type a = type;
                    List<string> ranks = new List<string>();

                    while (a.IsArray)
                    {
                        ranks.Add("[" + new string(',', a.GetArrayRank() - 1) + "]");
                        a = a.GetElementType();
                    }
                    return a.CSharpName(primitiveNames) + string.Join("", ranks);
                }

                return s_PrimitiveTypes.TryGetValue(type, out string value) && primitiveNames ? value : name;
            }

            StringBuilder sb = new StringBuilder();
            _ = sb.Append(name[..name.IndexOf('`')]);
            _ = sb.Append("<");
            _ = sb.Append(string.Join(", ", type.GetGenericArguments().Select(t => t.CSharpName(primitiveNames))));
            _ = sb.Append(">");
            return sb.ToString();
        }

        private static MethodInfo GetConversionMethod(Type from, Type to)
        {
            MethodInfo info = from.GetMethods(BindingFlags.Public | BindingFlags.Static).Where(m =>
            {
                if (m.ReturnType != to || m.Name is not "op_Implicit" and not "op_Explicit")
                {
                    return false;
                }

                ParameterInfo[] info = m.GetParameters();

                return info.Length == 1 && info[0].ParameterType == from;

            }).FirstOrDefault();

            info ??= to.GetMethods(BindingFlags.Public | BindingFlags.Static).Where(m =>
            {
                if (m.ReturnType != to || m.Name is not "op_Implicit" and not "op_Explicit")
                {
                    return false;
                }

                ParameterInfo[] info = m.GetParameters();

                return info.Length == 1 && info[0].ParameterType == from;

            }).FirstOrDefault();

            return info;
        }

        private static bool TryGetConversionMethod(Type from, Type to, out MethodInfo info)
        {
            info = GetConversionMethod(from, to);
            return info != null;
        }

        /// <summary>
        /// Is the given type in any way castable to the other type.
        /// </summary>
        /// <remarks>This will return <see langword="true"/> if the <paramref name="from"/> inherits from <paramref name="to"/>, or if either type has an implicit or explicit casting method defined, this will also return true for numeric types.</remarks>
        public static bool IsCastableFrom(this Type to, Type from)
        {
            return s_ConversionMethods.ContainsKey(new KeyValuePair<Type, Type>(to, from)) || to.IsAssignableFrom(from) || TryGetConversionMethod(from, to, out _) || (from.IsNumericType() && to.IsNumericType());
        }

        /// <summary>
        /// Cast a value from type a to type b.
        /// </summary>
        /// <exception cref="InvalidCastException"></exception>
        public static T Cast<T>(this object obj)
        {

            if (!typeof(T).IsCastableFrom(obj.GetType()))
            {
                throw new InvalidCastException();
            }

            if (s_ConversionMethods.ContainsKey(new KeyValuePair<Type, Type>(obj.GetType(), typeof(T))))
            {
                return (T)s_ConversionMethods[new KeyValuePair<Type, Type>(obj.GetType(), typeof(T))].Invoke(obj);
            }

            if (TryConvertNumericType<T>(obj, out object newObject))
            {
                return (T)newObject;
            }

            MethodInfo info = GetConversionMethod(obj.GetType(), typeof(T));

            if (info == null)
            {
                throw new InvalidCastException();
            }

            object newObj = info.Invoke(null, new object[] { obj });
            return (T)newObj;
        }

        /// <summary>
        /// Attempt to cast a value from type a to type b.
        /// </summary>
        public static bool TryCast<T>(this object obj, out T convertedObj)
        {
            try
            {
                convertedObj = obj.Cast<T>();
                return true;
            }
            catch
            {
                convertedObj = default;
                return false;
            }
        }

        private static bool TryConvertNumericType<T>(object obj, out object convertedObj)
        {
            Type from = obj.GetType();
            Type to = typeof(T);

            if (from == typeof(sbyte))
            {
                if (to == typeof(sbyte))
                {
                    convertedObj = (sbyte)obj;
                    return true;
                }
                else if (to == typeof(byte))
                {
                    convertedObj = (byte)(sbyte)obj;
                    return true;
                }
                else if (to == typeof(short))
                {
                    convertedObj = (short)(sbyte)obj;
                    return true;
                }
                else if (to == typeof(ushort))
                {
                    convertedObj = (ushort)(sbyte)obj;
                    return true;
                }
                else if (to == typeof(int))
                {
                    convertedObj = (int)(sbyte)obj;
                    return true;
                }
                else if (to == typeof(uint))
                {
                    convertedObj = (uint)(sbyte)obj;
                    return true;
                }
                else if (to == typeof(long))
                {
                    convertedObj = (long)(sbyte)obj;
                    return true;
                }
                else if (to == typeof(ulong))
                {
                    convertedObj = (ulong)(sbyte)obj;
                    return true;
                }
                else if (to == typeof(float))
                {
                    convertedObj = (float)(sbyte)obj;
                    return true;
                }
                else if (to == typeof(double))
                {
                    convertedObj = (double)(sbyte)obj;
                    return true;
                }
                else if (to == typeof(decimal))
                {
                    convertedObj = (decimal)(sbyte)obj;
                    return true;
                }
                else
                {
                    convertedObj = default(T);
                    return false;
                }
            }
            else if (from == typeof(byte))
            {
                if (to == typeof(sbyte))
                {
                    convertedObj = (sbyte)(byte)obj;
                    return true;
                }
                else if (to == typeof(byte))
                {
                    convertedObj = (byte)obj;
                    return true;
                }
                else if (to == typeof(short))
                {
                    convertedObj = (short)(byte)obj;
                    return true;
                }
                else if (to == typeof(ushort))
                {
                    convertedObj = (ushort)(byte)obj;
                    return true;
                }
                else if (to == typeof(int))
                {
                    convertedObj = (int)(byte)obj;
                    return true;
                }
                else if (to == typeof(uint))
                {
                    convertedObj = (uint)(byte)obj;
                    return true;
                }
                else if (to == typeof(long))
                {
                    convertedObj = (long)(byte)obj;
                    return true;
                }
                else if (to == typeof(ulong))
                {
                    convertedObj = (ulong)(byte)obj;
                    return true;
                }
                else if (to == typeof(float))
                {
                    convertedObj = (float)(byte)obj;
                    return true;
                }
                else if (to == typeof(double))
                {
                    convertedObj = (double)(byte)obj;
                    return true;
                }
                else if (to == typeof(decimal))
                {
                    convertedObj = (decimal)(byte)obj;
                    return true;
                }
                else
                {
                    convertedObj = default(T);
                    return false;
                }
            }
            else if (from == typeof(short))
            {
                if (to == typeof(sbyte))
                {
                    convertedObj = (sbyte)(short)obj;
                    return true;
                }
                else if (to == typeof(byte))
                {
                    convertedObj = (byte)(short)obj;
                    return true;
                }
                else if (to == typeof(short))
                {
                    convertedObj = (short)obj;
                    return true;
                }
                else if (to == typeof(ushort))
                {
                    convertedObj = (ushort)(short)obj;
                    return true;
                }
                else if (to == typeof(int))
                {
                    convertedObj = (int)(short)obj;
                    return true;
                }
                else if (to == typeof(uint))
                {
                    convertedObj = (uint)(short)obj;
                    return true;
                }
                else if (to == typeof(long))
                {
                    convertedObj = (long)(short)obj;
                    return true;
                }
                else if (to == typeof(ulong))
                {
                    convertedObj = (ulong)(short)obj;
                    return true;
                }
                else if (to == typeof(float))
                {
                    convertedObj = (float)(short)obj;
                    return true;
                }
                else if (to == typeof(double))
                {
                    convertedObj = (double)(short)obj;
                    return true;
                }
                else if (to == typeof(decimal))
                {
                    convertedObj = (decimal)(short)obj;
                    return true;
                }
                else
                {
                    convertedObj = default(T);
                    return false;
                }
            }
            else if (from == typeof(ushort))
            {
                if (to == typeof(sbyte))
                {
                    convertedObj = (sbyte)(ushort)obj;
                    return true;
                }
                else if (to == typeof(byte))
                {
                    convertedObj = (byte)(ushort)obj;
                    return true;
                }
                else if (to == typeof(short))
                {
                    convertedObj = (short)(ushort)obj;
                    return true;
                }
                else if (to == typeof(ushort))
                {
                    convertedObj = (ushort)obj;
                    return true;
                }
                else if (to == typeof(int))
                {
                    convertedObj = (int)(ushort)obj;
                    return true;
                }
                else if (to == typeof(uint))
                {
                    convertedObj = (uint)(ushort)obj;
                    return true;
                }
                else if (to == typeof(long))
                {
                    convertedObj = (long)(ushort)obj;
                    return true;
                }
                else if (to == typeof(ulong))
                {
                    convertedObj = (ulong)(ushort)obj;
                    return true;
                }
                else if (to == typeof(float))
                {
                    convertedObj = (float)(ushort)obj;
                    return true;
                }
                else if (to == typeof(double))
                {
                    convertedObj = (double)(ushort)obj;
                    return true;
                }
                else if (to == typeof(decimal))
                {
                    convertedObj = (decimal)(ushort)obj;
                    return true;
                }
                else
                {
                    convertedObj = default(T);
                    return false;
                }
            }
            else if (from == typeof(int))
            {
                if (to == typeof(sbyte))
                {
                    convertedObj = (sbyte)(int)obj;
                    return true;
                }
                else if (to == typeof(byte))
                {
                    convertedObj = (byte)(int)obj;
                    return true;
                }
                else if (to == typeof(short))
                {
                    convertedObj = (short)(int)obj;
                    return true;
                }
                else if (to == typeof(ushort))
                {
                    convertedObj = (ushort)(int)obj;
                    return true;
                }
                else if (to == typeof(int))
                {
                    convertedObj = (int)obj;
                    return true;
                }
                else if (to == typeof(uint))
                {
                    convertedObj = (uint)(int)obj;
                    return true;
                }
                else if (to == typeof(long))
                {
                    convertedObj = (long)(int)obj;
                    return true;
                }
                else if (to == typeof(ulong))
                {
                    convertedObj = (ulong)(int)obj;
                    return true;
                }
                else if (to == typeof(float))
                {
                    convertedObj = (float)(int)obj;
                    return true;
                }
                else if (to == typeof(double))
                {
                    convertedObj = (double)(int)obj;
                    return true;
                }
                else if (to == typeof(decimal))
                {
                    convertedObj = (decimal)(int)obj;
                    return true;
                }
                else
                {
                    convertedObj = default(T);
                    return false;
                }
            }
            else if (from == typeof(uint))
            {
                if (to == typeof(sbyte))
                {
                    convertedObj = (sbyte)(uint)obj;
                    return true;
                }
                else if (to == typeof(byte))
                {
                    convertedObj = (byte)(uint)obj;
                    return true;
                }
                else if (to == typeof(short))
                {
                    convertedObj = (short)(uint)obj;
                    return true;
                }
                else if (to == typeof(ushort))
                {
                    convertedObj = (ushort)(uint)obj;
                    return true;
                }
                else if (to == typeof(int))
                {
                    convertedObj = (int)(uint)obj;
                    return true;
                }
                else if (to == typeof(uint))
                {
                    convertedObj = (uint)obj;
                    return true;
                }
                else if (to == typeof(long))
                {
                    convertedObj = (long)(uint)obj;
                    return true;
                }
                else if (to == typeof(ulong))
                {
                    convertedObj = (ulong)(uint)obj;
                    return true;
                }
                else if (to == typeof(float))
                {
                    convertedObj = (float)(uint)obj;
                    return true;
                }
                else if (to == typeof(double))
                {
                    convertedObj = (double)(uint)obj;
                    return true;
                }
                else if (to == typeof(decimal))
                {
                    convertedObj = (decimal)(uint)obj;
                    return true;
                }
                else
                {
                    convertedObj = default(T);
                    return false;
                }
            }
            else if (from == typeof(int))
            {
                if (to == typeof(sbyte))
                {
                    convertedObj = (sbyte)(long)obj;
                    return true;
                }
                else if (to == typeof(byte))
                {
                    convertedObj = (byte)(long)obj;
                    return true;
                }
                else if (to == typeof(short))
                {
                    convertedObj = (short)(long)obj;
                    return true;
                }
                else if (to == typeof(ushort))
                {
                    convertedObj = (ushort)(long)obj;
                    return true;
                }
                else if (to == typeof(int))
                {
                    convertedObj = (int)(long)obj;
                    return true;
                }
                else if (to == typeof(uint))
                {
                    convertedObj = (uint)(long)obj;
                    return true;
                }
                else if (to == typeof(long))
                {
                    convertedObj = (long)obj;
                    return true;
                }
                else if (to == typeof(ulong))
                {
                    convertedObj = (ulong)(long)obj;
                    return true;
                }
                else if (to == typeof(float))
                {
                    convertedObj = (float)(long)obj;
                    return true;
                }
                else if (to == typeof(double))
                {
                    convertedObj = (double)(long)obj;
                    return true;
                }
                else if (to == typeof(decimal))
                {
                    convertedObj = (decimal)(long)obj;
                    return true;
                }
                else
                {
                    convertedObj = default(T);
                    return false;
                }
            }
            else if (from == typeof(ulong))
            {
                if (to == typeof(sbyte))
                {
                    convertedObj = (sbyte)(ulong)obj;
                    return true;
                }
                else if (to == typeof(byte))
                {
                    convertedObj = (byte)(ulong)obj;
                    return true;
                }
                else if (to == typeof(short))
                {
                    convertedObj = (short)(ulong)obj;
                    return true;
                }
                else if (to == typeof(ushort))
                {
                    convertedObj = (ushort)(ulong)obj;
                    return true;
                }
                else if (to == typeof(int))
                {
                    convertedObj = (int)(ulong)obj;
                    return true;
                }
                else if (to == typeof(uint))
                {
                    convertedObj = (uint)(ulong)obj;
                    return true;
                }
                else if (to == typeof(long))
                {
                    convertedObj = (long)(ulong)obj;
                    return true;
                }
                else if (to == typeof(ulong))
                {
                    convertedObj = (ulong)obj;
                    return true;
                }
                else if (to == typeof(float))
                {
                    convertedObj = (float)(ulong)obj;
                    return true;
                }
                else if (to == typeof(double))
                {
                    convertedObj = (double)(ulong)obj;
                    return true;
                }
                else if (to == typeof(decimal))
                {
                    convertedObj = (decimal)(ulong)obj;
                    return true;
                }
                else
                {
                    convertedObj = default(T);
                    return false;
                }
            }
            else if (from == typeof(float))
            {
                if (to == typeof(sbyte))
                {
                    convertedObj = (sbyte)(float)obj;
                    return true;
                }
                else if (to == typeof(byte))
                {
                    convertedObj = (byte)(float)obj;
                    return true;
                }
                else if (to == typeof(short))
                {
                    convertedObj = (short)(float)obj;
                    return true;
                }
                else if (to == typeof(ushort))
                {
                    convertedObj = (ushort)(float)obj;
                    return true;
                }
                else if (to == typeof(int))
                {
                    convertedObj = (int)(float)obj;
                    return true;
                }
                else if (to == typeof(uint))
                {
                    convertedObj = (uint)(float)obj;
                    return true;
                }
                else if (to == typeof(long))
                {
                    convertedObj = (long)(float)obj;
                    return true;
                }
                else if (to == typeof(ulong))
                {
                    convertedObj = (ulong)(float)obj;
                    return true;
                }
                else if (to == typeof(float))
                {
                    convertedObj = (float)obj;
                    return true;
                }
                else if (to == typeof(double))
                {
                    convertedObj = (double)(float)obj;
                    return true;
                }
                else if (to == typeof(decimal))
                {
                    convertedObj = (decimal)(float)obj;
                    return true;
                }
                else
                {
                    convertedObj = default(T);
                    return false;
                }
            }
            else if (from == typeof(double))
            {
                if (to == typeof(sbyte))
                {
                    convertedObj = (sbyte)(double)obj;
                    return true;
                }
                else if (to == typeof(byte))
                {
                    convertedObj = (byte)(double)obj;
                    return true;
                }
                else if (to == typeof(short))
                {
                    convertedObj = (short)(double)obj;
                    return true;
                }
                else if (to == typeof(ushort))
                {
                    convertedObj = (ushort)(double)obj;
                    return true;
                }
                else if (to == typeof(int))
                {
                    convertedObj = (int)(double)obj;
                    return true;
                }
                else if (to == typeof(uint))
                {
                    convertedObj = (uint)(double)obj;
                    return true;
                }
                else if (to == typeof(long))
                {
                    convertedObj = (long)(double)obj;
                    return true;
                }
                else if (to == typeof(ulong))
                {
                    convertedObj = (ulong)(double)obj;
                    return true;
                }
                else if (to == typeof(float))
                {
                    convertedObj = (float)(double)obj;
                    return true;
                }
                else if (to == typeof(double))
                {
                    convertedObj = (double)obj;
                    return true;
                }
                else if (to == typeof(decimal))
                {
                    convertedObj = (decimal)(double)obj;
                    return true;
                }
                else
                {
                    convertedObj = default(T);
                    return false;
                }
            }
            else if (from == typeof(decimal))
            {
                if (to == typeof(sbyte))
                {
                    convertedObj = (sbyte)(decimal)obj;
                    return true;
                }
                else if (to == typeof(byte))
                {
                    convertedObj = (byte)(decimal)obj;
                    return true;
                }
                else if (to == typeof(short))
                {
                    convertedObj = (short)(decimal)obj;
                    return true;
                }
                else if (to == typeof(ushort))
                {
                    convertedObj = (ushort)(decimal)obj;
                    return true;
                }
                else if (to == typeof(int))
                {
                    convertedObj = (int)(decimal)obj;
                    return true;
                }
                else if (to == typeof(uint))
                {
                    convertedObj = (uint)(decimal)obj;
                    return true;
                }
                else if (to == typeof(long))
                {
                    convertedObj = (long)(decimal)obj;
                    return true;
                }
                else if (to == typeof(ulong))
                {
                    convertedObj = (ulong)(decimal)obj;
                    return true;
                }
                else if (to == typeof(float))
                {
                    convertedObj = (float)(decimal)obj;
                    return true;
                }
                else if (to == typeof(double))
                {
                    convertedObj = (double)(decimal)obj;
                    return true;
                }
                else if (to == typeof(decimal))
                {
                    convertedObj = (decimal)obj;
                    return true;
                }
                else
                {
                    convertedObj = default(T);
                    return false;
                }
            }
            else
            {
                convertedObj = default(T);
                return false;
            }

        }

        /// <summary>
        /// Add a custom conversion method from <typeparamref name="T"/> to <typeparamref name="J"/>.
        /// </summary>
        public static void RegisterConverter<T, J>(Func<T, J> conversionMethod)
        {
            s_ConversionMethods.Add(
                new KeyValuePair<Type, Type>(typeof(T), typeof(J)),
                (i) => conversionMethod((T)i));
        }

    }
}
