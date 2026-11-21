namespace SDRSharp.SDRplay
{
    // Sample rates offered for the RSPduo in Single Tuner, zero IF, each with the widest IF filter below it.
    // Filter values: sdrplay_api_Bw_MHzT (sdrplay_api_tuner.h). The rates were checked on the device.
    public static class RspDuoSampleRates
    {
        public static readonly int[] SampleRate = new int[] { 2000000, 6000000, 8000000, 10000000 }; // S/s
        public static readonly int[] BwType = new int[] { 1536, 5000, 7000, 8000 }; // sdrplay_api_Bw_MHzT, kHz
        public static readonly string[] Names = new string[] { "2 MS/s (IF filter 1.536 MHz)", "6 MS/s (IF filter 5 MHz)", "8 MS/s (IF filter 7 MHz)", "10 MS/s (IF filter 8 MHz)" };

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
