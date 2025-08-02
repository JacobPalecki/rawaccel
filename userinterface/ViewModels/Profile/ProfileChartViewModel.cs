using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.Immutable;
using LiveChartsCore;
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
using userspace_backend.Model.EditableSettings;
using userspace_backend.Services;
using BE = userspace_backend.Model;

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
        private const int DefaultMaxX = 100;
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
        private readonly IMouseTrackingService mouseTrackingService;
        private BE.ProfileModel currentProfileModel = null!;
        
        private SolidColorPaint? cachedXStroke;
        private SolidColorPaint? cachedYStroke;
        
        private LineSeries<CurvePoint>? xSeries;
        private LineSeries<CurvePoint>? ySeries;
        private ScatterSeries<CurvePoint>? currentSpeedDotSeries;
        private ScatterSeries<CurvePoint>? currentYSpeedDotSeries;
        
        private readonly object syncObject = new object();

        public ProfileChartViewModel(IThemeService themeService, LocalizationService localizationService, PreviewChartRenderer previewRenderer, IMouseTrackingService mouseTrackingService)
        {
            this.themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
            this.localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
            this.previewRenderer = previewRenderer ?? throw new ArgumentNullException(nameof(previewRenderer));
            this.mouseTrackingService = mouseTrackingService ?? throw new ArgumentNullException(nameof(mouseTrackingService));

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
        
        private readonly ObservableCollection<CurvePoint> currentSpeedData = new ObservableCollection<CurvePoint>();
        private readonly ObservableCollection<CurvePoint> currentYSpeedData = new ObservableCollection<CurvePoint>();

        private double maxXAxisLimit = 0;
        private double maxYAxisLimit = 0;
        private bool preventAxisShrinking = true;
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
            XAxes[0].MinLimit = 0;
            XAxes[0].MaxLimit = DefaultMaxX;
            YAxes[0].MinLimit = 0;
            YAxes[0].MaxLimit = DefaultMaxY;
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
            YAxes[0].MinLimit = Math.Max(0, centerY - DefaultYRange);
            YAxes[0].MaxLimit = centerY + DefaultYRange;
            XAxes[0].MinLimit = Math.Max(0, centerX - DefaultAxisRange);
            XAxes[0].MaxLimit = centerX + DefaultAxisRange;
        }

        private void SetPaddedLimits(double minX, double maxX, double minY, double maxY)
        {
            var xRange = maxX - minX;
            var yRange = maxY - minY;
            var xPadding = xRange * DataPaddingRatio;
            var yPadding = yRange * DataPaddingRatio;
            XAxes[0].MinLimit = Math.Max(0, minX - xPadding);
            XAxes[0].MaxLimit = maxX + xPadding;
            YAxes[0].MinLimit = Math.Max(0, minY - yPadding);
            YAxes[0].MaxLimit = maxY + yPadding;
            
            if (preventAxisShrinking)
            {
                maxXAxisLimit = XAxes[0].MaxLimit ?? maxXAxisLimit;
                maxYAxisLimit = YAxes[0].MaxLimit ?? maxYAxisLimit;
                currentMaxXData = maxX;
                currentMaxYData = maxY;
            }
        }

        private void UpdateAxisLimitsIfNeeded()
        {
            if (!preventAxisShrinking || XAxes == null || YAxes == null) return;
            
            var xAxis = XAxes[0];
            var yAxis = YAxes[0];
            
            // Calculate padded limits based on data
            var xPadding = currentMaxXData * 0.1;
            var yPadding = currentMaxYData * 0.1;
            var newXMax = currentMaxXData + xPadding;
            var newYMax = currentMaxYData + yPadding;
            
            if (newXMax > maxXAxisLimit)
            {
                maxXAxisLimit = newXMax;
                xAxis.MaxLimit = maxXAxisLimit;
            }
            
            if (newYMax > maxYAxisLimit)
            {
                maxYAxisLimit = newYMax;
                yAxis.MaxLimit = maxYAxisLimit;
            }
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
            var currentXMax = preventAxisShrinking ? maxXAxisLimit : XAxes?[0]?.MaxLimit;
            var currentYMin = YAxes?[0]?.MinLimit;
            var currentYMax = preventAxisShrinking ? maxYAxisLimit : YAxes?[0]?.MaxLimit;

            RecreateAxes(currentXMin, currentXMax, currentYMin, currentYMax);

            OnPropertyChanged(nameof(TooltipTextPaint));
            OnPropertyChanged(nameof(TooltipBackgroundPaint));
        }

        private void OnLocalizationChanged(object? sender, PropertyChangedEventArgs e)
        {
            var currentXMin = XAxes?[0]?.MinLimit;
            var currentXMax = preventAxisShrinking ? maxXAxisLimit : XAxes?[0]?.MaxLimit;
            var currentYMin = YAxes?[0]?.MinLimit;
            var currentYMax = preventAxisShrinking ? maxYAxisLimit : YAxes?[0]?.MaxLimit;

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
                    IsVisible = false
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
                    IsVisible = false
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
            
            if (currentSpeedDotSeries != null)
            {
                currentSpeedDotSeries.IsVisible = true;
            }
            if (currentYSpeedDotSeries != null)
            {
                currentYSpeedDotSeries.IsVisible = hasYCurve;
            }
            
            mouseTrackingService.MouseMoved += OnMouseMoved;
            mouseTrackingService.MouseIdle += OnMouseIdle;
            mouseTrackingService.StartTracking();
            
            
            OnPropertyChanged(nameof(IsRealTimeTrackingEnabled));
        }
        
        private void StopRealTimeTracking()
        {
            if (!IsRealTimeTrackingEnabled) return;
            
            IsRealTimeTrackingEnabled = false;
            
            mouseTrackingService.MouseMoved -= OnMouseMoved;
            mouseTrackingService.MouseIdle -= OnMouseIdle;
            mouseTrackingService.StopTracking();
            
            currentSpeedData.Clear();
            currentYSpeedData.Clear();
            
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
                var hasYCurve = Math.Abs(YXRatio.CurrentValidatedValue - 1.0) > ToleranceThreshold;
                
                if (hasYCurve)
                {
                    // Calculate separate X and Y speeds
                    var xSpeed = Math.Abs(e.X) / 16.0 * 1000.0; // Convert to per second
                    var ySpeed = Math.Abs(e.Y) / 16.0 * 1000.0; // Convert to per second
                    
                    var xOutputValue = InterpolateOutputFromSpeed(xSpeed, XCurvePreview);
                    var yOutputValue = InterpolateOutputFromSpeed(ySpeed, YCurvePreview);
                    
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        UpdateCurrentSpeedDots(xSpeed, xOutputValue, ySpeed, yOutputValue, hasYCurve);
                    });
                }
                else
                {
                    // Combined mode - use combined speed
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
                UpdateAxisLimitsIfNeeded();
            }
            
            if (xOutputValue.HasValue && xOutputValue.Value > currentMaxYData)
            {
                currentMaxYData = xOutputValue.Value;
                UpdateAxisLimitsIfNeeded();
            }
            
            if (yOutputValue.HasValue && yOutputValue.Value > currentMaxYData)
            {
                currentMaxYData = yOutputValue.Value;
                UpdateAxisLimitsIfNeeded();
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