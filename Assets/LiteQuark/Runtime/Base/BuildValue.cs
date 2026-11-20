using System;

namespace LiteQuark.Runtime
{
    [Serializable]
    public struct BuildValue<T>
    {
        public T Release;
        public bool OverrideInDevelopment;
        public T Development;

        public BuildValue(T release)
        {
            Release = release;
            OverrideInDevelopment = false;
            Development = release;
        }

        public BuildValue(T release, T development)
        {
            Release = release;
            OverrideInDevelopment = true;
            Development = development;
        }

        public T Resolve(LiteBuildProfile profile)
        {
            return profile == LiteBuildProfile.Development && OverrideInDevelopment ? Development : Release;
        }

        // Only used on the detached runtime copy. Module properties then read and write the effective value.
        internal void Apply(LiteBuildProfile profile)
        {
            this = new BuildValue<T>(Resolve(profile));
        }
    }
}
