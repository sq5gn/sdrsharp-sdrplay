namespace SDRSharp.SDRplay
{
    // RSPduo antenna inputs in Single Tuner mode: tuner 1 (API tuner A) has a 50 ohm port and a Hi-Z port,
    // tuner 2 (API tuner B) a 50 ohm port. Port names as in SoapySDRPlay3 listAntennas.
    public static class RspDuoAntenna
    {
        public const int Tuner1_50Ohm = 0;
        public const int Tuner1HiZ = 1;
        public const int Tuner2_50Ohm = 2;

        public static readonly string[] Names = new string[] { "Tuner 1 50 Ω", "Tuner 1 Hi-Z", "Tuner 2 50 Ω" };

        public const int Default = Tuner1_50Ohm;

        public static int Clamp(int antenna)
        {
            if (antenna < Tuner1_50Ohm || antenna > Tuner2_50Ohm)
            {
                return Default;
            }
            return antenna;
        }

        public static int Tuner(int antenna)
        {
            return antenna == Tuner2_50Ohm ? SDRplayApi.TunerB : SDRplayApi.TunerA;
        }

        public static int AmPort(int antenna)
        {
            return antenna == Tuner1HiZ ? SDRplayApi.RspDuoAmPort1 : SDRplayApi.RspDuoAmPort2;
        }
    }
}
