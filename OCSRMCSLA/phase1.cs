using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace OCSRMCSLA
{
    class request
    {
        // all notations are according to paper
        public double S; 
        public double D; 
        public double F; 
        public double P;
        public int h; //exit door index
    }
    class CoilInSeq
    {
        public int num;
        public TypeInSeq TP;
    }
    class phase1
    {
        //all notations are according to paper
        public instance ins;
        public ArrayList Yset;//the set of available locations
        public ArrayList Lset;
        public ArrayList Qset;      
        public ArrayList Wset;
        public ArrayList Bset;
        public ArrayList Gset;
        public request[] AllCoils;
        public int[,] Mat_CL2;//indicating what coil is located in where location in the wharehouse
        public int[,] N_th2;  //the number of coils of type t needed for door h
        public phase1(instance inst)
        {
            ins = inst;
            Mat_CL2 = new int[ins.NumOf_r, ins.NumOf_n];
            for (int i = 0; i < ins.NumOf_r; i++)
                for (int j = 0; j < ins.NumOf_n; j++)
                {
                    Mat_CL2[i, j] = ins.Mat_CL[i, j];
                }
            N_th2 = new int[ins.NumOf_t, ins.NumOf_h];
            for (int i = 0; i < ins.NumOf_t; i++)
                for (int j = 0; j < ins.NumOf_h; j++)
                {
                    N_th2[i, j] = ins.N_th[i, j];
                }
            Yset = new ArrayList();
            for (int i = ins.NumOf_m - 1; i <= ins.NumOf_r - ins.NumOf_m; i++)
                for (int j = 1; j < ins.NumOf_n - 1; j++)
                {
                    if (Mat_CL2[i, j] == 0)
                    {
                        if (j % 2 == 1)
                        {
                            position ps;
                            ps.r = i; ps.n = j;
                            Yset.Add(ps);
                        }
                        else if (Mat_CL2[i, j - 1] > 0 && Mat_CL2[i, j + 1] > 0)
                        {
                            position ps;
                            ps.r = i; ps.n = j;
                            Yset.Add(ps);
                        }
                    }
                }
        }
        void AssignSets()
        {
            AllCoils = new request[ins.NumOfCoils];
            for (int i = 0; i < ins.NumOfCoils; i++)
            {
                AllCoils[i] = new request();
            }
            Qset = new ArrayList();
            Wset = new ArrayList();
            Bset = new ArrayList();
            Gset = new ArrayList();
            Lset = new ArrayList();
            bool[] Qflg = new bool[ins.NumOfCoils];
            bool[] Bflg = new bool[ins.NumOfCoils];
            bool[] Wflg = new bool[ins.NumOfCoils];
            for (int i = 0; i < ins.NumOfCoils; i++)
            {
                if (ins.coil_info[i].IsIncoming) { Qset.Add(i); Qflg[i] = true; }
                if (!ins.coil_info[i].IsIncoming && ins.NeededType[ins.coil_info[i].type - 1]) { Wset.Add(i); Wflg[i] = true; }
                if (ins.coil_info[i].pos.n > 0 && ins.coil_info[i].pos.n % 2 == 0)
                {
                    int j1 = Mat_CL2[ins.coil_info[i].pos.r, ins.coil_info[i].pos.n - 1];
                    int j2 = Mat_CL2[ins.coil_info[i].pos.r, ins.coil_info[i].pos.n + 1];
                    bool flg = false;
                    if (j1 > 0 && ins.NeededType[ins.coil_info[j1 - 1].type - 1]) flg = true;
                    if (j2 > 0 && ins.NeededType[ins.coil_info[j2 - 1].type - 1]) flg = true;
                    if (flg) { Bset.Add(i); Bflg[i] = true; }
                }
            }
            for (int i = 0; i < ins.NumOfCoils; i++)
            {
                if (Qflg[i] || Bflg[i]) Gset.Add(i);
                if (Qflg[i] || Wflg[i]) Lset.Add(i);
            }
        }
        public void solve()
        {
            AssignSets();
            int InOut = 0;//sum of incomming and outgoing coils
            for (int i = 0; i < ins.NumOf_t; i++)
            {
                InOut += ins.NumOf_outgoings[i];
            }
            InOut += ins.NumOf_incomings;
            foreach (int i in Gset)
            {
                double minn = double.MaxValue;
                foreach (position y in Yset)
                {
                    double tmp = ins.tf(ins.coil_info[i].pos, y);
                    if (tmp < minn) minn = tmp;
                }
                AllCoils[i].S = minn;
            }

            foreach (int i in Lset)
            {
                double minn = double.MaxValue;
                for (int m = 0; m < ins.NumOf_m; m++)
                {
                    double tmp = ins.te(ins.CranesPos[m], ins.coil_info[i].pos);
                    if (tmp < minn) minn = tmp;
                }
                AllCoils[i].D = minn;
            }

            foreach (int i in Wset)
            {
                if (ins.coil_info[i].pos.n % 2 == 1)
                {
                    if (ins.coil_info[i].pos.n > 1 && Mat_CL2[ins.coil_info[i].pos.r, ins.coil_info[i].pos.n - 1] > 0)
                        AllCoils[i].F = AllCoils[Mat_CL2[ins.coil_info[i].pos.r, ins.coil_info[i].pos.n - 1] - 1].S;
                    if (ins.coil_info[i].pos.n < ins.NumOf_n - 2 && Mat_CL2[ins.coil_info[i].pos.r, ins.coil_info[i].pos.n + 1] > 0) //NEMA ANABAR
                        AllCoils[i].F += AllCoils[Mat_CL2[ins.coil_info[i].pos.r, ins.coil_info[i].pos.n + 1] - 1].S;
                }
            }
            foreach (int i in Lset)
                if (ins.coil_info[i].IsIncoming)
                    AllCoils[i].P = AllCoils[i].S;

            if (ins.R == null) ins.R = new List<CoilInSeq>();

            while (InOut > 0)
            {

                foreach (int i in Lset)
                {
                    if (!ins.coil_info[i].IsIncoming)
                    {
                        double minn = double.MaxValue;
                        int htmp = ins.NumOf_h + 1;
                        for (int h = 0; h < ins.NumOf_h; h++)
                        {
                            if (N_th2[ins.coil_info[i].type - 1, h] > 0)
                            {
                                double tmp = ins.tf(ins.coil_info[i].pos, ins.DoorsPos[h]);
                                if (tmp < minn) { minn = tmp; htmp = h; }
                            }
                        }
                        AllCoils[i].P = AllCoils[i].F + minn;
                        AllCoils[i].h = htmp;
                    }
                }

                double mn = 1000000;
                int j = ins.NumOfCoils + 1;
                int idx = 0;
                for (int i = 0; i < Lset.Count; i++)
                {
                    int ii = (int)Lset[i];
                    if (ins.R.Count == 0)
                    {
                        if (AllCoils[ii].D + AllCoils[ii].P < mn)
                        {
                            mn = AllCoils[ii].D + AllCoils[ii].P;
                            j = ii;
                            idx = i;
                        }
                    }
                    else
                    {
                        if (AllCoils[ii].P < mn)
                        {
                            mn = AllCoils[ii].P;
                            j = ii;
                            idx = i;
                        }
                    }
                }

                Lset.RemoveAt(idx);
                InOut--;
                if (ins.coil_info[j].IsIncoming)
                {
                    CoilInSeq jp = new CoilInSeq();
                    jp.num = j;
                    jp.TP = TypeInSeq.Incomming;
                    ins.R.Add(jp);
                    continue;
                }
                N_th2[ins.coil_info[j].type - 1, AllCoils[j].h]--;
                int sumh = 0;
                for (int h = 0; h < ins.NumOf_h; h++)
                    sumh += N_th2[ins.coil_info[j].type - 1, h];
                if (sumh == 0)
                {
                    int id = 0;
                    while (id < Lset.Count)
                    {
                        if (ins.coil_info[j].type == ins.coil_info[(int)Lset[id]].type)
                            Lset.RemoveAt(id);
                        else id++;
                    }
                }
                if (ins.coil_info[j].pos.n % 2 == 1)
                {
                    int j1 = Mat_CL2[ins.coil_info[j].pos.r, ins.coil_info[j].pos.n - 1] - 1;
                    if (ins.coil_info[j].pos.n == 1) j1 = -1;
                    int j2 = Mat_CL2[ins.coil_info[j].pos.r, ins.coil_info[j].pos.n + 1] - 1;
                    if (ins.coil_info[j].pos.n == ins.NumOf_n - 2) j2 = -1;
                    if (j1 >= 0 && j2 == -1)
                    {
                        CoilInSeq jp= new CoilInSeq();
                        jp.num = j1;
                        jp.TP = TypeInSeq.Blocking;
                        ins.R.Add(jp);

                        AllCoils[Mat_CL2[ins.coil_info[j1].pos.r, ins.coil_info[j1].pos.n - 1] - 1].F -= AllCoils[j1].S;
                        AllCoils[Mat_CL2[ins.coil_info[j1].pos.r, ins.coil_info[j1].pos.n + 1] - 1].F -= AllCoils[j1].S;
                        Mat_CL2[ins.coil_info[j1].pos.r, ins.coil_info[j1].pos.n] = 0;
                    }
                    else if (j1 == -1 && j2 >= 0)
                    {
                        CoilInSeq jp = new CoilInSeq(); ;
                        jp.num = j2;
                        jp.TP = TypeInSeq.Blocking;
                        ins.R.Add(jp);

                        AllCoils[Mat_CL2[ins.coil_info[j2].pos.r, ins.coil_info[j2].pos.n - 1] - 1].F -= AllCoils[j2].S;
                        AllCoils[Mat_CL2[ins.coil_info[j2].pos.r, ins.coil_info[j2].pos.n + 1] - 1].F -= AllCoils[j2].S;
                        Mat_CL2[ins.coil_info[j2].pos.r, ins.coil_info[j2].pos.n] = 0;
                    }
                    else if (j1 >= 0 && j2 >= 0)
                    {
                        AllCoils[Mat_CL2[ins.coil_info[j1].pos.r, ins.coil_info[j1].pos.n - 1] - 1].F -= AllCoils[j1].S;
                        AllCoils[Mat_CL2[ins.coil_info[j1].pos.r, ins.coil_info[j1].pos.n + 1] - 1].F -= AllCoils[j1].S;
                        AllCoils[Mat_CL2[ins.coil_info[j2].pos.r, ins.coil_info[j2].pos.n - 1] - 1].F -= AllCoils[j2].S;
                        AllCoils[Mat_CL2[ins.coil_info[j2].pos.r, ins.coil_info[j2].pos.n + 1] - 1].F -= AllCoils[j2].S;
                        Mat_CL2[ins.coil_info[j1].pos.r, ins.coil_info[j1].pos.n] = 0;
                        Mat_CL2[ins.coil_info[j2].pos.r, ins.coil_info[j2].pos.n] = 0;
                        if (AllCoils[j1].S < AllCoils[j2].S)
                        {
                            CoilInSeq jp = new CoilInSeq();
                            jp.num = j1;
                            jp.TP = TypeInSeq.Blocking;
                            ins.R.Add(jp);
                            CoilInSeq jp2 = new CoilInSeq();
                            jp2.num = j2;
                            jp2.TP = TypeInSeq.Blocking;
                            ins.R.Add(jp2);
                        }
                        else
                        {
                            CoilInSeq jp = new CoilInSeq();
                            jp.num = j2;
                            jp.TP = TypeInSeq.Blocking;
                            ins.R.Add(jp);
                            CoilInSeq jp2 = new CoilInSeq();
                            jp2.num = j1;
                            jp2.TP = TypeInSeq.Blocking;
                            ins.R.Add(jp2);
                        }

                    }
                }
                CoilInSeq jpp = new CoilInSeq();
                jpp.num = j;
                jpp.TP = TypeInSeq.Outgoing;
                ins.R.Add(jpp);
                if (ins.coil_info[j].pos.n > 0 && ins.coil_info[j].pos.n < ins.NumOf_n - 1 && ins.coil_info[j].pos.n % 2 == 0) //NEMA ANBAR
                {
                    AllCoils[Mat_CL2[ins.coil_info[j].pos.r, ins.coil_info[j].pos.n - 1] - 1].F -= AllCoils[j].S;
                    AllCoils[Mat_CL2[ins.coil_info[j].pos.r, ins.coil_info[j].pos.n + 1] - 1].F -= AllCoils[j].S;
                    Mat_CL2[ins.coil_info[j].pos.r, ins.coil_info[j].pos.n] = 0;
                }
            }
            chek_doublicate();
        }
        void chek_doublicate()
        {
            int idx = 0;
            while (idx < ins.R.Count)
            {
                int j = 0;
                bool flg = false;
                CoilInSeq r = (CoilInSeq)ins.R[idx];
                for (int i = idx + 1; i < ins.R.Count; i++)
                {
                    CoilInSeq r2 = (CoilInSeq)ins.R[i];
                    if (r.num == r2.num)
                    {
                        j = i;
                        flg = true;
                        break;
                    }
                }
                if (flg)
                {
                    ins.R.RemoveAt(j);
                    r.TP = TypeInSeq.Outgoing;
                    ins.R[idx] = r;
                }
                idx++;
            }
        }
        public void PrintSequence()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("   Colors: ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(" Incoming  ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(" Outgoing  ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(" Blocking  ");

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("\n\n\n\n   Sequence: \n");
            Console.Write("  ");
            foreach (CoilInSeq i in ins.R)
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
                if (ins.R.IndexOf(i) != ins.R.Count - 1)
                    Console.Write(" - ");
            }
            Console.WriteLine("\n\n_______________________________________________\n");
        }
    }
}
