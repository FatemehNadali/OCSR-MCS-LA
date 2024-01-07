using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections;

namespace OCSRMCSLA
{
    enum TypeInSeq
    { Incomming, Outgoing, Blocking }
    struct position
    {
        public int r;
        public int n;
    }
    class coil
    {
        public int num;        //the number of coils
        public position pos;   //initial position of coild
        public int type;       //type of colil: A,B,C,...
        public bool IsIncoming;// is the coil incomming coil or not?
    }
    class instance
    {
        public int NumOfCoils;
        public int NumOf_incomings;
        public int NumOf_h;
        public int NumOf_m;
        public int NumOf_t;
        public int NumOf_n;
        public int NumOf_r;      
        public double v1;
        public double v2;
        public double lambda1;
        public double lambda2;
        public double mu;                         
        public double RowDisUnit;     
        public double RowDisUnit_2;  
        public double ColDisUnit;     
        public double ColDisUnit_2; 
        public int safeD;                    
        public int[,] Mat_CL;         //the number of the coil where located in position (r,n)
        public int[,] N_th;           //the number of needed coil of type t for exit door h 
        public coil[] coil_info;      //info of coils
        public position[] CranesPos;  //position of cranes
        public position[] DoorsPos;   //position of exit doors                    
        public List<CoilInSeq> R;     //sequence obtained by phase 1
        public bool[] NeededType;      //type t is needed for retrieval?
        public int[] NumOf_outgoings;  //the number of needed coils of all types for retrieval

        public double D1(int n, int n2)
        {
            double a = Math.Abs(n - n2) * ColDisUnit;
            double b = 0;
            if (n == 0 || n2 == 0 || n == NumOf_n - 1 || n2 == NumOf_n - 1)
            {
                b = ColDisUnit_2 - ColDisUnit;
            }
            return a + b;
        }
        public double D2(int r, int r2)
        {
            double a = Math.Abs(r - r2) * RowDisUnit;
            double b = 0;
            if (r == 0 || r2 == 0 || r == NumOf_r - 1 || r2 == NumOf_r - 1)
            {
                b = RowDisUnit_2 - RowDisUnit;
            }
            return a + b;
        }
        public void ReadData(int InstanceNum, string path = "")
        {
            if (path=="") ReadInitData("Ex" + InstanceNum + "_init.txt");
            else ReadInitData(path+"\\"+"Ex" + InstanceNum + "_init.txt");
            SetArrange();
        }
        void ReadInitData(string fn)
        {
            char[] delimeter = new char[] { '\t' };
            StreamReader f1 = new StreamReader(fn);
            string ln;

            ln = f1.ReadLine();
            string[] r = ln.Split(delimeter);
            NumOfCoils = Int32.Parse(r[1]);

            ln = f1.ReadLine();
            r = ln.Split(delimeter);
            NumOf_r = Int32.Parse(r[1]);

            ln = f1.ReadLine();
            r = ln.Split(delimeter);
            NumOf_n = Int32.Parse(r[1]);

            ln = f1.ReadLine();
            r = ln.Split(delimeter);
            NumOf_h = Int32.Parse(r[1]);

            ln = f1.ReadLine();
            r = ln.Split(delimeter);
            NumOf_t = Int32.Parse(r[1]);

            ln = f1.ReadLine();
            r = ln.Split(delimeter);
            NumOf_m = Int32.Parse(r[1]);

            ln = f1.ReadLine();
            r = ln.Split(delimeter);
            v1 = double.Parse(r[1]);

            ln = f1.ReadLine();
            r = ln.Split(delimeter);
            v2 = double.Parse(r[1]);

            ln = f1.ReadLine();
            r = ln.Split(delimeter);
            lambda1 = double.Parse(r[1]);

            ln = f1.ReadLine();
            r = ln.Split(delimeter);
            lambda2 = double.Parse(r[1]);

            ln = f1.ReadLine();
            r = ln.Split(delimeter);
            mu = double.Parse(r[1]);

            ln = f1.ReadLine();
            r = ln.Split(delimeter);
            RowDisUnit = double.Parse(r[1]);

            ln = f1.ReadLine();
            r = ln.Split(delimeter);
            ColDisUnit = double.Parse(r[1]);

            ln = f1.ReadLine();
            r = ln.Split(delimeter);
            RowDisUnit_2 = double.Parse(r[1]);

            ln = f1.ReadLine();
            r = ln.Split(delimeter);
            ColDisUnit_2 = double.Parse(r[1]);

            ln = f1.ReadLine();
            r = ln.Split(delimeter);
            safeD = Int32.Parse(r[1]);

            ln = f1.ReadLine();

            DoorsPos = new position[NumOf_h];
            for (int i = 0; i < NumOf_h; i++)
            {
                ln = f1.ReadLine();
                r = ln.Split(delimeter);
                DoorsPos[i].r = Int32.Parse(r[1]);
                DoorsPos[i].n = Int32.Parse(r[2]);
            }

            ln = f1.ReadLine();
            CranesPos = new position[NumOf_m];
            for (int i = 0; i < NumOf_m; i++)
            {
                ln = f1.ReadLine();
                r = ln.Split(delimeter);
                CranesPos[i].r = Int32.Parse(r[1]);
                CranesPos[i].n = Int32.Parse(r[2]);
            }

            ln = f1.ReadLine(); ln = f1.ReadLine();
            N_th = new int[NumOf_t, NumOf_h];
            NeededType = new bool[NumOf_t];
            NumOf_outgoings = new int[NumOf_t];
            for (int i = 0; i < NumOf_t; i++)
            {
                ln = f1.ReadLine();
                r = ln.Split(delimeter);
                for (int j = 0; j < NumOf_h; j++)
                {
                    N_th[i, j] = Int32.Parse(r[j + 1]);
                    if (N_th[i, j] > 0)
                    {
                        NeededType[i] = true;
                        NumOf_outgoings[i] += N_th[i, j];
                    }
                }
            }
            ln = f1.ReadLine();
            coil_info = new coil[NumOfCoils];
            for (int i = 0; i < NumOfCoils; i++)
            {
                ln = f1.ReadLine();
                r = ln.Split(delimeter);
                coil_info[i] = new coil();
                coil_info[i].num = Int32.Parse(r[0]);
                coil_info[i].pos.r = Int32.Parse(r[1]);
                coil_info[i].pos.n = Int32.Parse(r[2]);
                coil_info[i].type = Int32.Parse(r[3]);
                coil_info[i].IsIncoming = (coil_info[i].type == 0);
                if (coil_info[i].IsIncoming) NumOf_incomings++;
            }
            f1.Close();
        }
        public double tf(position pos1, position pos2)
        {
            return Math.Max(Convert.ToDouble(D1(pos1.n, pos2.n)) / v1, Convert.ToDouble(D2(pos1.r, pos2.r)) / v2) + 2 * mu;
        }
        public double te(position pos1, position pos2)
        {
            return Math.Max(Convert.ToDouble(D1(pos1.n, pos2.n)) / lambda1, Convert.ToDouble(D2(pos1.r, pos2.r)) / lambda2);
        }
        public void SetArrange()
        {
            Mat_CL = new int[NumOf_r, NumOf_n];
            for (int i = 0; i < NumOfCoils; i++)
            {
                Mat_CL[coil_info[i].pos.r, coil_info[i].pos.n] = coil_info[i].num;
            }
        }
        void WriteInitDataCS(int ExNum)
        {
            char delimeter = '\t';
            string fn = "Ex" + ExNum + "_init.txt";
            StreamWriter f1 = new StreamWriter(fn);
            f1.WriteLine("number fo coils" + delimeter + NumOfCoils);
            f1.WriteLine("number of rows (r)" + delimeter + NumOf_r);
            f1.WriteLine("number fo columns (n)" + delimeter + NumOf_n);
            f1.WriteLine("number of doors" + delimeter + NumOf_h);
            f1.WriteLine("number of types" + delimeter + NumOf_t);
            f1.WriteLine("number of cranes" + delimeter + NumOf_m);
            f1.WriteLine("v1" + delimeter + v1);
            f1.WriteLine("v2" + delimeter + v2);
            f1.WriteLine("lambda1" + delimeter + lambda1);
            f1.WriteLine("lambda2" + delimeter + lambda2);
            f1.WriteLine("mu" + delimeter + mu);
            f1.WriteLine("RowDisUnit" + delimeter + RowDisUnit);
            f1.WriteLine("ColDisUnit" + delimeter + ColDisUnit);
            f1.WriteLine("RowDisUnit_2" + delimeter + RowDisUnit_2);
            f1.WriteLine("ColDisUnit_2" + delimeter + ColDisUnit_2);
            f1.WriteLine("safeDistance" + delimeter + safeD);

            f1.WriteLine("door" + delimeter + "r" + delimeter + "n");
            for (int h = 0; h < NumOf_h; h++)
            {
                f1.WriteLine("{0}\t{1}\t{2}", h + 1, DoorsPos[h].r, DoorsPos[h].n);
            }

            f1.WriteLine("crane" + delimeter + "r" + delimeter + "n");
            for (int m = 0; m < NumOf_m; m++)
            {
                f1.WriteLine("{0}\t{1}\t{2}", m + 1, CranesPos[m].r, CranesPos[m].n);
            }

            f1.WriteLine("\tdoor");
            f1.Write("Type\t");
            for (int h = 0; h < NumOf_h; h++)
                f1.Write("{0}\t", h + 1);
            f1.WriteLine();
            for (int t = 0; t < NumOf_t; t++)
            {
                f1.Write("{0}\t", t + 1);
                for (int h = 0; h < NumOf_h; h++)
                {
                    f1.Write("{0}\t", N_th[t, h]);
                }
                f1.WriteLine();
            }
            f1.WriteLine("coil\tr\tn\ttype");
            for (int i = 0; i < NumOfCoils; i++)
            {
                f1.WriteLine("{0}\t{1}\t{2}\t{3}", i + 1, coil_info[i].pos.r, coil_info[i].pos.n, coil_info[i].type);
            }
            f1.Close();
        }
        public void WriteData(int u)
        {
            WriteInitDataCS(u);
        }
    }
}
