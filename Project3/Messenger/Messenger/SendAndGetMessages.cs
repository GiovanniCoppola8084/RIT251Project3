/*
 * The purpose of this class is to handle the set and get methods for the messages and the keys. Any info that will be
 * taken from the server or sent to the server, is done here. Either a message can be sent or received, or a key can
 * be sent or received.
 *
 * @author Giovanni Coppola (gac6151@rit.edu)
 */

using System.Numerics;
using System.Text;
using Newtonsoft.Json;

namespace Messenger
{
    /// <summary>
    /// This class will send the public key that was created by the user to the server so that it can be saved
    /// </summary>
    internal class SendKey
    {
        // Create an instance of the Http client to be able to send the key
        private static readonly HttpClient Client = new();
        
        private readonly string _email;

        public SendKey(string email)
        {
            _email = email;
        }
        
        /// <summary>
        /// This method will send the public key from the local file to the server
        /// </summary>
        public async Task SendPublicKey()
        {
            string? key = null;
            // Ensure that the file being read in exists
            try
            {
                var keyRead = JsonConvert.DeserializeObject<PublicKey>(await File.ReadAllTextAsync("public.key"));
                if (keyRead == null)
                {
                    Console.WriteLine("Invalid key");
                    Environment.Exit(0);
                }
                
                keyRead.Email = _email;
                key = JsonConvert.SerializeObject(keyRead);
            }
            catch
            {
                Console.WriteLine("The key does not exist");
                Environment.Exit(0);
            }
            
            var content = new StringContent(key, Encoding.UTF8, "application/json");
            // Write the public key to the server
            try
            {
                // Try and send the key and process the return message
                var response = await Client.PutAsync("http://kayrun.cs.rit.edu:5000/Key/" + _email, content);
                response.EnsureSuccessStatusCode();
                Console.WriteLine("Key saved");
                
                // Add the email to the private key on the local file
                var addPrivate = new AddPrivateEmail(_email);
                addPrivate.AddEmail();
            }
            // Catch an error if something happened while sending the key
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0}", e.Message);
            }
        }
    }

    /// <summary>
    /// This class will get the key from the server based on an email that was passed in via the command line arguments
    /// </summary>
    internal class GetKey
    {
        // Create an instance of the Http client so a key can be received
        private static readonly HttpClient Client = new();
        
        private readonly string _email;
        
        public GetKey(string email)
        {
            _email = email;
        }
        
        /// <summary>
        /// This method will make a get request from the server to get a key from a certain email
        /// </summary>
        public async Task GetPublicKey()
        {
            try
            {
                // Try and make a get request from the server and process the message
                var result = await Client.GetAsync("http://kayrun.cs.rit.edu:5000/Key/" + _email);
                result.EnsureSuccessStatusCode();

                // This will get the json conversion of the public key for the email given from the server
                var jsonString = result.Content.ReadAsStringAsync();
                var keyFound = JsonConvert.DeserializeObject<PublicKey>(jsonString.Result);
                if (keyFound is null)
                {
                    Console.WriteLine("The key found was not valid");
                    Environment.Exit(0);
                }

                var keySerialized = JsonConvert.SerializeObject(keyFound);
                await File.WriteAllTextAsync(_email + ".key", keySerialized);
                
                // Add the private email to the local file
                var addPrivate = new AddPrivateEmail(_email);
                addPrivate.AddEmail();
            }
            // Catch an error if something happened while getting the key
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0}", e.Message);
            }
        }
    }

    /// <summary>
    /// This class will be used to send a message to the server so someone can decrypt it
    /// </summary>
    internal class SendMsg
    {
        // Create an instance of the Http client so a message can be sent
        private static readonly HttpClient Client = new();
        
        private readonly string _email;
        private readonly string _message;

        public SendMsg(string email, string message)
        {
            _email = email;
            _message = message;
        }

        /// <summary>
        /// This method will take the properly encoded message and send it to the server
        /// </summary>
        public async Task SendTheMessage()
        {
            // Ensure the public key for the given email has already been stored
            if (!File.Exists(_email + ".key"))
            {
                Console.WriteLine("Key does not exist for {0}", _email);
                Environment.Exit(0);
            }
            
            // Encrypt the message
            var base64Encrypt = EncryptMessage(_message, _email);
            var sendMessage = new Message()
            {
                Email = _email,
                Content = base64Encrypt
            };

            var jsonMessage = JsonConvert.SerializeObject(sendMessage);
            
            var content = new StringContent(jsonMessage, Encoding.UTF8, "application/json");
            // Write the public key to the server
            try
            {
                // Try and send the key and process the return message
                var response = await Client.PutAsync("http://kayrun.cs.rit.edu:5000/Message/" + _email, content);
                response.EnsureSuccessStatusCode();
                Console.WriteLine("Message written");
                
                // Add the email to the private key on the local file
                var addPrivate = new AddPrivateEmail(_email);
                addPrivate.AddEmail();
            }
            // Catch an error if something happened while sending the key
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0}", e.Message);
            }
        }

        /// <summary>
        /// This method will encrypt the message so it can be sent in the proper formatting to the server
        /// </summary>
        /// <param name="message">The message that will be encrypted</param>
        /// <param name="email">The email for which it is sending from</param>
        /// <returns>A base64 encoded string of the message</returns>
        private static string EncryptMessage(string message, string email)
        {
            // Convert the message 
            var byteArrayMessage = Encoding.Default.GetBytes(message);
            var bigIntMessage = new BigInteger(byteArrayMessage);

            // Ensure that the file being read in exists
            PublicKey? publicKey = null;
            try
            {
                publicKey = JsonConvert.DeserializeObject<PublicKey>(File.ReadAllText(email + ".key"));
            }
            catch
            {
                Console.WriteLine("The key does not exist.");
                Environment.Exit(0);
            }

            if (publicKey is null)
            {
                Console.WriteLine("The key was not valid");
                Environment.Exit(0);
            }

            var extract = new ExtractKeyInformation(publicKey.Key);
            var key = extract.ExtractKey();

            // Encrypt the message
            var encryptedMessage = BigInteger.ModPow(bigIntMessage, key[0], key[1]);
            var bigIntEncrypt = encryptedMessage.ToByteArray();
            var base64Encrypt = Convert.ToBase64String(bigIntEncrypt);

            return base64Encrypt;
        }
    }

    /// <summary>
    /// This method will get a message from the server that we can decrypt with our local private key
    /// </summary>
    internal class GetMsg
    {
        // Create an instance of the Http client so a message can be received
        private static readonly HttpClient Client = new();

        private readonly string _email;

        public GetMsg(string email)
        {
            _email = email;
        }

        /// <summary>
        /// This method will get the message from the server and decode it
        /// </summary>
        public async Task GetTheMessage()
        {
            // Check to make sure there is a private key on file
            if (!File.Exists("private.key"))
            {
                Console.WriteLine("Private key does not exist.");
                Environment.Exit(0);
            }

            // Ensure that the file being read in exists
            PrivateKey? privateKey = null;
            try
            {
                privateKey = JsonConvert.DeserializeObject<PrivateKey>(await File.ReadAllTextAsync("private.key"));
            }
            catch
            {
                Console.WriteLine("The key does not exist.");
                Environment.Exit(0);
            }

            // Check if the email has not already been added to this list and if the key was null
            if (privateKey is not null && !privateKey.Email.Contains(_email))
            {
                Console.WriteLine("The email has not yet been added to the email list.");
                Environment.Exit(0);
            }

            try
            {
                // Try and make a get request from the server and process the message
                var result = await Client.GetAsync("http://kayrun.cs.rit.edu:5000/Message/" + _email);
                result.EnsureSuccessStatusCode();
                
                // This will get the json conversion of the public key for the email given from the server
                var jsonString = await result.Content.ReadAsStringAsync();
                var message = JsonConvert.DeserializeObject<Message>(jsonString);
                if (message is null)
                {
                    Console.WriteLine("The message found was not valid");
                    Environment.Exit(0);
                }
                DecryptMessage(message);
            }
            // Catch an error if something happened while getting the key
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0}", e.Message);
            }
        }

        /// <summary>
        /// This method will decrypt the message that was sent from the server. It will use the local private key
        /// since the user who sent it, sent it with the public key local to this email
        /// </summary>
        /// <param name="message">The message object that was taken from the server after converting from json</param>
        private static void DecryptMessage(Message message)
        {
            // Covert the message
            var bigIntMessage = new BigInteger(Convert.FromBase64String(message.Content));
            
            var privateKey = JsonConvert.DeserializeObject<PrivateKey>(File.ReadAllText("private.key"));
            if (privateKey is null)
            {
                Console.WriteLine("The key was not valid");
                Environment.Exit(0);
            }
            
            var extractKey = new ExtractKeyInformation(privateKey.Key);
            var key = extractKey.ExtractKey();
            
            // Decrypt the message
            var decryptedBigInt = BigInteger.ModPow(bigIntMessage, key[0], key[1]);
            var decryptedString = Encoding.Default.GetString(decryptedBigInt.ToByteArray());
            
            // Print the message to the console
            //Console.WriteLine("The message is: {0}", decryptedString);
            Console.Write(decryptedString);
        }
    }
}