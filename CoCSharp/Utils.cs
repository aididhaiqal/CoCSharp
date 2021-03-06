﻿using CoCSharp.Networking;
using System;
using System.IO;
using System.Net.Sockets;

namespace CoCSharp
{
    internal static class Utils
    {
        public static Random Random = new Random();

        public static void DumpBuffer(SocketAsyncEventArgs args)
        {
            File.WriteAllBytes("dump", args.Buffer);
        }

        public static void DumpBuffer(MessageWriter writer)
        {
            File.WriteAllBytes("dump", ((MemoryStream)writer.BaseStream).ToArray());
        }

        public static void DumpBuffer(MessageReader reader)
        {
            File.WriteAllBytes("dump", ((MemoryStream)reader.BaseStream).ToArray());
        }
    }
}
