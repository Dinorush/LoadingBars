using OptionalNetworking;
using SNetwork;

namespace LoadingBars
{
    internal class LoadingBarNetwork
    {
        private const float SyncInterval = 0.25f;

        public static readonly ModInfo ModInfo;
        private static readonly CustomDataSender<PlayerLoadingState> _sender;
        private static float _lastSyncTime = 0;

        static LoadingBarNetwork()
        {
            ModInfo = OptionalNetworkAPI.RegisterMod(EntryPoint.MODNAME);
            _sender = ModInfo.AddCustomDataSender<PlayerLoadingState>(ReceiveUpdate);
        }

        internal static void Init()
        {
        }

        internal static void SendUpdate(PlayerLoadingState state)
        {
            float time = Clock.Time;
            if (!state.IsDone && time - _lastSyncTime < SyncInterval) return;

            _lastSyncTime = time;
            _sender.SendData(state);
        }

        private static void ReceiveUpdate(SNet_Player player, PlayerLoadingState state)
        {
            LoadingBarManager.OnSyncedUpdate_Internal(player.Lookup, state);
        }
    }
}
