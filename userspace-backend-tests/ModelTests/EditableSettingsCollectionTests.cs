using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
    public class EditableSettingsCollectionTests
    {
        protected class TestDataType
        {
            public int Property { get; set; }
        }

        protected interface IEditableSettingsTestCollection : IEditableSettingsCollectionV2
        {
            public IEditableSettingSpecific<int> PropertySetting { get; }
        }

        protected class EditableSettingsTestCollection : EditableSettingsCollectionV2<TestDataType>, IEditableSettingsTestCollection
        {
            public const string ProperySettingName = $"{nameof(EditableSettingsTestCollection)}.{nameof(PropertySetting)}";

            public EditableSettingsTestCollection(
                [FromKeyedServices(ProperySettingName)]IEditableSettingSpecific<int> propertySetting)
                : base([propertySetting], [])
            {
                PropertySetting = propertySetting;
            }

            public IEditableSettingSpecific<int> PropertySetting { get; protected set; }

            public override TestDataType MapToData()
            {
                return new TestDataType()
                {
                    Property = PropertySetting.ModelValue,
                };
            }
        }

        [TestMethod]
        public void EditableSettingsCollection_Construction()
        {
            string testSettingName = "Test Setting";
            int testSettingInitialValue = 0;

            var services = new ServiceCollection();
            services.AddTransient<IEditableSettingsTestCollection, EditableSettingsTestCollection>();
            services.AddKeyedTransient<IEditableSettingSpecific<int>>(
                EditableSettingsTestCollection.ProperySettingName, (_, _) =>
                    new EditableSettingV2<int>(testSettingName, testSettingInitialValue, UserInputParsers.IntParser, ModelValueValidators.DefaultIntValidator, false));
            ServiceProvider serviceProvider = services.BuildServiceProvider();

            IEditableSettingsTestCollection testObject = serviceProvider.GetRequiredService<IEditableSettingsTestCollection>();
            Assert.IsNotNull(testObject);
            Assert.AreEqual(testSettingInitialValue, testObject.PropertySetting.ModelValue);
            Assert.AreEqual(testSettingName, testObject.PropertySetting.DisplayName);
        }
    }
}
