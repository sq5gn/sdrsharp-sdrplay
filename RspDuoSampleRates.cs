namespace SDRSharp.SDRplay
{
    // Sample rates offered for the RSPduo in Single Tuner. Zero IF: each rate with the widest IF filter below it.
    // Low IF: the API down-converts to baseband (API spec 3.15, sdrplay_api_Init) and delivers a lower rate than
    // the ADC; the narrow IF filter acts before the ADC. Filter values: sdrplay_api_Bw_MHzT (sdrplay_api_tuner.h).
    // All rates were measured on the device: IF 450 kHz at ADC 2 MHz gives 500 kS/s with the 200 and 300 kHz
    // filters (600 kHz gives 1 MS/s with dropouts, not offered); IF 1.620 MHz at ADC 6 MHz gives 2 MS/s.
    public static class RspDuoSampleRates
    {
        public static readonly int[] SampleRate = new int[] { 2000000, 6000000, 8000000, 10000000, 500000, 500000, 2000000 }; // S/s to SDR#
        public static readonly int[] AdcRate = new int[] { 2000000, 6000000, 8000000, 10000000, 2000000, 2000000, 6000000 }; // fsHz
        public static readonly int[] BwType = new int[] { 1536, 5000, 7000, 8000, 200, 300, 1536 }; // sdrplay_api_Bw_MHzT, kHz
        public static readonly int[] IfType = new int[] { 0, 0, 0, 0, 450, 450, 1620 }; // sdrplay_api_If_kHzT
        public static readonly string[] Names = new string[] { "2 MS/s (IF filter 1.536 MHz)", "6 MS/s (IF filter 5 MHz)", "8 MS/s (IF filter 7 MHz)", "10 MS/s (IF filter 8 MHz)",
            "0.5 MS/s Low IF (IF filter 200 kHz)", "0.5 MS/s Low IF (IF filter 300 kHz)", "2 MS/s Low IF (IF filter 1.536 MHz)" };

        public const int Default = 0;

        public static int Clamp(int index)
        {
            if (index < 0)
            {
                return 0;
            }
            if (index > SampleRate.Length - 1)
            {
                return SampleRate.Length - 1;
            }
            return index;
        }

        // Lowest AGC setpoint (dBFS) the API accepts at this sample rate (API spec 3.15, 2.5.3).
        public static int MinAgcSetPoint(int sampleRate)
        {
            if (sampleRate < 8064000)
            {
                return -72;
            }
            if (sampleRate <= 9216000)
            {
                return -60;
            }
            return -48;
        }
    }
}
