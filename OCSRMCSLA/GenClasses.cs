using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections;

namespace OCSRMCSLA
{
    class NemudInfo
    {
        public int i;      //number fo coils
        public int inp;    //number fo incomming coils
        public int outp;   //number of outgoing coils
        public int m;      //number of cranes
        public int h;      //number of doors
        public int t;      //number of types       
        public int r;      //number of rows
        public int n;      //number fo columns
        public double v1;  //speed of loaded crane in common track
        public double v2;  //speed of loaded crane in deticated track
        public double l1;  //speed of empty crane in common track
        public double l2;  //speed of empty crane in deticated track
        public double mu;  // loading and unloading time
        public int RowDisUnit;    // dictance between 2 consecutive location in a row
        public int ColDisUnit;    //dictance between 2 consecutive location in a column
        public int RowDisUnit_2;  //dictance between 2 consecutive location in a dummy row
        public int ColDisUnit_2;  //dictance between 2 consecutive location in a dummy column
        public int safeD;         //safety distance between 2 cranes      

    }
    class GenClasses
    {
        public instance ins;
        public NemudInfo info;
        public Random rand;
        public GenClasses(NemudInfo info2, Random Rand)
        {
            info = info2;
            rand = Rand;
        }
        void Generate_1()
        {
            ins = new instance();
            ins.NumOfCoils = info.i;
            ins.NumOf_n = info.n;
            ins.NumOf_r = info.r;
            ins.NumOf_h = info.h;
            ins.NumOf_m = info.m;
            ins.NumOf_t = info.t;
            ins.v1 = info.v1;
            ins.v2 = info.v2;
            ins.lambda1 = info.l1;
            ins.lambda2 = info.l2;
            ins.mu = info.mu;
            ins.NumOf_incomings = info.inp;
            ins.RowDisUnit = info.RowDisUnit;
            ins.ColDisUnit = info.ColDisUnit;
            ins.RowDisUnit_2 = info.RowDisUnit_2;
            ins.ColDisUnit_2 = info.ColDisUnit_2;
            ins.safeD = info.safeD;

            //generating position of exit doors
            ArrayList ps = new ArrayList();
            for (int r = ins.NumOf_m - 1; r <= ins.NumOf_r - ins.NumOf_m; r++)
                ps.Add(r);

            ins.DoorsPos = new position[ins.NumOf_h];
            for (int h = 0; h < ins.NumOf_h; h++)
            {
                int rw = rand.Next(0, ps.Count);
                position dps;
                dps.r = (int)ps[rw];
                dps.n = ins.NumOf_n - 1;
                ins.DoorsPos[h] = dps;
                ps.RemoveAt(rw);
            }

            //generating initial position for cranes 
            int[] rows = new int[ins.NumOf_r];
            for (int r = 0; r < ins.NumOf_r; r++) rows[r] = r;
            Shuffle(rows);
            int[] mrows = new int[ins.NumOf_m];
            for (int m = 0; m < ins.NumOf_m; m++) mrows[m] = rows[m];
            Array.Sort(mrows);
            ins.CranesPos = new position[ins.NumOf_m];
            for (int m = 0; m < ins.NumOf_m; m++)
            {
                ins.CranesPos[m].r = mrows[m];
                ins.CranesPos[m].n= rand.Next(0, ins.NumOf_n);
            }
            
            //generating coils
            {
                ins.coil_info = new coil[ins.NumOfCoils];
                for (int i = 0; i < ins.NumOfCoils; i++)
                {
                    ins.coil_info[i] = new coil();
                }
                // ++ generating incomming coils
                ArrayList pos1 = new ArrayList();
                for (int r = ins.NumOf_m - 1; r <= ins.NumOf_r - ins.NumOf_m; r++)
                    pos1.Add(r);

                for (int i = 0; i < info.inp; i++)
                {
                    int rw = rand.Next(0, pos1.Count);
                    position dps;
                    dps.r = (int)pos1[rw];
                    dps.n = 0;
                    ins.coil_info[i].type = 0;
                    ins.coil_info[i].IsIncoming = true;
                    ins.coil_info[i].pos = dps;
                    ins.coil_info[i].num = i + 1;
                }

                // ++ generating the coils where located in tbe wharehouse
                bool[,] arng = new bool[ins.NumOf_r, ins.NumOf_n];
                ArrayList pos2 = new ArrayList();
                for (int r = ins.NumOf_m - 1; r <= ins.NumOf_r - ins.NumOf_m; r++)
                    for (int n = 1; n < ins.NumOf_n - 1; n += 2)
                    {
                        position dps2;
                        dps2.r = r; dps2.n = n;
                        pos2.Add(dps2);

                    }

                for (int i = info.inp; i < ins.NumOfCoils; i++)
                {
                    int rw = rand.Next(0, pos2.Count);
                    position dps;
                    dps = ((position)pos2[rw]);
                    arng[dps.r, dps.n] = true;
                    ins.coil_info[i].pos = dps;
                    ins.coil_info[i].IsIncoming = false;
                    ins.coil_info[i].type = rand.Next(1, ins.NumOf_t + 1);
                    ins.coil_info[i].num = i + 1;
                    pos2.RemoveAt(rw);
                    if (dps.n > 2 && dps.n % 2 == 1)
                    {
                        if (arng[dps.r, dps.n - 2])
                        {
                            position dps2;
                            dps2.r = dps.r;
                            dps2.n = dps.n - 1;
                            pos2.Add(dps2);
                        }
                    }
                    if (dps.n < ins.NumOf_n - 2 && dps.n % 2 == 1)
                    {
                        if (arng[dps.r, dps.n + 2])
                        {
                            position dps2;
                            dps2.r = dps.r;
                            dps2.n = dps.n + 1;
                            pos2.Add(dps2);
                        }
                    }

                }
            }
        }
        void Generate_2()
        {
            //generating type of coils needed for each door
            int[] DoorReq = new int[ins.NumOf_h];
            int Rem = info.outp;
            for (int h = 0; h < ins.NumOf_h - 1; h++)
            {
                DoorReq[h] = Rem / (ins.NumOf_h - h);
                Rem -= DoorReq[h];
            }
            DoorReq[ins.NumOf_h - 1] = Rem;

            int[] NumType = new int[ins.NumOf_t];
            for (int i = 0; i < ins.NumOfCoils; i++)
            {
                if (ins.coil_info[i].type > 0) NumType[ins.coil_info[i].type - 1]++;
            }


            for (int h = 0; h < ins.NumOf_h; h++)
            {
                for (int k = 0; k < DoorReq[h]; k++)
                {
                    bool[] exist = new bool[ins.NumOf_t];
                    int ss = 0;
                    for (int t = 0; t < ins.NumOf_t; t++)
                    {
                        if (NumType[t] > 0) { exist[t] = true; ss++; }
                    }
                    if (ss == 0)
                    {
                        Console.WriteLine("Error . . .");
                    }
                    int rw = rand.Next(1, ss + 1);
                    int SelectedType = 0;
                    int cnt = 0;
                    for (int t = 0; t < ins.NumOf_t; t++)
                    {
                        if (exist[t])
                        {
                            cnt++;
                            if (cnt == rw)
                            {
                                SelectedType = t;
                                break;
                            }
                        }
                    }
                    NumType[SelectedType]--;
                    ins.N_th[SelectedType, h]++;
                }
            }

            for (int i = 0; i < ins.NumOf_t; i++)
            {
                for (int j = 0; j < ins.NumOf_h; j++)
                {
                    if (ins.N_th[i, j] > 0)
                    {
                        ins.NeededType[i] = true;
                        ins.NumOf_outgoings[i] += ins.N_th[i, j];
                    }
                }
            }
        }
        void Shuffle(int[] array)
        {
            int n = array.Count();
            while (n > 1)
            {
                n--;
                int rw = rand.Next(n + 1);
                int temp = array[rw];
                array[rw] = array[n];
                array[n] = temp;
            }
        }
        public void Generate()
        {
            Generate_1();
            ins.N_th = new int[ins.NumOf_t, ins.NumOf_h];
            ins.NeededType = new bool[ins.NumOf_t];
            ins.NumOf_outgoings = new int[ins.NumOf_t];
            Generate_2();
            ins.SetArrange();
        }
    }
}
