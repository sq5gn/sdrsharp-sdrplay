namespace SDRSharp.SDRplay
{
    // LNA gain reduction of the RSPduo by frequency range and LNAstate, 50 ohm ports and the tuner 1 Hi-Z port.
    // SDRplay API Specification 3.15, section 5 "Gain Reduction Tables", RSPduo.
    public static class RspDuoGainTable
    {
        private static readonly int[] Band0To60 = new int[] { 0, 6, 12, 18, 37, 42, 61 }; // dB, 0-60 MHz
        private static readonly int[] Band60To420 = new int[] { 0, 6, 12, 18, 20, 26, 32, 38, 57, 62 }; // dB, 60-420 MHz
        private static readonly int[] Band420To1000 = new int[] { 0, 7, 13, 19, 20, 27, 33, 39, 45, 64 }; // dB, 420-1000 MHz
        private static readonly int[] Band1000To2000 = new int[] { 0, 6, 12, 20, 26, 32, 38, 43, 62 }; // dB, 1000-2000 MHz
        private static readonly int[] HiZ0To60 = new int[] { 0, 6, 12, 18, 37 }; // dB, 0-60 MHz, Hi-Z port

        public const long HiZMaxFrequency = 60000000; // Hz, the Hi-Z row of the table covers 0-60 MHz

        public const int MinIFGainReduction = 20; // dB, sdrplay_api_NORMAL_MIN_GR
        public const int MaxIFGainReduction = 59; // dB, MAX_BB_GR
        public const int MinAgcSetPoint = -72; // dBFS, sample rates < 8.064 MS/s (API spec 2.5.3)
        public const int MaxAgcSetPoint = -20; // dBFS

        // Number of LNA states at this frequency (Hz); hiZ: the tuner 1 Hi-Z port is selected.
        public static int StateCount(long frequency, bool hiZ)
        {
            return Table(frequency, hiZ).Length;
        }

        // LNA gain reduction in dB for this state at this frequency; the state is clamped to the band's range.
        public static int ReductionDB(long frequency, bool hiZ, int state)
        {
            var table = Table(frequency, hiZ);
            if (state < 0)
            {
                state = 0;
            }
            if (state > table.Length - 1)
            {
                state = table.Length - 1;
            }
            return table[state];
        }

        private static int[] Table(long frequency, bool hiZ)
        {
            if (frequency < HiZMaxFrequency)
            {
                return hiZ ? HiZ0To60 : Band0To60;
            }
            if (frequency < 420000000)
            {
                return Band60To420;
            }
            if (frequency < 1000000000)
            {
                return Band420To1000;
            }
            return Band1000To2000;
        }
    }
}
