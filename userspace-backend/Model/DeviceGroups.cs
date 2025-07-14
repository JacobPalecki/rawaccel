using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using userspace_backend.Model.EditableSettings;

namespace userspace_backend.Model
{
    public interface IDeviceGroups
    {

    }

    public class DeviceGroups : EditableSettingsCollection<IEnumerable<string>>
    {
        public static readonly DeviceGroupModel DefaultDeviceGroup =
            new DeviceGroupModel("Default", ModelValueValidators.AllChangesInvalidStringValidator);

        public DeviceGroups(IEnumerable<string> devices)
            : base(devices)
        {
            GroupNameChangeValidator = new DeviceGroupValidator(this);
        }

        public ObservableCollection<DeviceGroupModel> DeviceGroupModels { get; set; }

        protected DeviceGroupValidator GroupNameChangeValidator { get; set; }

        public bool TryGetDeviceGroup(string name, out DeviceGroupModel? deviceGroup)
        {
            deviceGroup = DeviceGroupModels.FirstOrDefault(
                g => string.Equals(g.ModelValue, name, StringComparison.InvariantCultureIgnoreCase));

            return deviceGroup is not null;
        }

        public DeviceGroupModel AddOrGetDeviceGroup(string deviceGroupName)
        {
            if (!TryGetDeviceGroup(deviceGroupName, out DeviceGroupModel? deviceGroup))
            {
                deviceGroup = new DeviceGroupModel(deviceGroupName, GroupNameChangeValidator);
                DeviceGroupModels.Add(deviceGroup);
            }

            return deviceGroup;
        }

        protected bool TryGetDefaultDeviceGroup([MaybeNullWhen(false)] out string defaultName)
        {
            for (int i = 0; i < 10; i++)
            {
                string nameToAdd = $"DeviceGroup{i}";
                if (TryGetDeviceGroup(nameToAdd, out _))
                {
                    continue;
                }

                defaultName = nameToAdd;
                return true;
            }

            defaultName = null;
            return false;
        }

        public bool TryAddDeviceGroup(string? deviceGroupName = null)
        {
            if (deviceGroupName is null)
            {
                if (!TryGetDefaultDeviceGroup(out var defaultName))
                {
                    return false;
                }

                deviceGroupName = defaultName;
            }
            else if (TryGetDeviceGroup(deviceGroupName, out _))
            {
                return false;
            }

            DeviceGroupModel deviceGroup = new DeviceGroupModel(deviceGroupName, GroupNameChangeValidator);
            DeviceGroupModels.Add(deviceGroup);
            return true;
        }

        public bool RemoveDeviceGroup(DeviceGroupModel deviceGroup)
        {
            return DeviceGroupModels.Remove(deviceGroup);
        }

        public override IEnumerable<string> MapToData()
        {
            return DeviceGroupModels.Select(g => g.ModelValue);
        }

        protected override IEnumerable<IEditableSetting> EnumerateEditableSettings()
        {
            return DeviceGroupModels;
        }

        protected override IEnumerable<IEditableSettingsCollectionV2> EnumerateEditableSettingsCollections()
        {
            return [];
        }

        protected override void InitEditableSettingsAndCollections(IEnumerable<string> dataObject)
        {
            // This initialization does not set up all device group models.
            // That is done in backend construction in order to point the devices to their groups.
            DeviceGroupModels = new ObservableCollection<DeviceGroupModel>() { DefaultDeviceGroup };
        }
    }

    public class DeviceGroupValidator(DeviceGroups deviceGroups) : IModelValueValidator<string>
    {
        protected DeviceGroups DeviceGroups { get; } = deviceGroups;

        public bool Validate(string value)
        {
            return !DeviceGroups.TryGetDeviceGroup(value, out _);
        }
    }
}
