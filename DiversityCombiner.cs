using SDRSharp.Radio;
using System;
using System.Threading;

namespace SDRSharp.SDRplay
{
    // Diversity combination of the two RSPduo tuners (issue #36, stage 3): out = A + w * B, w = g * e^(j phi).
    // Pairs the packets of tuner A and tuner B with the same first sample number (measured: always equal) and
    // measures both streams over windows of about 100 ms.
    // Auto (closed form over one window, Rab = sum A * conj(B), Raa = sum |A|^2, Rbb = sum |B|^2):
    //   Null: w = -Rab / Rbb, the least-squares weight: minimum output power, a null on the strongest signal;
    //   Max:  w = Rab / Raa, maximal ratio combining for equal tuner noise: B in phase with A, weighted |hB| / |hA|.
    // Threads: the UI thread sets the weight and requests an auto; the API stream threads call Pair under the
    // source's write lock (one caller at a time); the display reads the published levels.
    public unsafe sealed class DiversityCombiner : IDisposable
    {
        public const int AutoNone = 0;
        public const int AutoNull = 1;
        public const int AutoMax = 2;

        public const double MinGainDB = -30;
        public const double MaxGainDB = 30;

        private const double WindowDuration = 0.1; // sec of samples per measurement
        private const double SettleDuration = 0.05; // sec skipped after an auto request (a retune settles)
        private const double FullScalePower = 32768.0 * 32768.0;

        private readonly object _weightLock = new object();
        private float _weightReal; // w, read by Pair under _weightLock
        private float _weightImag;
        private double _phase; // deg, 0..360, clockwise
        private double _gainDB;

        private volatile int _autoRequest = AutoNone;
        private volatile int _lastAuto = AutoNone;
        private volatile int _sampleRate = 2000000; // S/s of the packets (after the API's decimation)

        // Stream thread only (Pair).
        private UnsafeBuffer _pending;
        private int _pendingTuner;
        private uint _pendingNumber;
        private int _pendingCount;
        private int _autoActive = AutoNone;
        private int _skip;
        private int _windowLength;
        private int _windowCount;
        private double _sumAA;
        private double _sumBB;
        private double _sumOut;
        private double _sumABReal;
        private double _sumABImag;
        private int _windowSaturated;

        // Display (published per window).
        private double _powerA;
        private double _powerB;
        private double _powerOut;
        private double _coherence;
        private volatile int _saturated; // samples clipped in the last window
        private volatile int _unpaired; // packets without a partner since Reset, saturating
        private volatile bool _measured;

        public DiversityCombiner()
        {
            SetWeight(0, 0);
        }

        // Clockwise degrees, as drawn on the phase wheel.
        public double Phase
        {
            get { return Interlocked.CompareExchange(ref _phase, 0, 0); }
            set { SetWeight(value, GainDB); }
        }

        // Level of B relative to A in the sum, dB.
        public double GainDB
        {
            get { return Interlocked.CompareExchange(ref _gainDB, 0, 0); }
            set { SetWeight(Phase, value); }
        }

        public void SetWeight(double phase, double gainDB)
        {
            if (!double.IsFinite(phase))
            {
                phase = 0;
            }
            if (!double.IsFinite(gainDB))
            {
                gainDB = 0;
            }
            phase = WrapPhase(phase);
            gainDB = Math.Min(Math.Max(gainDB, MinGainDB), MaxGainDB);
            var gain = Math.Pow(10, gainDB / 20);
            var radians = -phase * Math.PI / 180;
            lock (_weightLock)
            {
                _weightReal = (float)(gain * Math.Cos(radians));
                _weightImag = (float)(gain * Math.Sin(radians));
                Interlocked.Exchange(ref _phase, phase);
                Interlocked.Exchange(ref _gainDB, gainDB);
            }
        }

        // AutoNull or AutoMax: measured over the next window after the settle time, then applied.
        public void RequestAuto(int mode)
        {
            if (mode == AutoNull || mode == AutoMax)
            {
                _lastAuto = mode;
                _autoRequest = mode;
            }
        }

        // After a retune: the phase between the tuners is new (issue #36, stage 1b); repeats the last auto.
        public void RepeatAuto()
        {
            var last = _lastAuto;
            if (last != AutoNone)
            {
                _autoRequest = last;
            }
        }

        public bool AutoPending => _autoRequest != AutoNone;

        public int SampleRate
        {
            get { return _sampleRate; }
            set { _sampleRate = Math.Max(value, 1); }
        }

        // dBFS per window; NaN before the first window.
        public double LevelA => Level(ref _powerA);

        public double LevelB => Level(ref _powerB);

        public double LevelOut => Level(ref _powerOut);

        // |Rab| / sqrt(Raa * Rbb), 0..1: how much of the two streams is the same signal.
        public double Coherence => _measured ? Interlocked.CompareExchange(ref _coherence, 0, 0) : double.NaN;

        public int Saturated => _saturated;

        public int Unpaired => _unpaired;

        // Stream start: forgets the pending packet and the measurements.
        public void Reset()
        {
            _pendingTuner = 0;
            _pendingCount = 0;
            _autoActive = AutoNone;
            _skip = 0;
            _unpaired = 0;
            _saturated = 0;
            _measured = false;
            ResetWindow();
        }

        // Stream thread. Returns the number of combined IQ samples written to output (interleaved int16, at least
        // 2 * count shorts), 0 while the packet waits for its partner. write = false: measures only.
        public int Pair(int tuner, uint number, short* xi, short* xq, int count, short* output, bool write)
        {
            if (count <= 0)
            {
                return 0;
            }
            if (_pendingCount == 0 || _pendingTuner == tuner || _pendingNumber != number || _pendingCount != count)
            {
                if (_pendingCount != 0 && _unpaired < int.MaxValue)
                {
                    _unpaired++;
                }
                Store(tuner, number, xi, xq, count);
                return 0;
            }
            var pendingPtr = (short*)_pending;
            _pendingCount = 0;
            if (tuner == SDRplayApi.TunerB)
            {
                return Combine(pendingPtr, xi, xq, count, output, write);
            }
            return CombineSwapped(xi, xq, pendingPtr, count, output, write);
        }

        private void Store(int tuner, uint number, short* xi, short* xq, int count)
        {
            if (_pending == null || _pending.Length < count)
            {
                if (_pending != null)
                {
                    _pending.Dispose();
                }
                _pending = UnsafeBuffer.Create(count, 2 * sizeof(short));
            }
            var pendingPtr = (short*)_pending;
            for (var i = 0; i < count; i++)
            {
                pendingPtr[2 * i] = xi[i];
                pendingPtr[2 * i + 1] = xq[i];
            }
            _pendingTuner = tuner;
            _pendingNumber = number;
            _pendingCount = count;
        }

        // a: tuner A interleaved (the pending packet), bi / bq: tuner B.
        private int Combine(short* a, short* bi, short* bq, int count, short* output, bool write)
        {
            float wr;
            float wi;
            lock (_weightLock)
            {
                wr = _weightReal;
                wi = _weightImag;
            }
            BeginPacket(count);
            var accumulate = _skip == 0;
            double sumAA = 0;
            double sumBB = 0;
            double sumOut = 0;
            double sumABReal = 0;
            double sumABImag = 0;
            var saturated = 0;
            for (var i = 0; i < count; i++)
            {
                float ar = a[2 * i];
                float ai = a[2 * i + 1];
                float br = bi[i];
                float bim = bq[i];
                var outReal = ar + wr * br - wi * bim;
                var outImag = ai + wr * bim + wi * br;
                if (accumulate)
                {
                    sumAA += ar * ar + ai * ai;
                    sumBB += br * br + bim * bim;
                    sumABReal += ar * br + ai * bim;
                    sumABImag += ai * br - ar * bim;
                    sumOut += outReal * outReal + outImag * outImag;
                }
                if (write)
                {
                    output[2 * i] = Saturate(outReal, ref saturated);
                    output[2 * i + 1] = Saturate(outImag, ref saturated);
                }
            }
            EndPacket(count, accumulate, sumAA, sumBB, sumOut, sumABReal, sumABImag, saturated);
            return write ? count : 0;
        }

        // B arrived first: the pending packet is B, the current one A.
        private int CombineSwapped(short* ai, short* aq, short* b, int count, short* output, bool write)
        {
            float wr;
            float wi;
            lock (_weightLock)
            {
                wr = _weightReal;
                wi = _weightImag;
            }
            BeginPacket(count);
            var accumulate = _skip == 0;
            double sumAA = 0;
            double sumBB = 0;
            double sumOut = 0;
            double sumABReal = 0;
            double sumABImag = 0;
            var saturated = 0;
            for (var i = 0; i < count; i++)
            {
                float ar = ai[i];
                float aim = aq[i];
                float br = b[2 * i];
                float bim = b[2 * i + 1];
                var outReal = ar + wr * br - wi * bim;
                var outImag = aim + wr * bim + wi * br;
                if (accumulate)
                {
                    sumAA += ar * ar + aim * aim;
                    sumBB += br * br + bim * bim;
                    sumABReal += ar * br + aim * bim;
                    sumABImag += aim * br - ar * bim;
                    sumOut += outReal * outReal + outImag * outImag;
                }
                if (write)
                {
                    output[2 * i] = Saturate(outReal, ref saturated);
                    output[2 * i + 1] = Saturate(outImag, ref saturated);
                }
            }
            EndPacket(count, accumulate, sumAA, sumBB, sumOut, sumABReal, sumABImag, saturated);
            return write ? count : 0;
        }

        // A new auto request restarts the window after the settle time; the skip counts down per packet.
        private void BeginPacket(int count)
        {
            var request = _autoRequest;
            if (request != _autoActive)
            {
                _autoActive = request;
                ResetWindow();
                _skip = request != AutoNone ? (int)(_sampleRate * SettleDuration) : 0;
            }
            if (_skip > 0)
            {
                _skip = _skip > count ? _skip - count : 0;
            }
        }

        private void EndPacket(int count, bool accumulated, double sumAA, double sumBB, double sumOut, double sumABReal, double sumABImag, int saturated)
        {
            if (!accumulated)
            {
                return;
            }
            _sumAA += sumAA;
            _sumBB += sumBB;
            _sumOut += sumOut;
            _sumABReal += sumABReal;
            _sumABImag += sumABImag;
            _windowSaturated = Math.Min(_windowSaturated + saturated, int.MaxValue - 2 * count);
            if (_windowCount < _windowLength - count)
            {
                _windowCount += count;
                return;
            }
            _windowCount += count;
            Publish();
            ResetWindow();
        }

        private void Publish()
        {
            var n = (double)_windowCount;
            Interlocked.Exchange(ref _powerA, _sumAA / n / FullScalePower);
            Interlocked.Exchange(ref _powerB, _sumBB / n / FullScalePower);
            Interlocked.Exchange(ref _powerOut, _sumOut / n / FullScalePower);
            var cross = Math.Sqrt(_sumABReal * _sumABReal + _sumABImag * _sumABImag);
            var norm = Math.Sqrt(_sumAA * _sumBB);
            Interlocked.Exchange(ref _coherence, norm > 0 ? cross / norm : 0);
            _saturated = _windowSaturated;
            _measured = true;
            var auto = _autoActive;
            if (auto == AutoNone)
            {
                return;
            }
            // w for out = A + w * B (Rab = sum A * conj(B)).
            double wr;
            double wi;
            if (auto == AutoNull)
            {
                if (_sumBB <= 0)
                {
                    return;
                }
                wr = -_sumABReal / _sumBB;
                wi = -_sumABImag / _sumBB;
            }
            else
            {
                if (_sumAA <= 0)
                {
                    return;
                }
                wr = _sumABReal / _sumAA;
                wi = _sumABImag / _sumAA;
            }
            var magnitude = Math.Sqrt(wr * wr + wi * wi);
            if (magnitude > 0)
            {
                // The phase wheel turns clockwise: phase = -arg(w).
                SetWeight(-Math.Atan2(wi, wr) * 180 / Math.PI, 20 * Math.Log10(magnitude));
            }
            Interlocked.CompareExchange(ref _autoRequest, AutoNone, auto);
            _autoActive = AutoNone;
        }

        private void ResetWindow()
        {
            _windowLength = Math.Max((int)(_sampleRate * WindowDuration), 4096);
            _windowCount = 0;
            _sumAA = 0;
            _sumBB = 0;
            _sumOut = 0;
            _sumABReal = 0;
            _sumABImag = 0;
            _windowSaturated = 0;
        }

        private static short Saturate(float value, ref int saturated)
        {
            var rounded = (int)Math.Round(value);
            if (rounded > short.MaxValue)
            {
                saturated++;
                return short.MaxValue;
            }
            if (rounded < short.MinValue)
            {
                saturated++;
                return short.MinValue;
            }
            return (short)rounded;
        }

        private double Level(ref double power)
        {
            var p = Interlocked.CompareExchange(ref power, 0, 0);
            return _measured && p > 0 ? 10 * Math.Log10(p) : double.NaN;
        }

        private static double WrapPhase(double phase)
        {
            phase %= 360;
            if (phase < 0)
            {
                phase += 360;
            }
            return phase;
        }

        public void Dispose()
        {
            if (_pending != null)
            {
                _pending.Dispose();
                _pending = null;
            }
        }
    }
}
