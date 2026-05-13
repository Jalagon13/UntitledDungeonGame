using UnityEngine;

namespace UntitledDungeonGame
{
    public abstract class BaseState
    {
        protected BaseState _currentSubState;
        public BaseState CurrentSubState => _currentSubState;
        private BaseState _currentSuperState;

        private bool _isSuperState = false;
        protected bool IsSuperState { set { _isSuperState = value; } }

        protected StateMachine Context { get; private set; }
        public AIState StateKey { get; private set; }

        public BaseState(AIState key, StateMachine context)
        {
            StateKey = key;
            Context = context;
        }

        public void EnterStateWithNetworkSync(AIStateData newStateData)
        {
            EnterState(newStateData);

            if (_isSuperState)
            {
                Context.ServerCharacter.SuperAIState.Value = newStateData;
            }
            else
            {
                Context.ServerCharacter.SubAIState.Value = newStateData;
            }
        }

        public virtual void ClientEnterState(AIStateData stateData) { }
        public virtual void ClientUpdateState(AIStateData stateData) { }
        public virtual void ClientExitState(AIStateData stateData) { }

        protected abstract void EnterState(AIStateData stateData);
        public abstract void UpdateState();
        public abstract void ExitState();
        public abstract void CheckSwitchStates();

        protected void SwitchState(AIStateData newStateData)
        {
            var newState = Context.GetState(newStateData.CurrentState);
            if (newState == this) return;

            // ExitState();

            if (_isSuperState)
            {
                Context.TransitionToState(newStateData); // This handles EnterState
            }
            else
            {
                newState.EnterStateWithNetworkSync(newStateData); // Only call EnterState directly for substates
                _currentSuperState?.SetSubState(newStateData.CurrentState);
            }
        }

        public void UpdateAllStates()
        {
            UpdateState();
            CheckSwitchStates();
            _currentSubState?.UpdateAllStates();
        }

        protected void SetSuperState(BaseState state)
        {
            _currentSuperState = state;
        }

        protected void SetSubState(AIState aiState)
        {
            var state = Context.GetState(aiState);

            _currentSubState = state;
            state.SetSuperState(this);
        }
    }
}
