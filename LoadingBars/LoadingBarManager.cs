using GTFO.API;
using LevelGeneration;
using SNetwork;
using System;
using Il2Collections = Il2CppSystem.Collections.Generic;

namespace LoadingBars
{
    public static class LoadingBarManager
    {
        private static PlayerLoadingState _localState = new();
        public static PlayerLoadingState LocalState => _localState;

        public static event Action<ulong, PlayerLoadingState>? OnPlayerUpdate = null;
        public static event Action<SNet_Player>? OnAddPlayer = null;
        public static event Action<SNet_Player>? OnRemovePlayer = null;

        private static Il2Collections.Queue<LG_FactoryJob>? _currentQueue;

        internal static void Init()
        {
            LevelAPI.OnBuildStart += OnBuildStart;
            LevelAPI.OnBuildDone += OnBuildDone;
            LoadingBarNetwork.ModInfo.OnPlayerModSet += OnPlayerModSet;
            LoadingBarNetwork.ModInfo.OnPlayerLeft += RemovePlayer;
        }

        internal static void ResetPlayers()
        {
            OnRemovePlayer?.Invoke(SNet.LocalPlayer);
            OnAddPlayer?.Invoke(SNet.LocalPlayer);

            foreach (var player in LoadingBarNetwork.ModInfo.OtherPlayers)
            {
                OnRemovePlayer?.Invoke(player);
                OnAddPlayer?.Invoke(player);
            }
        }

        private static void OnBuildStart()
        {
            _localState.Reset();
            _currentQueue = null;
        }

        private static void OnBuildDone()
        {
            _currentQueue = null;
            _localState.IsDone = true;
            OnPlayerUpdate?.Invoke(SNet.LocalPlayer.Lookup, _localState);
            LoadingBarNetwork.SendUpdate(_localState);
        }

        private static void OnPlayerModSet(SNet_Player player, bool hasMod)
        {
            if (hasMod)
                AddPlayer(player);
            else
                RemovePlayer(player);
        }

        private static void AddPlayer(SNet_Player player) => OnAddPlayer?.Invoke(player);
        private static void RemovePlayer(SNet_Player player) => OnRemovePlayer?.Invoke(player);

        internal static void OnSyncedUpdate_Internal(ulong lookup, PlayerLoadingState state)
        {
            OnPlayerUpdate?.Invoke(lookup, state);
        }

        internal static void OnBatchUpdate_Internal(LG_Factory factory)
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
