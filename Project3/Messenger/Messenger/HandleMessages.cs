/*
 * The purpose of this class is to handle the message portion of the lab. This is where the message object will be
 * stored. Also, the class to extract the key info that will be used in the encryption and decryption process
 * is stored here.
 *
 * @author Giovanni Coppola (gac6151@rit.edu)
 */

using System.Numerics;

namespace Messenger
{
    /// <summary>
    /// This class will store the information for the Message so it can be converted to a JSON object
    /// </summary>
    public class Message
    {
        public string Email { get; init; } = string.Empty;
        public string Content { get; init; } = string.Empty;
    }

    /// <summary>
    /// This class will be used to extract either the public key or private key data from the files to run in the
    /// encryption and decryption algorithms
    /// </summary>
    public class ExtractKeyInformation
    {
        private readonly string _key;

        public ExtractKeyInformation(string key)
        {
            _key = key;
        }
        
        /// <summary>
        /// This method will process the data for the keys and return the BigIntegers used in the algorithms
        /// </summary>
        /// <returns>An array of two BigIntegers to be used in the algorithm</returns>
        public BigInteger[] ExtractKey()
        {
            var decodedString = Convert.FromBase64String(_key);
            var index = 0;
            
            // Get the size of E form the string
            var newArray = new byte[4];
            Array.Copy(decodedString, index, newArray, 0, 4);
            index += 4;
            var eSizeArray = newArray.Reverse().ToArray();
            var eSize = BitConverter.ToInt32(eSizeArray, 0);

            // Get E from the string
            newArray = new byte[eSize];
            Array.Copy(decodedString, index, newArray, 0, eSize);
            index += eSize;
            var e = new BigInteger(newArray);

            // Get the size of N from the string
            newArray = new byte[4];
            Array.Copy(decodedString, index, newArray, 0, 4);
            index += 4;
            var nSizeArray = newArray.Reverse().ToArray();
            var nSize = BitConverter.ToInt32(nSizeArray, 0);

            // Get N from the string
            newArray = new byte[nSize];
            Array.Copy(decodedString, index, newArray, 0, nSize);
            var n = new BigInteger(newArray);

            // Create an array of BigIntegers to return 
            var processedKey = new BigInteger[2];
            processedKey[0] = e;
            processedKey[1] = n;

            return processedKey;
        }
    }
}