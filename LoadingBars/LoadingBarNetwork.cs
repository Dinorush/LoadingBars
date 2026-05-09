using GTFO.API;
using SNetwork;
using System.Collections.Generic;

namespace LoadingBars
{
    internal class LoadingBarNetwork
    {
        private const string UpdateEvent = "LoadingUpdate";
        private const string HasModEvent = "LoadingHasMod";
        private const float SyncInterval = 0.25f;

        private static readonly Dictionary<ulong, SNet_Player> _modPlayers = new();
        private static float _lastSyncTime = 0;

        internal static void Init()
        {
            NetworkAPI.RegisterEvent<PlayerLoadingState>(UpdateEvent, ReceiveUpdate);
            NetworkAPI.RegisterEvent<bool>(HasModEvent, ReceiveHasMod);
        }

        internal static void Clear()
        {
            _modPlayers.Clear();
        }

        internal static void SendUpdate(PlayerLoadingState state)
        {
            float time = Clock.Time;
            if (!state.IsDone && time - _lastSyncTime < SyncInterval) return;

            _lastSyncTime = time;
            foreach (var player in _modPlayers.Values)
                NetworkAPI.InvokeEvent(UpdateEvent, state, player, SNet_ChannelType.SessionOrderCritical);
        }

        private static void ReceiveUpdate(ulong lookup, PlayerLoadingState state)
        {
            LoadingBarManager.OnSyncedUpdate_Internal(lookup, state);
        }

        internal static void OnAddPlayer(SNet_Player player)
        {
            if (player.IsLocal)
                LoadingBarManager.OnAddPlayer_Internal(player);
            else
                NetworkAPI.InvokeEvent(HasModEvent, true, player, SNet_ChannelType.SessionOrderCritical);
        }

        internal static void SendHasModResponse(SNet_Player player)
        {
            NetworkAPI.InvokeEvent(HasModEvent, false, player, SNet_ChannelType.SessionOrderCritical);
        }

        private static void ReceiveHasMod(ulong lookup, bool _)
        {
            if (SNet.TryGetPlayer(lookup, out var player) && _modPlayers.TryAdd(lookup, player))
                LoadingBarManager.OnAddPlayer_Internal(player);
        }

        internal static void OnRemovePlayer(SNet_Player player)
        {
            if (_modPlayers.Remove(player.Lookup))
                LoadingBarManager.OnRemovePlayer_Internal(player);
        }
    }
}
