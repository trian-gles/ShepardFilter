using System.Collections.Generic;
using UnityEngine;
using System;

namespace ShepardFilter
{
    public abstract class Filter
    {
        public abstract void SetFreqAndQ(float frequency, float q);
        public abstract float Process(float sample);

        public float Frequency { get; protected set; }

        public abstract void Reset();
    }


    public class BiquadBandpassFilter : Filter
    {
        private double a0, a1, a2, b0, b1, b2;
        private double x1, x2, y1, y2;
        public float Q { get; private set; }
        public float SampleRate { get; private set; }



        public BiquadBandpassFilter(float sampleRate, float frequency, float q)
        {
            SampleRate = sampleRate;
            Frequency = frequency;
            Q = q;
            CalculateCoefficients();
        }

        private void CalculateCoefficients()
        {
            double omega = 2.0 * Math.PI * Frequency / SampleRate;
            double sinOmega = Math.Sin(omega);
            double cosOmega = Math.Cos(omega);
            double alpha = sinOmega / (2.0 * Q);

            b0 = sinOmega / 2.0;
            b1 = 0.0;
            b2 = -sinOmega / 2.0;

            a0 = 1.0 + alpha;
            a1 = -2.0 * cosOmega;
            a2 = 1.0 - alpha;

            // Normalize coefficients
            b0 /= a0;
            b1 /= a0;
            b2 /= a0;
            a1 /= a0;
            a2 /= a0;
        }

        public override float Process(float input)
        {
            double output = b0 * input + b1 * x1 + b2 * x2
                           - a1 * y1 - a2 * y2;

            output = Math.Clamp(output, -1, 1);

            // Shift input/output history
            x2 = x1;
            x1 = input;
            y2 = y1;
            y1 = output;

            return (float)output;
        }

        public override void SetFreqAndQ(float frequency, float q)
        {
            Frequency = frequency;
            Q = q;
            CalculateCoefficients();
            Reset();
        }

        public override void Reset()
        {
            x1 = x2 = y1 = y2 = 0.0f;
        }
    }

    public class ShepardFilterManager
    {
        const int FREQMIN = 20;

        List<Filter> filters;
        public float q { get; set; }
        int sr;
        float centerFreq;
        float width;
        float wetGain;
        float dryGain;
        public float rolloff;
        float lastOffset;

        private object fLock = new object();
        private List<float> gains = new List<float>();

        float max;
        float min = Mathf.Log(FREQMIN, 2);

        float lowestFreq;


        public ShepardFilterManager(int numBands, float q, float centerFreq, float width, float rolloff, float mix)
        {
            filters = new List<Filter>();
            sr = AudioSettings.outputSampleRate;
            max = Mathf.Log(sr / 2, 2);
            this.q = q;
            this.width = width;
            this.rolloff = rolloff;
            this.centerFreq = centerFreq;

            lowestFreq = Mathf.Log(centerFreq, 2) - width;


            SetGains(mix);

            for (float b = 0; b < numBands; b++)
            {
                float f = b * 2 * width / numBands + lowestFreq;
                float fHz = Mathf.Pow(2, f);
                filters.Add(new BiquadBandpassFilter(sr, fHz, q));
                gains.Add(0);

            }
            Shift(0);
        }


        public void SetGains(float mix)
        {
            wetGain = Mathf.Sqrt(mix);
            dryGain = Mathf.Sqrt(1 - mix);
        }

        public void SetCenterFreq(float f)
        {
            centerFreq = f;
            lowestFreq = Mathf.Log(centerFreq, 2) - width;
        }

        public void SetWidth(float width)
        {
            this.width = width;
            lowestFreq = Mathf.Log(centerFreq, 2) - width;
        }

        //  moves all filters to an offset position between 0-1
        public void Shift(float offset)
        {
            float currMag = offset - (offset % 1.0f);
            float prevMag = lastOffset - (lastOffset % 1.0f);

            lock (fLock)
            {
                if (currMag > prevMag)
                {
                    Filter last = filters[^1];
                    filters.RemoveAt(filters.Count - 1);
                    filters.Insert(0, last);
                }
                if (currMag < prevMag)
                {
                    Filter first = filters[0];
                    filters.RemoveAt(0);
                    filters.Add(first);
                }

                float adjOffset = offset % 1.0f;

                for (int b = 0; b < filters.Count; b++)
                {
                    float f = (b + adjOffset) * 2 * width / filters.Count + lowestFreq;
                    float fHz = Mathf.Pow(2, f);
                    filters[b].SetFreqAndQ(fHz, q);
                    float dist = 1 - (Mathf.Abs(Mathf.Log(centerFreq,2) - f) / width);

                    if (f < min || f > max)
                        gains[b] = 0;
                    else if (dist <= 0)
                        gains[b] = 0;
                    else if (dist >= 1)
                        gains[b] = 1;
                    else
                        gains[b] = Mathf.Pow(dist, rolloff);
                }
            }
            lastOffset = offset;

        }

        // Process a single sample through all parallel bandpass filters
        public double ProcessParallel(float input)
        {
            double outputs = 0;

            lock (fLock)
            {
                for (int i = 0; i < filters.Count; i++)
                    outputs += filters[i].Process(input) * gains[i];
            }

            return outputs * wetGain / filters.Count + input * dryGain;
        }


    }

    public class ShepardFilter : MonoBehaviour
    {
        private ShepardFilterManager filters;
        private float offset = 0.0f;

        [SerializeField] private float mix = 0.9f;
        [SerializeField] private float q = 16;
        [SerializeField] private float rolloff = 0.5f;
        [SerializeField] private float centerFreq = 500;
        [SerializeField] private int filterCount = 17;
        [SerializeField] private float width = 4.3f;

        private bool shouldUpdate = true;

        private void Awake()
        {
            filters = new ShepardFilterManager(filterCount, q, centerFreq, width, rolloff, mix);
        }

        public void SetMix(float m)
        {
            mix = m;
			shouldUpdate = true;
        }

        public void SetQ(int q)
        {
            filters.q = q;
			shouldUpdate = true;
        }

        public void SetRolloff(float r)
        {
            float rolloff = r;
            filters.rolloff = rolloff;
			shouldUpdate = true;
        }

		public void SetWidth(float w){
			filters.SetWidth(w);
			shouldUpdate = true;
		}

        public void SetCenterFreq(float f)
        {
            filters.SetCenterFreq(f);
            shouldUpdate = true;
        }

        public void SetOffset(float offset)
        {
            this.offset = offset;
            shouldUpdate = true;
        }

		private void DoUpdate(){
			
	         filters.SetGains(mix);
             filters.Shift(offset);
		}

        void OnAudioFilterRead(float[] data, int channels)
        {
            if (filters == null)
                return;

            if (shouldUpdate) 
            {
				DoUpdate();
                shouldUpdate = false;
            }

            for (int i = 0; i < data.Length; i++)
            {
                float sample = (float)filters.ProcessParallel(data[i]);

                data[i] = sample;
            }
        }
    }
}


