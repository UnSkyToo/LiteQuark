using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using LiteQuark.Runtime;

namespace LiteQuark.Editor
{
    internal sealed partial class AddonsValidationRunner : BundleValidation
    {
        private static AssetBundleBuild[] BuildMedia()
        {
            // A real imported PCM clip exercises Bundle loading without relying on an audio output device.
            using (var writer = new BinaryWriter(File.Create(Fixture + "/tone.wav")))
            {
                const int samples = 4410;
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + samples * 2);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)1);
                writer.Write(44100);
                writer.Write(88200);
                writer.Write((short)2);
                writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                writer.Write(samples * 2);
                for (var i = 0; i < samples; i++) writer.Write((short)(Math.Sin(i * 2 * Math.PI * 440 / 44100) * 1000));
            }
            File.WriteAllBytes(Fixture + "/config.bytes", new byte[] { 7, 128, 255, 7, 129, 0 });
            AssetDatabase.Refresh();
            var effect = new GameObject("validation_effect", typeof(ParticleSystem));
            var particle = effect.GetComponent<ParticleSystem>();
            particle.Stop();
            var main = particle.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.1f;
            main.startLifetime = 0.1f;
            effect.GetComponent<ParticleSystemRenderer>().sortingOrder = 7;
            var binder = effect.AddComponent<EffectBinder>();
            binder.Particles = new[] { particle };
            binder.Animators = Array.Empty<Animator>();
            binder.Trails = Array.Empty<TrailRenderer>();
            binder.LifeTime = 0.1f;
            binder.RetainTime = 0.5f;
            PrefabUtility.SaveAsPrefabAsset(effect, Fixture + "/effect.prefab");
            UnityEngine.Object.DestroyImmediate(effect);
            return new[] { Build("audio.ab", "tone.wav"), Build("effect.ab", "effect.prefab"), Build("config.ab", "config.bytes") };
        }
    }
}
