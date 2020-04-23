using System;
using System.Drawing;
using System.Windows.Forms;

namespace LibreHardwareMonitor.Rtss
{
    public partial class RtssOptionForm : Form
    {
        private readonly Color GoodState = Color.Green;
        private readonly Color BadState = Color.Red;

        private readonly RtssAdapter _rtssAdapter;

        private readonly string _originalServiceLocation;

        public RtssOptionForm()
        {
            InitializeComponent();

            Font = SystemFonts.MessageBoxFont;
        }

        public RtssOptionForm(RtssAdapter rtssAdapter) : this()
        {
            _rtssAdapter = rtssAdapter ?? throw new ArgumentNullException(nameof(rtssAdapter));

            txtRtssLocation.Text = _originalServiceLocation = _rtssAdapter.Service.RtssServiceLocation;

            chkGroupByType.Checked = _rtssAdapter.GroupByType;
            chkSeparateGroups.Checked = _rtssAdapter.SeparateGroups;
            chkUseSensorNameAsKey.Checked = _rtssAdapter.UseSensorNameAsKey;

            StatusTimerTick(this, EventArgs.Empty);
            LocationTextChanged(this, EventArgs.Empty);

            statusTimer.Interval = 500;
            statusTimer.Enabled = true;
        }

        private void StatusTimerTick(object sender, EventArgs e)
        {
            bool isAvailable = _rtssAdapter.IsServiceAvailable;
            bool isRunning = _rtssAdapter.IsServiceRunning;

            lblRunningState.Text = isRunning ? "Ok!" : "NO!";
            lblRunningState.ForeColor = isRunning ? GoodState : BadState;

            btnTryRun.Enabled = isAvailable && !isRunning;
        }

        private void LocationTextChanged(object sender, EventArgs e)
        {
            _rtssAdapter.Service.RtssServiceLocation = txtRtssLocation.Text;

            bool isAvailable = _rtssAdapter.IsServiceAvailable;
            bool isRunning = _rtssAdapter.IsServiceRunning;

            lblAvailableState.Text = isAvailable ? "Ok!" : "NO!";
            lblAvailableState.ForeColor = isAvailable ? GoodState : BadState;

            btnTryRun.Enabled = isAvailable && !isRunning;
        }

        private void DetectPathClick(object sender, EventArgs e)
        {
            txtRtssLocation.Text = _rtssAdapter.Service.FindDefaultLocation();
        }

        private void OptionFormClosed(object sender, FormClosedEventArgs e)
        {
            if (DialogResult == DialogResult.OK)
            {
                _rtssAdapter.Service.RtssServiceLocation = txtRtssLocation.Text;

                _rtssAdapter.GroupByType = chkGroupByType.Checked;
                _rtssAdapter.SeparateGroups = chkSeparateGroups.Checked;
                _rtssAdapter.UseSensorNameAsKey = chkUseSensorNameAsKey.Checked;
            }
            else
            {
                _rtssAdapter.Service.RtssServiceLocation = _originalServiceLocation;
            }
        }

        private void TryRunClick(object sender, EventArgs e)
        {
            _rtssAdapter.Service.TryRun();
        }
    }
}
