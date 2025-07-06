using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace userspace_backend.Model.EditableSettings
{
    public partial class EditableSetting<T> : ObservableObject, IEditableSettingSpecific<T> where T : IComparable
    {
        /// <summary>
        /// This value can be bound in UI for direct editing
        /// </summary>
        [ObservableProperty]
        public string interfaceValue;

        /// <summary>
        /// This value can be bound in UI for logic based on validated input
        /// </summary>
        [ObservableProperty]
        public T modelValue;

        public EditableSetting(
            string displayName,
            T initialValue,
            IUserInputParser<T> parser,
            IModelValueValidator<T> validator,
            bool autoUpdateFromInterface = false)
        {
            DisplayName = displayName;
            LastWrittenValue = initialValue;
            Parser = parser;
            Validator = validator;
            UpdateModelValueFromLastKnown();
            UpdateInterfaceValue();
            AutoUpdateFromInterface = autoUpdateFromInterface;
        }

        /// <summary>
        /// Display name for this setting in UI
        /// </summary>
        public string DisplayName { get; }

        // TODO: test or remove
        public T LastWrittenValue { get; protected set; }

        /// <summary>
        /// Interface can set this for cases when new value arrives all at once (such as menu selection)
        /// instead of cases where new value arrives in parts (typing)
        /// </summary>
        public bool AutoUpdateFromInterface { get; set; }

        private IUserInputParser<T> Parser { get; }

        //TODO: change settings collections init so that this can be made private for non-static validators
        public IModelValueValidator<T> Validator { get; set; }

        public bool HasChanged() => ModelValue.CompareTo(LastWrittenValue) == 0;

        public bool TryUpdateFromInterface()
        {
            if (string.IsNullOrEmpty(InterfaceValue))
            {
                UpdateInterfaceValue();
                return false;
            }

            if (!Parser.TryParse(InterfaceValue.Trim(), out T parsedValue))
            {
                UpdateInterfaceValue();
                return false;
            }

            if (parsedValue.CompareTo(ModelValue) == 0)
            {
                return true;
            }

            if (!Validator.Validate(parsedValue))
            {
                UpdateInterfaceValue();
                return false;
            }

            UpdatedModeValue(parsedValue);
            return true;
        }

        protected void UpdateInterfaceValue()
        {
            InterfaceValue = ModelValue?.ToString();
        }

        protected void UpdateModelValueFromLastKnown()
        {
            UpdatedModeValue(LastWrittenValue);
        }

        protected void UpdatedModeValue(T value)
        {
            ModelValue = value;
        }

        partial void OnInterfaceValueChanged(string value)
        {
            if (AutoUpdateFromInterface)
            {
                TryUpdateFromInterface();
            }
        }

        public bool TrySetFromData(T data)
        {
            throw new NotImplementedException();
        }
    }

    public partial class EditableSettingV2<T> : ObservableObject, IEditableSettingSpecific<T> where T : IComparable
    {
        /// <summary>
        /// This value can be bound in UI for direct editing
        /// </summary>
        [ObservableProperty]
        public string interfaceValue;

        /// <summary>
        /// This value can be bound in UI for logic based on validated input
        /// </summary>
        [ObservableProperty]
        public T modelValue;

        public EditableSettingV2(
            string displayName,
            T initialValue,
            IUserInputParser<T> parser,
            IModelValueValidator<T> validator,
            bool autoUpdateFromInterface = false)
        {
            DisplayName = displayName;
            LastWrittenValue = initialValue;
            Parser = parser;
            Validator = validator;
            UpdateModelValueFromLastKnown();
            UpdateInterfaceValue();
            AutoUpdateFromInterface = autoUpdateFromInterface;
        }

        /// <summary>
        /// Display name for this setting in UI
        /// </summary>
        public string DisplayName { get; }

        public T LastWrittenValue { get; protected set; }

        /// <summary>
        /// Interface can set this for cases when new value arrives all at once (such as menu selection)
        /// instead of cases where new value arrives in parts (typing)
        /// </summary>
        public bool AutoUpdateFromInterface { get; set; }

        private IUserInputParser<T> Parser { get; }

        //TODO: change settings collections init so that this can be made private for non-static validators
        public IModelValueValidator<T> Validator { get; set; }

        private bool AllowAutoUpdateFromInterface { get; set; } = true;

        public bool HasChanged() => ModelValue.CompareTo(LastWrittenValue) == 0;

        public bool TryUpdateFromInterface()
        {
            if (string.IsNullOrEmpty(InterfaceValue))
            {
                UpdateInterfaceValue();
                return false;
            }

            if (!Parser.TryParse(InterfaceValue.Trim(), out T parsedValue))
            {
                UpdateInterfaceValue();
                return false;
            }

            if (parsedValue.CompareTo(ModelValue) == 0)
            {
                return true;
            }

            if (!Validator.Validate(parsedValue))
            {
                UpdateInterfaceValue();
                return false;
            }

            UpdateModeValue(parsedValue);
            return true;
        }

        protected void UpdateInterfaceValue()
        {
            bool previous = AllowAutoUpdateFromInterface;
            AllowAutoUpdateFromInterface = false;
            InterfaceValue = ModelValue?.ToString();
            AllowAutoUpdateFromInterface = true;
        }

        protected void UpdateModelValueFromLastKnown()
        {
            UpdateModeValue(LastWrittenValue);
        }

        protected void UpdateModeValue(T value)
        {
            ModelValue = value;
        }

        partial void OnInterfaceValueChanged(string value)
        {
            // TODO: double-check race conditions
            if (AutoUpdateFromInterface && AllowAutoUpdateFromInterface)
            {
                TryUpdateFromInterface();
            }
        }

        // TODO: unit test
        public bool TrySetFromData(T data)
        {
            bool previous = AllowAutoUpdateFromInterface;
            AllowAutoUpdateFromInterface = false;
            InterfaceValue = data.ToString();
            bool result = TryUpdateFromInterface();
            AllowAutoUpdateFromInterface = previous;
            return result;
        }
    }
}
