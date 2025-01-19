using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace userspace_backend.Model.EditableSettings
{
    public abstract class EditableList<T>
    {
        public bool TryAddNewDefault()
        {
            for (int i = 0; i < 10; i++)
            {
                string defaultProfileName = GenerateDefaultName(i);
                
                if (!ContainsElementWithName(defaultProfileName))
                {
                    T newDefaultElement = GenerateDefaultElement(defaultProfileName);
                    AddElement(newDefaultElement);

                    return true;
                }
            }

            return false;
        }

        public bool TryAdd(T element)
        {
            string name = GetNameFromElement(element);

            if (!ContainsElementWithName(name))
            {
                AddElement(element);
                return true;
            }

            return true;
        }

        protected abstract string GetNameFromElement(T element);

        public abstract bool TryGetElement(string name, out T element);

        protected abstract string DefaultNameTemplate { get; }

        protected string GenerateDefaultName(int index) => $"{DefaultNameTemplate}{index}";

        protected bool ContainsElementWithName(string name) => TryGetElement(name, out T _);

        protected abstract bool AddElement(T element);

        protected abstract T GenerateDefaultElement(string name);
    }
}
