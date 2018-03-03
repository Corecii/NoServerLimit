using System;
using Spectrum.API;
using Spectrum.API.Configuration;
using Spectrum.Interop.Game;
using Spectrum.Interop.Game.Network;
using Spectrum.API.Interfaces.Plugins;
using Spectrum.API.Interfaces.Systems;
using UnityEngine;

namespace NoServerLimit
{
    public class Entry : IPlugin
    {
        public string FriendlyName => "Server Limit Unlocker";
        public string Author => "Ciastex";
        public string Contact => "ciastexx@live.com";
        public APILevel CompatibleAPILevel => APILevel.XRay;

        private Settings _settings;

        public void Initialize(IManager manager)
        {
            _settings = new Settings(typeof(Entry));
            ValidateSettings();

            MainMenu.Loaded += (sender, args) =>
            {
                var mainMenuObject = GameObject.Find("MainMenuFrontRoot");
                var mainMenuLogic = mainMenuObject?.GetComponent<MainMenuLogic>();
                var hostAGame = mainMenuLogic?.onlineMenuLogic_.hostAGamePanel_.GetComponent<HostAGame>();

                // most likely returned back to lobby from gamemode
                if (hostAGame == null)
                {
                    Console.WriteLine("Server Limit Unlocker: Not from main menu, trying fallback.");

                    var onlineMpPanel = GameObject.Find("OnlineMPFrontRoot");
                    var onlineMenuLogic = onlineMpPanel.GetComponent<OnlineMenuLogic>();

                    hostAGame = onlineMenuLogic?.hostAGamePanel_.GetComponent<HostAGame>();
                }

                var serverButton = hostAGame?.gameObject.transform.Find("Anchor - Center/UI Panel - Host a Game/StartServerButton");
                var serverButtonEx = serverButton?.GetComponents<UIExButton>();

                var maxPlayersNote = hostAGame?.gameObject.transform.Find("Anchor - Center/UI Panel - Host a Game/MaxPlayersNote");
                var maxPlayersNoteLabel = maxPlayersNote?.GetComponent<UILabel>();

                if (maxPlayersNoteLabel != null)
                {
                    maxPlayersNoteLabel.text = $"limited to 2-{_settings["MaxPlayerCount"]}";
                }

                if (hostAGame != null)
                {
                    var properInput = hostAGame.maxPlayersInput_ as UIExInputGeneric<string>;
                    properInput?.onFinish_.Clear();

                    properInput?.onFinish_.Add(new EventDelegate(() =>
                    {
                        int value;
                        if (!int.TryParse(properInput.value, out value))
                        {
                            properInput.value = "2";
                            return;
                        }

                        if (value > _settings.GetItem<int>("MaxPlayerCount"))
                        {
                            properInput.value = _settings.GetItem<int>("MaxPlayerCount").ToString();
                            return;
                        }

                        if (value < 2)
                        {
                            properInput.value = "2";
                        }
                    }));
                }
                else
                {
                    Console.WriteLine("Server Limit Unlocker: Couldn't get HostAGame component.");
                    return;
                }

                if (serverButtonEx != null)
                {
                    foreach (var button in serverButtonEx)
                    {
                        button.onClick.Clear();
                        button.onClick.Add(new EventDelegate(() =>
                        {
                            CreateServer(hostAGame.serverNameInput_.value, hostAGame.passwordInput_.value, int.Parse(hostAGame.maxPlayersInput_.value));
                        }));
                    }
                }
                else
                {
                    Console.WriteLine("Server Limit Unlocker: Couldn't get ServerButtonEx component list.");
                }
            };
        }

        private void CreateServer(string serverTitle, string password, int maxPlayerCount)
        {
            Network.InitializeSecurity();
            try
            {
                var networkingManager = G.Sys.NetworkingManager_;
                networkingManager.password_ = password;
                networkingManager.serverTitle_ = serverTitle;
                G.Sys.GameData_.SetString("ServerTitleDefault", networkingManager.serverTitle_);
                networkingManager.maxPlayerCount_ = maxPlayerCount;
                G.Sys.GameData_.SetInt("MaxPlayersDefault", networkingManager.maxPlayerCount_);
                int num = 1;
                int connections = networkingManager.maxPlayerCount_ - num;
                NetworkConnectionError networkConnectionError = Network.InitializeServer(connections, 32323, true);
                if (networkConnectionError != NetworkConnectionError.NoError)
                {
                    G.Sys.MenuPanelManager_.ShowError("Error code: " + networkConnectionError.ToString(), "Failed to start server", null, UIWidget.Pivot.Center);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
            }
        }

        public void Shutdown()
        {

        }

        private void ValidateSettings()
        {
            if (!_settings.ContainsKey("MaxPlayerCount"))
            {
                _settings["MaxPlayerCount"] = 32;
            }

            _settings.Save();
        }
    }
}
