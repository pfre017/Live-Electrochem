using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using System.IO;
using System.Diagnostics;
using System.Collections.ObjectModel;
using OxyPlot;
using System.Threading.Tasks;
using Helper.Common;
using Helper.Extensions;
using Helper.Analysis;
using MathNet.Numerics.Statistics;
using System.Xml.Serialization;
using System.Xml;
using System.ComponentModel;
using Xceed.Wpf.AvalonDock.Layout;
using Xceed.Wpf.AvalonDock.Layout.Serialization;
using System.Net.Mail;
using System.Net;
using Plotting;
using OxyPlot.Series;
using OxyPlot.Axes;
using OxyPlot.Wpf;
using System.Windows.Media;
using PixelLab.Common;
using System.Reflection;

namespace Live_Electrochem
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

#if DEBUG
            SetValue(SourceFolderProperty, @"C:\Users\pfre017\Desktop\#17-3 20th June Calib\");
            //SetValue(SourceFolderProperty, @"P:\Software Development\Live_Electrochem\Live_Electrochem\test folder\");
            //SetValue(SourceFolderProperty, @"C: \Users\pfre017\Desktop\Live_Electrochem\Live_Electrochem\test folder\");
#else
            SetValue(SourceFolderProperty, @"U:\LipskiLab\Shared\");
#endif

            SetValue(LogsProperty, new ObservableCollection<string>());
            SetValue(FilesProperty, new ObservableCollection<LVBinFile>());
            SetValue(CalibrationStepsProperty, new ObservableCollection<Event>());

            SetValue(AllIntegratedModelProperty, new IntegratedViewModel()
            {
                Title = "Temporal Charge",
                Model = new PlotModel()
                {
                    Title = "Temporal Charge",
                },
            });

            SetValue(ScopeModelProperty, new ScopeViewModel()
            {
                Title = "Voltamogram",
                Model = new PlotModel()
                {
                    Title = "Voltamogram",
                },
            });

            SetValue(IntegratedModelProperty, new IntegratedViewModel()
            {
                Title = "Charge",
                Model = new PlotModel()
                {
                    Title = "Charge",
                },
            });



            //SetValue(ScopeModelProperty, new ScopeViewModel() { AutoScaleX = true, AutoScaleY = true });

            string title = "Live Electrochemistry - Pete Freestone 2016 (single+multi-channel version; 25th August 2019)";
            this.Title = title;
            AddLog(title);

            //CalculateGainMultiplier(this.nA_V, this.AD_Gain);

            LoadSettings();

            
        }

        private System.IO.FileSystemWatcher watcher;

        #region Dependency Properties

        public ObservableCollection<Event> CalibrationSteps
        {
            get { return (ObservableCollection<Event>)GetValue(CalibrationStepsProperty); }
            set { SetValue(CalibrationStepsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CalibrationSteps.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CalibrationStepsProperty =
            DependencyProperty.Register("CalibrationSteps", typeof(ObservableCollection<Event>), typeof(MainWindow), new PropertyMetadata(null));

        public int CalibrationVoltamogramPeakIndex
        {
            get { return (int)GetValue(CalibrationVoltamogramPeakIndexProperty); }
            set { SetValue(CalibrationVoltamogramPeakIndexProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CalibrationVoltamogramPeakIndex.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CalibrationVoltamogramPeakIndexProperty =
            DependencyProperty.Register("CalibrationVoltamogramPeakIndex", typeof(int), typeof(MainWindow), new PropertyMetadata(617));

        public int CalibrationRMSWindowCentre
        {
            get { return (int)GetValue(CalibrationRMSWindowCentreProperty); }
            set { SetValue(CalibrationRMSWindowCentreProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CalibrationRMSWindowCentre.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CalibrationRMSWindowCentreProperty =
            DependencyProperty.Register("CalibrationRMSWindowCentre", typeof(int), typeof(MainWindow), new PropertyMetadata(1000));

        public int CalibrationRMSWindowWidth
        {
            get { return (int)GetValue(CalibrationRMSWindowWidthProperty); }
            set { SetValue(CalibrationRMSWindowWidthProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CalibrationRMSWindowWidth.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CalibrationRMSWindowWidthProperty =
            DependencyProperty.Register("CalibrationRMSWindowWidth", typeof(int), typeof(MainWindow), new PropertyMetadata(100));

        public string CalibrationSource
        {
            get { return (string)GetValue(CalibrationSourceProperty); }
            set { SetValue(CalibrationSourceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CalibrationSource.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CalibrationSourceProperty =
            DependencyProperty.Register("CalibrationSource", typeof(string), typeof(MainWindow), new PropertyMetadata(null));

        public AIChannel SelectedAIChannel
        {
            get { return (AIChannel)GetValue(SelectedAIChannelProperty); }
            set { SetValue(SelectedAIChannelProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedAIChannel.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedAIChannelProperty =
            DependencyProperty.Register("SelectedAIChannel", typeof(AIChannel), typeof(MainWindow), new PropertyMetadata(null));

        public AOChannel SelectedAOChannel
        {
            get { return (AOChannel)GetValue(SelectedAOChannelProperty); }
            set { SetValue(SelectedAOChannelProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedAOChannel.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedAOChannelProperty =
            DependencyProperty.Register("SelectedAOChannel", typeof(AOChannel), typeof(MainWindow), new PropertyMetadata(null));

        public bool PurgeLayoutOnExit
        {
            get { return (bool)GetValue(PurgeLayoutOnExitProperty); }
            set { SetValue(PurgeLayoutOnExitProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PurgeLayoutOnExit.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PurgeLayoutOnExitProperty =
            DependencyProperty.Register("PurgeLayoutOnExit", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

        public bool IsFilterEnabled
        {
            get { return (bool)GetValue(IsFilterEnabledProperty); }
            set { SetValue(IsFilterEnabledProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsFilterEnabled.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsFilterEnabledProperty =
            DependencyProperty.Register("IsFilterEnabled", typeof(bool), typeof(MainWindow), new PropertyMetadata(true, OnIsFilterEnabledChanged));

        private static void OnIsFilterEnabledChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            MainWindow w = (MainWindow)o;
            w.Reset();
        }

        public double FilterCutOffFrequency
        {
            get { return (double)GetValue(FilterCutOffFrequencyProperty); }
            set { SetValue(FilterCutOffFrequencyProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FilterCutOffFrequency.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FilterCutOffFrequencyProperty =
            DependencyProperty.Register("FilterCutOffFrequency", typeof(double), typeof(MainWindow), new PropertyMetadata(1660d, OnFilterCutOffFrequencyChanged));

        private static void OnFilterCutOffFrequencyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            MainWindow w = (MainWindow)o;
            w.Reset();              //not sure if this truely does reset the files and cause the new CutOff Frequency to be used
        }

        public bool IsExtensionFilterEnabled
        {
            get { return (bool)GetValue(IsExtensionFilterEnabledProperty); }
            set { SetValue(IsExtensionFilterEnabledProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsExtensionFilterEnabled.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsExtensionFilterEnabledProperty =
            DependencyProperty.Register("IsExtensionFilterEnabled", typeof(bool), typeof(MainWindow), new PropertyMetadata(true));

        public string ExtensionFilterString
        {
            get { return (string)GetValue(ExtensionFilterStringProperty); }
            set { SetValue(ExtensionFilterStringProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ExtensionFilterString.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ExtensionFilterStringProperty =
            DependencyProperty.Register("ExtensionFilterString", typeof(string), typeof(MainWindow), new PropertyMetadata("bin"));

        public decimal SelectedCharge
        {
            get { return (decimal)GetValue(SelectedChargeProperty); }
            set { SetValue(SelectedChargeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedChargeProperty =
            DependencyProperty.Register("SelectedCharge", typeof(decimal), typeof(MainWindow), new PropertyMetadata(0m));

        public DateTime ExperimentStart
        {
            get { return (DateTime)GetValue(ExperimentStartProperty); }
            set { SetValue(ExperimentStartProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ExperimentStartProperty =
            DependencyProperty.Register("ExperimentStart", typeof(DateTime), typeof(MainWindow), new PropertyMetadata(null));

        public bool IsExperimentStarted
        {
            get { return (bool)GetValue(IsExperimentStartedProperty); }
            set { SetValue(IsExperimentStartedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsExperimentStartedProperty =
            DependencyProperty.Register("IsExperimentStarted", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

        public decimal Offset
        {
            get { return (decimal)GetValue(OffsetProperty); }
            set { SetValue(OffsetProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OffsetProperty =
            DependencyProperty.Register("Offset", typeof(decimal), typeof(MainWindow), new PropertyMetadata(0m));

        public int BackgroundSubtractionSweepCount
        {
            get { return (int)GetValue(BackgroundSubtractionSweepCountProperty); }
            set { SetValue(BackgroundSubtractionSweepCountProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BackgroundSubtractionSweepCountProperty =
            DependencyProperty.Register("BackgroundSubtractionSweepCount", typeof(int), typeof(MainWindow), new PropertyMetadata(1));

        public double COV
        {
            get { return (double)GetValue(COVProperty); }
            set { SetValue(COVProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty COVProperty =
            DependencyProperty.Register("COV", typeof(double), typeof(MainWindow), new PropertyMetadata(0d));

        public int COV_window
        {
            get { return (int)GetValue(COV_windowProperty); }
            set { SetValue(COV_windowProperty, value); }
        }

        // Using a DependencyProperty as the backing store for COV_window.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty COV_windowProperty =
            DependencyProperty.Register("COV_window", typeof(int), typeof(MainWindow), new PropertyMetadata(3));


        public ObservableCollection<LVBinFile> Files
        {
            get { return (ObservableCollection<LVBinFile>)GetValue(FilesProperty); }
            set { SetValue(FilesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FilesProperty =
            DependencyProperty.Register("Files", typeof(ObservableCollection<LVBinFile>), typeof(MainWindow), new PropertyMetadata(null));

        public LVBinFile SelectedFile
        {
            get { return (LVBinFile)GetValue(SelectedFileProperty); }
            set { SetValue(SelectedFileProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedFileProperty =
            DependencyProperty.Register("SelectedFile", typeof(LVBinFile), typeof(MainWindow), new PropertyMetadata(null, SelectedFileChanged));

        private static void SelectedFileChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            //I don't think is necessary as it happens when the file is processed
            MainWindow window = (MainWindow)o;
            int old = window.SelectedSweep;
            window.SelectedSweep = 0;
            window.SelectedSweep = old;

            LVBinFile file = (LVBinFile)e.NewValue;

            if (file == null)
                return;
            if (file.AIChannels == null)
                return;
            if (file.AIChannels.Count == 0)
                return;

            window.IntegratedModel.Clear();

            double[] x_data = new double[file.AIChannels.First().IntegratedData.Length];
            x_data.FillSeriesArray(1);

            foreach (AIChannel channel in file.AIChannels)
            {
                window.IntegratedModel.AddLineSeries(x_data, channel.IntegratedData.Select(a => (double)a));
                ((OxyPlot.Series.LineSeries)window.IntegratedModel.Model.Series.Last()).Color = channel.Stroke.ToOxyColor();
            }

            window.IntegratedModel.Model.InvalidatePlot(true);
            window.IntegratedModel.ScalePlotAxes(false);

            window.CalculateSD();
        }

        public int LowerSweepBound
        {
            get { return (int)GetValue(LowerSweepBoundProperty); }
            set { SetValue(LowerSweepBoundProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LowerSweepBoundProperty =
            DependencyProperty.Register("LowerSweepBound", typeof(int), typeof(MainWindow), new PropertyMetadata(1209));

        public int UpperSweepBound
        {
            get { return (int)GetValue(UpperSweepBoundProperty); }
            set { SetValue(UpperSweepBoundProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UpperSweepBoundProperty =
            DependencyProperty.Register("UpperSweepBound", typeof(int), typeof(MainWindow), new PropertyMetadata(1211));

        public ObservableCollection<string> Logs
        {
            get { return (ObservableCollection<string>)GetValue(LogsProperty); }
            set { SetValue(LogsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LogsProperty =
            DependencyProperty.Register("Logs", typeof(ObservableCollection<string>), typeof(MainWindow), new PropertyMetadata(null));

        public string SourceFolder
        {
            get { return (string)GetValue(SourceFolderProperty); }
            set { SetValue(SourceFolderProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SourceFolderProperty =
            DependencyProperty.Register("SourceFolder", typeof(string), typeof(MainWindow), new PropertyMetadata(string.Empty));

        public bool IsMonitoring
        {
            get { return (bool)GetValue(IsMonitoringProperty); }
            set { SetValue(IsMonitoringProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsMonitoringProperty =
            DependencyProperty.Register("IsMonitoring", typeof(bool), typeof(MainWindow), new PropertyMetadata(false, IsMonitoringChanged));

        public double LowerVoltageBound
        {
            get { return (double)GetValue(LowerVoltageBoundProperty); }
            set { SetValue(LowerVoltageBoundProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LowerVoltageBoundProperty =
            DependencyProperty.Register("LowerVoltageBound", typeof(double), typeof(MainWindow), new PropertyMetadata(0.4d));

        public double UpperVoltageBound
        {
            get { return (double)GetValue(UpperVoltageBoundProperty); }
            set { SetValue(UpperVoltageBoundProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UpperVoltageBoundProperty =
            DependencyProperty.Register("UpperVoltageBound", typeof(double), typeof(MainWindow), new PropertyMetadata(0.9d));

        public int FileCount
        {
            get { return (int)GetValue(FileCountProperty); }
            set { SetValue(FileCountProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FileCountProperty =
            DependencyProperty.Register("FileCount", typeof(int), typeof(MainWindow), new PropertyMetadata(0));

        public int WindowLowerBound
        {
            get { return (int)GetValue(WindowLowerBoundProperty); }
            set { SetValue(WindowLowerBoundProperty, value); }
        }

        // Using a DependencyProperty as the backing store for WindowLowerBound.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty WindowLowerBoundProperty =
            DependencyProperty.Register("WindowLowerBound", typeof(int), typeof(MainWindow), new PropertyMetadata(0, OnWindowLowerBoundChanged));

        private static void OnWindowLowerBoundChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            MainWindow mw = (MainWindow)o;
            mw.UpdateOutputPlot((int)e.NewValue, mw.WindowUpperBound);
        }

        public int WindowUpperBound
        {
            get { return (int)GetValue(WindowUpperBoundProperty); }
            set { SetValue(WindowUpperBoundProperty, value); }
        }

        // Using a DependencyProperty as the backing store for WindowUpperBound.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty WindowUpperBoundProperty =
            DependencyProperty.Register("WindowUpperBound", typeof(int), typeof(MainWindow), new PropertyMetadata(2, OnWindowUpperBoundChanged));

        private static void OnWindowUpperBoundChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            MainWindow mw = (MainWindow)o;
            mw.UpdateOutputPlot(mw.WindowLowerBound, (int)e.NewValue);
        }

        public AggregateFunctionEnum OutputAggregateFunction
        {
            get { return (AggregateFunctionEnum)GetValue(OutputAggregateFunctionProperty); }
            set { SetValue(OutputAggregateFunctionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for OutputAggregateFunction.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OutputAggregateFunctionProperty =
            DependencyProperty.Register("OutputAggregateFunction", typeof(AggregateFunctionEnum), typeof(MainWindow), new PropertyMetadata(AggregateFunctionEnum.Average, OnOutputAggregateFunctionChanged));

        private static void OnOutputAggregateFunctionChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            MainWindow mw = (MainWindow)o;
            mw.UpdateOutputPlot(mw.WindowLowerBound, mw.WindowUpperBound);
        }

        #endregion

        #region Commands + UI input

        private void OpenSourceFolder_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            //Open Folder dialog to choose the folder where to look for files.
            //Forms.FolderBrowserDialog d = new Forms.FolderBrowserDialog();
            //BrowseForFolderDialog d = new BrowseForFolderDialog();

            Ookii.Dialogs.Wpf.VistaFolderBrowserDialog d = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();

            //if (d.ShowDialog(this).GetValueOrDefault(false) == true)
            //if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            if (d.ShowDialog(this).GetValueOrDefault(false) == true)

            {
                this.SourceFolder = d.SelectedPath;
                SaveSettings();
            }
        }

        private void ClearLogs_Click(object sender, RoutedEventArgs e)
        {
            this.Logs.Clear();
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(SourceFolder);
            }
            catch
            {
                MessageBox.Show("Unable to open the folder");
            }
        }

        private async void Analyze_Click(object sender, RoutedEventArgs e)
        {
            await Analyze();
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            Reset();
        }

        private void COV_Settings_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void PurgeLayout_Click(object sender, RoutedEventArgs e)
        {
            this.PurgeLayoutOnExit = true;
            MessageBox.Show("Window Layout Data will be cleared on exit.\nPlease restart application");
        }

        private void FileProperties_Click(object sender, RoutedEventArgs e)
        {
            FilePropertiesWindow w = new FilePropertiesWindow();
            w.File = this.SelectedFile;
            w.Show();
        }

        private void Close_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            this.Close();
        }


        #endregion

        #region Folder Watching

        private static void IsMonitoringChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            MainWindow window = (MainWindow)o;
            window.AddLog("Live Monitoring {0}", (bool)e.NewValue ? "ENABLED" : "DISABLED");
            window.IsMonitoringChangedInternal(o, e);
        }

        private async void IsMonitoringChangedInternal(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (this.watcher != null)
            {
                this.watcher.Created -= Watcher_Created;
                this.watcher.Dispose();
                this.watcher = null;
            }

            if (string.IsNullOrEmpty(this.SourceFolder))
            {
                MessageBox.Show("SourceFolder not set");
                return;
            }

            if (timers != null)
                foreach (System.Timers.Timer t in timers.Values)
                {
                    t.Enabled = false;
                    t.Elapsed -= T_Elapsed;
                    t.Dispose();
                }
            timers.Clear();

            this.watcher = new FileSystemWatcher(this.SourceFolder);
            this.watcher.Created += Watcher_Created;
            this.watcher.EnableRaisingEvents = (bool)e.NewValue;

            if ((bool)e.NewValue == false)
                IsExperimentStarted = false;
            else
            {
                FileSystemInfo[] files = new DirectoryInfo(SourceFolder).GetFileSystemInfos();
                if (files != null && files.Count() > 0)
                {
                    FileSystemInfo fileinfo = files.OrderByDescending(F => F.CreationTime).First();
                    ExperimentStart = fileinfo.CreationTime;
                    IsExperimentStarted = true;
                    await Analyze();            //analyzes exisiting files in the Folder
                }
            }
        }

        private void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            Debug.Print("Watcher_Created::File Created {1} {0}", sender.ToString(), e.FullPath);

            Dispatcher.BeginInvoke((Action)(() =>
            {
                AddLog("\tFileWatcher::File Created '{0}'", e.FullPath);

                if (IsExperimentStarted == false)
                {
                    //ExperimentStart = DateTime.Now;
                    //find oldest file in the folder

                    try
                    {
                        FileSystemInfo fileinfo = new DirectoryInfo(System.IO.Path.GetDirectoryName(e.FullPath)).GetFileSystemInfos().OrderByDescending(F => F.CreationTime).First();
                        ExperimentStart = fileinfo.CreationTime;
                    }
                    catch
                    {
                        ExperimentStart = DateTime.Now;
                    }

                    IsExperimentStarted = true;
                }

                if (IsFileLocked(e.FullPath))
                {
                    AddLog("\tFileWatcher::File is locked. Will start Timer to monitor when it is available.");

                    ExtTimer t = new ExtTimer(2000);
                    t.Elapsed += T_Elapsed;
                    t.Tag = e.FullPath;
                    t.AutoReset = true;
                    t.Start();
                    timers.Add(e.FullPath, t);
                }
                else
                {
                    this.ProcessFile(e.FullPath, true);
                }
            }));
        }

        private void T_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Debug.Print("T_Elapsed sender={0}     e={1}", sender, e);

            ExtTimer t = (ExtTimer)sender;

            if (IsFileLocked((string)t.Tag) == false)
            {
                t.Stop();
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    this.ProcessFile((string)t.Tag, true);      //ProcessFile with UpdatePlots = true
                    timers.Remove((string)(t.Tag));
                }));
                t.Elapsed -= T_Elapsed;
                t.Dispose();
            }
        }

        private Dictionary<string, ExtTimer> timers = new Dictionary<string, ExtTimer>();

        #endregion

        #region THE WORK

        private async Task Analyze()
        {
            Reset();

            AddLog("Analyzing files in {0}", SourceFolder);
            AddLog("Filter: {0}, Cut-off Freqeuncy {1}Hz", IsFilterEnabled ? "ON" : "OFF", FilterCutOffFrequency);
            AddLog("LowerSweepBound: {0}, UpperSweepBound: {1}", LowerSweepBound, UpperSweepBound);
            AddLog("LowerVoltageBound: {0}V, UpperVoltageBound: {1}V", LowerVoltageBound, UpperVoltageBound);

            if (System.IO.Directory.Exists(SourceFolder) == false)
            {
                AddLog("!!! Cannot find folder '{0}'", SourceFolder);
                return;
            }

            //analyze exisiting files first
            Debug.Print("processing exisiting files in folder {0}", SourceFolder);
            Debug.Print("^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^");
            DirectoryInfo folder = new DirectoryInfo(SourceFolder);
            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                //Process/Analyze only the first file (useful for debugging a whole folder)
                await ProcessFileSync(folder.GetFiles().OrderBy(a => a.LastWriteTime).First().FullName);
                return;
            }


            foreach (FileInfo f in folder.GetFiles().OrderBy(a => a.LastWriteTime))
            {
                bool validfile = false;

                if (IsExtensionFilterEnabled == false)
                    validfile = true;   //all files are valid when Extension Filtering is disabled
                else if (System.IO.Path.GetExtension(f.FullName) == (ExtensionFilterString == null ? string.Empty : ExtensionFilterString.EnsureStartsWith(".")))
                    validfile = true;

                if (validfile)
                {
                    if (IsFileLocked(f.FullName) == false)
                    {
                        Debug.Print("Analyze. About to call ProcessFileSync for {0}", f.FullName);
                        await ProcessFileSync(f.FullName, false);
                        //if (IsExtensionFilterEnabled == false)
                        //    await ProcessFileSync(f.FullName, false);
                        //else if (System.IO.Path.GetExtension(f.FullName) == (ExtensionFilterString == null ? string.Empty : ExtensionFilterString.EnsureStartsWith(".")))
                        //{
                        //    Debug.Print("System.IO.Path.GetExtension(f.FullName) = '{0}' == '{1}'", System.IO.Path.GetExtension(f.FullName), ExtensionFilterString == null ? string.Empty : ExtensionFilterString.EnsureStartsWith("."));
                        //    await ProcessFileSync(f.FullName, false);
                        //}
                    }
                    else
                    {
                        AddLog("\tFileWatcher::File is locked. Will start Timer to monitor when it is available.");

                        ExtTimer t = new ExtTimer(2000);
                        t.Elapsed += T_Elapsed;
                        t.Tag = f.FullName;
                        t.AutoReset = true;
                        t.Start();
                        timers.Add(f.FullName, t);
                    }
                }
            }

            //Debug.Print("Average GetScaledData time = {0} ms", Files.Average(a => a.averagetime));


            if (Files.Count == 0 && folder.GetFiles().Count() > 0)
            {
                MessageBox.Show("No files were Analyzed yet there are files in the older. Consider changing Extension Filter");
            }

            UpdateOutputPlot(this.WindowLowerBound, this.WindowUpperBound);
            CalculateCOV();


            Debug.Print("finsihed processing exisiting files in folder {0}", SourceFolder);
            Debug.Print("^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^");
        }

        //public static void MyParallelFor(int inclusiveLowerBound, int exclusiveUpperBound, Action<int> body)
        //{
        //    // Get the number of processors, initialize the number of remaining
        //    // threads, and set the starting point for the iteration.
        //    int numProcs = Environment.ProcessorCount;
        //    int remainingWorkItems = numProcs;
        //    int nextIteration = inclusiveLowerBound;
        //    using (ManualResetEvent mre = new ManualResetEvent(false))
        //    {
        //        // Create each of the work items.
        //        for (int p = 0; p < numProcs; p++)
        //        {
        //            ThreadPool.QueueUserWorkItem(delegate
        //            {
        //                int index;
        //                while ((index = Interlocked.Increment(ref nextIteration) - 1) < exclusiveUpperBound)
        //                    body(index);

        //                if (Interlocked.Decrement(ref remainingWorkItems) == 0)
        //                    mre.Set();
        //            });
        //        }
        //        // Wait for all threads to complete.
        //        mre.WaitOne();
        //    }
        //}

        private void ProcessFile(string Filename, bool UpdatePlots = false)
        {
            AddLog("\tProcessing File '{0}'", System.IO.Path.GetFileName(Filename));

            //open the file
            LVBinFile file = OpenLabViewBinFile(Filename);

            if (file == null)
            {
                AddLog("!!! File is Null (Unabled to Open File), Unable to Process File '{0}'", Filename);
                return;
            }

            //AddLog("\t\t{0}", file.InfoString);           //too much information

            if (LowerSweepBound <= 0 || LowerSweepBound > file.AIChannels.First().RawData.Length)
            {
                AddLog("!!! LowerSweepBound is invalid (file.AIChannels.First().RawData.Length = {0}", file.AIChannels.First().RawData.Length);
                return;
            }

            if (UpperSweepBound < 0 || UpperSweepBound > file.AIChannels.First().RawData.Length)
            {
                AddLog("!!! UpperSweepBound is invalid (file.AIChannels.First().RawData.Length = {0}", file.AIChannels.First().RawData.Length);
                return;
            }

            if (UpperSweepBound < LowerSweepBound)
            {
                AddLog("!!! UpperSweepBound cannot be less than LowerSweepBound");
                return;
            }

            try
            {
                long LowerBound = FindClosest(file.AOChannels.First().TriangleWaveform, (decimal)LowerVoltageBound);
                long UpperBound = FindClosest(file.AOChannels.First().TriangleWaveform, (decimal)UpperVoltageBound, ExpandDirection.Down);
                if (UpperBound == -1 || LowerBound == -1)
                {
                    AddLog("!!! FindClosest function returned -1. Check Lower and UpperSweepBound values");
                    return;
                }

                List<List<DataPoint>> list_datapoints = new List<List<DataPoint>>();

                foreach (AIChannel channel in file.AIChannels)
                {
                    channel.IntegratedData = new decimal[(UpperSweepBound - LowerSweepBound) + 1];
                    List<DataPoint> datapoints = new List<DataPoint>();

                    decimal a = (decimal)((file.AOChannels.First().WaveformDuration / 1000) / file.AOChannels.First().SampleCount);

                    int backgroundsubstactionsweepcount = BackgroundSubtractionSweepCount;
                    int lowersweepbound = LowerSweepBound;
                    int uppersweepbound = UpperSweepBound;

                    for (int i = lowersweepbound - 1; i < uppersweepbound; i++)
                    {
                        channel.IntegratedData[i - lowersweepbound + 1] = CalculateCharge(channel.GetScaledData(i, file.IsFilterEnabled, file.FilterCutOffFrequency, backgroundsubstactionsweepcount), LowerBound, UpperBound, (decimal)channel.SamplingPeriod);
                        datapoints.Add(new DataPoint(i - lowersweepbound + 1, (double)channel.IntegratedData[i - lowersweepbound + 1]));
                    }
                    list_datapoints.Add(datapoints);
                }
                if (Files.Contains(file) == false)
                    Files.Add(file);

                SelectedFile = Files.Last();
                SelectedAIChannel = SelectedFile.AIChannels.First();

                if (UpdatePlots)
                {
                    UpdateOutputPlot(WindowLowerBound, WindowUpperBound);
                    CalculateCOV();
                }
            }
            catch (DivideByZeroException e1)
            {
                AddLog("DivideByZeroException thrown. Probably erroneous calculation in Butterworth filter\n\n{0}", e1.StackTrace);
            }
            catch (OverflowException e2)
            {
                AddLog("OverflowException thrown. Probably erroneous (calculation in Butterworth filter\n\n{0}", e2.StackTrace);
            }
            catch (Exception e3)
            {
                AddLog("Exception thrown: {0}. \n\n{1}", e3.Message, e3.StackTrace);
            }
        }

        private void UpdateOutputPlot(int WindowLowerBound, int WindowUpperBound)
        {
            if (Files == null | Files.Count() == 0)
            {
                AddLog("There are no files (UpdateOutputPlot)");
                return;
            }
            if (WindowUpperBound <= WindowLowerBound)
            {
                //MessageBox.Show("WindowUpperBound is smaller or equal to WindowLowerBound", "Error");
                AddLog("WindowUpperBound is smaller or equal to WindowLowerBound (UpdateOutputPlot)");
                return;
            }
            if (WindowLowerBound >= WindowUpperBound)
            {
                //MessageBox.Show("WindowLowerBound is greater or equal to WindowUpperBound", "Error");
                AddLog("WindowLowerBound is greater or equal to WindowUpperBound (UpdateOutputPlot)");
                return;
            }
            if ((WindowUpperBound - WindowLowerBound) > Files.First().AIChannels.First().IntegratedData.Count())
            {
                AddLog("Window (Upper - Lower) exceeds number of Intergrated data points (UpdateOutputPlot)");
                return;
            }

            AllIntegratedModel.Clear();

            OxyPlot.Wpf.OxyColorConverter oxyColorConverter = new OxyColorConverter();

            double[] x_data = new double[Files.Count];
            double[] y_data = new double[Files.Count];
            x_data.FillSeriesArray(Files.First().Duration.TotalSeconds);
            for (int channelnumber = 0; channelnumber < Files.First().AIChannels.Count; channelnumber++)
            {
                int j = 0;
                foreach (LVBinFile file in Files)
                {
                    y_data[j++] = DataAnalysisHelper.AnalyzeData(file.AIChannels[channelnumber].IntegratedData, OutputAggregateFunction, WindowLowerBound, WindowUpperBound);
                }

                AllIntegratedModel.AddLineSeries(x_data, y_data, OxyColors.MediumPurple);
                ((OxyPlot.Series.LineSeries)AllIntegratedModel.Model.Series.Last()).MarkerSize = 3;
                ((OxyPlot.Series.LineSeries)AllIntegratedModel.Model.Series.Last()).Color = Files.First().AIChannels[channelnumber].Stroke.ToOxyColor();
            }
            AllIntegratedModel.Model.InvalidatePlot(true);
            AllIntegratedModel.ScalePlotAxes(false);
        }

        private async Task ProcessFileSync(string Filename, bool UpdatePlots = false)
        {
            await Task.Run(() =>
            Dispatcher.Invoke(() => ProcessFile(Filename, UpdatePlots)));
        }

        private LVBinFile OpenLabViewBinFile(string Filename)
        {
            Debug.Print("OpenLabViewBinFile called: {0}", Filename);
            //read header
            FileStream stream = null;
            LVBinaryReader reader = null;
            try
            {
                LVBinFile file = new LVBinFile();
                file.FullFilename = Filename;


                stream = new FileStream(Filename, FileMode.Open, FileAccess.Read, FileShare.Read);
                reader = new LVBinaryReader(stream);

                file.ActualFileSize = stream.Length;
                //DebugFileStructure(reader);

                reader.BaseStream.Seek(0, SeekOrigin.Begin);

                int d1 = reader.ReadInt32();
                int aochannel_count = reader.ReadInt32();

                int aosample_count = reader.ReadInt32();

                reader.BaseStream.Seek(12 + (aosample_count * 0), SeekOrigin.Begin);

                file.AOChannels = new ObservableCollection<AOChannel>();
                for (int channel_number = 0; channel_number < aochannel_count; channel_number++)
                {
                    file.AOChannels.Add(new AOChannel(channel_number));
                    file.AOChannels.Last().TriangleWaveform = new decimal[aosample_count];
                    for (int x = 0; x < aosample_count; x++)
                    {
                        file.AOChannels.Last().TriangleWaveform[x] = (decimal)reader.ReadDouble();
                    }
                }
                long fileoffset = 12 + (file.AOChannels.Count * 8 * aosample_count);       //modern version
                //13324

                //Section: Waveform
                reader.BaseStream.Seek(fileoffset, SeekOrigin.Begin);
                int read_repeat = reader.ReadInt32();
                for (int i = 0; i < read_repeat; i++)
                {
                    reader.ReadInt32();  //samplecount repeated as many times as channels
                }
                read_repeat = reader.ReadInt32();
                for (int i = 0; i < read_repeat; i++)
                {
                    file.AOChannels[i].WaveformUpdateRate = reader.ReadDouble();  //samplecount repeated as many times as channels
                }
                read_repeat = reader.ReadInt32();
                for (int i = 0; i < read_repeat; i++)
                {
                    file.AOChannels[i].WaveformDuration = reader.ReadDouble();  //samplecount repeated as many times as channels
                }
                read_repeat = reader.ReadInt32();
                for (int i = 0; i < read_repeat; i++)
                {
                    file.AOChannels[i].WaveformScanRate = reader.ReadDouble();  //samplecount repeated as many times as channels
                }
                read_repeat = reader.ReadInt32();
                for (int i = 0; i < read_repeat; i++)
                {
                    file.AOChannels[i].WaveformMultiplier = reader.ReadDouble();  //samplecount repeated as many times as channels
                }
                read_repeat = reader.ReadInt32();
                for (int i = 0; i < read_repeat; i++)
                {
                    file.AOChannels[i].WaveformOffset = reader.ReadDouble();  //samplecount repeated as many times as channels
                }
                file.WaveformFrequency = reader.ReadDouble();               //22868

                reader.ReadInt32();         //not sure what this is?
                read_repeat = reader.ReadInt32();
                reader.ReadInt32();         //not sure what this is?

                //Section: Stimulus
                file.StimulationFrequency = reader.ReadInt32();
                file.StimulationPulses = reader.ReadInt32();
                file.StimulationPulseWidth = reader.ReadDouble();
                file.StimulationAmplitude = reader.ReadDouble();
                file.StimulationPolarity = (StimulationPolarityEnum)reader.ReadInt16();
                file.StimulusToScanDelay = reader.ReadDouble();
                file.StimulationUpdateRate = reader.ReadInt32();
                file.StimulationMultiplier = reader.ReadDouble();           //22926

                //Section: Collection
                file.CollectionStimOrTTL = reader.ReadInt32();
                file.CollectionPreEventScanCount = reader.ReadInt32();
                file.CollectionPostEventScanCount = reader.ReadInt32();
                file.CollectionNumberChannels = reader.ReadInt32();


                reader.ReadInt32();         //2
                reader.ReadInt32();         //1

                Int16 activechannel = reader.ReadInt16();
                file.CollectionActiveChannel = (activechannel / 256) - 48;  //safe way of avoiding DivideByZero error
                Int32 eventmode = reader.ReadInt32();       //need to check if this value actually changes with eventmode or not

                //this is where the filetypes start to differ.
                if (eventmode == 2111)
                    file.FileVersion = 1.0;     //old FSCAV file version (widely used in the Lipski lab in 2016 till 2018+
                else if (eventmode == 305)
                    file.FileVersion = 2.0;     //multi-channel support version of Knowmad Technologies WCCV (3.7?? 2018)
                else if (eventmode == 306)
                    file.FileVersion = 3.0;     //added 20/8/2019
                else
                {
                    throw new FileFormatException(string.Format("Unrecognized FileVersion (={0})", eventmode));
                }

                file.CollectionEventMode = LVBinFile.GetEventMode(eventmode);   //PF 6/11/2018 Is this really EventMode? It doesn't look right at all.

                if (file.FileVersion == 3.0)
                    read_repeat = reader.ReadInt32();
                else if (file.FileVersion == 2.0)
                    read_repeat = reader.ReadInt32();
                else if (file.FileVersion == 1.0)
                    read_repeat = 8;            //forced value of 8, and not advancing the reader
                else
                    throw new Exception();

                file.nA_Vs = new decimal[read_repeat];
                UInt16[] ADGain_ = new UInt16[read_repeat];
                file.ADGains = new decimal[read_repeat];
                file.GainMultipliers = new decimal[read_repeat];    //read directly from file for FileVersion 2.0

                //BLOCK #1
                for (int i = 0; i < read_repeat; i++)
                {
                    if (file.FileVersion == 3.0)
                        file.GainMultipliers[i] = (decimal)reader.ReadDouble();
                    else if (file.FileVersion == 2.0)
                        file.GainMultipliers[i] = (decimal)reader.ReadDouble();
                    else if (file.FileVersion == 1.0)
                    {
                        ADGain_[i] = reader.ReadUInt16();
                        reader.BaseStream.Seek(6, SeekOrigin.Current);
                    }
                }
                reader.ReadInt16().ToString();         //not sure what this is (=3)
                reader.ReadInt16().ToString();         //not sure what this is (=0)
                reader.ReadInt16().ToString();         //not sure what this is (=1000)

                if (file.FileVersion == 3.0)
                    read_repeat = reader.ReadInt32();
                else if (file.FileVersion == 2.0)
                    read_repeat = reader.ReadInt32();
                else if (file.FileVersion == 1.0)
                {
                    reader.ReadInt32();
                    read_repeat = 8;            //forced value of 8, and not advancing the reader
                }
                else
                    throw new Exception();

                //BLOCK #2
                for (int i = 0; i < read_repeat; i++)
                {
                    if (file.FileVersion == 3.0)
                    {
                        file.nA_Vs[i] = (decimal)reader.ReadSingle();
                        file.ADGains[i] = (file.GainMultipliers[i] / (20m / 65536m)) / file.nA_Vs[i];
                    }
                    else if (file.FileVersion == 2.0)
                    {
                        file.nA_Vs[i] = (decimal)reader.ReadSingle();
                        file.ADGains[i] = (file.GainMultipliers[i] / (20m / 65536m)) / file.nA_Vs[i];
                    }
                    else if (file.FileVersion == 1.0)
                    {
                        file.nA_Vs[i] = LVBinFile.Get_nA_V(reader.ReadUInt16());
                        file.ADGains[i] = LVBinFile.Get_AD_Gain(ADGain_[i], file.nA_Vs[i]);
                        file.GainMultipliers[i] = (file.nA_Vs[i] / file.ADGains[i]) * (20m / 65536m);
                        reader.BaseStream.Seek(2, SeekOrigin.Current);
                    }
                }

                //Main Analog Input (pA) data reading section
                reader.BaseStream.Seek(1000000, SeekOrigin.Begin);

                //Debug.Print("Reading Analog Input Data from '{0}'", file.FullFilename);
                int scan_offset = (file.AOChannels.First().SampleCount * 2) * (file.CollectionNumberChannels - 1);
                //Debug.Print(">>>scan_offset = {0} (AOChannel.SampleCount = {1})", scan_offset, file.AOChannels.First().SampleCount);

                //check for corrupt file (incomplete recording)
                if (file.ActualFileSize != file.ExpectedFileSize)
                {
                    throw new FileFormatException(string.Format("File length ({0} bytes) less than expected ({1} bytes). File corrupted or recording terminated early", file.ActualFileSize, file.ExpectedFileSize));
                }


                //New reading logic. Reads a sample from each channel sequentially until the end of the scan, then advances to the next scan
                //add channels first
                for (int channel_number = 0; channel_number < file.CollectionNumberChannels; channel_number++)
                {
                    //Debug.Print("\t Adding channel #{0} of {1}", channel_number, file.CollectionNumberChannels);
                    //reader.BaseStream.Seek(1000000 + (channel_number * scan_offset), SeekOrigin.Begin);

                    file.AIChannels.Add(new AIChannel(channel_number));
                    file.AIChannels.Last().RawData = new short[file.TotalScanCount][];
                    file.AIChannels.Last().Data = new decimal[file.TotalScanCount][];
                    file.AIChannels.Last().SamplingPeriod = (file.AOChannels.First().WaveformDuration / 1000d) / file.AOChannels.First().SampleCount;
                    file.AIChannels.Last().nA_V = file.nA_Vs[channel_number];
                    file.AIChannels.Last().ADGain = file.ADGains[channel_number];
                    file.AIChannels.Last().GainMultiplier = file.GainMultipliers[channel_number];
                    if (channel_number == file.CollectionActiveChannel)
                        file.AIChannels.Last().IsActive = true;
                    file.AIChannels.Last().IsDisabled = false;

                    for (int scan = 0; scan < file.TotalScanCount; scan++)
                        file.AIChannels.Last().RawData[scan] = new short[file.AOChannels.First().SampleCount];
                }

                reader.BaseStream.Seek(1000000, SeekOrigin.Begin);

                for (int scan = 0; scan < file.TotalScanCount; scan++)
                {
                    //Debug.Print("\t\t scan {0} of {1}", scan.ToString().PadRight(5), file.TotalScanCount);

                    for (int y = 0; y < file.AOChannels.First().SampleCount; y++)
                    {
                        for (int channel = 0; channel < file.CollectionNumberChannels; channel++)
                        {
                            file.AIChannels[channel].RawData[scan][y] = reader.ReadInt16();
                        }
                    }
                    //reader.BaseStream.Seek(scan_offset, SeekOrigin.Current);
                }


                //Below For-Loop was used until the 15th November. This is sequential: reading a whole scan for a channel, then a whole scan of the next channel, then advancing to the next scan
                //for (int channel_number = 0; channel_number < file.CollectionNumberChannels; channel_number++)
                //{
                //    Debug.Print("\t channel #{0} of {1}", channel_number, file.CollectionNumberChannels);
                //    reader.BaseStream.Seek(1000000 + (channel_number * scan_offset), SeekOrigin.Begin);

                //    file.AIChannels.Add(new AIChannel(channel_number));
                //    file.AIChannels.Last().RawData = new short[file.TotalScanCount][];
                //    file.AIChannels.Last().Data = new decimal[file.TotalScanCount][];
                //    file.AIChannels.Last().SamplingPeriod = (file.AOChannels.First().WaveformDuration / 1000d) / file.AOChannels.First().SampleCount;
                //    file.AIChannels.Last().nA_V = file.nA_Vs[channel_number];
                //    file.AIChannels.Last().ADGain = file.ADGains[channel_number];
                //    file.AIChannels.Last().GainMultiplier = file.GainMultipliers[channel_number];
                //    if (channel_number == file.CollectionActiveChannel)
                //        file.AIChannels.Last().IsActive = true;
                //    file.AIChannels.Last().IsDisabled = false;

                //    for (int scan = 0; scan < file.TotalScanCount; scan++)
                //    {
                //        //Debug.Print("\t\t scan {0} of {1}", scan.ToString().PadRight(5), file.TotalScanCount);
                //        file.AIChannels.Last().RawData[scan] = new short[file.AOChannels.First().SampleCount];

                //        for (int y = 0; y < file.AOChannels.First().SampleCount; y++)
                //        {
                //            file.AIChannels.Last().RawData[scan][y] = reader.ReadInt16();
                //        }
                //        reader.BaseStream.Seek(scan_offset, SeekOrigin.Current);
                //    }
                //}


                file.IsFilterEnabled = this.IsFilterEnabled;
                file.FilterCutOffFrequency = this.FilterCutOffFrequency;
                return file;

            }
            catch (Exception e)
            {
                AddLog("!!!  Unable to open file '{0}'", System.IO.Path.GetFileName(Filename));
                AddLog("!!!  {0}", e.Message);
                return null;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
                if (reader != null)
                    reader.Close();
            }


        }

        #endregion

        #region Plotting

        public IntegratedViewModel AllIntegratedModel
        {
            get { return (IntegratedViewModel)GetValue(AllIntegratedModelProperty); }
            set { SetValue(AllIntegratedModelProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AllIntegratedModelProperty =
            DependencyProperty.Register("AllIntegratedModel", typeof(IntegratedViewModel), typeof(MainWindow), new PropertyMetadata(null));

        public IntegratedViewModel IntegratedModel
        {
            get { return (IntegratedViewModel)GetValue(IntegratedModelProperty); }
            set { SetValue(IntegratedModelProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IntegratedModelProperty =
            DependencyProperty.Register("IntegratedModel", typeof(IntegratedViewModel), typeof(MainWindow), new PropertyMetadata(null));

        public ScopeViewModel ScopeModel
        {
            get { return (ScopeViewModel)GetValue(ScopeModelProperty); }
            set { SetValue(ScopeModelProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ScopeModelProperty =
            DependencyProperty.Register("ScopeModel", typeof(ScopeViewModel), typeof(MainWindow), new PropertyMetadata(null));

        public int SelectedSweep
        {
            get { return (int)GetValue(SelectedSweepProperty); }
            set { SetValue(SelectedSweepProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedSweepProperty =
            DependencyProperty.Register("SelectedSweep", typeof(int), typeof(MainWindow), new PropertyMetadata(0, SelectedSweepChanged));

        private static void SelectedSweepChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            MainWindow window = (MainWindow)o;

            //Debug.Print("SelectedSweepChanged {0}   {1}", e.NewValue, window.SelectedFile.Data.Count(), window.SelectedFile.TotalScanCount);


            if (window.SelectedFile == null)
                return;
            if ((int)e.NewValue == -1)
                return;
            if (window.SelectedFile.AIChannels == null)
                return;
            if (window.SelectedFile.AIChannels.Count() == 0)
                return;


            //Debug.Print("integrated sweep {0} > {1}     {2}     {3}", sweep, trap, wedge, window.SelectedFile.IntegratedData[counter]);
            //IList<DataPoint> waveformpoints = new List<DataPoint>();
            //List<List<DataPoint>> list_currentpoints = new List<List<DataPoint>>();
            int j = 0;

            //foreach (double value in window.SelectedFile.AOChannels.First().TriangleWaveform)
            //{
            //    waveformpoints.Add(new DataPoint(j, value));
            //    j++;
            //}

            double[] x_data = new double[window.SelectedFile.AOChannels.First().SampleCount];
            double[] y_data = new double[window.SelectedFile.AOChannels.First().SampleCount];
            x_data.FillSeriesArray(1);

            window.ScopeModel.Clear();
            window.ScopeModel.Model.Axes.Add(new OxyPlot.Axes.LinearAxis() { Key = "primary", Position = AxisPosition.Left, Title = "Current (substracted) nA???" });

            window.ScopeModel.Model.Axes.Add(new OxyPlot.Axes.LinearAxis() { Key = "secondary", Position = AxisPosition.Right, Title = "Scan Voltage" });
            window.ScopeModel.AddLineSeries(x_data, window.SelectedFile.AOChannels.First().TriangleWaveform.Select(a => (double)a));
            ((OxyPlot.Series.LineSeries)window.ScopeModel.Model.Series.Last()).YAxisKey = "secondary";

            foreach (AIChannel channel in window.SelectedFile.AIChannels)
            {
                decimal[] data = channel.GetScaledData((int)e.NewValue, window.SelectedFile.IsFilterEnabled, window.SelectedFile.FilterCutOffFrequency, window.BackgroundSubtractionSweepCount);

                //List<DataPoint> currentpoints = new List<DataPoint>();

                j = 0;
                foreach (decimal value in data)
                {
                    //this is sample #/time (x-axis) against current (y axis)
                    //currentpoints.Add(new DataPoint(j, (double)value));
                    //j++;

                    //this is votlage (x) against current (y)
                    y_data[j++] = (double)value;
                }
                window.ScopeModel.AddLineSeries(x_data, y_data);
                ((OxyPlot.Series.LineSeries)window.ScopeModel.Model.Series.Last()).YAxisKey = "primary";
                ((OxyPlot.Series.LineSeries)window.ScopeModel.Model.Series.Last()).Color = channel.Stroke.ToOxyColor();
            }

            window.ScopeModel.Model.InvalidatePlot(true);
            window.ScopeModel.ScalePlotAxes(false);
        }

        #endregion

        #region Private and Protected Functions

        private void Reset()
        {
            AddLog("----------------------------------------------------");
            AddLog("Resetting now {0}", DateTime.Now);
            AddLog("----------------------------------------------------");

            FileCount = 0;
            if (Files != null)
                Files.Clear();

            SelectedFile = null;
            SelectedSweep = -1;

            if (ScopeModel != null)
            {
                ScopeModel.Clear();
            }
            if (IntegratedModel != null)
            {
                IntegratedModel.Clear();
            }
            if (AllIntegratedModel != null)
            {
                AllIntegratedModel.Clear();
            }

            //ScopeModel = null;
            //IntegratedModel = null;
            //AllIntegratedModel = null;

            //ChargePlot.InvalidatePlot();
            //ScopePlot.InvalidatePlot();
            //OutputPlot.InvalidatePlot();
        }

        protected virtual bool IsFileLocked(string Filename)
        {
            FileStream stream = null;

            try
            {
                FileInfo file = new FileInfo(Filename);

                stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }

        private static int FindClosest(decimal[] Data, decimal Value, System.Windows.Controls.ExpandDirection Direction = ExpandDirection.Up)
        {
            switch (Direction)
            {
                case ExpandDirection.Up:
                    {
                        for (int i = 0; i < Data.Length; i++)
                        {
                            if (Data[i] > Value)
                                return i;
                        }
                        break;
                    }
                case ExpandDirection.Down:
                    {
                        for (int i = 0; i < Data.Length; i++)
                        {
                            if (Data[i] > Value)
                                return i - 1;
                        }
                        break;
                    }
                default:
                    {
                        for (int i = 0; i < Data.Length; i++)
                        {
                            if (Data[i] > Value)
                                return i;
                        }
                        break;
                    }
            }
            return -1;
        }

        private void DebugFileStructure(LVBinaryReader reader)
        {
            long pos;
            double d_;
            float f_;
            Int32 i32_;
            Int16 i16_;
            UInt16 ui16_;

            long start = 0;
            pos = start;

            reader.BaseStream.Seek(start, SeekOrigin.Begin);

            int column_width = 15;

            do
            {
                pos = reader.BaseStream.Position;

                d_ = reader.ReadDouble();
                reader.BaseStream.Position = pos;
                f_ = reader.ReadSingle();
                reader.BaseStream.Position = pos;
                i32_ = reader.ReadInt32();
                reader.BaseStream.Position = pos;
                i16_ = reader.ReadInt16();
                reader.BaseStream.Position = pos;
                ui16_ = reader.ReadUInt16();
                reader.BaseStream.Position = pos;
                Debug.Print("{0}>\tDouble64:{1}\tFloat32:{2}\tInt32:{3}\tInt16:{4}\tUInt16:{5}",
                    pos,
                    d_.ToString("0.00E+00").PadRight(column_width),
                    f_.ToString("0.00E+00").PadRight(column_width),
                    i32_.ToString().PadRight(column_width),
                    i16_.ToString().PadRight(column_width),
                    ui16_.ToString().PadRight(column_width));
                //Debug.Print("{0}>    Double64:{1}    Float32:{2}    Int32:{3}     Int16:{4}", pos, d_.ToString("0.00E+00"), f_.ToString("0.00E+00"), i32_, i16_);

                reader.BaseStream.Seek(2, SeekOrigin.Current);
            } while (pos < 25350);
            //for (long i = start; i < 11652; i++)
            //{
            //    //Debug.Print(i.ToString());
            //    //reader.BaseStream.Seek(i, SeekOrigin.Begin);


            //}
        }

        private void AddLog(string Message, params object[] Items)
        {
            int i = this.Logs.Count + 1;
            string s = i + ": " + string.Format(Message, Items);

            Dispatcher.Invoke((Action)(() =>
            {
                Logs.Add(s);
                Debug.Print(s);
            }));

        }

        private void CalculateCOV()
        {
            if (COV_window < 2)
                AddLog("COV window size is invalid");

            double mean = 0;
            double stdev = 1;
            if (Files.Count > COV_window)
            {
                mean = Files.TakeLast(COV_window).SelectMany(a => a.AIChannels[SelectedAIChannel.ChannelNumber].IntegratedData).Select(b => Convert.ToDouble(b)).Average();
                //MathNet.Numerics.Statistics.Statistics.StandardDeviation()
                //Files.TakeLast(10).SelectMany(a => a.Key.IntegratedData).St     MathNet
                stdev = Files.TakeLast(COV_window).SelectMany(a => a.AIChannels[SelectedAIChannel.ChannelNumber].IntegratedData).Select(b => Convert.ToDouble(b)).StandardDeviation();
                COV = stdev / mean;
            }
            else
                COV = 0.0d;
        }

        /// <summary>
        /// Analyze the FSCV trace to get some values of interest
        /// </summary>
        private void CalculateSD()
        {
            if (Files == null | Files.Count == 0)
                return;

            AnalysisOutputLB.Items.Clear();
            AnalysisOutputLB.Items.Add("CalculateSD");

            double baseline_SD = 0;
            double evoked_Max = 0;

            LVBinFile file = this.Files.First();

            try
            {
                //baseline_SD = DataAnalysisHelper.AnalyzeData(IntegratedModel.Model.Series.First().CastTo<LineSeries>().Points.Select(a => a.Y).ToArray(), AggregateFunctionEnum.StandardDeviation, 0, file.CollectionPreEventScanCount);
                //evoked_Max = DataAnalysisHelper.AnalyzeData(IntegratedModel.Model.Series.First().CastTo<LineSeries>().Points.Select(a => a.Y).ToArray(), AggregateFunctionEnum.Maximum, file.CollectionPreEventScanCount, file.CollectionPreEventScanCount + 10);

                //AnalysisOutputLB.Items.Add(string.Format("Baseline SD: {0}", baseline_SD.ToString("0.000")));
                //AnalysisOutputLB.Items.Add(string.Format("Maximum: {0}", evoked_Max.ToString("0.000")));
                //AnalysisOutputLB.Items.Add(string.Format("Ratio: {0}", (evoked_Max / baseline_SD).ToString("0.00")));
                //AnalysisOutputLB.Items.Add(string.Format("Passed Threshold: {0}", (evoked_Max / baseline_SD) > 2 ? "TRUE" : "FALSE"));
            }
            catch
            {
                AnalysisOutputLB.Items.Add("FAILED");
            }
            //file.DebugDump();
        }

        private string GetSettingsFolder()
        {
            string folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (Directory.Exists(folder) == false)
                Directory.CreateDirectory(folder);
            return folder;
        }

        //private void CalculateGainMultiplier(decimal nA_V, decimal AD_Gain)
        //{
        //    Gain = AD_Gain;     //works with new multi-channel version of file 23/10/2018
        //    //if (AD_Gain > 0m)
        //    //    Gain = (20m / 65536) * (nA_V / AD_Gain);
        //}

        #endregion

        #region Integration

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Data"></param>
        /// <param name="LowerBound"></param>
        /// <param name="UpperBound"></param>
        /// <param name="a">The Sample Period (= sweep duration / # of samples) in Seconds</param>
        /// <returns>Result is in pColoumbs (I think)</returns>
        private decimal CalculateCharge(decimal[] Data, long LowerBound, long UpperBound, decimal a)
        {
            //Data = Data.MultiplyArray(1e-9m);

            decimal wedge = 0m;
            decimal curve = 0m;

            curve = IntegrateRange(Data, LowerBound, UpperBound, a);

            wedge = Integrate(Data, LowerBound, UpperBound, a * (UpperBound - LowerBound));
            return (curve - wedge) * 1e6m; // * 1e12m;
        }

        private decimal IntegrateRange(decimal[] Data, long LowerBound, long UpperBound, decimal a)
        {
            decimal result = 0m;
            for (long i = LowerBound; i < UpperBound; i++)
            {
                result += Integrate(Data, i, i + 1, a);
            }
            return result;
        }

        private decimal Integrate(decimal[] Data, long LowerBound, long UpperBound, decimal a)
        {
            return ((Data[LowerBound] + Data[UpperBound]) * 0.5m) * a;

            //return (0.5m * a) * (Data[LowerBound] + Data[UpperBound]);
        }

        #endregion

        #region Copy data

        private void CopyTemporalToClipboard_Click(object sender, RoutedEventArgs e)
        {
            //MessageBox.Show("Will copy the Selected AIChannel Integrated Temporal data only");
            if (SelectedAIChannel == null)
            {
                MessageBox.Show("No AIChannel selected");
                return;
            }
            if (OutputAggregateFunction == AggregateFunctionEnum.MaxminumEffect)
            {
                MessageBox.Show("OutputAggregateFunction.MaximumEffect data cannot be exported currently");
                return;
            }
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Filename,Channel 1,Channel 2");
                foreach (LVBinFile file in this.Files)
                {
                    sb.AppendLine(string.Format("{0},{1},{2}",
                        file.Filename,
                        file.AIChannels[0].IntegratedData != null ? file.AIChannels[0].IntegratedData.AnalyzeData(this.OutputAggregateFunction).ToString() : "0",
                        file.AIChannels[1].IntegratedData != null ? file.AIChannels[1].IntegratedData.AnalyzeData(this.OutputAggregateFunction).ToString() : "0"));
                }
                //if (SelectedAIChannel.ChannelNumber == 0)
                //    AllIntegratedModel.Points1.Select(a => a.Y).CopyToClipboard();
                //else if (SelectedAIChannel.ChannelNumber == 1)
                //    AllIntegratedModel.Points2.Select(a => a.Y).CopyToClipboard();
                Clipboard.SetText(sb.ToString(), TextDataFormat.CommaSeparatedValue);
                AddLog("Integrated Data copied to Clipboard ({0} files)", Files.Count);
            }
            catch
            {
                AddLog("Unable to copy Integrated Data to Clipboard");
            }
        }

        //private void CopyVoltageWaveform_Click(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        SelectedFile.TriangleWaveform.CopyToClipboard();
        //        AddLog("Voltage Waveform copied to Clipboard");
        //    }
        //    catch
        //    {
        //        AddLog("Unable to copy Votlage Waveform to Clipboard");
        //    }
        //}

        //private void CopyCurrentResponse_Click(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        SelectedFile.GetScaledData(SelectedSweep, Gain, Offset, nA_V, BackgroundSubtractionSweepCount).CopyToClipboard();
        //        AddLog("Current Waveform copied to Clipboard (array index {0})", SelectedSweep);
        //    }
        //    catch
        //    {
        //        AddLog("Unable to copy Current Waveform to Clipboard");
        //    }
        //}

        private void CopyChargeData_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedFile == null)
                return;
            if (SelectedAIChannel == null)
            {
                MessageBox.Show("First select an AIChannel");
                return;
            }
            try
            {
                SelectedAIChannel.IntegratedData.CopyToClipboard();
                AddLog("Charge data copied to Clipboard");
            }
            catch
            {
                AddLog("Unable to copy Charge data to Clipboard");
            }
        }

        private void CopyBackgroundResponse_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedFile == null)
                return;
            try
            {
                SelectedFile.AIChannels.First().SubtractData.CopyToClipboard();
                AddLog("Background Current Waveform copied to Clipboard (array index {0})", SelectedSweep);
            }
            catch
            {
                AddLog("Unable to copy Background Current Waveform to Clipboard");
            }
        }

        #endregion

        //--------------------------------------------------------------------------
        // This function returns the data filtered. Converted to C# 2 July 2014.
        // Original source written in VBA for Microsoft Excel, 2000 by Sam Van
        // Wassenbergh (University of Antwerp), 6 june 2007.
        //--------------------------------------------------------------------------

        #region Settings

        private string RegistryPathBase = "Software\\Wow6432Node\\Silicon Nervous System\\Live Electrochemistry";          //if changed, update in SNS_Post_Install_Scripting


        private object GetRegistryValue(string Value, bool CurrentUser = false)
        {
            if (CurrentUser == false)
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(RegistryPathBase))
                {
                    if (key != null)
                    {
                        return key.GetValue(Value);
                    }
                    else
                        Debug.Print("Unable to GetRegistryValue '{0}'", Value);
                }
            }
            else
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryPathBase))
                {
                    if (key != null)
                    {
                        return key.GetValue(Value);
                    }
                    else
                        Debug.Print("Unable to GetRegistryValue '{0}'", Value);
                }
            }
            return null;
        }

        private void SaveSettings()
        {
            XmlWriter writer = XmlWriter.Create(GetSettingsFolder() + "\\Live Electrochemistry Settings.xml");
            XmlSerializer serializer = new XmlSerializer(typeof(Settings));

            Settings s = new Settings();

            //s.CurrentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            var result = GetRegistryValue("InstallerVersion");
            if (result == null)
                s.CurrentVersion = new Version(0, 0, 0, 0).ToString();
            else if (!result.ToString().IsNullOrWhiteSpace())
                s.CurrentVersion = result.ToString();
            else
                s.CurrentVersion = new Version(0, 0, 0, 0).ToString();

            s.RecentFolder = this.SourceFolder;
            s.IsExtensionFilterEnabled = this.IsExtensionFilterEnabled;
            s.ExtensionFilterString = this.ExtensionFilterString;
            serializer.Serialize(writer, s);

            writer.Close();

            AddLog("SaveSettings::Filename = {0}", GetSettingsFolder() + "\\Live Electrochemistry Settings.xml");
        }

        private void LoadSettings()
        {
            Settings s = new Settings();
            s.RecentFolder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            if (File.Exists(GetSettingsFolder() + "\\Live Electrochemistry Settings.xml"))
            {
                XmlReader reader = null;
                Debug.Print("Settings::{0}", GetSettingsFolder() + "\\Live Electrochemistry Settings.xml");

                try
                {
                    reader = XmlReader.Create(GetSettingsFolder() + "\\Live Electrochemistry Settings.xml");
                    XmlSerializer Serializer = new XmlSerializer(typeof(Settings));
                    s = (Settings)Serializer.Deserialize(reader);
                }
                catch
                {
                    AddLog("Error occured trying to Deserialize Live Electrochemistry Settings.xml");
                }
                finally
                {
                    if (reader != null)
                        reader.Close();

                    //check if the Settings file was created with a different version of the Application (using InstallerVersion as a proxy for Application Version since I can control InstallerVersion easier)
                    var result = GetRegistryValue("InstallerVersion");
                    string CurrentVersion = result == null ? string.Empty : result.ToString();
                    
                    if (s.CurrentVersion != CurrentVersion)
                    {
                        MessageBox.Show(string.Format("Settings File was created with version {0}, which is not the current version {1}\n\nYou should delete the Settings and Layout .xml files from My Documents", s.CurrentVersion, CurrentVersion), "Incorrect Version", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    AddLog(string.Format("Settings File was created with Version {0} (Current Version {1})", s.CurrentVersion, CurrentVersion));
                }
            }
            else
            {
                //create empty Settings, since the file cannot be found (ie. running for the first time)
            }

            this.SourceFolder = s.RecentFolder;
            this.IsExtensionFilterEnabled = s.IsExtensionFilterEnabled;
            this.ExtensionFilterString = s.ExtensionFilterString;
        }

        #endregion

        private void File_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                var path = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];
                if (Directory.Exists(path))
                {
                    this.SourceFolder = path;
                    return;
                }

                LVBinFile file;

                foreach (string filename in files)
                {

                    file = OpenLabViewBinFile(filename);


                    if (file == null)
                    {
                        MessageBox.Show("Unable to open the file", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    file.AOChannels.First().TriangleWaveform.CopyToClipboard();


                    decimal[] scaleddata = file.AIChannels.First().GetScaledData(0, file.IsFilterEnabled, file.FilterCutOffFrequency, BackgroundSubtractionSweepCount);


                    IList<DataPoint> waveformpoints = new List<DataPoint>(file.TotalScanCount);
                    IList<DataPoint> currentpoints = new List<DataPoint>(file.TotalScanCount);
                    int j = 0;


                    foreach (decimal value in scaleddata)
                    {
                        waveformpoints.Add(new DataPoint(j, (double)file.AOChannels.First().TriangleWaveform[j]));
                        currentpoints.Add(new DataPoint(j, (double)value));
                        j++;
                    }

                    //QuickViewModel = new ScopeViewModel(currentpoints, waveformpoints);

                    decimal[] integrateddata = new decimal[file.TotalScanCount];

                    int LowerBound = FindClosest(file.AOChannels.First().TriangleWaveform, (decimal)LowerVoltageBound);
                    int UpperBound = FindClosest(file.AOChannels.First().TriangleWaveform, (decimal)UpperVoltageBound, ExpandDirection.Down);
                    //decimal a = (decimal)((file.SweepDuration / 1000) / file.SampleCount);

                    //decimal gain = Gain;
                    decimal offset = Offset;
                    //decimal na_v = nA_V;
                    int backgroundsubstactionsweepcount = BackgroundSubtractionSweepCount;

                    Parallel.For(0, file.TotalScanCount,
                        index =>
                        {
                            scaleddata = file.AIChannels.First().GetScaledData(index, file.IsFilterEnabled, file.FilterCutOffFrequency, backgroundsubstactionsweepcount);
                            integrateddata[index] = CalculateCharge(scaleddata, LowerBound, UpperBound, (decimal)file.AIChannels.First().SamplingPeriod);

                        });


                    //for (int i = 0; i < file.TotalScanCount; i++)
                    //{
                    //    scaleddata = file.GetScaledData(i, Gain, Offset, nA_V, BackgroundSubtractionSweepCount);

                    //    integrateddata[i] = CalculateCharge(scaleddata, LowerBound, UpperBound, (decimal)file.SamplingPeriod);

                    //}



                    //Helper.Extensions.Extensions.CopyToClipboard<decimal>(integrateddata);

                    //########################## The follow 1 line of code is disabled as it is not compatible with multichannel yet
                    //file.IntegratedData = integrateddata;
                    Files.Add(file);
                }
            }


            //############################## The following block could possoibly be updated to use the generic Files and SelectedFile objects
            //decimal[] averageintegrateddata = new decimal[quickfile.TotalScanCount];

            //if (quickfiles.Any(a => a.IntegratedData.Length != quickfiles.First().IntegratedData.Length))
            //{
            //    MessageBox.Show("Not all QuickFiles have the same duration and cannot be averaged together.\n\nPrevious files will be ignored");
            //    quickfiles.RemoveRange(0, quickfiles.Count - 2);
            //    return;
            //}

            //averageintegrateddata = Helper.Analysis.DataAnalysisHelper.CrossAverageArrays(quickfiles.Select(a => a.IntegratedData));

            //IList<DataPoint> integratedpoints = new List<DataPoint>(quickfile.TotalScanCount);
            //int p = 0;
            //foreach (decimal value in averageintegrateddata)
            //{
            //    integratedpoints.Add(new DataPoint(p, (double)value));
            //    p++;
            //}

            //QuickViewIntergratedModel = new IntegratedViewModel(integratedpoints);
            //######################################

            SelectedFile = Files.Last();
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(ExportSweepSelection.Text, out int sweep) == false)
            {
                MessageBox.Show("Invalid ExportSweepSelection value (must be a number)");
                return;
            }

            if (SelectedFile == null)
            {
                MessageBox.Show("No file opened");
                return;
            }

            if (sweep < 0)
            {
                MessageBox.Show("Invalid Sweep #)");
                return;
            }
            if (sweep > SelectedFile.TotalScanCount)
            {
                MessageBox.Show("Invalid Sweep #");
                return;
            }

            SelectedFile.AIChannels.First().GetScaledData(sweep, SelectedFile.IsFilterEnabled, SelectedFile.FilterCutOffFrequency, BackgroundSubtractionSweepCount).CopyToClipboard();
        }
        private void CopyTriangleWaveform_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(ExportSweepSelection.Text, out int sweep) == false)
            {
                MessageBox.Show("Invalid ExportSweepSelection value (must be a number)");
                return;
            }
            if (SelectedFile == null)
            {
                MessageBox.Show("No file opened");
                return;
            }

            SelectedFile.AOChannels.Select(a => a.TriangleWaveform).CopyToClipboard();
        }
        private void CopyVoltamogram_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(ExportSweepSelection.Text, out int sweep) == false)
            {
                MessageBox.Show("Invalid ExportSweepSelection value (must be a number)");
                return;
            }

            if (SelectedFile == null)
                return;
            if (SelectedAOChannel == null)
            {
                MessageBox.Show("First select an AOChannel");
                return;
            }
            try
            {
                SelectedAOChannel.TriangleWaveform.CopyToClipboard(
                    SelectedFile.AIChannels.First().GetScaledData(sweep, SelectedFile.IsFilterEnabled, SelectedFile.FilterCutOffFrequency, BackgroundSubtractionSweepCount),
                    new List<string>() { "Voltage (V)", "Current (nA?)" });
                AddLog(string.Format("Voltamogram copied to Clipboard (sweep #{0} of {1})", sweep, SelectedFile.Filename));
            }
            catch
            {
                AddLog("Unable to copy Voltamogram to Clipboard");
            }
        }

        #region Debug

        private void DebugExportRaw_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedFile == null)
                return;

            SelectedAIChannel.RawData[SelectedSweep].CopyToClipboard(AdditionalArray: null, ("Raw").ItemAsEnumerable());

            MessageBox.Show(string.Format("Raw Data (I16) exported to clipboard\n\nSweep #{0} (zero indexed?)", SelectedSweep));
        }

        private void DebugExport_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedFile == null)
                return;

            SelectedAIChannel.GetScaledData(SelectedSweep, SelectedFile.IsFilterEnabled, SelectedFile.FilterCutOffFrequency, 0).CopyToClipboard(AdditionalArray: null, ("Data").ItemAsEnumerable());

            MessageBox.Show(string.Format("Data (decimal) exported to clipboard\n\nSweep #{0} (zero indexed?)\n\nNot background subtracted\n\nGain = {1}    Offset = {2}   nA/V = {3}",
                SelectedSweep, SelectedAIChannel.ADGain, Offset, SelectedAIChannel.nA_V));

        }

        private void DebugExportRawBackground_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedFile == null)
                return;

            SelectedAIChannel.RawSubtractData.CopyToClipboard(AdditionalArray: null, ("Raw Background").ItemAsEnumerable());

            MessageBox.Show(string.Format("Raw Background Data (I16) exported to clipboard\n\n{0} sweeps (from 0)", BackgroundSubtractionSweepCount));

        }

        private void DebugExportBackground_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedFile == null)
                return;

            SelectedAIChannel.SubtractData.CopyToClipboard(AdditionalArray: null, ("Background").ItemAsEnumerable());

            MessageBox.Show(string.Format("Background Data (decimal) exported to clipboard\n\n{0} sweeps (from 0)", BackgroundSubtractionSweepCount));

        }

        private void DebugExportVoltageTriangle_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedFile == null)
                return;

            SelectedFile.AOChannels.Select(a => a.TriangleWaveform).CopyToClipboard(AdditionalArray: null, ("Volate Triangle").ItemAsEnumerable());

            MessageBox.Show(string.Format("Voltage Triangle Command (decimal) exported to clipboard\n\n{0} data points", SelectedFile.AOChannels.Count()));
        }

        #endregion
        private void FindPeak_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Feature disabled at the moment");
            //throw new NotImplementedException("This feature is not compatible with Multi-channel recording yet");
            //try
            //{
            //    double peak = (double)SelectedAIChannel.IntegratedData.Max();
            //    //Double peak = this.IntegratedModel.Points.Max(a => a.Y);
            //    DataPoint point;
            //    if (SelectedAIChannel.ChannelNumber == 0)
            //    {
            //        point = this.IntegratedModel.Points1.First(a => a.Y == peak);
            //    }
            //    else
            //        point = this.IntegratedModel.Points2.First(a => a.Y == peak);

            //    PeakValue.Text = peak.ToString("0.##");
            //    PeakValueTime.Text = "@ " + (point.X / SelectedFile.WaveformFrequency).ToString("0.0 s");

            //    SelectedSweep = (int)point.X;
            //}
            //catch
            //{
            //    MessageBox.Show("Unable to Find Peak");
            //}
        }

        #region Open and Close application, Load/Save Layout

        protected override void OnClosing(CancelEventArgs e)
        {
            SaveLayout();
            SaveSettings();
            base.OnClosing(e);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadLayout();
        }

        private void SaveLayout()
        {
            string filename = GetSettingsFolder() + "\\Live Electrochem Layout.xml";

            if (PurgeLayoutOnExit)
            {
                try
                {
                    File.Delete(filename);
                }
                catch
                {
                    throw;
                }
                return;
            }

            XmlWriter writer = XmlWriter.Create(filename);

            XmlLayoutSerializer layoutSerializer = new XmlLayoutSerializer(dockingmanager);
            layoutSerializer.Serialize(writer);

            Debug.Print("SaveLayout::Filename = {0}", filename);

            writer.Close();
        }

        private void LoadLayout()
        {
            string filename = GetSettingsFolder() + "\\Live Electrochem Layout.xml";

            try
            {
                XmlReader reader = XmlReader.Create(filename);
                var currentContentsList = this.dockingmanager.Layout.Descendents().OfType<LayoutContent>().Where(c => c.ContentId != null).ToArray();

                XmlLayoutSerializer layoutSerializer = new XmlLayoutSerializer(dockingmanager);
                layoutSerializer.LayoutSerializationCallback += (obj, arg) =>
                {
                    Debug.Print("   XmlLayoutSerializer.DeSerializing Title: {0}, Content: {1}",
                        arg.Model.Title, arg.Content == null ? "null" : arg.Content);
                    //arg.Content = arg.Content;
                    var prevContent = currentContentsList.FirstOrDefault(c => c.ContentId == arg.Model.ContentId);
                    if (prevContent != null) { arg.Content = prevContent.Content; }
                };
                layoutSerializer.Deserialize(reader);
                Debug.Print("Settings::LoadLayout completed");
            }
            catch (FileNotFoundException fileex)
            {
                AddLog("!!! Layout file {0} could not be found\n\nError Message: {1}", filename, fileex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Error occured loading Layout \n\t{0}", ex.Message), "Error");
            }
        }

        #endregion
        private void ScalePlot_Click(object sender, RoutedEventArgs e)
        {
            PlotViewModelBase pvmb = (PlotViewModelBase)((Button)sender).Tag;
            AxisPropertiesWindow apw = new AxisPropertiesWindow();
            apw.Owner = this;
            apw.PlotModel = pvmb;
            if (apw.ShowDialog().GetValueOrDefault(false) == true)
            {
                apw.PlotModel.ScalePlotAxes();
            }
        }

        #region Default Settings
        private void Default_FSCV_Click(object sender, RoutedEventArgs e)
        {
            WindowLowerBound = 0;
            WindowUpperBound = 9;
            OutputAggregateFunction = AggregateFunctionEnum.Maximum;

            //should try to automatically set Lower and Upper Window Bound from the files (or First example file)

        }

        private void Default_FSCAV_Click(object sender, RoutedEventArgs e)
        {
            WindowLowerBound = 0;
            WindowUpperBound = 2;
            OutputAggregateFunction = AggregateFunctionEnum.Average;
        }
        #endregion
        private DoubleRectangle GetFullDataRange(OxyPlot.Wpf.Plot Plot)
        {
            DoubleRectangle result = new DoubleRectangle();

            PlotModel pm = Plot.ActualModel;

            double ymin = double.MaxValue;
            double ymax = double.MinValue;
            double xmin = double.MaxValue;
            double xmax = double.MinValue;


            foreach (OxyPlot.Wpf.LineSeries series in Plot.Series)
            {
                foreach (DataPoint item in series.Items)
                {
                    Debug.Print("object = {0}", item);
                    ymin = Math.Min(ymin, item.Y);
                    ymax = Math.Max(ymax, item.Y);
                    xmin = Math.Min(xmin, item.X);
                    xmax = Math.Max(xmax, item.X);
                }
                //for (int i = 0; i <= series.Points.Count() - 1; i++)
                //{
                //    ymin = Math.Min(ymin, series.Points[i].Y);
                //    ymax = Math.Max(ymax, series.Points[i].Y);
                //    xmin = Math.Min(ymin, series.Points[i].X);
                //    xmax = Math.Max(ymax, series.Points[i].X);
                //}
            }

            result = new DoubleRectangle()
            {
                Bottom = ymin,
                Top = ymax,
                Left = xmin,
                Right = xmax
            };
            return result;
        }
        private void EmailLog_Click(object sender, RoutedEventArgs e)
        {
            SmtpClient client = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl = true
            };

            MailMessage message = new MailMessage();
            message.To.Add(new MailAddress("peter.s.freestone@gmail.com"));
            message.From = new MailAddress("siliconnervoussystem@gmail.com");
            message.Body = string.Join("\n", this.Logs);
            message.Subject = string.Format("Live Electrochem Debug: {0}", DateTime.Now.ToString());

            //foreach (string filename in this.Attachments)
            //{
            //    message.Attachments.Add(new Attachment(filename));
            //}

            NetworkCredential credential = new NetworkCredential("siliconnervoussystem@gmail.com", @"wP39Q[20", "");

            client.Credentials = credential;
            client.SendCompleted += new SendCompletedEventHandler(Client_SendCompleted);
            //client.SendAsync(message, string.Format("EmailFile_{0}", File.FileId));
            try
            {
                client.Send(message);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(string.Format("Local error occured trying to send email\n\n{0}", ex.Message), "Error Emailing Message", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                client.Dispose();
            }
        }
        static void Client_SendCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (e.Cancelled)
                System.Windows.MessageBox.Show("SendEmail was cancelled");
            if (e.Error != null)
                System.Windows.MessageBox.Show(string.Format("SendEmail Failed\n\n{0}", e.Error.Message));
            else
            {
                System.Windows.MessageBox.Show("Email sent ok");
                //add aduit entry
            }
        }

        private void CalibrationSource_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog d = new OpenFileDialog();        //eventually support opening a folder so that it works for FSCAV calibrations (not just FSCV)
            d.Multiselect = false;

            if (d.ShowDialog(this) == true)
            {
                this.CalibrationSource = d.FileName;
            }
            else
                return;

            LVBinFile calibrationfile = OpenLabViewBinFile(this.CalibrationSource);
            if (calibrationfile == null)
            {
                AddLog("Unable to open Calibration File");
                return;
            }
            AddLog("Calibration analysis started");

            long LowerBound = FindClosest(calibrationfile.AOChannels.First().TriangleWaveform, (decimal)LowerVoltageBound);
            long UpperBound = FindClosest(calibrationfile.AOChannels.First().TriangleWaveform, (decimal)UpperVoltageBound, ExpandDirection.Down);
            int RMS_lb = CalibrationRMSWindowCentre - (int)(0.5 * (CalibrationRMSWindowWidth - 1));
            int RMS_ub = CalibrationRMSWindowCentre + (int)(0.5 * (CalibrationRMSWindowWidth - 1));

            if (RMS_lb < 0)
            {
                AddLog("CalibrationRMSWindowCentre and/or CalibrationRMSWindowWidth are invalid (out of range)");
                return;
            }
            if (RMS_ub >= calibrationfile.AIChannels.First().SampleCount)
            {
                AddLog("CalibrationRMSWindowCentre and/or CalibrationRMSWindowWidth are invalid (out of range)");
                return;
            }
            CalibrationSteps.Clear();
            CalibrationSteps.AddRangeUnique(ExtractCalibrationSteps(calibrationfile));

            return;

            AddLog("Peak search Window {0} ({1} V) to {2} ({3} V)", LowerBound, LowerVoltageBound, UpperBound, UpperVoltageBound);

            foreach (AIChannel aichannel in calibrationfile.AIChannels)
            {
                double[] values_a = new double[aichannel.ScanCount - 1];
                double[] values_b = new double[aichannel.ScanCount - 1];
                double value_a; //this is the value (Current) taken from the voltamogram at a certain/fixed INDEX/VOLTAGE
                double value_b; //this is the PEAK value (Current) taken from the voltamogram (within a window)
                double value_RMS;

                List<double> performance_GetScaledData = new List<double>(aichannel.ScanCount);
                List<double> performance_Remaining = new List<double>(aichannel.ScanCount);

                Stopwatch sw = new Stopwatch();



                Stopwatch overall = new Stopwatch();
                overall.Restart();
                for (int scan = 0; scan < aichannel.ScanCount - 1; scan++)
                {
                    sw.Restart();
                    //double[] data = Array.ConvertAll<decimal, double>(aichannel.GetScaledData(i, calibrationfile.IsFilterEnabled, calibrationfile.FilterCutOffFrequency, BackgroundSubtractionSweepCount), new Converter<decimal, double>(DecimalToDouble));

                    decimal[] data = aichannel.GetScaledData(scan, calibrationfile.IsFilterEnabled, calibrationfile.FilterCutOffFrequency, BackgroundSubtractionSweepCount);
                    performance_GetScaledData.Add(sw.ElapsedMilliseconds);
                    sw.Stop();

                    sw.Restart();
                    value_a = (double)data[CalibrationVoltamogramPeakIndex];
                    value_b = data.Max(out long IndexOfPeak, LowerBound, UpperBound);

                    values_a[scan] = value_a;
                    values_b[scan] = value_b;
                    value_RMS = data.CalculateRMS(RMS_lb, RMS_ub);

                    Debug.Print("{0}\t{1} (@{6})\t{2} (@{3})\t{4}\t{5}", scan, value_a, value_b, IndexOfPeak, value_RMS, value_b / value_RMS, CalibrationVoltamogramPeakIndex);

                    sw.Stop();
                    performance_Remaining.Add(sw.ElapsedMilliseconds);

                }
                TemporalTrace tt = new TemporalTrace(values_a, 1, DateTime.Now);
                CalibrationPlot.AddTrace(tt, this, true);

                Debug.Print("Average performance for performance_GetScaledData = {0}±{1} ms", performance_GetScaledData.Average(), Helper.Analysis.DataAnalysisHelper.CalculateError(performance_GetScaledData.ToArray(), null, ErrorTypesEnum.SEM));
                Debug.Print("Average performance for performance_Remaining = {0}±{1} ms", performance_Remaining.Average(), Helper.Analysis.DataAnalysisHelper.CalculateError(performance_Remaining.ToArray(), null, ErrorTypesEnum.SEM));
                overall.Stop();
                Debug.Print("Overall time = {0} ms", overall.ElapsedMilliseconds);
            }


            CalibrationPlot.InvalidateVisual();
            CalibrationPlot.UpdateLayout();
        }

        private IEnumerable<Event> ExtractCalibrationSteps(LVBinFile CalibrationFile)
        {
            IEnumerable<Event> calibrationsteps = null;

            //initial scan to detect where concentrations are
            foreach (AIChannel aichannel in CalibrationFile.AIChannels)
            {
                decimal[] rawdata = new decimal[aichannel.ScanCount - 1];
                decimal[] rawdata_0 = new decimal[aichannel.ScanCount - 1];
                decimal[] rawdata_1 = new decimal[aichannel.ScanCount - 1];
                decimal[] rawdata_2 = new decimal[aichannel.ScanCount - 1];
                decimal[] rawdata_3 = new decimal[aichannel.ScanCount - 1];
                decimal[] rawdata_4 = new decimal[aichannel.ScanCount - 1];

                for (int scan = 0; scan < aichannel.ScanCount - 1; scan++)
                {

                    ///################################ ATTENTION - Currently uses the UNFILTERED data to improve performance
                    //decimal[] xxx = aichannel.GetScaledData(scan, false, calibrationfile.FilterCutOffFrequency, BackgroundSubtractionSweepCount);
                    //Debug.Print("scan #{0}\txxx is {1}", scan, xxx != null);
                    //Debug.Print("\t\t\t{0}", xxx[CalibrationVoltamogramPeakIndex]);
                    rawdata[scan] = aichannel.GetScaledData(scan, true, CalibrationFile.FilterCutOffFrequency, BackgroundSubtractionSweepCount)[CalibrationVoltamogramPeakIndex];

                    //Debug.Print("{0}\t{1}", scan, data[CalibrationVoltamogramPeakIndex]);
                }
                //filter the rawdata
                rawdata = DataAnalysisHelper.Butterworth(rawdata, 1 / CalibrationFile.WaveformFrequency, 0.1);
                //rawdata_0 = DataAnalysisHelper.Butterworth(rawdata, 1 / calibrationfile.WaveformFrequency, 0.1);      //these are for debugging only
                //rawdata_1 = DataAnalysisHelper.Butterworth(rawdata, 1 / calibrationfile.WaveformFrequency, 0.5);
                //rawdata_2 = DataAnalysisHelper.Butterworth(rawdata, 1 / calibrationfile.WaveformFrequency, 1);
                //rawdata_3 = DataAnalysisHelper.Butterworth(rawdata, 1 / calibrationfile.WaveformFrequency, 2);
                //rawdata_4 = DataAnalysisHelper.Butterworth(rawdata, 1 / calibrationfile.WaveformFrequency, 4);


                double[] xdata = new double[aichannel.ScanCount - 1];
                DataAnalysisHelper.FillSeriesArray<double>(xdata, 1 / CalibrationFile.WaveformFrequency);
                double[] data = Array.ConvertAll<decimal, double>(rawdata, new Converter<decimal, double>(DecimalToDouble));
                double[] differentiated = data.Differentiate(xdata);


                EventDetectorProfile normalprofile = EventDetectorProfile.EmptyProfile("Normal");
                EventDetectorProfile differentiatedprofile = new EventDetectorProfile("Differentiated")
                {
                    EventPolarity = EventPolarityEnum.Up,
                    IsMinimumValueEnabled = true,
                    MinimumValue = new CalculatedValue(0.2)
                };

                calibrationsteps = DataAnalysisHelper.DetectEvents(data, xdata, 1 / CalibrationFile.WaveformFrequency, normalprofile, differentiatedprofile).Select(a => (Event)a).ToList();

                //debug output to testing
                for (int scan = 0; scan < aichannel.ScanCount - 1; scan++)
                {
                    //Debug.Print("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", rawdata[scan], rawdata_0[scan], rawdata_1[scan], rawdata_2[scan], rawdata_3[scan], rawdata_4[scan]);


                    //Debug.Print("{0}\t{1}\t{2}", scan, rawdata[scan], differentiated[scan]);
                }
            }

            return calibrationsteps;
        }

        public static double DecimalToDouble(decimal value)
        {
            return (double)value;
        }
    }

}
