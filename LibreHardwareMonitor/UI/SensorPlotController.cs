using System.Drawing;
using System.Windows.Forms;
using OxyPlot;
using OxyPlot.Series;

namespace LibreHardwareMonitor.UI
{
    public class SensorPlotController : ControllerBase, IPlotController
    {
        static IViewCommand<OxyMouseDownEventArgs> Track = new DelegatePlotCommand<OxyMouseDownEventArgs>((view, controller, args) => controller.AddMouseManipulator(view, new FineTrackerManipulator(view) { Snap = false, PointsOnly = false }, args));
        static IViewCommand<OxyMouseDownEventArgs> SnapTrack = new DelegatePlotCommand<OxyMouseDownEventArgs>((view, controller, args) => controller.AddMouseManipulator(view, new FineTrackerManipulator(view) { Snap = true, PointsOnly = false }, args));
        static IViewCommand<OxyMouseDownEventArgs> PointsOnlyTrack = new DelegatePlotCommand<OxyMouseDownEventArgs>((view, controller, args) => controller.AddMouseManipulator(view, new FineTrackerManipulator(view) { Snap = false, PointsOnly = true }, args));

        public SensorPlotController()
        {
            // Zoom rectangle bindings: MMB / control RMB / control+alt LMB
            this.BindMouseDown(OxyMouseButton.Middle, PlotCommands.ZoomRectangle);
            this.BindMouseDown(OxyMouseButton.Right, OxyModifierKeys.Control, PlotCommands.ZoomRectangle);
            this.BindMouseDown(OxyMouseButton.Left, OxyModifierKeys.Control | OxyModifierKeys.Alt, PlotCommands.ZoomRectangle);

            // Reset bindings: Same as zoom rectangle, but double click / A key
            this.BindMouseDown(OxyMouseButton.Middle, OxyModifierKeys.None, 2, PlotCommands.ResetAt);
            this.BindMouseDown(OxyMouseButton.Right, OxyModifierKeys.Control, 2, PlotCommands.ResetAt);
            this.BindMouseDown(OxyMouseButton.Left, OxyModifierKeys.Control | OxyModifierKeys.Alt, 2, PlotCommands.ResetAt);
            this.BindKeyDown(OxyKey.A, PlotCommands.Reset);
            this.BindKeyDown(OxyKey.Home, PlotCommands.Reset);

            this.BindKeyDown(OxyKey.C, OxyModifierKeys.Control | OxyModifierKeys.Alt, PlotCommands.CopyCode);
            this.BindKeyDown(OxyKey.R, OxyModifierKeys.Control | OxyModifierKeys.Alt, PlotCommands.CopyTextReport);

            // Pan bindings: RMB / alt LMB / Up/down/left/right keys (panning direction on axis is opposite of key as it is more intuitive)
            this.BindMouseDown(OxyMouseButton.Right, PlotCommands.PanAt);
            this.BindMouseDown(OxyMouseButton.Left, OxyModifierKeys.Alt, PlotCommands.PanAt);
            this.BindKeyDown(OxyKey.Left, PlotCommands.PanLeft);
            this.BindKeyDown(OxyKey.Right, PlotCommands.PanRight);
            this.BindKeyDown(OxyKey.Up, PlotCommands.PanUp);
            this.BindKeyDown(OxyKey.Down, PlotCommands.PanDown);
            this.BindKeyDown(OxyKey.Left, OxyModifierKeys.Control, PlotCommands.PanLeftFine);
            this.BindKeyDown(OxyKey.Right, OxyModifierKeys.Control, PlotCommands.PanRightFine);
            this.BindKeyDown(OxyKey.Up, OxyModifierKeys.Control, PlotCommands.PanUpFine);
            this.BindKeyDown(OxyKey.Down, OxyModifierKeys.Control, PlotCommands.PanDownFine);

            // Tracker bindings: LMB
            this.BindMouseDown(OxyMouseButton.Left, SnapTrack);
            this.BindMouseDown(OxyMouseButton.Left, OxyModifierKeys.Control, Track);
            this.BindMouseDown(OxyMouseButton.Left, OxyModifierKeys.Shift, PointsOnlyTrack);

            // Zoom in/out binding: XB1 / XB2 / mouse wheels / +/- keys
            this.BindMouseDown(OxyMouseButton.XButton1, PlotCommands.ZoomInAt);
            this.BindMouseDown(OxyMouseButton.XButton2, PlotCommands.ZoomOutAt);
            this.BindMouseWheel(PlotCommands.ZoomWheel);
            this.BindMouseWheel(OxyModifierKeys.Control, PlotCommands.ZoomWheelFine);
        }

        private class FineTrackerManipulator : TrackerManipulator
        {
            private readonly Label _toolTip;

            private Series _currentSeries;

            public FineTrackerManipulator(IPlotView plotView) : base(plotView)
            {
                _toolTip = new Label
                {
                    Parent = (Control)plotView,
                    BackColor = SystemColors.Info,
                    ForeColor = SystemColors.InfoText,
                    BorderStyle = BorderStyle.FixedSingle,
                    AutoSize = true,
                    Padding = new Padding(2)
                };
            }

            public override void Completed(OxyMouseEventArgs e)
            {
                e.Handled = true;

                _currentSeries = null;
                HideTracker();

                PlotView.ActualModel?.RaiseTrackerChanged(null);
            }

            public override void Delta(OxyMouseEventArgs e)
            {
                e.Handled = true;

                if (_currentSeries == null || !LockToInitialSeries)
                {
                    // get the nearest
                    _currentSeries = PlotView.ActualModel?.GetSeriesFromPoint(e.Position, 20);
                }

                if (_currentSeries == null)
                {
                    if (!LockToInitialSeries)
                    {
                        HideTracker();
                    }

                    return;
                }

                var actualModel = PlotView.ActualModel;
                if (actualModel == null)
                {
                    return;
                }

                if (!actualModel.PlotArea.Contains(e.Position.X, e.Position.Y))
                {
                    return;
                }

                var result = GetNearestHit(_currentSeries, e.Position, Snap, PointsOnly);
                if (result != null)
                {
                    result.PlotModel = PlotView.ActualModel;
                    ShowTracker(result);

                    PlotView.ActualModel?.RaiseTrackerChanged(result);
                }
            }

            private void ShowTracker(TrackerHitResult data)
            {
                //PlotView.ShowTracker(result);

                //this.trackerLabel.Text = data.ToString();
                //this.trackerLabel.Top = (int)data.Position.Y - this.trackerLabel.Height;
                //this.trackerLabel.Left = (int)data.Position.X - this.trackerLabel.Width / 2;
                //this.trackerLabel.Visible = true;

                // This is for stop flickering tooltip

                var parent = (Control)PlotView;

                _toolTip.Text = data.ToString();

                int top = (int)data.Position.Y - _toolTip.Height;
                if (top < 0) top = 0;
                if (top > parent.Height - _toolTip.Height) top = parent.Height - _toolTip.Height;
                _toolTip.Top = top;

                int left = (int)data.Position.X - _toolTip.Width / 2;
                if (left < 0) left = 0;
                if (left > parent.Width - _toolTip.Width) left = parent.Width - _toolTip.Width;
                _toolTip.Left = left;

                _toolTip.Visible = true;
            }

            private void HideTracker()
            {
                //PlotView.HideTracker();
                _toolTip.Visible = false;
            }

            public override void Started(OxyMouseEventArgs e)
            {
                _currentSeries = PlotView.ActualModel?.GetSeriesFromPoint(e.Position);
                Delta(e);
            }

            private static TrackerHitResult GetNearestHit(Series series, ScreenPoint point, bool snap, bool pointsOnly)
            {
                if (series == null)
                {
                    return null;
                }

                // Check data points only
                if (snap || pointsOnly)
                {
                    var result = series.GetNearestPoint(point, false);
                    if (result != null)
                    {
                        if (result.Position.DistanceTo(point) < 20)
                        {
                            return result;
                        }
                    }
                }

                // Check between data points (if possible)
                if (!pointsOnly)
                {
                    var result = series.GetNearestPoint(point, true);
                    return result;
                }

                return null;
            }
        }
    }
}
