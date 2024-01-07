using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Diagnostics;
using Excel = Microsoft.Office.Interop.Excel;
using System.IO;

namespace OCSRMCSLA
{
    class Program
    {
        static void Main(string[] args)
        {
            int InsNum = 4;
            int ch = 1;
            int TimeLimit = 60000000; // available time (ms)

            bool PrintSeq = false;       //show the sequence or not?
            Random rand = new Random();

            instance ObjIns1;
            Console.BufferHeight = 10000; 
            System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US"); 

            // info of random instance
            NemudInfo InsInfo = new NemudInfo();
            {
                InsInfo.i = 570; //the thotal number of coils
                InsInfo.inp = 70;
                InsInfo.outp = 70;
                InsInfo.m = 2;
                InsInfo.h = 1;
                InsInfo.t = 10;
                InsInfo.r = 10;
                InsInfo.n = 99;
                InsInfo.v1 = 1;
                InsInfo.v2 = 1;
                InsInfo.l1 = 1;
                InsInfo.l2 = 1;
                InsInfo.mu = 0.5;
                InsInfo.RowDisUnit = 4;
                InsInfo.ColDisUnit = 2;
                InsInfo.RowDisUnit_2 = 5;
                InsInfo.ColDisUnit_2 = 4;
                InsInfo.safeD = 1;

            }
            // ch=1: solve the instance InsNum
            // ch=2: generate random instance with the characteristics determined above in InsInfo
            // ch=3: generate instances in batch mode
            // ch=4: solve instances in batch mode
            switch (ch)
            {
                case 1:
                    ObjIns1 = ReadInstance(InsNum);
                    Strategies(ObjIns1, InsNum, PrintSeq, TimeLimit, rand);
                    break;
                case 2:
                    ObjIns1 = CreatInstance(InsNum, InsInfo, rand);
                    ObjIns1.WriteData(InsNum);
                    break;
                case 3:
                    BatchInstancesGenerator(rand);
                    break;
                case 4:
                    BatchRun(TimeLimit, rand);
                    break;
            }
            Console.BackgroundColor = ConsoleColor.DarkRed;
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("\n\n<< Press any key to exit . . . >>");
            Console.ReadKey();
        }
        public static void CreateAndSolve(int insnum, NemudInfo InsInfo, Random rand)
        {
            // marbut be tolid instance va chap dar filhaye text
            Console.WriteLine("\n Create and Solve instance {0}\n", insnum);
            GenClasses Exm = new GenClasses(InsInfo, rand);
            Exm.Generate();
            instance f;
            f = Exm.ins;
            Banner1();
            phase1 alg = new phase1(f);
            alg.solve();
            alg.PrintSequence();
            f.WriteData(insnum);
            Banner2();
        }
        public static instance ReadInstance(int insnum)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n Read instance {0} . . . \n", insnum);
            instance obj = new instance();
            obj.ReadData(insnum);
            return obj;
        }
        public static instance CreatInstance(int insnum, NemudInfo InsInfo, Random rand)
        {
            GenClasses objGC = new GenClasses(InsInfo, rand);
            objGC.Generate();
            instance obj;
            obj = objGC.ins;
            return obj;
        }
        public static void Strategies(instance ObjIns1, int insnum, bool PS, int TimeLimit, Random rand)
        {
            Banner3();
            Console.BackgroundColor = ConsoleColor.DarkCyan;
            Console.ForegroundColor = ConsoleColor.White;
            SolutionType sol = new SolutionType();

            Console.WriteLine(" Heuristic  \n");
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Gray;

            Console.WriteLine("< 2-phase greedy heuristic >");
            var stw = new Stopwatch();
            stw.Start();
            phase1 objph1 = new phase1(ObjIns1);
            objph1.solve();
            ObjIns1.WriteData(insnum);
            sol.Seq = ObjIns1.R;
            {
                Phase2 objph2 = new Phase2(ObjIns1, sol);
                objph2.Solve();
                sol = objph2.sol;
            }
            PrintSolution(sol, PS);
            stw.Stop();
            Console.WriteLine("Elapsed Time : {0} Milli s.", stw.ElapsedMilliseconds);
            Console.WriteLine("___________________\n");
            Console.BackgroundColor = ConsoleColor.DarkCyan;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("\n  Metaheuristics  \n");

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.WriteLine("<GRASP>");
            {
                GRASP grasp = new GRASP(ObjIns1, sol.Seq, 0.2, 20, 0.2, rand);
                grasp.Solve(true, TimeLimit, true, true);
                sol = grasp.BestSoFar;
                PrintSolution(sol, PS);
            }
            Console.WriteLine("___________________\n");

           

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.WriteLine("<ACS-GRASP>");
            {
                ASC_Paramteters par;
                par.rh = 0.3;
                par.m = 16;
                par.beta = 1.08;
                par.say = 0.2;
                par.q0 = 0.73;
                ACS_AntGRASP acsgrasp = new ACS_AntGRASP(ObjIns1, par, 20, 0.05, 2, 0.2, rand);
                acsgrasp.Solve(PS, TimeLimit);

                // Chape full solution
                /*Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("\nFull Best So Far of ACS-Grasp : ");
                Console.ForegroundColor = ConsoleColor.White;

                Phase2 alg2 = new Phase2(ObjIns1, acsgrasp.BestSoFar);
                alg2.Solve();
                FullSolutionType fsol = new FullSolutionType();
                alg2.CopyToFullSolution(fsol);
                PrintFullSolution(fsol);
             */
            }
        }
        public static void PrintSolution(SolutionType sol, bool PS)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("Obj : {0} ", sol.Cost);
            if (PS) Console.Write(",  Seq : ");
            else { Console.WriteLine(); return; }
            foreach (CoilInSeq i in sol.Seq)
            {
                switch (i.TP)
                {
                    case TypeInSeq.Incomming:
                        Console.ForegroundColor = ConsoleColor.Green;
                        break;
                    case TypeInSeq.Outgoing:
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                    case TypeInSeq.Blocking:
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        break;
                }
                Console.Write(" {0} ", i.num + 1);
                Console.ForegroundColor = ConsoleColor.White;
                if (sol.Seq.IndexOf(i) != sol.Seq.Count - 1)
                    Console.Write(" - ");
            }
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();
        }
        public static void PrintFullSolution(FullSolutionType fsol)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Objective function value : {0}\n", fsol.Cost);
            int J = fsol.Seq.Count;
            for (int j = 0; j < J; j++)
            {
                Console.WriteLine("______________________________________\n");
                Console.WriteLine("Operation No. = {0}", j + 1);
                Console.WriteLine("Coil No. = {0}", fsol.Seq[j].num + 1);
                Console.WriteLine("Type = {0}", fsol.Seq[j].TP);
                Console.WriteLine("Assigned Crane = {0}", fsol.AssignedCrane[j] + 1);
                Console.WriteLine("Destination position = ({0},{1})", fsol.X[j].r, fsol.X[j].n);
                Console.WriteLine("Start = {0}", fsol.S[j]);
                Console.WriteLine("Finish = {0}", fsol.C[j]);
                for (int m = 0; m < fsol.p.GetLength(0); m++)
                {
                    Console.WriteLine("Crane{0}", m + 1);
                    Console.WriteLine("       p = ({0},{1})", fsol.p[m, j].r, fsol.p[m, j].n);
                    Console.WriteLine("       q = ({0},{1})", fsol.q[m, j].r, fsol.q[m, j].n);
                }
            }
        }
        public static void Banner1()
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("   ______________________________________");
            Console.Write("  |                                      |\n  | ");
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.BackgroundColor = ConsoleColor.Gray;
            Console.Write(" Phase 1 Of The Heuristic Algorithm ");
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(" |\n  |______________________________________| \n\n");
        }
        public static void Banner2()
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("\n   ______________________________________");
            Console.Write("  |                                      |\n  | ");
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.BackgroundColor = ConsoleColor.Gray;
            Console.Write(" Phase 2 Of The Heuristic Algorithm ");
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(" |\n  |______________________________________| \n\n");

        }
        public static void Banner3()
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("   ______________________");
            Console.Write("  |                      |\n  | ");
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.BackgroundColor = ConsoleColor.Gray;
            Console.Write(" Solving Strategies ");
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(" |\n  |______________________| \n\n");
        }
        public static void BatchInstancesGenerator(Random rand)
        {
            Console.WriteLine("Generating Batch Instances . . .");
            int BeginNum = 1;  //starting number for InsNum
            int Num = 10;      //Number of instances
            NemudInfo InsInfo = new NemudInfo();
            {
                InsInfo.i = 0; 
                InsInfo.inp = 0;
                InsInfo.outp = 0;
                InsInfo.m = 0;
                InsInfo.h = 0;
                InsInfo.t = 0;
                InsInfo.r = 0;
                InsInfo.n = 0;
                InsInfo.v1 = 1;
                InsInfo.v2 = 2;
                InsInfo.l1 = 2;
                InsInfo.l2 = 4;
                InsInfo.mu = 0.5;
                InsInfo.RowDisUnit = 4;
                InsInfo.ColDisUnit = 2;
                InsInfo.RowDisUnit_2 = 4;
                InsInfo.ColDisUnit_2 = 2;
                InsInfo.safeD = 1;
            }

            for (int i = BeginNum; i < BeginNum + Num; i++)
            {
                int a = 29;  //min of half of n
                int b = 29;  //max of half of n
                InsInfo.n = 2 * rand.Next((int)((a - 1) / 2), (int)((b - 1) / 2) + 1) + 1;
                InsInfo.t = rand.Next(20, 30 + 1);
                InsInfo.h = rand.Next(1, 2 + 1);
                InsInfo.m = rand.Next(2, 3 + 1);
                int idx = 0; if (InsInfo.m == 3) idx = 2;
                InsInfo.r = rand.Next(12 + idx, 15 + idx + 1);

                int MaxCoilsNum = (InsInfo.r - 2) * (InsInfo.n - 2); 
                double AccP = 0.5 + rand.NextDouble() * (0.75 - 0.5);
                InsInfo.inp = (int)Math.Floor((double)MaxCoilsNum * (0.05 + rand.NextDouble() * (0.08 - 0.06)));
                if (InsInfo.inp == 0) InsInfo.inp++;
                InsInfo.outp = (int)Math.Floor((double)MaxCoilsNum * (0.05 + rand.NextDouble() * (0.08 - 0.06)));
                if (InsInfo.outp == 0) InsInfo.outp++;
                int ii = (int)Math.Floor((double)MaxCoilsNum * AccP);
                InsInfo.i = ii + InsInfo.inp;

                GenClasses Exm = new GenClasses(InsInfo, rand);
                Exm.Generate();
                instance f;
                f = Exm.ins;
                phase1 alg = new phase1(f);
                alg.solve();
                f.WriteData(i);
                Console.WriteLine("Instance {0} is generated.", i);
            }
        }
        public static void BatchRun(int TimeLimit, Random rand)
        {
            string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase).Replace("file:\\", "");
            Excel.Application xl = new Excel.Application();


            string ExcelfileName = "res.xlsx";

            string folder = "fn";
            int sheetnum = 1;
            int FirstInsNum = 1;
            int InstancesNum = 4;
            int RunNum = 2;

            var stw = new Stopwatch();
            int ii = 0;
            for (int i = FirstInsNum; i < FirstInsNum + InstancesNum; i++)
            {
                ii++;
                Console.WriteLine("Solving instance {0}", i);
                instance ObjIns1 = new instance();
                ObjIns1.ReadData(i, folder);

                //Greedy Heuristic
                {
                    Console.WriteLine("Greedy heuristic . . .");
                    Excel.Workbook wr = xl.Workbooks.Open(path + "\\" + ExcelfileName);
                    Excel.Worksheet sheet1 = wr.Sheets[sheetnum];
                    Excel.Range rng = sheet1.UsedRange;

                    SolutionType sol = new SolutionType();

                    stw.Start();
                    phase1 objph1 = new phase1(ObjIns1);
                    objph1.solve();
                    sol.Seq = ObjIns1.R;
                    {
                        Phase2 objph2 = new Phase2(ObjIns1, sol);
                        objph2.Solve();
                        sol = objph2.sol;
                    }
                    stw.Stop();

                    PrintSolution(sol, false);
                    Console.WriteLine("Elapsed Time : {0} Milli s.", stw.ElapsedMilliseconds);
                    Console.WriteLine("_____________");
                    rng.Cells[ii + 2, 1].value2 = i;
                    rng.Cells[ii + 2, 2].value2 = sol.Cost;
                    rng.Cells[ii + 2, 3].value2 = stw.ElapsedMilliseconds;
                    wr.Close(true);

                }
                Console.WriteLine("_____________");

                //ACS-GRASP
                {
                    Console.WriteLine("ASC-GRASP . . .");
                    Excel.Workbook wr = xl.Workbooks.Open(path + "\\" + ExcelfileName);
                    Excel.Worksheet sheet1 = wr.Sheets[sheetnum];
                    Excel.Range rng = sheet1.UsedRange;

                    double[] Costs = new double[RunNum];
                    double[] Times = new double[RunNum];

                    for (int j = 0; j < RunNum; j++)
                    {
                        Console.WriteLine("\ninstance {0}, run {1}", i, j);
                        ASC_Paramteters par;
                        par.rh = 0.1;
                        par.m = 5;
                        par.beta = 1.91;
                        par.say = 0.05;
                        par.q0 = 0.5;
                        ACS_AntGRASP acsgrasp = new ACS_AntGRASP(ObjIns1, par, 20, 0.2, 5, 0.2, rand);
                        stw.Restart();
                        acsgrasp.Solve(false, TimeLimit);
                        stw.Stop();

                        Costs[j] = acsgrasp.BestSoFar.Cost;
                        Times[j] = stw.ElapsedMilliseconds;
                    }
                    double AvgCost = Costs.Average();
                    double AvgTime = Times.Average();
                    double sumOfSquaresOfDifferences = Costs.Select(val => (val - AvgCost) * (val - AvgCost)).Sum();
                    double sd = Math.Sqrt(sumOfSquaresOfDifferences / RunNum);

                    rng.Cells[ii + 2, 7].value2 = AvgCost;
                    rng.Cells[ii + 2, 8].value2 = AvgTime;
                    rng.Cells[ii + 2, 9].value2 = sd;
                    wr.Close(true);
                }

                //GRASP               
                {
                    Console.WriteLine("GRASP . . .");
                    Excel.Workbook wr = xl.Workbooks.Open(path + "\\" + ExcelfileName);
                    Excel.Worksheet sheet1 = wr.Sheets[sheetnum];
                    Excel.Range rng = sheet1.UsedRange;

                    double[] Costs = new double[RunNum];
                    double[] Times = new double[RunNum];

                    for (int j = 0; j < RunNum; j++)
                    {
                        Console.WriteLine("\ninstance {0}, run {1}", i, j);
                        GRASP grasp = new GRASP(ObjIns1, ObjIns1.R, 0.2, 20, 0.2, rand);
                        stw.Restart();
                        grasp.Solve(false, TimeLimit, true, true);
                        stw.Stop();

                        Costs[j] = grasp.BestSoFar.Cost;
                        Times[j] = stw.ElapsedMilliseconds;
                    }
                    double AvgCost = Costs.Average();
                    double AvgTime = Times.Average();
                    double sumOfSquaresOfDifferences = Costs.Select(val => (val - AvgCost) * (val - AvgCost)).Sum();
                    double sd = Math.Sqrt(sumOfSquaresOfDifferences / RunNum);

                    rng.Cells[ii + 2, 4].value2 = AvgCost;
                    rng.Cells[ii + 2, 5].value2 = AvgTime;
                    rng.Cells[ii + 2, 6].value2 = sd;
                    wr.Close(true);
                }
                Console.WriteLine("_____________");
                
            }
        }
    }
}
