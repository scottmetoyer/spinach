using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using NAudio.Wave;

namespace Spinach.Domain
{
    public class Utility
    {
        public static byte[] LoadWave(string filename)
        {
            byte[] buffer;

            using (WaveFileReader reader = new WaveFileReader(filename))
            {
                buffer = new byte[(int)reader.Length];
                reader.Read(buffer, 0, (int)reader.Length);
            }

            return buffer;
        }

        public static float[] LoadWaveSamples(string filename)
        {
            float[] buffer;

            using (AudioFileReader reader = new AudioFileReader(filename))
            {
                buffer = new float[(int)reader.Length];
                reader.Read(buffer, 0, (int)reader.Length);
            }

            return buffer;
        }

        public static void ConvertBuffer(float[] from, byte[] to)
        {
            const int bytesPerSample = 2;
            int samplesPerBuffer = from.Length;

            for (int i = 0; i < samplesPerBuffer; i++)
            {
                // First clamp the value to the [-1.0..1.0] range
                float floatSample = MathHelper.Clamp(from[i], -1.0f, 1.0f);

                // Convert it to the 16 bit [short.MinValue..short.MaxValue] range
                short shortSample = (short)(floatSample >= 0.0f ? floatSample * short.MaxValue : floatSample * short.MinValue * -1);

                // Calculate the right index based on the PCM format of interleaved samples per channel [L-R-L-R]
                int index = i * 2;

                // Store the 16 bit sample as two consecutive 8 bit values in the buffer with regard to endian-ness
                if (!BitConverter.IsLittleEndian)
                {
                    to[index] = (byte)(shortSample >> 8);
                    to[index + 1] = (byte)shortSample;
                }
                else
                {
                    to[index] = (byte)shortSample;
                    to[index + 1] = (byte)(shortSample >> 8);
                }
            }
        }
    }
}
