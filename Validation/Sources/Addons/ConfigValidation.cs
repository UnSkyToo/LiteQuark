using System;
using System.Collections.Generic;
using System.Threading;
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
            var config = new ConfigSystem();
            await config.Initialize();
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
                await config.LoadAll();
                Check(parsed.Count == 2 && parsed.Contains("LiteQuark session validation") &&
                    parsed.Contains("Second asset in the shared text bundle") && config.GetData<FirstRow>(1) != null,
                    "Explicit loading after system initialization publishes both text tables.");
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

        private static async UniTask ConfigFormats()
        {
            using var content = new AssetScope(manual: true);
            var config = new ConfigSystem();
            var binaryParsed = false;
            await config.Initialize();
            try
            {
                config.RegisterTable<(string Code, string Value), string>(TextPath,
                    asset => new() { ("title", asset.text) }, row => row.Code);
                config.RegisterTable<(byte Region, byte Code, byte Value), (byte Region, byte Code)>(BinaryConfigPath, asset =>
                {
                    var bytes = asset.bytes;
                    binaryParsed = true;
                    return new() { (bytes[0], bytes[1], bytes[2]), (bytes[3], bytes[4], bytes[5]) };
                }, row => (row.Region, row.Code));

                var loading = config.LoadAll();
                await Wait(() => content.Content.Locater.Manual.ContainsKey("text.ab") &&
                    content.Content.Locater.Manual.ContainsKey("config.ab"), "Both config Bundles are in flight");
                content.Content.Finish("config.ab", true);
                await Wait(() => binaryParsed, "Binary config while text is still pending");
                var binary = config.GetTable<(byte Region, byte Code, byte Value), (byte Region, byte Code)>();
                Check(!loading.Status.IsCompleted() && binary.TryGet((7, 128), out var first) && first.Value == 255 &&
                    binary.TryGet((7, 129), out var second) && second.Value == 0,
                    "The binary table publishes byte values under a composite key while the text Bundle waits.");

                content.Content.Finish("text.ab", true);
                await loading;
                var text = config.GetTable<(string Code, string Value), string>();
                Check(text.TryGet("title", out var title) && title.Value == "LiteQuark session validation" &&
                    content.Content.Refs("text.ab") == 0 && content.Content.Refs("config.ab") == 0,
                    "The text table uses a string key and both loaders return their asset handles.");
            }
            finally { await config.DisposeAsync(); }
        }

        private static async UniTask ConfigClose()
        {
            using var content = new AssetScope();
            var config = new ConfigSystem();
            await config.Initialize();
            var parses = 0;
            config.RegisterTable<FirstRow>(TextPath, _ => { parses++; return new List<FirstRow>(); });
            config.RegisterTable<SecondRow>(SecondTextPath, _ => { parses++; return new List<SecondRow>(); });
            var retained = content.Content.Asset<TextAsset>(TextPath);
            using var ct = new CancellationTokenSource();
            var startup = config.LoadAll(ct.Token).Preserve();
            var batch = config.LoadAll().Preserve();
            var single = config.Load<FirstRow>().Preserve();
            ct.Cancel();
            Check(startup.Status == UniTaskStatus.Canceled && batch.Status == UniTaskStatus.Pending,
                "Canceling a startup batch ends its wait independently of concurrent loads.");
            await config.DisposeAsync();
            foreach (var task in new[] { startup, batch, single })
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
