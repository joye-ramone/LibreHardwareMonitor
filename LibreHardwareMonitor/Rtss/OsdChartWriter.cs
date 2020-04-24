using System.Collections.Generic;
using RTSSSharedMemoryNET;

namespace LibreHardwareMonitor.Rtss
{
    public sealed class OsdChartWriter
    {
        private readonly int _dwWidth;
        private readonly int _dwHeight;
        private readonly int _dwMargin;

        private uint _numberOfCharts;
        private uint _historyLength;

        private readonly List<float[]> _buffers = new List<float[]>();

        private uint _currentPosition;
        private uint _currentOffset;

        public OsdChartWriter(int dwWidth, int dwHeight, int dwMargin)
        {
            _dwWidth = dwWidth;
            _dwHeight = dwHeight;
            _dwMargin = dwMargin;
        }

        public void Init(uint numberOfCharts, uint historyLength)
        {
            // set position to 0

            _numberOfCharts = numberOfCharts;
            _historyLength = historyLength;

            _buffers.Clear();

            for (int i = 0; i < _numberOfCharts; i++)
            {
                _buffers.Add(new float[_historyLength]);
            }

            _currentPosition = 0;
        }

        public void Begin()
        {
            // set dwObjectOffset = 0
            // set dwObjectSize = 0

            _currentOffset = 0;
        }

        public string Append(OSD osd, int chartNumber, float value, float min, float max, OSD.EMBEDDED_OBJECT_GRAPH dwFlags = 0)
        {
            float[] data = _buffers[chartNumber];

            data[_currentPosition] = value;

            uint localNext = (_currentPosition + 1) & (_historyLength - 1);

            uint dwObjectSize = osd.EmbedGraph(_currentOffset, data, localNext, _dwWidth, _dwHeight, _dwMargin, min, max, dwFlags);

            if (dwObjectSize > 0)
            {
                //print embedded object
                string strObj = $"<OBJ={_currentOffset:X8}>";

                //modify object offset
                _currentOffset += dwObjectSize;

                return strObj;
            }

            return string.Empty;
        }

        public void End()
        {
            // set position + 1

            _currentPosition = (_currentPosition + 1) & (_historyLength - 1);
        }
    }
}
