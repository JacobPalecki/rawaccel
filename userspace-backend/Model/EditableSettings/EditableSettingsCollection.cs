using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace userspace_backend.Model.EditableSettings
{
    public abstract class EditableSettingsCollection<T> : ObservableObject, IEditableSettingsCollection
    {
        public EditableSettingsCollection(T dataObject)
        {
            InitEditableSettingsAndCollections(dataObject);
            GatherEditableSettings();
            GatherEditableSettingsCollections();
        }

        public EventHandler AnySettingChanged { get; set; }

        public IEnumerable<IEditableSetting> AllContainedEditableSettings { get; set; }

        public IEnumerable<IEditableSettingsCollection> AllContainedEditableSettingsCollections { get; set; }

        public bool HasChanged { get; protected set; }

        public void EvaluateWhetherHasChanged()
        {
            if (AllContainedEditableSettings.Any(s => s.HasChanged()) ||
                AllContainedEditableSettingsCollections.Any(c => c.HasChanged))
            {
                HasChanged = true;
            }
            else
            {
                HasChanged = false;
            }
        }

        public void GatherEditableSettings()
        {
            AllContainedEditableSettings = EnumerateEditableSettings();

            foreach (var setting in AllContainedEditableSettings)
            {
                // TODO: revisit settings composition so that this null check is unnecessary
                if (setting != null)
                {
                    setting.PropertyChanged += EditableSettingChangedEventHandler;
                }
            }
        }
        public void GatherEditableSettingsCollections()
        {
            AllContainedEditableSettingsCollections = EnumerateEditableSettingsCollections();

            // TODO: separate "All" and "currently selected" settings collections
            // so that incorrect assignment is not done here for collections that alter this through use
            foreach (var settingsCollection in AllContainedEditableSettingsCollections)
            {
                settingsCollection.AnySettingChanged += EditableSettingsCollectionChangedEventHandler;
            }
        }

        protected void EditableSettingChangedEventHandler(object? sender, PropertyChangedEventArgs e)
        {
            if (string.Equals(e.PropertyName, nameof(IEditableSettingSpecific<IComparable>.ModelValue)))
            {
                OnAnySettingChanged();
            }
        }

        protected void EditableSettingsCollectionChangedEventHandler(object? sender, EventArgs e)
        {
            OnAnySettingChanged();
        }

        protected void OnAnySettingChanged()
        {
            AnySettingChanged?.Invoke(this, new EventArgs());
        }

        protected abstract void InitEditableSettingsAndCollections(T dataObject);

        protected abstract IEnumerable<IEditableSetting> EnumerateEditableSettings();

        protected abstract IEnumerable<IEditableSettingsCollection> EnumerateEditableSettingsCollections();

        public abstract T MapToData();
    }
}
