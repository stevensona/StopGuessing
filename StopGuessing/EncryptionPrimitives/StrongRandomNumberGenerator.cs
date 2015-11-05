﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace StopGuessing.EncryptionPrimitives
{
    /// <summary>
    /// A utility interface to .NET's cryptographically-strong random number generator
    /// (the interface we which .NET provided)
    /// </summary>
    public static class StrongRandomNumberGenerator
    {        
        // Pre-allocate a thread-safe random number generator
        private static readonly RNGCryptoServiceProvider LocalRandomNumberGenerator = new RNGCryptoServiceProvider();

        public static void GetBytes(byte[] bytes)
        {
            LocalRandomNumberGenerator.GetBytes(bytes);
        }

        public static ulong Get64Bits(ulong? mod)
        {
            // We'll need the randomness to determine which bit to set and which to clear 
            byte[] randBytes = new byte[8];
            GetBytes(randBytes);

            ulong result = BitConverter.ToUInt64(randBytes, 0);
            if (mod.HasValue)
                result = result % mod.Value;
            return result;
        }

        public static uint Get32Bits(uint? mod)
        {
            // We'll need the randomness to determine which bit to set and which to clear 
            byte[] randBytes = new byte[4];
            GetBytes(randBytes);

            uint result = BitConverter.ToUInt32(randBytes, 0);
            if (mod.HasValue)
                result = result % mod.Value;
            return result;
        }


    }
}