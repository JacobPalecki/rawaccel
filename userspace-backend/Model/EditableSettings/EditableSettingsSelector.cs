using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace userspace_backend.Model.EditableSettings
{
    public interface IEditableSettingsSelector<T, U> : IEditableSettingsCollectionSpecific<U> where T : Enum
    {
        public IEditableSettingSpecific<T> Selection { get; }

        public IEditableSettingsCollectionSpecific<U> GetSelectable(T choice);
    }

    public static class EditableSettingsSelectorHelper
    {
        public static string GetSelectionKey<T>(T value) where T : Enum
        {
            return $"{typeof(T)}.{value.ToString()}";
        }

    }

    public abstract class EditableSettingsSelectableIntermediate<T> :
        EditableSettingsCollectionV2<T>
    {
        protected EditableSettingsSelectableIntermediate(
            IEnumerable<IEditableSetting> editableSettings,
            IEnumerable<IEditableSettingsCollectionV2> editableSettingsCollections)
            : base(editableSettings, editableSettingsCollections)
        {
        }
    }

    public abstract class EditableSettingsSelectable<T, U> :
        EditableSettingsSelectableIntermediate<T>,
        IEditableSettingsCollectionSpecific<U> where T : class, U
    {
        protected EditableSettingsSelectable(
            IEnumerable<IEditableSetting> editableSettings,
            IEnumerable<IEditableSettingsCollectionV2> editableSettingsCollections)
            : base(editableSettings, editableSettingsCollections)
        {
        }

        public bool TryMapFromData(U data)
        {
            T dataCasted = data as T;
            return dataCasted == null ? false : TryMapFromData(dataCasted);
        }

        U IEditableSettingsCollectionSpecific<U>.MapToData()
        {
            return MapToData();
        }
    }

    public abstract class EditableSettingsSelector<T, U>
        : EditableSettingsCollectionV2<U>,
          IEditableSettingsSelector<T, U> where T : Enum
    {
        protected EditableSettingsSelector(
            IServiceProvider serviceProvider,
            IEditableSettingSpecific<T> selection,
            IEnumerable<IEditableSetting> editableSettings,
            IEnumerable<IEditableSettingsCollectionV2> editableSettingsCollections)
            : base(editableSettings.Union([selection]), editableSettingsCollections)
        {
            SelectionLookup = new Dictionary<T, IEditableSettingsCollectionSpecific<U>>();
            InitSelectionLookup(serviceProvider);
            Selection = selection;
        }

        public IEditableSettingSpecific<T> Selection { get; }

        protected IDictionary<T, IEditableSettingsCollectionSpecific<U>> SelectionLookup { get; }

        public IEditableSettingsCollectionSpecific<U> GetSelectable(T choice) => SelectionLookup[choice];

        public IEditableSettingsCollectionSpecific<U> Selected => GetSelectable(Selection.ModelValue);

        protected void InitSelectionLookup(IServiceProvider serviceProvider)
        {
            foreach (T value in Enum.GetValues(typeof(T)))
            {
                string key = EditableSettingsSelectorHelper.GetSelectionKey(value);
                SelectionLookup.Add(value, serviceProvider.GetRequiredKeyedService<IEditableSettingsCollectionSpecific<U>>(key));
            }
        }

        public override U MapToData()
        {
            return Selected.MapToData();
        }
    }
}
