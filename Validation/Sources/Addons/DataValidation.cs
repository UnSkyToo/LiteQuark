using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using LiteQuark.Runtime;
using UnityEngine;

namespace LiteQuark.Editor
{
    internal sealed partial class AddonsValidationRunner : BundleValidation
    {
        private sealed class DataProviderProbe : IDataProvider
        {
            public readonly Dictionary<string, object> Values = new();
            public UniTaskCompletionSource InitializeGate;
            public UniTaskCompletionSource WriteGate;
            public UniTaskCompletionSource ReadGate;
            public bool InitializeResult = true;

            public Exception WriteError;
            public Exception DisposeError;

            public int Initializations;
            public int InitializeThread;
            public int Disposals;
            public int DisposeThread;

            public async UniTask<bool> Initialize()
            {
                Initializations++;
                if (InitializeGate != null) await InitializeGate.Task;
                InitializeThread = Thread.CurrentThread.ManagedThreadId;

                return InitializeResult;
            }

            public void Save<T>(string key, T data) => Values[key] = data;
            public async UniTask SaveAsync<T>(string key, T data)
            {
                if (WriteGate != null) await WriteGate.Task;
                if (WriteError != null) throw WriteError;
                Save(key, data);
            }
            public T Load<T>(string key, T defaultValue = default) => Values.TryGetValue(key, out var value) ? (T)value : defaultValue;
            public async UniTask<T> LoadAsync<T>(string key, T defaultValue = default)
            {
                if (ReadGate != null) await ReadGate.Task;
                return Load(key, defaultValue);
            }
            public bool Has(string key) => Values.ContainsKey(key);
            public void Delete(string key) => Values.Remove(key);
            public void DeleteAll() => Values.Clear();
            public void Dispose()
            {
                Disposals++;
                DisposeThread = Thread.CurrentThread.ManagedThreadId;

                if (DisposeError != null) throw DisposeError;
            }
        }

        private static async UniTask<DataSystem> Data(DataProviderMode mode = DataProviderMode.PlayerPrefs, bool encrypted = false)
        {
            var data = new DataSystem { Setting = new DataSystemSetting { ProviderMode = mode, EnableEncryption = encrypted } };
            await data.Initialize();
            return data;
        }

        private static async UniTask DataIO()
        {
            var data = await Data();
            var provider = new DataProviderProbe { ReadGate = new UniTaskCompletionSource(), WriteGate = new UniTaskCompletionSource(),
                WriteError = new IOException("Controlled write failure"), DisposeError = new IOException("Controlled dispose failure") };
            try
            {
                await data.SwitchProvider(provider);
                data.Save("ready", "value");
                var write = data.SaveAsync("other", "value");
                var read = data.LoadAsync<string>("ready");
                var close = data.DisposeAsync().Preserve();
                Check(close.Status == UniTaskStatus.Pending && provider.Disposals == 0, "Closing waits for accepted I/O before disposing its provider.");
                provider.WriteGate.TrySetResult();
                Exception failure = null;
                try { await write; } catch (IOException ex) { failure = ex; }
                Check(ReferenceEquals(failure, provider.WriteError) && close.Status == UniTaskStatus.Pending, "Write reports its original failure without cancelling another read.");
                var mainThread = Thread.CurrentThread.ManagedThreadId;
                provider.ReadGate.TrySetResult();
                Check(await read == "value", "Accepted read completes.");
                try { await close; } catch (IOException ex) { failure = ex; }
                Check(ReferenceEquals(failure, provider.DisposeError) && provider.Disposals == 1 && provider.DisposeThread == mainThread,
                    "Provider is released once on the main thread and its error propagates.");
            }
            finally { provider.ReadGate.TrySetResult(); provider.WriteGate.TrySetResult(); await data.DisposeAsync(); }
        }

        private static async UniTask DataSwitch()
        {
            var data = await Data();
            var original = new DataProviderProbe();
            var candidate = new DataProviderProbe { InitializeGate = new UniTaskCompletionSource() };
            var main = Thread.CurrentThread.ManagedThreadId;
            try
            {
                var retired = new DataProviderProbe { DisposeError = new IOException("Controlled dispose failure") };
                await data.SwitchProvider(retired);
                IOException releaseError = null;
                try { await data.SwitchProvider(original); } catch (IOException ex) { releaseError = ex; }
                Check(ReferenceEquals(releaseError, retired.DisposeError) && retired.Disposals == 1 &&
                    data.Provider == original && original.Disposals == 0, "A release error leaves the new provider in service.");
                data.Save("ready", "value");
                var failed = new DataProviderProbe { InitializeResult = false };
                var rejected = false;
                try { await data.SwitchProvider(failed); } catch (InvalidOperationException) { rejected = true; }
                Check(rejected && failed.Disposals == 1 && data.Provider == original, "Failed candidate leaves the working provider in place.");
                var change = data.SwitchProvider(candidate).Preserve();
                Check(data.Load<string>("ready") == "value" && data.Provider == original, "Previous provider remains readable during initialization.");
                var close = data.DisposeAsync().Preserve();
                Check(close.Status == UniTaskStatus.Pending && candidate.Disposals == 0, "Shutdown waits for the accepted switch.");
                await System.Threading.Tasks.Task.Run(() => candidate.InitializeGate.TrySetResult());
                await change;
                await close;
                Check(candidate.InitializeThread != main && original.DisposeThread == main && candidate.DisposeThread == main &&
                    original.Disposals == 1 && candidate.Disposals == 1, "Worker completion publishes and releases on the main thread.");
            }
            finally { candidate.InitializeGate.TrySetResult(); await data.DisposeAsync(); }
        }

        [Serializable] private sealed class DataRecord { public string Value; public DataRecord() { } }

        private static async UniTask DataPersistence()
        {
            var scratch = Path.Combine(Root, "Scratch", "data-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(scratch);
            try
            {
                foreach (var mode in new[] { DataProviderMode.PlayerPrefs, DataProviderMode.JsonFile, DataProviderMode.BinaryFile })
                foreach (var encrypted in new[] { false, true })
                {
                    var key = "validation-" + Guid.NewGuid().ToString("N");
                    var value = new DataRecord { Value = "持久化-" + key };
                    var data = await Data(mode, encrypted);
                    try
                    {
                        if (mode != DataProviderMode.PlayerPrefs) Set(data.Provider, "_savePath", scratch);
                        var write = data.SaveAsync(key, value);
                        await data.DisposeAsync();
                        Check(write.Status == UniTaskStatus.Succeeded, mode + " shutdown waited for save.");
                        await write;
                        data = await Data(mode, encrypted);
                        if (mode != DataProviderMode.PlayerPrefs) Set(data.Provider, "_savePath", scratch);
                        var read = data.LoadAsync<DataRecord>(key);
                        await data.DisposeAsync();
                        Check((await read)?.Value == value.Value, mode + " reopens saved value, encryption=" + encrypted);
                    }
                    finally
                    {
                        await data.DisposeAsync();
                        if (mode == DataProviderMode.PlayerPrefs) { PlayerPrefs.DeleteKey(key); PlayerPrefs.Save(); }
                        else File.Delete(Path.Combine(scratch, key + (mode == DataProviderMode.JsonFile ? ".json" : ".dat")));
                    }
                }
            }
            finally { Directory.Delete(scratch); }
        }
    }
}
