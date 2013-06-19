using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Engine
{
    //TODO: if time, make the image processing faster
    using DistFunc = Func<CIELAB, CIELAB, double>;

    public class Category
    {
        public String title;
        public List<String> members;

        public Category(String name, String[] memberNames)
        {
            title = name;
            members = new List<String>(memberNames);
        }
    }

    public class ColorAssignment
    {
        private Dictionary<String, Color> colors = new Dictionary<String, Color>();
        public Category category;

        public ColorAssignment(Category c)
        {
            colors = new Dictionary<String, Color>();
            category = c;
        }

        public void Set(String concept, Color cand)
        {
            if (!colors.ContainsKey(concept))
                colors.Add(concept, cand);
            else
                colors[concept] = cand;
        }

        public Color Get(String concept)
        {
            return colors[concept];
        }

        public String[] Keys 
        {
            get{ return colors.Keys.ToArray<String>(); }
        }

        public double Overlap(ColorAssignment other)
        {
            double score = 0;

            //score the two palettes
            if (colors.Keys.SequenceEqual<String>(other.colors.Keys))
            {
                foreach (String key in colors.Keys)
                {
                    if (other.colors[key] == this.colors[key])
                        score += 1;
                }
                score /= colors.Keys.Count();  
            }
            else
            {
                //something is wrong
                Console.WriteLine("The two palettes are not compatible");
                throw new ArgumentException();
            }

            return score;
        }
    }

    public class ColorAssigner
    {
        //Paths
        String json = "";//"C:\\Users\\sharon\\Documents\\Color\\c3\\data\\xkcd\\c3_data.json";
        String imageDir = ""; //where images are stored
        String cacheDir = ""; //where cached histograms are stored

        String apiKey = "";
        String cxId = "";

        //Color name distance calculations
        ColorNames colorNames;
        double[,] CNcache;

        //Tableau 20 palette
        String[] paletteHex = new String[] { "#1f77b4", "#ff7f0e", "#2ca02c", "#d62728", "#9467bd", "#8c564b", "#e377c2", "#7f7f7f", "#bcbd22", "#17becf", "#aec7e8", "#ffbb78", "#98df8a", "#ff9896", "#c5b0d5", "#c49c94", "#f7b6d2", "#c7c7c7", "#dbdb8d", "#9edae5" };
        List<Color> paletteRGB = new List<Color>();
        List<CIELAB> paletteLAB = new List<CIELAB>();

        //histogram params
        static int binSize = 5;
        static int Lbins = 100 / binSize;//0 to 100
        static int Abins = 200 / binSize; //-100 to 100
        static int Bbins = 200 / binSize; //-100 to 100
  
       
        double epsilon = 0.00001;

        //images are stored concept name "_" replacing spaces and "_clipart" if clipart/ numbers per image
        //potato_clipart/0.jpg etc
        //stored by query
        public ColorAssigner(String imageDirectory, String cacheDirectory, String jsonFile, String googleApiKey, String customEngineId)
        {
            imageDir = imageDirectory;
            json = jsonFile;
            cacheDir = cacheDirectory;

            apiKey = googleApiKey;
            cxId = customEngineId;
        
            //make sure the directories exist
            Directory.CreateDirectory(imageDir);
            Directory.CreateDirectory(cacheDir);

            //initialize color names cache
            colorNames = new ColorNames(jsonFile);
            CNcache = new double[colorNames.colors.Count(), colorNames.colors.Count()];
            for (int i = 0; i < colorNames.colors.Count(); i++)
                for (int j = 0; j < colorNames.colors.Count(); j++)
                    CNcache[i, j] = -1;


            foreach (String hex in paletteHex)
            {
                Color color = ColorTranslator.FromHtml(hex);
                paletteRGB.Add(color);
                paletteLAB.Add(Util.RGBtoLAB(color));
            }
        }


        public ColorAssignment AssignColors(Category category)
        {
            //check that number of category members is less than number of palette colors
            if (category.members.Count() > paletteHex.Count())
                throw new ArgumentException("Too many category members, or not enough palette colors!");

            //Get the images, if needed
            SearchImages(category, 3);

            //Compute the histograms
            ComputeHistograms(category);

            //Compute affinities and assign colors
            double[,] affinities = ComputeAffinities(category);
            
            //Since the Hungarian Algorithm minimizes sum of affinities, let's invert the affinities
            //Assume that the number of category members is less than the number of palette colors
            double[,] matrix = new double[paletteHex.Count(), paletteHex.Count()];
            for (int c=0; c<paletteHex.Count(); c++)
                for (int w=0; w<category.members.Count(); w++)
                    matrix[w,c] = 1-affinities[c,w];
                    
            
            //create an assignment from colors to concept names
            List<int> assignIds = HungarianAlgorithm.Solve(matrix);

            ColorAssignment assignment = new ColorAssignment(category);
            for (int w = 0; w < category.members.Count(); w++)
            {
                assignment.Set(category.members[w], paletteRGB[assignIds[w]]);
            }

            return assignment;
        }

        //Reads in categories from file, and assigns colors to all of them
        public List<ColorAssignment> AssignColors(String categoryFile)
        {
            List<Category> categories = ReadCategories(categoryFile);

            List<ColorAssignment> assignments = new List<ColorAssignment>();
            foreach (Category category in categories)
                assignments.Add(AssignColors(category));
                

            return assignments;
        }

        
        //Reading Category Files
        public List<Category> ReadCategories(String filename)
        {
            String[] lines = File.ReadAllLines(filename);
            List<Category> categories = new List<Category>();
            foreach (String line in lines)
            {
                String[] fields = line.Split(new string[] { "\",\"" }, StringSplitOptions.None);
                Category c = new Category(fields[1].Replace("\"", ""), fields[0].Replace("\"", "").Split('|'));
                categories.Add(c);
            }
            return categories;
        }

        public double GetCNDist(CIELAB a, CIELAB b)
        {
            int i = colorNames.GetBin(a);
            int j = colorNames.GetBin(b);
            if (CNcache[i, j] < 0)
            {
                double dist = 1 - colorNames.CosineDistance(i,j);
                CNcache[i, j] = dist;
                CNcache[j, i] = dist;
            }
            return CNcache[i, j];
        }

        private double[,] ComputeAffinities(Category category, double clipartPrior=0.7, double saturationThresh = 0.1, double sigma=0.2, double whiteThresh=20)
        {
            //get the color probabilities for regular and clipart queries
            double[,] pcw_regular = new double[paletteHex.Count(), category.members.Count()];
            double[,] pcw_clipart = new double[paletteHex.Count(), category.members.Count()];
            double[,] pcw = new double[paletteHex.Count(), category.members.Count()];


            Parallel.For(0, category.members.Count(), w =>
            {
                String query = Encode(category.members[w]);
                double[] pc_regular = GetProbabilities(query, GetCNDist, new GaussianKernel(sigma), whiteThresh);
                double[] pc_clipart = GetProbabilities(query+"_clipart", GetCNDist, new GaussianKernel(sigma), whiteThresh);

                for (int c=0; c<paletteLAB.Count(); c++)
                {
                    pcw_regular[c, w] = pc_regular[c];
                    pcw_clipart[c, w] = pc_clipart[c];
                }
            });

            Console.WriteLine("Done computing probabilities");

            //compute combined probability
            for (int w=0; w<category.members.Count(); w++)
            {
                
                double rentropy = 0;
                double centropy = 0;

                for (int c=0; c<paletteHex.Count(); c++)
                {
                    if (pcw_clipart[c,w] > 0)
                        centropy += pcw_clipart[c,w]*Math.Log(pcw_clipart[c,w]);
                    if (pcw_regular[c,w] > 0)
                        rentropy += pcw_regular[c,w]*Math.Log(pcw_regular[c,w]);
                }
                
                centropy *= -1;
                rentropy *= -1;

                //avoid divide by zero
                centropy = Math.Max(centropy, epsilon);
                rentropy = Math.Max(rentropy, epsilon);

                double cw = clipartPrior/centropy;
                double rw = (1-clipartPrior)/rentropy;

                for (int c=0; c<paletteHex.Count(); c++)
                {
                    CIELAB lab = paletteLAB[c];
                    double chroma = Math.Sqrt(lab.A*lab.A+lab.B*lab.B);
                    double saturation = chroma/Math.Max(Math.Sqrt(chroma*chroma+lab.L*lab.L), epsilon);

                    pcw[c, w] = Math.Max(saturation, saturationThresh)*(cw*pcw_clipart[c,w] + rw*pcw_regular[c,w]);
                }
            }

            //renormalize
            double[] memberSums = new double[category.members.Count()];
            for (int c=0; c<paletteHex.Count(); c++)
                for (int w=0; w<category.members.Count(); w++)
                    memberSums[w] += pcw[c,w];
            for (int c = 0; c < paletteHex.Count(); c++)
                for (int w = 0; w < category.members.Count(); w++)
                    pcw[c,w] /= memberSums[w];
                        
 
            //now compute affinities from the combined probabilities
            double[,] affinities = new double[paletteHex.Count(), category.members.Count()];


            double[,] pwc = new double[category.members.Count(), paletteHex.Count()];

            //compute p(w|c)
            //p(w|c) = p(c|w)*p(w)/p(c)
            double colorZ = 0;
            double[] colorSums = new double[paletteHex.Count()];
            for (int c=0; c < paletteHex.Count(); c++)
            {
                for (int w=0; w<category.members.Count(); w++)
                {
                    colorSums[c] += pcw[c,w];
                    colorZ += pcw[c,w];
                }
            }

            for (int c=0; c<paletteHex.Count(); c++)
                for (int w=0; w<category.members.Count(); w++)
                    pwc[w,c] = pcw[c,w]*(1.0/category.members.Count())/(Math.Max(colorSums[c],epsilon)/colorZ);

            double minScore = Double.PositiveInfinity;
            double maxScore = Double.NegativeInfinity;

            //balance with entropy H(w|c)         
            for (int c = 0; c < paletteHex.Count(); c++)
            {
                double Hwc = 0; 
                for (int w = 0; w < category.members.Count(); w++)
                {
                    if (pwc[w,c] > 0)
                        Hwc += pwc[w,c] * Math.Log(pwc[w,c]);
                }
                Hwc *= -1;
                System.Diagnostics.Debug.Assert(!double.IsNaN(Hwc));

                for (int w = 0; w < category.members.Count(); w++)
                {
                    affinities[c, w] = pcw[c, w] / Math.Max(Hwc, epsilon);
                    minScore = Math.Min(minScore, affinities[c, w]);
                    maxScore = Math.Max(maxScore, affinities[c, w]);
                }

            }
            System.Diagnostics.Debug.Assert(maxScore != minScore);

            //scale the affinities between 0 and 1s (easier to visualize)
            for (int c = 0; c < paletteHex.Count(); c++)
            {
                for (int w = 0; w < category.members.Count(); w++)
                {
                    affinities[c, w] = (affinities[c, w] - minScore) / (maxScore - minScore);
                    if (Double.IsNaN(affinities[c,w]))
                        throw new FormatException("Affinity is NaN! Affinity " + category.members[w] + " " + paletteHex[c] );
                }
            }

            Console.WriteLine("Done computing affinities");
            return affinities;
        }



       public Bitmap RenderAffinities(Category category)
       {
           double[,] hist = ComputeAffinities(category);

           int numMembers = category.members.Count();
           int colorSize = 10;
           int padding = 2;
           int textWidth = 200;
           int barSize = 100;
           int paddingBottom = 10;

           Bitmap result = new Bitmap(textWidth + paletteHex.Count() * colorSize, numMembers * barSize + padding+paddingBottom);
           Graphics hg = Graphics.FromImage(result);

           Brush black = new SolidBrush(Color.Black);
           Brush gray = new SolidBrush(Color.Gray);
           Font headers = new Font("Arial", 9); 

           hg.FillRectangle(new SolidBrush(Color.White), 0, 0, result.Width, result.Height);

           //write out the concept titles
           for (int w = 0; w < numMembers; w++)
           {
               hg.DrawString(category.members[w], headers, black, 0, w * barSize + barSize / 2);
           }

           //draw the bar charts
           for (int w = 0; w < numMembers; w++)
           {
               for (int c = 0; c < paletteHex.Count(); c++)
               {
                   int height = (int)Math.Round(hist[c, w] * (barSize - 5));
                   int offset = barSize - height;
                   hg.FillRectangle(new SolidBrush(paletteRGB[c]), c * colorSize + textWidth, w * barSize + offset, colorSize, height);
               }
               //draw a line
               hg.DrawLine(new Pen(gray), textWidth, (w + 1) * barSize, 20 * colorSize + textWidth, (w + 1) * barSize);
           }
           return result;
       }

        public Bitmap RenderAssignment(ColorAssignment assignment)
        {
            int numMembers = assignment.Keys.Count();
            int colorSize = 20;
            int padding = 2;
            int textWidth = 120;
            int topPadding = 10;

            Bitmap result = new Bitmap(textWidth + colorSize+padding, numMembers * colorSize + padding+topPadding);
            Graphics pg = Graphics.FromImage(result);

            pg.FillRectangle(new SolidBrush(Color.White), 0, 0, result.Width, result.Height);

            Brush black = new SolidBrush(Color.Black);
            Font headers = new Font("Arial", 9);

            //write out the concept titles
            for (int w = 0; w < numMembers; w++)
            {
                pg.DrawString(assignment.Keys[w], headers, black, 0, w * colorSize+topPadding);
            }
       
            //draw the assigned colors 
            for (int w = 0; w < assignment.Keys.Count(); w++)
            {
                int x = textWidth;
                int y = w * colorSize+topPadding;
                pg.FillRectangle(new SolidBrush(assignment.Get(assignment.Keys[w])), x, y, colorSize - padding, colorSize - padding);
            }

            return result;
        }

        /**
         * Removing whitespaces when saving cached computations
         * */
        private String Encode(String raw)
        {
            return raw.Trim().Replace(" ","_").Replace("/","_").Replace("\\","_").ToLower();
        }

        /**
         * Get images if needed
         * */
        private void SearchImages(Category category, int pages=3, bool ifNeeded = true)
        {
            int thresh = 10;
            foreach (String member in category.members)
            {
                GoogleImageSearch engine = new GoogleImageSearch(apiKey, cxId);

                String[] labels = { "", "_clipart" };

                foreach (String label in labels)
                {

                    String dirName = Path.Combine(imageDir, Encode(member) + label);
                    
                    //images have been downloaded already
                    if (Directory.Exists(dirName) && Directory.GetFiles(dirName).Count() >= thresh)
                    {
                        Console.WriteLine("Already have images for " + member + label.Replace("_"," "));
                        continue;
                    }
                    
                    List<String> urls = engine.Search(member+label.Replace("_"," "), pages);

                    Directory.CreateDirectory(dirName);

                    //save the images
                    for (int i = 0; i < urls.Count(); i++)
                    {
                        Bitmap image = Util.BitmapFromWeb(urls[i]);

                        if (image == null)
                            continue;

                        image.Save(Path.Combine(dirName, i + ".png"));
                        image.Dispose();
                    }

                }
            }
            Console.WriteLine("Done getting images");
        }


        /**
         * Compute histograms for the given category, both clipart and regular
         **/
        private void ComputeHistograms(Category category, bool loadIfPossible=true)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            Parallel.For(0, category.members.Count(), m =>
            {
                String member = category.members[m];

                //Get the relevant images
                String[] queryLabels = { "", "_clipart" };

                foreach (String label in queryLabels)
                {
                    String baseName = Encode(member) + label;
                    String outFile = Path.Combine(cacheDir, baseName + ".txt");
                    String memberDir = Path.Combine(imageDir, baseName);

                    if (File.Exists(outFile) && loadIfPossible)
                    {
                        Console.WriteLine("Reusing histogram for " + baseName);
                        continue;
                    }

                    double[, ,] histogram = new double[Lbins, Abins, Bbins];
                    String[] files = Directory.GetFiles(memberDir);

                    foreach (String f in files)
                    {
                        //TODO: maybe process the image here, or filter it out
                        try
                        {

                            Bitmap image = ResizeImage(f);

                            Color[,] imageRGB = Util.BitmapToArray(image);
                            CIELAB[,] imageLAB = Util.Map<Color, CIELAB>(imageRGB, Util.RGBtoLAB);

                            //Process the image
                            bool valid = ProcessImage(imageRGB, imageLAB);
                            if (!valid)
                                continue;

                            double pixelCount = 0;
                            for (int i = 0; i < image.Width; i++)
                            {
                                for (int j = 0; j < image.Height; j++)
                                {
                                    if (imageRGB[i, j].A >= 1)
                                    {
                                        pixelCount++;
                                    }
                                }
                            }

                            if (pixelCount > 0)
                            {
                                for (int i = 0; i < image.Width; i++)
                                {
                                    for (int j = 0; j < image.Height; j++)
                                    {
                                        if (imageRGB[i, j].A <= 1)
                                            continue;

                                        CIELAB lab = imageLAB[i, j];//Util.RGBtoLAB(imageRGB[i,j]);

                                        //update histogram
                                        int L = (int)Math.Floor((lab.L / (double)binSize) + 0.5);
                                        int A = (int)Math.Floor(((100 + lab.A) / (double)binSize) + 0.5);
                                        int B = (int)Math.Floor(((100 + lab.B) / (double)binSize) + 0.5);

                                        L = clamp(L, 0, Lbins - 1);
                                        A = clamp(A, 0, Abins - 1);
                                        B = clamp(B, 0, Bbins - 1);

                                        histogram[L, A, B] += 1.0 / pixelCount;
                                    }
                                }
                            }

                            //cleanup
                            image.Dispose();
                        }
                        catch (Exception)
                        {
                            Console.WriteLine("Could not process image " + f);
                        }

                    }

                    //save the histogram to a file
                    List<String> lines = new List<String>();

                    for (int i = 0; i < Lbins; i++)
                    {
                        for (int j = 0; j < Abins; j++)
                        {
                            for (int k = 0; k < Bbins; k++)
                            {
                                lines.Add(histogram[i, j, k].ToString());
                            }
                        }
                    }
                    File.WriteAllLines(outFile, lines.ToArray<String>());
                }
            });
            watch.Stop();
            Console.WriteLine("Done with histograms Time: " + watch.ElapsedMilliseconds / 1000.0);
        }

        private double[] GetProbabilities(String query, Func<CIELAB, CIELAB, double> distFunc, Kernel kernel, double whiteThresh=20)
        {
            CIELAB white = new CIELAB(100, 0, 0);

            //load the histogram
            double[, ,] hist = new double[Lbins, Abins, Bbins];
            String histFile = Path.Combine(cacheDir, query)+".txt";

            String[] hlines = File.ReadAllLines(histFile);
            for (int i = 0; i < hlines.Count(); i++)
            {
                int L = i / (Abins * Bbins);
                int plane = (i % (Abins * Bbins));
                int A = plane / Bbins;
                int B = plane % Bbins;

                hist[L, A, B] = Double.Parse(hlines[i]);
            }
                    
            int ncolors = paletteLAB.Count();
            double[] freq = new double[20];

            Parallel.For(0, ncolors, l =>
            {
                double count = 0;

                for (int i = 0; i < Lbins; i++)
                {
                    for (int j = 0; j < Abins; j++)
                    {
                        for (int k = 0; k < Bbins; k++)
                        {
                            double val = hist[i, j, k];

                            System.Diagnostics.Debug.Assert(!double.IsNaN(val));

                            CIELAB lab = new CIELAB(i * binSize, j * binSize - 100, k * binSize - 100);

                            if (white.SqDist(lab) < whiteThresh * whiteThresh)
                                continue;
                            if (val <= 0)
                                continue;

                            count += val;


                            freq[l] += val * kernel.Eval(distFunc(paletteLAB[l], lab));
                        }
                    }
                }
                if (count > 0)
                {
                    freq[l] /= count;
                }
            });

            //now renormalize
            double totalFreq = 0;
            for (int i=0; i<freq.Count(); i++)
                totalFreq += freq[i];

            //this should only happen if the histogram is empty or has no valid bins
            if (totalFreq == 0)
                totalFreq = 1;

            for (int i=0; i<freq.Count(); i++)
                freq[i] /= totalFreq;

            return freq;
        }


        private int clamp(int value, int min, int max)
        {
            return Math.Max(min, Math.Min(value, max));
        }


        ///Image processing methods
        
        Bitmap ResizeImage(String file, int maxDim=150)
        {
            Image orig = Image.FromFile(file);

            int width = orig.Width;
            int height = orig.Height;

            if (orig.Width > maxDim || orig.Height > maxDim)
            {
                width = maxDim;
                height = maxDim;
                if (orig.Width > orig.Height)
                {
                    width = maxDim;
                    height = maxDim * orig.Height/orig.Width;
                }
                else
                {
                    height = maxDim;
                    width = maxDim * orig.Width/orig.Height;
                }
            }

            Bitmap image = new Bitmap(orig, width, height);
            orig.Dispose();
            return image;

        }



        private bool ProcessImage(Color[,] image, CIELAB[,] imageLAB)
        {
            double thresh = 5;
            UnionFind<CIELAB> uf = new UnionFind<CIELAB>((a,b)=>(a.SqDist(b)<=thresh));
            int[,] assignments = uf.ConnectedComponents(imageLAB);//Util.Map<Color, CIELAB>(image, Util.RGBtoLAB));
            int numC = -1;
            for(int i=0; i<image.GetLength(0); i++)
                for (int j=0; j<image.GetLength(1); j++)
                    numC = Math.Max(numC, assignments[i,j]+1);
            if (numC >= 2)
                RemoveBackground(image, imageLAB);

            //if it is a black and white image (with num connected components >= 2), it's not a valid color image
            return !(isBlackWhite(image, imageLAB) && numC >= 2);

        }

        private bool isBlackWhite(Color[,] image, CIELAB[,] imageLAB)
        {
            double nonGray = 0;
            double thresh = 0.001;
            CIELAB white = new CIELAB(100, 0, 0);
            CIELAB black = new CIELAB(0,0,0);

            int width = image.GetLength(0);
            int height = image.GetLength(1);

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    Color color = image[i, j];
                    CIELAB lab = imageLAB[i, j];//Util.RGBtoLAB(color);
                    bool gray = color.GetSaturation() <= 0.2 || lab.SqDist(white) <= 5 || lab.SqDist(black) <= 5;

                    if (!gray)
                        nonGray++;
                }
            }
            return nonGray/(width*height) < thresh;
        }


        private void RemoveBackground(Color[,] image, CIELAB[,] imageLAB)
        {
            //check perimeter to see if it's mostly black or white

            //RGB to LAB
            CIELAB black = new CIELAB(0, 0, 0);
            CIELAB white = new CIELAB(100, 0, 0);

            int width = image.GetLength(0);
            int height = image.GetLength(1);

            CIELAB[,] labs = imageLAB;//Util.Map<Color, CIELAB>(image, (c) => Util.RGBtoLAB(c));

            int numBlack = 0;
            int numWhite = 0;
            int thresh = 3 * 3;
            List<Point> perimeterIdx = new List<Point>();
            double totalPerimeter = 4 * width + 4 * height;
            double bgThresh = totalPerimeter*0.75;

            for (int i = 0; i < width; i++)
            {
                //top
                for (int j = 0; j < 2; j++)
                {
                    if (black.SqDist(labs[i, j])<thresh)
                        numBlack++;
                    if (white.SqDist(labs[i, j]) < thresh)
                        numWhite++;
                    perimeterIdx.Add(new Point(i, j));
                }

                //bottom
                for (int j = height - 2; j < height; j++)
                {
                    perimeterIdx.Add(new Point(i, j));
                    if (black.SqDist(labs[i, j]) < thresh)
                        numBlack++;
                    if (white.SqDist(labs[i, j]) < thresh)
                        numWhite++;
                }
            }

            for (int j = 0; j < height; j++)
            {
                //left
                for (int i=0; i<2; i++)
                {
                    perimeterIdx.Add(new Point(i,j));
                    if (black.SqDist(labs[i, j]) < thresh)
                        numBlack++;
                    if (white.SqDist(labs[i, j]) < thresh)
                        numWhite++;
                }

                //right
                for (int i=width-2; i<width; i++)
                {
                    perimeterIdx.Add(new Point(i, j));
                    if (black.SqDist(labs[i, j]) < thresh)
                        numBlack++;
                    if (white.SqDist(labs[i, j]) < thresh)
                        numWhite++;
                }
            }

            if (numBlack >= bgThresh || numWhite >= bgThresh)
            {
                //connected components
                UnionFind<CIELAB> uf = new UnionFind<CIELAB>((a,b) => a.SqDist(b)<thresh);
                int[,] cc = uf.ConnectedComponents(labs);

                SortedSet<int> ids = new SortedSet<int>();

                //go around the perimeter to collect the right ids
                foreach (Point p in perimeterIdx)
                {
                    if (numWhite > numBlack)
                    {
                        if (labs[p.X, p.Y].SqDist(white) < thresh)
                            ids.Add(cc[p.X, p.Y]);
                    }
                    else
                    {
                        if (labs[p.X, p.Y].SqDist(black) < thresh)
                            ids.Add(cc[p.X, p.Y]);
                    }
                }

                //fill the bitmap with transparency
                for (int i = 0; i < width; i++)
                {
                    for (int j = 0; j < height; j++)
                    {
                        if (ids.Contains(cc[i, j]))
                            image[i, j] = Color.FromArgb(0, 0, 0, 0);
                    }
                }
            }
        }




    }
}
