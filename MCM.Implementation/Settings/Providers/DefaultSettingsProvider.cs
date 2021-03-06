﻿using MCM.Abstractions;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Settings.Base;
using MCM.Abstractions.Settings.Containers;
using MCM.Abstractions.Settings.Containers.Global;
using MCM.Abstractions.Settings.Containers.PerCharacter;
using MCM.Abstractions.Settings.Models;
using MCM.Abstractions.Settings.Providers;
using MCM.Utils;

using System.Collections.Generic;
using System.Linq;

using TaleWorlds.Core;

namespace MCM.Implementation.Settings.Providers
{
    [Version("e1.0.0",  3)]
    [Version("e1.0.1",  3)]
    [Version("e1.0.2",  3)]
    [Version("e1.0.3",  3)]
    [Version("e1.0.4",  3)]
    [Version("e1.0.5",  3)]
    [Version("e1.0.6",  3)]
    [Version("e1.0.7",  3)]
    [Version("e1.0.8",  3)]
    [Version("e1.0.9",  3)]
    [Version("e1.0.10", 3)]
    [Version("e1.0.11", 3)]
    [Version("e1.1.0",  3)]
    [Version("e1.2.0",  3)]
    [Version("e1.2.1",  3)]
    [Version("e1.3.0",  3)]
    [Version("e1.3.1",  3)]
    [Version("e1.4.0",  3)]
    [Version("e1.4.1",  3)]
    internal sealed class DefaultSettingsProvider : BaseSettingsProvider
    {
        private List<ISettingsContainer> SettingsContainers { get; }

        public override IEnumerable<SettingsDefinition> CreateModSettingsDefinitions => SettingsContainers
            .SelectMany(sp => sp.CreateModSettingsDefinitions);

        public DefaultSettingsProvider()
        {
            SettingsContainers = new List<ISettingsContainer>()
                .Concat(DI.GetBaseImplementations<IGlobalSettingsContainer>())
                .Concat(DI.GetBaseImplementations<IPerCharacterSettingsContainer>())
                .ToList();
        }

        public override BaseSettings? GetSettings(string id)
        {
            foreach (var settingsContainer in SettingsContainers)
            {
                if (settingsContainer.GetSettings(id) is {} settings)
                    return settings;
            }
            return null;
        }
        public override object? GetSettingsObject(string id)
        {
            foreach (var settingsContainer in SettingsContainers)
            {
                if (settingsContainer.GetSettings(id) is { } settings)
                    return settings switch
                    {
                        IWrapper wrapper => wrapper.Object,
                        _ => settings
                    };
            }
            return null;
        }

        public override void SaveSettings(BaseSettings settings)
        {
            foreach (var settingsContainer in SettingsContainers)
                settingsContainer.SaveSettings(settings);
            settings.OnPropertyChanged(BaseSettings.SaveTriggered);
        }

        public override void ResetSettings(BaseSettings settings)
        {
            foreach (var settingsContainer in SettingsContainers)
                settingsContainer.ResetSettings(settings);
        }
        public override void OverrideSettings(BaseSettings settings)
        {
            foreach (var settingsContainer in SettingsContainers)
                settingsContainer.OverrideSettings(settings);
        }

        public override void OnGameStarted(Game game)
        {
            foreach (var settingsContainer in SettingsContainers)
            {
                if (settingsContainer is IPerCharacterSettingsContainer perCharacterContainer)
                {
                    perCharacterContainer.OnGameStarted(game);
                }
            }
        }
        public override void OnGameEnded(Game game)
        {
            foreach (var settingsContainer in SettingsContainers)
            {
                if (settingsContainer is IPerCharacterSettingsContainer perCharacterContainer)
                {
                    perCharacterContainer.OnGameEnded(game);

                }
            }
        }
    }
}