﻿using CoCSharp.Logic;
using System;

namespace CoCSharp.Networking.Messages.Commands
{
    /// <summary>
    /// Command that is sent by the client to the server to tell
    /// it that a building was bought.
    /// </summary>
    public class BuyBuildingCommand : Command
    {
        /// <summary>
        /// Intializes a new instance of the <see cref="BuyBuildingCommand"/> class.
        /// </summary>
        public BuyBuildingCommand()
        {
            // Space
        }

        /// <summary>
        /// Gets the ID of the <see cref="BuyBuildingCommand"/>.
        /// </summary>
        public override int ID { get { return 500; } }

        /// <summary>
        /// X coordinates of the building.
        /// </summary>
        public int X;
        /// <summary>
        /// Y coordinates of the building.
        /// </summary>
        public int Y;
        /// <summary>
        /// Data ID of the building that was bought.
        /// </summary>
        public int BuildingDataID;

        /// <summary>
        /// Unknown integer 1.
        /// </summary>
        public int Unknown1;

        /// <summary>
        /// Reads the <see cref="BuyBuildingCommand"/> from the specified <see cref="MessageReader"/>.
        /// </summary>
        /// <param name="reader">
        /// <see cref="MessageReader"/> that will be used to read the <see cref="BuyBuildingCommand"/>.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="reader"/> is null.</exception>
        /// <exception cref="InvalidCommandException"><see cref="BuildingDataID"/> is invalid.</exception>
        public override void ReadCommand(MessageReader reader)
        {
            ThrowIfReaderNull(reader);

            X = reader.ReadInt32();
            Y = reader.ReadInt32();
            BuildingDataID = reader.ReadInt32();

            Unknown1 = reader.ReadInt32(); // 4746

            if (!IDConverter.IsValidData<Building>(BuildingDataID))
                throw new InvalidCommandException("Unexpected data ID: " + BuildingDataID, this);
        }

        /// <summary>
        /// Writes the <see cref="BuyBuildingCommand"/> to the specified <see cref="MessageWriter"/>.
        /// </summary>
        /// <param name="writer">
        /// <see cref="MessageWriter"/> that will be used to write the <see cref="BuyBuildingCommand"/>.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="writer"/> is null.</exception>
        public override void WriteCommand(MessageWriter writer)
        {
            ThrowIfWriterNull(writer);

            writer.Write(X);
            writer.Write(Y);
            writer.Write(BuildingDataID);

            writer.Write(Unknown1);
        }
    }
}
