using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using System.IO;


namespace Engine
{
    public class HungarianAlgorithm
    {
        //Implement the Hungarian Algorithm, the Matrix version
        //From the description on: http://www.wikihow.com/Use-the-Hungarian-Algorithm
        //Returns a list of assignments from x to y
        public static List<int> Solve(double[,] origMatrix, bool test = false, bool print = false)
        {
            //int n = b.Count();
            List<int> assignments = new List<int>();

            //bool print = false;

            //fill the matrix
            int n = origMatrix.GetLength(0);
            double[,] matrix = new double[n, n];
            Array.Copy(origMatrix, matrix, origMatrix.Length);

            if (test)
            {
                //use test matrix
                n = 4;
                matrix = new double[4, 4]{
                {90,75, 75, 80},
                {35, 85, 55, 65},
                {125, 95, 90, 105},
                {45, 110, 95, 115}};

            }

            if (print)
                PrintMatrices(matrix, new int[n, n]);

            //go through the matrix steps
            int maxIters = 500;

            //Step 1 - subtract min row weight from each row
            for (int i = 0; i < n; i++)
            {
                double rowMin = RowMin(matrix, i);
                for (int j = 0; j < n; j++)
                {
                    matrix[i, j] -= rowMin;
                }
            }
            if (print)
                PrintMatrices(matrix, new int[n, n]);


            //Step 2 - subtract min column weight from each column
            for (int j = 0; j < n; j++)
            {
                double colMin = ColumnMin(matrix, j);
                for (int i = 0; i < n; i++)
                {
                    matrix[i, j] -= colMin;
                }
            }


            //Step 3 - cover the zero elements with a minimum number of lines
            for (int iter = 0; iter < maxIters; iter++)
            {
                int[,] covered = new int[n, n];
                List<int> rows = new List<int>();
                List<int> cols = new List<int>();

                if (print)
                    PrintMatrices(matrix, covered);
                int numLines = CoverZeroElements(matrix, ref covered, rows, cols);
                int uncovered = NumUncoveredZeros(matrix, covered);

                if (uncovered > 0)
                    Console.WriteLine("There is more than 1 uncovered zero!");

                if (print)
                    PrintMatrices(matrix, covered);

                //Step 4
                //if the number of lines equals the number of rows, then find the matching and end
                if (numLines == n)
                {
                    //find matching
                    int[,] matching = new int[n, n];
                    FindMatching(matrix, ref matching);
                    if (print)
                        PrintMatrices(matrix, matching);

                    //Now update the matching
                    //a to b
                    for (int i = 0; i < n; i++)
                    {
                        for (int j = 0; j < n; j++)
                        {
                            if (matching[i, j] == 1)
                            {
                                assignments.Add(j);
                                break;
                            }
                        }
                    }


                    //Check that these are unique, otherwise we have a problem
                    List<int> unique = assignments.Distinct<int>().ToList<int>();
                    Debug.Assert(unique.Count() == assignments.Count());

                    break;
                }
                else
                {
                    //Otherwise, find the minimum uncovered element, and add it
                    //to every covered element, then subtract it
                    //from every element

                    //find the minimum uncovered element
                    double minUncovered = double.PositiveInfinity;
                    for (int i = 0; i < n; i++)
                        for (int j = 0; j < n; j++)
                            if (covered[i, j] == 0 && matrix[i, j] < minUncovered)
                                minUncovered = matrix[i, j];

                    //add it to every covered element, add it twice to every twice-covered element
                    foreach (int row in rows)
                        for (int j = 0; j < n; j++)
                            matrix[row, j] += minUncovered;

                    foreach (int col in cols)
                        for (int i = 0; i < n; i++)
                            matrix[i, col] += minUncovered;

                    //subtract it from all elements
                    for (int i = 0; i < n; i++)
                        for (int j = 0; j < n; j++)
                            matrix[i, j] -= minUncovered;
                    if (print)
                        PrintMatrices(matrix, covered);
                    //go back to the beginning of step 3
                }
            }


            return assignments;
        }


        private static void FindMatching(double[,] matrix, ref int[,] matching)
        {
            //go through the matrix and make as many unique matchings as possible
            //first by row then by columns, then by rows etc
            int n = matrix.GetLength(0);

            int numAssignments = 1;
            List<int> rowCandidates = new List<int>();
            List<int> colCandidates = new List<int>();
            for (int i = 0; i < n; i++)
            {
                rowCandidates.Add(i);
                colCandidates.Add(i);
            }

            int totalAssignments = 0;
            while (numAssignments > 0 && totalAssignments < n)
            {
                numAssignments = 0;
                List<int> rowRemove = new List<int>();
                List<int> colRemove = new List<int>();

                foreach (int i in rowCandidates)
                {
                    int numZeroes = 0;
                    int idx = -1;
                    foreach (int j in colCandidates)
                    {
                        if (matrix[i, j] == 0)
                        {
                            numZeroes++;
                            idx = j;
                        }
                    }
                    if (numZeroes == 1 && !colRemove.Contains(idx))
                    {
                        matching[i, idx] = 1;
                        numAssignments++;

                        //remove row and column
                        rowRemove.Add(i);
                        colRemove.Add(idx);
                        break;
                    }
                }

                //remove row and column
                foreach (int i in rowRemove)
                    rowCandidates.Remove(i);
                foreach (int j in colRemove)
                    colCandidates.Remove(j);

                //check
                if (numAssignments == n)
                    break;

                //otherwise, do columns
                foreach (int j in colCandidates)
                {
                    int numZeroes = 0;
                    int idx = -1;
                    foreach (int i in rowCandidates)
                    {
                        if (matrix[i, j] == 0)
                        {
                            numZeroes++;
                            idx = i;
                        }
                    }
                    if (numZeroes == 1 && !rowRemove.Contains(idx))
                    {
                        matching[idx, j] = 1;
                        numAssignments++;

                        rowRemove.Add(idx);
                        colRemove.Add(j);
                        break;
                    }
                }

                //remove row and column
                foreach (int i in rowRemove)
                    rowCandidates.Remove(i);
                foreach (int j in colRemove)
                    colCandidates.Remove(j);

                totalAssignments += numAssignments;

                if (numAssignments == 0 && totalAssignments < n)
                {
                    //make an arbitrary assignment, then continue
                    foreach (int j in colCandidates)
                    {
                        int numZeroes = 0;
                        int idx = -1;
                        bool found = false;
                        foreach (int i in rowCandidates)
                        {
                            if (matrix[i, j] == 0)
                            {
                                numZeroes++;
                                idx = i;
                                matching[idx, j] = 1;
                                numAssignments++;

                                rowRemove.Add(idx);
                                colRemove.Add(j);
                                found = true;
                                break;
                            }
                        }
                        if (found)
                            break;
                    }

                    //remove row and column
                    foreach (int i in rowRemove)
                        rowCandidates.Remove(i);
                    foreach (int j in colRemove)
                        colCandidates.Remove(j);
                }

            }

            //we're done!

        }


        //cover the matrix with the line
        private static void CoverMatrix(ref int[,] covered, int line)
        {
            int n = covered.GetLength(0);

            if (line > 0)
            {
                int row = line - 1;
                for (int j = 0; j < n; j++)
                {
                    covered[row, j] = 1;
                }
            }
            else
            {
                int column = -1 * line - 1;
                for (int i = 0; i < n; i++)
                {
                    covered[i, column] = 1;
                }

            }

        }


        private static int CoverZeroElements(double[,] matrix, ref int[,] covered, List<int> rows, List<int> cols)
        {
            //identify rows and columns with zero elements
            //assuming nonzero elements
            int n = matrix.GetLength(0); //assume square matrix

            //Do it the Wikipedia way
            //find a matching
            int[,] matching = new int[n, n];
            FindMatching(matrix, ref matching);
            //PrintMatrices(matrix, matching);

            //find unmatched rows
            //mark all rows having no assignments, then mark
            //all columns having 0s in those rows
            List<int> markedRows = new List<int>();
            List<int> markedCols = new List<int>();

            //mark rows with no assignments
            for (int i = 0; i < n; i++)
            {
                bool assignmentExists = false;
                for (int j = 0; j < n; j++)
                {
                    if (matching[i, j] == 1)
                    {
                        assignmentExists = true;
                        break;
                    }
                }
                if (!assignmentExists)
                {

                    markedRows.Add(i);
                }
            }
            int newMarks = markedRows.Count();

            List<int> newMarkedCols = new List<int>();
            List<int> newMarkedRows = new List<int>(markedRows);

            while (newMarks > 0)
            {
                newMarks = 0;

                //Now mark the columns having 0s in the marked rows
                foreach (int i in newMarkedRows)
                    for (int j = 0; j < n; j++)
                        if (matrix[i, j] == 0 && !markedCols.Contains(j) && !newMarkedCols.Contains(j))
                            newMarkedCols.Add(j);

                //update
                foreach (int j in newMarkedCols)
                    markedCols.Add(j);

                newMarks += newMarkedRows.Count();
                newMarkedRows = new List<int>();

                //Now mark the rows having assignments in the marked columns
                foreach (int j in newMarkedCols)
                    for (int i = 0; i < n; i++)
                        if (matching[i, j] == 1 && !markedRows.Contains(i) && !newMarkedRows.Contains(i))
                            newMarkedRows.Add(i);

                //update
                foreach (int i in newMarkedRows)
                    markedRows.Add(i);

                //refresh
                newMarks += newMarkedCols.Count();
                newMarkedCols = new List<int>();

                if (newMarks == 0)
                    break;

            }

            if (markedCols.Distinct<int>().Count() != markedCols.Count())
                Console.WriteLine("Something wrong, duplicated marked columns!");

            //Now draw lines through the marked columns and the unmarked rows
            foreach (int j in markedCols)
                cols.Add(j);

            for (int i = 0; i < n; i++)
                rows.Add(i);
            foreach (int i in markedRows)
                rows.Remove(i);

            foreach (int j in cols)
                CoverMatrix(ref covered, -(j + 1));
            foreach (int i in rows)
                CoverMatrix(ref covered, i + 1);

            return rows.Count() + cols.Count();
        }


        private static void PrintMatrices(double[,] matrix, int[,] covered)
        {
            int n = matrix.GetLength(0);
            Console.WriteLine();

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    //print out the matrix
                    if (covered[i, j] == 1)
                        Console.Write("XX.xx");
                    else
                    {
                        if (matrix[i, j] != 0)
                            Console.Write(String.Format("{0:00.00}", matrix[i, j]));
                        else
                            Console.Write(" zero");
                    }
                    if (j < n - 1)
                        Console.Write(", ");
                    else
                        Console.Write("\n");
                }
            }
            Console.WriteLine();

        }

        private static int NumUncoveredZeros(double[,] matrix, int[,] covered)
        {
            int n = matrix.GetLength(0);
            int uncovered = 0;

            //count number of uncovered zeros
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (matrix[i, j] == 0 && covered[i, j] == 0)
                        uncovered++;
                }
            }


            return uncovered;
        }



        //find min weight in the specifie
        private static double RowMin(double[,] matrix, int index)
        {
            double min = double.PositiveInfinity;
            int m = matrix.GetLength(0);
            int n = matrix.GetLength(1);
            Debug.Assert(index < m);


            for (int j = 0; j < n; j++)
            {
                min = Math.Min(matrix[index, j], min);
            }

            return min;
        }

        private static double ColumnMin(double[,] matrix, int index)
        {
            double min = double.PositiveInfinity;
            int m = matrix.GetLength(0);
            int n = matrix.GetLength(1);

            Debug.Assert(index < n);

            for (int i = 0; i < m; i++)
            {
                min = Math.Min(matrix[i, index], min);
            }
            return min;
        }
    }
}
