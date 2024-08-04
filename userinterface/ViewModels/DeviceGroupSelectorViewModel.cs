﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using userspace_backend.Model;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels
{
    public partial class DeviceGroupSelectorViewModel : ViewModelBase
    {
        protected DeviceGroupModel selectedEntry;

        public DeviceGroupSelectorViewModel(BE.DeviceModel device, BE.DeviceGroups deviceGroupsBE)
        {
            Device = device;
            DeviceGroupsBE = deviceGroupsBE;
            RefreshSelectedDeviceGroup();
        }

        protected BE.DeviceModel Device { get; set; }

        protected BE.DeviceGroups DeviceGroupsBE { get; }

        public ObservableCollection<DeviceGroupModel> DeviceGroupEntries { get => DeviceGroupsBE.DeviceGroupModels; }

        public BE.DeviceGroupModel SelectedEntry
        {
            get => selectedEntry;
            set
            {
                if (DeviceGroupEntries.Contains(value))
                {
                    Device.DeviceGroup = value;
                    selectedEntry = value;
                }
            }
        }

        public bool IsValid { get; set; }

        public void RefreshSelectedDeviceGroup()
        {
            if (!DeviceGroupEntries.Contains(Device.DeviceGroup))
            {
                IsValid = false;
                SelectedEntry = BE.DeviceGroups.DefaultDeviceGroup;
                return;
            }

            IsValid = true;
            selectedEntry = Device.DeviceGroup;
            return;
        }
    }
}
