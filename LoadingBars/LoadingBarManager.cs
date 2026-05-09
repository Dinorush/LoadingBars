using GTFO.API;
using LevelGeneration;
using SNetwork;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Il2Collections = Il2CppSystem.Collections.Generic;

namespace LoadingBars
{
    public class LoadingBarManager
    {
        public static readonly LoadingBarManager Current = new();

        private ulong _localLookup = 0;
        private PlayerLoadingState _localState = new();
        public PlayerLoadingState LocalState => _localState;

        public static event Action<ulong, PlayerLoadingState>? OnPlayerUpdate = null;
        public static event Action<SNet_Player>? OnAddPlayer = null;
        public static event Action<SNet_Player>? OnRemovePlayer = null;

        private readonly Dictionary<ulong, PlayerLoadingState> _syncedStates = new();
        public static bool TryGetSyncedState(ulong lookup, [MaybeNullWhen(false)] out PlayerLoadingState state) => Current._syncedStates.TryGetValue(lookup, out state);

        private Il2Collections.Queue<LG_FactoryJob>? _currentQueue;

        internal static void Init()
        {
            LevelAPI.OnBuildStart += Current.OnBuildStart;
            LevelAPI.OnBuildDone += Current.OnBuildDone;
        }

        internal static void Clear()
        {
            Current._syncedStates.Clear();
            Current._localLookup = 0;
        }

        internal void ResetPlayers()
        {
            OnRemovePlayer?.Invoke(SNet.LocalPlayer);
            OnAddPlayer?.Invoke(SNet.LocalPlayer);

            foreach (ulong lookup in _syncedStates.Keys)
            {
                if (SNet.TryGetPlayer(lookup, out var player))
                {
                    OnRemovePlayer?.Invoke(player);
                    OnAddPlayer?.Invoke(player);
                }
            }
        }

        private void OnBuildStart()
        {
            foreach (var state in _syncedStates.Values)
                state.Reset();
            _localState.Reset();
            _currentQueue = null;
        }

        private void OnBuildDone()
        {
            _currentQueue = null;
            _localState.IsDone = true;
            OnPlayerUpdate?.Invoke(SNet.LocalPlayer.Lookup, _localState);
            LoadingBarNetwork.SendUpdate(_localState);
        }

        internal static void OnAddPlayer_Internal(SNet_Player player)
        {
            PlayerLoadingState state = new();
            if (player.IsLocal)
            {
                if (player.Lookup != Current._localLookup)
                {
                    Current._localLookup = player.Lookup;
                    OnAddPlayer?.Invoke(player);
                }
            }
            else if (Current._syncedStates.TryAdd(player.Lookup, state))
                OnAddPlayer?.Invoke(player);
        }

        internal static void OnRemovePlayer_Internal(SNet_Player player)
        {
            if (Current._syncedStates.Remove(player.Lookup))
                OnRemovePlayer?.Invoke(player);
        }

        internal static void OnSyncedUpdate_Internal(ulong lookup, PlayerLoadingState state)
        {
            Current._syncedStates[lookup] = state;
            OnPlayerUpdate?.Invoke(lookup, state);
        }

        internal static void OnBatchUpdate_Internal(LG_Factory factory) => Current.OnBatchUpdate_Instance(factory);
        private void OnBatchUpdate_Instance(LG_Factory factory)
        {
            if (_localState.Batch != factory.m_batchStep)
            {
                _localState.Batch = factory.m_batchStep;
                _localState.BatchCount = 0;
                _currentQueue = factory.m_currentBatch.Jobs;
            }
            else
                _localState.BatchCount++;

            _localState.BatchCountRemaining = _currentQueue!.Count;
            OnPlayerUpdate?.Invoke(SNet.LocalPlayer.Lookup, _localState);
            LoadingBarNetwork.SendUpdate(_localState);
        }
    }
}
