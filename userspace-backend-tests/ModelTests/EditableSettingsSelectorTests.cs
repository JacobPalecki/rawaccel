using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using userspace_backend.Model.EditableSettings;

namespace userspace_backend_tests.ModelTests
{
    [TestClass]
    public class EditableSettingsSelectorTests
    {

        #region TestClasses

        public abstract class TestDataAbstract
        {
            public enum TestDataType
            {
                A,
                B,
            }

            public TestDataType Type { get; set; }
        }

        public class TestDataA
        {
            public int PropertyA;
        }

        public class TestDataB
        {
            public int PropertyB;
        }

        public interface IEditableSettingsTestA : IEditableSettingsCollectionSpecific<TestDataA>
        {
            public IEditableSettingSpecific<int> PropertyA { get; }
        }

        public interface IEditableSettingsTestB : IEditableSettingsCollectionSpecific<TestDataB>
        {
            public IEditableSettingSpecific<int> PropertyB { get; }
        }

        public interface IEditableSettingsTestSelector : IEditableSettingsSelector<
                TestDataAbstract.TestDataType,
                IEditableSettingsCollectionSpecific<TestDataAbstract>,
                TestDataAbstract>
        {
        }

        public class EditableSettingsTestA : EditableSettingsCollectionV2<TestDataA>, IEditableSettingsTestA
        {
            public EditableSettingsTestA(IEditableSettingSpecific<int> propertyA)
                : base([propertyA], [])
            {
                PropertyA = propertyA;
            }

            public IEditableSettingSpecific<int> PropertyA { get; }

            public override TestDataA MapToData()
            {
                return new TestDataA()
                {
                    PropertyA = PropertyA.ModelValue,
                };
            }
        }

        public class EditableSettingsTestB : EditableSettingsCollectionV2<TestDataB>, IEditableSettingsTestB
        {
            public EditableSettingsTestB(IEditableSettingSpecific<int> propertyB)
                : base([propertyB], [])
            {
                PropertyB = propertyB;
            }

            public IEditableSettingSpecific<int> PropertyB { get; }

            public override TestDataB MapToData()
            {
                return new TestDataB()
                {
                    PropertyB = PropertyB.ModelValue,
                };
            }
        }

        public class EditableSettingsTestSelector : EditableSettingsSelector<
                TestDataAbstract.TestDataType,
                IEditableSettingsCollectionSpecific<TestDataAbstract>,
                TestDataAbstract>
            , IEditableSettingsTestSelector
        {
            public EditableSettingsTestSelector(
                IEditableSettingSpecific<TestDataAbstract.TestDataType> selection,
                IServiceProvider serviceProvider)
                : base(selection, serviceProvider, [], [])
            {
            }

            public override TestDataAbstract MapToData()
            {
                return GetSelectable(Selection.ModelValue).MapToData();
            }
        }

        #endregion TestClasses
    }
}
