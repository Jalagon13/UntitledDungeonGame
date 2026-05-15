using System.Collections.Generic;
using UnityEngine;

namespace UntitledDungeonGame
{
    public abstract class StateMachine
    {
        protected Dictionary<AIState, BaseState> _states = new();

        protected BaseState _currentState;
        public BaseState CurrentState => _currentState;
        
        protected bool _isTransitioningState = false;

        protected ServerCharacter _serverCharacter;
        public ServerCharacter ServerCharacter => _serverCharacter;

        public BaseState GetState(AIState key)
        {
            if (_states.TryGetValue(key, out var state))
            {
                return state;
            }

            Debug.LogWarning($"State {key} not found in state machine.");
            return null;
        }

        public void StartStateMachine()
        {
            _currentState?.EnterStateWithNetworkSync(new AIStateData(_currentState.StateKey, 0));
            _currentState.CurrentSubState?.EnterStateWithNetworkSync(new AIStateData(_currentState.CurrentSubState.StateKey, 0));
        }

        public virtual void UpdateAI()
        {
            if (_currentState == null) return;

            _currentState.UpdateAllStates();
        }

        public virtual void OwnerInitialization() { }
        public virtual void Dispose() { }

        public abstract void ReceiveHP(ServerCharacter inflicter, int amount);

        public void TransitionToState(AIStateData stateData)
        {
            _isTransitioningState = true;
            _currentState.ExitState();
            _currentState = _states[stateData.CurrentState];
            _currentState.EnterStateWithNetworkSync(stateData);
            _isTransitioningState = false;
        }
    }

    public enum AIState
    {
        None,

        // Super States
        Grounded,
        Attacking,
        SpellCasting,
        Dead, // Used for player death animation as well as npc death animation cleanup stuff before formally despawning

        // Sub States
        Idle,
        Moving,
        Knockbacked,
        Pursuing,
        Fleeing,
    }
}
