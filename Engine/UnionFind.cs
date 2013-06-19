using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace Engine
{
    //Simple Union Find for images
    public class UnionFind<T>
    {
        int[] parents;
        Func<T, T, bool> compare;
       
        public UnionFind(Func<T,T,bool> comp)
        {
            compare = comp;
        }


        public Bitmap RenderComponents(int[,] assignments, T[,] image, T selection)
        {
            //render the components
            int width = assignments.GetLength(0);
            int height = assignments.GetLength(1);

            Dictionary<int, Color> idToColor = new Dictionary<int, Color>();


            Bitmap result = new Bitmap(width, height);

            Random random = new Random();

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    if (compare(image[i, j],selection) && assignments[i,j]>=0)
                    {
                        int id = assignments[i, j];
                        if (!idToColor.ContainsKey(id))
                            idToColor.Add(id, Color.FromArgb(random.Next(0, 256), random.Next(0, 256), random.Next(256)));
                        result.SetPixel(i, j, idToColor[id]);
                    }
                    else
                    {
                        result.SetPixel(i, j, Color.White);
                    }
                }
            }

            return result;

        }

        public Bitmap RenderComponents(int[,] assignments)
        {
            //render the components
            int width = assignments.GetLength(0);
            int height = assignments.GetLength(1);

            Dictionary<int, Color> idToColor = new Dictionary<int, Color>();

            Bitmap result = new Bitmap(width, height);

            Random random = new Random();

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    int id = assignments[i, j];

                    if (id >= 0)
                    {
                        if (!idToColor.ContainsKey(id))
                            idToColor.Add(id, Color.FromArgb(random.Next(0, 256), random.Next(0, 256), random.Next(256)));
                        result.SetPixel(i, j, idToColor[id]);
                    }
                    else
                    {
                        result.SetPixel(i, j, Color.White);
                    }
                }
            }

            return result;

        }

        public int[,] ConnectedComponentsNoiseRemoval(T[,] image, double thresh = 0.00050)
        {
            
            int width = image.GetLength(0);
            int height = image.GetLength(1);

            //connected components with neighbor dist 1
            int[,] assignments = ConnectedComponents(image, 1);
            assignments = RemoveNoise(image, assignments, thresh);

            int[] status = AssignmentsToParents(assignments);
            assignments = ConnectedComponents(image, 2, status);

            return assignments;
        }

        private int[] AssignmentsToParents(int[,] assignments)
        {
            int width = assignments.GetLength(0);
            int height = assignments.GetLength(1);

            int[] p = new int[width*height];
            Dictionary<int, int> numToParent = new Dictionary<int, int>();

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    int idx = PointToIndex(new Point(i, j), width);
                    int num = assignments[i, j];
                    if (!numToParent.ContainsKey(num))
                    {
                        if (num < 0)
                            numToParent.Add(num, num);
                        else
                            numToParent.Add(num, idx);
                    }
                    p[idx] = numToParent[num];
                }
            }

            return p;

        }


        public int[,] RemoveNoise(T[,] image, int[,] assignments, double thresh = 0.00050)
        {
            //merge noise with component size <= thresh
            //calculate sizes of segments
            Dictionary<int, int> segToCompSize = new Dictionary<int, int>();

            int width = image.GetLength(0);
            int height = image.GetLength(1);

            parents = AssignmentsToParents(assignments);

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    
                    int idx = Find(PointToIndex(new Point(i,j), width));
                    if (!segToCompSize.ContainsKey(idx))
                        segToCompSize.Add(idx, 0);
                    segToCompSize[idx]++;

                }
            }
            
            //get the candidate segments to merge
            double absthresh = thresh * width * height;
            List<int> candidates = new List<int>();
            foreach (int id in segToCompSize.Keys)
                if (segToCompSize[id] <= absthresh)
                    candidates.Add(id);

            for (int i = 0; i < candidates.Count(); i++)
            {
                for (int j = 0; j < candidates.Count(); j++)
                {
                    Union(candidates[i], candidates[j], image);
                }
            }


            SortedSet<int> candidateRoots = new SortedSet<int>();
            //set candidates to negative numbers (to remove them from consideration)
            for (int i = 0; i < candidates.Count(); i++)
            {
                int cidx = Find(candidates[i]);
                if (cidx>=0 && !candidateRoots.Contains(cidx))
                {
                    candidateRoots.Add(cidx);
                    parents[cidx] = -1 * candidateRoots.Count();
                }
            }

            int[,] result = new int[width, height];
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    int idx = PointToIndex(new Point(i, j), width);
                    int fidx = Find(idx);
                    result[i, j] = fidx;
                }
            }
            return result;
        }

        //8-connected
        public int[,] ConnectedComponents(T[,] image, T ignore, int neighborDist = 1, int[] initParents = null)
        {
            int width = image.GetLength(0);
            int height = image.GetLength(1);

            int totalSize = width * height;

            if (initParents == null)
            {
                parents = new int[totalSize];

                //initialize each pixel to its own component
                for (int i = 0; i < totalSize; i++)
                {
                    Point p = IndexToPoint(i, width);
                    if (compare(image[p.X, p.Y], ignore))
                        parents[i] = -1;
                    else
                        parents[i] = i;
                }

            }
            else
            {
                parents = initParents;
            }

            //now start merging components
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    int id = PointToIndex(new Point(i, j), width);

                    //check the 8 neighbors
                    for (int dx = -neighborDist; dx <= neighborDist; dx++)
                    {
                        for (int dy = -neighborDist; dy <= neighborDist; dy++)
                        {
                            int x = i + dx;
                            int y = j + dy;
                            if (x >= 0 && x < width && y >= 0 && y < height)
                            {
                                int nid = PointToIndex(new Point(x, y), width);
                                Union(id, nid, image);
                            }
                        }
                    }

                }
            }

            int[,] result = new int[width, height];

            //relabel the result with the root ids, renumbered
            int[] vals = parents.Distinct().ToArray();
            Dictionary<int, int> renumber = new Dictionary<int, int>();
            int counter = 0;

            renumber.Add(-1, -1);
            foreach (int v in vals)
            {
                if (!renumber.ContainsKey(v))
                    renumber.Add(v, counter++);
            }

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    int id = PointToIndex(new Point(i, j), width);
                    result[i, j] = renumber[Find(id)];
                }
            }


            return result;
        }

        public int[,] ConnectedComponents(T[,] image, int neighborDist=1, int[] initParents=null)
        {
            int width = image.GetLength(0);
            int height = image.GetLength(1);

            int totalSize = width * height;

            if (initParents == null)
            {
                parents = new int[totalSize];

                //initialize each pixel to its own component
                for (int i = 0; i < totalSize; i++)
                    parents[i] = i;

            }
            else
            {
                parents = initParents;
            }

            //now start merging components
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    int id = PointToIndex(new Point(i, j), width);
                  
                    //check the 8 neighbors (to 2 pixels)
                    for (int dx = -neighborDist; dx <= neighborDist; dx++)
                    {
                        for (int dy = -neighborDist; dy <= neighborDist; dy++)
                        {
                            int x = i + dx;
                            int y = j + dy;
                            if (x >= 0 && x < width && y >= 0 && y < height)
                            {
                                int nid = PointToIndex(new Point(x,y), width);
                                Union(id, nid, image);
                            }
                        }
                    }

                }
            }

            int[,] result = new int[width, height];

            //relabel the result with the root ids, renumbered
            int[] vals = parents.Distinct().ToArray();
            Dictionary<int, int> renumber = new Dictionary<int, int>();
            int counter = 0;
            foreach (int v in vals)
            {
                if (!renumber.ContainsKey(v))
                    if (v < 0)
                        renumber.Add(v, v);
                    else
                        renumber.Add(v, counter++);
            }

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    int id = PointToIndex(new Point(i, j), width);
                    result[i, j] = renumber[Find(id)];
                }
            }


            return result;
        }

        private Point IndexToPoint(int idx, int width)
        {
            Point result = new Point();
            result.X = idx % width;
            result.Y = idx / width;
            return result;
        }

        private int PointToIndex(Point p, int width)
        {
            int idx = p.Y * width + p.X;
            return idx;
        }

        

        private bool Union(int aid, int bid, T[,] image)
        {
            //union the two if they can be unioned
            int width = image.GetLength(0);
            Point a = IndexToPoint(aid, width);
            Point b = IndexToPoint(bid, width);

            //ignore background
            if (Find(aid)<0 || Find(bid)<0) 
                return false;

            //already unioned
            if (Find(aid) == Find(bid))
                return false;

            //not equal, and so cannot be unioned
            if (!compare(image[a.X, a.Y], image[b.X, b.Y]))
                return false;
            
            //now union the two
            int pa = Find(aid);
            int pb = Find(bid);

            if (pa < pb)
                parents[pb] = pa;
            else
                parents[pa] = pb;

            return true;
        }

        private int Find(int id)
        {
            int p = id;

            if (p < 0 )
                return p;

            if (parents[p] < 0)
                return parents[p];

            if (p == parents[p])
                return p;

            int parentId = parents[p];
            while (parentId != parents[parentId])
            {
                parentId = parents[parentId];
                if (parentId < 0)
                    return parentId;
            }
            //compress the path
            parents[parentId] = parentId;

            return parentId;
        }

    }
}
