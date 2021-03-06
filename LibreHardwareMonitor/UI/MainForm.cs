// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Aga.Controls.Tree;
using Aga.Controls.Tree.NodeControls;
using LibreHardwareMonitor.Hardware;
using LibreHardwareMonitor.Rtss;
using LibreHardwareMonitor.Utilities;
using LibreHardwareMonitor.Wmi;
using Open.WinKeyboardHook;

namespace LibreHardwareMonitor.UI
{
    public sealed partial class MainForm : Form
    {
        private readonly PersistentSettings _settings;
        private readonly UnitManager _unitManager;
        private readonly Computer _computer;
        private readonly ComputerNode _root;

        private readonly StartupManager _startupManager = new StartupManager();
        private readonly UpdateVisitor _updateVisitor = new UpdateVisitor();

        private readonly SystemTray _systemTray;
        private readonly SensorGadget _gadget;
        private readonly RtssAdapter _rtssAdapter;
        private readonly HttpServer _server;

        private readonly PlotPanel _plotPanel;
        private readonly Color[] _plotColorPalette;
        private readonly Form _plotForm;

        private readonly UserOption _showPlot;
        private readonly UserRadioGroup _plotLocation;

        private readonly UserOption _minimizeToTray;
        private readonly UserOption _minimizeOnClose;
        private readonly UserOption _autoStart;

        private readonly UserOption _readMainboardSensors;
        private readonly UserOption _readCpuSensors;
        private readonly UserOption _readRamSensors;
        private readonly UserOption _readGpuSensors;
        private readonly UserOption _readFanControllersSensors;
        private readonly UserOption _readHddSensors;
        private readonly UserOption _readNicSensors;

        private readonly UserOption _showGadget;

        private readonly UserOption _runRtssService;
        private readonly UserOption _runWebServer;
        private readonly UserOption _wmiService;
        private readonly UserOption _logSensors;

        private readonly UserRadioGroup _loggingInterval;
        private readonly UserRadioGroup _sensorValuesTimeWindow;

        private readonly WmiProvider _wmiProvider;
        private readonly Logger _logger;

        private readonly GlobalHotkey _globalHotkey;

        private bool _selectionDragging;
        private int _delayCount;

        private IDictionary<ISensor, Color> _sensorPlotColors = new Dictionary<ISensor, Color>();

        public MainForm()
        {
            InitializeComponent();

            // check if the LibreHardwareMonitorLib assembly has the correct version
            if (Assembly.GetAssembly(typeof(Computer)).GetName().Version != Assembly.GetExecutingAssembly().GetName().Version)
            {
                MessageBox.Show("The version of the file LibreHardwareMonitorLib.dll is incompatible.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
            }

            _settings = new PersistentSettings();
            _settings.Load(Path.ChangeExtension(Application.ExecutablePath, ".config"));

            _unitManager = new UnitManager(_settings);

            // make sure the buffers used for double buffering are not disposed
            // after each draw call
            BufferedGraphicsManager.Current.MaximumBuffer = Screen.PrimaryScreen.Bounds.Size;

            // set the DockStyle here, to avoid conflicts with the MainMenu
            splitContainer.Dock = DockStyle.Fill;

            Font = SystemFonts.MessageBoxFont;
            treeView.Font = SystemFonts.MessageBoxFont;

            // Set the bounds immediately, so that our child components can be
            // properly placed.
            Bounds = new Rectangle
            {
                X = _settings.GetValue("mainForm.Location.X", Location.X),
                Y = _settings.GetValue("mainForm.Location.Y", Location.Y),
                Width = _settings.GetValue("mainForm.Width", 470),
                Height = _settings.GetValue("mainForm.Height", 640)
            };

            _plotPanel = new PlotPanel(_settings, _unitManager)
            {
                Font = SystemFonts.MessageBoxFont, Dock = DockStyle.Fill
            };
            _plotForm = new Form
            {
                FormBorderStyle = FormBorderStyle.Sizable,
                MinimizeBox = false,
                MaximizeBox = false,
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual,
                Icon = Icon
            };

            AddOwnedForm(_plotForm);

            _showPlot = new UserOption("plotMenuItem", false, plotMenuItem, _settings);
            _plotLocation = new UserRadioGroup("plotLocation", 0, new[] { plotWindowMenuItem, plotBottomMenuItem, plotRightMenuItem }, _settings);

            nodeTextBoxText.DrawText += NodeTextBoxText_DrawText;
            nodeTextBoxValue.DrawText += NodeTextBoxText_DrawText;
            nodeTextBoxMin.DrawText += NodeTextBoxText_DrawText;
            nodeTextBoxMax.DrawText += NodeTextBoxText_DrawText;
            nodeTextBoxText.EditorShowing += NodeTextBoxText_EditorShowing;

            foreach (TreeColumn column in treeView.Columns)
            {
                column.Width = Math.Max(20,
                    Math.Min(400, _settings.GetValue("treeView.Columns." + column.Header + ".Width", column.Width)));
            }

            _root = new ComputerNode(_settings);

            treeView.NodeAdded += (sender, e) =>
            {
                if (e.Node.Tag is IExpandPersistNode persistNode)
                {
                    e.Node.IsExpanded = persistNode.Expanded;
                }
                else
                {
                    e.Node.IsExpanded = true;
                }
            };

            TreeModel treeViewModel = new TreeModel(_root);

            treeView.Model = treeViewModel;

            _computer = new Computer(_settings);

            _systemTray = new SystemTray(_computer, _settings, _unitManager);
            _systemTray.HideShowCommand += HideShowClick;
            _systemTray.ExitCommand += ExitClick;

            bool externalMenuVisible = false;

            if (Software.OperatingSystem.IsUnix)
            {
                // Unix
                treeView.RowHeight = Math.Max(treeView.RowHeight, 18);

                splitContainer.BorderStyle = BorderStyle.None;
                splitContainer.Border3DStyle = Border3DStyle.Adjust;
                splitContainer.SplitterWidth = 4;
                treeView.BorderStyle = BorderStyle.Fixed3D;
                _plotPanel.BorderStyle = BorderStyle.Fixed3D;
                gadgetMenuItem.Visible = false;
                minCloseMenuItem.Visible = false;
                minTrayMenuItem.Visible = false;
                startMinMenuItem.Visible = false;
            }
            else
            { 
                // Windows
                treeView.RowHeight = Math.Max(treeView.Font.Height + 1, 18);

                _gadget = new SensorGadget(_computer, _settings, _unitManager);
                _gadget.HideShowCommand += HideShowClick;

                _wmiProvider = new WmiProvider(_computer);

                _wmiService = new UserOption("wmiMenuItem", false, wmiMenuItem, _settings);
                _wmiService.Changed += (sender, a) =>
                {
                    if (_wmiService.Value)
                        _wmiProvider.Start();
                    else
                        _wmiProvider.Stop();
                };

                externalMenuVisible = true;
            }

            _rtssAdapter = new RtssAdapter(_settings, _unitManager);

            if (_rtssAdapter.PlatformNotSupported)
            {
                rtssMenuItem.Visible = false;
            }
            else
            {
                externalMenuVisible = true;
            }

            _runRtssService = new UserOption("rtssMenuItemRun", false, rtssMenuItemRun, _settings);
            _runRtssService.Changed += delegate
            {
                if (_runRtssService.Value)
                    _rtssAdapter.Start();
                else
                    _rtssAdapter.Stop();
            };

            _logger = new Logger(_computer);

            _plotColorPalette = new Color[13];
            _plotColorPalette[0] = Color.Blue;
            _plotColorPalette[1] = Color.OrangeRed;
            _plotColorPalette[2] = Color.Green;
            _plotColorPalette[3] = Color.LightSeaGreen;
            _plotColorPalette[4] = Color.Goldenrod;
            _plotColorPalette[5] = Color.DarkViolet;
            _plotColorPalette[6] = Color.YellowGreen;
            _plotColorPalette[7] = Color.SaddleBrown;
            _plotColorPalette[8] = Color.RoyalBlue;
            _plotColorPalette[9] = Color.DeepPink;
            _plotColorPalette[10] = Color.MediumSeaGreen;
            _plotColorPalette[11] = Color.Olive;
            _plotColorPalette[12] = Color.Firebrick;

            _computer.HardwareAdded += HardwareAdded;
            _computer.HardwareRemoved += HardwareRemoved;
            _computer.Open();

            timer.Enabled = true;

            UserOption showHiddenSensors = new UserOption("hiddenMenuItem", false, hiddenMenuItem, _settings);
            showHiddenSensors.Changed += delegate
            {
                treeViewModel.ForceVisible = showHiddenSensors.Value;
            };

            UserOption showValue = new UserOption("valueMenuItem", true, valueMenuItem, _settings);
            showValue.Changed += delegate
            {
                value.IsVisible = showValue.Value;
            };

            UserOption showMin = new UserOption("minMenuItem", false, minMenuItem, _settings);
            showMin.Changed += delegate
            {
                min.IsVisible = showMin.Value;
            };

            UserOption showMax = new UserOption("maxMenuItem", true, maxMenuItem, _settings);
            showMax.Changed += delegate
            {
                max.IsVisible = showMax.Value;
            };

            var _ = new UserOption("startMinMenuItem", false, startMinMenuItem, _settings);
            _minimizeToTray = new UserOption("minTrayMenuItem", true, minTrayMenuItem, _settings);
            _minimizeToTray.Changed += delegate
            {
                _systemTray.IsMainIconEnabled = _minimizeToTray.Value;
            };

            _minimizeOnClose = new UserOption("minCloseMenuItem", false, minCloseMenuItem, _settings);

            _autoStart = new UserOption(null, _startupManager.Startup, startupMenuItem, _settings);
            _autoStart.Changed += delegate
            {
                try
                {
                    _startupManager.Startup = _autoStart.Value;
                }
                catch (InvalidOperationException)
                {
                    MessageBox.Show("Updating the auto-startup option failed.", "Error",
                      MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _autoStart.Value = _startupManager.Startup;
                }
            };

            _readMainboardSensors = new UserOption("mainboardMenuItem", true, mainboardMenuItem, _settings);
            _readMainboardSensors.Changed += delegate
            {
                _computer.IsMotherboardEnabled = _readMainboardSensors.Value;
            };

            _readCpuSensors = new UserOption("cpuMenuItem", true, cpuMenuItem, _settings);
            _readCpuSensors.Changed += delegate
            {
                _computer.IsCpuEnabled = _readCpuSensors.Value;
            };

            _readRamSensors = new UserOption("ramMenuItem", true, ramMenuItem, _settings);
            _readRamSensors.Changed += delegate
            {
                _computer.IsMemoryEnabled = _readRamSensors.Value;
            };

            _readGpuSensors = new UserOption("gpuMenuItem", true, gpuMenuItem, _settings);
            _readGpuSensors.Changed += delegate
            {
                _computer.IsGpuEnabled = _readGpuSensors.Value;
            };

            _readFanControllersSensors = new UserOption("fanControllerMenuItem", true, fanControllerMenuItem, _settings);
            _readFanControllersSensors.Changed += delegate
            {
                _computer.IsControllerEnabled = _readFanControllersSensors.Value;
            };

            _readHddSensors = new UserOption("hddMenuItem", true, hddMenuItem, _settings);
            _readHddSensors.Changed += delegate
            {
                _computer.IsStorageEnabled = _readHddSensors.Value;
            };

            _readNicSensors = new UserOption("nicMenuItem", true, nicMenuItem, _settings);
            _readNicSensors.Changed += delegate
            {
                _computer.IsNetworkEnabled = _readNicSensors.Value;
            };

            _showGadget = new UserOption("gadgetMenuItem", false, gadgetMenuItem, _settings);
            _showGadget.Changed += delegate
            {
                if (_gadget != null)
                    _gadget.Visible = _showGadget.Value;
            };

            celsiusMenuItem.Checked = _unitManager.TemperatureUnit != TemperatureUnit.Fahrenheit;
            fahrenheitMenuItem.Checked = !celsiusMenuItem.Checked;

            _server = new HttpServer(_root, _settings.GetValue("listenerPort", 8085), _unitManager);

            if (_server.PlatformNotSupported)
            {
                webMenuItem.Visible = false;
            }
            else
            {
                externalMenuVisible = true;
            }

            _runWebServer = new UserOption("runWebServerMenuItem", false, runWebServerMenuItem, _settings);
            _runWebServer.Changed += delegate
            {
                if (_runWebServer.Value)
                    _server.StartHttpListener();
                else
                    _server.StopHttpListener();
            };

            _logSensors = new UserOption("logSensorsMenuItem", false, logSensorsMenuItem, _settings);

            _loggingInterval = new UserRadioGroup("loggingInterval", 0,
                new[] { log1sMenuItem, log2sMenuItem, log5sMenuItem, log10sMenuItem,
                log30sMenuItem, log1minMenuItem, log2minMenuItem, log5minMenuItem,
                log10minMenuItem, log30minMenuItem, log1hMenuItem, log2hMenuItem,
                log6hMenuItem}, _settings);

            _loggingInterval.Changed += (sender, e) =>
            {
                switch (_loggingInterval.Value)
                {
                    case 0: _logger.LoggingInterval = new TimeSpan(0, 0, 1); break;
                    case 1: _logger.LoggingInterval = new TimeSpan(0, 0, 2); break;
                    case 2: _logger.LoggingInterval = new TimeSpan(0, 0, 5); break;
                    case 3: _logger.LoggingInterval = new TimeSpan(0, 0, 10); break;
                    case 4: _logger.LoggingInterval = new TimeSpan(0, 0, 30); break;
                    case 5: _logger.LoggingInterval = new TimeSpan(0, 1, 0); break;
                    case 6: _logger.LoggingInterval = new TimeSpan(0, 2, 0); break;
                    case 7: _logger.LoggingInterval = new TimeSpan(0, 5, 0); break;
                    case 8: _logger.LoggingInterval = new TimeSpan(0, 10, 0); break;
                    case 9: _logger.LoggingInterval = new TimeSpan(0, 30, 0); break;
                    case 10: _logger.LoggingInterval = new TimeSpan(1, 0, 0); break;
                    case 11: _logger.LoggingInterval = new TimeSpan(2, 0, 0); break;
                    case 12: _logger.LoggingInterval = new TimeSpan(6, 0, 0); break;
                }
            };

            _sensorValuesTimeWindow = new UserRadioGroup("sensorValuesTimeWindow", 10,
                new[] { timeWindow30sMenuItem, timeWindow1minMenuItem, timeWindow2minMenuItem,
                timeWindow5minMenuItem, timeWindow10minMenuItem, timeWindow30minMenuItem,
                timeWindow1hMenuItem, timeWindow2hMenuItem, timeWindow6hMenuItem,
                timeWindow12hMenuItem, timeWindow24hMenuItem}, _settings);

            _sensorValuesTimeWindow.Changed += (sender, e) =>
            {
                TimeSpan timeWindow = TimeSpan.Zero;
                switch (_sensorValuesTimeWindow.Value)
                {
                    case 0: timeWindow = new TimeSpan(0, 0, 30); break;
                    case 1: timeWindow = new TimeSpan(0, 1, 0); break;
                    case 2: timeWindow = new TimeSpan(0, 2, 0); break;
                    case 3: timeWindow = new TimeSpan(0, 5, 0); break;
                    case 4: timeWindow = new TimeSpan(0, 10, 0); break;
                    case 5: timeWindow = new TimeSpan(0, 30, 0); break;
                    case 6: timeWindow = new TimeSpan(1, 0, 0); break;
                    case 7: timeWindow = new TimeSpan(2, 0, 0); break;
                    case 8: timeWindow = new TimeSpan(6, 0, 0); break;
                    case 9: timeWindow = new TimeSpan(12, 0, 0); break;
                    case 10: timeWindow = new TimeSpan(24, 0, 0); break;
                }

                _computer.Accept(new SensorVisitor(delegate (ISensor sensor)
                {
                    sensor.ValuesTimeWindow = timeWindow;
                }));
            };

            externalMenuItemSeparator.Visible = externalMenuVisible;

            InitializePlotForm();
            InitializeSplitter();

            startupMenuItem.Visible = _startupManager.IsAvailable;

            if (startMinMenuItem.Checked)
            {
                if (!minTrayMenuItem.Checked)
                {
                    WindowState = FormWindowState.Minimized;
                    Show();
                }
            }
            else
            {
                Show();
            }

            _globalHotkey = new GlobalHotkey(_settings);
            _globalHotkey.SetHotKeys(GetGlobalHotKeys());
            _globalHotkey.Start();

            // Create a handle, otherwise calling Close() does not fire FormClosed

            // Make sure the settings are saved when the user logs off
            Microsoft.Win32.SystemEvents.SessionEnded += delegate { SessionEnd(); };

            KeyPreview = true;
            KeyDown += (sender, e) =>
            {
                if (e.KeyData == Keys.Escape)
                {
                    SysTrayHideShow();
                }
            };

            Microsoft.Win32.SystemEvents.PowerModeChanged += PowerModeChanged;
        }

        private void PowerModeChanged(object sender, Microsoft.Win32.PowerModeChangedEventArgs eventArgs)
        {

            if (eventArgs.Mode == Microsoft.Win32.PowerModes.Resume)
            {
                _computer.Reset();
            }
        }

        private IEnumerable<(string, Action)> GetGlobalHotKeys()
        {
            yield return ("ShowHideHotKey", () => { SysTrayHideShow(); });
            yield return ("RtssHotKey", () => { _runRtssService.Value = !_runRtssService.Value; });
        }

        private void SessionEnd()
        {
            SaveCollapsedNodeState(treeView);

            _rtssAdapter.Stop();

            _root.SaveSortOrder();

            _wmiProvider.Dispose();

            _computer.Close();

            SaveConfiguration();

            if (_runWebServer.Value)
            {
                _server.Quit();
            }
        }

        private void InitializeSplitter()
        {
            splitContainer.SplitterDistance = _settings.GetValue("splitContainer.SplitterDistance", 400);
            splitContainer.SplitterMoved += delegate
            {
                _settings.SetValue("splitContainer.SplitterDistance", splitContainer.SplitterDistance);
            };
        }

        private void InitializePlotForm()
        {
            _plotForm.Bounds = new Rectangle
            {
                X = _settings.GetValue("plotForm.Location.X", -100000),
                Y = _settings.GetValue("plotForm.Location.Y", 100),
                Width = _settings.GetValue("plotForm.Width", 600),
                Height = _settings.GetValue("plotForm.Height", 400)
            };

            _showPlot.Changed += delegate
            {
                if (_plotLocation.Value == 0)
                {
                    if (_showPlot.Value && Visible)
                        _plotForm.Show();
                    else
                        _plotForm.Hide();
                }
                else
                {
                    splitContainer.Panel2Collapsed = !_showPlot.Value;
                }
                treeView.Invalidate();
            };
            _plotLocation.Changed += delegate
            {
                switch (_plotLocation.Value)
                {
                    case 0:
                        splitContainer.Panel2.Controls.Clear();
                        splitContainer.Panel2Collapsed = true;
                        _plotForm.Controls.Add(_plotPanel);
                        if (_showPlot.Value && Visible)
                            _plotForm.Show();
                        break;
                    case 1:
                        _plotForm.Controls.Clear();
                        _plotForm.Hide();
                        splitContainer.Orientation = Orientation.Horizontal;
                        splitContainer.Panel2.Controls.Add(_plotPanel);
                        splitContainer.Panel2Collapsed = !_showPlot.Value;
                        break;
                    case 2:
                        _plotForm.Controls.Clear();
                        _plotForm.Hide();
                        splitContainer.Orientation = Orientation.Vertical;
                        splitContainer.Panel2.Controls.Add(_plotPanel);
                        splitContainer.Panel2Collapsed = !_showPlot.Value;
                        break;
                }
            };

            _plotForm.FormClosing += delegate (object sender, FormClosingEventArgs e)
            {
                if (e.CloseReason == CloseReason.UserClosing)
                {
                    // just switch off the plotting when the user closes the form
                    if (_plotLocation.Value == 0)
                    {
                        _showPlot.Value = false;
                    }
                    e.Cancel = true;
                }
            };


            void MoveOrResizePlotForm(object sender, EventArgs e)
            {
                if (_plotForm.WindowState != FormWindowState.Minimized)
                {
                    _settings.SetValue("plotForm.Location.X", _plotForm.Bounds.X);
                    _settings.SetValue("plotForm.Location.Y", _plotForm.Bounds.Y);
                    _settings.SetValue("plotForm.Width", _plotForm.Bounds.Width);
                    _settings.SetValue("plotForm.Height", _plotForm.Bounds.Height);
                }
            }

            _plotForm.Move += MoveOrResizePlotForm;
            _plotForm.Resize += MoveOrResizePlotForm;

            _plotForm.VisibleChanged += delegate
            {
                Rectangle bounds = new Rectangle(_plotForm.Location, _plotForm.Size);
                Screen screen = Screen.FromRectangle(bounds);
                Rectangle intersection = Rectangle.Intersect(screen.WorkingArea, bounds);

                if (intersection.Width < Math.Min(16, bounds.Width) ||
                    intersection.Height < Math.Min(16, bounds.Height))
                {
                    _plotForm.Location = new Point(
                      screen.WorkingArea.Width / 2 - bounds.Width / 2,
                      screen.WorkingArea.Height / 2 - bounds.Height / 2);
                }
            };

            VisibleChanged += delegate
            {
                if (Visible && _showPlot.Value && _plotLocation.Value == 0)
                    _plotForm.Show();
                else
                    _plotForm.Hide();
            };
        }

        private void SubHardwareAdded(IHardware hardware, Node node)
        {
            HardwareNode hardwareNode = new HardwareNode(hardware, _settings, _unitManager);
            hardwareNode.PlotSelectionChanged += PlotSelectionChanged;
            node.AddChild(hardwareNode);
            foreach (IHardware subHardware in hardware.SubHardware)
                SubHardwareAdded(subHardware, hardwareNode);
        }

        private void HardwareAdded(IHardware hardware)
        {
            SubHardwareAdded(hardware, _root);

            PlotSelectionChanged(this, null);
        }

        private void HardwareRemoved(IHardware hardware)
        {
            List<HardwareNode> nodesToRemove = new List<HardwareNode>();

            foreach (Node node in _root.Nodes)
            {
                if (node is HardwareNode hardwareNode)
                {
                    if (hardwareNode.Hardware == hardware)
                    {
                        nodesToRemove.Add(hardwareNode);
                    }
                }
            }

            foreach (HardwareNode hardwareNode in nodesToRemove)
            {
                _root.RemoveChild(hardwareNode);

                hardwareNode.PlotSelectionChanged -= PlotSelectionChanged;
            }

            PlotSelectionChanged(this, null);
        }

        private void NodeTextBoxText_DrawText(object sender, DrawEventArgs e)
        {
            if (e.Node.Tag is Node node)
            {
                if (node.IsVisible)
                {
                    if (plotMenuItem.Checked && node is SensorNode sensorNode && _sensorPlotColors.TryGetValue(sensorNode.Sensor, out Color color))
                        e.TextColor = color;
                }
                else
                    e.TextColor = Color.DarkGray;
            }
        }

        private void PlotSelectionChanged(object sender, EventArgs e)
        {
            List<ISensor> selected = new List<ISensor>();
            IDictionary<ISensor, Color> colors = new Dictionary<ISensor, Color>();
            int colorIndex = 0;

            foreach (TreeNodeAdv node in treeView.AllNodes)
            {
                if (node.Tag is SensorNode sensorNode)
                {
                    if (sensorNode.Plot)
                    {
                        if (!sensorNode.PenColor.HasValue)
                        {
                            colors.Add(sensorNode.Sensor,
                              _plotColorPalette[colorIndex % _plotColorPalette.Length]);
                        }
                        selected.Add(sensorNode.Sensor);
                    }
                    colorIndex++;
                }
            }

            // if a sensor is assigned a color that's already being used by another
            // sensor, try to assign it a new color. This is done only after the
            // previous loop sets an unchanging default color for all sensors, so that
            // colors jump around as little as possible as sensors get added/removed
            // from the plot
            var usedColors = new List<Color>();
            foreach (ISensor curSelectedSensor in selected)
            {
                if (!colors.ContainsKey(curSelectedSensor))
                    continue;

                Color curColor = colors[curSelectedSensor];
                if (usedColors.Contains(curColor))
                {
                    foreach (Color potentialNewColor in _plotColorPalette)
                    {
                        if (!colors.Values.Contains(potentialNewColor))
                        {
                            colors[curSelectedSensor] = potentialNewColor;
                            usedColors.Add(potentialNewColor);
                            break;
                        }
                    }
                }
                else
                {
                    usedColors.Add(curColor);
                }
            }

            foreach (TreeNodeAdv node in treeView.AllNodes)
            {
                if (node.Tag is SensorNode sensorNode && sensorNode.Plot && sensorNode.PenColor.HasValue)
                    colors.Add(sensorNode.Sensor, sensorNode.PenColor.Value);
            }

            _sensorPlotColors = colors;
            _plotPanel.SetSensors(selected, colors);

            // move to other place 
            _rtssAdapter.SetSensors(selected, colors);
        }

        private void NodeTextBoxText_EditorShowing(object sender, CancelEventArgs e)
        {
            e.Cancel = !(treeView.CurrentNode != null &&
              (treeView.CurrentNode.Tag is SensorNode ||
               treeView.CurrentNode.Tag is HardwareNode));
        }

        private void ExitClick(object sender, EventArgs e)
        {
            CloseApplication();
        }

        private void TimerTick(object sender, EventArgs e)
        {
            if (_delayCount < 4)
            {
                _delayCount++;
                return;
            }

            _computer.Accept(_updateVisitor);

            if (Visible)
            {
                treeView.Invalidate();
                _plotPanel.InvalidateData();
            }

            _rtssAdapter.InvalidateData();

            _systemTray.Redraw();

            if (_gadget != null && _gadget.SensorsCount > 0)
                _gadget?.Redraw();

            _wmiProvider?.Update();

            if (_logSensors.Value)
                _logger.Log();
        }

        private void SaveConfiguration()
        {
            if (_settings == null)
                return;

            _globalHotkey.SaveCurrentSettings();

            _plotPanel.SaveCurrentSettings();

            foreach (TreeColumn column in treeView.Columns)
                _settings.SetValue("treeView.Columns." + column.Header + ".Width", column.Width);

            _settings.SetValue("listenerPort", _server.ListenerPort);

            string fileName = Path.ChangeExtension(Application.ExecutablePath, ".config");

            try
            {
                _settings.Save(fileName);
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("Access to the path '" + fileName + "' is denied. " +
                  "The current settings could not be saved.",
                  "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (IOException)
            {
                MessageBox.Show("The path '" + fileName + "' is not writeable. " +
                  "The current settings could not be saved.",
                  "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Rectangle newBounds = new Rectangle
            {
                X = _settings.GetValue("mainForm.Location.X", Location.X),
                Y = _settings.GetValue("mainForm.Location.Y", Location.Y),
                Width = _settings.GetValue("mainForm.Width", 470),
                Height = _settings.GetValue("mainForm.Height", 640)
            };

            Rectangle fullWorkingArea = new Rectangle(int.MaxValue, int.MaxValue, int.MinValue, int.MinValue);

            foreach (Screen screen in Screen.AllScreens)
                fullWorkingArea = Rectangle.Union(fullWorkingArea, screen.Bounds);

            Rectangle intersection = Rectangle.Intersect(fullWorkingArea, newBounds);
            if (intersection.Width < 20 || intersection.Height < 20 || !_settings.Contains("mainForm.Location.X"))
            {
                newBounds.X = (Screen.PrimaryScreen.WorkingArea.Width / 2) - (newBounds.Width / 2);
                newBounds.Y = (Screen.PrimaryScreen.WorkingArea.Height / 2) - (newBounds.Height / 2);
            }
            Bounds = newBounds;

            FormClosed += MainForm_FormClosed;
        }

        private void SaveCollapsedNodeState(TreeViewAdv treeViewAdv)
        {
            var collapsedHwNodes = treeViewAdv.AllNodes
                .Where(n => n.Tag is IExpandPersistNode expandPersistNode && expandPersistNode.Expanded != n.IsExpanded)
                .OrderByDescending(n => n.Level)
                .ToList();

            foreach (TreeNodeAdv node in collapsedHwNodes)
            {
                var expandPersistNode = (IExpandPersistNode)node.Tag;
                expandPersistNode.Expanded = node.IsExpanded;
            }
        }

        private void CloseApplication()
        {
            FormClosed -= MainForm_FormClosed;

            Visible = false;
            _systemTray.IsMainIconEnabled = false;

            timer.Enabled = false;

            SessionEnd();

            _globalHotkey.Stop();
            _systemTray.Dispose();

            Application.Exit();
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            CloseApplication();
        }

        private void TreeView_Click(object sender, TreeNodeAdvMouseEventArgs e)
        {
            if (!(e is TreeNodeAdvMouseEventArgs m) || (m.Button != MouseButtons.Right)) // m.Button != MouseButtons.Left && 
                return;

            TreeNodeAdv treeViewSelectedNode = e.Node;

            treeView.SelectedNode = treeViewSelectedNode;

            if (treeViewSelectedNode != null)
            {
                if (treeViewSelectedNode.Tag is SensorNode node && node.Sensor != null)
                {
                    treeContextMenu.MenuItems.Clear();
                    if (node.Sensor.Parameters.Count > 0)
                    {
                        MenuItem item = new MenuItem("Parameters...");
                        item.Click += delegate
                        {
                            ShowParameterForm(node);
                        };
                        treeContextMenu.MenuItems.Add(item);
                    }
                    if (nodeTextBoxText.EditEnabled)
                    {
                        MenuItem item = new MenuItem("Rename");
                        item.Click += delegate
                        {
                            nodeTextBoxText.BeginEdit();
                        };
                        treeContextMenu.MenuItems.Add(item);
                    }
                    if (node.IsVisible)
                    {
                        MenuItem item = new MenuItem("Hide");
                        item.Click += delegate
                        {
                            node.IsVisible = false;
                        };
                        treeContextMenu.MenuItems.Add(item);
                    }
                    else
                    {
                        MenuItem item = new MenuItem("Unhide");
                        item.Click += delegate
                        {
                            node.IsVisible = true;
                        };
                        treeContextMenu.MenuItems.Add(item);
                    }
                    treeContextMenu.MenuItems.Add(new MenuItem("-"));
                    {
                        MenuItem item = new MenuItem("Pen Color...");
                        item.Click += delegate
                        {
                            ShowColorForm(node);
                        };
                        treeContextMenu.MenuItems.Add(item);
                    }
                    {
                        MenuItem item = new MenuItem("Reset Pen Color");
                        item.Click += delegate
                        {
                            node.PenColor = null;
                        };
                        treeContextMenu.MenuItems.Add(item);
                    }
                    treeContextMenu.MenuItems.Add(new MenuItem("-"));
                    {
                        MenuItem item = new MenuItem("Show in Plot") { Checked = node.Plot };
                        item.Click += delegate { node.Plot = !item.Checked; };
                        treeContextMenu.MenuItems.Add(item);
                    }
                    {
                        MenuItem item = new MenuItem("Show in Tray") { Checked = _systemTray.Contains(node.Sensor) };
                        item.Click += delegate
                        {
                            if (item.Checked)
                                _systemTray.Remove(node.Sensor);
                            else
                                _systemTray.Add(node.Sensor, true);
                        };
                        treeContextMenu.MenuItems.Add(item);
                    }
                    if (_gadget != null)
                    {
                        MenuItem item = new MenuItem("Show in Gadget") { Checked = _gadget.Contains(node.Sensor) };
                        item.Click += delegate
                        {
                            if (item.Checked)
                            {
                                _gadget.Remove(node.Sensor);
                            }
                            else
                            {
                                _gadget.Add(node.Sensor);
                            }
                        };
                        treeContextMenu.MenuItems.Add(item);
                    }
                    if (node.Sensor.Control != null)
                    {
                        treeContextMenu.MenuItems.Add(new MenuItem("-"));
                        IControl control = node.Sensor.Control;
                        MenuItem controlItem = new MenuItem("Control");
                        MenuItem defaultItem = new MenuItem("Default") { Checked = control.ControlMode == ControlMode.Default };
                        controlItem.MenuItems.Add(defaultItem);
                        defaultItem.Click += delegate
                        {
                            control.SetDefault();
                        };
                        MenuItem manualItem = new MenuItem("Manual");
                        controlItem.MenuItems.Add(manualItem);
                        manualItem.Checked = control.ControlMode == ControlMode.Software;
                        for (int i = 0; i <= 100; i += 5)
                        {
                            if (i <= control.MaxSoftwareValue &&
                                i >= control.MinSoftwareValue)
                            {
                                MenuItem item = new MenuItem(i + " %") { RadioCheck = true };
                                manualItem.MenuItems.Add(item);
                                item.Checked = control.ControlMode == ControlMode.Software && (int)Math.Round(control.SoftwareValue) == i;
                                int softwareValue = i;
                                item.Click += delegate
                                {
                                    control.SetSoftware(softwareValue);
                                };
                            }
                        }
                        treeContextMenu.MenuItems.Add(controlItem);
                    }

                    treeContextMenu.Show(treeView, new Point(m.X, m.Y));
                }

                if (treeViewSelectedNode.Tag is HardwareNode hardwareNode && hardwareNode.Hardware != null)
                {
                    treeContextMenu.MenuItems.Clear();

                    if (nodeTextBoxText.EditEnabled)
                    {
                        MenuItem item = new MenuItem("Rename");
                        item.Click += delegate
                        {
                            nodeTextBoxText.BeginEdit();
                        };
                        treeContextMenu.MenuItems.Add(item);
                    }
                    if (hardwareNode.IsVisible)
                    {
                        MenuItem item = new MenuItem("Hide");
                        item.Click += delegate
                        {
                            hardwareNode.IsVisible = false;
                        };
                        treeContextMenu.MenuItems.Add(item);
                    }
                    else
                    {
                        MenuItem item = new MenuItem("Unhide");
                        item.Click += delegate
                        {
                            hardwareNode.IsVisible = true;
                        };
                        treeContextMenu.MenuItems.Add(item);
                    }

                    if (hardwareNode.Parent == _root)
                    {
                        treeContextMenu.MenuItems.Add(new MenuItem("-"));

                        MenuItem itemUp = new MenuItem("Move up");
                        itemUp.Click += delegate
                        {
                            SaveCollapsedNodeState(treeView);
                            _root.Move(hardwareNode, false);
                        };
                        treeContextMenu.MenuItems.Add(itemUp);

                        MenuItem itemDown = new MenuItem("Move down");
                        itemDown.Click += delegate
                        {
                            SaveCollapsedNodeState(treeView);
                            _root.Move(hardwareNode, true);
                        };
                        treeContextMenu.MenuItems.Add(itemDown);
                    }

                    treeContextMenu.Show(treeView, new Point(m.X, m.Y));
                }
            }
        }

        private void SaveReportMenuItem_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.RestoreDirectory = true;
                saveFileDialog.DefaultExt = "txt";
                saveFileDialog.FileName = "LibreHardwareMonitor.Report.txt";
                saveFileDialog.Filter = "Text Documents|*.txt|All Files|*.*";
                saveFileDialog.Title = "Save Report As";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    using (TextWriter w = new StreamWriter(saveFileDialog.FileName))
                    {
                        w.Write(_computer.GetReport());
                    }
                }
            }
        }

        private void SysTrayHideShow()
        {
            Visible = !Visible;

            if (Visible)
            {
                Activate();

                treeView.Invalidate();
                _plotPanel.InvalidateData();
            }
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_SYSCOMMAND = 0x112;
            const int SC_MINIMIZE = 0xF020;
            const int SC_CLOSE = 0xF060;

            if (_minimizeToTray.Value && m.Msg == WM_SYSCOMMAND && m.WParam.ToInt64() == SC_MINIMIZE)
            {
                SysTrayHideShow();
            }
            else if (_minimizeOnClose.Value && m.Msg == WM_SYSCOMMAND && m.WParam.ToInt64() == SC_CLOSE)
            {

                //Apparently the user wants to minimize rather than close
                //Now we still need to check if we're going to the tray or not
                //Note: the correct way to do this would be to send out SC_MINIMIZE,
                //but since the code here is so simple,
                //that would just be a waste of time.
                if (_minimizeToTray.Value)
                    SysTrayHideShow();
                else
                    WindowState = FormWindowState.Minimized;
            }
            else
            {
                base.WndProc(ref m);
            }
        }

        private void HideShowClick(object sender, EventArgs e)
        {
            SysTrayHideShow();
        }

        private void ShowColorForm(SensorNode node)
        {
            using (ColorDialog dialog = new ColorDialog())
            {
                dialog.Color = node.PenColor.GetValueOrDefault();

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    node.PenColor = dialog.Color;
                }
            }
        }

        private void ShowParameterForm(SensorNode sensorNode)
        {
            using (ParameterForm form = new ParameterForm())
            {
                form.Caption= sensorNode.Text;
                form.SetParameters(sensorNode.Sensor.Parameters);

                form.ShowDialog();
            }
        }

        private void TreeView_NodeMouseDoubleClick(object sender, TreeNodeAdvMouseEventArgs e)
        {
            if (e.Node.Tag is SensorNode node && node.Sensor != null && node.Sensor.Parameters.Count > 0)
            {
                ShowParameterForm(node);
            }
        }

        private void CelsiusMenuItem_Click(object sender, EventArgs e)
        {
            celsiusMenuItem.Checked = true;
            fahrenheitMenuItem.Checked = false;
            _unitManager.TemperatureUnit = TemperatureUnit.Celsius;
        }

        private void FahrenheitMenuItem_Click(object sender, EventArgs e)
        {
            celsiusMenuItem.Checked = false;
            fahrenheitMenuItem.Checked = true;
            _unitManager.TemperatureUnit = TemperatureUnit.Fahrenheit;
        }

        private void ResetMinMaxMenuItem_Click(object sender, EventArgs e)
        {
            _computer.Accept(new SensorVisitor(delegate (ISensor sensor)
            {
                sensor.ResetMin();
                sensor.ResetMax();
            }));
        }

        private void MainForm_MoveOrResize(object sender, EventArgs e)
        {
            if (WindowState != FormWindowState.Minimized)
            {
                _settings.SetValue("mainForm.Location.X", Bounds.X);
                _settings.SetValue("mainForm.Location.Y", Bounds.Y);
                _settings.SetValue("mainForm.Width", Bounds.Width);
                _settings.SetValue("mainForm.Height", Bounds.Height);
            }
        }

        private void ResetClick(object sender, EventArgs e)
        {
            // disable the fallback MainIcon during reset, otherwise icon visibility
            // might be lost
            _systemTray.IsMainIconEnabled = false;
            _computer.Reset();
            // restore the MainIcon setting
            _systemTray.IsMainIconEnabled = _minimizeToTray.Value;
        }

        private void TreeView_MouseMove(object sender, MouseEventArgs e)
        {
            _selectionDragging &= (e.Button & (MouseButtons.Left | MouseButtons.Right)) > 0;
            if (_selectionDragging)
                treeView.SelectedNode = treeView.GetNodeAt(e.Location);
        }

        private void TreeView_MouseDown(object sender, MouseEventArgs e)
        {
            _selectionDragging = true;
        }

        private void TreeView_MouseUp(object sender, MouseEventArgs e)
        {
            _selectionDragging = false;
        }

        private void ServerOptionsMenuItem_Click(object sender, EventArgs e)
        {
            ShowDialogAndDispose(new PortForm(_server));
        }

        private void RtssMenuItemOptions_Click(object sender, EventArgs e)
        {
            ShowDialogAndDispose(new RtssOptionForm(_rtssAdapter));
        }

        private void GlobalHotKeyMenuItem_Click(object sender, EventArgs e)
        {
            ShowDialogAndDispose(new GlobalHotkeyForm(_globalHotkey));
        }

        private void AboutMenuItem_Click(object sender, EventArgs e)
        {
            ShowDialogAndDispose(new AboutBox());
        }

        private static void ShowDialogAndDispose(Form form)
        {
            using (form)
            {
                form.ShowDialog();
            }
        }
    }
}
