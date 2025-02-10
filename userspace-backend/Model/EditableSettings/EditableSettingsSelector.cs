using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace userspace_backend.Model.EditableSettings
{
    public interface IEditableSettingsSelector<T, U, V> : IEditableSettingsCollectionSpecific<V> where T : Enum where U : IEditableSettingsCollectionV2
    {
        public IEditableSettingSpecific<T> Selection { get; }

        public U GetSelectable(T choice);
    }

    public abstract class EditableSettingsSelector<T, U, V>
        : EditableSettingsCollectionV2<V>,
          IEditableSettingsSelector<T, U, V> where T : Enum where U : IEditableSettingsCollectionV2
    {
        protected EditableSettingsSelector(
            IEditableSettingSpecific<T> selection,
            IServiceProvider serviceProvider,
            IEnumerable<IEditableSetting> editableSettings,
            IEnumerable<IEditableSettingsCollectionV2> editableSettingsCollections)
            : base(editableSettings, editableSettingsCollections)
        {
            SelectionLookup = new Dictionary<T, U>();
            Selection = selection;
        }

        public IEditableSettingSpecific<T> Selection { get; }

        protected IDictionary<T, U> SelectionLookup { get; }

        public U GetSelectable(T choice) => SelectionLookup[choice];

        protected void InitSelectionLookup(IServiceProvider serviceProvider)
        {
            foreach (T value in Enum.GetValues(typeof(T)))
            {
                SelectionLookup.Add(value, serviceProvider.GetRequiredService<U>());
            }
        }
    }
}
