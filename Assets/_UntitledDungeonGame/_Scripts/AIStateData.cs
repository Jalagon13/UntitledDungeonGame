using System;
using Unity.Netcode;
using UnityEngine;

namespace UntitledDungeonGame
{
    public struct AIStateData : IEquatable<AIStateData>, INetworkSerializable
    {
        public AIState CurrentState;

        // Single payload; using Vector3 so you can expand later if needed.
        // .x is the “amount”, .y/.z are free for future use or ignored.
        public Vector3 Payload;

        public float Amount
        {
            get => Payload.x;
            set => Payload.x = value;
        }

        public AIStateData(AIState currentState)
        {
            CurrentState = currentState;
            Payload = default; // all zero
        }

        public AIStateData(AIState currentState, float amount)
        {
            CurrentState = currentState;
            Payload = new Vector3(amount, 0f, 0f);
        }

        public AIStateData(AIState currentState, Vector3 payload)
        {
            CurrentState = currentState;
            Payload = payload;
        }

        public bool Equals(AIStateData other)
        {
            return CurrentState == other.CurrentState && Payload == other.Payload;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref CurrentState);
            serializer.SerializeValue(ref Payload);
        }
    }
}
