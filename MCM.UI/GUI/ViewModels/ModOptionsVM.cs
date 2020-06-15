﻿using ComparerExtensions;

using MCM.Abstractions.Settings.Providers;
using MCM.Utils;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Engine;
using TaleWorlds.Engine.Screens;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace MCM.UI.GUI.ViewModels
{
    internal sealed class ModOptionsVM : ViewModel
    {
        private string _titleLabel = "";
        private string _cancelButtonText = "";
        private string _doneButtonText = "";
        private string _modsText = "";
        private SettingsVM? _selectedMod;
        private MBBindingList<SettingsVM> _modSettingsList = new MBBindingList<SettingsVM>();
        private string _hintText = "";
        private string _searchText = "";

        [DataSourceProperty]
        public string Name
        {
            get => _titleLabel;
            set
            {
                _titleLabel = value;
                OnPropertyChanged(nameof(Name));
            }
        }
        [DataSourceProperty]
        public bool ChangesMade => ModSettingsList.Any(x => x.URS.ChangesMade);
        [DataSourceProperty]
        public string DoneButtonText
        {
            get => _doneButtonText;
            set
            {
                _doneButtonText = value;
                OnPropertyChanged(nameof(DoneButtonText));
            }
        }
        [DataSourceProperty]
        public string CancelButtonText
        {
            get => _cancelButtonText; set
            {
                _cancelButtonText = value;
                OnPropertyChanged(nameof(CancelButtonText));
            }
        }
        [DataSourceProperty]
        public string ModsText
        {
            get => _modsText; set
            {
                _modsText = value;
                OnPropertyChanged(nameof(ModsText));
            }
        }
        [DataSourceProperty]
        public MBBindingList<SettingsVM> ModSettingsList
        {
            get => _modSettingsList;
            set
            {
                if (_modSettingsList != value)
                {
                    _modSettingsList = value;
                    OnPropertyChanged(nameof(ModSettingsList));
                }
            }
        }
        [DataSourceProperty]
        public SettingsVM? SelectedMod
        {
            get => _selectedMod;
            set
            {
                if (_selectedMod != value)
                {
                    _selectedMod?.PresetsSelector?.SetOnChangeAction(null);
                    _selectedMod = value;

                    OnPropertyChanged(nameof(SelectedMod));
                    OnPropertyChanged(nameof(SelectedDisplayName));
                    OnPropertyChanged(nameof(SomethingSelected));

                    if (_selectedMod?.PresetsSelector != null)
                    {
                        PresetsSelector.SetOnChangeAction(null);
                        OnPropertyChanged(nameof(PresetsSelector));
                        PresetsSelector.ItemList = _selectedMod.PresetsSelector.ItemList;
                        PresetsSelector.SelectedIndex = _selectedMod.PresetsSelector.SelectedIndex;
                        PresetsSelector.HasSingleItem = _selectedMod.PresetsSelector.HasSingleItem;
                        _selectedMod.PresetsSelector.SetOnChangeAction(OnModPresetsSelectorChange);
                        PresetsSelector.SetOnChangeAction(OnPresetsSelectorChange);

                        OnPropertyChanged(nameof(IsPresetsSelectorVisible));
                    }
                }
            }
        }
        [DataSourceProperty]
        public string SelectedDisplayName => SelectedMod == null ? new TextObject("{=ModOptionsVM_NotSpecified}Mod Name not Specified.").ToString() : SelectedMod.DisplayName;
        [DataSourceProperty]
        public bool SomethingSelected => SelectedMod != null;
        [DataSourceProperty]
        public string HintText
        {
            get => _hintText;
            set
            {
                if (_hintText != value)
                {
                    _hintText = value;
                    OnPropertyChanged(nameof(HintText));
                    OnPropertyChanged(nameof(IsHintVisible));
                }
            }
        }
        [DataSourceProperty]
        public bool IsHintVisible => !string.IsNullOrWhiteSpace(HintText);
        [DataSourceProperty]
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged(nameof(SearchText));
                    if (SelectedMod?.SettingPropertyGroups.Count > 0)
                    {
                        foreach (var group in SelectedMod.SettingPropertyGroups)
                            group.NotifySearchChanged();
                    }
                }
            }
        }
        [DataSourceProperty]
        public SelectorVM<SelectorItemVM> PresetsSelector { get; } = new SelectorVM<SelectorItemVM>(Array.Empty<string>(), -1, null);
        [DataSourceProperty]
        public bool IsPresetsSelectorVisible => SelectedMod != null;

        public ModOptionsVM()
        {
            Name = new TextObject("{=ModOptionsVM_Name}Mod Options").ToString();
            DoneButtonText = new TextObject("{=WiNRdfsm}Done").ToString();
            CancelButtonText = new TextObject("{=3CpNUnVl}Cancel").ToString();
            ModsText = new TextObject("{=ModOptionsPageView_Mods}Mods").ToString();
            SearchText = "";

            ModSettingsList = new MBBindingList<SettingsVM>();
            // Fancy
            new TaskFactory().StartNew(syncContext => // Build the options in a separate context if possible
            {
                if (!(syncContext is SynchronizationContext uiContext))
                    return;

                var settingsVM = BaseSettingsProvider.Instance.CreateModSettingsDefinitions
                    .Parallel()
                    .Select(s => new SettingsVM(s, this));
                foreach (var viewModel in settingsVM)
                {
                    uiContext.Send(o =>
                    {
                        if (!(o is SettingsVM vm))
                            return;

                        vm.AddSelectCommand(ExecuteSelect);
                        ModSettingsList.Add(vm);
                        vm.RefreshValues();
                    }, viewModel);
                }

                // Yea, I imported a whole library that converts LINQ style order to IComparer
                // because I wasn't able to recreate the logic via IComparer. TODO: Fix that
                ModSettingsList.Sort(KeyComparer<SettingsVM>
                    .OrderByDescending(x => x.SettingsDefinition.SettingsId.StartsWith("MCM") ||
                                            x.SettingsDefinition.SettingsId.StartsWith("Testing") ||
                                            x.SettingsDefinition.SettingsId.StartsWith("ModLib"))
                    .ThenByDescending(x => x.DisplayName, new AlphanumComparatorFast()));
            }, SynchronizationContext.Current);

            RefreshValues();
        }

        public override void RefreshValues()
        {
            base.RefreshValues();

            foreach (var viewModel in ModSettingsList)
                viewModel.RefreshValues();

            OnPropertyChanged(nameof(SelectedMod));

            if (SelectedMod != null)
            {
                PresetsSelector.SetOnChangeAction(null);
                PresetsSelector.SelectedIndex = SelectedMod?.PresetsSelector?.SelectedIndex ?? -1;
                PresetsSelector.SetOnChangeAction(OnPresetsSelectorChange);
            }
        }

        private void OnPresetsSelectorChange(SelectorVM<SelectorItemVM> selector)
        {
            InformationManager.ShowInquiry(new InquiryData(new TextObject("{=ModOptionsVM_ChangeToPreset}Change to preset '{PRESET}'", new Dictionary<string, TextObject>()
                {
                    { "PRESET", new TextObject(selector.SelectedItem.StringItem) }
                }).ToString(), 
                new TextObject("{=ModOptionsVM_Discard}Are you sure you wish to discard the current settings for {NAME} to '{ITEM}'?", new Dictionary<string, TextObject>()
                {
                    { "NAME", new TextObject(SelectedMod!.DisplayName) },
                    { "ITEM", new TextObject(selector.SelectedItem.StringItem) }
                }).ToString(), 
                true, true, new TextObject("{=aeouhelq}Yes").ToString(), new TextObject("{=8OkPHu4f}No").ToString(),
                () =>
                {
                    SelectedMod!.ChangePreset(PresetsSelector.SelectedItem.StringItem);
                    var selectedMod = SelectedMod;
                    ExecuteSelect(null);
                    ExecuteSelect(selectedMod);
                },
                () =>
                {
                    PresetsSelector.SetOnChangeAction(null);
                    PresetsSelector.SelectedIndex = SelectedMod.PresetsSelector?.SelectedIndex ?? -1;
                    PresetsSelector.SetOnChangeAction(OnPresetsSelectorChange);
                }));
        }
        private void OnModPresetsSelectorChange(SelectorVM<SelectorItemVM> selector)
        {
            PresetsSelector.SetOnChangeAction(null);
            PresetsSelector.SelectedIndex = selector.SelectedIndex;
            PresetsSelector.SetOnChangeAction(OnPresetsSelectorChange);
        }

        public void ExecuteClose()
        {
            foreach (var viewModel in ModSettingsList)
            {
                viewModel.URS.UndoAll();
                viewModel.URS.ClearStack();
            }
        }

        public bool ExecuteCancel() => ExecuteCancelInternal(true);
        public bool ExecuteCancelInternal(bool popScreen, Action? onClose = null)
        {
            OnFinalize();
            if (popScreen) ScreenManager.PopScreen();
            else onClose?.Invoke();
            foreach (var viewModel in ModSettingsList)
            {
                viewModel.URS.UndoAll();
                viewModel.URS.ClearStack();
            }
            return true;
        }

        public void ExecuteDone() => ExecuteDoneInternal(true);
        public void ExecuteDoneInternal(bool popScreen, Action? onClose = null)
        {
            if (!ModSettingsList.Any(x => x.URS.ChangesMade))
            {
                OnFinalize();
                if (popScreen) ScreenManager.PopScreen();
                else onClose?.Invoke();
                return;
            }

            //Save the changes to file.
            var changedModSettings = ModSettingsList.Where(x => x.URS.ChangesMade).ToList();

            var requireRestart = changedModSettings.Any(x => x.RestartRequired());
            if (requireRestart)
            {
                InformationManager.ShowInquiry(new InquiryData(new TextObject("{=ModOptionsVM_RestartTitle}Game Needs to Restart").ToString(),
                    new TextObject("{=ModOptionsVM_RestartDesc}The game needs to be restarted to apply mod settings changes. Do you want to close the game now?").ToString(), 
                    true, true, new TextObject("{=aeouhelq}Yes").ToString(), new TextObject("{=3CpNUnVl}Cancel").ToString(),
                    () =>
                    {
                        foreach (var changedModSetting in changedModSettings)
                        {
                            changedModSetting.SaveSettings();
                            changedModSetting.URS.ClearStack();
                        }

                        OnFinalize();
                        onClose?.Invoke();
                        Utilities.QuitGame();
                    }, () => { }));
            }
            else
            {
                foreach (var changedModSetting in changedModSettings)
                {
                    changedModSetting.SaveSettings();
                    changedModSetting.URS.ClearStack();
                }

                OnFinalize();
                if (popScreen) ScreenManager.PopScreen();
                else onClose?.Invoke();
            }
        }

        public void ExecuteSelect(SettingsVM? viewModel)
        {
            if (SelectedMod != viewModel)
            {
                if (SelectedMod != null)
                    SelectedMod.IsSelected = false;

                SelectedMod = viewModel;

                if (SelectedMod != null)
                    SelectedMod.IsSelected = true;
            }
        }


        public override void OnFinalize()
        {
            foreach (var modSettings in ModSettingsList)
                modSettings.OnFinalize();

            base.OnFinalize();
        }
    }
}