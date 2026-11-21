using SDRSharp.Radio;
using System;

namespace SDRSharp.SDRplay
{
    // Measures the two tuners in one band of the stream: the band is shifted to 0 and
    // decimated (DownConverter), low-pass filtered to its width (ComplexFilter), then Raa = sum |A|^2,
    // Rbb = sum |B|^2 and Rab = sum A * conj(B) are summed over the filtered samples. A and B go through
    // identical objects, so the phase between them is kept. Used by the stream thread only (DiversityCombiner).
    internal unsafe sealed class BandEstimator : IDisposable
    {
        private const int BlockLength = 1024; // input samples per DownConverter call (a multiple of 16)
        private const int FilterOrder = 255;
        private const double MinOutputRate = 4000; // S/s after the decimation
        private const double RateToWidth = 4; // the decimated rate holds at least 4 x the band width

        private readonly DownConverter _downConverterA;
        private readonly DownConverter _downConverterB;
        private readonly ComplexFilter _filterA;
        private readonly ComplexFilter _filterB;
        private readonly UnsafeBuffer _bufferA;
        private readonly UnsafeBuffer _bufferB;
        private readonly int _windowLength; // filtered samples summed
        private int _skip; // filtered samples skipped while the filters settle
        private int _count; // input samples in the block buffers
        private int _summed;
        private double _sumAA;
        private double _sumBB;
        private double _sumABReal;
        private double _sumABImag;

        // offset: centre of the band relative to the stream centre, Hz; width: Hz; duration: s of filtered samples.
        public BandEstimator(double sampleRate, double offset, double width, double duration)
        {
            var ratio = 1;
            while (sampleRate / (2 * ratio) >= Math.Max(RateToWidth * width, MinOutputRate) && ratio < 1 << 14)
            {
                ratio *= 2;
            }
            var outputRate = sampleRate / ratio;
            _downConverterA = new DownConverter(sampleRate, ratio);
            _downConverterB = new DownConverter(sampleRate, ratio);
            _downConverterA.Frequency = offset;
            _downConverterB.Frequency = offset;
            var kernel = FilterBuilder.MakeLowPassKernel(outputRate, FilterOrder, width / 2, WindowType.BlackmanHarris4);
            _filterA = new ComplexFilter(kernel);
            _filterB = new ComplexFilter(kernel);
            _bufferA = UnsafeBuffer.Create(BlockLength, sizeof(Complex));
            _bufferB = UnsafeBuffer.Create(BlockLength, sizeof(Complex));
            _skip = 2 * _filterA.Delay + 1;
            _windowLength = Math.Max((int)(outputRate * duration), 1);
        }

        public bool Done => _summed >= _windowLength;

        public double SumAA => _sumAA;

        public double SumBB => _sumBB;

        public double SumABReal => _sumABReal;

        public double SumABImag => _sumABImag;

        // A and B as I / Q pointers with a stride in shorts (2 = interleaved, 1 = separate arrays).
        public void Feed(short* ai, short* aq, int strideA, short* bi, short* bq, int strideB, int count)
        {
            var bufferA = (Complex*)_bufferA;
            var bufferB = (Complex*)_bufferB;
            var filled = _count;
            for (var i = 0; i < count && !Done; i++)
            {
                bufferA[filled].Real = ai[i * strideA];
                bufferA[filled].Imag = aq[i * strideA];
                bufferB[filled].Real = bi[i * strideB];
                bufferB[filled].Imag = bq[i * strideB];
                if (filled < BlockLength - 1)
                {
                    filled++;
                }
                else
                {
                    filled = 0;
                    ProcessBlock(bufferA, bufferB);
                }
            }
            _count = filled;
        }

        private void ProcessBlock(Complex* bufferA, Complex* bufferB)
        {
            var length = _downConverterA.Process(bufferA, BlockLength);
            _downConverterB.Process(bufferB, BlockLength);
            _filterA.Process(bufferA, length);
            _filterB.Process(bufferB, length);
            var start = Math.Min(_skip, length);
            _skip -= start;
            var end = Math.Min(length, start + _windowLength - _summed);
            double sumAA = 0;
            double sumBB = 0;
            double sumABReal = 0;
            double sumABImag = 0;
            for (var i = start; i < end; i++)
            {
                var a = bufferA[i];
                var b = bufferB[i];
                var p = a * ~b;
                sumAA += a.ModulusSquared();
                sumBB += b.ModulusSquared();
                sumABReal += p.Real;
                sumABImag += p.Imag;
            }
            _sumAA += sumAA;
            _sumBB += sumBB;
            _sumABReal += sumABReal;
            _sumABImag += sumABImag;
            _summed += Math.Max(end - start, 0);
        }

        public void Dispose()
        {
            _downConverterA.Dispose();
            _downConverterB.Dispose();
            _filterA.Dispose();
            _filterB.Dispose();
            _bufferA.Dispose();
            _bufferB.Dispose();
        }
    }
}
