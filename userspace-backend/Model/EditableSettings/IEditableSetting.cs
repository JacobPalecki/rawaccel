using System;
using System.ComponentModel;

namespace userspace_backend.Model.EditableSettings
{
    public interface IEditableSetting : INotifyPropertyChanged
    {
        string DisplayName { get; }

        string InterfaceValue { get; set; }

        bool HasChanged();

        bool AutoUpdateFromInterface { get; set; }

        bool TryUpdateFromInterface();
    }

    public interface IEditableSettingSpecific<T> : IEditableSetting where T : IComparable
    {
        public T ModelValue { get; }
    }
}
