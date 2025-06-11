namespace Fusion.Addons.FSM
{
    //FIXME: 這段要留在這？需要fusion耶，應該要拆走
    public unsafe ref struct InterpolationData
    {
        public NetworkBehaviourBuffer FromBuffer;
        public NetworkBehaviourBuffer ToBuffer;
        public int From;
        public int To;
        public float Alpha;
    }

    public partial class StateMachine<TState>
    {
        // PRIVATE MEMBERS

        private int _activeStateId;
        private int _previousStateId;
        private int _defaultStateId;
        private int _stateChangeTick;
        private int _bitState;

        private int _wordCount;
        private bool _statesContainData;
        private bool _hasChildMachines;

        // IStateMachine INTERFACE

        IState[] IStateMachine.States => _states;
        IState IStateMachine.ActiveState => ActiveState;
        IState IStateMachine.PreviousState => PreviousState;

        int IStateMachine.WordCount
        {
            get
            {
                if (_wordCount == 0)
                    // CollectChildMachines();
                    _wordCount = GetWordCount();

                return _wordCount;
            }
        }

        unsafe void IStateMachine.Read(int* ptr)
        {
            _activeStateId = *ptr;
            ptr++;

            _previousStateId = *ptr;
            ptr++;

            _defaultStateId = *ptr;
            ptr++;

            _stateChangeTick = *ptr;
            ptr++;

            _bitState = *ptr;
            ptr++;

            if (_statesContainData == true)
                for (var i = 0; i < _stateCount; i++)
                {
                    var state = _states[i];

                    state.Read(ptr);
                    ptr += state.GetWordCount();
                }

            ReadUserData(ptr);

            // if (_hasChildMachines == true)
            // {
            // 	for (int i = 0; i < _stateCount; i++)
            // 	{
            // 		var state = _states[i];
            //
            // 		for (int j = 0; j < state.ChildMachines.Length; j++)
            // 		{
            // 			var childMachine = state.ChildMachines[j];
            //
            // 			childMachine.Read(ptr);
            // 			ptr += childMachine.WordCount;
            // 		}
            // 	}
            // }
        }

        unsafe void IStateMachine.Write(int* ptr)
        {
            *ptr = _activeStateId;
            ptr++;

            *ptr = _previousStateId;
            ptr++;

            *ptr = _defaultStateId;
            ptr++;

            *ptr = _stateChangeTick;
            ptr++;

            *ptr = _bitState;
            ptr++;

            if (_statesContainData == true)
                for (var i = 0; i < _stateCount; i++)
                {
                    var state = _states[i];

                    state.Write(ptr);
                    ptr += state.GetWordCount();
                }

            WriteUserData(ptr);

            // if (_hasChildMachines == true)
            // {
            // 	for (int i = 0; i < _stateCount; i++)
            // 	{
            // 		var state = _states[i];
            //
            // 		for (int j = 0; j < state.ChildMachines.Length; j++)
            // 		{
            // 			var childMachine = state.ChildMachines[j];
            //
            // 			childMachine.Write(ptr);
            // 			ptr += childMachine.WordCount;
            // 		}
            // 	}
            // }
        }

        unsafe void IStateMachine.Interpolate(InterpolationData interpolationData)
        {
            // Save interpolation float tick so we can calculate correct state time when asked from Render-related methods
            _interpolationTick = UnityEngine.Mathf.Lerp(interpolationData.FromBuffer.Tick,
                interpolationData.ToBuffer.Tick, interpolationData.Alpha);

            // We prefer using only From values
            var useFrom = true;

            _activeStateId = useFrom == true
                ? interpolationData.FromBuffer[interpolationData.From]
                : interpolationData.ToBuffer[interpolationData.To];
            interpolationData.From++;
            interpolationData.To++;

            _previousStateId = useFrom == true
                ? interpolationData.FromBuffer[interpolationData.From]
                : interpolationData.ToBuffer[interpolationData.To];
            interpolationData.From++;
            interpolationData.To++;

            _defaultStateId = useFrom == true
                ? interpolationData.FromBuffer[interpolationData.From]
                : interpolationData.ToBuffer[interpolationData.To];
            interpolationData.From++;
            interpolationData.To++;

            _stateChangeTick = useFrom == true
                ? interpolationData.FromBuffer[interpolationData.From]
                : interpolationData.ToBuffer[interpolationData.To];
            interpolationData.From++;
            interpolationData.To++;

            _bitState = useFrom == true
                ? interpolationData.FromBuffer[interpolationData.From]
                : interpolationData.ToBuffer[interpolationData.To];
            interpolationData.From++;
            interpolationData.To++;

            if (_statesContainData == true)
                for (var i = 0; i < _stateCount; i++)
                {
                    var state = _states[i];
                    var wordCount = state.GetWordCount();

                    state.Interpolate(interpolationData);

                    interpolationData.From += wordCount;
                    interpolationData.To += wordCount;
                }

            InterpolateUserData(interpolationData);

            // if (_hasChildMachines == true)
            // {
            // 	for (int i = 0; i < _stateCount; i++)
            // 	{
            // 		var state = _states[i];
            //
            // 		for (int j = 0; j < state.ChildMachines.Length; j++)
            // 		{
            // 			var childMachine = state.ChildMachines[j];
            //
            // 			childMachine.Interpolate(interpolationData);
            //
            // 			interpolationData.From += childMachine.WordCount;
            // 			interpolationData.To   += childMachine.WordCount;
            // 		}
            // 	}
            // }
        }

        // PROTECTED METHODS

        protected virtual int GetUserDataWordCount()
        {
            return 0;
        }

        protected virtual unsafe void ReadUserData(int* ptr)
        {
        }

        protected virtual unsafe void WriteUserData(int* ptr)
        {
        }

        protected virtual void InterpolateUserData(InterpolationData interpolationData)
        {
        }

        // PRIVATE METHODS

        // private void CollectChildMachines()
        // {
        //     _hasChildMachines = false;
        //
        //     var childMachines = ListPool.Get<IStateMachine>(8);
        //
        //     for (var i = 0; i < _stateCount; i++)
        //     {
        //         var state = _states[i];
        //
        //         childMachines.Clear();
        //
        //         state.CollectChildStateMachines(childMachines);
        //         state.ChildMachines = childMachines.ToArray();
        //
        //         _hasChildMachines |= childMachines.Count > 0;
        //     }
        //
        //     ListPool.Return(childMachines);
        // }

        private int GetWordCount()
        {
            var count = 5; // StateMachine data
            count += GetUserDataWordCount();

            for (var i = 0; i < _stateCount; i++)
            {
                var state = _states[i];

                var stateWordCount = state.GetWordCount();
                _statesContainData |= stateWordCount > 0;

                count += stateWordCount;

                // for (var j = 0; j < state.ChildMachines.Length; j++) count += state.ChildMachines[j].WordCount;
            }

            return count;
        }
    }
}