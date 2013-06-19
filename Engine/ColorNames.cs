using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Engine
{
    //Ported subset from https://github.com/StanfordHCI/c3
    public class ColorNames
    {
        public List<CIELAB> colors;
        public int[] T;
        public List<String> terms;
        public Dictionary<String, int> map;
        public Dictionary<int, int> tcount;
        public Dictionary<int, int> ccount;

        public ColorNames(String jsonFile)
        {
            //init
            colors = new List<CIELAB>();
            terms = new List<String>();

            map = new Dictionary<String, int>();
            tcount = new Dictionary<int, int>();
            ccount = new Dictionary<int, int>();

            //read in the json file
            String[] lines = File.ReadAllLines(jsonFile);

            //put it in all one line
            String json = String.Join("", lines).Replace("{","").Replace("}","").Replace("[","").Replace("]","");
            string[] fields = json.Split(new string[] { "\"color\":", ",\"terms\":", ",\"T\":",",\"A\":" },StringSplitOptions.RemoveEmptyEntries);

            if (fields.Count() != 4)
                Console.WriteLine("JSON parse problem!");

            //populate the colors
            String[] colorVals = fields[0].Split(',');
            for (int i = 0; i < colorVals.Count(); i += 3)
            {
                colors.Add(new CIELAB(double.Parse(colorVals[i]), double.Parse(colorVals[i + 1]), double.Parse(colorVals[i + 2])));
            }

            //populate the terms 
            terms = new List<String>(fields[1].Split(','));

            T = new int[terms.Count() * colors.Count()];

            //populate the counts table T
            String[] countVals = fields[2].Split(',');
            for (int i = 0; i < countVals.Count(); i += 2)
            {
                T[Int32.Parse(countVals[i])] = Int32.Parse(countVals[i + 1]);
            }

            //word association matrix in fields[2]

            //calculate the total term and color counts
            //foreach (int idx in T.Keys)
            for (int idx=0; idx<T.Count(); idx++)
            {
                double W = terms.Count();
                int c = (int)Math.Floor(idx / W);
                int w = (int)Math.Floor(idx % W);
                int v = T[idx];

                if (!ccount.ContainsKey(c))
                    ccount.Add(c, 0);
                if (!tcount.ContainsKey(w))
                    tcount.Add(w, 0);

                ccount[c] += v;
                tcount[w] += v;
            }

            //build the map from color vals to index
            for (int c = 0; c < colors.Count(); c++)
            {
                map.Add(colors[c].L + "," + colors[c].A + "," + colors[c].B, c);
            }

        }

        public int GetBin(CIELAB x)
        {
           int L = (int)(5 * Math.Round(x.L/5));
           int A = (int)(5 * Math.Round(x.A/5));
           int B = (int)(5 * Math.Round(x.B/5));
           String s = L+","+A+","+B;

           if (map.ContainsKey(s))
           {
               return map[s];
           } 
           else {
                //look at nearby bins
               CIELAB[] neighbors = new CIELAB[] {new CIELAB(L, A, B + 5), new CIELAB(L, A, B - 5), new CIELAB(L, A + 5, B), new CIELAB(L, A - 5, B), new CIELAB(L - 5, A, B), new CIELAB(L + 5, A, B)};
               double bestDist = Double.PositiveInfinity;
               String best = "";
              
               for (int i = 0; i < neighbors.Count(); i++)
               {
                   String key = neighbors[i].L + "," + neighbors[i].A + "," + neighbors[i].B;
                   double dist = neighbors[i].SqDist(x);
                   if (dist < bestDist && map.ContainsKey(key))
                   {
                       bestDist = dist;
                       best = key;
                   }
               }
               if (best != "")
               {
                   //return the best
                   return map[best];
               } 
               else
                   //give up
                   return -1;
           }
        }

        public double CosineDistance(int i, int j)
        {
            int C = colors.Count();
            int W = terms.Count();

            double sa = 0, sb = 0, sc = 0;
            int ta;
            int tb;
            for (var w = 0; w < W; w++)
            {
                ta = T[i * W + w];
                tb = T[j * W + w];
                sa += ta * ta;
                sb += tb * tb;
                sc += ta * tb;
            }

            return sc / Math.Max((Math.Sqrt(sa * sb)), 1);
        }

        public double CosineDistance(CIELAB a, CIELAB b)
        {
            int i = GetBin(a);
            int j = GetBin(b);
            int C = colors.Count();
            int W = terms.Count();

            double sa = 0, sb = 0, sc = 0;
            int ta;
            int tb;
            for (var w = 0; w < W; w++)
            {
                ta = T[i * W + w];
                tb = T[j * W + w]; 
                sa += ta * ta;
                sb += tb * tb;
                sc += ta * tb;
            }

            return sc / Math.Max((Math.Sqrt(sa * sb)),1);
        }

        public double Saliency(CIELAB a)
        {
            double H = 0;
            int W = terms.Count();
            int i=GetBin(a);
            for (int w = 0; w < W; w++)
            {
                double p = T[i * W + w];
                p /= ccount[i];
                if (p > 0)
                    H += p * Math.Log(p) / Math.Log(2);
            }
            return H;
        }

        public double NormalizedSaliency(CIELAB a, double minE=-4.5, double maxE=0)
        {
            //hardcoded to XKCD
            //double minE = -4.5;
            //double maxE = 0;
            return (Saliency(a) - minE) / (maxE - minE);
        }
    }
}
