/*
 * The purpose of this class is to handle the public and private keys. The objects for both the public and private keys
 * are stored here. Also, the classes to print the keys to local files, the method to convert a key to a base64 string,
 * and the method to add an email to the private list will be stored here.
 *
 * @author Giovanni Coppola (gac6151@rit.edu)
 */

using System.Numerics;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Messenger
{
    /// <summary>
    /// The class that will store the information for the public key so that it can be converted to a JSON file
    /// </summary>
    public class PublicKey
    {
        public string? Email { get; set; }
        public string Key { get; init; } = string.Empty;
    }

    /// <summary>
    /// The class that will store the information for the private key so that it can be converted to a JSON file
    /// </summary>
    public class PrivateKey
    {
        public string[] Email { get; set; } = Array.Empty<string>();
        public string Key { get; init; } = string.Empty;
    }

    /// <summary>
    /// This class will take a key and convert it to it's Base64 encoding
    /// </summary>
    public class ConvertKeyToBase64
    {
        private readonly BigInteger _eorD;
        private readonly BigInteger _n;

        public ConvertKeyToBase64(BigInteger eorD, BigInteger n)
        {
            _eorD = eorD;
            _n = n;
        }

        /// <summary>
        /// Convert the key into a Base64 encoded string and return it
        /// </summary>
        /// <returns></returns>
        public string ConvertBase64()
        {
            // Get the size of the different numbers
            var eorDSizeByteArray = BitConverter.GetBytes(_eorD.GetByteCount());
            var nByteSizeArray = BitConverter.GetBytes(_n.GetByteCount());
            var newEorDByteArray = _eorD.ToByteArray();
            var newNByteArray = _n.ToByteArray();
            
            // Convert the byte arrays of the sizes if the format is in little endian (we want big endian for the sizes)
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(eorDSizeByteArray);
                Array.Reverse(nByteSizeArray);
            }
            else
            {
                Array.Reverse(newEorDByteArray);
                Array.Reverse(newNByteArray);
            }

            // Put the key into a byte array to be base64 encoded
            var encodedByteArray = Array.Empty<byte>();
            var array = encodedByteArray.Concat(eorDSizeByteArray).Concat(newEorDByteArray).Concat(nByteSizeArray)
                .Concat(newNByteArray).ToArray();
            
            return Convert.ToBase64String(array);
        }
    }

    /// <summary>
    /// This class will add the email to the list of emails in the private key so it can be added on the local file
    /// </summary>
    public class AddPrivateEmail
    {
        private readonly string _email;
        private readonly PrivateKey? _privateKey;

        public AddPrivateEmail(string email)
        {
            _email = email;
            
            _privateKey = JsonConvert.DeserializeObject<PrivateKey>(File.ReadAllText("private.key"));
        }

        /// <summary>
        /// This method will add the email to the list of emails.
        /// It will only add it if the email is not already in the list of emails.
        /// </summary>
        public void AddEmail()
        {
            // Make sure the key is valid
            if (_privateKey is null)
            {
                Console.WriteLine("The private key was not valid");
                Environment.Exit(0);
            }
            
            // Check if the email has not already been added to this list and if the key was null
            if (!_privateKey.Email.Contains(_email))
            {
                // Append the email to the list
                _privateKey.Email = _privateKey.Email.Append(_email).ToArray();
                var newPrivateKey = JsonSerializer.Serialize(_privateKey);
                File.WriteAllText("private.key", newPrivateKey);
            }
        }
    }
}