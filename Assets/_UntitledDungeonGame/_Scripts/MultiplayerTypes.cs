using System;
using Unity.Netcode;
using UnityEngine;

namespace UntitledDungeonGame
{
    public struct SyncItemData : IEquatable<SyncItemData>, INetworkSerializable
    {
        public ushort ItemId;
        public int Quantity;

        public bool Equals(SyncItemData other)
        {
            // Check if basic properties are equal
            if (ItemId != other.ItemId || Quantity != other.Quantity)
                return false;

            return true;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ItemId);
            serializer.SerializeValue(ref Quantity);
        }
    }
}