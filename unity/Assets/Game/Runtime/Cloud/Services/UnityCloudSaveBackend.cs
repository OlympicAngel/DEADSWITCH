using System;
using System.Collections.Generic;
using Deadswitch.Game.Core;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;
using Unity.Services.Core;
using UnityEngine;

namespace Deadswitch.Game.Cloud
{
    /// <summary>
    /// Cloud backup through Unity Gaming Services Cloud Save (doc 10 s2). An anonymous player account is created on
    /// first use (backup only, nothing required to play). This assembly compiles only when com.unity.services.cloudsave
    /// is installed (versionDefines), and the project must be linked to a Unity Cloud project in the Editor.
    /// </summary>
    public sealed class UnityCloudSaveBackend : ICloudBackend
    {
        private const string Key = "deadswitch_save";

        private bool _ready;

        public bool Available => Application.internetReachability != NetworkReachability.NotReachable;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            Bootstrap.Booted += _ => CloudBackup.Backend = new UnityCloudSaveBackend();
        }

        public async void Upload(byte[] save, Action<string> done)
        {
            try
            {
                await SignIn();
                await CloudSaveService.Instance.Data.Player.SaveAsync(new Dictionary<string, object> { { Key, Convert.ToBase64String(save) } });
                done(null);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[DEADSWITCH] Cloud backup failed: " + ex.Message);
                done("Backup failed. The run is still saved on this device.");
            }
        }

        public async void Download(Action<byte[], string> done)
        {
            try
            {
                await SignIn();
                Dictionary<string, Item> data = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { Key });
                if (!data.TryGetValue(Key, out Item item))
                {
                    done(null, "No backup found.");
                    return;
                }

                done(Convert.FromBase64String(item.Value.GetAs<string>()), null);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[DEADSWITCH] Cloud restore failed: " + ex.Message);
                done(null, "Could not reach the backup.");
            }
        }

        private async System.Threading.Tasks.Task SignIn()
        {
            if (!_ready)
            {
                await UnityServices.InitializeAsync();
                _ready = true;
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
        }
    }
}
