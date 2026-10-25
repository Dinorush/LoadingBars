using CellMenu;
using FluffyUnderware.DevTools.Extensions;
using Player;
using SNetwork;
using System.Collections.Generic;
using UnityEngine;

namespace LoadingBars
{
    public class LoadingBarUI
    {
        public static Dictionary<ulong, LoadingBarState> _playerBars = new();
        public static void Init()
        {
            LoadingBarManager.OnAddPlayer += RegisterPlayer;
            LoadingBarManager.OnRemovePlayer += OnPlayerLeave;
            LoadingBarManager.OnPlayerUpdate += OnPlayerUpdate;
            LoadingBarNetwork.ModInfo.OnLobbyLeft += Clear;
        }
        private static readonly string m_prefabPath = "CM_TimedButton";
        public static void RegisterPlayer(SNet_Player player)
        {
            if (player.IsBot) return; // Bots load same as host

            if (_playerBars.ContainsKey(player.Lookup))
            {
                DinoLogger.Warning($"Player {player.NickName} already has a registered loading bar??");
                return;
            }

            var jobBar = CreateAndStyleBar();
            jobBar.name = "JobProgress";
            jobBar.gameObject.SetActive(false);

            var batchBar = CreateAndStyleBar();
            batchBar.name = "BatchProgress";
            batchBar.gameObject.SetActive(false);

            GameObject holder = new GameObject("Loading Bar Holder");
            holder.transform.SetParent(jobBar.transform.parent, false);
            jobBar.transform.SetParent(holder.transform, false);
            batchBar.transform.SetParent(holder.transform, false);

            _playerBars.Add(player.Lookup, new LoadingBarState(player, holder, jobBar, batchBar));

            // SNet_Players don't have PlayerAgents set up at this point of creation
            // So disable the bars until we can put them on the nav markers for tracking later
            holder.gameObject.SetActive(false);
            // Use .gameObject.SetActive instead of .SetVisible because BlinkOut does as well
        }
        private static CM_TimedButton CreateAndStyleBar()
        {
            var layer = GuiManager.NavMarkerLayer;
            var btn = layer.AddRectComp(m_prefabPath).TryCast<CM_TimedButton>()!;

            int sizeX = 300;

            btn.SetSize(new Vector2(sizeX, 30));
            btn.SetButtonEnabled(false);

            // the background sprite on this prefab is normally set to null, so set it manually
            btn.m_backgroundSprite = btn.transform.Find("Background").GetComponent<SpriteRenderer>();
            //btn.m_backgroundSprite.color = new Color(0.2f, 0.2f, 0.2f, 0.2f);
            btn.m_backgroundSprite.color = Color.grey;

            btn.m_holdBtnProgressSprite.sprite = SpriteAsset.TopLeftPivotSprite;
            btn.m_holdBtnProgressSprite.size = new Vector2(sizeX-6, 24);
            btn.m_holdBtnProgressSprite.color = new Color(0.65f, 0.7f, 0.8f);
            //btn.m_holdBtnProgressSprite.color = playerColor;
            //btn.m_holdBtnProgressSprite.color = Color.Lerp(playerColor, new Color(0.7f, 0.7f, 0.7f), 0.33f);
            btn.m_holdBtnProgressSprite.sortingOrder = 1;
            btn.m_holdBtnProgressSprite.enabled = true;

            // bunch of minor position and rect changes for styling
            var rectTrans = btn.m_holdBtnProgressSprite.transform.TryCast<RectTransform>()!;
            rectTrans.anchoredPosition = new Vector2(2, -3);
            rectTrans.anchorMax = new Vector2(1, 1);
            rectTrans.anchorMin = new Vector2(0, 0);
            rectTrans.pivot = new Vector2(0, 1);

            btn.m_btnText.autoSizeTextContainer = true;
            //btn.m_btnText.color = new Color(0.25f, 0.25f, 0.25f, 1f);
            btn.m_btnText.color = Color.white;
            btn.m_btnText.sortingOrder = 2;
            btn.m_btnText.verticalAlignment = TMPro.VerticalAlignmentOptions.Middle;
            btn.m_btnText.fontStyle = TMPro.FontStyles.Normal | TMPro.FontStyles.Bold;
            btn.m_btnText.SetText("");
            btn.m_btnText.fontSize = 18;
            btn.m_btnText.lineSpacing = 30;

            // double the anchor size for text over/underflowing
            rectTrans = btn.m_btnText.transform.TryCast<RectTransform>()!;
            rectTrans.anchorMin = new Vector2(-1, 1);
            rectTrans.anchorMax = new Vector2(2, 1);
            rectTrans.localPosition = Vector3.zero;

            return btn;
        }

        public static void Clear()
        {
            foreach (var key in _playerBars.Keys)
                OnPlayerLeave(key);
        }

        public static void OnPlayerLeave(SNet_Player player)
        => OnPlayerLeave(player.Lookup);

        public static void OnPlayerLeave(ulong lookup)
        {
            if (_playerBars.Remove(lookup, out var bar))
            {
                bar.Destroy();
            }
        }

        public static void OnPlayerUpdate(ulong playerLookup, PlayerLoadingState state)
        {
            if (!_playerBars.TryGetValue(playerLookup, out var barState))
            {
                DinoLogger.Warning($"Didn't get player bars for {playerLookup}");
                return;
            }

            barState.Update(state);
        }

        public static void SetBarsActive(bool active)
        {
            foreach (var bar in _playerBars.Values)
                if (bar.Holder != null)
                    bar.Holder.SetActive(active && !bar.IsDone);
        }

        public class LoadingBarState
        {
            public GameObject Holder;
            private readonly SNet_Player _player;
            private NavMarker _marker = null!;
            private readonly CM_TimedButton _jobBar;
            private readonly CM_TimedButton _batchBar;
            private bool _isSetup = false;
            public LoadingBarState(SNet_Player player, GameObject holder, CM_TimedButton jobBar, CM_TimedButton batchBar)
            {
                _player = player;
                Holder = holder;
                _jobBar = jobBar;
                _batchBar = batchBar;
            }

            internal bool IsDone = false;
            public void Update(PlayerLoadingState state)
            {
                if (!_isSetup)
                    Setup();

                if (IsDone) return;

                float progressThruBatches = (float)(state.Batch + 1) / PlayerLoadingState.NumBatches;
                _batchBar.m_holdBtnProgressSprite.transform.localScale = new Vector3(progressThruBatches, 1, 1);
                _batchBar.m_btnText.SetText($"Batch {state.BatchName}\n{state.Batch + 1} / {PlayerLoadingState.NumBatches}\n ");

                _jobBar.m_holdBtnProgressSprite.transform.localScale = new Vector3(state.BatchProgress, 1, 1);
                _jobBar.m_btnText.SetText($"Jobs\n{state.BatchCount} / {state.BatchCountMax}\n ");

                if (state.IsDone)
                {
                    CoroutineManager.BlinkOut(Holder);
                    IsDone = true;
                }
            }

            private void Setup()
            {
                if (_player.IsLocal) // don't put local player load bar on nav marker
                {
                    _batchBar.transform.localPosition = new Vector3(0, -200, 0);
                    _jobBar.transform.localPosition = new Vector3(0, -300, 0);
                }
                else
                {
                    if (!_player.HasPlayerAgent)
                    {
                        DinoLogger.Warning("Still don't have nav marker >:(");
                        return;
                    }

                    GameObject go = _player.PlayerAgent.Cast<PlayerAgent>().PlayerSyncModel.m_tentacleTarget.gameObject;
                    var marker = GuiManager.NavMarkerLayer.PlaceCustomMarker(NavMarkerOption.Empty, go, "");
                    marker.SetPinEnabled(false);

                    Holder.transform.SetParent(marker.transform, false);
                    _batchBar.transform.localPosition = new Vector3(0, 100, 0);
                    _jobBar.transform.localPosition = new Vector3(0, 0, 0);

                    marker.m_isInPlayerDimension = true;
                    _marker = marker;
                }

                _batchBar.gameObject.SetActive(true);
                _jobBar.gameObject.SetActive(true);

                // defer color setting to here because players don't have color at the time they join lobby
                ColorMarkers(_player.PlayerColor);

                _isSetup = true;

                Holder.SetActive(true);

                GuiManager.NavMarkerLayer.SetVisible(true);
            }

            private void ColorMarkers(Color color)
            {
                color = Color.Lerp(color, Color.white, 0.33f);
                _jobBar.m_holdBtnProgressSprite.color = color;
                _batchBar.m_holdBtnProgressSprite.color = color;
            }

            public void Destroy()
            {
                Holder.Destroy();
                GuiManager.NavMarkerLayer.RemoveMarker(_marker);
            }
        }
    }

    // The default sprite on the prefab we're using has its pivot as the top center, which means it will fill outwards on both sides
    // Since a loading bar generally fills from left to right, we need to replace it with one that pivots from the left
    // The first one I can think of comes from the ready button on the loadout screen, which should definitely be loaded by the time this gets called once
    internal static class SpriteAsset
    {
        internal static Sprite TopLeftPivotSprite;

        static SpriteAsset()
        {
            TopLeftPivotSprite = GuiManager.MainMenuLayer.PageLoadout.m_readyButton.m_holdBtnProgressSprite.sprite;
        }
    }
}
