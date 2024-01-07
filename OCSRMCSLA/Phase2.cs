using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCSRMCSLA
{
    class SolutionType
    {
        public List<CoilInSeq> Seq;
        public double Cost;
        public bool IsImprovedByLS;
    }
    class FullSolutionType : SolutionType
    {
        public position[,] p;
        public position[,] q;
        public position[] X;
        public double[] S;
        public double[] C;
        public double[] fj;
        public double[] ej;
        public int[] AssignedCrane;

    }
    class Phase2
    {
        public instance ins;
        public SolutionType sol;
        public position[,] p;
        public position[,] q;
        public position[,] p2;
        public position[,] q2;
        public position[] X;
        public bool[,] IsBlocking;
        public int[] AssignedCrane;
        public double[] S;
        public double[] C;
        public List<position> Yset;
        public int[,] Mat_CL2;
        public int[,] N_th2;
        public int[] km; // k[m]
        public List<int>[] Mj;
        public double[] fj;
        public double[] ej;
        public Phase2(instance inst, SolutionType sol2)
        {
            ins = inst;
            sol = sol2;
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
            km = new int[ins.NumOf_m];
            for (int m = 0; m < ins.NumOf_m; m++) km[m] = -1;
            fj = new double[sol.Seq.Count];
            ej = new double[sol.Seq.Count];
            Mj = new List<int>[sol.Seq.Count];
            p = new position[ins.NumOf_m, sol.Seq.Count];
            q = new position[ins.NumOf_m, sol.Seq.Count];
            p2 = new position[ins.NumOf_m, sol.Seq.Count];
            q2 = new position[ins.NumOf_m, sol.Seq.Count];
            X = new position[sol.Seq.Count];
            S = new double[sol.Seq.Count];
            C = new double[sol.Seq.Count];
            AssignedCrane = new int[sol.Seq.Count];

            IsBlocking = new bool[ins.NumOf_r, ins.NumOf_n];
            foreach (CoilInSeq cl in sol.Seq)
            {
                if (cl.TP==TypeInSeq.Outgoing && ins.coil_info[cl.num].pos.n % 2 == 1)
                {
                    if (ins.coil_info[cl.num].pos.n > 1) IsBlocking[ins.coil_info[cl.num].pos.r, ins.coil_info[cl.num].pos.n - 1] = true;
                    if (ins.coil_info[cl.num].pos.n < ins.NumOf_n - 2) IsBlocking[ins.coil_info[cl.num].pos.r, ins.coil_info[cl.num].pos.n + 1] = true;
                }
            }

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
                            if (!IsBlocking[i, j]) Yset.Add(ps);
                        }
                        else if (Mat_CL2[i, j - 1] > 0 && Mat_CL2[i, j + 1] > 0)
                        {
                            position ps;
                            ps.r = i; ps.n = j;
                            if (!IsBlocking[i, j]) Yset.Add(ps);
                        }
                    }
                }

        }
        private void SubAlg1(int j, int m) //greedy location assignment 
        {
            CoilInSeq cl = sol.Seq[j]; 
            int i = cl.num;
            if (cl.TP == TypeInSeq.Outgoing)
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
                fj[j] = minn;
                X[j] = ins.DoorsPos[htmp];
                N_th2[ins.coil_info[i].type - 1, htmp]--;
                q[m, j] = ins.DoorsPos[htmp];
                if (!IsBlocking[ins.coil_info[i].pos.r, ins.coil_info[i].pos.n]) Yset.Add(ins.coil_info[i].pos);
                Mat_CL2[ins.coil_info[i].pos.r, ins.coil_info[i].pos.n] = 0;
                if (ins.coil_info[i].pos.n % 2 == 1)
                {
                    int cnt = 0;
                    int flg = 0;
                    while (cnt < Yset.Count)
                    {
                        if (Yset[cnt].r == ins.coil_info[i].pos.r && Yset[cnt].n == ins.coil_info[i].pos.n + 1)
                        {
                            flg++;
                            Yset.RemoveAt(cnt);
                            continue;
                        }
                        if (Yset[cnt].r == ins.coil_info[i].pos.r && Yset[cnt].n == ins.coil_info[i].pos.n - 1)
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
                double minn = double.MaxValue;
                position argminpos;
                argminpos.r = -2; argminpos.n = -2;
                foreach (position y in Yset)
                {
                    double tmp = ins.tf(ins.coil_info[i].pos, y);
                    if (tmp < minn) { minn = tmp; argminpos = y; };
                }
                X[j] = argminpos;
                fj[j] = minn;
                q[m, j] = argminpos;
                Mat_CL2[argminpos.r, argminpos.n] = ins.coil_info[i].num;
                int cnt = 0;
                while (cnt < Yset.Count)
                {
                    if (Yset[cnt].r == argminpos.r && Yset[cnt].n == argminpos.n)
                    {
                        Yset.RemoveAt(cnt);
                        break;
                    }
                    cnt++;
                }
                if (argminpos.n % 2 == 1)
                {
                    if (argminpos.n > 1)
                        if (Mat_CL2[argminpos.r, argminpos.n - 2] > 0)
                        {
                            position tmp_pos;
                            tmp_pos.r = argminpos.r;
                            tmp_pos.n = argminpos.n - 1;
                            Yset.Add(tmp_pos);
                        }
                    if (argminpos.n < ins.NumOf_n - 2)
                        if (Mat_CL2[argminpos.r, argminpos.n + 2] > 0)
                        {
                            position tmp_pos;
                            tmp_pos.r = argminpos.r;
                            tmp_pos.n = argminpos.n + 1;
                            Yset.Add(tmp_pos);
                        }

                }
            }
        }
        private void SubAlg2(int j, int m)
        {
            int kk = km[m] + 1;
            position q2_0; q2_0.n = -1; q2_0.r = -1;
            if (kk == 0) q2_0 = ins.CranesPos[m]; else q2[m, kk - 1] = q[m, kk - 1];
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
                    pprim.r = gr(q2_0.r, ins.coil_info[sol.Seq[j + 1].num].pos.r, rpMin, rpMax, ej[k]);
                    pprim.n = gn(q2_0.n, ins.coil_info[sol.Seq[j + 1].num].pos.n, ej[k]);
                }
                else
                {
                    pprim.r = gr(q2[m, k - 1].r, ins.coil_info[sol.Seq[j + 1].num].pos.r, rpMin, rpMax, ej[k]);
                    pprim.n = gn(q2[m, k - 1].n, ins.coil_info[sol.Seq[j + 1].num].pos.n, ej[k]);
                }
                p2[m, k] = pprim;
                position qprim;
                qprim.r = gr(p2[m, k].r, ins.coil_info[sol.Seq[j + 1].num].pos.r, rqMin, rqMax, fj[k]);
                qprim.n = gn(p2[m, k].n, ins.coil_info[sol.Seq[j + 1].num].pos.n, fj[k]);
                q2[m, k] = qprim;
            }

        }
        public void Solve()
        {
            int J = sol.Seq.Count;
            for (int j = 0; j < J; j++) Mj[j] = new List<int>();
            int i = sol.Seq[0].num;
            int mprim = ins.NumOf_m + 1;
            double e_minn = double.MaxValue;
            for (int m = 0; m < ins.NumOf_m; m++)
            {
                double e_m = ins.te(ins.CranesPos[m], ins.coil_info[i].pos);
                if (e_m < e_minn) { e_minn = e_m; mprim = m; }
            }
            ej[0] = e_minn;
            S[0] = e_minn;
            p[mprim, 0] = ins.coil_info[i].pos;
            km[mprim] = 0;
            Mj[0].Add(mprim);
            AssignedCrane[0] = mprim;
            SubAlg1(0, mprim);
            C[0] = S[0] + fj[0];
            for (int k = 1; k < J; k++)
            {
                i = sol.Seq[k].num;
                q2[mprim, k - 1] = q[mprim, k - 1];
                for (int m3 = 0; m3 < ins.NumOf_m; m3++)
                    if (m3 != mprim) SubAlg2(k - 1, m3);

                mprim = ins.NumOf_m + 1;
                e_minn = double.MaxValue;
                for (int m = 0; m < ins.NumOf_m; m++)
                {
                    double e_m = ins.te(q2[m, k - 1], ins.coil_info[i].pos);
                    if (e_m < e_minn) { e_minn = e_m; mprim = m; }
                }

                for (int w = km[mprim] + 1; w <= k - 1; w++)
                {
                    Mj[w].Add(mprim);
                    p[mprim, w] = p2[mprim, w];
                    q[mprim, w] = q2[mprim, w];
                }
                ej[k] = e_minn;
                S[k] = C[k - 1] + ej[k];
                km[mprim] = k;
                Mj[k].Add(mprim);
                AssignedCrane[k] = mprim;
                p[mprim, k] = ins.coil_info[i].pos;
                SubAlg1(k, mprim);
                C[k] = S[k] + fj[k];
            }

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
                    int tmpP,tmpQ;
                    if (w==0) { tmpP = ins.CranesPos[m].r; tmpQ = tmpP; } else { tmpP = p[m, w - 1].r; tmpQ = tmpP = q[m, w - 1].r; }; 
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
            sol.Cost = C[J - 1];
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
                    for (int r = Math.Max( rx, rmin); r <= Math.Min(ry, rmax); r++)
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
        public void CopyToFullSolution(FullSolutionType fsol)
        {
            fsol.S = new double[sol.Seq.Count];
            fsol.C = new double[sol.Seq.Count];
            fsol.fj = new double[sol.Seq.Count];
            fsol.ej = new double[sol.Seq.Count];
            fsol.X = new position[sol.Seq.Count];
            fsol.p = new position[ins.NumOf_m, sol.Seq.Count];
            fsol.q = new position[ins.NumOf_m, sol.Seq.Count];
            fsol.AssignedCrane = new int[sol.Seq.Count];
            fsol.Cost = sol.Cost;
            fsol.Seq = sol.Seq;
            for (int j = 0; j < sol.Seq.Count; j++)
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
    }
}
