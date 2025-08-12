using userinterface.Services;
using userinterface.ViewModels.Controls;
using userspace_backend.Logging;
using BE = userspace_backend.Model.AccelDefinitions;

namespace userinterface.ViewModels.Profile
{
    public partial class AccelerationLUTSettingsViewModel : ViewModelBase
    {
        private readonly ILoggingService? loggingService;
        private readonly INotificationService? notificationService;
        private readonly IModalService? modalService;
        private readonly LocalizationService? localizationService;

        public AccelerationLUTSettingsViewModel(BE.LookupTableDefinitionModel lutAccelBE, ILoggingService? loggingService = null, INotificationService? notificationService = null, IModalService? modalService = null, LocalizationService? localizationService = null)
        {
            LUTAccelBE = lutAccelBE;
            this.loggingService = loggingService;
            this.notificationService = notificationService;
            this.modalService = modalService;
            this.localizationService = localizationService;

            PointsCollection = new LUTPointsCollectionViewModel(notificationService, loggingService, modalService, localizationService);

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

            loggingService?.LogDebug(LogSource.LUT, "Setting coordinates: {Coordinates}", coordinateString);
            loggingService?.LogDebug(LogSource.LUT, "LUTAccelBE type: {Type}", LUTAccelBE.GetType().Name);

            LUTAccelBE.Data.InterfaceValue = coordinateString;
            var success = LUTAccelBE.Data.TryUpdateFromInterface();

            loggingService?.LogDebug(LogSource.LUT, "TryUpdateFromInterface result: {Success}", success);
            loggingService?.LogDebug(LogSource.LUT, "CurrentValidatedValue: {Value}", LUTAccelBE.Data.CurrentValidatedValue.ToString());

            // WORKAROUND: Manually trigger AnySettingChanged event if automatic event chain fails
            if (success)
            {
                loggingService?.LogDebug(LogSource.LUT, "Manually triggering AnySettingChanged event");
                LUTAccelBE.AnySettingChanged?.Invoke(LUTAccelBE, System.EventArgs.Empty);
            }
        }
    }
}