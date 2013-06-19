using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Diagnostics;
using Engine;

namespace SemanticColors
{
    class Program
    {
        static void Main(string[] args)
        {
            String imageDir = "";
            String jsonFile = "";
            String cacheDir = "";
            String key = "";
            String cx = "";

            if (args.Length== 3 || args.Length == 4)
            {                
                String inFile = args[0];
                String outFile = args[1];
                String paramsFile = args[2];

                //read in the params file
                String[] paramLines = File.ReadAllLines(paramsFile);
                foreach (String line in paramLines)
                {
                    String[] fields = line.Trim().Split('>');
                    String field = fields[0].Trim();
                    String value = fields[1].Trim();

                    switch (field)
                    {
                        case "imageDir":
                            imageDir = value;
                            break;
                        case "jsonFile":
                            jsonFile = value;
                            break;
                        case "cacheDir":
                            cacheDir = value;
                            break;
                        case "apiKey":
                            key = value;
                            break;
                        case "cx":
                            cx = value;
                            break;
                        default:
                            Console.WriteLine("paramsFile error: don't recognize " + field);
                            break;
                    }

                }

                Stopwatch watch = new Stopwatch();
                watch.Start();
                ColorAssigner assigner = new ColorAssigner(imageDir, cacheDir, jsonFile, key, cx);
                List<ColorAssignment> assignments = assigner.AssignColors(inFile);
                watch.Stop();

                //write the assignments to file
                List<String> lines = new List<String>();
                foreach (ColorAssignment a in assignments)
                {
                    String members = String.Join("|", a.category.members.ToArray<String>());
                    String[] colors = a.category.members.Select<String, String>(m => ColorTranslator.ToHtml(a.Get(m))).ToArray<String>();
                    String colorString = String.Join("|", colors);

                    lines.Add(String.Format("\"{0}\",\"{1}\",\"{2}\"", members, a.category.title, colorString));
                }
                File.WriteAllLines(outFile, lines.ToArray<String>());

                if (args.Length == 4 && args[3] != "")
                {
                    String renderDir = args[3].Trim();
                    Directory.CreateDirectory(renderDir);

                    //render the assignments
                    foreach (ColorAssignment a in assignments)
                    {
                        Bitmap image = assigner.RenderAssignment(a);
                        Bitmap hist = assigner.RenderAffinities(a.category);
                        image.Save(Path.Combine(renderDir, a.category.title + "_assgn.png"));
                        hist.Save(Path.Combine(renderDir, a.category.title + "_aff.png"));
                        image.Dispose();
                        hist.Dispose();
                    }
                }
                Console.WriteLine("Done. Assignment time " + watch.ElapsedMilliseconds / 1000.0);

            }
            else
            {
                Console.WriteLine("SemanticColors [inFile] [outFile] [paramsFile] [renderDir]");
                Console.WriteLine("inFile - file listing categories to assign colors");
                Console.WriteLine("outFile - file to output color assignments");
                Console.WriteLine("paramsFile - file with filepaths to image dir, color names json file, cache dir, api key, and custom search engine id");
                Console.WriteLine("renderDir - (optional) directory to save images of the color assignments and histogram");

            }
        }
    }
}
