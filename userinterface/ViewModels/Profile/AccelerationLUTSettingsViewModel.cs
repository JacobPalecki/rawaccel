using userinterface.ViewModels.Controls;
using userspace_backend.Model.AccelDefinitions;
using BE = userspace_backend.Model.AccelDefinitions;

namespace userinterface.ViewModels.Profile
{
    public partial class AccelerationLUTSettingsViewModel : ViewModelBase
    {
        public AccelerationLUTSettingsViewModel(BE.LookupTableDefinitionModel lutAccelBE)
        {
            LUTAccelBE = lutAccelBE;
            
            PointsCollection = new LUTPointsCollectionViewModel();
            
            LoadPointsFromBackend();
            
            PointsCollection.CollectionChanged += OnPointsCollectionChanged;
        }

        public BE.LookupTableDefinitionModel LUTAccelBE { get; }

        public LUTPointsCollectionViewModel PointsCollection { get; set; }

        private void LoadPointsFromBackend()
        {
            var data = LUTAccelBE.Data.ModelValue.Data;
            PointsCollection.LoadFromData(data);
        }

        private void OnPointsCollectionChanged(object? sender, CollectionChangedEventArgs e)
        {
            var coordinates = PointsCollection.ConvertToData();
            var coordinateString = string.Join(",", coordinates);
            
            // DEBUG: Log the data being sent
            System.Diagnostics.Debug.WriteLine($"[LUT DEBUG] Setting coordinates: {coordinateString}");
            System.Diagnostics.Debug.WriteLine($"[LUT DEBUG] LUTAccelBE type: {LUTAccelBE?.GetType().Name}");
            
            LUTAccelBE.Data.InterfaceValue = coordinateString;
            var success = LUTAccelBE.Data.TryUpdateFromInterface();
            
            // DEBUG: Log the result
            System.Diagnostics.Debug.WriteLine($"[LUT DEBUG] TryUpdateFromInterface result: {success}");
            System.Diagnostics.Debug.WriteLine($"[LUT DEBUG] CurrentValidatedValue: {LUTAccelBE.Data.CurrentValidatedValue?.ToString()}");
            
            // WORKAROUND: Manually trigger AnySettingChanged event if automatic event chain fails
            if (success)
            {
                System.Diagnostics.Debug.WriteLine("[LUT DEBUG] Manually triggering AnySettingChanged");
                LUTAccelBE.AnySettingChanged?.Invoke(LUTAccelBE, System.EventArgs.Empty);
            }
        }
    }
}