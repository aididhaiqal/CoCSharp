﻿using CoCSharp.Networking;
using CoCSharp.Networking.Cryptography;
using CoCSharp.Networking.Messages;
using System;
using System.IO;
using System.Net.Sockets;

namespace CoCSharp.Proxy
{
    public class CoCProxyClient
    {
        public CoCProxyClient(Socket client, Socket server, NetworkManagerAsyncSettings settings)
        {
            // initiated first because message receive triggers too quickly sometimes
            var crypto = new Crypto8(MessageDirection.Server);
            crypto.UpdateSharedKey(Crypto8.SupercellPublicKey);  // use supercell's public key

            // client connection is initiated with standard keys because we are acting as the server
            ClientConnection = new NetworkManagerAsync(client, settings);
            ClientConnection.MessageReceived += ClientReceived;

            // server connection is initiated with generated keys because we are acting as the client
            ServerConnection = new NetworkManagerAsync(server, settings, crypto);
            ServerConnection.MessageReceived += ServerReceived;

            var publicKeyS = Utils.BytesToString(ClientConnection.Crypto.KeyPair.PublicKey);
            var privateKeyS = Utils.BytesToString(ClientConnection.Crypto.KeyPair.PrivateKey);
            Console.WriteLine("Acting as server with standard \n\tpublickey: {0} \n\tprivatekey: {1}", publicKeyS, privateKeyS);

            var publicKeyC = Utils.BytesToString(ServerConnection.Crypto.KeyPair.PublicKey);
            var privateKeyC = Utils.BytesToString(ServerConnection.Crypto.KeyPair.PrivateKey);
            Console.WriteLine("Acting as client with generated \n\tpublickey: {0} \n\tprivatekey: {1}", publicKeyC, privateKeyC);
        }

        public NetworkManagerAsync ClientConnection { get; private set; } // connection to client
        public NetworkManagerAsync ServerConnection { get; private set; } // connection to server

        private byte[] _snonce;
        private byte[] _rnonce;

        private void ClientReceived(object sender, MessageReceivedEventArgs e)
        {
            // C -> P -> S

            Console.WriteLine("[S < C] => ID:{0} Name:{1}", e.Message.ID, e.Message.GetType().Name);
            if (!e.MessageFullyRead)
                Console.WriteLine("        => Did not fully read.");
            if (e.Exception != null)
                Console.WriteLine("        => Warning: Exception occured during reading: {0}", e.Exception.Message);

            var message = e.Message;
            var messageBytes = (byte[])null;
            if (message is SessionRequestMessage)
                messageBytes = e.MessageData;
            else if (message is LoginRequestMessage)
            {
                var lrMessage = e.Message as LoginRequestMessage;
                var rpkStr = Utils.BytesToString(lrMessage.PublicKey);
                var opkStr = Utils.BytesToString(ServerConnection.Crypto.KeyPair.PublicKey);

                Console.WriteLine("        => Decrypted LoginRequestMessage with pk {0}", rpkStr);
                _snonce = (byte[])lrMessage.Nonce.Clone();
                messageBytes = new byte[e.MessageData.Length];

                var body = (byte[])e.MessageBody.Clone();
                ServerConnection.Crypto.Encrypt(ref body);

                Console.WriteLine("        => Encrypted LoginRequestMessage with pk {0}", opkStr);
                Buffer.BlockCopy(e.MessageData, 0, messageBytes, 0, Message.HeaderSize); // header
                Buffer.BlockCopy(ServerConnection.Crypto.KeyPair.PublicKey, 0, messageBytes, Message.HeaderSize, CoCKeyPair.KeyLength); // gen public key
                Buffer.BlockCopy(body, 0, messageBytes, Message.HeaderSize + CoCKeyPair.KeyLength, body.Length); // body

                ServerConnection.Crypto.UpdateNonce(_snonce, UpdateNonceType.Blake);
                ServerConnection.Crypto.UpdateNonce(_snonce, UpdateNonceType.Encrypt); // set _snonce for crypto to use for later encryption
            }
            else
            {
                messageBytes = new byte[e.MessageData.Length];

                var body = (byte[])e.MessageBody.Clone();
                ServerConnection.Crypto.Encrypt(ref body);

                Buffer.BlockCopy(e.MessageData, 0, messageBytes, 0, Message.HeaderSize); // header
                Buffer.BlockCopy(body, 0, messageBytes, Message.HeaderSize, body.Length); // body
            }

            File.WriteAllBytes("messages\\[C2S] " + DateTime.Now.ToString("hh-mm-ss.fff") + " " + e.Message.ID, e.MessageBody);
            ServerConnection.Connection.Send(messageBytes);
        }

        private void ServerReceived(object sender, MessageReceivedEventArgs e)
        {
            // C <- P <- S

            Console.WriteLine("[S > C] => ID:{0} Name:{1}", e.Message.ID, e.Message.GetType().Name);
            if (!e.MessageFullyRead)
                Console.WriteLine("        => Warning: Did not fully read message.");
            if (e.Exception != null)
                Console.WriteLine("        => Warning: Exception occured during reading: {0}", e.Exception.Message);


            var message = e.Message;
            var messageBytes = (byte[])null;
            if (message is SessionSuccessMessage)
                messageBytes = e.MessageData;
            else if (message is LoginSuccessMessage)
            {
                var lsMessage = e.Message as LoginSuccessMessage;
                _rnonce = (byte[])lsMessage.Nonce.Clone();
                messageBytes = new byte[e.MessageData.Length];

                var body = (byte[])e.MessageBody.Clone();
                ClientConnection.Crypto.Encrypt(ref body);

                Buffer.BlockCopy(e.MessageData, 0, messageBytes, 0, Message.HeaderSize); // header
                Buffer.BlockCopy(body, 0, messageBytes, Message.HeaderSize, body.Length); // body

                ClientConnection.Crypto.UpdateNonce(_rnonce, UpdateNonceType.Encrypt);
                ClientConnection.Crypto.UpdateSharedKey(lsMessage.PublicKey); // 'k'
            }
            else
            {
                if (message is OwnHomeDataMessage)
                {
                    var ohdMessage = message as OwnHomeDataMessage;
                    File.WriteAllText("villages\\" + DateTime.Now.ToString("hh-mm-ss.fff") + " ownhomedata.json", 
                                      ohdMessage.OwnAvatarData.OwnVillageData.Home.DeserializedJson);
                }
                messageBytes = new byte[e.MessageData.Length];

                var body = e.MessageBody;
                ClientConnection.Crypto.Encrypt(ref body);

                Buffer.BlockCopy(e.MessageData, 0, messageBytes, 0, Message.HeaderSize); // header
                Buffer.BlockCopy(body, 0, messageBytes, Message.HeaderSize, body.Length); // body
            }

            File.WriteAllBytes("messages\\[C2S] " + DateTime.Now.ToString("hh-mm-ss.fff") + " " + e.Message.ID, e.MessageBody);
            ClientConnection.Connection.Send(messageBytes);
        }
    }
}
