using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
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

            public abstract TestDataType Type { get; }

            public static DefaultModelValueValidator<TestDataType> DefaultTestDataTypeValidator =
                new DefaultModelValueValidator<TestDataType>();

            public override bool Equals(object? obj)
            {
                return obj is TestDataAbstract @abstract &&
                       Type == @abstract.Type;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Type);
            }
        }

        public class TestDataTypeParser : IUserInputParser<TestDataAbstract.TestDataType>
        {
            public static TestDataTypeParser Singleton = new TestDataTypeParser();

            public bool TryParse(string input, out TestDataAbstract.TestDataType parsedValue)
            {
                if (Enum.TryParse(input, ignoreCase: true, out parsedValue))
                {
                    return true;
                }

                parsedValue = default;
                return false;
            }
        }

        public class TestDataA : TestDataAbstract
        {
            public int PropertyA;

            public override TestDataType Type => TestDataType.A;

            public override bool Equals(object? obj)
            {
                return obj is TestDataA a &&
                       base.Equals(obj) &&
                       Type == a.Type &&
                       PropertyA == a.PropertyA;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(base.GetHashCode(), Type, PropertyA);
            }
        }

        public class TestDataB : TestDataAbstract
        {
            public int PropertyB;

            public override TestDataType Type => TestDataType.B;

            public override bool Equals(object? obj)
            {
                return obj is TestDataB b &&
                       base.Equals(obj) &&
                       Type == b.Type &&
                       PropertyB == b.PropertyB;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(base.GetHashCode(), Type, PropertyB);
            }
        }

        public interface IEditableSettingsTestA : IEditableSettingsCollectionSpecific<TestDataAbstract>
        {
            public IEditableSettingSpecific<int> PropertyA { get; }
        }

        public interface IEditableSettingsTestB : IEditableSettingsCollectionSpecific<TestDataAbstract>
        {
            public IEditableSettingSpecific<int> PropertyB { get; }
        }

        public interface IEditableSettingsTestSelector : IEditableSettingsSelector<
                TestDataAbstract.TestDataType,
                TestDataAbstract>
        {
        }

        public class EditableSettingsTestA : EditableSettingsCollectionV2<TestDataAbstract>, IEditableSettingsTestA
        {
            public const string PropertyAName = $"{nameof(EditableSettingsTestA)}.{nameof(PropertyA)}";

            public EditableSettingsTestA([FromKeyedServices(PropertyAName)]IEditableSettingSpecific<int> propertyA)
                : base([propertyA], [])
            {
                PropertyA = propertyA;
            }

            public IEditableSettingSpecific<int> PropertyA { get; }

            public override TestDataAbstract MapToData()
            {
                return new TestDataA()
                {
                    PropertyA = PropertyA.ModelValue,
                };
            }
        }

        public class EditableSettingsTestB : EditableSettingsCollectionV2<TestDataAbstract>, IEditableSettingsTestB
        {
            public const string PropertyBName = $"{nameof(EditableSettingsTestB)}.{nameof(PropertyB)}";

            public EditableSettingsTestB([FromKeyedServices(PropertyBName)]IEditableSettingSpecific<int> propertyB)
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
                TestDataAbstract>
            , IEditableSettingsTestSelector
        {
            public const string SelectionName = $"{nameof(EditableSettingsTestSelector)}.{nameof(Selection)}";

            public EditableSettingsTestSelector(
                [FromKeyedServices(SelectionName)]IEditableSettingSpecific<TestDataAbstract.TestDataType> selection,
                IServiceProvider serviceProvider)
                : base(selection, serviceProvider, [], [])
            {
            }
        }

        #endregion TestClasses

        #region InitDI

        protected static IEditableSettingsTestSelector InitTestObject(
            string selectionName,
            string aName,
            string bName,
            TestDataAbstract.TestDataType selectionInitialValue,
            int aInitialValue,
            int bInitialValue)
        {
            ServiceCollection services = new ServiceCollection();
            services.AddTransient<IEditableSettingsTestSelector, EditableSettingsTestSelector>();
            services.AddKeyedTransient<IEditableSettingSpecific<TestDataAbstract.TestDataType>>(
                EditableSettingsTestSelector.SelectionName, (_, _) => 
                    new EditableSettingV2<TestDataAbstract.TestDataType>(
                        selectionName,
                        selectionInitialValue,
                        TestDataTypeParser.Singleton,
                        TestDataAbstract.DefaultTestDataTypeValidator,
                        autoUpdateFromInterface: false));
            services.AddTransient<IEditableSettingsTestA, EditableSettingsTestA>();
            services.AddKeyedTransient<IEditableSettingsCollectionSpecific<TestDataAbstract>, EditableSettingsTestA>(
                EditableSettingsSelectorHelper.GetSelectionKey(TestDataAbstract.TestDataType.A));
            services.AddKeyedTransient<IEditableSettingSpecific<int>>(
                EditableSettingsTestA.PropertyAName, (_, _) => 
                    new EditableSettingV2<int>(
                        aName,
                        aInitialValue,
                        UserInputParsers.IntParser,
                        ModelValueValidators.DefaultIntValidator,
                        autoUpdateFromInterface: false));
            services.AddTransient<IEditableSettingsTestB, EditableSettingsTestB>();
            services.AddKeyedTransient<IEditableSettingsCollectionSpecific<TestDataAbstract>, EditableSettingsTestB>(
                EditableSettingsSelectorHelper.GetSelectionKey(TestDataAbstract.TestDataType.B));
            services.AddKeyedTransient<IEditableSettingSpecific<int>>(
                EditableSettingsTestB.PropertyBName, (_, _) => 
                    new EditableSettingV2<int>(
                        bName,
                        bInitialValue,
                        UserInputParsers.IntParser,
                        ModelValueValidators.DefaultIntValidator,
                        autoUpdateFromInterface: false));

            ServiceProvider serviceProvider = services.BuildServiceProvider();
            IEditableSettingsTestSelector testObject = serviceProvider.GetRequiredService<IEditableSettingsTestSelector>();
            return testObject;
        }

        #endregion InitDI

        #region Tests

        [TestMethod]
        public void EditableSettingsSelector_Construction()
        {
            string selectionName = "Selection";
            string aName = "Property A";
            string bName = "Property B";
            TestDataAbstract.TestDataType selectionInitialValue = TestDataAbstract.TestDataType.A;
            int aInitialValue = 1;
            int bInitialValue = 2;

            IEditableSettingsTestSelector testObject =
                InitTestObject(selectionName, aName, bName, selectionInitialValue, aInitialValue, bInitialValue);
            Assert.IsNotNull(testObject);
            Assert.IsNotNull(testObject.Selection);
            Assert.AreEqual(selectionInitialValue, testObject.Selection.ModelValue);

            IEditableSettingsCollectionSpecific<TestDataAbstract> testObjectAbstractA =
                testObject.GetSelectable(TestDataAbstract.TestDataType.A);
            Assert.IsNotNull(testObjectAbstractA);
            IEditableSettingsTestA testObjectA = testObjectAbstractA as IEditableSettingsTestA;
            Assert.IsNotNull(testObjectA);
            Assert.AreEqual(aName, testObjectA.PropertyA.DisplayName);
            Assert.AreEqual(aInitialValue, testObjectA.PropertyA.ModelValue);

            IEditableSettingsCollectionSpecific<TestDataAbstract> testObjectAbstractB =
                testObject.GetSelectable(TestDataAbstract.TestDataType.B);
            Assert.IsNotNull(testObjectAbstractB);
            IEditableSettingsTestB testObjectB = testObjectAbstractB as IEditableSettingsTestB;
            Assert.IsNotNull(testObjectB);
            Assert.AreEqual(bName, testObjectB.PropertyB.DisplayName);
            Assert.AreEqual(bInitialValue, testObjectB.PropertyB.ModelValue);
        }

        [TestMethod]
        public void EditableSettingsSelector_SelectionChange()
        {
            string selectionName = "Selection";
            string aName = "Property A";
            string bName = "Property B";
            TestDataAbstract.TestDataType selectionInitialValue = TestDataAbstract.TestDataType.A;
            int aInitialValue = 1;
            int bInitialValue = 2;

            IEditableSettingsTestSelector testObject =
                InitTestObject(selectionName, aName, bName, selectionInitialValue, aInitialValue, bInitialValue);

            int propertyChangedHandlerCalls = 0;
            void TestSettingChangedHandler(object? sender, EventArgs e)
            {
                propertyChangedHandlerCalls++;
            }

            testObject.AnySettingChanged += TestSettingChangedHandler;
            testObject.Selection.InterfaceValue = "b";
            testObject.Selection.TryUpdateFromInterface();

            Assert.AreEqual(TestDataAbstract.TestDataType.B, testObject.Selection.ModelValue);
            Assert.AreEqual(1, propertyChangedHandlerCalls);
        }
    

        [TestMethod]
        public void EditableSettingsSelector_MapToData()
        {
            string selectionName = "Selection";
            string aName = "Property A";
            string bName = "Property B";
            TestDataAbstract.TestDataType selectionInitialValue = TestDataAbstract.TestDataType.A;
            int aInitialValue = 1;
            int bInitialValue = 2;

            IEditableSettingsTestSelector testObject =
                InitTestObject(selectionName, aName, bName, selectionInitialValue, aInitialValue, bInitialValue);

            // Test case: initial values
            TestDataAbstract expectedData = new TestDataA()
            {
                PropertyA = aInitialValue,
            };
            TestDataAbstract actualData = testObject.MapToData();
            Assert.AreEqual(expectedData, actualData);

            // Test case: selection changes
            testObject.Selection.InterfaceValue = "b";
            testObject.Selection.TryUpdateFromInterface();
            expectedData = new TestDataB()
            {
                PropertyB = bInitialValue,
            };
            actualData = testObject.MapToData();
            Assert.AreEqual(expectedData, actualData);
        }

        #endregion Tests
    }
}
