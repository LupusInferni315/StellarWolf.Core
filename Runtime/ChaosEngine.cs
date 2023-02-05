#region Using Directives

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using UnityEngine;

#endregion

namespace StellarWolf
{

    /// <summary>
    /// A thread-safe pseudo-random number generator.
    /// </summary>
    [Serializable]
    public sealed class ChaosEngine
    {

        #region CONSTANTS

        private const int SEED = 0xBADDAD;

        #endregion

        #region Fields

        [SerializeField, Delayed] private string m_Seed;
        [SerializeField, HideInInspector] private int m_INext;
        [SerializeField, HideInInspector] private int m_INextP;
        [SerializeField, HideInInspector] private int[] m_SeedArray = new int[55];
        private readonly object m_StateLock = new object();

        private static ChaosEngine m_Shared;

        #endregion

        #region Properties

        /// <summary>
        /// The seed used for the current state of the <seealso cref="ChaosEngine"/>.
        /// </summary>
        public string Seed { get => m_Seed; set => Reseed(value); }

        /// <summary>
        /// A static instance of the <seealso cref="ChaosEngine"/>.
        /// </summary>
        public static ChaosEngine Shared
        {
            get => m_Shared ??= new ChaosEngine();
            set => m_Shared = value ?? new ChaosEngine();
        }

        #endregion

        #region Constructors

        /// <summary>
        /// A thread-safe pseudo-random number generator.
        /// </summary>
        public ChaosEngine()
        {
            Reseed(null);
        }

        /// <summary>
        /// A thread-safe pseudo-random number generator.
        /// </summary>
        /// <param name="seed">The initial state of the <seealso cref="ChaosEngine"/>.</param>
        public ChaosEngine(string seed)
        {
            Reseed(seed);
        }

        /// <summary>
        /// A thread-safe pseudo-random number generator.
        /// </summary>
        /// <param name="seed">The initial state of the <seealso cref="ChaosEngine"/>.</param>
        public ChaosEngine(int seed)
        {
            Reseed(seed);
        }

        #endregion

        #region Seed Methods

        private static int ParseSeed(string seed, out string storedValue)
        {

            if (string.IsNullOrEmpty(seed))
            {
                int value = Guid.NewGuid().GetHashCode();
                storedValue = value.ToString();
                return value;
            }
            else if (int.TryParse(seed, out int i))
            {
                storedValue = seed;
                return i;
            }
            else
            {
                storedValue = seed;
                return seed.GetHashCode();
            }
        }

        /// <summary>
        /// Reset the <seealso cref="ChaosEngine"/> to the initial seeded state.
        /// </summary>
        public void Reset()
        {
            Reseed(m_Seed);
        }

        /// <summary>
        /// Reseed the <seealso cref="ChaosEngine"/> to a new undetermined state.
        /// </summary>
        public void Reseed()
        {
            Reseed(null);
        }

        /// <summary>
        /// Reseed the <seealso cref="ChaosEngine"/> to a new state.
        /// </summary>
        public void Reseed(string seed)
        {
            int state = ParseSeed(seed, out string stored);
            Reseed(state, stored);
        }

        /// <summary>
        /// Reseed the <seealso cref="ChaosEngine"/> to a new state.
        /// </summary>
        public void Reseed(int seed)
        {
            Reseed(seed, seed.ToString());
        }

        private void Reseed(int seed, string stored)
        {
            lock (m_StateLock)
            {
                m_Seed = stored;

                int subtraction = seed == int.MinValue ? int.MaxValue : Math.Abs(seed);
                int mj = SEED - subtraction;
                m_SeedArray[54] = mj;
                int mk = 1;

                for (int i = 0; i < 54; i++)
                {
                    int ii = (42 * (i + 1) % 55) - 1;
                    m_SeedArray[ii] = mk;
                    mk = mj - mk;

                    if (mk < 0)
                    {
                        mk += int.MaxValue;
                    }

                    mj = m_SeedArray[ii];
                }

                for (int k = 1; k < 5; k++)
                {
                    for (int i = 0; i < 55; i++)
                    {
                        m_SeedArray[i] -= m_SeedArray[(i + 30) % 55];

                        if (m_SeedArray[i] < 0)
                        {
                            m_SeedArray[i] += int.MaxValue;
                        }
                    }
                }

                m_INext = 0;
                m_INextP = 42;
            }
        }

        #endregion

        #region Private Methods

        private int NextSample()
        {
            lock (m_StateLock)
            {

                int retVal = m_SeedArray[m_INext] - m_SeedArray[m_INextP];

                if (retVal == int.MaxValue)
                {
                    retVal--;
                }

                if (retVal < 0)
                {
                    retVal = int.MaxValue;
                }

                m_SeedArray[m_INext] = retVal;

                if (++m_INext >= 55)
                {
                    m_INext = 0;
                }

                if (++m_INextP >= 55)
                {
                    m_INextP = 0;
                }

                return retVal;
            }
        }

        private double NextRange(bool inclusive)
        {
            int sample = NextSample();
            return inclusive ? sample * (1.0 / (int.MaxValue - 1)) : sample * (1.0 / int.MaxValue);
        }

        private static void ThrowMinMaxError(string minValue, string maxValue, bool inclusive)
        {
            if (inclusive)
                throw new ArgumentException($"'{minValue}' cannot be greater than '{maxValue}'.");
            else
                throw new ArgumentException($"'{minValue}' cannot be greater than or equal to '{maxValue}'.");
        }

        private static void ThrowBufferError(string bufferName)
        {
            throw new ArgumentNullException(bufferName, $"'{bufferName}' cannot be null or contain less than one element.");
        }

        private static void ThrowTooLowError(string paramName, string count)
        {
            throw new ArgumentException($"'{paramName}' cannot be less than {count}.", paramName);
        }

        private static void ThrowTooLowErrorZero(string paramName) => ThrowTooLowError(paramName, "zero");
        private static void ThrowTooLowErrorOne(string paramName) => ThrowTooLowError(paramName, "one");

        #endregion

        #region Booleans

        #region Boolean

        /// <summary>
        /// Return a random <see cref="bool">boolean value</see> at equal odds.
        /// </summary>
        public bool NextBoolean() => NextInteger(0, 2) == 1;

        #endregion

        #region Boolean Buffer

        /// <summary>
        /// Fill a <paramref name="buffer"/> with random <see cref="bool">boolean values</see> at equal odds.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        public void NextBooleans(bool[] buffer)
        {
            if (buffer is null || buffer.Length < 1)
                ThrowBufferError(nameof(buffer));
            Contract.EndContractBlock();

            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = NextBoolean();
        }

        #endregion

        #region Boolean Enumerable

        /// <summary>
        /// Return a number of random <see cref="bool">boolean values</see> at equal odds.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public IEnumerable<bool> NextBooleans(int count)
        {
            if (count < 1)
                ThrowTooLowErrorOne(nameof(count));
            Contract.EndContractBlock();

            for (int i = 0; i < count; i++)
                yield return NextBoolean();
        }

        #endregion

        #endregion

        #region Probabilties

        #region Probability

        /// <summary>
        /// Return a random <see cref="bool">boolean value</see> with a <paramref name="percent"/> change of being <see langword="true"/>.
        /// </summary>
        /// <param name="percent">The chance of being <see langword="true"/> [0, 1].</param>
        public bool NextProbability(float percent) => percent >= 1 || (percent > 0 && NextFloat() < percent);

        /// <summary>
        /// Return a random <see cref="bool">boolean value</see> with a <paramref name="percent"/> change of being <see langword="true"/>.
        /// </summary>
        /// <param name="percent">The chance of being <see langword="true"/> [0, 1].</param>
        public bool NextProbability(double percent) => percent >= 1 || (percent > 0 && NextDouble() < percent);

        /// <summary>
        /// Return a random <see cref="bool">boolean value</see> with a <paramref name="percent"/> change of being <see langword="true"/>.
        /// </summary>
        /// <param name="percent">The chance of being <see langword="true"/> [0, 100].</param>
        public bool NextProbability(int percent) => percent >= 100 || (percent > 0 && NextInteger(100) < percent);

        #endregion

        #region Probability Buffer

        /// <summary>
        /// Fill a <paramref name="buffer"/> with random <see cref="bool">boolean values</see> with a <paramref name="percent"/> change of being <see langword="true"/>.
        /// </summary>
        /// <param name="percent">The chance of being <see langword="true"/> [0, 1].</param>
        public void NextProbabilities(bool[] buffer, float percent)
        {
            if (buffer is null || buffer.Length < 1)
                ThrowBufferError(nameof(buffer));
            Contract.EndContractBlock();

            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = NextProbability(percent);
        }

        /// <summary>
        /// Fill a <paramref name="buffer"/> with random <see cref="bool">boolean values</see> with a <paramref name="percent"/> change of being <see langword="true"/>.
        /// </summary>
        /// <param name="percent">The chance of being <see langword="true"/> [0, 1].</param>
        public void NextProbabilities(bool[] buffer, double percent)
        {
            if (buffer is null || buffer.Length < 1)
                ThrowBufferError(nameof(buffer));
            Contract.EndContractBlock();

            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = NextProbability(percent);
        }

        /// <summary>
        /// Fill a <paramref name="buffer"/> with random <see cref="bool">boolean values</see> with a <paramref name="percent"/> change of being <see langword="true"/>.
        /// </summary>
        /// <param name="percent">The chance of being <see langword="true"/> [0, 100].</param>
        public void NextProbabilities(bool[] buffer, int percent)
        {
            if (buffer is null || buffer.Length < 1)
                ThrowBufferError(nameof(buffer));
            Contract.EndContractBlock();

            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = NextProbability(percent);
        }

        #endregion

        #region Probability Enumerable

        /// <summary>
        /// Return a number of random <see cref="bool">boolean values</see> with a <paramref name="percent"/> change of being <see langword="true"/>.
        /// </summary>
        /// <param name="percent">The chance of being <see langword="true"/> [0, 1].</param>
        public IEnumerable<bool> NextProbabilities(int count, float percent)
        {
            if (count < 1)
                ThrowTooLowErrorOne(nameof(count));
            Contract.EndContractBlock();

            for (int i = 0; i < count; i++)
                yield return NextProbability(percent);
        }

        /// <summary>
        /// Return a number of random <see cref="bool">boolean values</see> with a <paramref name="percent"/> change of being <see langword="true"/>.
        /// </summary>
        /// <param name="percent">The chance of being <see langword="true"/> [0, 1].</param>
        public IEnumerable<bool> NextProbabilities(int count, double percent)
        {
            if (count < 1)
                ThrowTooLowErrorOne(nameof(count));
            Contract.EndContractBlock();

            for (int i = 0; i < count; i++)
                yield return NextProbability(percent);
        }

        /// <summary>
        /// Return a number of random <see cref="bool">boolean values</see> with a <paramref name="percent"/> change of being <see langword="true"/>.
        /// </summary>
        /// <param name="percent">The chance of being <see langword="true"/> [0, 100].</param>
        public IEnumerable<bool> NextProbabilities(int count, int percent)
        {
            if (count < 1)
                ThrowTooLowErrorOne(nameof(count));
            Contract.EndContractBlock();

            for (int i = 0; i < count; i++)
                yield return NextProbability(percent);
        }

        #endregion

        #endregion

        #region Odds

        /// <summary>
        /// Return a random <see cref="bool">boolean value</see> with an <paramref name="a"/> in <paramref name="b"/> chance of being <see langword="true"/>.
        /// </summary>
        /// <exception cref="DivideByZeroException"></exception>
        public bool NextOdds(int a, int b)
        {
            if (b == 0)
                throw new DivideByZeroException();
            Contract.EndContractBlock();

            return NextProbability((double)a / b);
        }

        /// <summary>
        /// Fill a <paramref name="buffer"/> with random <see cref="bool">boolean values</see> with an <paramref name="a"/> in <paramref name="b"/> chance of being <see langword="true"/>.
        /// </summary>
        /// <exception cref="DivideByZeroException"></exception>
        public void NextOdds(bool[] buffer, int a, int b)
        {
            if (buffer is null || buffer.Length < 1)
                ThrowBufferError(nameof(buffer));
            Contract.EndContractBlock();

            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = NextOdds(a, b);
        }

        /// <summary>
        /// Return a number of random <see cref="bool">boolean values</see> with an <paramref name="a"/> in <paramref name="b"/> chance of being <see langword="true"/>.
        /// </summary>
        /// <exception cref="DivideByZeroException"></exception>
        public IEnumerable<bool> NextOdds(int count, int a, int b)
        {
            if (count < 1)
                ThrowTooLowErrorOne(nameof(count));
            Contract.EndContractBlock();

            for (int i = 0; i < count; i++)
                yield return NextOdds(a, b);
        }

        #endregion

        #region Bytes

        #region Byte

        /// <summary>
        /// Return a random <see cref="byte">8-bit signed integer</see> in the range [0, <see cref="byte.MaxValue"/>].
        /// </summary>
        public byte NextByte() => (byte)NextInteger(0, 256);

        /// <summary>
        /// Return a random <see cref="byte">8-bit signed integer</see> in the range [<paramref name="minValue"/>, <paramref name="maxValue"/>].
        /// </summary>
        /// <param name="minValue">The inclusive minimum value.</param>
        /// <param name="maxValue">The inclusive maximum value.</param>
        /// <exception cref="ArgumentException"></exception>
        public byte NextByte(byte minValue, byte maxValue)
        {
            if (minValue > maxValue)
                ThrowMinMaxError(nameof(minValue), nameof(maxValue), true);
            Contract.EndContractBlock();

            if (minValue == maxValue)
                return minValue;

            return (byte)NextInteger(minValue, maxValue + 1);
        }

        /// <summary>
        /// Return a random <see cref="byte">8-bit signed integer</see> in the range [0, <paramref name="maxValue"/>].
        /// </summary>
        /// <param name="maxValue">The inclusive maximum value.</param>
        public byte NextByte(byte maxValue) => NextByte(0, maxValue);

        #endregion

        #region Byte Buffer

        /// <summary>
        /// Fill a <paramref name="buffer"/> with random <see cref="byte">8-bit signed integers</see> in the range [0, <see cref="byte.MaxValue"/>].
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        public void NextBytes(byte[] buffer)
        {
            if (buffer is null || buffer.Length < 1)
                ThrowBufferError(nameof(buffer));
            Contract.EndContractBlock();

            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = NextByte();
        }

        /// <summary>
        /// Fill a <paramref name="buffer"/> with random <see cref="byte">8-bit signed integers</see> in the range [<paramref name="minValue"/>, <paramref name="maxValue"/>].
        /// </summary>
        /// <param name="minValue">The inclusive minimum value.</param>
        /// <param name="maxValue">The inclusive maximum value.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public void NextBytes(byte[] buffer, byte minValue, byte maxValue)
        {
            if (buffer is null || buffer.Length < 1)
                ThrowBufferError(nameof(buffer));
            Contract.EndContractBlock();

            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = NextByte(minValue, maxValue);
        }

        /// <summary>
        /// Fill a <paramref name="buffer"/> with random <see cref="byte">8-bit signed integers</see> in the range [0, <paramref name="maxValue"/>].
        /// </summary>
        /// <param name="maxValue">The inclusive maximum value.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public void NextBytes(byte[] buffer, byte maxValue)
        {
            if (buffer is null || buffer.Length < 1)
                ThrowBufferError(nameof(buffer));
            Contract.EndContractBlock();

            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = NextByte(maxValue);
        }

        #endregion

        #region Byte Enumerable

        /// <summary>
        /// Return a number of random <see cref="byte">8-bit signed integers</see> in the range [0, <see cref="byte.MaxValue"/>].
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public IEnumerable<byte> NextBytes(int count)
        {
            if (count < 1)
                ThrowTooLowErrorOne(nameof(count));
            Contract.EndContractBlock();

            for (int i = 0; i < count; i++)
                yield return NextByte();
        }

        /// <summary>
        /// Return a number of random <see cref="byte">8-bit signed integers</see> in the range [<paramref name="minValue"/>, <paramref name="maxValue"/>].
        /// </summary>
        /// <param name="minValue">The inclusive minimum value.</param>
        /// <param name="maxValue">The inclusive maximum value.</param>
        /// <exception cref="ArgumentException"></exception>
        public IEnumerable<byte> NextBytes(int count, byte minValue, byte maxValue)
        {
            if (count < 1)
                ThrowTooLowErrorOne(nameof(count));
            Contract.EndContractBlock();

            for (int i = 0; i < count; i++)
                yield return NextByte(minValue, maxValue);
        }

        /// <summary>
        /// Return a number of random <see cref="byte">8-bit signed integers</see> in the range [0, <paramref name="maxValue"/>].
        /// </summary>
        /// <param name="maxValue">The inclusive maximum value.</param>
        /// <exception cref="ArgumentException"></exception>
        public IEnumerable<byte> NextBytes(int count, byte maxValue)
        {
            if (count < 1)
                ThrowTooLowErrorOne(nameof(count));
            Contract.EndContractBlock();

            for (int i = 0; i < count; i++)
                yield return NextByte(maxValue);
        }

        #endregion

        #endregion

        #region Integers

        #region Integer

        /// <summary>
        /// Return a random <see cref="int">32-bit signed integer</see> in the range [0, <see cref="int.MaxValue"/>).
        /// </summary>
        public int NextInteger() => NextSample();

        /// <summary>
        /// Return a random <see cref="int">32-bit signed integer</see> in the range [<paramref name="minValue"/>, <paramref name="maxValue"/>).
        /// </summary>
        /// <param name="minValue">The inclusive minimum value.</param>
        /// <param name="maxValue">The exclusive maximum value.</param>
        /// <exception cref="ArgumentException"></exception>
        public int NextInteger(int minValue, int maxValue)
        {
            if (minValue >= maxValue)
                ThrowMinMaxError(nameof(minValue), nameof(maxValue), false);
            Contract.EndContractBlock();

            if (minValue == maxValue - 1)
                return minValue;

            long range = maxValue - minValue;
            return (int)(NextRange(false) * range) + minValue;
        }

        /// <summary>
        /// Return a random <see cref="int">32-bit signed integer</see> in the range [0, <paramref name="maxValue"/>).
        /// </summary>
        /// <param name="maxValue">The exclusive maximum value.</param>
        /// <exception cref="ArgumentException"></exception>
        public int NextInteger(int maxValue)
        {
            if (maxValue < 1)
                ThrowTooLowErrorOne(nameof(maxValue));
            Contract.EndContractBlock();
            return NextInteger(0, maxValue);
        }

        #endregion

        #region Integer Buffer

        /// <summary>
        /// Fill a <paramref name="buffer"/> with random <see cref="int">32-bit signed integers</see> in the range [0, <see cref="int.MaxValue"/>).
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        public void NextIntegers(int[] buffer)
        {
            if (buffer is null || buffer.Length < 1)
                ThrowBufferError(nameof(buffer));
            Contract.EndContractBlock();

            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = NextInteger();
        }

        /// <summary>
        /// Fill a <paramref name="buffer"/> with random <see cref="int">32-bit signed integers</see> in the range [<paramref name="minValue"/>, <paramref name="maxValue"/>).
        /// </summary>
        /// <param name="minValue">The inclusive minimum value.</param>
        /// <param name="maxValue">The exclusive maximum value.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public void NextIntegers(int[] buffer, int minValue, int maxValue)
        {
            if (buffer is null || buffer.Length < 1)
                ThrowBufferError(nameof(buffer));
            Contract.EndContractBlock();

            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = NextInteger(minValue, maxValue);
        }

        /// <summary>
        /// Fill a <paramref name="buffer"/> with random <see cref="int">32-bit signed integers</see> in the range [0, <paramref name="maxValue"/>).
        /// </summary>
        /// <param name="maxValue">The exclusive maximum value.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public void NextIntegers(int[] buffer, int maxValue)
        {
            if (buffer is null || buffer.Length < 1)
                ThrowBufferError(nameof(buffer));
            Contract.EndContractBlock();

            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = NextInteger(maxValue);
        }

        #endregion

        #region Integer Enumerable

        /// <summary>
        /// Return a number of random <see cref="int">32-bit signed integers</see> in the range [0, <see cref="int.MaxValue"/>).
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public IEnumerable<int> NextIntegers(int count)
        {
            if (count < 1)
                ThrowTooLowErrorOne(nameof(count));
            Contract.EndContractBlock();

            for (int i = 0; i < count; i++)
                yield return NextInteger();
        }

        /// <summary>
        /// Return a number of random <see cref="int">32-bit signed integers</see> in the range [<paramref name="minValue"/>, <paramref name="maxValue"/>).
        /// </summary>
        /// <param name="minValue">The inclusive minimum value.</param>
        /// <param name="maxValue">The exclusive maximum value.</param>
        /// <exception cref="ArgumentException"></exception>
        public IEnumerable<int> NextIntegers(int count, int minValue, int maxValue)
        {
            if (count < 1)
                ThrowTooLowErrorOne(nameof(count));
            Contract.EndContractBlock();

            for (int i = 0; i < count; i++)
            {
                yield return NextInteger(minValue, maxValue);
            }
        }

        /// <summary>
        /// Return a number of random <see cref="int">32-bit signed integers</see> in the range [0, <paramref name="maxValue"/>).
        /// </summary>
        /// <param name="maxValue">The exclusive maximum value.</param>
        /// <exception cref="ArgumentException"></exception>
        public IEnumerable<int> NextIntegers(int count, int maxValue)
        {
            if (count < 1)
                ThrowTooLowErrorOne(nameof(count));
            Contract.EndContractBlock();

            for (int i = 0; i < count; i++)
                yield return NextInteger(maxValue);
        }

        #endregion

        #endregion

        #region Singles

        #region Single

        /// <summary>
        /// Return a random <see cref="float">single-precision floating-point number</see> in the range [0, 1].
        /// </summary>
        public float NextFloat() => (float)NextRange(true);

        /// <summary>
        /// Return a random <see cref="float">single-precision floating-point number</see> in the range [<paramref name="minValue"/>, <paramref name="maxValue"/>].
        /// </summary>
        /// <param name="minValue">The inclusive minimum value.</param>
        /// <param name="maxValue">The inclusive maximum value.</param>
        /// <exception cref="ArgumentException"></exception>
        public float NextFloat(float minValue, float maxValue)
        {
            if (minValue > maxValue)
                ThrowMinMaxError(nameof(minValue), nameof(maxValue), true);
            Contract.EndContractBlock();

            if (minValue == maxValue)
                return minValue;

            float range = maxValue - minValue;
            return (NextFloat() * range) + minValue;
        }

        /// <summary>
        /// Return a random <see cref="float">single-precision floating-point number</see> in the range [0, <paramref name="maxValue"/>].
        /// </summary>
        /// <param name="maxValue">The inclusive maximum value.</param>
        /// <exception cref="ArgumentException"></exception>
        public float NextFloat(float maxValue)
        {
            if (maxValue < 0)
                ThrowTooLowErrorZero(nameof(maxValue));
            Contract.EndContractBlock();

            return NextFloat(0, maxValue);
        }

        #endregion

        #region Single Buffer

        /// <summary>
        /// Fill a <paramref name="buffer"/> with random <see cref="float">single-precision floating-point numbers</see> in the range [0, 1].
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        public void NextFloats(float[] buffer)
        {
            if (buffer is null || buffer.Length < 1)
                ThrowBufferError(nameof(buffer));
            Contract.EndContractBlock();

            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = NextFloat();
        }

        /// <summary>
        /// Fill a <paramref name="buffer"/> with random <see cref="float">single-precision floating-point numbers</see> in the range [<paramref name="minValue"/>, <paramref name="maxValue"/>].
        /// </summary>
        /// <param name="minValue">The inclusive minimum value.</param>
        /// <param name="maxValue">The inclusive maximum value.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public void NextFloats(float[] buffer, float minValue, float maxValue)
        {
            if (buffer is null || buffer.Length < 1)
                ThrowBufferError(nameof(buffer));
            Contract.EndContractBlock();

            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = NextFloat(minValue, maxValue);
        }

        /// <summary>
        /// Fill a <paramref name="buffer"/> with random <see cref="float">single-precision floating-point numbers</see> in the range [0, <paramref name="maxValue"/>].
        /// </summary>
        /// <param name="maxValue">The inclusive maximum value.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public void NextFloats(float[] buffer, float maxValue)
        {
            if (buffer is null || buffer.Length < 1)
                ThrowBufferError(nameof(buffer));

            Contract.EndContractBlock();

            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = NextFloat(maxValue);
        }

        #endregion

        #region Single Enumerable

        /// <summary>
        /// Return a number of random <see cref="float">single-precision floating-point numbers</see> in the range [0, 1].
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public IEnumerable<float> NextFloats(int count)
        {
            if (count < 1)
                ThrowTooLowErrorOne(nameof(count));

            Contract.EndContractBlock();

            for (int i = 0; i < count; i++)
                yield return NextFloat();
        }

        /// <summary>
        /// Return a number of random <see cref="float">single-precision floating-point numbers</see> in the range [<paramref name="minValue"/>, <paramref name="maxValue"/>].
        /// </summary>
        /// <param name="minValue">The inclusive minimum value.</param>
        /// <param name="maxValue">The inclusive maximum value.</param>
        /// <exception cref="ArgumentException"></exception>
        public IEnumerable<float> NextFloats(int count, float minValue, float maxValue)
        {
            if (count < 1)
                ThrowTooLowErrorOne(nameof(count));
            Contract.EndContractBlock();

            for (int i = 0; i < count; i++)
                yield return NextFloat(minValue, maxValue);
        }

        /// <summary>
        /// Return a number of random <see cref="float">single-precision floating-point numbers</see> in the range [0, <paramref name="maxValue"/>].
        /// </summary>
        /// <param name="maxValue">The exclusive maximum value.</param>
        /// <exception cref="ArgumentException"></exception>
        public IEnumerable<float> NextFloats(int count, float maxValue)
        {
            if (count < 1)
                ThrowTooLowErrorOne(nameof(count));

            Contract.EndContractBlock();

            for (int i = 0; i < count; i++)
                yield return NextFloat(maxValue);
        }

        #endregion

        #endregion

        #region Doubles

        #region Double

        /// <summary>
        /// Return a random <see cref="double">double-precision floating-point number</see> in the range [0, 1].
        /// </summary>
        public double NextDouble() => NextRange(true);

        /// <summary>
        /// Return a random <see cref="double">double-precision floating-point number</see> in the range [<paramref name="minValue"/>, <paramref name="maxValue"/>].
        /// </summary>
        /// <param name="minValue">The inclusive minimum value.</param>
        /// <param name="maxValue">The inclusive maximum value.</param>
        /// <exception cref="ArgumentException"></exception>
        public double NextDouble(double minValue, double maxValue)
        {
            if (minValue > maxValue)
                ThrowMinMaxError(nameof(minValue), nameof(maxValue), true);
            Contract.EndContractBlock();

            if (minValue == maxValue)
                return minValue;

            double range = maxValue - minValue;
            return (NextDouble() * range) + minValue;
        }

        /// <summary>
        /// Return a random <see cref="double">double-precision floating-point number</see> in the range [0, <paramref name="maxValue"/>].
        /// </summary>
        /// <param name="maxValue">The inclusive maximum value.</param>
        /// <exception cref="ArgumentException"></exception>
        public double NextDouble(double maxValue)
        {
            if (maxValue < 0)
                ThrowTooLowErrorZero(nameof(maxValue));
            Contract.EndContractBlock();

            return NextDouble(0, maxValue);
        }

        #endregion

        #region Double Buffer

        /// <summary>
        /// Fill a <paramref name="buffer"/> with random <see cref="double">double-precision floating-point numbers</see> in the range [0, 1].
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        public void NextDoubles(double[] buffer)
        {
            if (buffer is null || buffer.Length < 1)
                ThrowBufferError(nameof(buffer));
            Contract.EndContractBlock();

            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = NextDouble();
        }

        /// <summary>
        /// Fill a <paramref name="buffer"/> with random <see cref="double">double-precision floating-point numbers</see> in the range [<paramref name="minValue"/>, <paramref name="maxValue"/>].
        /// </summary>
        /// <param name="minValue">The inclusive minimum value.</param>
        /// <param name="maxValue">The inclusive maximum value.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public void NextDoubles(double[] buffer, double minValue, double maxValue)
        {
            if (buffer is null || buffer.Length < 1)
                ThrowBufferError(nameof(buffer));
            Contract.EndContractBlock();

            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = NextDouble(minValue, maxValue);
        }

        /// <summary>
        /// Fill a <paramref name="buffer"/> with random <see cref="double">double-precision floating-point numbers</see> in the range [0, <paramref name="maxValue"/>].
        /// </summary>
        /// <param name="maxValue">The inclusive maximum value.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public void NextDoubles(double[] buffer, double maxValue)
        {
            if (buffer is null || buffer.Length < 1)
                ThrowBufferError(nameof(buffer));
            Contract.EndContractBlock();

            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = NextDouble(maxValue);
        }

        #endregion

        #region Double Enumerable

        /// <summary>
        /// Return a number of random <see cref="double">double-precision floating-point numbers</see> in the range [0, 1].
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public IEnumerable<double> NextDoubles(int count)
        {
            if (count < 1)
                ThrowTooLowErrorOne(nameof(count));
            Contract.EndContractBlock();

            for (int i = 0; i < count; i++)
                yield return NextDouble();
        }

        /// <summary>
        /// Return a number of random <see cref="double">double-precision floating-point numbers</see> in the range [<paramref name="minValue"/>, <paramref name="maxValue"/>].
        /// </summary>
        /// <param name="minValue">The inclusive minimum value.</param>
        /// <param name="maxValue">The inclusive maximum value.</param>
        /// <exception cref="ArgumentException"></exception>
        public IEnumerable<double> NextDoubles(int count, double minValue, double maxValue)
        {
            if (count < 1)
                ThrowTooLowErrorOne(nameof(count));
            Contract.EndContractBlock();

            for (int i = 0; i < count; i++)
                yield return NextDouble(minValue, maxValue);
        }

        /// <summary>
        /// Return a number of random <see cref="double">double-precision floating-point numbers</see> in the range [0, <paramref name="maxValue"/>].
        /// </summary>
        /// <param name="maxValue">The exclusive maximum value.</param>
        /// <exception cref="ArgumentException"></exception>
        public IEnumerable<double> NextDoubles(int count, double maxValue)
        {
            if (count < 1)
                ThrowTooLowErrorOne(nameof(count));
            Contract.EndContractBlock();

            for (int i = 0; i < count; i++)
                yield return NextDouble(maxValue);
        }

        #endregion

        #endregion

        #region Shuffle

        /// <summary>
        /// Shuffles the values in a list.
        /// </summary>
        public void Shuffle<T>(IList<T> list)
        {
            if (list is null || list.Count() < 1)
                ThrowBufferError(nameof(list));
            Contract.EndContractBlock();

            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = NextInteger(n + 1);
                (list[n], list[k]) = (list[k], list[n]);
            }
        }

        /// <summary>
        /// Shuffles the values in an array.
        /// </summary>
        public void Shuffle<T>(T[] array)
        {

            if (array is null || array.Count() < 1)
                ThrowBufferError(nameof(array));
            Contract.EndContractBlock();

            int n = array.Length;

            while (n > 1)
            {
                n--;
                int k = NextInteger(n + 1);
                (array[n], array[k]) = (array[k], array[n]);
            }
        }

        #endregion

        #region Choose

        /// <summary>
        /// Selects a random element out of a <paramref name="collection"/>.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Types implementing <see cref="IWeighted"/> will be selected by <see cref="IWeighted.Weight"/>.</item>
        /// <item>A Weight of 0 or less, removes the value from being selected.</item>
        /// </list>
        /// </remarks>
        public T Choose<T>(IEnumerable<T> collection)
        {
            if (collection is null || collection.Count() < 1)
                ThrowBufferError(nameof(collection));
            Contract.EndContractBlock();

            if (typeof(IWeighted).IsAssignableFrom(typeof(T)))
            {
                List<T> newList = new List<T>();

                foreach (T t in collection)
                    for (int i = 0; i < (t as IWeighted).Weight; i++)
                        newList.Add(t);
                return newList[NextInteger(newList.Count)];
            }
            return collection.ElementAt(NextInteger(collection.Count()));
        }

        /// <summary>
        /// Selects a random weighted element out of a <paramref name="collection"/>.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Returns <see cref="IWeightedObject{T}.Value"/> instead of <see cref="IWeightedObject{T}"/>.</item>
        /// <item>A Weight of 0 or less, removes the value from being selected.</item>
        /// </list>
        /// </remarks>
        public T Choose<T>(IEnumerable<IWeightedObject<T>> collection)
        {
            if (collection is null || collection.Count() < 1)
                ThrowBufferError(nameof(collection));
            Contract.EndContractBlock();

            List<IWeightedObject<T>> newList = new List<IWeightedObject<T>>();

            foreach (IWeightedObject<T> t in collection)
                for (int i = 0; i < t.Weight; i++)
                    newList.Add(t);
            return newList[NextInteger(newList.Count)].Value;
        }

        /// <summary>
        /// Fills a <paramref name="buffer"/> with random elements out of a <paramref name="collection"/>.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Types implementing <see cref="IWeighted"/> will be selected by <see cref="IWeighted.Weight"/>.</item>
        /// <item>A Weight of 0 or less, removes the value from being selected.</item>
        /// </list>
        /// </remarks>
        public void Choose<T>(T[] buffer, IEnumerable<T> collection)
        {
            if (buffer is null || buffer.Length < 1)
                ThrowBufferError(nameof(buffer));
            Contract.EndContractBlock();

            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = Choose(collection);
        }

        /// <summary>
        /// Fills a <paramref name="buffer"/> with random weighted elements out of a <paramref name="collection"/>.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Returns <see cref="IWeightedObject{T}.Value"/> instead of <see cref="IWeightedObject{T}"/>.</item>
        /// <item>A Weight of 0 or less, removes the value from being selected.</item>
        /// </list>
        /// </remarks>
        public void Choose<T>(T[] buffer, IEnumerable<IWeightedObject<T>> collection)
        {
            if (buffer is null || buffer.Length < 1)
                ThrowBufferError(nameof(buffer));
            Contract.EndContractBlock();

            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = Choose(collection);
        }

        /// <summary>
        /// Returns a number of random elements out of a <paramref name="collection"/>.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Types implementing <see cref="IWeighted"/> will be selected by <see cref="IWeighted.Weight"/>.</item>
        /// <item>A Weight of 0 or less, removes the value from being selected.</item>
        /// </list>
        /// </remarks>
        public IEnumerable<T> Choose<T>(int count, IEnumerable<T> collection)
        {
            if (count < 1)
                ThrowTooLowErrorOne(nameof(count));
            Contract.EndContractBlock();

            for (int i = 0; i < count; i++)
                yield return Choose(collection);
        }

        /// <summary>
        /// Returns a number of random weighted elements out of a <paramref name="collection"/>.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Returns <see cref="IWeightedObject{T}.Value"/> instead of <see cref="IWeightedObject{T}"/>.</item>
        /// <item>A Weight of 0 or less, removes the value from being selected.</item>
        /// </list>
        /// </remarks>
        public IEnumerable<T> Choose<T>(int count, IEnumerable<IWeightedObject<T>> collection)
        {
            if (count < 1)
                ThrowTooLowErrorOne(nameof(count));
            Contract.EndContractBlock();

            for (int i = 0; i < count; i++)
                yield return Choose(collection);
        }

        #endregion

        #region Enum

        /// <summary>
        /// Selects a random value from an <seealso cref="Enum"/>.
        /// </summary>
        public T NextEnum<T>() where T : Enum
        {
            T[] values = (T[])Enum.GetValues(typeof(T));

            List<T> newValues = new List<T>();

            foreach (T t in values)
                for (int i = 0; i < t.GetWeight(); i++)
                    newValues.Add(t);
            return Choose(newValues);
        }

        /// <summary>
        /// Fill a <paramref name="buffer"/> with random values from an <seealso cref="Enum"/>.
        /// </summary>
        public void NextEnum<T>(T[] buffer) where T : Enum
        {
            if (buffer is null || buffer.Length < 1)
                ThrowBufferError(nameof(buffer));
            Contract.EndContractBlock();

            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = NextEnum<T>();
        }

        /// <summary>
        /// Return a number of random values from an <seealso cref="Enum"/>.
        /// </summary>
        public IEnumerable<T> NextEnum<T>(int count) where T : Enum
        {
            if (count < 1)
                ThrowTooLowErrorOne(nameof(count));
            Contract.EndContractBlock();

            for (int i = 0; i < count; i++)
                yield return NextEnum<T>();
        }

        #endregion

    }

}
