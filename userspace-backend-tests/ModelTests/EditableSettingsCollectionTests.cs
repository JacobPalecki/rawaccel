using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using userspace_backend.Model.EditableSettings;

namespace userspace_backend_tests.ModelTests
{
    [TestClass]
    public class EditableSettingsCollectionTests
    {
        #region TestClasses

        protected class TestDataType
        {
            public int Property { get; set; }

            public TestSubDataType? SubData { get; set; }

            public override bool Equals(object? obj)
            {
                return obj is TestDataType type &&
                       Property == type.Property &&
                       EqualityComparer<TestSubDataType?>.Default.Equals(SubData, type.SubData);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Property, SubData);
            }
        }

        protected class TestSubDataType
        {
            public int SubProperty { get; set; }

            public override bool Equals(object? obj)
            {
                return obj is TestSubDataType type &&
                       SubProperty == type.SubProperty;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(SubProperty);
            }
        }

        protected interface IEditableSettingsTestCollection : IEditableSettingsCollectionSpecific<TestDataType>
        {
            public IEditableSettingSpecific<int> PropertySetting { get; }

            public IEditableSettingsTestSubCollection SubCollection { get; }
        }

        protected interface IEditableSettingsTestSubCollection : IEditableSettingsCollectionSpecific<TestSubDataType>
        {
            public IEditableSettingSpecific<int> SubPropertySetting { get; }
        }

        protected class EditableSettingsTestCollection : EditableSettingsCollectionV2<TestDataType>, IEditableSettingsTestCollection
        {
            public const string ProperySettingName = $"{nameof(EditableSettingsTestCollection)}.{nameof(PropertySetting)}";
            public const string SubCollectionName = $"{nameof(EditableSettingsTestCollection)}.{nameof(SubCollection)}";

            public EditableSettingsTestCollection(
                [FromKeyedServices(ProperySettingName)]IEditableSettingSpecific<int> propertySetting,
                IEditableSettingsTestSubCollection subCollection)
                : base([propertySetting], [subCollection])
            {
                PropertySetting = propertySetting;
                SubCollection = subCollection;
            }

            public IEditableSettingSpecific<int> PropertySetting { get; protected set; }

            public IEditableSettingsTestSubCollection SubCollection { get; }

            public override TestDataType MapToData()
            {
                return new TestDataType()
                {
                    Property = PropertySetting.ModelValue,
                    SubData = SubCollection.MapToData(),
                };
            }

            protected override bool TryMapEditableSettingsCollectionsFromData(TestDataType data)
            {
                return SubCollection.TryMapFromData(data.SubData);
            }

            protected override bool TryMapEditableSettingsFromData(TestDataType data)
            {
                return PropertySetting.TryUpdateModelDirectly(data.Property);
            }
        }

        protected class EditableSettingsTestSubCollection : EditableSettingsCollectionV2<TestSubDataType>, IEditableSettingsTestSubCollection
        {
            public const string SubPropertySettingName = $"{nameof(EditableSettingsTestSubCollection)}.{nameof(SubPropertySetting)}";

            public EditableSettingsTestSubCollection(
                [FromKeyedServices(SubPropertySettingName)]IEditableSettingSpecific<int> subPropertySetting)
                : base([subPropertySetting], [])
            {
                SubPropertySetting = subPropertySetting;
            }

            public IEditableSettingSpecific<int> SubPropertySetting { get; protected set; }

            public override TestSubDataType MapToData()
            {
                return new TestSubDataType()
                {
                    SubProperty = SubPropertySetting.ModelValue,
                };
            }

            protected override bool TryMapEditableSettingsCollectionsFromData(TestSubDataType data)
            {
                return true;
            }

            protected override bool TryMapEditableSettingsFromData(TestSubDataType data)
            {
                return SubPropertySetting.TryUpdateModelDirectly(data.SubProperty);
            }
        }

        #endregion TestClasses

        #region InitDI

        protected static IEditableSettingsTestCollection InitTestObject(
            string testSettingName,
            string testSubSettingName,
            int testSettingInitialValue,
            int testSubSettingInitialValue)
        {
            ServiceCollection services = new ServiceCollection();
            services.AddTransient<IEditableSettingsTestCollection, EditableSettingsTestCollection>();
            services.AddKeyedTransient<IEditableSettingSpecific<int>>(
                EditableSettingsTestCollection.ProperySettingName, (_, _) =>
                    new EditableSettingV2<int>(
                        testSettingName,
                        testSettingInitialValue,
                        UserInputParsers.IntParser,
                        ModelValueValidators.DefaultIntValidator,
                        autoUpdateFromInterface: false));
            services.AddTransient<IEditableSettingsTestSubCollection, EditableSettingsTestSubCollection>();
            services.AddKeyedTransient<IEditableSettingSpecific<int>>(
                EditableSettingsTestSubCollection.SubPropertySettingName, (_, _) =>
                    new EditableSettingV2<int>(
                        testSubSettingName,
                        testSubSettingInitialValue,
                        UserInputParsers.IntParser,
                        ModelValueValidators.DefaultIntValidator,
                        autoUpdateFromInterface: false));

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            IEditableSettingsTestCollection testObject = serviceProvider.GetRequiredService<IEditableSettingsTestCollection>();
            return testObject;
        }

        #endregion InitDI

        #region Tests

        [TestMethod]
        public void EditableSettingsCollection_Construction()
        {
            string testSettingName = "Test Setting";
            string testSubSettingName = "Test Sub Setting";
            int testSettingInitialValue = 0;
            int testSubSettingInitialValue = 1;

            IEditableSettingsTestCollection testObject =
                InitTestObject(testSettingName, testSubSettingName, testSettingInitialValue, testSubSettingInitialValue);

            Assert.IsNotNull(testObject);
            Assert.IsNotNull(testObject.PropertySetting);
            Assert.AreEqual(testSettingInitialValue, testObject.PropertySetting.ModelValue);
            Assert.AreEqual(testSettingName, testObject.PropertySetting.DisplayName);

            Assert.IsNotNull(testObject.SubCollection);
            Assert.IsNotNull(testObject.SubCollection.SubPropertySetting);
            Assert.AreEqual(testSubSettingInitialValue, testObject.SubCollection.SubPropertySetting.ModelValue);
            Assert.AreEqual(testSubSettingName, testObject.SubCollection.SubPropertySetting.DisplayName);
        }

        [TestMethod]
        public void EditableSettingsCollection_PropertyChange()
        {
            string testSettingName = "Test Setting";
            string testSubSettingName = "Test Sub Setting";
            int testSettingInitialValue = 0;
            int testSubSettingInitialValue = 1;

            IEditableSettingsTestCollection testObject =
                InitTestObject(testSettingName, testSubSettingName, testSettingInitialValue, testSubSettingInitialValue);

            // Test Case: property is changed
            int propertyChangedHandlerCalls = 0;
            void TestSettingChangedHandler(object? sender, EventArgs e)
            {
                propertyChangedHandlerCalls++;
            }

            testObject.AnySettingChanged += TestSettingChangedHandler;
            testObject.PropertySetting.InterfaceValue = "2";
            testObject.PropertySetting.TryUpdateFromInterface();

            Assert.AreEqual(2, testObject.PropertySetting.ModelValue);
            Assert.AreEqual(1, propertyChangedHandlerCalls);

            // Test Case: property of sub collection is changed
            propertyChangedHandlerCalls = 0;
            testObject.SubCollection.SubPropertySetting.InterfaceValue = "3";
            testObject.SubCollection.SubPropertySetting.TryUpdateFromInterface();

            Assert.AreEqual(3, testObject.SubCollection.SubPropertySetting.ModelValue);
            Assert.AreEqual(1, propertyChangedHandlerCalls);
        }

        [TestMethod]
        public void EditableSettingsCollection_MapToData()
        {
            string testSettingName = "Test Setting";
            string testSubSettingName = "Test Sub Setting";
            int testSettingInitialValue = 0;
            int testSubSettingInitialValue = 1;

            IEditableSettingsTestCollection testObject =
                InitTestObject(testSettingName, testSubSettingName, testSettingInitialValue, testSubSettingInitialValue);

            // Test Case: initial values
            TestDataType expectedData = new TestDataType()
            {
                Property = testSettingInitialValue,
                SubData = new TestSubDataType()
                {
                    SubProperty = testSubSettingInitialValue,
                }
            };

            TestDataType actualData = testObject.MapToData();
            Assert.IsNotNull(actualData);
            Assert.AreEqual(expectedData, actualData);

            // Test case: properties change
            testObject.PropertySetting.InterfaceValue = "2";
            testObject.PropertySetting.TryUpdateFromInterface();
            testObject.SubCollection.SubPropertySetting.InterfaceValue = "3";
            testObject.SubCollection.SubPropertySetting.TryUpdateFromInterface();

            expectedData = new TestDataType()
            {
                Property = 2,
                SubData = new TestSubDataType()
                {
                    SubProperty = 3,
                }
            };

            actualData = testObject.MapToData();
            Assert.IsNotNull(actualData);
            Assert.AreEqual(expectedData, actualData);
        }

        #endregion Tests
    }
}
