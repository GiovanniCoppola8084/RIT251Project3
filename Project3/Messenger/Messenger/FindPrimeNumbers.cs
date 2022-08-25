/*
 * The purpose of this class is to find the prime numbers that will be generated for the key gen portion of this lab.
 * Most of this was taken directly from project 2. Only minor portions were made. 
 *
 * @author Giovanni Coppola (gac6151@rit.edu)
 */

using System.Numerics;
using System.Security.Cryptography;
using CustomExtensions;

namespace Messenger
{
     /// <summary>
    /// This class was imported from project 2. It will be used to create random prime numbers for the encryption
    /// and decryption process.
    /// </summary>
    internal class FindPrimeNumbers
    {
        private int NumberOfBits { get; }

        /// <summary>
        /// Constructor method for the FindPrimeNumbers class
        /// </summary>
        /// <param name="numberOfBits">The number of bits that the number must be.</param>
        public FindPrimeNumbers(int numberOfBits)
        {
            NumberOfBits = numberOfBits;
        }

        /// <summary>
        /// The method will generate the random numbers in parallel and check if they are prime
        /// </summary>
        public BigInteger GeneratePrimeNumbers()
        {
            BigInteger number = 0;
            
            // Set up the random number generator to find the correct size integers at random
            var random = new RNGCryptoServiceProvider();

            // Run a parallel loop to find if a number is prime or not
            Parallel.For((long)0, int.MaxValue, (_, parallelLoopState) =>
            {
                var bytes = new byte[NumberOfBits/8];
                random.GetBytes(bytes);
                var randomBigInt = new BigInteger(bytes);

                if (randomBigInt.IsProbablyPrime() && randomBigInt.GetByteCount() == NumberOfBits / 8)
                {
                    number = randomBigInt;
                    parallelLoopState.Stop();
                }
            });

            return number;
        }
    }
}

namespace CustomExtensions
{
    /// <summary>
    /// This class is used to store the extension methods on the BigInteger class (specifically the one that we made to
    /// check if the given BigInteger is a prime number).
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// This was created by the teacher and we had to copy it into the code. This extension method will determine if
        /// the number that we found is prime or not.
        /// </summary>
        /// <param name="value">The integer being checked</param>
        /// <param name="k">The amount of rounds it will be tested in</param>
        /// <returns>If the number is prime or not</returns>
        public static bool IsProbablyPrime(this BigInteger value, int k = 10)
        {
            if (value <= 1) return false;
            if (k <= 0) k = 10;

            var d = value - 1;
            var s = 0;

            while (d % 2 == 0)
            {
                d /= 2;
                s += 1;
            }

            var bytes = new byte[value.ToByteArray().LongLength];

            for (var i = 0; i < k; i++)
            {
                BigInteger a;
                do
                {
                    var gen = new Random();
                    gen.NextBytes(bytes);
                    a = new BigInteger(bytes);
                } while (a < 2 || a >= value - 2);
                
                var x = BigInteger.ModPow(a, d, value);
                if (x == 1 || x == (value - 1)) continue;
                for (var r = 1; r < s; r++)
                {
                    x = BigInteger.ModPow(x, 2, value);
                    if (x == 1) return false;
                    if (x == value - 1) break;
                }

                if (x != value - 1) return false;
            }

            return true;
        }
    }
}