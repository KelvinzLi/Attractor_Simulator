using System;

using SFML.Graphics;
using SFML.Audio;

using System;
using SFML.Window;
using SFML.Graphics;
using SFML.System;

using global::System.Collections.Generic;
using global::System.Numerics;

namespace SFML
{
    using global::System.Linq;
    using global::System.Threading;
    using global::System.Threading.Tasks;
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Numerics.LinearAlgebra.Single;
    using Matrix = Accord.Math.Matrix;

    class Program
    {
        static void Main(string[] args)
        {
            float[,] plane1 = new float[1000, 3];
            for (int ii=0; ii < 1000; ii += 1)
            {
                plane1[ii, 2] = 3 * (ii/100)-5;
                plane1[ii, 1] = 3 * ((ii%100)/10)-5;
                plane1[ii, 0] = 3 * ((ii%100)%10)-5+1f;
            }
            
            Lorentz_attractor Lorentz = new Lorentz_attractor();
            Attractor_V att = new Attractor_V(plane1, Lorentz);
            att.Show();
        }
    }

    class Lorentz_attractor : attractor_constuctor
    {
        private float a = 1.4f;
        public float X_slope(float x, float y, float z)
        {
            return 10 * (-1 * x + y);
            // return 5 * x - y * z;
            // return y - 3f * x + 2.7f * y * z;
            // return 0.2f * x + y * z;
            // return (z - 0.7f) * x - 3.5f * y;
            // return -a * x - 4 * y - 4 * z - (float) Math.Pow(y, 2);
        }

        public float Y_slope(float x, float y, float z)
        {
            return -1 * x * z + 28 * x - y;
            // return -10 * y + x * z;
            // return 1.7f * y - x * z + z;
            // return 0.01f * x - 0.4f * y - x * z;
            // return 3.5f * x + (z - 0.7f) * y;
            // return -a * y - 4 * z - 4 * x - (float) Math.Pow(z, 2);
        }

        public float Z_slope(float x, float y, float z)
        {
            return x * y - (float)(8f * z / 3f);
            // return -0.38f * z + x * y / 3;
            // return 2 * x * y - 9 * z;
            // return -z - x * y;
            // return (float) (0.6 + 0.95 * z - z * z * z / 3 - (x * x + y * y) * (1 + 0.25 * z) + 0.1 * z * x * x * x);
            // return -a * z - 4 * x - 4 * y - (float) Math.Pow(x, 2);
        }
    }
}