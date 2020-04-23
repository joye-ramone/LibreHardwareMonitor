using System;
using System.Windows.Forms;

namespace LibreHardwareMonitor.Rtss
{
    public partial class RtssOptionForm : Form
    {
        private readonly RtssAdapter _rtssAdapter;

        public RtssOptionForm()
        {
            InitializeComponent();
        }

        public RtssOptionForm(RtssAdapter rtssAdapter) : this()
        {
            _rtssAdapter = rtssAdapter ?? throw new ArgumentNullException(nameof(rtssAdapter));
        }
    }
}
