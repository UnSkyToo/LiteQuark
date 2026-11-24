using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using LiteQuark.Runtime;
using UnityEngine;

namespace LiteQuark.Editor
{
    internal sealed partial class AddonsValidationRunner : BundleValidation
    {
        private sealed class FirstRow : IConfigData { public int Id { get; set; } }
        private sealed class SecondRow : IConfigData { public int Id { get; set; } }

        private static async UniTask ConfigReload()
        {
            using var content = new AssetScope();
            var config = new ConfigSystem { Setting = new ConfigSystemSetting { LoadAllOnInitialize = true } };
            var revision = 1;
            var parsed = new List<string>();
            config.RegisterTable<FirstRow>(TextPath, text =>
            {
                parsed.Add(text);
                if (revision == 3) throw new FormatException("Controlled config parser failure.");
                return new List<FirstRow> { new FirstRow { Id = revision } };
            });
            config.RegisterTable<SecondRow>(SecondTextPath, text =>
            {
                parsed.Add(text);
                return new List<SecondRow> { new SecondRow { Id = revision } };
            });
            try
            {
                var init = config.Initialize();
                await Wait(() => init.Status.IsCompleted(), "Auto-loaded config");
                Check(await init && parsed.SequenceEqual(new[] { "LiteQuark session validation", "Second asset in the shared text bundle" }) && config.GetData<FirstRow>(1) != null, "Registered tables parse in order.");
                revision = 2;
                await config.Load<FirstRow>();
                var published = config.GetTable<FirstRow>();
                Check(config.TryGetData<FirstRow>(2, out var row) && row.Id == 2, "Successful reload publishes the new row.");
                revision = 3;
                await config.LoadAll();
                Check(ReferenceEquals(published, config.GetTable<FirstRow>()) && config.GetData<SecondRow>(3) != null &&
                    content.Content.Refs("text.ab") == 0, "Parser failure preserves old table, continues batch and returns temporary handles.");
            }
            finally { await config.DisposeAsync(); }
        }

        private static async UniTask ConfigClose()
        {
            using var content = new AssetScope();
            var config = new ConfigSystem { Setting = new ConfigSystemSetting() };
            await config.Initialize();
            var parses = 0;
            config.RegisterTable<FirstRow>(TextPath, _ => { parses++; return new List<FirstRow>(); });
            var retained = content.Content.Asset<TextAsset>(TextPath);
            var batch = config.LoadAll().Preserve();
            var single = config.Load<FirstRow>().Preserve();
            await config.DisposeAsync();
            foreach (var task in new[] { batch, single })
            {
                var cancelled = false;
                try { await task; } catch (OperationCanceledException) { cancelled = true; }
                Check(cancelled, "Closing cancels accepted config consumers.");
            }
            await Wait(() => retained.IsDone, "Shared late TextAsset");
            Check(retained.Result != null && parses == 0,
                "Only the independent owner receives the late asset and the cancelled loads skip parsing.");
        }
    }
}
