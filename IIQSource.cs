namespace SDRSharp.SDRplay
{
    // A device that delivers interleaved little-endian int16 I/Q samples to the bridge.
    public interface IIQSource
    {
        uint MaximumSampleRate { get; }

        uint MaximumBandwidth { get; }

        uint DecimationStageCount { get; }

        uint MaximumGainIndex { get; }

        uint MinimumFrequency { get; }

        uint MaximumFrequency { get; }

        uint Resolution { get; }

        // Samples per block returned by Read.
        int BlockSamples { get; }

        long Frequency { get; set; }

        int DecimationStage { get; set; }

        int GainIndex { get; set; }

        // Display: the opened device, the last failure ("" = none), the sample rate measured on the stream (S/s),
        // the number of blocks lost because the reader did not keep up (saturating).
        string DeviceName { get; }

        string Failure { get; }

        double MeasuredSampleRate { get; }

        int Overruns { get; }

        // A client session starts (HELLO): fixes the settings DEVICE_INFO announces (the maximum sample rate)
        // until the next session, so the device always runs at the rate the client was told.
        void BeginSession();

        // Opens the device and starts the stream; on failure Failure is set and Read returns 0.
        void Start();

        void Stop();

        // buffer: at least offset + 4 * BlockSamples bytes. Blocks until a block is ready;
        // returns the number of samples written (I, Q int16 interleaved), 0 after Stop or a failure.
        int Read(byte[] buffer, int offset);
    }
}
