using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace OCSRMCSLA
{
    class BaseCoilTypeAnt : BaseCoilType
    {
        public double Heuristic;
    }
    struct ASC_Paramteters
    {
        public double beta;
        public double say;
        public double q0;
        public int m;
        public double rh;
    }
    class ACS_AntGRASP
    {
        bool PrintTimeToFile;
        string FilNam; //filename

        Random rand;
        instance ins;
        int MaxIt;       //Max iteration of ACS
        int GraspIt;     //GRASP iteration
        double alpha;    //for GRASP
        double RateH;    //evaporation rate
        double Tau0;     // according to paper
        double gamma = 1;

        double beta; // according to paper
        double say;//according to paper
        double q0; //according to paper
        int NumOfAnts;      // number of ants

        double Q = 1;    //according to paper
        double pr;       //Local Search rate
        int NumOutgCoils;
        int[] index;
        public SolutionType BestSoFar;       
        List<BaseCoilTypeAnt>[] InfoTyp;
        List<CoilInSeq> InCommings;
        public ACS_AntGRASP(instance inst, ASC_Paramteters par, int maxit, double a, int MT, double pr2, Random r, bool PTF = false, string FN = "")
        {
            PrintTimeToFile = PTF;
            FilNam = FN;

            ins = inst;

            RateH = par.rh;
            NumOfAnts = par.m;
            beta = par.beta;
            say = par.say;
            q0 = par.q0;
            

            MaxIt = maxit;
            alpha = a;
            pr = pr2;
            rand = r;
            GraspIt = MT;
            index = new int[ins.NumOfCoils];
            NumOutgCoils = 0;
            InfoTyp = new List<BaseCoilTypeAnt>[ins.NumOf_t];
            InCommings = new List<CoilInSeq>();
            for (int t = 0; t < ins.NumOf_t; t++)
            {
                NumOutgCoils += ins.NumOf_outgoings[t];
                InfoTyp[t] = new List<BaseCoilTypeAnt>();
            }
            for (int j = 0; j < ins.NumOfCoils; j++)
            {
                if (ins.coil_info[j].IsIncoming == false)
                {
                    BaseCoilTypeAnt tmp = new BaseCoilTypeAnt();
                    tmp.num = j;
                    tmp.pheromone = 1;
                    tmp.IsSelected = false;
                    InfoTyp[ins.coil_info[j].type - 1].Add(tmp);
                    index[j] = InfoTyp[ins.coil_info[j].type - 1].Count() - 1;
                }
                else
                {
                    CoilInSeq tmp = new CoilInSeq();
                    tmp.num = j;
                    tmp.TP = TypeInSeq.Incomming;
                    InCommings.Add(tmp);
                }
            }
        }
        public List<CoilInSeq> SelectBase()
        {
            List<CoilInSeq> BaseSet = new List<CoilInSeq>(); 
            double[] SumCounter = new double[ins.NumOf_t];  
            for (int t = 0; t < ins.NumOf_t; t++)
            {
                for (int j = 0; j < InfoTyp[t].Count; j++)
                {
                    InfoTyp[t][j].IsSelected = false;
                    SumCounter[t] += Math.Pow(InfoTyp[t][j].pheromone, gamma) * Math.Pow(InfoTyp[t][j].Heuristic, beta);
                }
            }

            for (int t = 0; t < ins.NumOf_t; t++)
            {
                if (!ins.NeededType[t]) continue;
                for (int i = 0; i < ins.NumOf_outgoings[t]; i++)
                {
                    double value = 0;
                    int number = 0;
                    for (int j = 0; j < InfoTyp[t].Count; j++)
                        if (!InfoTyp[t][j].IsSelected)
                        {
                            double tmp1 = Math.Pow(InfoTyp[t][j].pheromone, gamma) * Math.Pow(InfoTyp[t][j].Heuristic, beta);
                            if (value < tmp1) { value = tmp1; number = j; }
                        }

                    double q = rand.NextDouble();
                    if (q <= q0)
                    {
                        InfoTyp[t][number].IsSelected = true;
                        SumCounter[t] -= value;
                        CoilInSeq tmp2 = new CoilInSeq();
                        tmp2.num = InfoTyp[t][number].num;
                        tmp2.TP = TypeInSeq.Outgoing;
                        BaseSet.Add(tmp2);
                        continue;
                    }
                    double PartialCum = SumCounter[t];
                    int slc = 0;
                    double p = rand.NextDouble();
                    for (int j = InfoTyp[t].Count - 1; j >= 0; j--)
                    {
                        if (!InfoTyp[t][j].IsSelected)
                        {
                            if (p <= PartialCum / SumCounter[t])
                            {
                                slc = j;
                                PartialCum -= Math.Pow(InfoTyp[t][j].pheromone, gamma) * Math.Pow(InfoTyp[t][j].Heuristic, beta);
                            }
                            else break;
                        }
                    }
                    InfoTyp[t][slc].IsSelected = true;
                    SumCounter[t] -= Math.Pow(InfoTyp[t][slc].pheromone, gamma) * Math.Pow(InfoTyp[t][slc].Heuristic, beta); ;
                    CoilInSeq tmp3 = new CoilInSeq();
                    tmp3.num = InfoTyp[t][slc].num;
                    tmp3.TP = TypeInSeq.Outgoing;
                    BaseSet.Add(tmp3);
                }
            }
            //pheremone local update
            foreach (var i in BaseSet)
            {
                InfoTyp[ins.coil_info[i.num].type - 1][index[i.num]].pheromone = (1 - say) * InfoTyp[ins.coil_info[i.num].type - 1][index[i.num]].pheromone + say * Tau0;
            }
            return BaseSet;
        }
        void AddRemaningCoils(List<CoilInSeq> BaseSet)
        {
            int cnt = BaseSet.Count;
            for (int j = 0; j < cnt; j++)
            {
                int i = BaseSet[j].num;
                if (ins.coil_info[i].pos.n % 2 == 1)
                {
                    if (ins.coil_info[i].pos.n > 1)
                    {
                        int k = ins.Mat_CL[ins.coil_info[i].pos.r, ins.coil_info[i].pos.n - 1] - 1;
                        if (k > -1)
                        {
                            if (!InfoTyp[ins.coil_info[k].type - 1][index[k]].IsSelected)
                            {
                                CoilInSeq tmp = new CoilInSeq();
                                tmp.num = k;
                                tmp.TP = TypeInSeq.Blocking;
                                BaseSet.Add(tmp);
                                InfoTyp[ins.coil_info[k].type - 1][index[k]].IsSelected = true;
                            }
                        }
                    }
                    if (ins.coil_info[i].pos.n < ins.NumOf_n - 1)
                    {
                        int k = ins.Mat_CL[ins.coil_info[i].pos.r, ins.coil_info[i].pos.n + 1] - 1;
                        if (k > -1)
                        {
                            if (!InfoTyp[ins.coil_info[k].type - 1][index[k]].IsSelected)
                            {
                                CoilInSeq tmp = new CoilInSeq();
                                tmp.num = k;
                                tmp.TP = TypeInSeq.Blocking;
                                BaseSet.Add(tmp);
                                InfoTyp[ins.coil_info[k].type - 1][index[k]].IsSelected = true;
                            }
                        }
                    }
                }
            }
            for (int i = 0; i < InCommings.Count; i++)
            {
                BaseSet.Add(InCommings[i]);
            }
        }
        void CalculateHeuristics()
        {
            double[] NeededTime = new double[ins.NumOfCoils]; 
            List<position> Yset = new List<position>();
            for (int i = ins.NumOf_m - 1; i <= ins.NumOf_r - ins.NumOf_m; i++)
                for (int j = 1; j < ins.NumOf_n - 1; j++)
                {
                    if (ins.Mat_CL[i, j] == 0)
                    {
                        if (j % 2 == 1)
                        { position ps; ps.r = i; ps.n = j; Yset.Add(ps); }
                        else if (ins.Mat_CL[i, j - 1] > 0 && ins.Mat_CL[i, j + 1] > 0)
                        { position ps; ps.r = i; ps.n = j; Yset.Add(ps); }
                    }
                }
            for (int t = 0; t < ins.NumOf_t; t++)
                foreach (var i in InfoTyp[t])
                {
                    double min1 = 1000000;
                    foreach (position y in Yset)
                    {
                        double tmp = ins.tf(ins.coil_info[i.num].pos, y);
                        if (tmp < min1) min1 = tmp;
                    }
                    NeededTime[i.num] = min1;
                }
            
            for (int t = 0; t < ins.NumOf_t; t++)
                if (ins.NeededType[t])
                    foreach (var i in InfoTyp[t])
                    {
                        if (ins.coil_info[i.num].pos.n % 2 == 1)
                        {
                            if (ins.coil_info[i.num].pos.n > 1 && ins.Mat_CL[ins.coil_info[i.num].pos.r, ins.coil_info[i.num].pos.n - 1] > 0)
                                i.Heuristic = NeededTime[ins.Mat_CL[ins.coil_info[i.num].pos.r, ins.coil_info[i.num].pos.n - 1] - 1];
                            if (ins.coil_info[i.num].pos.n < ins.NumOf_n - 2 && ins.Mat_CL[ins.coil_info[i.num].pos.r, ins.coil_info[i.num].pos.n + 1] > 0) //NEMA ANABAR
                                i.Heuristic += NeededTime[ins.Mat_CL[ins.coil_info[i.num].pos.r, ins.coil_info[i.num].pos.n + 1] - 1];
                        }
                    }
            
            for (int t = 0; t < ins.NumOf_t; t++)
                if (ins.NeededType[t])
                    foreach (var i in InfoTyp[t])
                    {
                        double min2 = 100000;
                        for (int h = 0; h < ins.NumOf_h; h++)
                        {
                            if (ins.tf(ins.coil_info[i.num].pos, ins.DoorsPos[h]) < min2)
                                min2 = ins.tf(ins.coil_info[i.num].pos, ins.DoorsPos[h]);
                        }

                        i.Heuristic += min2;
                        i.Heuristic = 1 / i.Heuristic;
                    }
        }
        public void Solve(bool PS, int TimeLimit = 360000000)
        {
            StreamWriter f1 = null;
            if (PrintTimeToFile) { f1 = new StreamWriter(FilNam); }

            var stw = new Stopwatch();
            stw.Start();
           
            CalculateHeuristics();
            List<CoilInSeq> Dlist;
            {
                if (ins.R == null)
                {
                    phase1 objph1 = new phase1(ins);
                    objph1.solve();
                }
                GRASP grasp = new GRASP(ins, ins.R, 0, 5, pr, rand);
                grasp.Solve();
                BestSoFar = grasp.BestSoFar;
            }

            Tau0 = 1 / BestSoFar.Cost;

            for (int t = 0; t < ins.NumOf_t; t++)
                if (ins.NeededType[t])
                    foreach (var i in InfoTyp[t])
                        i.pheromone = Tau0;


            for (int it = 1; it <= MaxIt; it++)
            {
                bool BFisChanged = false;
                SolutionType[] obj_ST = new SolutionType[NumOfAnts];
                for (int m = 0; m < NumOfAnts; m++)
                {
                    GRASP grasp2;
                    if (m>0)
                    {
                        Dlist = SelectBase();
                        AddRemaningCoils(Dlist);
                        grasp2 = new GRASP(ins, Dlist, alpha, GraspIt, pr, rand);
                    }
                    else grasp2 = new GRASP(ins, ins.R, 0.4, GraspIt, pr, rand);

                    grasp2.Solve();
                    obj_ST[m] = grasp2.BestSoFar;
                    if (grasp2.BestCost < BestSoFar.Cost) { BestSoFar = grasp2.BestSoFar; BFisChanged = true; };
                }
                //evaporation
                for (int t = 0; t < ins.NumOf_t; t++)
                    if (ins.NeededType[t])
                        foreach (var i in InfoTyp[t])
                            i.pheromone *= 1 - RateH;
                bool flgChange = false;
                //------

                foreach (var i in BestSoFar.Seq)
                {
                    if (i.TP == TypeInSeq.Outgoing) InfoTyp[ins.coil_info[i.num].type - 1][index[i.num]].pheromone += Q / BestSoFar.Cost;

                }

                PrintSolution(it, BestSoFar, PS, BFisChanged, flgChange);

                if (PrintTimeToFile) f1.WriteLine("{0}\t{1}", (double)(stw.ElapsedMilliseconds) / 1000.0, BestSoFar.Cost);

                if (stw.ElapsedMilliseconds >= TimeLimit) break;
            }
            stw.Stop();
            Console.WriteLine("Elapsed Time : {0} Milli s.", stw.ElapsedMilliseconds);
            if (PrintTimeToFile) f1.Close();

        }
        static void PrintSolution(int iter, SolutionType sol, bool PS, bool BFC, bool flgchange) 
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("Iter {0, -5}  Obj : ", iter);
            if (BFC)
                Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("{0} ", sol.Cost);
            if (BFC) Console.ForegroundColor = ConsoleColor.White;

            if (flgchange) { Console.ForegroundColor = ConsoleColor.Red; Console.Write(" << Base set of BestSoFar is changed >>"); Console.ForegroundColor = ConsoleColor.White; }
            //-------------
            if (sol.IsImprovedByLS) { Console.ForegroundColor = ConsoleColor.Yellow; Console.Write(" Improved By LS "); Console.ForegroundColor = ConsoleColor.White; }
            sol.IsImprovedByLS = false;

            if (PS) Console.Write(",  Seq : "); else { Console.WriteLine(); return; };
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
                    Console.Write("-");
            }
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();
        }
    }
}
