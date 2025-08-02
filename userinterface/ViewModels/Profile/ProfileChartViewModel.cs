using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.Immutable;
using LiveChartsCore;
using System.Diagnostics;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using userinterface.Commands;
using userinterface.Interfaces;
using userinterface.Services;
using userspace_backend.Display;
using userspace_backend.Hardware;
using userspace_backend.Model.EditableSettings;
using userspace_backend;
using BE = userspace_backend.Model;
using Data = userspace_backend.Data;

namespace userinterface.ViewModels.Profile
{
    public partial class ProfileChartViewModel : ViewModelBase, IAsyncInitializable
    {
        // Animation settings
        private const int AnimationMilliseconds = 200;

        // Data fitting and bounds
        private const double DataPaddingRatio = 0.1;

        private const double ToleranceThreshold = 0.001;

        // Default chart limits when no data or centering
        private const int DefaultAxisRange = 50;
        private const int DefaultYRange = 1;
        private const int DefaultMaxX = 500;
        private const int DefaultMaxY = 2;

        // Line and stroke thickness
        private const int MainStrokeThickness = 2;
        private const int StandardStrokeThickness = 1;
        private const float SubStrokeThickness = 0.5f;

        // Color transparency values
        private const byte SubSeparatorAlpha = 100;

        private const byte TooltipBackgroundAlpha = 180;

        // Theme color resource keys
        private static readonly string AxisTitleBrush = "PrimaryTextBrush";
        private static readonly string AxisLabelsBrush = "SecondaryTextBrush";
        private static readonly string AxisSeparatorsBrush = "BorderBrush";
        private static readonly string TooltipBackgroundBrush = "CardBackgroundBrush";

        // Axis labeling and text
        private const int AxisNameTextSize = 14;
        private const int AxisTextSize = 12;

        public static readonly TimeSpan AnimationsTime = new(days: 0, hours: 0, minutes: 0, seconds: 0, milliseconds: AnimationMilliseconds);

        private readonly IThemeService themeService;
        private readonly LocalizationService localizationService;
        private readonly PreviewChartRenderer previewRenderer;
        private readonly IMouseTracker mouseTracker;
        private readonly BackEnd backEnd;
        private readonly IDeviceInfoProvider? deviceInfoProvider;
        private BE.ProfileModel currentProfileModel = null!;
        
        private SolidColorPaint? cachedXStroke;
        private SolidColorPaint? cachedYStroke;
        
        private LineSeries<CurvePoint>? xSeries;
        private LineSeries<CurvePoint>? ySeries;
        private ScatterSeries<CurvePoint>? currentSpeedDotSeries;
        private ScatterSeries<CurvePoint>? currentYSpeedDotSeries;
        
        private readonly object syncObject = new object();

        public ProfileChartViewModel(IThemeService themeService, LocalizationService localizationService, PreviewChartRenderer previewRenderer, IMouseTracker mouseTracker, BackEnd backEnd, IDeviceInfoProvider? deviceInfoProvider = null)
        {
            this.themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
            this.localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
            this.previewRenderer = previewRenderer ?? throw new ArgumentNullException(nameof(previewRenderer));
            this.mouseTracker = mouseTracker ?? throw new ArgumentNullException(nameof(mouseTracker));
            this.backEnd = backEnd ?? throw new ArgumentNullException(nameof(backEnd));
            this.deviceInfoProvider = deviceInfoProvider;

            RecreateAxesCommand = new RelayCommand(() => 
            {
                EnsureInteractiveChartLoaded();
                RecreateAxes();
            });
            FitToDataCommand = new RelayCommand(() => 
            {
                EnsureInteractiveChartLoaded();
                FitToData();
            });
            ToggleRealTimeTrackingCommand = new RelayCommand(ToggleRealTimeTracking);
        }

        public bool IsInitialized { get; private set; }

        public bool IsInitializing { get; private set; }
        
        public bool IsInteractiveMode { get; private set; } = false;
        
        public bool IsLoadingChart { get; private set; } = false;
        
        public double ChartOpacity { get; private set; } = 0.0;
        
        private bool hasUserInteracted = false;
        
        public bool IsRealTimeTrackingEnabled { get; private set; } = false;
        
        public string CurrentMouseDevice { get; private set; } = "No device detected";
        
        public string CurrentDeviceDPI { get; private set; } = "Unknown DPI";
        
        private readonly ObservableCollection<CurvePoint> currentSpeedData = new ObservableCollection<CurvePoint>();
        private readonly ObservableCollection<CurvePoint> currentYSpeedData = new ObservableCollection<CurvePoint>();

        private double maxXAxisLimit = 0;
        private double maxYAxisLimit = 0;
        private bool preventAxisResizing = true;
        private double currentMaxXData = 0;
        private double currentMaxYData = 0;

        public object Sync => syncObject;

        private ICurvePreview XCurvePreview { get; set; } = null!;

        private ICurvePreview YCurvePreview { get; set; } = null!;

        private EditableSetting<double> YXRatio { get; set; } = null!;

        public void Initialize(BE.ProfileModel profileModel)
        {
            if (currentProfileModel == profileModel)
                return;

            // Unsubscribe from previous events
            if (currentProfileModel != null)
            {
                UnsubscribeFromEvents();
            }

            currentProfileModel = profileModel;
            XCurvePreview = profileModel.XCurvePreview;
            YCurvePreview = profileModel.YCurvePreview;
            YXRatio = profileModel.YXRatio;

            if (XCurvePreview?.Points != null && YCurvePreview?.Points != null)
            {
                InitializeSeries();
            }
            
            SubscribeToEvents();
        }


        // ================================================================================================
        // INITIALIZATION & SETUP
        // ================================================================================================

        public Task InitializeAsync()
        {
            if (IsInitializing || IsInitialized || currentProfileModel == null)
                return Task.CompletedTask;

            IsInitializing = true;

            try
            {
                IsLoadingChart = true;
                OnPropertyChanged(nameof(IsLoadingChart));
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(100);
                        await Task.Run(() =>
                        {
                            InitializeSeries();
                        });
                        
                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            try
                            {
                                XAxes = CreateXAxes();
                                YAxes = CreateYAxes();
                                TooltipTextPaint = new SolidColorPaint(themeService.GetCachedColor(AxisTitleBrush));
                                TooltipBackgroundPaint = new SolidColorPaint(themeService.GetCachedColor(TooltipBackgroundBrush).WithAlpha(TooltipBackgroundAlpha));
                                
                                this.themeService.ThemeChanged += OnThemeChanged;
                                this.localizationService.PropertyChanged += OnLocalizationChanged;
                                
                                OnPropertyChanged(nameof(XAxes));
                                OnPropertyChanged(nameof(YAxes));
                                OnPropertyChanged(nameof(TooltipTextPaint));
                                OnPropertyChanged(nameof(TooltipBackgroundPaint));
                                OnPropertyChanged(nameof(Series));
                                
                                TransitionToInteractiveMode();
                                
                                IsInitialized = true;
                            }
                            catch (Exception)
                            {
                                IsLoadingChart = false;
                                OnPropertyChanged(nameof(IsLoadingChart));
                            }
                        });
                    }
                    catch (Exception)
                    {
                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            IsLoadingChart = false;
                            OnPropertyChanged(nameof(IsLoadingChart));
                        });
                    }
                });
                
                IsInitialized = true;
            }
            finally
            {
                IsInitializing = false;
            }
            
            return Task.CompletedTask;
        }


        private void EnsureInteractiveChartLoaded()
        {
            if (!IsInteractiveMode && !IsLoadingChart && !hasUserInteracted)
            {
                hasUserInteracted = true;
                _ = ForceInteractiveMode();
            }
        }

        private async Task ForceInteractiveMode()
        {
            if (IsInteractiveMode)
                return;

            IsLoadingChart = true;
            OnPropertyChanged(nameof(IsLoadingChart));

            await Task.Run(() =>
            {
                // LiveCharts automatically uses the full ObservableCollection data
            });

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                OnPropertyChanged(nameof(Series));
                TransitionToInteractiveMode();
            });
        }

        private void InitializeSeries()
        {
            if (cachedXStroke == null)
                cachedXStroke = new SolidColorPaint(SKColors.CornflowerBlue) { StrokeThickness = MainStrokeThickness };
            if (cachedYStroke == null)
                cachedYStroke = new SolidColorPaint(SKColors.OrangeRed) { StrokeThickness = MainStrokeThickness };
            xSeries = new LineSeries<CurvePoint>
            {
                Values = XCurvePreview.Points,
                Fill = null,
                Stroke = cachedXStroke,
                Mapping = (curvePoint, index) => new LiveChartsCore.Kernel.Coordinate(x: curvePoint.MouseSpeed, y: curvePoint.Output),
                GeometrySize = 0,
                GeometryStroke = null,
                GeometryFill = null,
                AnimationsSpeed = TimeSpan.FromMilliseconds(100),
                EasingFunction = LiveChartsCore.EasingFunctions.EaseOut,
                Name = "X Curve Profile",
                LineSmoothness = 0,
                XToolTipLabelFormatter = (chartPoint) => $"Speed: {chartPoint.Coordinate.SecondaryValue:F2}",
                YToolTipLabelFormatter = (chartPoint) => $"X Output: {chartPoint.Coordinate.PrimaryValue:F2}"
            };

            ySeries = new LineSeries<CurvePoint>
            {
                Values = YCurvePreview.Points,
                Fill = null,
                Stroke = cachedYStroke,
                Mapping = (curvePoint, index) => new LiveChartsCore.Kernel.Coordinate(x: curvePoint.MouseSpeed, y: curvePoint.Output),
                GeometrySize = 0,
                GeometryStroke = null,
                GeometryFill = null,
                AnimationsSpeed = TimeSpan.FromMilliseconds(100),
                EasingFunction = LiveChartsCore.EasingFunctions.EaseOut,
                Name = "Y Curve Profile",
                LineSmoothness = 0,
                XToolTipLabelFormatter = (chartPoint) => $"Speed: {chartPoint.Coordinate.SecondaryValue:F2}",
                YToolTipLabelFormatter = (chartPoint) => $"Y Output: {chartPoint.Coordinate.PrimaryValue:F2}"
            };

            Series.Clear();
            Series.Add(xSeries);
            
            InitializeCurrentSpeedDotSeries();
            UpdateYSeriesVisibility();
        }

        private async void TransitionToInteractiveMode()
        {
            IsLoadingChart = false;
            IsInteractiveMode = true;
            ChartOpacity = 0.0;
            
            OnPropertyChanged(nameof(IsLoadingChart));
            OnPropertyChanged(nameof(IsInteractiveMode));
            OnPropertyChanged(nameof(ChartOpacity));
            
            await Task.Delay(100);
            
            ChartOpacity = 1.0;
            OnPropertyChanged(nameof(ChartOpacity));
        }

        public Task SwitchToProfileAsync(BE.ProfileModel profileModel)
        {
            if (currentProfileModel == profileModel && IsInitialized)
                return Task.CompletedTask;

            if (currentProfileModel != null)
            {
                UnsubscribeFromEvents();
            }

            currentProfileModel = profileModel;
            XCurvePreview = profileModel.XCurvePreview;
            YCurvePreview = profileModel.YCurvePreview;
            YXRatio = profileModel.YXRatio;

            SubscribeToEvents();
            if (xSeries != null && ySeries != null)
            {
                UpdateYSeriesVisibility();
            }

            return Task.CompletedTask;
        }

        public ObservableCollection<ISeries> Series { get; set; } = new ObservableCollection<ISeries>();

        public Axis[] XAxes { get; set; } = new Axis[] { new Axis { Name = "Loading...", MinLimit = 0, MaxLimit = 1 } };

        public Axis[] YAxes { get; set; } = new Axis[] { new Axis { Name = "Loading...", MinLimit = 0, MaxLimit = 1 } };

        public SolidColorPaint TooltipTextPaint { get; set; } = new SolidColorPaint(SKColors.Black);

        public SolidColorPaint TooltipBackgroundPaint { get; set; } = new SolidColorPaint(SKColors.White);

        public ICommand RecreateAxesCommand { get; }

        public ICommand FitToDataCommand { get; }
        
        public ICommand ToggleRealTimeTrackingCommand { get; }

        // ================================================================================================
        // PUBLIC METHODS
        // ================================================================================================

        public void FitToData()
        {
            var allPoints = XCurvePreview.Points.ToList();

            if (Math.Abs(YXRatio.CurrentValidatedValue - 1.0) > ToleranceThreshold)
            {
                allPoints.AddRange(YCurvePreview.Points);
            }

            if (allPoints.Count == 0)
            {
                SetDefaultLimits();
                return;
            }

            var (minX, maxX, minY, maxY) = CalculateDataBounds(allPoints);
            if (Math.Abs(maxY - minY) < ToleranceThreshold)
            {
                SetCenteredLimits(minX, maxX, minY, maxY);
            }
            else
            {
                SetPaddedLimits(minX, maxX, minY, maxY);
            }
        }

        public void RecreateAxes(double? xMinLimit = null, double? xMaxLimit = null, double? yMinLimit = null, double? yMaxLimit = null)
        {
            XAxes = CreateXAxes(xMinLimit, xMaxLimit);
            YAxes = CreateYAxes(yMinLimit, yMaxLimit);

            OnPropertyChanged(nameof(XAxes));
            OnPropertyChanged(nameof(YAxes));
        }

        // ================================================================================================
        // CLEANUP & DISPOSAL
        // ================================================================================================

        public void Dispose()
        {
            StopRealTimeTracking();
            
            themeService.ThemeChanged -= OnThemeChanged;
            localizationService.PropertyChanged -= OnLocalizationChanged;
            UnsubscribeFromEvents();
            
            if (cachedXStroke != null)
            {
                cachedXStroke.Dispose();
                cachedXStroke = null;
            }
            if (cachedYStroke != null)
            {
                cachedYStroke.Dispose();
                cachedYStroke = null;
            }
            
            previewRenderer.ClearCache();
        }


        // ================================================================================================
        // EVENT HANDLERS
        // ================================================================================================

        private void SubscribeToEvents()
        {
            if (YXRatio != null)
                YXRatio.PropertyChanged += OnYXRatioChanged;
        }

        private void UnsubscribeFromEvents()
        {
            if (YXRatio != null)
                YXRatio.PropertyChanged -= OnYXRatioChanged;
        }

        private void OnYXRatioChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(EditableSetting<double>.CurrentValidatedValue))
            {
                UpdateYSeriesVisibility();
            }
        }

        private void UpdateYSeriesVisibility()
        {
            if (ySeries == null) return;
            
            var hasYCurve = Math.Abs(YXRatio.CurrentValidatedValue - 1.0) > ToleranceThreshold;
            var ySeriesExists = Series.Contains(ySeries);
            
            if (hasYCurve && !ySeriesExists)
            {
                Series.Add(ySeries);
            }
            else if (!hasYCurve && ySeriesExists)
            {
                Series.Remove(ySeries);
            }
            
            // Update Y speed dot visibility based on curve separation
            if (currentYSpeedDotSeries != null && IsRealTimeTrackingEnabled)
            {
                currentYSpeedDotSeries.IsVisible = hasYCurve;
            }
        }

        // ================================================================================================
        // CHART AXES CREATION
        // ================================================================================================

        private Axis[] CreateXAxes(double? minLimit = null, double? maxLimit = null)
        {
            var axisName = localizationService?.GetText("ChartAxisMouseSpeed") ?? "Mouse Speed";
            var titleColor = themeService.GetCachedColor(AxisTitleBrush);
            var labelColor = themeService.GetCachedColor(AxisLabelsBrush);
            var separatorColor = themeService.GetCachedColor(AxisSeparatorsBrush);

            return new Axis[]
            {
                new Axis()
                {
                    Name = axisName,
                    NameTextSize = AxisNameTextSize,
                    NamePaint = new SolidColorPaint(titleColor),
                    LabelsPaint = new SolidColorPaint(labelColor),
                    TextSize = AxisTextSize,
                    SeparatorsPaint = new SolidColorPaint(separatorColor) { StrokeThickness = StandardStrokeThickness },
                    TicksPaint = new SolidColorPaint(titleColor) { StrokeThickness = StandardStrokeThickness },
                    SubseparatorsPaint = new SolidColorPaint(separatorColor.WithAlpha(SubSeparatorAlpha)) { StrokeThickness = SubStrokeThickness },
                    AnimationsSpeed = TimeSpan.FromMilliseconds(100),
                EasingFunction = LiveChartsCore.EasingFunctions.EaseOut,
                    MinLimit = minLimit ?? 0,
                    MaxLimit = maxLimit
                }
            };
        }

        private Axis[] CreateYAxes(double? minLimit = null, double? maxLimit = null)
        {
            var axisName = localizationService?.GetText("ChartAxisOutput") ?? "Output";
            var titleColor = themeService.GetCachedColor(AxisTitleBrush);
            var labelColor = themeService.GetCachedColor(AxisLabelsBrush);
            var separatorColor = themeService.GetCachedColor(AxisSeparatorsBrush);

            return new Axis[]
            {
                new Axis()
                {
                    Name = axisName,
                    NameTextSize = AxisNameTextSize,
                    NamePaint = new SolidColorPaint(titleColor),
                    LabelsPaint = new SolidColorPaint(labelColor),
                    TextSize = AxisTextSize,
                    SeparatorsPaint = new SolidColorPaint(separatorColor) { StrokeThickness = StandardStrokeThickness },
                    TicksPaint = new SolidColorPaint(titleColor) { StrokeThickness = StandardStrokeThickness },
                    SubseparatorsPaint = new SolidColorPaint(separatorColor.WithAlpha(SubSeparatorAlpha)) { StrokeThickness = SubStrokeThickness },
                    AnimationsSpeed = TimeSpan.FromMilliseconds(100),
                EasingFunction = LiveChartsCore.EasingFunctions.EaseOut,
                    MinLimit = minLimit ?? 0,
                    MaxLimit = maxLimit
                }
            };
        }


        // ================================================================================================
        // AXIS LIMITS MANAGEMENT
        // ================================================================================================

        private void SetDefaultLimits()
        {
            if (preventAxisResizing)
            {
                // Don't allow axis limits to shrink below stored maximums
                XAxes[0].MinLimit = 0;
                XAxes[0].MaxLimit = Math.Max(maxXAxisLimit, DefaultMaxX);
                YAxes[0].MinLimit = 0;
                YAxes[0].MaxLimit = Math.Max(maxYAxisLimit, DefaultMaxY);
                
                // Update stored maximums if they increased
                maxXAxisLimit = XAxes[0].MaxLimit ?? maxXAxisLimit;
                maxYAxisLimit = YAxes[0].MaxLimit ?? maxYAxisLimit;
            }
            else
            {
                XAxes[0].MinLimit = 0;
                XAxes[0].MaxLimit = DefaultMaxX;
                YAxes[0].MinLimit = 0;
                YAxes[0].MaxLimit = DefaultMaxY;
            }
        }

        private static (double minX, double maxX, double minY, double maxY) CalculateDataBounds(System.Collections.Generic.List<CurvePoint> points)
        {
            var minX = points.Min(p => p.MouseSpeed);
            var maxX = points.Max(p => p.MouseSpeed);
            var minY = points.Min(p => p.Output);
            var maxY = points.Max(p => p.Output);
            return (minX, maxX, minY, maxY);
        }

        private void SetCenteredLimits(double minX, double maxX, double minY, double maxY)
        {
            var centerY = (minY + maxY) / 2;
            var centerX = (minX + maxX) / 2;
            
            if (preventAxisResizing)
            {
                // Don't allow axis limits to shrink below stored maximums
                YAxes[0].MinLimit = Math.Max(0, centerY - DefaultYRange);
                YAxes[0].MaxLimit = Math.Max(maxYAxisLimit, centerY + DefaultYRange);
                XAxes[0].MinLimit = Math.Max(0, centerX - DefaultAxisRange);
                XAxes[0].MaxLimit = Math.Max(maxXAxisLimit, centerX + DefaultAxisRange);
                
                // Update stored maximums if they increased
                maxXAxisLimit = XAxes[0].MaxLimit ?? maxXAxisLimit;
                maxYAxisLimit = YAxes[0].MaxLimit ?? maxYAxisLimit;
            }
            else
            {
                YAxes[0].MinLimit = Math.Max(0, centerY - DefaultYRange);
                YAxes[0].MaxLimit = centerY + DefaultYRange;
                XAxes[0].MinLimit = Math.Max(0, centerX - DefaultAxisRange);
                XAxes[0].MaxLimit = centerX + DefaultAxisRange;
            }
        }

        private void SetPaddedLimits(double minX, double maxX, double minY, double maxY)
        {
            var xRange = maxX - minX;
            var yRange = maxY - minY;
            var xPadding = xRange * DataPaddingRatio;
            var yPadding = yRange * DataPaddingRatio;
            
            if (preventAxisResizing)
            {
                // Don't allow axis limits to shrink below stored maximums
                XAxes[0].MinLimit = Math.Max(0, minX - xPadding);
                XAxes[0].MaxLimit = Math.Max(maxXAxisLimit, maxX + xPadding);
                YAxes[0].MinLimit = Math.Max(0, minY - yPadding);
                YAxes[0].MaxLimit = Math.Max(maxYAxisLimit, maxY + yPadding);
                
                // Update stored maximums if they increased
                maxXAxisLimit = XAxes[0].MaxLimit ?? maxXAxisLimit;
                maxYAxisLimit = YAxes[0].MaxLimit ?? maxYAxisLimit;
                currentMaxXData = Math.Max(currentMaxXData, maxX);
                currentMaxYData = Math.Max(currentMaxYData, maxY);
            }
            else
            {
                XAxes[0].MinLimit = Math.Max(0, minX - xPadding);
                XAxes[0].MaxLimit = maxX + xPadding;
                YAxes[0].MinLimit = Math.Max(0, minY - yPadding);
                YAxes[0].MaxLimit = maxY + yPadding;
            }
        }

        private void UpdateAxisLimitsIfNeeded()
        {
            if (!preventAxisResizing || XAxes == null || YAxes == null) return;
            
            // When preventAxisResizing is true, don't update axis limits at all
            // This prevents both shrinking and growing during real-time tracking
            return;
        }

        private void OnThemeChanged(object? sender, EventArgs e)
        {
            TooltipTextPaint.Color = themeService.GetCachedColor(AxisTitleBrush);
            TooltipBackgroundPaint.Color = themeService.GetCachedColor(TooltipBackgroundBrush).WithAlpha(TooltipBackgroundAlpha);

            var accentColor = themeService.GetCachedColor("SecondaryAccentBrush");
            if (currentSpeedDotSeries != null)
            {
                if (currentSpeedDotSeries.Stroke is SolidColorPaint strokePaint)
                {
                    strokePaint.Color = accentColor;
                }
                if (currentSpeedDotSeries.Fill is SolidColorPaint fillPaint)
                {
                    fillPaint.Color = accentColor;
                }
            }
            if (currentYSpeedDotSeries != null)
            {
                if (currentYSpeedDotSeries.Stroke is SolidColorPaint yStrokePaint)
                {
                    yStrokePaint.Color = accentColor;
                }
                if (currentYSpeedDotSeries.Fill is SolidColorPaint yFillPaint)
                {
                    yFillPaint.Color = accentColor;
                }
            }

            var currentXMin = XAxes?[0]?.MinLimit;
            var currentXMax = preventAxisResizing ? maxXAxisLimit : XAxes?[0]?.MaxLimit;
            var currentYMin = YAxes?[0]?.MinLimit;
            var currentYMax = preventAxisResizing ? maxYAxisLimit : YAxes?[0]?.MaxLimit;

            RecreateAxes(currentXMin, currentXMax, currentYMin, currentYMax);

            OnPropertyChanged(nameof(TooltipTextPaint));
            OnPropertyChanged(nameof(TooltipBackgroundPaint));
        }

        private void OnLocalizationChanged(object? sender, PropertyChangedEventArgs e)
        {
            var currentXMin = XAxes?[0]?.MinLimit;
            var currentXMax = preventAxisResizing ? maxXAxisLimit : XAxes?[0]?.MaxLimit;
            var currentYMin = YAxes?[0]?.MinLimit;
            var currentYMax = preventAxisResizing ? maxYAxisLimit : YAxes?[0]?.MaxLimit;

            RecreateAxes(currentXMin, currentXMax, currentYMin, currentYMax);
        }
        
        private void InitializeCurrentSpeedDotSeries()
        {
            var accentColor = themeService.GetCachedColor("SecondaryAccentBrush");
            
            if (currentSpeedDotSeries == null)
            {
                currentSpeedDotSeries = new ScatterSeries<CurvePoint>
                {
                    Values = currentSpeedData,
                    GeometrySize = 8,
                    Stroke = new SolidColorPaint(accentColor) { StrokeThickness = 2 },
                    Fill = new SolidColorPaint(accentColor),
                    Mapping = (curvePoint, index) => new LiveChartsCore.Kernel.Coordinate(x: curvePoint.MouseSpeed, y: curvePoint.Output),
                    Name = "Current X Speed",
                    IsVisible = false,
                    DataPadding = new LiveChartsCore.Drawing.LvcPoint(0, 0)
                };
            }
            
            if (currentYSpeedDotSeries == null)
            {
                currentYSpeedDotSeries = new ScatterSeries<CurvePoint>
                {
                    Values = currentYSpeedData,
                    GeometrySize = 8,
                    Stroke = new SolidColorPaint(accentColor) { StrokeThickness = 2 },
                    Fill = new SolidColorPaint(accentColor),
                    Mapping = (curvePoint, index) => new LiveChartsCore.Kernel.Coordinate(x: curvePoint.MouseSpeed, y: curvePoint.Output),
                    Name = "Current Y Speed",
                    IsVisible = false,
                    DataPadding = new LiveChartsCore.Drawing.LvcPoint(0, 0)
                };
            }
            
            // Always ensure they're in the series collection after a clear
            if (!Series.Contains(currentSpeedDotSeries))
            {
                Series.Add(currentSpeedDotSeries);
            }
            if (!Series.Contains(currentYSpeedDotSeries))
            {
                Series.Add(currentYSpeedDotSeries);
            }
        }
        
        private void ToggleRealTimeTracking()
        {
            if (IsRealTimeTrackingEnabled)
            {
                StopRealTimeTracking();
            }
            else
            {
                StartRealTimeTracking();
            }
        }
        
        private void StartRealTimeTracking()
        {
            if (IsRealTimeTrackingEnabled) return;
            
            IsRealTimeTrackingEnabled = true;
            
            currentSpeedData.Clear();
            currentYSpeedData.Clear();
            
            var hasYCurve = Math.Abs(YXRatio.CurrentValidatedValue - 1.0) > ToleranceThreshold;
            
            // Set BackEnd reference and device service for centralized device handling
            mouseTracker.SetBackEnd(backEnd);
            
            // Pass device service if available
            if (deviceInfoProvider != null)
            {
                mouseTracker.SetDeviceInfoProvider(deviceInfoProvider);
            }
            
            var activeDeviceModel = GetActiveDeviceModel();
            if (activeDeviceModel != null)
            {
                mouseTracker.SetDeviceDPI(activeDeviceModel.DPI.CurrentValidatedValue);
            }
            
            if (currentSpeedDotSeries != null)
            {
                currentSpeedDotSeries.IsVisible = true;
            }
            if (currentYSpeedDotSeries != null)
            {
                currentYSpeedDotSeries.IsVisible = hasYCurve;
            }
            
            mouseTracker.MouseMoved += OnMouseMoved;
            mouseTracker.MouseIdle += OnMouseIdle;
            mouseTracker.StartTracking();
            
            Debug.WriteLine("\n=== Real-Time Tracking Started ===\nChart will now display current mouse device and track movement.");
            
            OnPropertyChanged(nameof(IsRealTimeTrackingEnabled));
        }

        private BE.DeviceModel? GetActiveDeviceModel()
        {
            // For now, get the first device or return null to use default DPI
            // TODO: Implement proper active device detection based on current mapping
            return backEnd.Devices.Devices.FirstOrDefault();
        }
        
        private void StopRealTimeTracking()
        {
            if (!IsRealTimeTrackingEnabled) return;
            
            IsRealTimeTrackingEnabled = false;
            
            mouseTracker.MouseMoved -= OnMouseMoved;
            mouseTracker.MouseIdle -= OnMouseIdle;
            mouseTracker.StopTracking();
            
            currentSpeedData.Clear();
            currentYSpeedData.Clear();
            
            CurrentMouseDevice = "No device detected";
            CurrentDeviceDPI = "Unknown DPI";
            OnPropertyChanged(nameof(CurrentMouseDevice));
            OnPropertyChanged(nameof(CurrentDeviceDPI));
            
            Debug.WriteLine("\n=== Real-Time Tracking Stopped ===\nChart no longer tracking mouse devices.");
            
            if (currentSpeedDotSeries != null)
            {
                currentSpeedDotSeries.IsVisible = false;
            }
            if (currentYSpeedDotSeries != null)
            {
                currentYSpeedDotSeries.IsVisible = false;
            }
            
            OnPropertyChanged(nameof(IsRealTimeTrackingEnabled));
        }
        
        private void OnMouseMoved(object? sender, MouseMovementEventArgs e)
        {
            if (!IsRealTimeTrackingEnabled) return;
            
            try
            {
                // Update current device info using BackEnd cross-reference
                var (deviceName, sourceDPI, isKnownDevice) = backEnd.GetCurrentDeviceInfo();
                
                string displayName = isKnownDevice ? deviceName : e.DeviceName;
                string dpiInfo = $"{sourceDPI} DPI";
                
                if (CurrentMouseDevice != displayName || CurrentDeviceDPI != dpiInfo)
                {
                    Debug.WriteLine($"\n=== UI Device Change ===\nDevice: {displayName}\nDPI: {sourceDPI}\nKnown Device: {isKnownDevice}\nHandle: {e.DeviceHandle.ToInt64():X}");
                    
                    CurrentMouseDevice = displayName;
                    CurrentDeviceDPI = dpiInfo;
                    OnPropertyChanged(nameof(CurrentMouseDevice));
                    OnPropertyChanged(nameof(CurrentDeviceDPI));
                }
                
                var hasYCurve = Math.Abs(YXRatio.CurrentValidatedValue - 1.0) > ToleranceThreshold;
                
                if (hasYCurve)
                {
                    var xOutputValue = InterpolateOutputFromSpeed(e.XSpeed, XCurvePreview);
                    var yOutputValue = InterpolateOutputFromSpeed(e.YSpeed, YCurvePreview);
                    
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        UpdateCurrentSpeedDots(e.XSpeed, xOutputValue, e.YSpeed, yOutputValue, hasYCurve);
                    });
                }
                else
                {
                    var outputValue = InterpolateOutputFromSpeed(e.MouseSpeed, XCurvePreview);
                    
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        UpdateCurrentSpeedDots(e.MouseSpeed, outputValue, 0, null, hasYCurve);
                    });
                }
            }
            catch (Exception ex)
            {
            }
        }
        
        private double? InterpolateOutputFromSpeed(double mouseSpeed, ICurvePreview curvePreview)
        {
            if (curvePreview?.Points == null || curvePreview.Points.Count == 0)
                return null;
                
            var points = curvePreview.Points.ToList();
            
            // Find the closest points for interpolation
            var lowerPoint = points.LastOrDefault(p => p.MouseSpeed <= mouseSpeed);
            var upperPoint = points.FirstOrDefault(p => p.MouseSpeed >= mouseSpeed);
            
            if (lowerPoint == null && upperPoint == null)
                return null;
                
            if (lowerPoint == null)
                return upperPoint!.Output;
                
            if (upperPoint == null)
                return lowerPoint.Output;
                
            if (Math.Abs(lowerPoint.MouseSpeed - upperPoint.MouseSpeed) < 0.001)
                return lowerPoint.Output;
                
            // Linear interpolation
            double ratio = (mouseSpeed - lowerPoint.MouseSpeed) / (upperPoint.MouseSpeed - lowerPoint.MouseSpeed);
            return lowerPoint.Output + ratio * (upperPoint.Output - lowerPoint.Output);
        }

        private void UpdateCurrentSpeedDots(double xSpeed, double? xOutputValue, double ySpeed, double? yOutputValue, bool hasYCurve)
        {
            if (!IsRealTimeTrackingEnabled) return;
            
            // Update X speed dot position (keep it persistent, just update position)
            if (xOutputValue.HasValue && xSpeed > 0)
            {
                if (currentSpeedData.Count == 0)
                {
                    currentSpeedData.Add(new CurvePoint { MouseSpeed = xSpeed, Output = xOutputValue.Value });
                }
                else
                {
                    currentSpeedData[0].MouseSpeed = xSpeed;
                    currentSpeedData[0].Output = xOutputValue.Value;
                }
            }
            
            // Update Y speed dot position (only when separate curves)
            if (hasYCurve && yOutputValue.HasValue && ySpeed > 0)
            {
                if (currentYSpeedData.Count == 0)
                {
                    currentYSpeedData.Add(new CurvePoint { MouseSpeed = ySpeed, Output = yOutputValue.Value });
                }
                else
                {
                    currentYSpeedData[0].MouseSpeed = ySpeed;
                    currentYSpeedData[0].Output = yOutputValue.Value;
                }
            }
            
            // Update dot visibility based on curve separation
            if (currentSpeedDotSeries != null)
            {
                currentSpeedDotSeries.IsVisible = IsRealTimeTrackingEnabled;
            }
            if (currentYSpeedDotSeries != null)
            {
                currentYSpeedDotSeries.IsVisible = IsRealTimeTrackingEnabled && hasYCurve;
            }
            
            // Track maximum data values for axis expansion
            if (xSpeed > currentMaxXData || ySpeed > currentMaxXData)
            {
                currentMaxXData = Math.Max(xSpeed, ySpeed);
            }
            
            if (xOutputValue.HasValue && xOutputValue.Value > currentMaxYData)
            {
                currentMaxYData = xOutputValue.Value;
            }
            
            if (yOutputValue.HasValue && yOutputValue.Value > currentMaxYData)
            {
                currentMaxYData = yOutputValue.Value;
            }
        }
        
        private void OnMouseIdle(object? sender, EventArgs e)
        {
            if (!IsRealTimeTrackingEnabled) return;
            
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                currentSpeedData.Clear();
                currentYSpeedData.Clear();
            });
        }
    }
}