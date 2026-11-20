namespace SDRSharp.SDRplay
{
    // SpyServer wire protocol: little-endian uint32 fields, no padding.
    // Values measured by public reflection on the SDR# 1922 client types (SDRSharp.FrontEnds.SpyServer).
    public static class SpyServerProtocol
    {
        public const uint ProtocolVersion = (2u << 24) | 1700u; // 2.0.1700
        public const uint ProtocolMajorMask = 0xFF000000;
        public const int MaxCommandBodySize = 256;
        public const int MessageHeaderSize = 20;
        public const int CommandHeaderSize = 8;
        public const int DeviceInfoSize = 48;
        public const int ClientSyncSize = 56;

        public const uint CmdHello = 0;
        public const uint CmdGetSetting = 1;
        public const uint CmdSetSetting = 2;
        public const uint CmdPing = 3;

        public const uint SettingStreamingMode = 0;
        public const uint SettingStreamingEnabled = 1;
        public const uint SettingGain = 2;
        public const uint SettingOptimizedStreaming = 3;
        public const uint SettingIQFormat = 100;
        public const uint SettingIQFrequency = 101;
        public const uint SettingIQDecimation = 102;
        public const uint SettingIQDigitalGain = 103;

        public const uint StreamTypeStatus = 0;
        public const uint StreamTypeIQ = 1;

        public const uint StreamModeIQOnly = 1;

        public const uint FormatInt16 = 2;

        public const uint MsgDeviceInfo = 0;
        public const uint MsgClientSync = 1;
        public const uint MsgPong = 2;
        public const uint MsgInt16IQ = 101;

        public const uint DeviceAirspyOne = 1;
    }
}
