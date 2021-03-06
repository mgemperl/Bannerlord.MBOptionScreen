﻿using HarmonyLib;

using System.Reflection;

using TaleWorlds.Library;

namespace MCM.Abstractions.Attributes
{
    internal sealed class VersionAttributeWrapper : IWrapper, IVersion
    {
        public object Object { get; }
        private PropertyInfo? GameVersionProperty { get; }
        private PropertyInfo? ImplementationVersionProperty { get; }
        public bool IsCorrect { get; }

        public ApplicationVersion GameVersion => GameVersionProperty?.GetValue(Object) as ApplicationVersion? ?? ApplicationVersion.Empty;
        public int ImplementationVersion => ImplementationVersionProperty?.GetValue(Object) as int? ?? 0;

        public VersionAttributeWrapper(object @object)
        {
            Object = @object;
            var type = @object.GetType();

            GameVersionProperty = AccessTools.Property(type, nameof(GameVersion));
            ImplementationVersionProperty = AccessTools.Property(type, nameof(ImplementationVersion));

            IsCorrect = GameVersionProperty != null && ImplementationVersionProperty != null;
        }
    }
}