using System;
using System.Linq;
using userspace_backend.Model.EditableSettings;
using userspace_backend.Data;
using System.Collections.Specialized;

namespace userspace_backend.Model
{
    public interface IMappingModel : IEditableSettingsCollectionSpecific<Mapping>
    {
    }

    public class MappingModel: NamedEditableSettingsCollection<Mapping>, IMappingModel
    {
        public const string NameDIKey = $"{nameof(MappingModel)}.{nameof(Name)}";

        public MappingModel(
            IEditableSettingSpecific<string> name,
            IModelValueValidator<string> nameValidator,
            IDeviceGroups deviceGroups,
            IProfilesModel profiles)
            : base(name, [], [])
        {
            NameValidator = nameValidator;
            SetActive = true;
            DeviceGroups = deviceGroups;
            Profiles = profiles;
        }

        public bool SetActive { get; set; }

        protected IModelValueValidator<string> NameValidator { get; }

        protected IDeviceGroups DeviceGroups { get; }

        protected IProfilesModel Profiles { get; }

        public override Mapping MapToData()
        {
            Mapping mapping = new Mapping()
            {
                Name = Name.ModelValue,
                GroupsToProfiles = new Mapping.GroupsToProfilesMapping(),
            };

            foreach (var group in IndividualMappings)
            {
                mapping.GroupsToProfiles.Add(group.DeviceGroup.ModelValue, group.Profile.Name.ModelValue);
            }

            return mapping;
        }

        protected void InitIndividualMappings(Mapping dataObject)
        {
            foreach (var kvp in dataObject.GroupsToProfiles)
            {
                TryAddMapping(kvp.Key, kvp.Value);
            }
        }

        public bool TryAddMapping(string deviceGroupName, string profileName)
        {
            if (!DeviceGroups.TryGetDeviceGroup(deviceGroupName, out DeviceGroupModel? deviceGroup)
                || deviceGroup == null
                || IndividualMappings.Any(m => m.DeviceGroup.Equals(deviceGroup)))
            {
                return false;
            }

            if (!Profiles.TryGetElement(profileName, out IProfileModel? profile)
                || profile == null)
            {
                return false;
            }

            MappingGroup group = new MappingGroup()
            {
                DeviceGroup = deviceGroup,
                Profile = profile,
                Profiles = Profiles,
            };

            IndividualMappings.Add(group);
            return true;
        }

        protected void FindDeviceGroupsStillUnmapped()
        {
            DeviceGroupsStillUnmapped.Clear();

            foreach (DeviceGroupModel group in DeviceGroups.DeviceGroupModels)
            {
                if (!IndividualMappings.Any(m => m.DeviceGroup.Equals(group)))
                {
                    DeviceGroupsStillUnmapped.Add(group);
                }
            }
        }

        protected void OnIndividualMappingsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            FindDeviceGroupsStillUnmapped();
        }

        protected override bool TryMapEditableSettingsFromData(Mapping data)
        {
            return Name.TryUpdateModelDirectly(data.Name);
        }

        protected override bool TryMapEditableSettingsCollectionsFromData(Mapping data)
        {
            return true;
        }
    }

    public class MappingGroup
    {
        public DeviceGroupModel DeviceGroup { get; set; }

        public IProfileModel Profile { get; set; }

        // This is here for easy binding
        public IProfilesModel Profiles { get; set; }
    }
}
