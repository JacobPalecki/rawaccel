using System;
using System.Collections.Generic;
using System.Linq;
using DATA = userspace_backend.Data;
using userspace_backend.Model;
using Microsoft.Extensions.DependencyInjection;

namespace userspace_backend
{
    public interface IBackEnd
    {
        void Load();

        void Apply();
    }

    public class BackEnd
    {

        public BackEnd(IBackEndLoader backEndLoader)
        {
            // TODO: fully construct BackEnd via DI
            ServiceCollection services = new ServiceCollection();
            IServiceProvider serviceProvider = BackEndComposer.Compose(services);

            BackEndLoader = backEndLoader;
            Devices = new DevicesModel(serviceProvider.GetRequiredService<ISystemDevicesProvider>());
            Profiles = new ProfilesModel(serviceProvider);
        }

        public DevicesModel Devices { get; set; }

        public MappingsModel Mappings { get; set; }

        public ProfilesModel Profiles { get; set; }

        protected IBackEndLoader BackEndLoader { get; set; }

        public void Load()
        {
            IEnumerable<DATA.Device> devicesData = BackEndLoader.LoadDevices(); ;
            LoadDevicesFromData(devicesData);

            IEnumerable<DATA.Profile> profilesData = BackEndLoader.LoadProfiles(); ;
            LoadProfilesFromData(profilesData);

            DATA.MappingSet mappingData = BackEndLoader.LoadMappings();
            Mappings = new MappingsModel(mappingData, Devices.DeviceGroups, Profiles);
        }

        protected void LoadDevicesFromData(IEnumerable<DATA.Device> devicesData)
        {
            foreach(var deviceData in devicesData)
            {
                Devices.TryAddDevice(deviceData);
            }
        }

        protected void LoadProfilesFromData(IEnumerable<DATA.Profile> profileData)
        {
            foreach (var profile in profileData)
            {
                Profiles.TryAdd(profile);
            }
        }

        public void Apply()
        {
            try
            {
                //WriteToDriver();
            }
            catch (Exception ex)
            {
                return;
            }

            WriteSettingsToDisk();
        }

        protected void WriteSettingsToDisk()
        {
            BackEndLoader.WriteSettingsToDisk(
                Devices.DevicesEnumerable,
                Mappings,
                Profiles.Elements);
        }

        protected void WriteToDriver()
        {
            MappingModel mappingToApply = Mappings.GetMappingToSetActive();
            DriverConfig config = MapToDriverConfig(mappingToApply);
            try
            {
                config.Activate();
            }
            catch(Exception ex)
            {
                // Log this once logging is added
            }
        }

        protected DriverConfig MapToDriverConfig(MappingModel mappingModel)
        {
            IEnumerable<DeviceSettings> configDevices = MapToDriverDevices(mappingModel);
            IEnumerable<Profile> configProfiles = MapToDriverProfiles(mappingModel);

            DriverConfig config = DriverConfig.GetDefault();
            config.profiles = configProfiles.ToList();
            config.devices = configDevices.ToList();
            config.accels = configProfiles.Select(p => new ManagedAccel(p)).ToList();
            return config;
        }

        protected IEnumerable<DeviceSettings> MapToDriverDevices(MappingModel mapping)
        {
            return mapping.IndividualMappings.SelectMany(
                dg => MapToDriverDevices(dg.DeviceGroup, dg.Profile.Name.ModelValue));
        }

        protected IEnumerable<Profile> MapToDriverProfiles(MappingModel mapping)
        {
            IEnumerable<ProfileModel> ProfilesToMap = mapping.IndividualMappings.Select(m => m.Profile).Distinct();
            return ProfilesToMap.Select(p => p.CurrentValidatedDriverProfile);
        }

        protected IEnumerable<DeviceSettings> MapToDriverDevices(DeviceGroupModel dg, string profileName)
        {
            IEnumerable<DeviceModel> deviceModels = Devices.Devices.Where(d => d.DeviceGroup.Equals(dg));
            return deviceModels.Select(dm => MapToDriverDevice(dm, profileName));
        }

        protected DeviceSettings MapToDriverDevice(DeviceModel deviceModel, string profileName)
        {
            return new DeviceSettings()
            {
                id = deviceModel.HardwareID.ModelValue,
                name = deviceModel.Name.ModelValue,
                profile = profileName,
                config = new DeviceConfig()
                {
                    disable = deviceModel.Ignore.ModelValue,
                    dpi = deviceModel.DPI.ModelValue,
                    pollingRate = deviceModel.DPI.ModelValue,
                    pollTimeLock = false,
                    setExtraInfo = false,
                    maximumTime = 200,
                    minimumTime = 0.1,
                }
            };
        }
    }
}
