using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using LiteQuark.Runtime;
using UnityEngine;

namespace LiteQuark.Editor
{
    internal sealed partial class AddonsValidationRunner : BundleValidation
    {
        private static async UniTask Audio()
        {
            using var content = new AssetScope();
            var system = new AudioSystem();
            var pause = AudioListener.pause;
            await system.Initialize();
            try
            {
                var id = system.PlaySound(AudioPath, true);
                var sound = system.GetAudio(id);
                await Wait(() => sound.IsLoaded, "Real clip delivery");
                var carrier = sound.Carrier;
                Check(carrier != null && sound.Source.clip != null, "Loaded sound owns a real carrier and clip.");
                system.StopAudio(id);
                system.StopAudio(id);
                system.Tick(0);
                LiteRuntime.ObjectPool.RemoveUnusedPools();
                Check(sound.Carrier == null && content.Content.Refs("audio.ab") == 0, "Repeated stop releases the clip and carrier.");
                content.Content.Assets.UnloadUnusedAssets();
                var pending = system.GetAudio(system.PlayMusic(AudioPath));
                var independent = content.Content.Asset<AudioClip>(AudioPath);
                Check(!pending.IsLoaded, "Replacement play is awaiting the shared clip.");
                system.PauseAllAudio();
                await system.DisposeAsync();
                await Wait(() => independent.IsDone && carrier == null, "Clip settles and audio carrier is destroyed");
                Check(independent.Result != null && !pending.IsLoaded && pending.Carrier == null && AudioListener.pause == pause,
                    "Late delivery remains with the independent owner and shutdown restores listener pause.");
            }
            finally { await system.DisposeAsync(); AudioListener.pause = pause; }
        }
    }
}
