/*
 * The purpose of this class is to run the command line arguments. This class with also generate the public and private
 * keys when the user needs to.
 *
 * @author Giovanni Coppola (gac6151@rit.edu)
 */

using System.Numerics;
using Newtonsoft.Json;

namespace Messenger
{
    /// <summary>
    /// This class will be used to handle the command line arguments to run the correct methods so the messages and
    /// keys can be processed properly
    /// </summary>
    public static class Messenger
    {
        public static async Task Main(string[] args)
        {
            // Check to ensure there are enough command line arguments first
            if (args.Length <= 1)
            {
                PrintUsage();
            }
            
            // Check to see what the command line options are
            switch(args[0])
            {
                case "keyGen":
                    if (args.Length == 2)
                    {
                        // Take in the bit size, and generate the keys based on the bit size.
                        var bitSize = int.Parse(args[1]);
                        
                        // Check if the number of bits is below the minimum
                        if (bitSize < 32)
                        {
                            Console.WriteLine("Please enter a bit size over 32.");
                            Environment.Exit(0);
                        }
                        
                        var generateKey = new CreateKey(bitSize);
                        generateKey.CreateTheKey();
                    }
                    else
                    {
                        PrintUsage();
                    }
                    break;
                case "sendKey":
                    if (args.Length == 2)
                    {
                        // Send the key to the server with the given email
                        var sendKey = new SendKey(args[1]);
                        await sendKey.SendPublicKey();
                    }
                    else
                    {
                        PrintUsage();
                    }
                    break;
                case "getKey":
                    if (args.Length == 2)
                    {
                        // Get the key from the server of the person with the given email address
                        var getKey = new GetKey(args[1]);
                        await getKey.GetPublicKey();
                    }
                    else
                    {
                        PrintUsage();
                    }
                    break;
                case "sendMsg":
                    if (args.Length == 3)
                    {
                        // Send a given message to a given email on the server
                        var sendMsg = new SendMsg(args[1], args[2]);
                        await sendMsg.SendTheMessage();
                    }
                    else
                    {
                        PrintUsage();
                    }
                    break;
                case "getMsg":
                    if (args.Length == 2)
                    {
                        // Get a message from a given email that has encrypted the key with the public key I made
                        var getMsg = new GetMsg(args[1]);
                        await getMsg.GetTheMessage();
                    }
                    else
                    {
                        PrintUsage();
                    }
                    break;
                default:
                    PrintUsage();
                    break;
            }
        }

        /// <summary>
        /// This method will print a usage statement for the command line arguments and exit the code
        /// </summary>
        private static void PrintUsage()
        {
            Console.WriteLine("Usage: Program <option> <other arguments>");
            Console.WriteLine("<option>:");
            Console.WriteLine("keyGen - generate a key of a given size in bits");
            Console.WriteLine("sendKey - send the public key to the given email address");
            Console.WriteLine("getKey - get the public key from a given email address");
            Console.WriteLine("sendMsg - send a given message to a given email address");
            Console.WriteLine("getMsg - get a message from a given email address");
            Environment.Exit(0);
        }
    }

    /// <summary>
    /// This class will generate both the public and private keys and store them in their files
    /// </summary>
    public class CreateKey
    {
        private readonly int _bitSize;

        public CreateKey(int bitSize)
        {
            _bitSize = bitSize;
        }

        /// <summary>
        /// This method was made by the teacher for us. It will let us use the modular inverse when calculating
        /// the value of d from e and n
        /// </summary>
        /// <param name="a">The value e</param>
        /// <param name="n">The value n</param>
        /// <returns>The value of d</returns>
        private static BigInteger ModInverse(BigInteger a, BigInteger n)
        {
            BigInteger i = n, v = 0, d = 1;
            while (a > 0)
            {
                BigInteger t = i / a, x = a;
                a = i % x;
                i = x;
                x = d;
                d = v - t * x;
                v = x;
            }

            v %= n;
            if (v < 0) v = (v + n) % n;
            return v;
        }

        /// <summary>
        /// This method will generate the values for both the public and private keys and add them to their
        /// respective files
        /// </summary>
        public void CreateTheKey()
        {
            // Generate a random bit size for p
            var random = new Random(0);
            var randomBitSize = random.Next((int)(.5*_bitSize - .5*_bitSize*.3), (int)(.5*_bitSize + .5*_bitSize*.3))/8*8;

            // Find the first random prime number for p
            var findPrimeP = new FindPrimeNumbers(randomBitSize);
            var p = findPrimeP.GeneratePrimeNumbers();

            // Find the value for q by using 1024-the size of p for the size of q
            var findPrimeQ = new FindPrimeNumbers((_bitSize - randomBitSize));
            var q = findPrimeQ.GeneratePrimeNumbers();

            // Find N, r, R, and d
            var n = p * q;
            var r = (p - 1) * (q - 1);
            BigInteger e = 65537;
            var d = ModInverse(e, r);
            
            // Print the public key to the local file
            var convertKey = new ConvertKeyToBase64(e, n);
            var newPublicKey = convertKey.ConvertBase64();
            var publicKey = new PublicKey
            {
                Email = null,
                Key = newPublicKey
            };
            var jsonKeyPublic = JsonConvert.SerializeObject(publicKey); 
            File.WriteAllText("public.key", jsonKeyPublic);
            
            // Print the private key to the local file
            convertKey = new ConvertKeyToBase64(d, n);
            var newPrivateKey = convertKey.ConvertBase64();
            var privateKey = new PrivateKey()
            {
                Email = Array.Empty<string>(),
                Key = newPrivateKey
            };
            var jsonKeyPrivate = JsonConvert.SerializeObject(privateKey);
            File.WriteAllText("private.key", jsonKeyPrivate);
        }
    }
}