using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace userspace_backend.Model.EditableSettings
{
    public abstract class EditableSettingsCollection<T> : ObservableObject, IEditableSettingsCollectionV2
    {
        public EditableSettingsCollection(T dataObject)
        {
            InitEditableSettingsAndCollections(dataObject);
            GatherEditableSettings();
            GatherEditableSettingsCollections();
        }

        public EventHandler AnySettingChanged { get; set; }

        public IEnumerable<IEditableSetting> AllContainedEditableSettings { get; set; }

        public IEnumerable<IEditableSettingsCollectionV2> AllContainedEditableSettingsCollections { get; set; }

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

        protected abstract IEnumerable<IEditableSettingsCollectionV2> EnumerateEditableSettingsCollections();

        public abstract T MapToData();
    }

    /// <summary>
    /// Collection of editable settings and other collections.
    /// Internal node of settings tree.
    /// </summary>
    /// <remarks>
    /// The settings model for this backend is a composed object containing other composed objects and settings.
    /// In this way, the objects form a tree. The root node is the base model class, the internal nodes are objects containing other objects
    /// and settings, and the settings themselves are the leaf nodes.
    /// This interface is then an internal node of the tree. It can contain other settings collections (internal nodes) and also contain
    /// settings themselves.
    /// </remarks>
    public interface IEditableSettingsCollectionV2
    {
        public bool HasChanged { get; }

        public EventHandler AnySettingChanged { get; set; }
    }

    public interface IEditableSettingsCollectionSpecific<T> : IEditableSettingsCollectionV2
    {
        T MapToData();
    }

    /// <summary>
    /// Base class for settings collections.
    /// </summary>
    /// <remarks>
    /// Each unique set of collection logic should be generalized into a class that is either this class or a child of this class,
    /// but not the actual class in the model.
    /// This class, and any child class that is a parent to actual settings collections in the model, requires unit tests.
    /// The actual settings collections in the model do not need to each be tested beyond composition.
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    public abstract class EditableSettingsCollectionV2<T> : ObservableObject, IEditableSettingsCollectionSpecific<T>
    {
        public EditableSettingsCollectionV2(
            IEnumerable<IEditableSetting> editableSettings,
            IEnumerable<IEditableSettingsCollectionV2> editableSettingsCollections)
        {
            AllContainedEditableSettings = editableSettings;
            AllContainedEditableSettingsCollections = editableSettingsCollections;

            foreach (var setting in AllContainedEditableSettings)
            {
                // TODO: revisit settings composition so that this null check is unnecessary
                if (setting != null)
                {
                    setting.PropertyChanged += EditableSettingChangedEventHandler;
                }
            }

            // TODO: separate "All" and "currently selected" settings collections
            // so that incorrect assignment is not done here for collections that alter this through use
            foreach (var settingsCollection in AllContainedEditableSettingsCollections)
            {
                settingsCollection.AnySettingChanged += EditableSettingsCollectionChangedEventHandler;
            }
        }

        public EventHandler AnySettingChanged { get; set; }

        public IEnumerable<IEditableSetting> AllContainedEditableSettings { get; set; }

        public IEnumerable<IEditableSettingsCollectionV2> AllContainedEditableSettingsCollections { get; set; }

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

        public abstract T MapToData();
    }
}
