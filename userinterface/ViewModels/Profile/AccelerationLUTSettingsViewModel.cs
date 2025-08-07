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
            
            LUTAccelBE.Data.InterfaceValue = coordinateString;
            LUTAccelBE.Data.TryUpdateFromInterface();
        }
    }
}