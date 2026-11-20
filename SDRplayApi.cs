using System;
using System.Runtime.InteropServices;

namespace SDRSharp.SDRplay
{
    // SDRplay API 3.15 data types, written from the installed headers (C:\Program Files\SDRplay\API\inc).
    // Sequential layout with natural alignment = the C layout on x64 (the headers use no #pragma pack).
    public static class SDRplayApi
    {
        public const float Version = 3.15f;
        public const int MaxDevices = 16;
        public const int RSPduoId = 3; // SDRPLAY_RSPduo_ID

        public const int Success = 0;

        public const int TunerA = 1;
        public const int TunerB = 2;
        public const int TunerBoth = 3;

        public const int RspDuoModeSingleTuner = 1;
        public const int RspDuoModeDualTuner = 2;
        public const int RspDuoModeMaster = 4;

        public const int Bw1536 = 1536; // sdrplay_api_BW_1_536
        public const int IFZero = 0;

        public const int AgcDisable = 0;
        public const int Agc100Hz = 1;
        public const int Agc50Hz = 2;
        public const int Agc5Hz = 3;

        public const uint UpdateTunerGr = 0x00008000;
        public const uint UpdateTunerFrf = 0x00020000;
        public const uint UpdateCtrlAgc = 0x01000000;
        public const uint UpdateCtrlDecimation = 0x00800000;
        public const uint UpdateCtrlOverloadMsgAck = 0x04000000;
        public const uint UpdateDevPpm = 0x00000002;
        public const uint UpdateRspDuoBiasTControl = 0x08000000;
        public const uint UpdateRspDuoAmPortSelect = 0x10000000;
        public const uint UpdateRspDuoTuner1AmNotchControl = 0x20000000;
        public const uint UpdateRspDuoRfNotchControl = 0x40000000;
        public const uint UpdateRspDuoRfDabNotchControl = 0x80000000;
        public const uint UpdateExt1None = 0;

        // sdrplay_api_RspDuo_AmPortSelectT. AMPORT_1 is the tuner 1 Hi-Z port (SoapySDRPlay3 setAntenna, "Tuner 1 Hi-Z").
        public const int RspDuoAmPort1 = 1;
        public const int RspDuoAmPort2 = 0;

        public const int EventGainChange = 0;
        public const int EventPowerOverloadChange = 1;
        public const int EventDeviceRemoved = 2;
        public const int EventRspDuoModeChange = 3;
        public const int EventDeviceFailure = 4;

        public const int OverloadDetected = 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct SDRplayDevice
    {
        public fixed byte SerNo[64];
        public byte HwVer;
        public int Tuner;
        public int RspDuoMode;
        public byte Valid;
        public double RspDuoSampleFreq;
        public IntPtr Dev;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct SDRplayDeviceParams
    {
        public SDRplayDevParams* DevParams;
        public SDRplayRxChannelParams* RxChannelA;
        public SDRplayRxChannelParams* RxChannelB;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDRplayFsFreq
    {
        public double FsHz;
        public byte SyncUpdate;
        public byte ReCal;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDRplaySyncUpdate
    {
        public uint SampleNum;
        public uint Period;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDRplayResetFlags
    {
        public byte ResetGainUpdate;
        public byte ResetRfUpdate;
        public byte ResetFsUpdate;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDRplayRsp1aParams
    {
        public byte RfNotchEnable;
        public byte RfDabNotchEnable;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDRplayRsp2Params
    {
        public byte ExtRefOutputEn;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDRplayRspDuoParams
    {
        public int ExtRefOutputEn;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDRplayRspDxParams
    {
        public byte HdrEnable;
        public byte BiasTEnable;
        public int AntennaSel;
        public byte RfNotchEnable;
        public byte RfDabNotchEnable;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDRplayDevParams
    {
        public double Ppm;
        public SDRplayFsFreq FsFreq;
        public SDRplaySyncUpdate SyncUpdate;
        public SDRplayResetFlags ResetFlags;
        public int Mode;
        public uint SamplesPerPkt;
        public SDRplayRsp1aParams Rsp1aParams;
        public SDRplayRsp2Params Rsp2Params;
        public SDRplayRspDuoParams RspDuoParams;
        public SDRplayRspDxParams RspDxParams;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDRplayGainValues
    {
        public float Curr;
        public float Max;
        public float Min;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDRplayGain
    {
        public int GRdB;
        public byte LNAstate;
        public byte SyncUpdate;
        public int MinGr;
        public SDRplayGainValues GainVals;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDRplayRfFreq
    {
        public double RfHz;
        public byte SyncUpdate;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDRplayDcOffsetTuner
    {
        public byte DcCal;
        public byte SpeedUp;
        public int TrackTime;
        public int RefreshRateTime;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDRplayTunerParams
    {
        public int BwType;
        public int IfType;
        public int LoMode;
        public SDRplayGain Gain;
        public SDRplayRfFreq RfFreq;
        public SDRplayDcOffsetTuner DcOffsetTuner;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDRplayDcOffset
    {
        public byte DCenable;
        public byte IQenable;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDRplayDecimation
    {
        public byte Enable;
        public byte DecimationFactor;
        public byte WideBandSignal;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDRplayAgc
    {
        public int Enable;
        public int SetPointDBfs;
        public ushort AttackMs;
        public ushort DecayMs;
        public ushort DecayDelayMs;
        public ushort DecayThresholdDB;
        public int SyncUpdate;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDRplayControlParams
    {
        public SDRplayDcOffset DcOffset;
        public SDRplayDecimation Decimation;
        public SDRplayAgc Agc;
        public int AdsbMode;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDRplayRsp1aTunerParams
    {
        public byte BiasTEnable;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDRplayRsp2TunerParams
    {
        public byte BiasTEnable;
        public int AmPortSel;
        public int AntennaSel;
        public byte RfNotchEnable;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDRplayRspDuoTunerParams
    {
        public byte BiasTEnable;
        public int Tuner1AmPortSel;
        public byte Tuner1AmNotchEnable;
        public byte RfNotchEnable;
        public byte RfDabNotchEnable;
        public byte ResetGainUpdate;
        public byte ResetRfUpdate;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDRplayRspDxTunerParams
    {
        public int HdrBw;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDRplayRxChannelParams
    {
        public SDRplayTunerParams TunerParams;
        public SDRplayControlParams CtrlParams;
        public SDRplayRsp1aTunerParams Rsp1aTunerParams;
        public SDRplayRsp2TunerParams Rsp2TunerParams;
        public SDRplayRspDuoTunerParams RspDuoTunerParams;
        public SDRplayRspDxTunerParams RspDxTunerParams;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDRplayGainCbParam
    {
        public uint GRdB;
        public uint LnaGRdB;
        public double CurrGain;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDRplayStreamCbParams
    {
        public uint FirstSampleNum;
        public int GrChanged;
        public int RfChanged;
        public int FsChanged;
        public uint NumSamples;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct SDRplayCallbackFns
    {
        public delegate* unmanaged[Cdecl]<short*, short*, SDRplayStreamCbParams*, uint, uint, void*, void> StreamACbFn;
        public delegate* unmanaged[Cdecl]<short*, short*, SDRplayStreamCbParams*, uint, uint, void*, void> StreamBCbFn;
        public delegate* unmanaged[Cdecl]<int, int, void*, void*, void> EventCbFn;
    }
}
