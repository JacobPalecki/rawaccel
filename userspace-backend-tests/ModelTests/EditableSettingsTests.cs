using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using userspace_backend.Model.EditableSettings;

namespace userspace_backend_tests.ModelTests
{
    [TestClass]
    public class EditableSettingsTests
    {
        [TestMethod]
        public void EditableSetting_Construction()
        {
            string testSettingName = "Test Setting";
            int testSettingInitialValue = 0;

            EditableSettingV2<int> testObject = 
                new EditableSettingV2<int>(testSettingName, testSettingInitialValue, UserInputParsers.IntParser, ModelValueValidators.DefaultIntValidator, false);

            Assert.IsNotNull(testObject);
            Assert.AreEqual(testSettingInitialValue, testObject.ModelValue);
            Assert.AreEqual(testSettingName, testObject.DisplayName);
        }

        [TestMethod]
        public void EditableSetting_SetFromInterface()
        {
            int propertyChangedHookCalls = 0;

            void TestObject_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
            {
                if (string.Equals(e.PropertyName, nameof(EditableSettingV2<IComparable>.ModelValue)))
                {
                    propertyChangedHookCalls++;
                }
            }

            string testSettingName = "Test Setting";
            int testSettingInitialValue = 0;
            int testSettingSecondValue = 500;

            EditableSettingV2<int> testObject = 
                new EditableSettingV2<int>(testSettingName, testSettingInitialValue, UserInputParsers.IntParser, ModelValueValidators.DefaultIntValidator, false);

            testObject.PropertyChanged += TestObject_PropertyChanged;
            testObject.InterfaceValue = testSettingSecondValue.ToString();
            bool updateResult = testObject.TryUpdateFromInterface();

            Assert.IsTrue(updateResult);
            Assert.AreEqual(testSettingSecondValue, testObject.ModelValue);
            Assert.AreEqual(1, propertyChangedHookCalls);
        }

        [TestMethod]
        public void EditableSetting_SetFromInterface_Automatic()
        {
            int propertyChangedHookCalls = 0;

            void TestObject_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
            {
                if (string.Equals(e.PropertyName, nameof(EditableSettingV2<IComparable>.ModelValue)))
                {
                    propertyChangedHookCalls++;
                }
            }

            string testSettingName = "Test Setting";
            int testSettingInitialValue = 0;
            int testSettingSecondValue = 500;

            EditableSettingV2<int> testObject = 
                new EditableSettingV2<int>(testSettingName, testSettingInitialValue, UserInputParsers.IntParser, ModelValueValidators.DefaultIntValidator, true);

            testObject.PropertyChanged += TestObject_PropertyChanged;
            testObject.InterfaceValue = testSettingSecondValue.ToString();

            Assert.AreEqual(testSettingSecondValue, testObject.ModelValue);
            Assert.AreEqual(1, propertyChangedHookCalls);
        }

        [TestMethod]
        public void EditableSetting_SetFromInterface_BadValue()
        {
            int propertyChangedHookCalls = 0;

            void TestObject_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
            {
                if (string.Equals(e.PropertyName, nameof(EditableSettingV2<IComparable>.ModelValue)))
                {
                    propertyChangedHookCalls++;
                }
            }

            string testSettingName = "Test Setting";
            int testSettingInitialValue = 0;

            // Test case: bad value, cannot parse
            EditableSettingV2<int> testObject = 
                new EditableSettingV2<int>(testSettingName, testSettingInitialValue, UserInputParsers.IntParser, ModelValueValidators.DefaultIntValidator, false);

            testObject.PropertyChanged += TestObject_PropertyChanged;
            testObject.InterfaceValue = "ASDFJLKL";
            bool updateResult = testObject.TryUpdateFromInterface();

            Assert.IsFalse(updateResult);
            Assert.AreEqual(testSettingInitialValue, testObject.ModelValue);
            Assert.AreEqual(0, propertyChangedHookCalls);

            // Test case: validator determines input is invalid
            testObject = 
                new EditableSettingV2<int>(testSettingName, testSettingInitialValue, UserInputParsers.IntParser, new AllChangeInvalidValueValidator<int>(), false);

            testObject.PropertyChanged += TestObject_PropertyChanged;
            testObject.InterfaceValue = 500.ToString();
            updateResult = testObject.TryUpdateFromInterface();

            Assert.IsFalse(updateResult);
            Assert.AreEqual(testSettingInitialValue, testObject.ModelValue);
            Assert.AreEqual(0, propertyChangedHookCalls);
        }
    }
}
