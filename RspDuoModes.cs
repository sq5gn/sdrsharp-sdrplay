namespace SDRSharp.SDRplay
{
    // Operating modes offered in the plugin window. Diversity runs the RSPduo in Dual Tuner mode (API spec 3.15,
    // section 1): both tuners at 2 MS/s each, Low-IF (ADC 6 MHz, IF 1.620 MHz, filter 1.536 MHz), 50 ohm ports only.
    // The values were measured on the device.
    public static class RspDuoModes
    {
        public const int Single = 0;
        public const int Diversity = 1;

        public static readonly string[] Names = new string[] { "Single Tuner", "Diversity" };

        public const int Default = Single;

        public const int DiversitySampleRate = 2000000; // S/s per tuner, after the API's Low-IF down-conversion
        public const double DiversityAdcRate = 6000000.0; // rspDuoSampleFreq
        public const int DiversityBwType = 1536; // sdrplay_api_BW_1_536
        public const int DiversityIfType = 1620; // sdrplay_api_IF_1_620

        // What goes to SDR# in Diversity: one tuner, or the combination A + w * B (DiversityCombiner).
        public static readonly string[] OutputNames = new string[] { "Tuner 1", "Tuner 2", "Combined" };

        // OutputNames index <-> SDRplaySource.Output (SDRplayApi.TunerA, TunerB, TunerBoth).
        public static int OutputIndex(int output)
        {
            return output == SDRplayApi.TunerB ? 1 : output == SDRplayApi.TunerBoth ? 2 : 0;
        }

        public static int OutputOf(int index)
        {
            return index == 1 ? SDRplayApi.TunerB : index == 2 ? SDRplayApi.TunerBoth : SDRplayApi.TunerA;
        }

        public static int Clamp(int mode)
        {
            return mode == Diversity ? Diversity : Single;
        }
    }
}
