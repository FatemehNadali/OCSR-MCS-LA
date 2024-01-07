using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace OCSRMCSLA
{
    class CoilInWait : CoilInSeq
    {
        public int mprim;    //selected crane
        public position dest;//selected location for coil
        public double ft;    //time for loaded crane to move from initial position to the selected location
        public double e;     //time for the nearest crane to empty move to the initial location of the selected coil
        public int ht;       //selected door
        public double delta; //delta 
        public position[,] p2; //temp location of cranes at the start of operation
        public position[,] q2;//temp location (column) of cranes at the end of the operation
        public CoilInWait(CoilInSeq S, int M, int J)
        {
            TP = S.TP;
            num = S.num;
            p2 = new position[M, J];
            q2 = new position[M, J];
        }
    }
    class BaseCoilType
    {
        public int num;        //number fo coil (starting from zero)
        public double pheromone; //containing pheromone
        public bool IsSelected;
    }
    class constructor
    {
        Random rn;
        instance ins;
        int J;                     //number of operations
        public double Cost;        // tghe value of objective function
        double alpha;              //alpha of GRASP 
        public CoilInSeq[] D;      // the set of coils to be operated
        public List<CoilInWait> E; //the set of candidate coils
        public List<CoilInWait> F; //the restricted set of candidate coils
        public List<CoilInSeq> seq; //sequence (output of the algorithm)
        public position[,] p;      //position of cranes at the start of the operation
        public position[,] q;      //position of cranes at the end of operations
        public position[] X;       //assigned position
        public int[,] IsBlocking; //show that the position (r,n) blocked how many outgoing coils in the sequence
        public int[] AssignedCrane;//assigned crane to operation
        public double[] S;
        public double[] C;
        public List<position> Yset; //set of positions
        public int[,] Mat_CL2;
        public int[,] N_th2;
        public int[] km;
        public List<int>[] Mj;
        public double[] fj;
        public double[] ej;
        public constructor(instance inst, CoilInSeq[] DD, double a, Random rr)
        {
            rn = rr;
            alpha = a;
            ins = inst;
            D = DD;

            IsBlocking = new int[ins.NumOf_r, ins.NumOf_n];
            for (int i = 0; i < DD.Length; i++)
            {
                CoilInSeq cl = DD[i];
                if (cl.TP == TypeInSeq.Outgoing && ins.coil_info[cl.num].pos.n % 2 == 1)
                {
                    if (ins.coil_info[cl.num].pos.n > 1) IsBlocking[ins.coil_info[cl.num].pos.r, ins.coil_info[cl.num].pos.n - 1]++;
                    if (ins.coil_info[cl.num].pos.n < ins.NumOf_n - 2) IsBlocking[ins.coil_info[cl.num].pos.r, ins.coil_info[cl.num].pos.n + 1]++;
                }
            }

            J = D.Length;
            Mat_CL2 = new int[ins.NumOf_r, ins.NumOf_n];
            for (int i = 0; i < ins.NumOf_r; i++)
                for (int j = 0; j < ins.NumOf_n; j++)
                {
                    Mat_CL2[i, j] = ins.Mat_CL[i, j];
                    if (Mat_CL2[i, j] > 0) Mat_CL2[i, j] += J + 1;
                }
            N_th2 = new int[ins.NumOf_t, ins.NumOf_h];
            for (int i = 0; i < ins.NumOf_t; i++)
                for (int j = 0; j < ins.NumOf_h; j++)
                {
                    N_th2[i, j] = ins.N_th[i, j];
                }
            km = new int[ins.NumOf_m];
            for (int m = 0; m < ins.NumOf_m; m++) km[m] = -1;
            fj = new double[J];
            ej = new double[J];
            Mj = new List<int>[J];
            p = new position[ins.NumOf_m, J];
            q = new position[ins.NumOf_m, J];
            X = new position[J];
            S = new double[J];
            C = new double[J];
            AssignedCrane = new int[J];

            Yset = new List<position>();
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
                            if (IsBlocking[i, j] == 0) Yset.Add(ps);
                        }
                    }
                }

            for (int j = 0; j < J; j++)
            {
                CoilInSeq cl = D[j];
                if (cl.TP != TypeInSeq.Incomming) Mat_CL2[ins.coil_info[cl.num].pos.r, ins.coil_info[cl.num].pos.n] = j + 1;
            }
        }
        private void LAP(CoilInWait s)
        {
            if (s.TP == TypeInSeq.Outgoing)
            {
                double minn = double.MaxValue;
                int htmp = ins.NumOf_h + 1;
                for (int h = 0; h < ins.NumOf_h; h++)
                {
                    if (N_th2[ins.coil_info[s.num].type - 1, h] > 0)
                    {
                        double tmp = ins.tf(ins.coil_info[s.num].pos, ins.DoorsPos[h]);
                        if (tmp < minn) { minn = tmp; htmp = h; }
                    }
                }
                s.ft = minn;
                s.ht = htmp;
                s.dest = ins.DoorsPos[htmp];
            }
            else
            {
                double minn = double.MaxValue;
                position argminpos;
                argminpos.r = -2; argminpos.n = -2;
                foreach (position y in Yset)
                {
                    double tmp = ins.tf(ins.coil_info[s.num].pos, y);
                    if (tmp < minn) { minn = tmp; argminpos = y; };
                }
                s.dest = argminpos;
                s.ft = minn;

            }
        }
        private void UAL(CoilInWait s, int j, int m)
        {
            if (s.TP == TypeInSeq.Outgoing || s.TP == TypeInSeq.Blocking)
                if (ins.coil_info[s.num].pos.n % 2 == 1)
                {
                    if (ins.coil_info[s.num].pos.n > 1)
                        if (IsBlocking[ins.coil_info[s.num].pos.r, ins.coil_info[s.num].pos.n - 1] > 0) IsBlocking[ins.coil_info[s.num].pos.r, ins.coil_info[s.num].pos.n - 1]--;
                    if (ins.coil_info[s.num].pos.n < ins.NumOf_n - 1)
                        if (IsBlocking[ins.coil_info[s.num].pos.r, ins.coil_info[s.num].pos.n + 1] > 0) IsBlocking[ins.coil_info[s.num].pos.r, ins.coil_info[s.num].pos.n + 1]--;
                }


            q[m, j] = s.dest;
            if (s.TP == TypeInSeq.Outgoing)
            {
                X[j] = s.dest;
                N_th2[ins.coil_info[s.num].type - 1, s.ht]--;
                if (IsBlocking[ins.coil_info[s.num].pos.r, ins.coil_info[s.num].pos.n] == 0) Yset.Add(ins.coil_info[s.num].pos);
                Mat_CL2[ins.coil_info[s.num].pos.r, ins.coil_info[s.num].pos.n] = 0;
                if (ins.coil_info[s.num].pos.n % 2 == 1)
                {
                    if (ins.coil_info[s.num].pos.n > 1) Mat_CL2[ins.coil_info[s.num].pos.r, ins.coil_info[s.num].pos.n - 1] = 0;
                    if (ins.coil_info[s.num].pos.n < ins.NumOf_n - 2) Mat_CL2[ins.coil_info[s.num].pos.r, ins.coil_info[s.num].pos.n - 1] = 0;

                    int cnt = 0;
                    int flg = 0;
                    while (cnt < Yset.Count)
                    {
                        if (Yset[cnt].r == ins.coil_info[s.num].pos.r && Yset[cnt].n == ins.coil_info[s.num].pos.n + 1)
                        {
                            flg++;
                            Yset.RemoveAt(cnt);
                            continue;
                        }
                        if (Yset[cnt].r == ins.coil_info[s.num].pos.r && Yset[cnt].n == ins.coil_info[s.num].pos.n - 1)
                        {
                            flg++;
                            Yset.RemoveAt(cnt);
                            continue;
                        }
                        cnt++;
                        if (flg == 2) break;
                    }
                }
                return;
            }
            else
            {
                Mat_CL2[s.dest.r, s.dest.n] = ins.coil_info[s.num].num;
                if (s.TP == TypeInSeq.Blocking) Mat_CL2[ins.coil_info[s.num].pos.r, ins.coil_info[s.num].pos.n] = 0;
                int cnt = 0;
                while (cnt < Yset.Count)
                {
                    if (Yset[cnt].r == s.dest.r && Yset[cnt].n == s.dest.n)
                    {
                        Yset.RemoveAt(cnt);
                        break;
                    }
                    cnt++;
                }
                if (s.dest.n % 2 == 1)
                {
                    if (s.dest.n > 1)
                        if (Mat_CL2[s.dest.r, s.dest.n - 2] > 0 && IsBlocking[s.dest.r, s.dest.n - 1] == 0)
                        {
                            position tmp_pos;
                            tmp_pos.r = s.dest.r;
                            tmp_pos.n = s.dest.n - 1;
                            Yset.Add(tmp_pos);
                        }
                    if (s.dest.n < ins.NumOf_n - 2)
                        if (Mat_CL2[s.dest.r, s.dest.n + 2] > 0 && IsBlocking[s.dest.r, s.dest.n + 1] == 0)
                        {
                            position tmp_pos;
                            tmp_pos.r = s.dest.r;
                            tmp_pos.n = s.dest.n + 1;
                            Yset.Add(tmp_pos);
                        }
                }
            }
        }
        private void CLF(CoilInWait s, int j, int m)
        {
            int kk = km[m] + 1;
            position q2_0; q2_0.n = -1; q2_0.r = -1;
            if (kk == 0) q2_0 = ins.CranesPos[m]; else s.q2[m, kk - 1] = q[m, kk - 1];
            for (int k = kk; k <= j; k++)
            {
                int mL = -1;
                foreach (int mp in Mj[k])
                    if (mp < m && mp > mL) mL = mp;
                int mR = ins.NumOf_m + 1;
                foreach (int mp in Mj[k])
                    if (mp > m && mp < mR) mR = mp;
                int rpMin = 0; int rqMin = 0;
                int rpMax = ins.NumOf_r - 1;
                int rqMax = ins.NumOf_r - 1;
                if (m > 0 && mL != -1)
                {
                    rpMin = p[mL, k].r + (m - mL) * ins.safeD;
                    rqMin = q[mL, k].r + (m - mL) * ins.safeD;
                }
                if (m < ins.NumOf_m - 1 && mR != ins.NumOf_m + 1)
                {
                    rpMax = p[mR, k].r + (m - mR) * ins.safeD;
                    rqMax = q[mR, k].r + (m - mR) * ins.safeD;
                }
                position pprim;
                if (k == 0)
                {
                    pprim.r = gr(q2_0.r, ins.coil_info[s.num].pos.r, rpMin, rpMax, ej[k]);
                    pprim.n = gn(q2_0.n, ins.coil_info[s.num].pos.n, ej[k]);
                }
                else
                {
                    pprim.r = gr(s.q2[m, k - 1].r, ins.coil_info[s.num].pos.r, rpMin, rpMax, ej[k]);
                    pprim.n = gn(s.q2[m, k - 1].n, ins.coil_info[s.num].pos.n, ej[k]);
                }
                s.p2[m, k] = pprim;
                position qprim;
                qprim.r = gr(s.p2[m, k].r, ins.coil_info[s.num].pos.r, rqMin, rqMax, fj[k]);
                qprim.n = gn(s.p2[m, k].n, ins.coil_info[s.num].pos.n, fj[k]);
                s.q2[m, k] = qprim;
            }
        }
        private int gr(int rx, int ry, int rmin, int rmax, double T)
        {
            int res;
            if (rx <= ry)
            {
                int maxx = rx;
                if (rx > rmax)
                    maxx = rmax;
                else
                {
                    for (int r = Math.Max(rx, rmin); r <= Math.Min(ry, rmax); r++)
                    {
                        if (tr(rx, r) <= T) maxx = r; else break;
                    }
                }
                res = maxx;
            }
            else
            {
                int minn = rx;
                if (rx < rmin)
                    minn = rmin;
                else
                {
                    for (int r = Math.Min(rx, rmax); r >= Math.Max(ry, rmin); r--)
                    {
                        if (tr(rx, r) <= T) minn = r; else break;
                    }
                }
                res = minn;
            }
            return res;
        }
        private int gn(int nx, int ny, double T)
        {
            int res;
            if (nx <= ny)
            {
                int maxx = nx;
                for (int n = nx; n <= ny; n++)
                {
                    if (tn(nx, n) <= T) maxx = n; else break;
                }
                res = maxx;
            }
            else
            {
                int minn = nx;
                for (int n = nx; n >= ny; n--)
                {
                    if (tn(nx, n) <= T) minn = n; else break;
                }
                res = minn;
            }
            return res;
        }
        private double tr(int r1, int r2)
        {
            return Convert.ToDouble(ins.D2(r1, r2)) / ins.lambda2;
        }
        private double tn(int n1, int n2)
        {
            return Convert.ToDouble(ins.D1(n1, n2)) / ins.lambda1;
        }
        private void CreateE()
        {
            E = new List<CoilInWait>();
            for (int j = 0; j < J; j++)
            {
                if (D[j].TP == TypeInSeq.Incomming || ins.coil_info[D[j].num].pos.n % 2 == 0)
                {
                    CoilInWait Etmp = new CoilInWait(D[j], ins.NumOf_m, J);
                    E.Add(Etmp);
                }
                else if (ins.coil_info[D[j].num].pos.n == 1 && Mat_CL2[ins.coil_info[D[j].num].pos.r, 2] == 0)
                {
                    CoilInWait Etmp = new CoilInWait(D[j], ins.NumOf_m, J);
                    E.Add(Etmp);
                }
                else if (ins.coil_info[D[j].num].pos.n == ins.NumOf_n - 2 && Mat_CL2[ins.coil_info[D[j].num].pos.r, ins.coil_info[D[j].num].pos.n - 1] == 0)
                {
                    CoilInWait Etmp = new CoilInWait(D[j], ins.NumOf_m, J);
                    E.Add(Etmp);
                }
                else if (Mat_CL2[ins.coil_info[D[j].num].pos.r, ins.coil_info[D[j].num].pos.n - 1] == 0 && Mat_CL2[ins.coil_info[D[j].num].pos.r, ins.coil_info[D[j].num].pos.n + 1] == 0)
                {
                    CoilInWait Etmp = new CoilInWait(D[j], ins.NumOf_m, J);
                    E.Add(Etmp);
                }

            }

        }
        public void GRC(bool Step10 = false)
        {
            seq = new List<CoilInSeq>();
            for (int j = 0; j < J; j++) Mj[j] = new List<int>();
            CreateE();
            F = new List<CoilInWait>();
            CoilInWait u = E[0]; 
            for (int k = 0; k < J; k++)
            {
                if (k > 0)
                {
                    foreach (CoilInWait s in E) s.q2[u.mprim, k - 1] = q[u.mprim, k - 1];
                    foreach (CoilInWait s in E)
                        for (int m2 = 0; m2 < ins.NumOf_m; m2++)
                            if (m2 != u.mprim) CLF(s, k - 1, m2);
                }

                //step3
                foreach (CoilInWait s in E)
                {
                    int mprim = ins.NumOf_m + 1;
                    double e_minn = double.MaxValue;
                    position tmp;
                    for (int m = 0; m < ins.NumOf_m; m++)
                    {
                        if (k == 0) tmp = ins.CranesPos[m]; else tmp = s.q2[m, k - 1];
                        double e_m = ins.te(tmp, ins.coil_info[s.num].pos);
                        if (e_m < e_minn) { e_minn = e_m; mprim = m; }
                    }
                    s.e = e_minn; s.mprim = mprim;
                }

                //step4
                foreach (CoilInWait s in E)
                {
                    LAP(s);
                    s.delta = s.e + s.ft;
                }
                //step5
                double DeltaMin = double.MaxValue; double DeltaMax = 0;
                for (int si = 0; si < E.Count; si++)
                {
                    if (E[si].delta > DeltaMax) DeltaMax = E[si].delta;
                    if (E[si].delta < DeltaMin) DeltaMin = E[si].delta;
                }

                double trs = DeltaMin + alpha * (DeltaMax - DeltaMin);
                foreach (CoilInWait s in E) if (s.delta <= trs) F.Add(s);
                int ui = rn.Next(F.Count);
                u = F[ui];
                F.Clear();
                UpdateE(u);
                AssignedCrane[k] = u.mprim;

                UAL(u, k, u.mprim);
                //step7
                for (int w = km[u.mprim] + 1; w <= k - 1; w++)
                {
                    Mj[w].Add(u.mprim);
                    p[u.mprim, w] = u.p2[u.mprim, w];
                    q[u.mprim, w] = u.q2[u.mprim, w];
                }
                //step8
                double CC = 0;
                if (k > 0) CC = C[k - 1];
                S[k] = CC + u.e;
                ej[k] = u.e;
                fj[k] = u.ft;
                C[k] = S[k] + u.ft;
                km[u.mprim] = k;
                Mj[k].Add(u.mprim);
                p[u.mprim, k] = ins.coil_info[u.num].pos;
                //step9
                CoilInSeq tmpc = new CoilInSeq();
                tmpc.TP = u.TP;
                tmpc.num = u.num;
                seq.Add(tmpc);
            }
            Cost = C[J - 1];
            //step10
            if (Step10)
            {
                for (int m = 0; m < ins.NumOf_m; m++)
                    for (int w = km[m] + 1; w < J; w++)
                    {
                        if (km[m] < 0)
                        {
                            p[m, w].n = ins.CranesPos[m].n;
                            q[m, w].n = ins.CranesPos[m].n;
                        }
                        else
                        {
                            p[m, w].n = q[m, km[m]].n;
                            q[m, w].n = q[m, km[m]].n;
                        }
                        int mL = -1;
                        int mR = ins.NumOf_m + 1;
                        foreach (int m2 in Mj[w])
                        {
                            if (m2 < m && m2 > mL) mL = m2;
                            if (m2 > m && m2 < mR) mR = m2;
                        }
                        bool flg = true;
                        int tmpP, tmpQ;
                        if (w == 0) { tmpP = ins.CranesPos[m].r; tmpQ = tmpP; } else { tmpP = p[m, w - 1].r; tmpQ = tmpP = q[m, w - 1].r; };
                        if (mL != -1)
                        {
                            if (tmpP < p[mL, w].r + ins.safeD * (m - mL))
                            {
                                p[m, w].r = p[mL, w].r + ins.safeD * (m - mL);
                                flg = false;
                            }
                        }
                        else if (mR < ins.NumOf_m + 1)
                        {
                            if (tmpP > p[mR, w].r + ins.safeD * (m - mR))
                            {
                                p[m, w].r = p[mR, w].r + ins.safeD * (m - mR);
                                flg = false;
                            }
                        }
                        if (flg) p[m, w].r = tmpQ;

                        flg = true;
                        if (mL != -1)
                        {
                            if (tmpQ < q[mL, w].r + ins.safeD * (m - mL))
                            {
                                q[m, w].r = q[mL, w].r + ins.safeD * (m - mL);
                                flg = false;
                            }
                        }
                        else if (mR < ins.NumOf_m + 1)
                        {
                            if (tmpQ > q[mR, w].r + ins.safeD * (m - mR))
                            {
                                q[m, w].r = q[mR, w].r + ins.safeD * (m - mR);
                                flg = false;
                            }
                        }
                        if (flg) q[m, w].r = p[m, w].r;
                    }

            }
        }
        public void UpdateE(CoilInWait s)
        {
            for (int i = 0; i < E.Count; i++)
                if (s.num == E[i].num) { E.RemoveAt(i); break; }
            if (s.TP == TypeInSeq.Incomming) return;
            if (IsBlocking[ins.coil_info[s.num].pos.r, ins.coil_info[s.num].pos.n] > 0)
            {
                if (ins.coil_info[s.num].pos.n == 2)
                {
                    if (Mat_CL2[ins.coil_info[s.num].pos.r, 1] < J + 1 && Mat_CL2[ins.coil_info[s.num].pos.r, 1] > 0)
                    {
                        CoilInWait Etmp = new CoilInWait(D[Mat_CL2[ins.coil_info[s.num].pos.r, 1] - 1], ins.NumOf_m, J);
                        E.Add(Etmp);
                    }
                }
                if (ins.coil_info[s.num].pos.n == ins.NumOf_n - 3)
                {
                    if (Mat_CL2[ins.coil_info[s.num].pos.r, ins.coil_info[s.num].pos.n + 1] < J + 1 && Mat_CL2[ins.coil_info[s.num].pos.r, ins.coil_info[s.num].pos.n + 1] > 0)
                    {
                        CoilInWait Etmp = new CoilInWait(D[Mat_CL2[ins.coil_info[s.num].pos.r, ins.coil_info[s.num].pos.n + 1] - 1], ins.NumOf_m, J);
                        E.Add(Etmp);
                    }
                }

                if (ins.coil_info[s.num].pos.n > 2)
                    if (Mat_CL2[ins.coil_info[s.num].pos.r, ins.coil_info[s.num].pos.n - 2] == 0 && Mat_CL2[ins.coil_info[s.num].pos.r, ins.coil_info[s.num].pos.n - 1] < J + 1 && Mat_CL2[ins.coil_info[s.num].pos.r, ins.coil_info[s.num].pos.n - 1] > 0)
                    {
                        CoilInWait Etmp = new CoilInWait(D[Mat_CL2[ins.coil_info[s.num].pos.r, ins.coil_info[s.num].pos.n - 1] - 1], ins.NumOf_m, J);
                        E.Add(Etmp);
                    }
                if (ins.coil_info[s.num].pos.n < ins.NumOf_n - 3)
                    if (Mat_CL2[ins.coil_info[s.num].pos.r, ins.coil_info[s.num].pos.n + 2] == 0 && Mat_CL2[ins.coil_info[s.num].pos.r, ins.coil_info[s.num].pos.n + 1] < J + 1 && Mat_CL2[ins.coil_info[s.num].pos.r, ins.coil_info[s.num].pos.n + 1] > 0)
                    {
                        CoilInWait Etmp = new CoilInWait(D[Mat_CL2[ins.coil_info[s.num].pos.r, ins.coil_info[s.num].pos.n + 1] - 1], ins.NumOf_m, J);
                        E.Add(Etmp);
                    }



            }
        }
        public void CopyToSolution(SolutionType sol)
        {
            sol.Cost = Cost;
            sol.Seq = seq;
        }
        public void CopyToFullSolution(FullSolutionType fsol)
        {
            fsol.S = new double[seq.Count];
            fsol.C = new double[seq.Count];
            fsol.fj = new double[seq.Count];
            fsol.ej = new double[seq.Count];
            fsol.X = new position[seq.Count];
            fsol.p = new position[ins.NumOf_m, seq.Count];
            fsol.q = new position[ins.NumOf_m, seq.Count];
            fsol.AssignedCrane = new int[seq.Count];
            fsol.Cost = Cost;
            fsol.Seq = seq;
            for (int j = 0; j < seq.Count; j++)
            {
                fsol.S[j] = S[j];
                fsol.C[j] = C[j];
                fsol.fj[j] = fj[j];
                fsol.ej[j] = ej[j];
                fsol.X[j] = X[j];
                fsol.AssignedCrane[j] = AssignedCrane[j];
                for (int m = 0; m < ins.NumOf_m; m++)
                {
                    fsol.p[m, j] = p[m, j];
                    fsol.q[m, j] = q[m, j];
                }
            }
        }
        public void PrintSequence()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("Obj : {0} ,  Seq : ", Cost);
            foreach (CoilInSeq i in seq)
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
                if (seq.IndexOf(i) != seq.Count - 1)
                    Console.Write(" - ");
            }
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();
        }
    }
    class GRASP
    {
        bool PrintTimeToFile;
        string FilNam; //filename 
        Random rn;
        instance ins;
        double alpha;          
        int GraspIt;            //GRASP iteration
        public CoilInSeq[] D;   
        public SolutionType BestSoFar;
        public FullSolutionType FullBestSoFar;
        public double BestCost; //the objective function value of best-so-far
        public double pr; 
        public GRASP(instance inst, List<CoilInSeq> Seq, double a, int iteratin, double pr2, Random r, bool PTF = false, string FN = "")
        {
            PrintTimeToFile = PTF;
            FilNam = FN;
            ins = inst;
            alpha = a;
            GraspIt = iteratin;
            pr = pr2;
            rn = r;
            D = new CoilInSeq[Seq.Count];
            for (int j = 0; j < Seq.Count; j++)
                D[j] = Seq[j];
        }
        public void Solve(bool flgZero = true, int TimeLimit = 360000000, bool PrintTime = false, bool PrintIterSol = false)
        {
            StreamWriter f1 = null;
            if (PrintTimeToFile) { f1 = new StreamWriter(FilNam); }

            var stw = new Stopwatch();
            stw.Start();
            BestCost = 10e20;
            BestSoFar = new SolutionType();
            for (int it = 0; it < GraspIt; it++)
            {
                double alpha2 = alpha;
                if (it == 0 && flgZero) alpha2 = 0;
                constructor grcp = new constructor(ins, D, alpha2, rn);
                grcp.GRC(true);
                //grcp.PrintSequence();
                bool BFC = false;
                if (grcp.Cost < BestCost)
                {
                    BFC = true;
                    //grcp.CopyToFullSolution(FullBestSoFar); 
                    grcp.CopyToSolution(BestSoFar);
                    BestCost = grcp.Cost;
                }
                if (PrintIterSol) PrintSolution(it, BestSoFar, false, BFC);

                if (PrintTimeToFile) f1.WriteLine("{0}\t{1}", (double)(stw.ElapsedMilliseconds) / 1000.0, BestSoFar.Cost);
                if (stw.ElapsedMilliseconds >= TimeLimit) break;
            }
            stw.Stop();
            if (PrintTime) Console.WriteLine("Elapsed Time : {0} Milli s.", stw.ElapsedMilliseconds);

            if (PrintTimeToFile) f1.Close();

            LS();
          
        }
        public void LS()
        {
            bool IsImproved = false;
            for (int i = 0; i < BestSoFar.Seq.Count - 1; i++)
            {
                int ii = BestSoFar.Seq[i].num;
                double p = rn.NextDouble();
                if (p <= pr)
                {
                    int k = 0;
                    if (BestSoFar.Seq[i].TP == TypeInSeq.Incomming || ins.coil_info[ii].pos.n % 2 == 1) k = i + 1;
                    else
                        for (int j = i + 1; j < BestSoFar.Seq.Count; j++)
                        {
                            int jj = BestSoFar.Seq[j].num;
                            bool flg = false;
                            if (ins.Mat_CL[ins.coil_info[ii].pos.r, ins.coil_info[ii].pos.n - 1] == jj + 1) flg = true;
                            if (ins.Mat_CL[ins.coil_info[ii].pos.r, ins.coil_info[ii].pos.n + 1] == jj + 1) flg = true;
                            if (!flg) { k = j; break; }
                        }
                    CoilInSeq tmp = new CoilInSeq();
                    tmp.num = BestSoFar.Seq[i].num;
                    tmp.TP = BestSoFar.Seq[i].TP;
                    BestSoFar.Seq[i] = BestSoFar.Seq[k];
                    BestSoFar.Seq[k] = tmp;

                    double BestOld = BestSoFar.Cost;
                    Phase2 objph2 = new Phase2(ins, BestSoFar);
                    objph2.Solve();
                    if (BestSoFar.Cost >= BestOld)
                    {
                        CoilInSeq tmp2 = new CoilInSeq();
                        tmp2.num = BestSoFar.Seq[i].num;
                        tmp2.TP = BestSoFar.Seq[i].TP;
                        BestSoFar.Seq[i] = BestSoFar.Seq[k];
                        BestSoFar.Seq[k] = tmp2;
                        BestSoFar.Cost = BestOld;
                    }
                    else IsImproved = true;
                }
            }
            BestSoFar.IsImprovedByLS = IsImproved;
        }
        static void PrintSolution(int iter, SolutionType sol, bool PS, bool BFC) //flgchange baraye test ast va badan hazf mishava az argomanha
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("Iter {0, -5}  Obj : ", iter);
            if (BFC)
                Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("{0} ", sol.Cost);
            if (BFC) Console.ForegroundColor = ConsoleColor.White;
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
