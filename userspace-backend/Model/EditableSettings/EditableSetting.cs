﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace userspace_backend.Model.EditableSettings
{
    public class EditableSetting<T> : IEditableSetting where T : IEquatable<T>
    {
        public EditableSetting(T initialValue, IParser<T> parser, Action setCallback = null)
        {
            EditableValue = initialValue;
            LastKnownValue = initialValue;
            Parser = parser;
            SetCallback = setCallback;
        }

        public T EditableValue { get; protected set; }

        public T LastKnownValue { get; protected set; }

        public string EditedValueForDiplay { get => EditableValue?.ToString() ?? string.Empty; }

        protected Action SetCallback { get; }

        private IParser<T> Parser { get; }

        public bool HasChanged() => !EditableValue.Equals(LastKnownValue);

        public bool TryParseAndSet(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return false;
            }

            if (Parser.TryParse(input.Trim(), out T parsedValue))
            {
                EditableValue = parsedValue;
                SetCallback?.Invoke();
                return true;
            }

            return false;
        }
    }
}
