using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine
{

    public abstract class Kernel
    {

        //default is a triangle kernel
        public abstract double Eval(double dist);
        public abstract String Name();
        public abstract double Sigma();
 
    }

    public class TriangleKernel : Kernel
    {
        double sigma;
        public TriangleKernel(double s)
        {
            sigma = s;
        }

        public override double Sigma()
        {
            return sigma;
        }

        public override string Name()
        {
            return "triangle";
        }

        public override double Eval(double dist)
        {
            double u = dist / sigma;
            if (u <= 1)
                return 1 - u;
            else
                return 0;
        }
    }

    public class GaussianKernel : Kernel
    {
        double sigma;
        public GaussianKernel(double s)
        {
            sigma = s;
        }

        public override string Name()
        {
            return "gaussian";
        }

        public override double Eval(double dist)
        {
            double u = dist/sigma;
            double c = 1.0/(2*Math.PI);
            return c*Math.Exp(-0.5 * u*u);
        }

        public override double Sigma()
        {
            return sigma;
        }
    }
}
