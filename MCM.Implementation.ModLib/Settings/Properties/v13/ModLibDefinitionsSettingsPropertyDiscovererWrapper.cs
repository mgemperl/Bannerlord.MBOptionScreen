﻿using MCM.Abstractions.Settings.Properties;

namespace MCM.Implementation.ModLib.Settings.Properties.v13
{
    /// <summary>
    /// For DI
    /// </summary>
    public class ModLibDefinitionsSettingsPropertyDiscovererWrapper : BaseSettingsPropertyDiscovererWrapper, IModLibDefinitionsSettingsPropertyDiscoverer
    {
        public ModLibDefinitionsSettingsPropertyDiscovererWrapper(object @object) : base(@object) { }
    }
}