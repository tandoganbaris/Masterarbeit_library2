using Google.OrTools.ConstraintSolver;
using Google.OrTools.LinearSolver;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Masterarbeit_library2;

public class Extreme_Algorithms : ICloneable
{
    //    4-------v3-------3
    //    |                |
    //    |                |
    //    v4               v2
    //    |                |
    //    |                | 
    //    1-------v1-------2

    // length is Y
    // width  is X
    // all vertices start with P1 and end with P2, dimensions increase so P1<P2 on the relevant axis
    internal List<Vertex2D> verticestoconsider { get; set; } = new List<Vertex2D>();
    internal List<ExtremePoint> ActiveExtremePoints { get; set; } = new List<ExtremePoint>();
    internal List<ExtremePoint> Notallowed { get; set; } = new List<ExtremePoint>(); //used and destroyed extreme points
    public int Chosen_maxdim { get; set; } = 150;
    public int Chosen_mindim { get; set; } = 1;
    public int Curve { get; set; } = 1; //0 soft 1 hard for penalty decision (depends on Opt)
    //
    public double A1 { get; set; } =1; //bottom
    public double A2 { get; set; } =3;//righside
    public double A3 { get; set; } =0;//topside
    public double A4 { get; set; } =2; //leftside
    public bool R { get; set; } = true; //rewarding perfect overlap
    public int Gamma { get; set; } = 30; //penalty for strip height incrrease
    public int Beta { get; set; } = 0; //preference factor of lower positions
    public bool VolumeUse { get; set; } = false; //using volume as a factor to decide
    public int StripHeight { get; set; } = 0;
    public double Largestvol { get; set; }
    public int Multiplier { get; set; } = 2; //in the case the mindim is 1 to prevent overlap
    public int Opt { get; set; } = 206; //to move the sigmoid curve for penalty 
    public double RatioBan { get; set; } = 100;
    public string RatioBanOrientation { get; set; } = "Horizontal";//"Vertical"; "Horizontal";
    public double Shadowsearchmultiplier { get; set; } = 2; //multiplies the maxdim
    public List<string> Errorlog { get; set; } = new List<string>();
    public List<Package2D> Input_packages { get; set; } = new List<Package2D>();
    public List<Package2D> Load_order { get; set; } = new List<Package2D>();
    public List<Package2D> Inbetween_loadorder { get; set; } = new List<Package2D>();
    internal List<Vertex2D> Virtual_Vertices { get; set; } = new List<Vertex2D>();
    internal Dictionary<Point2D, MasterRule> Rules { get; set; } = new Dictionary<Point2D, MasterRule>();
    public Package2D Bin { get; set; } = new Package2D(200, 300); //needs to be adjusted
    public Random rnd = new Random();
    public Extreme_Algorithms() { }

    public object Clone()
    {
        Extreme_Algorithms output = new Extreme_Algorithms();

        output.A1 = this.A1;
        output.A2 = this.A2;
        output.A3 = this.A3;
        output.A4 = this.A4;
        output.R = this.R; ;
        output.Gamma = this.Gamma;
        output.Beta = this.Beta;
        output.VolumeUse = this.VolumeUse;
        output.StripHeight = this.StripHeight;
        output.Largestvol = this.Largestvol;
        output.Multiplier = this.Multiplier;
        output.Opt = this.Opt;
        output.Bin = new Package2D(Bin.Width, Bin.Length);
        output.Chosen_maxdim = this.Chosen_maxdim;
        output.Chosen_mindim = this.Chosen_mindim;

        foreach (Package2D p in this.Input_packages)
        {
            output.Input_packages.Add((Package2D)p.Clone());
        }
        output.Reset_Runs();
        return output;

    }

    /// <summary>
    /// online input, placement is arbitrary, no rotation
    /// </summary>
    public void Main_OnUO()
    {
        List<Package2D> input = Input_packages.ToList();
        List<Package2D> loadorder = new List<Package2D>();
        loadorder.Add(Bin);
        loadorder.Add(input[0]);
        loadorder[0].OverwritePosition(0, 0, 1); //set the bin

        verticestoconsider.AddRange(loadorder[0].Vertixes); //add the vertices of the bin
        loadorder[1].OverwritePosition(0, 0, 1); //use first point as handle and place bottom left
        MasterRule r = new MasterRule(loadorder[1]);
        Rules.Add(r.Center, r);
        ExtremePoint FirstE_point = new ExtremePoint(Chosen_maxdim, 0, 0, 1);
        FirstE_point.Create_Space(verticestoconsider);
        ActiveExtremePoints.Clear(); ActiveExtremePoints.Add(FirstE_point);
        Refresh_ExtremePoints(loadorder[1], FirstE_point);
        Refresh_Vertices(loadorder[1], FirstE_point);
        input.Remove(input[0]);

        SortedList<double, List<Tuple<Package2D, ExtremePoint>>> FitnessList =
                new SortedList<double, List<Tuple<Package2D, ExtremePoint>>>();
        while (input.ToList().Count != 0)
        {


            foreach (ExtremePoint E in ActiveExtremePoints.ToList())// can add another loop for packs to do best fit
            {

                if (input.First().Indexes["Instance"] == 241 && E.X == 31 && E.Y == 120)
                {
                    string here = string.Empty;
                }
                bool needsspace = Simple_Overlapcheck_manual(E, Rules.Last().Value.Rulepoints);
                if ((needsspace == false) || (E.Space.Count == 0)) { E.Create_Space(Fetchrelevant_vertices(E)); }
                Package2D current_pack = input.First();
                current_pack.OverwritePosition(E.X, E.Y, 1); //move package to this Extreme Point, index is handle= P1
                Package2D current_pack_rotated = new Package2D(current_pack.Length, current_pack.Width);
                current_pack_rotated.Indexes.Add("Instance", current_pack.Indexes["Instance"]);
                current_pack_rotated.OverwritePosition(E.X, E.Y, 1);

                //bool fitsmaster = true;

                //foreach (MasterRule rule in Rules.Values)
                //{
                //    fitsmaster = rule.TestPoints(current_pack.Pointslist.ToList());
                //    if (fitsmaster == false) { fitsmaster = false; break; }
                //}
                //if (fitsmaster == false) { continue; }


                bool fits1 = E.Fitsinspace2(current_pack.Pointslist);
                bool fits2 = false;// E.Fitsinspace2(current_pack_rotated.Pointslist);
                if (!fits1 && !fits2) { continue; }
                if (fits1 && fits2) //if it fits within the boundaries
                {
                    double calculated_fitness = Fitness(current_pack, E);
                    double calculated_fitness_r = Fitness(current_pack_rotated, E);
                    if (calculated_fitness > calculated_fitness_r)
                    {
                        if (FitnessList.Keys.Contains(calculated_fitness))
                        {
                            FitnessList[calculated_fitness].Add(new Tuple<Package2D, ExtremePoint>(current_pack, E));

                        }
                        else
                        {
                            FitnessList.Add(calculated_fitness, new List<Tuple<Package2D, ExtremePoint>>() { new Tuple<Package2D, ExtremePoint>(current_pack, E) });
                        }

                    }
                    else
                    {
                        if (FitnessList.Keys.Contains(calculated_fitness_r))
                        {
                            FitnessList[calculated_fitness_r].Add(new Tuple<Package2D, ExtremePoint>(current_pack_rotated, E));

                        }
                        else
                        {
                            FitnessList.Add(calculated_fitness_r, new List<Tuple<Package2D, ExtremePoint>>() { new Tuple<Package2D, ExtremePoint>(current_pack_rotated, E) });
                        }

                    }

                }
                else if (fits1 && !fits2)
                {
                    double calculated_fitness = Fitness(current_pack, E);
                    if (FitnessList.Keys.Contains(calculated_fitness))
                    {
                        FitnessList[calculated_fitness].Add(new Tuple<Package2D, ExtremePoint>(current_pack, E));

                    }
                    else
                    {
                        FitnessList.Add(calculated_fitness, new List<Tuple<Package2D, ExtremePoint>>() { new Tuple<Package2D, ExtremePoint>(current_pack, E) });
                    }

                }
                else if (!fits1 && fits2)
                {
                    double calculated_fitness_r = Fitness(current_pack_rotated, E);
                    if (FitnessList.Keys.Contains(calculated_fitness_r))
                    {
                        FitnessList[calculated_fitness_r].Add(new Tuple<Package2D, ExtremePoint>(current_pack_rotated, E));

                    }
                    else
                    {
                        FitnessList.Add(calculated_fitness_r, new List<Tuple<Package2D, ExtremePoint>>() { new Tuple<Package2D, ExtremePoint>(current_pack_rotated, E) });
                    }

                }


            }

            Package2D chosenpack = FitnessList.Last().Value[0].Item1;
            ExtremePoint chosenE = FitnessList.Last().Value[0].Item2;
            chosenpack.OverwritePosition(chosenE.X, chosenE.Y, 1);
            loadorder.Add(chosenpack);
            Inbetween_loadorder = loadorder.ToList();
            if (chosenpack.Indexes["Instance"] == 240)
            {
                string here = string.Empty;
            }
            MasterRule r2 = new MasterRule(chosenpack);
            Rules.Add(r2.Center, r2);
            Refresh_ExtremePoints(chosenpack, chosenE);
            Refresh_Vertices(chosenpack, chosenE);
            FitnessList.Clear();
            input.Remove(input.First());

        }
        Load_order.Clear();
        Load_order.AddRange(loadorder);


        return;
    }
    /// <summary>
    /// online input, sequential placement, no rotation
    /// </summary>
    public void Main_OnSO()
    {
        List<Package2D> input = Input_packages.ToList();
        List<Package2D> loadorder = new List<Package2D>();
        loadorder.Add(Bin);
        loadorder.Add(input[0]);
        loadorder[0].OverwritePosition(0, 0, 1); //set the bin

        verticestoconsider.AddRange(loadorder[0].Vertixes); //add the vertices of the bin
        loadorder[1].OverwritePosition(0, 0, 1); //use first point as handle and place bottom left
        MasterRule r = new MasterRule(loadorder[1]);
        Rules.Add(r.Center, r);
        ExtremePoint FirstE_point = new ExtremePoint(Chosen_maxdim, 0, 0, 1);
        FirstE_point.Create_Space(verticestoconsider);
        ActiveExtremePoints.Clear(); ActiveExtremePoints.Add(FirstE_point);
        Refresh_ExtremePoints(loadorder[1], FirstE_point);
        Refresh_Vertices(loadorder[1], FirstE_point);
        input.Remove(input[0]);

        SortedList<double, List<Tuple<Package2D, ExtremePoint>>> FitnessList =
                new SortedList<double, List<Tuple<Package2D, ExtremePoint>>>();
        while (input.ToList().Count != 0)
        {


            foreach (ExtremePoint E in ActiveExtremePoints.ToList())// can add another loop for packs to do best fit
            {

                if (input.First().Indexes["Instance"] == 99 && E.X == 174 && E.Y == 103)
                {
                    string here = string.Empty;
                }
                bool needsspace = Simple_Overlapcheck_manual(E, Rules.Last().Value.Rulepoints);
                if ((needsspace == false) || (E.Space.Count == 0)) { E.Create_Space(Fetchrelevant_vertices(E)); }
                Package2D current_pack = input.First();
                current_pack.OverwritePosition(E.X, E.Y, 1); //move package to this Extreme Point, index is handle= P1
                bool fitssequential = Sequential_Overlapcheck(E, current_pack);
                if (!fitssequential) { continue; }



                bool fits1 = E.Fitsinspace2(current_pack.Pointslist);

                if (!fits1) { continue; }

                else if (fits1)
                {
                    double calculated_fitness = Fitness(current_pack, E);
                    if (FitnessList.Keys.Contains(calculated_fitness))
                    {
                        FitnessList[calculated_fitness].Add(new Tuple<Package2D, ExtremePoint>(current_pack, E));

                    }
                    else
                    {
                        FitnessList.Add(calculated_fitness, new List<Tuple<Package2D, ExtremePoint>>() { new Tuple<Package2D, ExtremePoint>(current_pack, E) });
                    }

                }


            }

            Package2D chosenpack = FitnessList.Last().Value[0].Item1;
            ExtremePoint chosenE = FitnessList.Last().Value[0].Item2;
            chosenpack.OverwritePosition(chosenE.X, chosenE.Y, 1);
            loadorder.Add(chosenpack);

            if (chosenpack.Indexes["Instance"] == 176)
            {
                string here = string.Empty;
            }
            MasterRule r2 = new MasterRule(chosenpack);
            Rules.Add(r2.Center, r2);
            Refresh_ExtremePoints(chosenpack, chosenE);
            Refresh_Vertices(chosenpack, chosenE);
            FitnessList.Clear();
            input.Remove(input.First());

        }
        Load_order.Clear();
        Load_order.AddRange(loadorder);


        return;
    }
    /// <summary>
    ///  online input, placement is arbitrary, with dynamic rotation
    /// </summary>
    public void Main_OnUR()
    {
        List<Package2D> input = Input_packages.ToList();
        List<Package2D> loadorder = new List<Package2D>();
        loadorder.Add(Bin);
        loadorder.Add(input[0]);
        loadorder[0].OverwritePosition(0, 0, 1); //set the bin

        verticestoconsider.AddRange(loadorder[0].Vertixes); //add the vertices of the bin
        loadorder[1].OverwritePosition(0, 0, 1); //use first point as handle and place bottom left
        if (loadorder[1].Width < loadorder[1].Length) { loadorder[1].Rotate(); }
        MasterRule r = new MasterRule(loadorder[1]);
        Rules.Add(r.Center, r);
        ExtremePoint FirstE_point = new ExtremePoint(Chosen_maxdim, 0, 0, 1);
        FirstE_point.Create_Space(verticestoconsider);
        ActiveExtremePoints.Clear(); ActiveExtremePoints.Add(FirstE_point);
        Refresh_ExtremePoints(loadorder[1], FirstE_point);
        Refresh_Vertices(loadorder[1], FirstE_point);
        input.Remove(input[0]);

        SortedList<double, List<Tuple<Package2D, ExtremePoint>>> FitnessList =
                new SortedList<double, List<Tuple<Package2D, ExtremePoint>>>();
        while (input.ToList().Count != 0)
        {


            foreach (ExtremePoint E in ActiveExtremePoints.ToList())// can add another loop for packs to do best fit
            {

                if (input.First().Indexes["Instance"] == 81)// && E.X == 72 && E.Y == 85)
                {
                    string here = string.Empty;
                }
                bool needsspace = Simple_Overlapcheck_manual(E, Rules.Last().Value.Rulepoints);
                if ((needsspace == false) || (E.Space.Count == 0)) { E.Create_Space(Fetchrelevant_vertices(E)); }
                Package2D current_pack = input.First();
                current_pack.OverwritePosition(E.X, E.Y, 1); //move package to this Extreme Point, index is handle= P1
                Package2D current_pack_rotated = new Package2D(current_pack.Length, current_pack.Width);
                current_pack_rotated.Indexes.Add("Instance", current_pack.Indexes["Instance"]);
                current_pack_rotated.OverwritePosition(E.X, E.Y, 1);

                //bool fitsmaster = true;

                //foreach (MasterRule rule in Rules.Values)
                //{
                //    fitsmaster = rule.TestPoints(current_pack.Pointslist.ToList());
                //    if (fitsmaster == false) { fitsmaster = false; break; }
                //}
                //if (fitsmaster == false) { continue; }


                bool fits1 = E.Fitsinspace2(current_pack.Pointslist);
                bool fits2 = E.Fitsinspace2(current_pack_rotated.Pointslist);
                if (!fits1 && !fits2) { continue; }
                if (fits1 && fits2) //if it fits within the boundaries
                {
                    double calculated_fitness = Fitness(current_pack, E);
                    double calculated_fitness_r = Fitness(current_pack_rotated, E);
                    if (calculated_fitness > calculated_fitness_r)
                    {
                        if (FitnessList.Keys.Contains(calculated_fitness))
                        {
                            FitnessList[calculated_fitness].Add(new Tuple<Package2D, ExtremePoint>(current_pack, E));

                        }
                        else
                        {
                            FitnessList.Add(calculated_fitness, new List<Tuple<Package2D, ExtremePoint>>() { new Tuple<Package2D, ExtremePoint>(current_pack, E) });
                        }

                    }
                    else
                    {
                        if (FitnessList.Keys.Contains(calculated_fitness_r))
                        {
                            FitnessList[calculated_fitness_r].Add(new Tuple<Package2D, ExtremePoint>(current_pack_rotated, E));

                        }
                        else
                        {
                            FitnessList.Add(calculated_fitness_r, new List<Tuple<Package2D, ExtremePoint>>() { new Tuple<Package2D, ExtremePoint>(current_pack_rotated, E) });
                        }

                    }

                }
                else if (fits1 && !fits2)
                {
                    double calculated_fitness = Fitness(current_pack, E);
                    if (FitnessList.Keys.Contains(calculated_fitness))
                    {
                        FitnessList[calculated_fitness].Add(new Tuple<Package2D, ExtremePoint>(current_pack, E));

                    }
                    else
                    {
                        FitnessList.Add(calculated_fitness, new List<Tuple<Package2D, ExtremePoint>>() { new Tuple<Package2D, ExtremePoint>(current_pack, E) });
                    }

                }
                else if (!fits1 && fits2)
                {
                    double calculated_fitness_r = Fitness(current_pack_rotated, E);
                    if (FitnessList.Keys.Contains(calculated_fitness_r))
                    {
                        FitnessList[calculated_fitness_r].Add(new Tuple<Package2D, ExtremePoint>(current_pack_rotated, E));

                    }
                    else
                    {
                        FitnessList.Add(calculated_fitness_r, new List<Tuple<Package2D, ExtremePoint>>() { new Tuple<Package2D, ExtremePoint>(current_pack_rotated, E) });
                    }

                }


            }
            if (FitnessList.Count == 0) { break; }
            Package2D chosenpack = FitnessList.Last().Value[0].Item1;
            ExtremePoint chosenE = FitnessList.Last().Value[0].Item2;
            chosenpack.OverwritePosition(chosenE.X, chosenE.Y, 1);
            loadorder.Add(chosenpack);

            if (chosenpack.Indexes["Instance"] == 83)
            {
                string here = string.Empty;
            }
            MasterRule r2 = new MasterRule(chosenpack);
            Rules.Add(r2.Center, r2);
            Refresh_ExtremePoints(chosenpack, chosenE);
            Refresh_Vertices(chosenpack, chosenE);
            FitnessList.Clear();
            input.Remove(input.First());

        }
        Load_order.Clear();
        Load_order.AddRange(loadorder);


        return;
    }
    /// <summary>
    /// online input, sequential placement, dynamic rotation
    /// </summary>
    public void Main_OnSR()
    {
        List<Package2D> input = Input_packages.ToList();
        List<Package2D> loadorder = new List<Package2D>();
        loadorder.Add(Bin);
        loadorder.Add(input[0]);
        loadorder[0].OverwritePosition(0, 0, 1); //set the bin

        verticestoconsider.AddRange(loadorder[0].Vertixes); //add the vertices of the bin
        loadorder[1].OverwritePosition(0, 0, 1); //use first point as handle and place bottom left
        if (loadorder[1].Width < loadorder[1].Length) { loadorder[1].Rotate(); }
        MasterRule r = new MasterRule(loadorder[1]);
        Rules.Add(r.Center, r);
        ExtremePoint FirstE_point = new ExtremePoint(Chosen_maxdim, 0, 0, 1);
        FirstE_point.Create_Space(verticestoconsider);
        ActiveExtremePoints.Clear(); ActiveExtremePoints.Add(FirstE_point);
        Refresh_ExtremePoints(loadorder[1], FirstE_point);
        Refresh_Vertices(loadorder[1], FirstE_point);
        input.Remove(input[0]);

        SortedList<double, List<Tuple<Package2D, ExtremePoint>>> FitnessList =
                new SortedList<double, List<Tuple<Package2D, ExtremePoint>>>();
        while (input.ToList().Count != 0)
        {


            foreach (ExtremePoint E in ActiveExtremePoints.ToList())// can add another loop for packs to do best fit
            {


                bool needsspace = Simple_Overlapcheck_manual(E, Rules.Last().Value.Rulepoints);
                if ((needsspace == false) || (E.Space.Count == 0)) { E.Create_Space(Fetchrelevant_vertices(E)); }
                Package2D current_pack = input.First();
                current_pack.OverwritePosition(E.X, E.Y, 1); //move package to this Extreme Point, index is handle= P1
                bool fits1 = true;
                bool fitssequential = Sequential_Overlapcheck(E, current_pack);
                if (!fitssequential) { fits1 = false; }
                else { fits1 = E.Fitsinspace2(current_pack.Pointslist); }

                Package2D current_pack_rotated = new Package2D(current_pack.Length, current_pack.Width);
                current_pack_rotated.Indexes.Add("Instance", current_pack.Indexes["Instance"]);
                current_pack_rotated.OverwritePosition(E.X, E.Y, 1);
                bool fits2 = true;
                bool fitssequential2 = Sequential_Overlapcheck(E, current_pack_rotated);
                if (!fitssequential2) { fits2 = false; }
                else { fits2 = E.Fitsinspace2(current_pack_rotated.Pointslist); }


                if (!fits1 && !fits2) { continue; }
                if (fits1 && fits2) //if it fits within the boundaries
                {
                    double calculated_fitness = Fitness(current_pack, E);
                    double calculated_fitness_r = Fitness(current_pack_rotated, E);
                    if (calculated_fitness > calculated_fitness_r)
                    {
                        if (FitnessList.Keys.Contains(calculated_fitness))
                        {
                            FitnessList[calculated_fitness].Add(new Tuple<Package2D, ExtremePoint>(current_pack, E));

                        }
                        else
                        {
                            FitnessList.Add(calculated_fitness, new List<Tuple<Package2D, ExtremePoint>>() { new Tuple<Package2D, ExtremePoint>(current_pack, E) });
                        }

                    }
                    else
                    {
                        if (FitnessList.Keys.Contains(calculated_fitness_r))
                        {
                            FitnessList[calculated_fitness_r].Add(new Tuple<Package2D, ExtremePoint>(current_pack_rotated, E));

                        }
                        else
                        {
                            FitnessList.Add(calculated_fitness_r, new List<Tuple<Package2D, ExtremePoint>>() { new Tuple<Package2D, ExtremePoint>(current_pack_rotated, E) });
                        }

                    }

                }
                else if (fits1 && !fits2)
                {
                    double calculated_fitness = Fitness(current_pack, E);
                    if (FitnessList.Keys.Contains(calculated_fitness))
                    {
                        FitnessList[calculated_fitness].Add(new Tuple<Package2D, ExtremePoint>(current_pack, E));

                    }
                    else
                    {
                        FitnessList.Add(calculated_fitness, new List<Tuple<Package2D, ExtremePoint>>() { new Tuple<Package2D, ExtremePoint>(current_pack, E) });
                    }

                }
                else if (!fits1 && fits2)
                {
                    double calculated_fitness_r = Fitness(current_pack_rotated, E);
                    if (FitnessList.Keys.Contains(calculated_fitness_r))
                    {
                        FitnessList[calculated_fitness_r].Add(new Tuple<Package2D, ExtremePoint>(current_pack_rotated, E));

                    }
                    else
                    {
                        FitnessList.Add(calculated_fitness_r, new List<Tuple<Package2D, ExtremePoint>>() { new Tuple<Package2D, ExtremePoint>(current_pack_rotated, E) });
                    }

                }


            }

            Package2D chosenpack = FitnessList.Last().Value[0].Item1;
            ExtremePoint chosenE = FitnessList.Last().Value[0].Item2;
            chosenpack.OverwritePosition(chosenE.X, chosenE.Y, 1);
            loadorder.Add(chosenpack);

            if (chosenpack.Indexes["Instance"] == 83)
            {
                string here = string.Empty;
            }
            MasterRule r2 = new MasterRule(chosenpack);
            Rules.Add(r2.Center, r2);
            Refresh_ExtremePoints(chosenpack, chosenE);
            Refresh_Vertices(chosenpack, chosenE);
            FitnessList.Clear();
            input.Remove(input.First());

        }
        Load_order.Clear();
        Load_order.AddRange(loadorder);


        return;
    }
    public void Main_OnSRPreR()
    {
        List<Package2D> input = Input_packages.ToList();
        List<Package2D> loadorder = new List<Package2D>();
        loadorder.Add(Bin);
        loadorder.Add(input[0]);
        loadorder[0].OverwritePosition(0, 0, 1); //set the bin

        verticestoconsider.AddRange(loadorder[0].Vertixes); //add the vertices of the bin
        loadorder[1].OverwritePosition(0, 0, 1); //use first point as handle and place bottom left
        if (loadorder[1].Width < loadorder[1].Length) { loadorder[1].Rotate(); }
        MasterRule r = new MasterRule(loadorder[1]);
        Rules.Add(r.Center, r);
        ExtremePoint FirstE_point = new ExtremePoint(Chosen_maxdim, 0, 0, 1);
        FirstE_point.Create_Space(verticestoconsider);
        ActiveExtremePoints.Clear(); ActiveExtremePoints.Add(FirstE_point);
        Refresh_ExtremePoints(loadorder[1], FirstE_point);
        Refresh_Vertices(loadorder[1], FirstE_point);
        input.Remove(input[0]);

        SortedList<double, List<Tuple<Package2D, ExtremePoint>>> FitnessList =
                new SortedList<double, List<Tuple<Package2D, ExtremePoint>>>();
        while (input.ToList().Count != 0)
        {


            foreach (ExtremePoint E in ActiveExtremePoints.ToList())// can add another loop for packs to do best fit
            {

                if (input.First().Indexes["Instance"] == 99 && E.X == 174 && E.Y == 103)
                {
                    string here = string.Empty;
                }
                bool needsspace = Simple_Overlapcheck_manual(E, Rules.Last().Value.Rulepoints);
                if ((needsspace == false) || (E.Space.Count == 0)) { E.Create_Space(Fetchrelevant_vertices(E)); }
                Package2D current_pack = input.First();
                current_pack.OverwritePosition(E.X, E.Y, 1); //move package to this Extreme Point, index is handle= P1
                if (current_pack.Width < current_pack.Length) { current_pack.Rotate(); }
                bool fitssequential = Sequential_Overlapcheck(E, current_pack);
                if (!fitssequential) { continue; }



                bool fits1 = E.Fitsinspace2(current_pack.Pointslist);

                if (!fits1) { continue; }

                else if (fits1)
                {
                    double calculated_fitness = Fitness(current_pack, E);
                    if (FitnessList.Keys.Contains(calculated_fitness))
                    {
                        FitnessList[calculated_fitness].Add(new Tuple<Package2D, ExtremePoint>(current_pack, E));

                    }
                    else
                    {
                        FitnessList.Add(calculated_fitness, new List<Tuple<Package2D, ExtremePoint>>() { new Tuple<Package2D, ExtremePoint>(current_pack, E) });
                    }

                }


            }

            Package2D chosenpack = FitnessList.Last().Value[0].Item1;
            ExtremePoint chosenE = FitnessList.Last().Value[0].Item2;
            chosenpack.OverwritePosition(chosenE.X, chosenE.Y, 1);
            loadorder.Add(chosenpack);

            if (chosenpack.Indexes["Instance"] == 176)
            {
                string here = string.Empty;
            }
            MasterRule r2 = new MasterRule(chosenpack);
            Rules.Add(r2.Center, r2);
            Refresh_ExtremePoints(chosenpack, chosenE);
            Refresh_Vertices(chosenpack, chosenE);
            FitnessList.Clear();
            input.Remove(input.First());

        }
        Load_order.Clear();
        Load_order.AddRange(loadorder);


        return;
    }
    /// <summary>
    /// online input, placement is arbitrary, with pre rotation
    /// </summary>
    public void Main_OnURPreR()
    {
        List<Package2D> input = Input_packages.ToList();
        List<Package2D> loadorder = new List<Package2D>();
        loadorder.Add(Bin);
        loadorder.Add(input[0]);
        loadorder[0].OverwritePosition(0, 0, 1); //set the bin

        verticestoconsider.AddRange(loadorder[0].Vertixes); //add the vertices of the bin

        loadorder[1].OverwritePosition(0, 0, 1); //use first point as handle and place bottom left
        loadorder[1].Rotate();
        MasterRule r = new MasterRule(loadorder[1]);
        Rules.Add(r.Center, r);
        ExtremePoint FirstE_point = new ExtremePoint(Chosen_maxdim, 0, 0, 1);
        FirstE_point.Create_Space(verticestoconsider);
        ActiveExtremePoints.Clear(); ActiveExtremePoints.Add(FirstE_point);
        Refresh_ExtremePoints(loadorder[1], FirstE_point);
        Refresh_Vertices(loadorder[1], FirstE_point);
        input.Remove(input[0]);

        SortedList<double, List<Tuple<Package2D, ExtremePoint>>> FitnessList =
                new SortedList<double, List<Tuple<Package2D, ExtremePoint>>>();
        while (input.ToList().Count != 0)
        {


            foreach (ExtremePoint E in ActiveExtremePoints.ToList())// can add another loop for packs to do best fit
            {

                if (input.First().Indexes["Instance"] == 172 && E.X == 0 && E.Y == 212)
                {
                    string here = string.Empty;
                }
                bool needsspace = Simple_Overlapcheck_manual(E, Rules.Last().Value.Rulepoints);
                if ((needsspace == false) || (E.Space.Count == 0)) { E.Create_Space(Fetchrelevant_vertices(E)); }
                Package2D current_pack = input.First();
                current_pack.OverwritePosition(E.X, E.Y, 1); //move package to this Extreme Point, index is handle= P1
                if (current_pack.Width < current_pack.Length) { current_pack.Rotate(); }

                bool fits1 = E.Fitsinspace2(current_pack.Pointslist);

                if (!fits1) { continue; }

                else if (fits1)
                {
                    double calculated_fitness = Fitness(current_pack, E);
                    if (FitnessList.Keys.Contains(calculated_fitness))
                    {
                        FitnessList[calculated_fitness].Add(new Tuple<Package2D, ExtremePoint>(current_pack, E));

                    }
                    else
                    {
                        FitnessList.Add(calculated_fitness, new List<Tuple<Package2D, ExtremePoint>>() { new Tuple<Package2D, ExtremePoint>(current_pack, E) });
                    }

                }



            }

            Package2D chosenpack = FitnessList.Last().Value[0].Item1;
            ExtremePoint chosenE = FitnessList.Last().Value[0].Item2;
            chosenpack.OverwritePosition(chosenE.X, chosenE.Y, 1);
            loadorder.Add(chosenpack);

            if (chosenpack.Indexes["Instance"] == 175)
            {
                string here = string.Empty;
            }
            MasterRule r2 = new MasterRule(chosenpack);
            Rules.Add(r2.Center, r2);
            Refresh_ExtremePoints(chosenpack, chosenE);
            Refresh_Vertices(chosenpack, chosenE);
            FitnessList.Clear();
            input.Remove(input.First());

        }
        Load_order.Clear();
        Load_order.AddRange(loadorder);


        return;
    }
    /// <summary>
    /// offline input, placement is arbitrary, no rotation
    /// </summary>
    public void Main_OffUO()
    {
        List<Package2D> input = Input_packages.ToList();
        List<Package2D> loadorder = new List<Package2D>();
        loadorder.Add(Bin);
        input = input.OrderByDescending(x => x.Volume).ToList(); //The Prep
        //loadorder.Add(input[0]);
        loadorder[0].OverwritePosition(0, 0, 1); //set the bin

        verticestoconsider.AddRange(loadorder[0].Vertixes); //add the vertices of the bin
        //loadorder[1].OverwritePosition(0, 0, 1); //use first point as handle and place bottom left
        //if (loadorder[1].Width < loadorder[1].Length) { loadorder[1].Rotate(); }
        //MasterRule r = new MasterRule(loadorder[1]);
        //Rules.Add(r.Center, r);
        ExtremePoint FirstE_point = new ExtremePoint(Chosen_maxdim, 0, 0, 1);
        FirstE_point.Create_Space(verticestoconsider);
        ActiveExtremePoints.Clear(); ActiveExtremePoints.Add(FirstE_point);
        //Refresh_ExtremePoints(loadorder[1], FirstE_point);
        //Refresh_Vertices(loadorder[1], FirstE_point);
        //input.Remove(input[0]);

        SortedList<double, List<Tuple<Package2D, ExtremePoint>>> FitnessList =
                new SortedList<double, List<Tuple<Package2D, ExtremePoint>>>();
        while (input.ToList().Count != 0)
        {


            foreach (ExtremePoint E in ActiveExtremePoints.ToList())// can add another loop for packs to do best fit
            {

                bool needsspace = true;
                if (Rules.Count != 0) { needsspace = Simple_Overlapcheck_manual(E, Rules.Last().Value.Rulepoints); }
                if ((needsspace == false) || (E.Space.Count == 0)) { E.Create_Space(Fetchrelevant_vertices(E)); }
                foreach (Package2D p in input.ToList())
                {
                    if (loadorder.Count == 134 && p.Indexes["Instance"] == 8 && E.Y == 145 && E.X < 85)
                    {
                        string here = string.Empty;
                    }
                    Package2D current_pack = p;
                    current_pack.OverwritePosition(E.X, E.Y, 1); //move package to this Extreme Point, index is handle= P1
                    Package2D current_pack_rotated = new Package2D(current_pack.Length, current_pack.Width);
                    current_pack_rotated.Indexes.Add("Instance", current_pack.Indexes["Instance"]);
                    current_pack_rotated.OverwritePosition(E.X, E.Y, 1);
                    bool fits1 = E.Fitsinspace2(current_pack.Pointslist);

                    if (!fits1) { continue; }

                    else if (fits1)
                    {
                        double calculated_fitness = Fitness(current_pack, E);
                        if (FitnessList.Keys.Contains(calculated_fitness))
                        {
                            FitnessList[calculated_fitness].Add(new Tuple<Package2D, ExtremePoint>(current_pack, E));

                        }
                        else
                        {
                            FitnessList.Add(calculated_fitness, new List<Tuple<Package2D, ExtremePoint>>() { new Tuple<Package2D, ExtremePoint>(current_pack, E) });
                        }

                    }


                }


            }

            Package2D chosenpack = FitnessList.Last().Value[0].Item1;
            ExtremePoint chosenE = FitnessList.Last().Value[0].Item2;
            if (chosenpack.Indexes["Instance"] == 8)//p. && 
            {
                string here = string.Empty;
            }
            chosenpack.OverwritePosition(chosenE.X, chosenE.Y, 1);
            loadorder.Add(chosenpack);
            MasterRule r2 = new MasterRule(chosenpack);
            Rules.Add(r2.Center, r2);
            Refresh_ExtremePoints(chosenpack, chosenE);
            Refresh_Vertices(chosenpack, chosenE);
            FitnessList.Clear();
            input.Remove(input.Where(x => x.Indexes["Instance"] == chosenpack.Indexes["Instance"]).ToList()[0]);

        }
        Load_order.Clear();
        Load_order.AddRange(loadorder);


        return;
    }
    /// <summary>
    /// offline input, placement is arbitrary, dynamic rotation
    /// </summary>
    public void Main_OffUR()
    {
        List<Package2D> input = Input_packages.ToList();
        List<Package2D> loadorder = new List<Package2D>();
        loadorder.Add(Bin);
        input = input.OrderByDescending(x => x.Largestdim).ToList(); //The Prep
        loadorder.Add(input[0]);
        loadorder[0].OverwritePosition(0, 0, 1); //set the bin

        verticestoconsider.AddRange(loadorder[0].Vertixes); //add the vertices of the bin
        loadorder[1].OverwritePosition(0, 0, 1); //use first point as handle and place bottom left
        if (loadorder[1].Width < loadorder[1].Length) { loadorder[1].Rotate(); }
        MasterRule r = new MasterRule(loadorder[1]);
        Rules.Add(r.Center, r);
        ExtremePoint FirstE_point = new ExtremePoint(Chosen_maxdim, 0, 0, 1);
        FirstE_point.Create_Space(verticestoconsider);
        ActiveExtremePoints.Clear(); ActiveExtremePoints.Add(FirstE_point);
        Refresh_ExtremePoints(loadorder[1], FirstE_point);
        Refresh_Vertices(loadorder[1], FirstE_point);
        input.Remove(input[0]);

        SortedList<double, List<Tuple<Package2D, ExtremePoint>>> FitnessList =
                new SortedList<double, List<Tuple<Package2D, ExtremePoint>>>();
        while (input.ToList().Count != 0)
        {

            //ActiveExtremePoints = ActiveExtremePoints.OrderBy(x => x.Y).ThenBy(x => x.X).ToList();
            foreach (ExtremePoint E in ActiveExtremePoints.ToList())// can add another loop for packs to do best fit
            {
                bool needsspace = Simple_Overlapcheck_manual(E, Rules.Last().Value.Rulepoints);
                if ((needsspace == false) || (E.Space.Count == 0)) { E.Create_Space(Fetchrelevant_vertices(E)); }
                foreach (Package2D p in input.ToList())
                {
                    Package2D current_pack = p;
                    current_pack.OverwritePosition(E.X, E.Y, 1); //move package to this Extreme Point, index is handle= P1
                    Package2D current_pack_rotated = new Package2D(current_pack.Length, current_pack.Width);
                    current_pack_rotated.Indexes.Add("Instance", current_pack.Indexes["Instance"]);
                    current_pack_rotated.OverwritePosition(E.X, E.Y, 1);


                    bool fits1 = E.Fitsinspace2(current_pack.Pointslist);
                    bool fits2 = E.Fitsinspace2(current_pack_rotated.Pointslist);
                    if (!fits1 && !fits2) { continue; }
                    if (fits1 && fits2) //if it fits within the boundaries
                    {
                        double calculated_fitness = Fitness(current_pack, E);
                        double calculated_fitness_r = Fitness(current_pack_rotated, E);
                        if (calculated_fitness > calculated_fitness_r)
                        {
                            if (FitnessList.Keys.Contains(calculated_fitness))
                            {
                                FitnessList[calculated_fitness].Add(new Tuple<Package2D, ExtremePoint>(current_pack, E));

                            }
                            else
                            {
                                FitnessList.Add(calculated_fitness, new List<Tuple<Package2D, ExtremePoint>>() { new Tuple<Package2D, ExtremePoint>(current_pack, E) });
                            }

                        }
                        else
                        {
                            if (FitnessList.Keys.Contains(calculated_fitness_r))
                            {
                                FitnessList[calculated_fitness_r].Add(new Tuple<Package2D, ExtremePoint>(current_pack_rotated, E));

                            }
                            else
                            {
                                FitnessList.Add(calculated_fitness_r, new List<Tuple<Package2D, ExtremePoint>>() { new Tuple<Package2D, ExtremePoint>(current_pack_rotated, E) });
                            }

                        }

                    }
                    else if (fits1 && !fits2)
                    {
                        double calculated_fitness = Fitness(current_pack, E);
                        if (FitnessList.Keys.Contains(calculated_fitness))
                        {
                            FitnessList[calculated_fitness].Add(new Tuple<Package2D, ExtremePoint>(current_pack, E));

                        }
                        else
                        {
                            FitnessList.Add(calculated_fitness, new List<Tuple<Package2D, ExtremePoint>>() { new Tuple<Package2D, ExtremePoint>(current_pack, E) });
                        }

                    }
                    else if (!fits1 && fits2)
                    {
                        double calculated_fitness_r = Fitness(current_pack_rotated, E);
                        if (FitnessList.Keys.Contains(calculated_fitness_r))
                        {
                            FitnessList[calculated_fitness_r].Add(new Tuple<Package2D, ExtremePoint>(current_pack_rotated, E));

                        }
                        else
                        {
                            FitnessList.Add(calculated_fitness_r, new List<Tuple<Package2D, ExtremePoint>>() { new Tuple<Package2D, ExtremePoint>(current_pack_rotated, E) });
                        }

                    }


                }


            }
            Package2D chosenpack = FitnessList.Last().Value[0].Item1;
            ExtremePoint chosenE = FitnessList.Last().Value[0].Item2;
            chosenpack.OverwritePosition(chosenE.X, chosenE.Y, 1);
            loadorder.Add(chosenpack);
            MasterRule r2 = new MasterRule(chosenpack);
            Rules.Add(r2.Center, r2);
            //if (chosenpack.Indexes["Instance"] == 22)//p. && 
            //{
            //    string here = string.Empty;
            //}
            Refresh_ExtremePoints(chosenpack, chosenE);
            Refresh_Vertices(chosenpack, chosenE);
            FitnessList.Clear();
            input.Remove(input.Where(x => x.Indexes["Instance"] == chosenpack.Indexes["Instance"]).ToList()[0]);

        }
        Load_order.Clear();
        Load_order.AddRange(loadorder);


        return;
    }
    /// <summary>
    /// The first package is decided also on fitness
    /// </summary>
    public void Main_OffUR2()
    {
        List<Package2D> input = Input_packages.ToList();
        List<Package2D> loadorder = new List<Package2D>();
        loadorder.Add(Bin);
        //input = input.OrderByDescending(x => x.Length).ThenBy(x => x.Width).ToList(); //The Prep
        input = input.OrderByDescending(x => x.Volume).ToList(); //The Prep
        loadorder[0].OverwritePosition(0, 0, 1); //set the bin

        verticestoconsider.AddRange(loadorder[0].Vertixes); //add the vertices of the bin   
        ExtremePoint FirstE_point = new ExtremePoint(Chosen_maxdim, 0, 0, 1);
        FirstE_point.Create_Space(verticestoconsider);
        ActiveExtremePoints.Clear(); ActiveExtremePoints.Add(FirstE_point);

        SortedList<double, List<Tuple<Package2D, ExtremePoint>>> FitnessList =
                new SortedList<double, List<Tuple<Package2D, ExtremePoint>>>();
        while (input.ToList().Count != 0)
        {

            //ActiveExtremePoints = ActiveExtremePoints.OrderBy(x => x.Y).ThenBy(x => x.X).ToList();
            foreach (ExtremePoint E in ActiveExtremePoints.ToList())// can add another loop for packs to do best fit
            {
                bool needsspace = false;
                if (Rules.Count() > 0) { needsspace = Simple_Overlapcheck_manual(E, Rules.Last().Value.Rulepoints); }
                if ((needsspace == false) || (E.Space.Count == 0)) { E.Create_Space(Fetchrelevant_vertices(E)); }
                foreach (Package2D p in input.ToList())
                {
                    if ((p.Priority != 100000) || (p.Priority != 1))
                    {
                        if ((p.Width >= Bin.Width) | (p.Length >= Bin.Width)) { p.Priority = 100000; }
                        else { p.Priority = 1; }
                    }

                    Package2D current_pack = p;
                    current_pack.OverwritePosition(E.X, E.Y, 1); //move package to this Extreme Point, index is handle= P1
                    Package2D current_pack_rotated = new Package2D(current_pack.Length, current_pack.Width);
                    current_pack_rotated.Indexes.Add("Instance", current_pack.Indexes["Instance"]); current_pack_rotated.Priority = p.Priority;
                    current_pack_rotated.OverwritePosition(E.X, E.Y, 1);
                    if (current_pack.Indexes["Instance"] == 30)//p. && 
                    {
                        string here = string.Empty;
                    }

                    bool fits1 = E.Fitsinspace2(current_pack.Pointslist);
                    bool fits2 = E.Fitsinspace2(current_pack_rotated.Pointslist);
                    if (!fits1 && !fits2) { continue; }
                    if (fits1 && fits2) //if it fits within the boundaries
                    {
                        double calculated_fitness = Fitness(current_pack, E);
                        double calculated_fitness_r = Fitness(current_pack_rotated, E);
                        if (calculated_fitness > calculated_fitness_r)
                        {
                            if (FitnessList.Keys.Contains(calculated_fitness))
                            {
                                FitnessList[calculated_fitness].Add(new Tuple<Package2D, ExtremePoint>(current_pack, E));

                            }
                            else
                            {
                                FitnessList.Add(calculated_fitness, new List<Tuple<Package2D, ExtremePoint>>() { new Tuple<Package2D, ExtremePoint>(current_pack, E) });
                            }

                        }
                        else
                        {
                            if (FitnessList.Keys.Contains(calculated_fitness_r))
                            {
                                FitnessList[calculated_fitness_r].Add(new Tuple<Package2D, ExtremePoint>(current_pack_rotated, E));

                            }
                            else
                            {
                                FitnessList.Add(calculated_fitness_r, new List<Tuple<Package2D, ExtremePoint>>() { new Tuple<Package2D, ExtremePoint>(current_pack_rotated, E) });
                            }

                        }

                    }
                    else if (fits1 && !fits2)
                    {
                        double calculated_fitness = Fitness(current_pack, E);
                        if (FitnessList.Keys.Contains(calculated_fitness))
                        {
                            FitnessList[calculated_fitness].Add(new Tuple<Package2D, ExtremePoint>(current_pack, E));

                        }
                        else
                        {
                            FitnessList.Add(calculated_fitness, new List<Tuple<Package2D, ExtremePoint>>() { new Tuple<Package2D, ExtremePoint>(current_pack, E) });
                        }

                    }
                    else if (!fits1 && fits2)
                    {
                        double calculated_fitness_r = Fitness(current_pack_rotated, E);
                        if (FitnessList.Keys.Contains(calculated_fitness_r))
                        {
                            FitnessList[calculated_fitness_r].Add(new Tuple<Package2D, ExtremePoint>(current_pack_rotated, E));

                        }
                        else
                        {
                            FitnessList.Add(calculated_fitness_r, new List<Tuple<Package2D, ExtremePoint>>() { new Tuple<Package2D, ExtremePoint>(current_pack_rotated, E) });
                        }

                    }


                }


            }
            Package2D chosenpack = FitnessList.Last().Value[0].Item1;
            ExtremePoint chosenE = FitnessList.Last().Value[0].Item2;
            chosenpack.OverwritePosition(chosenE.X, chosenE.Y, 1);
            loadorder.Add(chosenpack);
            MasterRule r2 = new MasterRule(chosenpack);
            Rules.Add(r2.Center, r2);
            //if (chosenpack.Indexes["Instance"] == 8)//p. && 
            //{
            //    string here = string.Empty;
            //}
            Refresh_ExtremePoints(chosenpack, chosenE);
            Refresh_Vertices(chosenpack, chosenE);
            FitnessList.Clear();
            input.Remove(input.Where(x => x.Indexes["Instance"] == chosenpack.Indexes["Instance"]).ToList()[0]);

        }
        Load_order.Clear();
        Load_order.AddRange(loadorder);


        return;
    }
    /// <summary>
    ///  offline input, placement is arbitrary, with pre rotation
    /// </summary>
    public void Main_OffURPreR()
    {
        List<Package2D> input = Input_packages.ToList();
        List<Package2D> loadorder = new List<Package2D>();
        loadorder.Add(Bin);
        input = input.OrderByDescending(x => x.Volume).ToList(); //The Prep
        loadorder.Add(input[0]);
        loadorder[0].OverwritePosition(0, 0, 1); //set the bin

        verticestoconsider.AddRange(loadorder[0].Vertixes); //add the vertices of the bin
        loadorder[1].OverwritePosition(0, 0, 1); //use first point as handle and place bottom left
        if (loadorder[1].Width < loadorder[1].Length) { loadorder[1].Rotate(); }
        MasterRule r = new MasterRule(loadorder[1]);
        Rules.Add(r.Center, r);
        ExtremePoint FirstE_point = new ExtremePoint(Chosen_maxdim, 0, 0, 1);
        FirstE_point.Create_Space(verticestoconsider);
        ActiveExtremePoints.Clear(); ActiveExtremePoints.Add(FirstE_point);
        Refresh_ExtremePoints(loadorder[1], FirstE_point);
        Refresh_Vertices(loadorder[1], FirstE_point);
        input.Remove(input[0]);

        SortedList<double, List<Tuple<Package2D, ExtremePoint>>> FitnessList =
                new SortedList<double, List<Tuple<Package2D, ExtremePoint>>>();
        while (input.ToList().Count != 0)
        {


            foreach (ExtremePoint E in ActiveExtremePoints.ToList())// can add another loop for packs to do best fit
            {
                bool needsspace = Simple_Overlapcheck_manual(E, Rules.Last().Value.Rulepoints);
                if ((needsspace == false) || (E.Space.Count == 0)) { E.Create_Space(Fetchrelevant_vertices(E)); }
                foreach (Package2D p in input.ToList())
                {
                    Package2D current_pack = p;
                    current_pack.OverwritePosition(E.X, E.Y, 1); //move package to this Extreme Point, index is handle= P1
                    if (current_pack.Width < current_pack.Length) { current_pack.Rotate(); }


                    bool fits1 = E.Fitsinspace2(current_pack.Pointslist);

                    if (!fits1) { continue; }

                    else if (fits1)
                    {
                        double calculated_fitness = Fitness(current_pack, E);
                        if (FitnessList.Keys.Contains(calculated_fitness))
                        {
                            FitnessList[calculated_fitness].Add(new Tuple<Package2D, ExtremePoint>(current_pack, E));

                        }
                        else
                        {
                            FitnessList.Add(calculated_fitness, new List<Tuple<Package2D, ExtremePoint>>() { new Tuple<Package2D, ExtremePoint>(current_pack, E) });
                        }

                    }


                }


            }
            Package2D chosenpack = FitnessList.Last().Value[0].Item1;
            ExtremePoint chosenE = FitnessList.Last().Value[0].Item2;
            chosenpack.OverwritePosition(chosenE.X, chosenE.Y, 1);
            loadorder.Add(chosenpack);
            MasterRule r2 = new MasterRule(chosenpack);
            Rules.Add(r2.Center, r2);
            Refresh_ExtremePoints(chosenpack, chosenE);
            Refresh_Vertices(chosenpack, chosenE);
            FitnessList.Clear();
            input.Remove(input.Where(x => x.Indexes["Instance"] == chosenpack.Indexes["Instance"]).ToList()[0]);

        }
        Load_order.Clear();
        Load_order.AddRange(loadorder);


        return;
    }
    /// <summary>
    /// offline input, placement is arbitrary, no rotation, pre ordering
    /// </summary>
    public void Main_OffUOPrep()
    {
        List<Package2D> input = Input_packages.ToList();
        List<Package2D> loadorder = new List<Package2D>();
        loadorder.Add(Bin);
        input = input.OrderByDescending(x => x.Volume).ToList(); //The Prep thats binding
        loadorder.Add(input[0]);
        loadorder[0].OverwritePosition(0, 0, 1); //set the bin

        verticestoconsider.AddRange(loadorder[0].Vertixes); //add the vertices of the bin
        loadorder[1].OverwritePosition(0, 0, 1); //use first point as handle and place bottom left
        if (loadorder[1].Width < loadorder[1].Length) { loadorder[1].Rotate(); }
        MasterRule r = new MasterRule(loadorder[1]);
        Rules.Add(r.Center, r);
        ExtremePoint FirstE_point = new ExtremePoint(Chosen_maxdim, 0, 0, 1);
        FirstE_point.Create_Space(verticestoconsider);
        ActiveExtremePoints.Clear(); ActiveExtremePoints.Add(FirstE_point);
        Refresh_ExtremePoints(loadorder[1], FirstE_point);
        Refresh_Vertices(loadorder[1], FirstE_point);
        input.Remove(input[0]);


        SortedList<double, List<Tuple<Package2D, ExtremePoint>>> FitnessList =
                new SortedList<double, List<Tuple<Package2D, ExtremePoint>>>();
        while (input.ToList().Count != 0)
        {


            foreach (ExtremePoint E in ActiveExtremePoints.ToList())// can add another loop for packs to do best fit
            {
                bool needsspace = Simple_Overlapcheck_manual(E, Rules.Last().Value.Rulepoints);
                if ((needsspace == false) || (E.Space.Count == 0)) { E.Create_Space(Fetchrelevant_vertices(E)); }

                Package2D current_pack = input.First();
                current_pack.OverwritePosition(E.X, E.Y, 1); //move package to this Extreme Point, index is handle= P1
                Package2D current_pack_rotated = new Package2D(current_pack.Length, current_pack.Width);
                current_pack_rotated.Indexes.Add("Instance", current_pack.Indexes["Instance"]);
                current_pack_rotated.OverwritePosition(E.X, E.Y, 1);

                //bool fitsmaster = true;

                //foreach (MasterRule rule in Rules.Values)
                //{
                //    fitsmaster = rule.TestPoints(current_pack.Pointslist.ToList());
                //    if (fitsmaster == false) { fitsmaster = false; break; }
                //}
                //if (fitsmaster == false) { continue; }


                bool fits1 = E.Fitsinspace2(current_pack.Pointslist);

                if (!fits1) { continue; }

                else if (fits1)
                {
                    double calculated_fitness = Fitness(current_pack, E);
                    if (FitnessList.Keys.Contains(calculated_fitness))
                    {
                        FitnessList[calculated_fitness].Add(new Tuple<Package2D, ExtremePoint>(current_pack, E));

                    }
                    else
                    {
                        FitnessList.Add(calculated_fitness, new List<Tuple<Package2D, ExtremePoint>>() { new Tuple<Package2D, ExtremePoint>(current_pack, E) });
                    }

                }




            }
            Package2D chosenpack = FitnessList.Last().Value[0].Item1;
            ExtremePoint chosenE = FitnessList.Last().Value[0].Item2;
            chosenpack.OverwritePosition(chosenE.X, chosenE.Y, 1);
            loadorder.Add(chosenpack);
            MasterRule r2 = new MasterRule(chosenpack);
            Rules.Add(r2.Center, r2);
            if (chosenpack.Indexes["Instance"] == 261)//p. && 
            {
                string here = string.Empty;
            }
            Refresh_ExtremePoints(chosenpack, chosenE);
            Refresh_Vertices(chosenpack, chosenE);
            FitnessList.Clear();
            input.Remove(input.First());

        }
        Load_order.Clear();
        Load_order.AddRange(loadorder);


        return;
    }
    /// <summary>
    /// made for n13 burke et al. 2004
    /// </summary>
    public void Large_OffUOPrep()
    {
        List<Package2D> input = Input_packages.ToList();
        List<Package2D> loadorder = new List<Package2D>();
        loadorder.Add(Bin);
        foreach (Package2D p in input)
        {
            if (p.Width < p.Length) { p.Rotate(); }
            if (p.Width > RatioBan * p.Length) { p.Rotationallowance["XY"] = false; }
        }
        input = input.OrderByDescending(x => x.Width).ThenBy(x => x.Length).ToList(); //The Prep
        loadorder.Add(input[0]);
        loadorder[0].OverwritePosition(0, 0, 1); //set the bin

        verticestoconsider.AddRange(loadorder[0].Vertixes); //add the vertices of the bin
        loadorder[1].OverwritePosition(0, 0, 1); //use first point as handle and place bottom left
        if (loadorder[1].Width < loadorder[1].Length) { loadorder[1].Rotate(); }
        MasterRule r = new MasterRule(loadorder[1]);
        Rules.Add(r.Center, r);
        ExtremePoint FirstE_point = new ExtremePoint(Chosen_maxdim, 0, 0, 1);
        FirstE_point.Create_Space(verticestoconsider);
        ActiveExtremePoints.Clear(); ActiveExtremePoints.Add(FirstE_point);
        Refresh_ExtremePoints(loadorder[1], FirstE_point);
        Refresh_Vertices(loadorder[1], FirstE_point);
        input.Remove(input[0]);


        SortedList<double, List<Tuple<Package2D, ExtremePoint>>> FitnessList =
                new SortedList<double, List<Tuple<Package2D, ExtremePoint>>>();
        while (input.ToList().Count != 0)
        {
            List<ExtremePoint> consideredE = new List<ExtremePoint>();
            if (ActiveExtremePoints.Count > 500) { consideredE = ActiveExtremePoints.OrderBy(x => rnd.Next()).ToList().GetRange(0, (int)(ActiveExtremePoints.Count * 0.6)); }
            else { consideredE = ActiveExtremePoints.ToList(); }
            foreach (ExtremePoint E in ActiveExtremePoints)
            {
                bool needsspace = Simple_Overlapcheck_manual(E, Rules.Last().Value.Rulepoints);
                if ((needsspace == false) || (E.Space.Count == 0)) { E.Create_Space(Fetchrelevant_vertices(E)); }
            }
            foreach (ExtremePoint E in consideredE)// can add another loop for packs to do best fit
            {
                Package2D current_pack = input.First();
                current_pack.OverwritePosition(E.X, E.Y, 1); //move package to this Extreme Point, index is handle= P1
                Package2D current_pack_rotated = new Package2D(current_pack.Length, current_pack.Width);
                current_pack_rotated.Indexes.Add("Instance", current_pack.Indexes["Instance"]);
                current_pack_rotated.OverwritePosition(E.X, E.Y, 1);

                //bool fitsmaster = true;

                //foreach (MasterRule rule in Rules.Values)
                //{
                //    fitsmaster = rule.TestPoints(current_pack.Pointslist.ToList());
                //    if (fitsmaster == false) { fitsmaster = false; break; }
                //}
                //if (fitsmaster == false) { continue; }


                bool fits1 = E.Fitsinspace2(current_pack.Pointslist);

                if (!fits1) { continue; }

                else if (fits1)
                {
                    double calculated_fitness = Fitness(current_pack, E);
                    if (FitnessList.Keys.Contains(calculated_fitness))
                    {
                        FitnessList[calculated_fitness].Add(new Tuple<Package2D, ExtremePoint>(current_pack, E));

                    }
                    else
                    {
                        FitnessList.Add(calculated_fitness, new List<Tuple<Package2D, ExtremePoint>>() { new Tuple<Package2D, ExtremePoint>(current_pack, E) });
                    }

                }




            }
            Package2D chosenpack = FitnessList.Last().Value[0].Item1;
            ExtremePoint chosenE = FitnessList.Last().Value[0].Item2;
            chosenpack.OverwritePosition(chosenE.X, chosenE.Y, 1);
            loadorder.Add(chosenpack);
            MasterRule r2 = new MasterRule(chosenpack);
            Rules.Add(r2.Center, r2);
            Refresh_ExtremePoints(chosenpack, chosenE);
            Refresh_Vertices(chosenpack, chosenE);
            FitnessList.Clear();
            input.Remove(input.First());

        }
        Load_order.Clear();
        Load_order.AddRange(loadorder);


        return;
    }
    /// <summary>
    /// large for skyline, no rotation
    /// </summary>
    public void Large_OffURPrepSky()
    {
        List<Package2D> input = Input_packages.ToList();
        List<Package2D> loadorder = new List<Package2D>();
        loadorder.Add(Bin);
        foreach (Package2D p in input)
        {
            if ((p.Width < p.Length) && (p.Length <= Bin.Width)) { p.Rotate(); }
            if ((p.Width > RatioBan * p.Length) && (p.Width <= Bin.Width)) { p.Rotationallowance["XY"] = false; }
        }
        input = input.OrderByDescending(x => x.Volume).ToList(); //The Prep
        loadorder.Add(input[0]);
        loadorder[0].OverwritePosition(0, 0, 1); //set the bin

        verticestoconsider.AddRange(loadorder[0].Vertixes); //add the vertices of the bin
        loadorder[1].OverwritePosition(0, 0, 1); //use first point as handle and place bottom left
        if (loadorder[1].Width < loadorder[1].Length) { loadorder[1].Rotate(); }
        MasterRule r = new MasterRule(loadorder[1]);
        Rules.Add(r.Center, r);
        ExtremePoint FirstE_point = new ExtremePoint(Chosen_maxdim, 0, 0, 1);
        FirstE_point.Create_Space(verticestoconsider);
        ActiveExtremePoints.Clear(); ActiveExtremePoints.Add(FirstE_point);
        Refresh_ExtremePoints(loadorder[1], FirstE_point);
        Refresh_Vertices(loadorder[1], FirstE_point);
        input.Remove(input[0]);


        SortedList<double, List<Tuple<Package2D, ExtremePoint>>> FitnessList =
                new SortedList<double, List<Tuple<Package2D, ExtremePoint>>>();
        while (input.ToList().Count != 0)
        {
            List<ExtremePoint> consideredE = new List<ExtremePoint>();
            if (ActiveExtremePoints.Count > 500) { consideredE = ActiveExtremePoints.OrderBy(x => rnd.Next()).ToList().GetRange(0, (int)(ActiveExtremePoints.Count * 0.6)); }
            else { consideredE = ActiveExtremePoints.ToList(); }
            foreach (ExtremePoint E in ActiveExtremePoints)
            {
                bool needsspace = Simple_Overlapcheck_manual(E, Rules.Last().Value.Rulepoints);
                if ((needsspace == false) || (E.Space.Count == 0)) { E.Create_Space(Fetchrelevant_vertices(E)); }
            }
            foreach (ExtremePoint E in consideredE)// can add another loop for packs to do best fit
            {

                Package2D current_pack = input.First();
                current_pack.OverwritePosition(E.X, E.Y, 1); //move package to this Extreme Point, index is handle= P1
                Package2D current_pack_rotated = new Package2D(current_pack.Length, current_pack.Width);
                current_pack_rotated.Indexes.Add("Instance", current_pack.Indexes["Instance"]);
                current_pack_rotated.OverwritePosition(E.X, E.Y, 1);

                //bool fitsmaster = true;

                //foreach (MasterRule rule in Rules.Values)
                //{
                //    fitsmaster = rule.TestPoints(current_pack.Pointslist.ToList());
                //    if (fitsmaster == false) { fitsmaster = false; break; }
                //}
                //if (fitsmaster == false) { continue; }
                bool fits1 = true;
                bool fits2 = false;
                bool fitssequential2 = false;
                bool fitssequential1 = Sequential_Overlapcheck(E, current_pack);
                if (!fitssequential1) { fits1 = false; }
                else { fits1 = E.Fitsinspace2(current_pack.Pointslist); }



                if (current_pack.Rotationallowance["XY"])
                {
                    fitssequential2 = Sequential_Overlapcheck(E, current_pack_rotated);
                    if (!fitssequential2) { fits2 = false; }
                    else { fits2 = E.Fitsinspace2(current_pack_rotated.Pointslist); }

                }
                if (!fits1 && !fits2) { continue; }
                if (fits1 && fits2) //if it fits within the boundaries
                {
                    double calculated_fitness = Fitness(current_pack, E);
                    double calculated_fitness_r = Fitness(current_pack_rotated, E);
                    if (calculated_fitness > calculated_fitness_r)
                    {
                        if (FitnessList.Keys.Contains(calculated_fitness))
                        {
                            FitnessList[calculated_fitness].Add(new Tuple<Package2D, ExtremePoint>(current_pack, E));

                        }
                        else
                        {
                            FitnessList.Add(calculated_fitness, new List<Tuple<Package2D, ExtremePoint>>() { new Tuple<Package2D, ExtremePoint>(current_pack, E) });
                        }

                    }
                    else
                    {
                        if (FitnessList.Keys.Contains(calculated_fitness_r))
                        {
                            FitnessList[calculated_fitness_r].Add(new Tuple<Package2D, ExtremePoint>(current_pack_rotated, E));

                        }
                        else
                        {
                            FitnessList.Add(calculated_fitness_r, new List<Tuple<Package2D, ExtremePoint>>() { new Tuple<Package2D, ExtremePoint>(current_pack_rotated, E) });
                        }

                    }

                }
                else if (fits1 && !fits2)
                {
                    double calculated_fitness = Fitness(current_pack, E);
                    if (FitnessList.Keys.Contains(calculated_fitness))
                    {
                        FitnessList[calculated_fitness].Add(new Tuple<Package2D, ExtremePoint>(current_pack, E));

                    }
                    else
                    {
                        FitnessList.Add(calculated_fitness, new List<Tuple<Package2D, ExtremePoint>>() { new Tuple<Package2D, ExtremePoint>(current_pack, E) });
                    }

                }
                else if (!fits1 && fits2)
                {
                    double calculated_fitness_r = Fitness(current_pack_rotated, E);
                    if (FitnessList.Keys.Contains(calculated_fitness_r))
                    {
                        FitnessList[calculated_fitness_r].Add(new Tuple<Package2D, ExtremePoint>(current_pack_rotated, E));

                    }
                    else
                    {
                        FitnessList.Add(calculated_fitness_r, new List<Tuple<Package2D, ExtremePoint>>() { new Tuple<Package2D, ExtremePoint>(current_pack_rotated, E) });
                    }

                }

            }





            Package2D chosenpack = FitnessList.Last().Value[0].Item1;
            ExtremePoint chosenE = FitnessList.Last().Value[0].Item2;
            chosenpack.OverwritePosition(chosenE.X, chosenE.Y, 1);
            loadorder.Add(chosenpack);
            MasterRule r2 = new MasterRule(chosenpack);
            Rules.Add(r2.Center, r2);
            Refresh_ExtremePoints(chosenpack, chosenE);
            Refresh_Vertices(chosenpack, chosenE);
            FitnessList.Clear();
            input.Remove(input.First());

        }
        Load_order.Clear();
        Load_order.AddRange(loadorder);


        return;
    }
    /// <summary>
    /// skyline version for large sets
    /// </summary>
    public void Large_OffUOPrepSky()
    {
        List<Package2D> input = Input_packages.ToList();
        List<Package2D> loadorder = new List<Package2D>();
        loadorder.Add(Bin);
        foreach (Package2D p in input)
        {
            if ((p.Width < p.Length) && (p.Length <= Bin.Width)) { p.Rotate(); }
            if ((p.Width > RatioBan * p.Length) && (p.Width <= Bin.Width)) { p.Rotationallowance["XY"] = false; }
        }
        input = input.OrderByDescending(x => x.Width).ThenBy(x => x.Length).ToList(); //The Prep
        loadorder.Add(input[0]);
        loadorder[0].OverwritePosition(0, 0, 1); //set the bin

        verticestoconsider.AddRange(loadorder[0].Vertixes); //add the vertices of the bin
        loadorder[1].OverwritePosition(0, 0, 1); //use first point as handle and place bottom left
        if (loadorder[1].Width < loadorder[1].Length) { loadorder[1].Rotate(); }
        MasterRule r = new MasterRule(loadorder[1]);
        Rules.Add(r.Center, r);
        ExtremePoint FirstE_point = new ExtremePoint(Chosen_maxdim, 0, 0, 1);
        FirstE_point.Create_Space(verticestoconsider);
        ActiveExtremePoints.Clear(); ActiveExtremePoints.Add(FirstE_point);
        Refresh_ExtremePoints(loadorder[1], FirstE_point);
        Refresh_Vertices(loadorder[1], FirstE_point);
        input.Remove(input[0]);


        SortedList<double, List<Tuple<Package2D, ExtremePoint>>> FitnessList =
                new SortedList<double, List<Tuple<Package2D, ExtremePoint>>>();
        while (input.ToList().Count != 0)
        {
            List<ExtremePoint> consideredE = new List<ExtremePoint>();
            if (ActiveExtremePoints.Count > 500) { consideredE = ActiveExtremePoints.OrderBy(x => rnd.Next()).ToList().GetRange(0, (int)(ActiveExtremePoints.Count * 1)); }
            else { consideredE = ActiveExtremePoints.ToList(); }
            foreach (ExtremePoint E in ActiveExtremePoints)
            {
                bool needsspace = Simple_Overlapcheck_manual(E, Rules.Last().Value.Rulepoints);
                if ((needsspace == false) || (E.Space.Count == 0)) { E.Create_Space(Fetchrelevant_vertices(E)); }
            }
            foreach (ExtremePoint E in consideredE)// can add another loop for packs to do best fit
            {

                Package2D current_pack = input.First();
                current_pack.OverwritePosition(E.X, E.Y, 1); //move package to this Extreme Point, index is handle= P1
                Package2D current_pack_rotated = new Package2D(current_pack.Length, current_pack.Width);
                current_pack_rotated.Indexes.Add("Instance", current_pack.Indexes["Instance"]);
                current_pack_rotated.OverwritePosition(E.X, E.Y, 1);

                //bool fitsmaster = true;

                //foreach (MasterRule rule in Rules.Values)
                //{
                //    fitsmaster = rule.TestPoints(current_pack.Pointslist.ToList());
                //    if (fitsmaster == false) { fitsmaster = false; break; }
                //}
                //if (fitsmaster == false) { continue; }
                bool fits1 = true;
                bool fitssequential = Sequential_Overlapcheck(E, current_pack);
                if (!fitssequential) { fits1 = false; }
                else { fits1 = E.Fitsinspace2(current_pack.Pointslist); }



                if (!fits1) { continue; }

                else if (fits1)
                {
                    double calculated_fitness = Fitness(current_pack, E);
                    if (FitnessList.Keys.Contains(calculated_fitness))
                    {
                        FitnessList[calculated_fitness].Add(new Tuple<Package2D, ExtremePoint>(current_pack, E));

                    }
                    else
                    {
                        FitnessList.Add(calculated_fitness, new List<Tuple<Package2D, ExtremePoint>>() { new Tuple<Package2D, ExtremePoint>(current_pack, E) });
                    }

                }




            }
            Package2D chosenpack = FitnessList.Last().Value[0].Item1;
            ExtremePoint chosenE = FitnessList.Last().Value[0].Item2;
            chosenpack.OverwritePosition(chosenE.X, chosenE.Y, 1);
            loadorder.Add(chosenpack);
            MasterRule r2 = new MasterRule(chosenpack);
            Rules.Add(r2.Center, r2);
            Refresh_ExtremePoints(chosenpack, chosenE);
            Refresh_Vertices(chosenpack, chosenE);
            FitnessList.Clear();
            input.Remove(input.First());

        }
        Load_order.Clear();
        Load_order.AddRange(loadorder);


        return;
    }
    /// <summary>
    /// offline input, placement is arbitrary, pre ordering, pre rotation
    /// </summary>
    public void Main_OffURPrepPreR()
    {
        List<Package2D> input = Input_packages.ToList();
        List<Package2D> loadorder = new List<Package2D>();
        loadorder.Add(Bin);
        foreach (Package2D p in input)
        {
            if (p.Width < p.Length) { p.Rotate(); }
        }
        input = input.OrderByDescending(x => x.Width).ThenBy(x => x.Length).ToList(); //The Prep
        loadorder.Add(input[0]);
        loadorder[0].OverwritePosition(0, 0, 1); //set the bin

        verticestoconsider.AddRange(loadorder[0].Vertixes); //add the vertices of the bin
        loadorder[1].OverwritePosition(0, 0, 1); //use first point as handle and place bottom left
        if (loadorder[1].Width < loadorder[1].Length) { loadorder[1].Rotate(); }
        MasterRule r = new MasterRule(loadorder[1]);
        Rules.Add(r.Center, r);
        ExtremePoint FirstE_point = new ExtremePoint(Chosen_maxdim, 0, 0, 1);
        FirstE_point.Create_Space(verticestoconsider);
        ActiveExtremePoints.Clear(); ActiveExtremePoints.Add(FirstE_point);
        Refresh_ExtremePoints(loadorder[1], FirstE_point);
        Refresh_Vertices(loadorder[1], FirstE_point);
        input.Remove(input[0]);

        SortedList<double, List<Tuple<Package2D, ExtremePoint>>> FitnessList =
                new SortedList<double, List<Tuple<Package2D, ExtremePoint>>>();
        while (input.ToList().Count != 0)
        {


            foreach (ExtremePoint E in ActiveExtremePoints.ToList())// can add another loop for packs to do best fit
            {
                bool needsspace = Simple_Overlapcheck_manual(E, Rules.Last().Value.Rulepoints);
                if ((needsspace == false) || (E.Space.Count == 0)) { E.Create_Space(Fetchrelevant_vertices(E)); }



                Package2D current_pack = input.First();

                current_pack.OverwritePosition(E.X, E.Y, 1); //move package to this Extreme Point, index is handle= P1


                bool fits1 = E.Fitsinspace2(current_pack.Pointslist);

                if (!fits1) { continue; }

                else if (fits1)
                {
                    double calculated_fitness = Fitness(current_pack, E);
                    if (FitnessList.Keys.Contains(calculated_fitness))
                    {
                        FitnessList[calculated_fitness].Add(new Tuple<Package2D, ExtremePoint>(current_pack, E));

                    }
                    else
                    {
                        FitnessList.Add(calculated_fitness, new List<Tuple<Package2D, ExtremePoint>>() { new Tuple<Package2D, ExtremePoint>(current_pack, E) });
                    }

                }


            }
            Package2D chosenpack = FitnessList.Last().Value[0].Item1;
            ExtremePoint chosenE = FitnessList.Last().Value[0].Item2;
            chosenpack.OverwritePosition(chosenE.X, chosenE.Y, 1);
            loadorder.Add(chosenpack);

            if (chosenpack.Indexes["Instance"] == 99)
            {
                string here = string.Empty;
            }
            MasterRule r2 = new MasterRule(chosenpack);
            Rules.Add(r2.Center, r2);
            Refresh_ExtremePoints(chosenpack, chosenE);
            Refresh_Vertices(chosenpack, chosenE);
            FitnessList.Clear();
            input.Remove(input.First());

        }
        Load_order.Clear();
        Load_order.AddRange(loadorder);


        return;
    }
    /// <summary>
    ///  offline input, placement is arbitrary, dynamic rotation, pre ordering
    /// </summary>
    public void Main_OffURPrep()
    {
        List<Package2D> input = Input_packages.ToList();
        List<Package2D> loadorder = new List<Package2D>();
        loadorder.Add(Bin);

        switch (RatioBanOrientation)
        {
            case "Vertical":
                {
                    foreach (Package2D p in input)
                    {
                        if ((p.Width > p.Length) && (p.Length <= Bin.Width)) { p.Rotate(); }
                        if ((p.Length > RatioBan * p.Width) && (p.Length <= Bin.Width)) { p.Rotationallowance["XY"] = false; }
                    }
                    break;
                }
            case "Horizontal":
                {
                    foreach (Package2D p in input)
                    {
                        if ((p.Width < p.Length) && (p.Length <= Bin.Width)) { p.Rotate(); }
                        if ((p.Width > RatioBan * p.Length) && (p.Width <= Bin.Width)) { p.Rotationallowance["XY"] = false; }
                    }
                    break;
                }

        }


        //input = input.OrderByDescending(x => x.Width).ThenBy(x => x.Length).ToList(); //The Prep
        input = input.OrderByDescending(x => x.Perimeter).ToList();
        loadorder.Add(input[0]);
        loadorder[0].OverwritePosition(0, 0, 1); //set the bin

        verticestoconsider.AddRange(loadorder[0].Vertixes); //add the vertices of the bin
        loadorder[1].OverwritePosition(0, 0, 1); //use first point as handle and place bottom left
        if ((loadorder[1].Width < loadorder[1].Length) && (loadorder[1].Length <= Bin.Width)) { loadorder[1].Rotate(); }
        MasterRule r = new MasterRule(loadorder[1]);
        Rules.Add(r.Center, r);
        ExtremePoint FirstE_point = new ExtremePoint(Chosen_maxdim, 0, 0, 1);
        FirstE_point.Create_Space(verticestoconsider);
        ActiveExtremePoints.Clear(); ActiveExtremePoints.Add(FirstE_point);
        Refresh_ExtremePoints(loadorder[1], FirstE_point);
        Refresh_Vertices(loadorder[1], FirstE_point);
        input.Remove(input[0]);

        SortedList<double, List<Tuple<Package2D, ExtremePoint>>> FitnessList =
                new SortedList<double, List<Tuple<Package2D, ExtremePoint>>>();
        while (input.ToList().Count != 0)
        {

            //ActiveExtremePoints = ActiveExtremePoints.OrderBy(x => x.Y).ThenBy(x => x.X).ToList();
            foreach (ExtremePoint E in ActiveExtremePoints.ToList())// can add another loop for packs to do best fit
            {
                bool needsspace = Simple_Overlapcheck_manual(E, Rules.Last().Value.Rulepoints);
                if ((needsspace == false) || (E.Space.Count == 0)) { E.Create_Space(Fetchrelevant_vertices(E)); }

                Package2D current_pack = input.First();
                current_pack.OverwritePosition(E.X, E.Y, 1); //move package to this Extreme Point, index is handle= P1
                Package2D current_pack_rotated = new Package2D(current_pack.Length, current_pack.Width);
                current_pack_rotated.Indexes.Add("Instance", current_pack.Indexes["Instance"]);
                current_pack_rotated.OverwritePosition(E.X, E.Y, 1);
                //if (current_pack.Indexes["Instance"] == 14 && E.X == 16 && E.Y == 13)
                //{
                //    string here = string.Empty;
                //}

                bool fits1 = E.Fitsinspace2(current_pack.Pointslist);
                bool fits2 = false;
                if (current_pack.Rotationallowance["XY"])
                {
                    fits2 = E.Fitsinspace2(current_pack_rotated.Pointslist);
                }
                if (!fits1 && !fits2) { continue; }
                if (fits1 && fits2) //if it fits within the boundaries
                {
                    double calculated_fitness = Fitness(current_pack, E);
                    double calculated_fitness_r = Fitness(current_pack_rotated, E);
                    if (calculated_fitness > calculated_fitness_r)
                    {
                        if (FitnessList.Keys.Contains(calculated_fitness))
                        {
                            FitnessList[calculated_fitness].Add(new Tuple<Package2D, ExtremePoint>(current_pack, E));

                        }
                        else
                        {
                            FitnessList.Add(calculated_fitness, new List<Tuple<Package2D, ExtremePoint>>() { new Tuple<Package2D, ExtremePoint>(current_pack, E) });
                        }

                    }
                    else
                    {
                        if (FitnessList.Keys.Contains(calculated_fitness_r))
                        {
                            FitnessList[calculated_fitness_r].Add(new Tuple<Package2D, ExtremePoint>(current_pack_rotated, E));

                        }
                        else
                        {
                            FitnessList.Add(calculated_fitness_r, new List<Tuple<Package2D, ExtremePoint>>() { new Tuple<Package2D, ExtremePoint>(current_pack_rotated, E) });
                        }

                    }

                }
                else if (fits1 && !fits2)
                {
                    double calculated_fitness = Fitness(current_pack, E);
                    if (FitnessList.Keys.Contains(calculated_fitness))
                    {
                        FitnessList[calculated_fitness].Add(new Tuple<Package2D, ExtremePoint>(current_pack, E));

                    }
                    else
                    {
                        FitnessList.Add(calculated_fitness, new List<Tuple<Package2D, ExtremePoint>>() { new Tuple<Package2D, ExtremePoint>(current_pack, E) });
                    }

                }
                else if (!fits1 && fits2)
                {
                    double calculated_fitness_r = Fitness(current_pack_rotated, E);
                    if (FitnessList.Keys.Contains(calculated_fitness_r))
                    {
                        FitnessList[calculated_fitness_r].Add(new Tuple<Package2D, ExtremePoint>(current_pack_rotated, E));

                    }
                    else
                    {
                        FitnessList.Add(calculated_fitness_r, new List<Tuple<Package2D, ExtremePoint>>() { new Tuple<Package2D, ExtremePoint>(current_pack_rotated, E) });
                    }

                }

            }
            //if(FitnessList.Count == 0)
            //{
            //    Load_order.Clear();
            //    Load_order.AddRange(loadorder);
            //    return;
            //}
            Package2D chosenpack = FitnessList.Last().Value[0].Item1;
            ExtremePoint chosenE = FitnessList.Last().Value[0].Item2;
            chosenpack.OverwritePosition(chosenE.X, chosenE.Y, 1);
            loadorder.Add(chosenpack);
            Inbetween_loadorder = loadorder.ToList();
            MasterRule r2 = new MasterRule(chosenpack);
            Rules.Add(r2.Center, r2);

            Refresh_ExtremePoints(chosenpack, chosenE);
            Refresh_Vertices(chosenpack, chosenE);
            FitnessList.Clear();
            input.Remove(input.First());
            UpdateDims(input);

        }
        Load_order.Clear();
        Load_order.AddRange(loadorder);


        return;
    }
    public void Main_OffURPrep2()
    {
        List<Package2D> input = Input_packages.ToList();
        List<Package2D> loadorder = new List<Package2D>();
        loadorder.Add(Bin);

        switch (RatioBanOrientation)
        {
            case "Vertical":
                {
                    foreach (Package2D p in input)
                    {
                        if ((p.Width > p.Length) && (p.Length <= Bin.Width)) { p.Rotate(); }
                        if ((p.Length > RatioBan * p.Width) && (p.Length <= Bin.Width)) { p.Rotationallowance["XY"] = false; }
                    }
                    break;
                }
            case "Horizontal":
                {
                    foreach (Package2D p in input)
                    {
                        if ((p.Width < p.Length) && (p.Length <= Bin.Width)) { p.Rotate(); }
                        if ((p.Width > RatioBan * p.Length) && (p.Width <= Bin.Width)) { p.Rotationallowance["XY"] = false; }
                    }
                    break;
                }

        }


        //input = input.OrderByDescending(x => x.Width).ThenBy(x => x.Length).ToList(); //The Prep
        input = input.OrderByDescending(x => x.Volume).ToList();
        //loadorder.Add(input[0]);
        loadorder[0].OverwritePosition(0, 0, 1); //set the bin

        verticestoconsider.AddRange(loadorder[0].Vertixes); //add the vertices of the bin
        //loadorder[1].OverwritePosition(0, 0, 1); //use first point as handle and place bottom left
        //if ((loadorder[1].Width < loadorder[1].Length) && (loadorder[1].Length <= Bin.Width)) { loadorder[1].Rotate(); }
        //MasterRule r = new MasterRule(loadorder[1]);
        //Rules.Add(r.Center, r);
        ExtremePoint FirstE_point = new ExtremePoint(Chosen_maxdim, 0, 0, 1);
        FirstE_point.Create_Space(verticestoconsider);
        ActiveExtremePoints.Clear(); ActiveExtremePoints.Add(FirstE_point);
        //Refresh_ExtremePoints(loadorder[1], FirstE_point);
        //Refresh_Vertices(loadorder[1], FirstE_point);
        //input.Remove(input[0]);

        SortedList<double, List<Tuple<Package2D, ExtremePoint>>> FitnessList =
                new SortedList<double, List<Tuple<Package2D, ExtremePoint>>>();
        while (input.ToList().Count != 0)
        {

            //ActiveExtremePoints = ActiveExtremePoints.OrderBy(x => x.Y).ThenBy(x => x.X).ToList();
            foreach (ExtremePoint E in ActiveExtremePoints.ToList())// can add another loop for packs to do best fit
            {
                //if (input.First().Indexes["Instance"] == 11 && E.X == 14 && E.Y == 18)
                //{
                //    string here = string.Empty;
                //}
                bool needsspace = true;
                if (ActiveExtremePoints.ToList().Count > 1) { needsspace = Simple_Overlapcheck_manual(E, Rules.Last().Value.Rulepoints); }
                else { needsspace = false; }
                if ((needsspace == false) || (E.Space.Count == 0)) { E.Create_Space(Fetchrelevant_vertices(E)); }

                Package2D current_pack = input.First();
                current_pack.OverwritePosition(E.X, E.Y, 1); //move package to this Extreme Point, index is handle= P1
                Package2D current_pack_rotated = new Package2D(current_pack.Length, current_pack.Width);
                current_pack_rotated.Indexes.Add("Instance", current_pack.Indexes["Instance"]);
                current_pack_rotated.OverwritePosition(E.X, E.Y, 1);
                //if (current_pack.Indexes["Instance"] == 11 && E.X == 14 && E.Y == 18)
                //{
                //    string here = string.Empty;
                //}

                bool fits1 = E.Fitsinspace2(current_pack.Pointslist);
                bool fits2 = false;
                if (current_pack.Rotationallowance["XY"])
                {
                    fits2 = E.Fitsinspace2(current_pack_rotated.Pointslist);
                }
                if (!fits1 && !fits2) { continue; }
                if (fits1 && fits2) //if it fits within the boundaries
                {
                    double calculated_fitness = Fitness(current_pack, E);
                    double calculated_fitness_r = Fitness(current_pack_rotated, E);
                    if (calculated_fitness > calculated_fitness_r)
                    {
                        if (FitnessList.Keys.Contains(calculated_fitness))
                        {
                            FitnessList[calculated_fitness].Add(new Tuple<Package2D, ExtremePoint>(current_pack, E));

                        }
                        else
                        {
                            FitnessList.Add(calculated_fitness, new List<Tuple<Package2D, ExtremePoint>>() { new Tuple<Package2D, ExtremePoint>(current_pack, E) });
                        }

                    }
                    else
                    {
                        if (FitnessList.Keys.Contains(calculated_fitness_r))
                        {
                            FitnessList[calculated_fitness_r].Add(new Tuple<Package2D, ExtremePoint>(current_pack_rotated, E));

                        }
                        else
                        {
                            FitnessList.Add(calculated_fitness_r, new List<Tuple<Package2D, ExtremePoint>>() { new Tuple<Package2D, ExtremePoint>(current_pack_rotated, E) });
                        }

                    }

                }
                else if (fits1 && !fits2)
                {
                    double calculated_fitness = Fitness(current_pack, E);
                    if (FitnessList.Keys.Contains(calculated_fitness))
                    {
                        FitnessList[calculated_fitness].Add(new Tuple<Package2D, ExtremePoint>(current_pack, E));

                    }
                    else
                    {
                        FitnessList.Add(calculated_fitness, new List<Tuple<Package2D, ExtremePoint>>() { new Tuple<Package2D, ExtremePoint>(current_pack, E) });
                    }

                }
                else if (!fits1 && fits2)
                {
                    double calculated_fitness_r = Fitness(current_pack_rotated, E);
                    if (FitnessList.Keys.Contains(calculated_fitness_r))
                    {
                        FitnessList[calculated_fitness_r].Add(new Tuple<Package2D, ExtremePoint>(current_pack_rotated, E));

                    }
                    else
                    {
                        FitnessList.Add(calculated_fitness_r, new List<Tuple<Package2D, ExtremePoint>>() { new Tuple<Package2D, ExtremePoint>(current_pack_rotated, E) });
                    }

                }

            }
            if (FitnessList.Count == 0)
            {
                Load_order.Clear();
                Load_order.AddRange(loadorder);
                return;
            }
            Package2D chosenpack = FitnessList.Last().Value[0].Item1;
            ExtremePoint chosenE = FitnessList.Last().Value[0].Item2;
            chosenpack.OverwritePosition(chosenE.X, chosenE.Y, 1);
            loadorder.Add(chosenpack);
            Inbetween_loadorder = loadorder.ToList();
            MasterRule r2 = new MasterRule(chosenpack);
            Rules.Add(r2.Center, r2);
            //if (chosenpack.Indexes["Instance"] == 12)//p. && 
            //{
            //    string here = string.Empty;
            //}
            Refresh_ExtremePoints(chosenpack, chosenE);
            Refresh_Vertices(chosenpack, chosenE);
            FitnessList.Clear();
            input.Remove(input.First());
            UpdateDims(input);

        }
        Load_order.Clear();
        Load_order.AddRange(loadorder);


        return;
    }
    /// <summary>
    /// same as main but if the overlap is perfect the extreme points are not added
    /// </summary>
    /// 
    public void Large_OffURPrep()
    {
        List<Package2D> input = Input_packages.ToList();
        List<Package2D> loadorder = new List<Package2D>();
        loadorder.Add(Bin);
        switch (RatioBanOrientation)
        {
            case "Vertical":
                {
                    foreach (Package2D p in input)
                    {
                        if ((p.Width > p.Length) && (p.Length <= Bin.Width)) { p.Rotate(); }
                        if ((p.Length > RatioBan * p.Width) && (p.Length <= Bin.Width)) { p.Rotationallowance["XY"] = false; }
                    }
                    break;
                }
            case "Horizontal":
                {
                    foreach (Package2D p in input)
                    {
                        if ((p.Width < p.Length) && (p.Length <= Bin.Width)) { p.Rotate(); }
                        if ((p.Width > RatioBan * p.Length) && (p.Width <= Bin.Width)) { p.Rotationallowance["XY"] = false; }
                    }
                    break;
                }

        }
        //input = input.OrderByDescending(x => x.Width).ThenBy(x => x.Length).ToList(); //The Prep
        input = input.OrderByDescending(x => x.Volume).ToList();
        loadorder.Add(input[0]);
        loadorder[0].OverwritePosition(0, 0, 1); //set the bin

        verticestoconsider.AddRange(loadorder[0].Vertixes); //add the vertices of the bin
        loadorder[1].OverwritePosition(0, 0, 1); //use first point as handle and place bottom left
        if ((loadorder[1].Width < loadorder[1].Length) && (loadorder[1].Length <= Bin.Width)) { loadorder[1].Rotate(); }
        MasterRule r = new MasterRule(loadorder[1]);
        Rules.Add(r.Center, r);
        ExtremePoint FirstE_point = new ExtremePoint(Chosen_maxdim, 0, 0, 1);
        FirstE_point.Create_Space(verticestoconsider);
        ActiveExtremePoints.Clear(); ActiveExtremePoints.Add(FirstE_point);
        Refresh_ExtremePoints(loadorder[1], FirstE_point);
        Refresh_Vertices(loadorder[1], FirstE_point);
        input.Remove(input[0]);

        SortedList<double, List<Tuple<Package2D, ExtremePoint>>> FitnessList =
                new SortedList<double, List<Tuple<Package2D, ExtremePoint>>>();
        while (input.ToList().Count != 0)
        {

            //ActiveExtremePoints = ActiveExtremePoints.OrderBy(x => x.Y).ThenBy(x => x.X).ToList();
            foreach (ExtremePoint E in ActiveExtremePoints.ToList())// can add another loop for packs to do best fit
            {
                bool needsspace = Simple_Overlapcheck_manual(E, Rules.Last().Value.Rulepoints);
                if ((needsspace == false) || (E.Space.Count == 0)) { E.Create_Space(Fetchrelevant_vertices(E)); }

                Package2D current_pack = input.First();
                current_pack.OverwritePosition(E.X, E.Y, 1); //move package to this Extreme Point, index is handle= P1
                Package2D current_pack_rotated = new Package2D(current_pack.Length, current_pack.Width);
                current_pack_rotated.Indexes.Add("Instance", current_pack.Indexes["Instance"]);
                current_pack_rotated.OverwritePosition(E.X, E.Y, 1);
                //if (current_pack.Indexes["Instance"] == 14 && E.X == 16 && E.Y == 13)
                //{
                //    string here = string.Empty;
                //}

                bool fits1 = E.Fitsinspace2(current_pack.Pointslist);
                bool fits2 = false;
                if (current_pack.Rotationallowance["XY"])
                {
                    fits2 = E.Fitsinspace2(current_pack_rotated.Pointslist);
                }
                if (!fits1 && !fits2) { continue; }
                if (fits1 && fits2) //if it fits within the boundaries
                {
                    double calculated_fitness = Fitness(current_pack, E);
                    double calculated_fitness_r = Fitness(current_pack_rotated, E);
                    if (calculated_fitness > calculated_fitness_r)
                    {
                        if (FitnessList.Keys.Contains(calculated_fitness))
                        {
                            FitnessList[calculated_fitness].Add(new Tuple<Package2D, ExtremePoint>(current_pack, E));

                        }
                        else
                        {
                            FitnessList.Add(calculated_fitness, new List<Tuple<Package2D, ExtremePoint>>() { new Tuple<Package2D, ExtremePoint>(current_pack, E) });
                        }

                    }
                    else
                    {
                        if (FitnessList.Keys.Contains(calculated_fitness_r))
                        {
                            FitnessList[calculated_fitness_r].Add(new Tuple<Package2D, ExtremePoint>(current_pack_rotated, E));

                        }
                        else
                        {
                            FitnessList.Add(calculated_fitness_r, new List<Tuple<Package2D, ExtremePoint>>() { new Tuple<Package2D, ExtremePoint>(current_pack_rotated, E) });
                        }

                    }

                }
                else if (fits1 && !fits2)
                {
                    double calculated_fitness = Fitness(current_pack, E);
                    if (FitnessList.Keys.Contains(calculated_fitness))
                    {
                        FitnessList[calculated_fitness].Add(new Tuple<Package2D, ExtremePoint>(current_pack, E));

                    }
                    else
                    {
                        FitnessList.Add(calculated_fitness, new List<Tuple<Package2D, ExtremePoint>>() { new Tuple<Package2D, ExtremePoint>(current_pack, E) });
                    }

                }
                else if (!fits1 && fits2)
                {
                    double calculated_fitness_r = Fitness(current_pack_rotated, E);
                    if (FitnessList.Keys.Contains(calculated_fitness_r))
                    {
                        FitnessList[calculated_fitness_r].Add(new Tuple<Package2D, ExtremePoint>(current_pack_rotated, E));

                    }
                    else
                    {
                        FitnessList.Add(calculated_fitness_r, new List<Tuple<Package2D, ExtremePoint>>() { new Tuple<Package2D, ExtremePoint>(current_pack_rotated, E) });
                    }

                }

            }
            //if(FitnessList.Count == 0)
            //{
            //    Load_order.Clear();
            //    Load_order.AddRange(loadorder);
            //    return;
            //}
            Package2D chosenpack = FitnessList.Last().Value[0].Item1;
            ExtremePoint chosenE = FitnessList.Last().Value[0].Item2;
            chosenpack.OverwritePosition(chosenE.X, chosenE.Y, 1);
            loadorder.Add(chosenpack);
            Inbetween_loadorder = loadorder.ToList();
            MasterRule r2 = new MasterRule(chosenpack);
            Rules.Add(r2.Center, r2);
            //if (chosenpack.Indexes["Instance"] == 78)//p. && 
            //{
            //    string here = string.Empty;
            //}

            Refresh_ExtremePoints_Large(chosenpack, chosenE);
            Refresh_Vertices(chosenpack, chosenE);
            FitnessList.Clear();
            input.Remove(input.First());
            UpdateDims(input);

        }
        Load_order.Clear();
        Load_order.AddRange(loadorder);


        return;
    }
    public void Main_OffURPrepParallel() //doesnt work
    {
        List<Package2D> input = Input_packages.ToList();
        List<Package2D> loadorder = new List<Package2D>();
        loadorder.Add(Bin);
        foreach (Package2D p in input)
        {
            if ((p.Width < p.Length) && (p.Length <= Bin.Width)) { p.Rotate(); }
            if ((p.Width > RatioBan * p.Length) && (p.Width <= Bin.Width)) { p.Rotationallowance["XY"] = false; }
        }
        //input = input.OrderByDescending(x => x.Width).ThenBy(x => x.Length).ToList(); //The Prep
        input = input.OrderByDescending(x => x.Volume).ToList();
        loadorder.Add(input[0]);
        loadorder[0].OverwritePosition(0, 0, 1); //set the bin

        verticestoconsider.AddRange(loadorder[0].Vertixes); //add the vertices of the bin
        loadorder[1].OverwritePosition(0, 0, 1); //use first point as handle and place bottom left
        if ((loadorder[1].Width < loadorder[1].Length) && (loadorder[1].Length <= Bin.Width)) { loadorder[1].Rotate(); }
        MasterRule r = new MasterRule(loadorder[1]);
        Rules.Add(r.Center, r);
        ExtremePoint FirstE_point = new ExtremePoint(Chosen_maxdim, 0, 0, 1);
        FirstE_point.Create_Space(verticestoconsider);
        ActiveExtremePoints.Clear(); ActiveExtremePoints.Add(FirstE_point);
        Refresh_ExtremePoints(loadorder[1], FirstE_point);
        Refresh_Vertices(loadorder[1], FirstE_point);
        input.Remove(input[0]);

        ConcurrentBag<Tuple<double, Package2D, ExtremePoint>> FitnessList = new ConcurrentBag<Tuple<double, Package2D, ExtremePoint>>();
        //SortedList<double, List<Tuple<Package2D, ExtremePoint>>> FitnessList =
        //        new SortedList<double, List<Tuple<Package2D, ExtremePoint>>>();
        while (input.ToList().Count != 0)
        {


            Parallel.ForEach(ActiveExtremePoints.ToList(), E =>// can add another loop for packs to do best fit
            {
                bool needsspace = Simple_Overlapcheck_manual(E, Rules.Last().Value.Rulepoints);
                if ((needsspace == false) || (E.Space.Count == 0)) { E.Create_Space(Fetchrelevant_vertices(E)); }

                Package2D current_pack = input.First();
                current_pack.OverwritePosition(E.X, E.Y, 1); //move package to this Extreme Point, index is handle= P1
                Package2D current_pack_rotated = new Package2D(current_pack.Length, current_pack.Width);
                current_pack_rotated.Indexes.Add("Instance", current_pack.Indexes["Instance"]);
                current_pack_rotated.OverwritePosition(E.X, E.Y, 1);
                if (current_pack.Indexes["Instance"] == 2)// && E.X == 16 && E.Y == 13)
                {
                    string here = string.Empty;
                }

                bool fits1 = E.Fitsinspace2(current_pack.Pointslist);
                bool fits2 = false;
                if (current_pack.Rotationallowance["XY"])
                {
                    fits2 = E.Fitsinspace2(current_pack_rotated.Pointslist);
                }
                if (!fits1 && !fits2) { }
                else if (fits1 && fits2) //if it fits within the boundaries
                {
                    double calculated_fitness = Fitness(current_pack, E);
                    double calculated_fitness_r = Fitness(current_pack_rotated, E);
                    if (calculated_fitness > calculated_fitness_r)
                    {


                        FitnessList.Add(new Tuple<double, Package2D, ExtremePoint>(calculated_fitness, current_pack, E));
                    }


                    else
                    {
                        FitnessList.Add(new Tuple<double, Package2D, ExtremePoint>(calculated_fitness_r, current_pack, E));
                    }
                }





                else if (fits1 && !fits2)
                {
                    double calculated_fitness = Fitness(current_pack, E);
                    FitnessList.Add(new Tuple<double, Package2D, ExtremePoint>(calculated_fitness, current_pack, E));
                }
                else if (!fits1 && fits2)
                {
                    double calculated_fitness_r = Fitness(current_pack_rotated, E);
                    FitnessList.Add(new Tuple<double, Package2D, ExtremePoint>(calculated_fitness_r, current_pack, E));


                }

            });

            SortedList<double, List<Tuple<Package2D, ExtremePoint>>> FitnessList2 = new SortedList<double, List<Tuple<Package2D, ExtremePoint>>>();
            foreach (Tuple<double, Package2D, ExtremePoint> t in FitnessList)
            {
                if (FitnessList2.Keys.Contains(t.Item1))
                {
                    FitnessList2[t.Item1].Add(new Tuple<Package2D, ExtremePoint>(t.Item2, t.Item3));
                }
                else
                {
                    FitnessList2.Add(t.Item1, new List<Tuple<Package2D, ExtremePoint>> { new Tuple<Package2D, ExtremePoint>(t.Item2, t.Item3) });
                }
            }
            Package2D chosenpack = FitnessList2.Last().Value[0].Item1;
            ExtremePoint chosenE = FitnessList2.Last().Value[0].Item2;
            chosenpack.OverwritePosition(chosenE.X, chosenE.Y, 1);
            loadorder.Add(chosenpack);
            Inbetween_loadorder = loadorder.ToList();
            MasterRule r2 = new MasterRule(chosenpack);
            Rules.Add(r2.Center, r2);
            //if (chosenpack.Indexes["Instance"] == 80)//p. && 
            //{
            //    string here = string.Empty;
            //}
            Refresh_ExtremePoints(chosenpack, chosenE);
            Refresh_Vertices(chosenpack, chosenE);
            FitnessList.Clear();
            input.Remove(input.First());

        }
        Load_order.Clear();
        Load_order.AddRange(loadorder);


        return;
    }
    public void UpdateDims(List<Package2D> input)
    {
        int mindim = Chosen_mindim;
        int maxdim = Chosen_maxdim;
        if (input.Count > 0)
        {
            foreach (Package2D p in input)
            {
                if ((p.Width < mindim) | (p.Length < mindim))
                {
                    bool usewidth = (p.Width < mindim) ? true : false;
                    bool uselength = (p.Length < mindim) ? true : false;
                    int choosesmaller = (p.Width < p.Length) ? p.Width : p.Length;

                    if (usewidth && uselength)
                    {
                        mindim = choosesmaller;
                    }
                    else if (usewidth)
                    {
                        mindim = p.Width;
                    }
                    else if (uselength)
                    {
                        mindim = p.Length;
                    }


                }
                //else if ((p.Width > maxdim) | (p.Length > maxdim))
                //{
                //    bool usewidth = (p.Width > maxdim) ? true : false;
                //    bool uselength = (p.Length > maxdim) ? true : false;
                //    int chooselarger = (p.Width > p.Length) ? p.Width : p.Length;

                //    if (usewidth && uselength)
                //    {
                //        maxdim = chooselarger;
                //    }
                //    else if (usewidth)
                //    {
                //        maxdim = p.Width;
                //    }
                //    else if (uselength)
                //    {
                //        maxdim = p.Length;
                //    }


                //}
            }
        }


        //Chosen_maxdim = maxdim;
        Chosen_mindim = mindim;


        return;
    }
    public List<Vertex2D> Fetchrelevant_vertices(ExtremePoint E) //returns vertices inside the packing space.
    {
        //if (E.X == 0 && E.Y ==32)//p. && 
        //{
        //    string here = string.Empty;
        //}
        //Right side in: the right side of the vertex is within the bounds while the left point is outside
        //left side in: the left side of the vertex is within the bounds while the right point is outside
        //both side in: both sides of the vertex are within the bounds 
        //both sides out: btoh sides are outside of the bounds but the height bounds the placement
        List<Vertex2D> foundvertices = new List<Vertex2D>();

        Vertex2D v2 = E.Initial_Space.Vertixes.Where(x => x.ID == "v2").ToList()[0];
        Vertex2D v3 = E.Initial_Space.Vertixes.Where(x => x.ID == "v3").ToList()[0];

        foreach (Vertex2D v in verticestoconsider)
        {
            switch (v.Orientation)
            {
                case "Horizontal":
                    {
                        if ((v.P1.Y >= E.Y) && (v.P1.Y <= v3.P1.Y))
                        { //within the vertical bounds
                            if ((v.P2.X < E.X) ||
                             (v.P1.X > v2.P1.X)) //either too far left or too far right 
                            { continue; }                                //(((v.P1.X> E.X) &&( v2.P1.X>v.P2.X))| //both sides in
                                                                         //((v.P1.X < v2.P1.X )&& (v2.P1.X<v.P2.X)) |//left side is in
                                                                         //((v.P1.X < E.X) && (v.P2.X > E.X) && (v2.P1.X > v.P2.X))) //right side in

                            else//within the vertical bounds
                            { foundvertices.Add(v); }
                        }
                        else { continue; }
                        break;
                    }
                case "Vertical":
                    {
                        if ((v.P1.X >= E.X) && (v.P1.X <= v2.P1.X))
                        { //within the horizontal bounds
                            if ((v.P2.Y < E.Y) ||
                            (v.P1.Y > v3.P1.Y)) //either too high or too low
                            { continue; }

                            else//within the horizontal bounds
                            { foundvertices.Add(v); }
                        }


                        else { continue; }
                        break;

                    }


            }

        }



        return foundvertices;
    }
    public List<Vertex2D> Fetchcoincident_vertices(List<Vertex2D> verticestoconsider, Point2D E, string orientation) //returns vertices that pass over a point
    {
        List<Vertex2D> foundvertices = new List<Vertex2D>();
        List<Vertex2D> relevantvertices = new List<Vertex2D>();
        if (orientation == "Vertical") { relevantvertices = verticestoconsider.Where(x => x.Orientation == "Vertical").ToList(); }
        else if (orientation == "Horizontal") { relevantvertices = verticestoconsider.Where(x => x.Orientation == "Horizontal").ToList(); }
        foreach (Vertex2D v in relevantvertices)
        {
            switch (v.Orientation)
            {
                case "Horizontal":
                    {
                        if (v.P1.Y == E.Y)
                        { //within the vertical bounds
                            if ((v.P1.X < E.X && v.P2.X < E.X) |
                             (v.P1.X > E.X && v.P2.X > E.X)) //either too far left or too far right 
                            { continue; }
                            else//within the vertical bounds
                            { foundvertices.Add(v); }
                        }
                        else { continue; }
                        break;
                    }
                case "Vertical":
                    {
                        if (v.P1.X == E.X)
                        { //within the horizontal bounds
                            if ((v.P1.Y < E.Y && v.P2.Y < E.Y) |
                            (v.P1.Y > E.Y && v.P2.Y > E.Y)) //either too high or too low 
                            { continue; }

                            else//within the horizontal bounds
                            { foundvertices.Add(v); }
                        }


                        else { continue; }
                        break;

                    }


            }

        }


        return foundvertices;
    }
    public List<Vertex2D> Fetchcollinear_vertices(List<Vertex2D> relevantverts, Vertex2D vertexin, string orientation) //returns vertices that touch each other and have the same direction
    {
        List<Vertex2D> foundvertices = new List<Vertex2D>();
        List<Vertex2D> relevantvertices = new List<Vertex2D>();
        if (orientation == "Vertical") { relevantvertices = relevantverts.Where(x => x.Orientation == "Vertical").ToList(); }
        else if (orientation == "Horizontal") { relevantvertices = relevantverts.Where(x => x.Orientation == "Horizontal").ToList(); }
        foreach (Vertex2D v in relevantvertices)
        {
            switch (v.Orientation)
            {
                case "Horizontal":
                    {
                        if (v.P1.Y == vertexin.P1.Y)
                        { //within the vertical bounds
                            if ((v.P1.X < vertexin.P1.X && v.P2.X < vertexin.P1.X) |
                             (v.P1.X > vertexin.P2.X && v.P2.X > vertexin.P2.X)) //either too far left or too far right 
                            { continue; }
                            else//within the vertical bounds
                            { foundvertices.Add(v); }
                        }
                        else { continue; }
                        break;
                    }
                case "Vertical":
                    {
                        if (v.P1.X == vertexin.P1.X)
                        { //within the horizontal bounds
                            if ((v.P1.Y < vertexin.P1.Y && v.P2.Y < vertexin.P1.Y) |
                             (v.P1.Y > vertexin.P2.Y && v.P2.Y > vertexin.P2.Y)) //either too far left or too far right 
                            { continue; }


                            else//within the horizontal bounds
                            { foundvertices.Add(v); }
                        }


                        else { continue; }
                        break;

                    }


            }

        }


        return foundvertices;
    }
    /// <summary>
    /// returns the vertex that crosses over the existing vertex closest to the given point. P1---point--p2, vertex is between P1--point (excludes point)
    /// </summary>
    /// <param name="verticestoconsider"></param>
    /// <param name="vertexin"></param>
    /// <param name="nearestothis"></param>
    /// <returns></returns>
    public Vertex2D Fetchcrossover_vertex_nearest(List<Vertex2D> verticestoconsider, Vertex2D vertexin, Point2D nearestothis) //returns vertices that cross over perpendicular
    {
        List<Vertex2D> foundvertices = new List<Vertex2D>();
        List<Vertex2D> relevantvertices = new List<Vertex2D>();
        Vertex2D v_out = new Vertex2D(new Point2D(0, 0, 0), new Point2D(0, 0, 0), vertexin.Orientation);
        if (vertexin.Orientation == "Horizontal") { relevantvertices = verticestoconsider.Where(x => x.Orientation == "Vertical").ToList(); }
        else if (vertexin.Orientation == "Vertical") { relevantvertices = verticestoconsider.Where(x => x.Orientation == "Horizontal").ToList(); }
        foreach (Vertex2D v in relevantvertices)
        {
            switch (v.Orientation)
            {
                case "Horizontal":
                    {
                        if ((v.P1.Y < nearestothis.Y) &&
                            (v.P1.Y >= vertexin.P1.Y) &&
                            (v.P1.X <= nearestothis.X) &&
                            (v.P2.X >= nearestothis.X))
                        { //within the vertical bounds and crossing over 

                            foundvertices.Add(v);
                        }
                        else { continue; }
                        break;
                    }
                case "Vertical":
                    {
                        if ((v.P2.X < nearestothis.X) &&
                             (v.P1.X >= vertexin.P1.X) &&
                             (v.P1.Y <= nearestothis.Y) &&
                             (v.P2.Y >= nearestothis.Y))
                        { //within the horizontal bounds and crossing over 

                            foundvertices.Add(v);
                        }
                        else { continue; }
                        break;

                    }


            }

        }
        if (foundvertices.Count > 0)
        {
            switch (foundvertices[0].Orientation)
            {
                case "Horizontal":
                    {
                        foundvertices = foundvertices.OrderByDescending(x => x.P1.Y).ToList();
                        break;
                    }
                case "Vertical":
                    {
                        foundvertices = foundvertices.OrderByDescending(x => x.P1.X).ToList();
                        break;
                    }
            }
            v_out = foundvertices[0];
        }


        return v_out;
    }
    public List<Vertex2D> Fetchcrossover_vertices(List<Vertex2D> verticestoconsider, Vertex2D vertexin) //returns vertices that cross over perpendicular
    {
        List<Vertex2D> foundvertices = new List<Vertex2D>();
        List<Vertex2D> relevantvertices = new List<Vertex2D>();
        if (vertexin.Orientation == "Horizontal") { relevantvertices = verticestoconsider.Where(x => x.Orientation == "Vertical").ToList(); }
        else if (vertexin.Orientation == "Vertical") { relevantvertices = verticestoconsider.Where(x => x.Orientation == "Horizontal").ToList(); }
        foreach (Vertex2D v in relevantvertices)
        {
            switch (v.Orientation)
            {
                case "Horizontal":
                    {
                        if ((v.P1.Y < vertexin.P2.Y) &&
                            (v.P1.Y >= vertexin.P1.Y) &&
                            (v.P1.X <= vertexin.P1.X) &&
                            (v.P2.X >= vertexin.P1.X))
                        { //within the vertical bounds and crossing over 

                            foundvertices.Add(v);
                        }
                        else { continue; }
                        break;
                    }
                case "Vertical":
                    {
                        if ((v.P2.X < vertexin.P2.X) &&
                             (v.P1.X >= vertexin.P1.X) &&
                             (v.P1.Y <= vertexin.P1.Y) &&
                             (v.P2.Y >= vertexin.P1.Y))
                        { //within the horizontal bounds and crossing over 

                            foundvertices.Add(v);
                        }
                        else { continue; }
                        break;

                    }


            }

        }


        return foundvertices;
    }


    public List<MasterRule> Fetch_Relevant_Masterrules(ExtremePoint E) //checks perimeter of a E_point
    {
        List<MasterRule> output = new List<MasterRule>();
        bool fitsmaster = true;

        foreach (MasterRule rule in Rules.Values)
        {
            List<Point2D> handle = new List<Point2D> { rule.Center };
            fitsmaster = E.Overlapcheck.TestPoints(handle);
            if (fitsmaster == false) { output.Add(rule); }
        }




        return output;

    }
    public void Refresh_ExtremePoints(Package2D p, ExtremePoint chosen)
    //first refresh this then vertices as it can cause confusion with vertices
    {
        if ((chosen.Y + p.Length) > StripHeight) { StripHeight = chosen.Y + p.Length; }
        List<ExtremePoint> toremove = new List<ExtremePoint> { chosen };
        //ActiveExtremePoints = ActiveExtremePoints.Except(toremove).ToList();
        //ActiveExtremePoints.Remove(chosen);
        ActiveExtremePoints.RemoveAll(x => x.X == chosen.X && x.Y == chosen.Y);
        Notallowed.Add(chosen);

        var E_toappend = from point in p.Pointslist //fetch the 2nd and 4th points
                         where point.Index % 2 == 0
                         // where point.Index == 4
                         select point;
        List<ExtremePoint> newE = new List<ExtremePoint>();
        foreach (var e in E_toappend) //add these to a collection 
        {
            ExtremePoint Epoint = new ExtremePoint(Chosen_maxdim, e.X, e.Y, e.Index);
            newE.Add(Epoint);
        }
        foreach (ExtremePoint e in newE)
        {
            List<Vertex2D> verticesofpoint = new List<Vertex2D>();


            switch (e.Index)
            {   //merge v2 with the found vertex VERTICAL OUTPUT
                case 2:
                    {
                        if ((e.X + Chosen_mindim <= Bin.Width) && (Notallowed.Where(x => x.X == e.X && x.Y == e.Y).ToList().Count == 0)) { ActiveExtremePoints.Add(e); }
                        Vertex2D v2 = p.Vertixes.Where(x => x.ID == "v2").ToList()[0];
                        verticesofpoint = Fetchcoincident_vertices(verticestoconsider, e, "Vertical").ToList();


                        if (verticesofpoint.Count == 0)
                        { //add shadows here
                            Vertex2D E_v1 = chosen.Initial_Space.Vertixes.Where(x => x.ID == "v1").ToList()[0];
                            List<Vertex2D> verticesofpoint2 = Fetchcoincident_vertices(verticestoconsider, e, "Horizontal").ToList();
                            if (verticesofpoint2.Count == 0) //hanging
                            {
                                ExtremePoint ShadowVertical = ShadowPoint(p, e, v2.Orientation);
                                bool testthis = Simple_Overlapcheck(ShadowVertical);
                                if (ShadowVertical.Index != 1000)
                                {
                                    Vertex2D virtualvert = new Vertex2D(ShadowVertical, ShadowVertical.Original_Point, ShadowVertical.Shadowextension_Orientation);
                                    Virtual_Vertices.Add(virtualvert);
                                    ActiveExtremePoints.Add(ShadowVertical);
                                }
                            }
                            verticestoconsider.Add(v2);
                        }//if not on an existing vertex add to extreme points

                        else if (verticesofpoint.Count > 0)
                        {
                            List<Vertex2D> used = new List<Vertex2D>();
                            Vertex2D newvert = v2;
                            foreach (Vertex2D verticesofp in verticesofpoint)
                            {
                                if (verticesofp.Orientation == v2.Orientation)
                                {
                                    newvert = MergeVertices(verticesofp, v2);
                                    used.Add(verticesofp);
                                    if (newvert.P2.X == 0 && newvert.P2.Y == 0) { Errorlog.Add($"Merge Error between: {verticesofpoint[0]} and {v2}"); }
                                }
                            }


                            if (newvert.P1.Y != newvert.P2.Y)
                            {
                                verticestoconsider = verticestoconsider.Except(used).ToList();
                                verticestoconsider.Add(newvert);
                            }

                        }

                        break;
                    }
                //merge v3 with the found vertex HORIZONTAL OUTPUT
                case 4:
                    {
                        if ((e.Y + Chosen_mindim <= Bin.Length) && (Notallowed.Where(x => x.X == e.X && x.Y == e.Y).ToList().Count == 0)) { ActiveExtremePoints.Add(e); }
                        verticesofpoint = Fetchcoincident_vertices(verticestoconsider, e, "Horizontal").ToList();
                        Vertex2D v3 = p.Vertixes.Where(x => x.ID == "v3").ToList()[0];

                        if (verticesofpoint.Count == 0)
                        {
                            Vertex2D E_v4 = chosen.Initial_Space.Vertixes.Where(x => x.ID == "v4").ToList()[0];
                            List<Vertex2D> verticesofpoint4 = Fetchcoincident_vertices(verticestoconsider, e, "Vertical").ToList();
                            if (verticesofpoint4.Count == 0) //stepping up
                            {
                                ExtremePoint ShadowHorizontal = ShadowPoint(p, e, v3.Orientation);

                                if (ShadowHorizontal.Index != 1000)
                                {
                                    Vertex2D virtualvert = new Vertex2D(ShadowHorizontal, ShadowHorizontal.Original_Point, ShadowHorizontal.Shadowextension_Orientation);
                                    Virtual_Vertices.Add(virtualvert);
                                    ActiveExtremePoints.Add(ShadowHorizontal);
                                }
                            }


                            verticestoconsider.Add(v3);
                        }//if not on an existing vertex add to extreme points
                        else if (verticesofpoint.Count > 0)
                        {
                            List<Vertex2D> used = new List<Vertex2D>();
                            Vertex2D newvert = v3;
                            foreach (Vertex2D verticesofp in verticesofpoint)
                            {
                                if (verticesofp.Orientation == v3.Orientation)
                                {
                                    newvert = MergeVertices(verticesofp, v3);
                                    used.Add(verticesofp);
                                    if (newvert.P2.X == 0 && newvert.P2.Y == 0)
                                    {
                                        Errorlog.Add($"Merge Error between: {verticesofpoint[0]} and {v3}");
                                    }
                                }

                            }
                            if (newvert.P1.X != newvert.P2.X)
                            {
                                verticestoconsider = verticestoconsider.Except(used).ToList();
                                verticestoconsider.Add(newvert);
                            }

                        }

                        break;
                    }
            }

        }
        foreach (ExtremePoint E in ActiveExtremePoints.ToList()) //checks overlaps of Extreme points
        {
            //if (E.X == 440 && E.Y == 1049)
            //{
            //    string here = string.Empty;
            //}
            //if (Notallowed.Where(x => x.X == E.X && x.Y == E.Y).ToList().Count != 0)
            //{
            //    ActiveExtremePoints.Remove(E);
            //}
            Overlapcheck(E);


        }

        foreach (Vertex2D v in Virtual_Vertices.ToList()) //check future shadows from existing shadow casts (if another package enters between)
        {

            List<Vertex2D> crossingvirtual = Fetchcrossover_vertices(Virtual_Vertices, v);
            Vertex2D crossing = Fetchcrossover_vertex_nearest(p.Vertixes.ToList(), v, v.P2); //we only check the last package as the last package is going to cross 
            if (!(crossing.P1.Index == 0 && crossing.P2.Index == 0))
            {

                switch (crossing.Orientation)
                {
                    case "Vertical":
                        {
                            if ((crossing.P2.X != v.P1.X) && (ActiveExtremePoints.Where(x => x.X == crossing.P1.X && x.Y == v.P1.Y).ToList().Count == 0))
                            {

                                ExtremePoint newshadow = new ExtremePoint(Chosen_maxdim, crossing.P1.X, v.P1.Y, v.P2.Index);
                                if (newshadow.X == 145 && newshadow.Y == 82)
                                {
                                    string testing = string.Empty;
                                }
                                if (Notallowed.Where(x => x.X == newshadow.X && x.Y == newshadow.Y).ToList().Count > 0) { break; }
                                bool test = Simple_Overlapcheck(newshadow); if (!test)
                                {
                                    Notallowed.Add(newshadow);
                                    break;
                                }

                                newshadow.Identifier = "Shadow"; newshadow.Original_Point = v.P2;
                                newshadow.Shadowextension_Orientation = v.Orientation;
                                Vertex2D newvertex = new Vertex2D(newshadow, newshadow.Original_Point, newshadow.Shadowextension_Orientation);
                                Virtual_Vertices.Add(newvertex);
                                ActiveExtremePoints.Add(newshadow);
                                //Overlapcheck(newshadow);
                            }
                            break;
                        }

                    case "Horizontal":
                        {

                            if ((crossing.P2.Y != v.P1.Y) && (ActiveExtremePoints.Where(x => x.X == v.P1.X && x.Y == crossing.P1.Y).ToList().Count == 0)) //when 2 is going down, there is no existing E on this found crossover vertex
                            {

                                ExtremePoint newshadow = new ExtremePoint(Chosen_maxdim, v.P1.X, crossing.P1.Y, v.P2.Index);

                                if (Notallowed.Where(x => x.X == newshadow.X && x.Y == newshadow.Y).ToList().Count > 0) { break; }
                                bool test = Simple_Overlapcheck(newshadow); if (!test)
                                {
                                    Notallowed.Add(newshadow);
                                    break;
                                }
                                newshadow.Identifier = "Shadow"; newshadow.Original_Point = v.P2;
                                newshadow.Shadowextension_Orientation = v.Orientation;
                                Vertex2D newvertex = new Vertex2D(newshadow, newshadow.Original_Point, newshadow.Shadowextension_Orientation);
                                Virtual_Vertices.Add(newvertex);
                                ActiveExtremePoints.Add(newshadow);
                                //Overlapcheck(newshadow);
                            }
                            break;
                        }
                }
            }
            if (crossingvirtual.Count > 0)
            {
                switch (v.Orientation)
                {
                    case "Vertical": //falling down
                        {
                            foreach (Vertex2D v2 in crossingvirtual)
                            {
                                if ((ActiveExtremePoints.Where(x => x.X == v.P1.X && x.Y == v2.P1.Y).ToList().Count == 0) && (Notallowed.Where(x => x.X == v.P1.X && x.Y == v2.P1.Y).ToList().Count == 0))
                                {
                                    ExtremePoint newvirtual = new ExtremePoint(Chosen_maxdim, v.P1.X, v2.P1.Y, v.P2.Index);
                                    bool test = Simple_Overlapcheck(newvirtual); if (!test) { Notallowed.Add(newvirtual); break; }

                                    newvirtual.Identifier = "Virtual"; newvirtual.Original_Point = v.P2;
                                    newvirtual.Shadowextension_Orientation = v.Orientation;
                                    Vertex2D newvertex = new Vertex2D(newvirtual, newvirtual.Original_Point, newvirtual.Shadowextension_Orientation);
                                    Virtual_Vertices.Add(newvertex);
                                    ActiveExtremePoints.Add(newvirtual);
                                    //Overlapcheck(newshadow);}
                                }

                            }
                            break;
                        }
                    case "Horizontal":
                        {
                            foreach (Vertex2D v2 in crossingvirtual)
                            {
                                if ((ActiveExtremePoints.Where(x => x.X == v2.P1.X && x.Y == v.P1.Y).ToList().Count == 0) && (Notallowed.Where(x => x.X == v2.P1.X && x.Y == v.P1.Y).ToList().Count == 0))
                                {
                                    ExtremePoint newvirtual = new ExtremePoint(Chosen_maxdim, v2.P1.X, v.P1.Y, v.P2.Index);
                                    bool test = Simple_Overlapcheck(newvirtual); if (!test)
                                    { Notallowed.Add(newvirtual); break; }

                                    newvirtual.Identifier = "Virtual"; newvirtual.Original_Point = v.P2;
                                    newvirtual.Shadowextension_Orientation = v.Orientation;
                                    Vertex2D newvertex = new Vertex2D(newvirtual, newvirtual.Original_Point, newvirtual.Shadowextension_Orientation);
                                    Virtual_Vertices.Add(newvertex);
                                    ActiveExtremePoints.Add(newvirtual);
                                    //Overlapcheck(newshadow);
                                }
                            }
                            break;
                        }
                }
            }
        }


        return;
    }
    public void Refresh_ExtremePoints_Large(Package2D p, ExtremePoint chosen)
    //first refresh this then vertices as it can cause confusion with vertices
    {
        if ((chosen.Y + p.Length) > StripHeight) { StripHeight = chosen.Y + p.Length; }
        ActiveExtremePoints.RemoveAll(x => x.X == chosen.X && x.Y == chosen.Y);
        Notallowed.Add(chosen);

        var E_toappend = from point in p.Pointslist //fetch the 2nd and 4th points
                         where point.Index % 2 == 0
                         // where point.Index == 4
                         select point;
        List<ExtremePoint> newE = new List<ExtremePoint>();
        foreach (var e in E_toappend) //add these to a collection 
        {
            ExtremePoint Epoint = new ExtremePoint(Chosen_maxdim, e.X, e.Y, e.Index);
            newE.Add(Epoint);
        }
        foreach (ExtremePoint e in newE)
        {
            List<Vertex2D> verticesofpoint = new List<Vertex2D>();


            switch (e.Index)
            {   //merge v2 with the found vertex VERTICAL OUTPUT
                case 2:
                    {

                        Vertex2D v2 = p.Vertixes.Where(x => x.ID == "v2").ToList()[0];
                        verticesofpoint = Fetchcoincident_vertices(verticestoconsider, e, "Vertical").ToList();


                        if (verticesofpoint.Count == 0)
                        { //add shadows here
                            if ((e.X + Chosen_mindim <= Bin.Width) && (Notallowed.Where(x => x.X == e.X && x.Y == e.Y).ToList().Count == 0)) { ActiveExtremePoints.Add(e); }
                            Vertex2D E_v1 = chosen.Initial_Space.Vertixes.Where(x => x.ID == "v1").ToList()[0];
                            List<Vertex2D> verticesofpoint2 = Fetchcoincident_vertices(verticestoconsider, e, "Horizontal").ToList();
                            if (verticesofpoint2.Count == 0) //hanging
                            {
                                ExtremePoint ShadowVertical = ShadowPoint(p, e, v2.Orientation);
                                bool testthis = Simple_Overlapcheck(ShadowVertical);
                                if (ShadowVertical.Index != 1000)
                                {
                                    Vertex2D virtualvert = new Vertex2D(ShadowVertical, ShadowVertical.Original_Point, ShadowVertical.Shadowextension_Orientation);
                                    Virtual_Vertices.Add(virtualvert);
                                    ActiveExtremePoints.Add(ShadowVertical);
                                }
                            }
                            verticestoconsider.Add(v2);
                        }//if not on an existing vertex add to extreme points

                        else if (verticesofpoint.Count > 0)
                        {
                            List<Vertex2D> used = new List<Vertex2D>();
                            Vertex2D newvert = v2;
                            foreach (Vertex2D verticesofp in verticesofpoint)
                            {
                                if (verticesofp.Orientation == v2.Orientation)
                                {
                                    newvert = MergeVertices(verticesofp, v2);
                                    used.Add(verticesofp);
                                    if (newvert.P2.X == 0 && newvert.P2.Y == 0) { Errorlog.Add($"Merge Error between: {verticesofpoint[0]} and {v2}"); }
                                }
                            }


                            if (newvert.P1.Y != newvert.P2.Y)
                            {
                                verticestoconsider = verticestoconsider.Except(used).ToList();
                                verticestoconsider.Add(newvert);
                            }

                        }

                        break;
                    }
                //merge v3 with the found vertex HORIZONTAL OUTPUT
                case 4:
                    {

                        verticesofpoint = Fetchcoincident_vertices(verticestoconsider, e, "Horizontal").ToList();
                        Vertex2D v3 = p.Vertixes.Where(x => x.ID == "v3").ToList()[0];

                        if (verticesofpoint.Count == 0)
                        {
                            if ((e.Y + Chosen_mindim <= Bin.Length) && (Notallowed.Where(x => x.X == e.X && x.Y == e.Y).ToList().Count == 0)) { ActiveExtremePoints.Add(e); }
                            Vertex2D E_v4 = chosen.Initial_Space.Vertixes.Where(x => x.ID == "v4").ToList()[0];
                            List<Vertex2D> verticesofpoint4 = Fetchcoincident_vertices(verticestoconsider, e, "Vertical").ToList();
                            if (verticesofpoint4.Count == 0) //stepping up
                            {
                                ExtremePoint ShadowHorizontal = ShadowPoint(p, e, v3.Orientation);

                                if (ShadowHorizontal.Index != 1000)
                                {
                                    Vertex2D virtualvert = new Vertex2D(ShadowHorizontal, ShadowHorizontal.Original_Point, ShadowHorizontal.Shadowextension_Orientation);
                                    Virtual_Vertices.Add(virtualvert);
                                    ActiveExtremePoints.Add(ShadowHorizontal);
                                }
                            }


                            verticestoconsider.Add(v3);
                        }//if not on an existing vertex add to extreme points
                        else if (verticesofpoint.Count > 0)
                        {
                            List<Vertex2D> used = new List<Vertex2D>();
                            Vertex2D newvert = v3;
                            foreach (Vertex2D verticesofp in verticesofpoint)
                            {
                                if (verticesofp.Orientation == v3.Orientation)
                                {
                                    newvert = MergeVertices(verticesofp, v3);
                                    used.Add(verticesofp);
                                    if (newvert.P2.X == 0 && newvert.P2.Y == 0)
                                    {
                                        Errorlog.Add($"Merge Error between: {verticesofpoint[0]} and {v3}");
                                    }
                                }

                            }
                            if (newvert.P1.X != newvert.P2.X)
                            {
                                verticestoconsider = verticestoconsider.Except(used).ToList();
                                verticestoconsider.Add(newvert);
                            }

                        }

                        break;
                    }
            }

        }
        foreach (ExtremePoint E in ActiveExtremePoints.ToList()) //checks overlaps of Extreme points
        {
            if (E.X == 0 && E.Y == 88)
            {
                string here = string.Empty;
            }
            //if (Notallowed.Where(x => x.X == E.X && x.Y == E.Y).ToList().Count != 0)
            //{
            //    ActiveExtremePoints.Remove(E);
            //}
            Overlapcheck(E);


        }

        foreach (Vertex2D v in Virtual_Vertices.ToList()) //check future shadows from existing shadow casts (if another package enters between)
        {

            List<Vertex2D> crossingvirtual = Fetchcrossover_vertices(Virtual_Vertices, v);
            Vertex2D crossing = Fetchcrossover_vertex_nearest(p.Vertixes.ToList(), v, v.P2); //we only check the last package as the last package is going to cross 
            if (!(crossing.P1.Index == 0 && crossing.P2.Index == 0))
            {

                switch (crossing.Orientation)
                {
                    case "Vertical":
                        {
                            if ((crossing.P2.X != v.P1.X) && (ActiveExtremePoints.Where(x => x.X == crossing.P1.X && x.Y == v.P1.Y).ToList().Count == 0))
                            {

                                ExtremePoint newshadow = new ExtremePoint(Chosen_maxdim, crossing.P1.X, v.P1.Y, v.P2.Index);
                                if (newshadow.X == 145 && newshadow.Y == 82)
                                {
                                    string testing = string.Empty;
                                }
                                if (Notallowed.Where(x => x.X == newshadow.X && x.Y == newshadow.Y).ToList().Count > 0) { break; }
                                bool test = Simple_Overlapcheck(newshadow); if (!test)
                                {
                                    Notallowed.Add(newshadow);
                                    break;
                                }

                                newshadow.Identifier = "Shadow"; newshadow.Original_Point = v.P2;
                                newshadow.Shadowextension_Orientation = v.Orientation;
                                Vertex2D newvertex = new Vertex2D(newshadow, newshadow.Original_Point, newshadow.Shadowextension_Orientation);
                                Virtual_Vertices.Add(newvertex);
                                ActiveExtremePoints.Add(newshadow);
                                //Overlapcheck(newshadow);
                            }
                            break;
                        }

                    case "Horizontal":
                        {

                            if ((crossing.P2.Y != v.P1.Y) && (ActiveExtremePoints.Where(x => x.X == v.P1.X && x.Y == crossing.P1.Y).ToList().Count == 0)) //when 2 is going down, there is no existing E on this found crossover vertex
                            {

                                ExtremePoint newshadow = new ExtremePoint(Chosen_maxdim, v.P1.X, crossing.P1.Y, v.P2.Index);

                                if (Notallowed.Where(x => x.X == newshadow.X && x.Y == newshadow.Y).ToList().Count > 0) { break; }
                                bool test = Simple_Overlapcheck(newshadow); if (!test)
                                {
                                    Notallowed.Add(newshadow);
                                    break;
                                }
                                newshadow.Identifier = "Shadow"; newshadow.Original_Point = v.P2;
                                newshadow.Shadowextension_Orientation = v.Orientation;
                                Vertex2D newvertex = new Vertex2D(newshadow, newshadow.Original_Point, newshadow.Shadowextension_Orientation);
                                Virtual_Vertices.Add(newvertex);
                                ActiveExtremePoints.Add(newshadow);
                                //Overlapcheck(newshadow);
                            }
                            break;
                        }
                }
            }
            if (crossingvirtual.Count > 0)
            {
                switch (v.Orientation)
                {
                    case "Vertical": //falling down
                        {
                            foreach (Vertex2D v2 in crossingvirtual)
                            {
                                if ((ActiveExtremePoints.Where(x => x.X == v.P1.X && x.Y == v2.P1.Y).ToList().Count == 0) && (Notallowed.Where(x => x.X == v.P1.X && x.Y == v2.P1.Y).ToList().Count == 0))
                                {
                                    ExtremePoint newvirtual = new ExtremePoint(Chosen_maxdim, v.P1.X, v2.P1.Y, v.P2.Index);
                                    bool test = Simple_Overlapcheck(newvirtual); if (!test) { Notallowed.Add(newvirtual); break; }

                                    newvirtual.Identifier = "Virtual"; newvirtual.Original_Point = v.P2;
                                    newvirtual.Shadowextension_Orientation = v.Orientation;
                                    Vertex2D newvertex = new Vertex2D(newvirtual, newvirtual.Original_Point, newvirtual.Shadowextension_Orientation);
                                    Virtual_Vertices.Add(newvertex);
                                    ActiveExtremePoints.Add(newvirtual);
                                    //Overlapcheck(newshadow);}
                                }

                            }
                            break;
                        }
                    case "Horizontal":
                        {
                            foreach (Vertex2D v2 in crossingvirtual)
                            {
                                if ((ActiveExtremePoints.Where(x => x.X == v2.P1.X && x.Y == v.P1.Y).ToList().Count == 0) && (Notallowed.Where(x => x.X == v2.P1.X && x.Y == v.P1.Y).ToList().Count == 0))
                                {
                                    ExtremePoint newvirtual = new ExtremePoint(Chosen_maxdim, v2.P1.X, v.P1.Y, v.P2.Index);
                                    bool test = Simple_Overlapcheck(newvirtual); if (!test)
                                    { Notallowed.Add(newvirtual); break; }

                                    newvirtual.Identifier = "Virtual"; newvirtual.Original_Point = v.P2;
                                    newvirtual.Shadowextension_Orientation = v.Orientation;
                                    Vertex2D newvertex = new Vertex2D(newvirtual, newvirtual.Original_Point, newvirtual.Shadowextension_Orientation);
                                    Virtual_Vertices.Add(newvertex);
                                    ActiveExtremePoints.Add(newvirtual);
                                    //Overlapcheck(newshadow);
                                }
                            }
                            break;
                        }
                }
            }
        }


        return;
    }
    public void Overlapcheck(ExtremePoint E)
    {
        if (E.Overlapcheck == null)
        {
            int perimeterdim = Convert.ToInt32(Chosen_maxdim * 2);
            Package2D Perimeter = new Package2D(perimeterdim, perimeterdim);
            Perimeter.OverwritePosition(E.X - perimeterdim / 2, E.Y - perimeterdim / 2, 1);
            MasterRule Extreme_inside = new MasterRule(Perimeter);
            Extreme_inside.Center = E;
            E.Overlapcheck = Extreme_inside;
        }
        List<MasterRule> relevantones = Fetch_Relevant_Masterrules(E);
        if (relevantones.Count > 0)
        {
            bool fitsmaster = true;
            List<Point2D> inputofrule = new List<Point2D> { E, new Point2D(E.X + Chosen_mindim, E.Y + Chosen_mindim, 0), new Point2D(E.X, E.Y + Chosen_mindim, 0), new Point2D(E.X + Chosen_mindim, E.Y, 0), new Point2D(E.X, E.Y + 1, 0), new Point2D(E.X + 1, E.Y, 0) };
            foreach (MasterRule rule in relevantones)
            {
                fitsmaster = rule.TestPoints(inputofrule);
                if (fitsmaster == false) { break; }
            }
            if (fitsmaster == false)
            {
                if (E.Identifier == "Shadow") // if the removed point is a shadow we need to find a new shadow (if exists)
                {
                    Vertex2D extension = new Vertex2D(E, E.Original_Point, E.Shadowextension_Orientation);
                    Vertex2D uncastingshadow = Fetchcrossover_vertex_nearest(verticestoconsider, extension, E.Original_Point);
                    if (!(uncastingshadow.P1.Index == 0 && uncastingshadow.P2.Index == 0))
                    {
                        switch (E.Shadowextension_Orientation)
                        {
                            case "Vertical":
                                {
                                    if (Notallowed.Where(x => x.Y == uncastingshadow.P1.Y && x.X == E.X).ToList().Count != 0) { break; }
                                    if ((uncastingshadow.P1.Y == E.Original_Point.Y) || (uncastingshadow.P1.Y == E.Y)) { break; }
                                    ExtremePoint newshadow = new ExtremePoint(Chosen_maxdim, E.X, uncastingshadow.P1.Y, E.Index);
                                    newshadow.Identifier = "Shadow"; newshadow.Original_Point = E.Original_Point;
                                    newshadow.Shadowextension_Orientation = E.Shadowextension_Orientation;
                                    Vertex2D oldvertex = Virtual_Vertices.Where(x => x.P2.X == E.Original_Point.X && x.P2.Y == E.Original_Point.Y && x.P1.X == E.X && x.P1.Y == E.Y).First();
                                    Virtual_Vertices.Remove(oldvertex);
                                    Vertex2D newvertex = new Vertex2D(newshadow, newshadow.Original_Point, newshadow.Shadowextension_Orientation);
                                    Virtual_Vertices.Add(newvertex);
                                    ActiveExtremePoints.Add(newshadow);

                                    break;
                                }

                            case "Horizontal":
                                {
                                    if (Notallowed.Where(x => x.X == uncastingshadow.P1.X && x.Y == E.Y).ToList().Count != 0) { break; }
                                    if ((uncastingshadow.P1.X == E.Original_Point.X) || (uncastingshadow.P1.X == E.X)) { break; }
                                    ExtremePoint newshadow = new ExtremePoint(Chosen_maxdim, uncastingshadow.P1.X, E.Y, E.Index);

                                    newshadow.Identifier = "Shadow"; newshadow.Original_Point = E.Original_Point;
                                    newshadow.Shadowextension_Orientation = E.Shadowextension_Orientation;

                                    Vertex2D oldvertex = Virtual_Vertices.Where(x => x.P2.X == E.Original_Point.X && x.P2.Y == E.Original_Point.Y && x.P1.X == E.X && x.P1.Y == E.Y).First();
                                    Virtual_Vertices.Remove(oldvertex);
                                    Vertex2D newvertex = new Vertex2D(newshadow, newshadow.Original_Point, newshadow.Shadowextension_Orientation);
                                    Virtual_Vertices.Add(newvertex);
                                    ActiveExtremePoints.Add(newshadow);

                                    break;
                                }
                        }
                    }
                }
                List<ExtremePoint> toremove = ActiveExtremePoints.Where(x => x.X == E.X && x.Y == E.Y).ToList();
                ActiveExtremePoints = ActiveExtremePoints.Except(toremove).ToList();
            }
            //if (ActiveExtremePoints.Where(x => x.X == 31 && x.Y == 120).ToList().Count != 0)
            //{
            //    string test = string.Empty;
            //}
        }
        return;

    }
    public bool Simple_Overlapcheck(ExtremePoint E)
    {
        if (E.X == 15 && E.Y == 301)
        {
            string test = string.Empty;
        }
        bool fitsmaster = true;
        if (E.Overlapcheck == null)
        {
            int perimeterdim = Convert.ToInt32(Chosen_maxdim * 2);
            Package2D Perimeter = new Package2D(perimeterdim, perimeterdim);
            Perimeter.OverwritePosition(E.X - perimeterdim / 2, E.Y - perimeterdim / 2, 1);
            MasterRule Extreme_inside = new MasterRule(Perimeter);
            Extreme_inside.Center = E;
            E.Overlapcheck = Extreme_inside;
        }
        List<MasterRule> relevantones = Fetch_Relevant_Masterrules(E);
        if (relevantones.Count > 0)
        {
            List<Point2D> inputofrule = new List<Point2D> { E, new Point2D(E.X + Chosen_mindim, E.Y + Chosen_mindim, 0), new Point2D(E.X, E.Y + Chosen_mindim, 0), new Point2D(E.X + Chosen_mindim, E.Y, 0), new Point2D(E.X, E.Y + 1, 0), new Point2D(E.X + 1, E.Y, 0) };
            foreach (MasterRule rule in relevantones)
            {

                fitsmaster = rule.TestPoints(inputofrule);
                if (fitsmaster == false)
                {
                    fitsmaster = false; break;
                }
            }

        }
        return fitsmaster;

    }

    public bool Simple_Overlapcheck_manual(ExtremePoint E, List<Point2D> inputtest)
    {
        bool fitsmaster = true;
        if (E.Overlapcheck == null)
        {
            int perimeterdim = Convert.ToInt32(Chosen_maxdim * 2);
            Package2D Perimeter = new Package2D(perimeterdim, perimeterdim);
            Perimeter.OverwritePosition(E.X - perimeterdim / 2, E.Y - perimeterdim / 2, 1);
            MasterRule Extreme_inside = new MasterRule(Perimeter);
            Extreme_inside.Center = E;
            E.Overlapcheck = Extreme_inside;
        }

        fitsmaster = E.Overlapcheck.TestPoints(inputtest);




        return fitsmaster;

    }
    public bool Sequential_Overlapcheck(ExtremePoint E, Package2D P)
    {
        bool fitssequence = true;
        Vertex2D fromE = new Vertex2D(E, new Point2D(E.X, Bin.Length, 2), "Vertical");
        Point2D P2 = P.Pointslist.Where(x => x.Index == 2).ToList()[0];
        Vertex2D fromP2 = new Vertex2D(P2, new Point2D(P2.X, Bin.Length, 2), "Vertical");

        List<Vertex2D> horizontalcollection_fromE = Fetchcrossover_vertices(verticestoconsider, fromE);
        foreach (Vertex2D v in horizontalcollection_fromE.ToList())
        {
            if ((v.P2.X == E.X) || (v.P1.Y == Bin.Length) || (v.P1.Y == E.Y))
            { horizontalcollection_fromE.Remove(v); }
        }
        List<Vertex2D> horizontalcollection_fromP2 = Fetchcrossover_vertices(verticestoconsider, fromP2);
        foreach (Vertex2D v in horizontalcollection_fromP2.ToList())
        {
            if ((v.P1.X == P2.X) || (v.P1.Y == Bin.Length) || (v.P1.Y == P2.Y))
            { horizontalcollection_fromP2.Remove(v); }
        }
        if ((horizontalcollection_fromE.Count > 0) || (horizontalcollection_fromP2.Count > 0))
        {
            fitssequence = false;
        }


        return fitssequence;
    }
    /// <summary>
    /// use after packing 
    /// </summary>
    /// <param name="p"></param>
    /// <param name="chosen"></param>
    public void Refresh_Vertices(Package2D p, ExtremePoint chosen) //after packing
    {   //overlapping vertices must be merged (v2,v3) done

        //ADD V1
        Vertex2D v1 = p.Vertixes.Where(x => x.ID == "v1").ToList()[0];
        Vertex2D E_v1 = chosen.Initial_Space.Vertixes.Where(x => x.ID == "v1").ToList()[0];

        List<Vertex2D> Contains_v1 = Fetchcollinear_vertices(verticestoconsider, v1, v1.Orientation);

        if (Contains_v1.Count > 0) //should be positive if there are real vertices
        {
            Vertex2D merged = v1;
            foreach (Vertex2D vexists in Contains_v1.ToList())
            {
                if (!((vexists.P1.X > v1.P2.X) || (vexists.P2.X < v1.P1.X)))
                {

                    merged = MergeVertices(vexists, merged); //cumulate all vertices
                    if (merged.P2.X == 0 && merged.P2.Y == 0) { continue; }
                    //else we add the shorter vertex output

                    verticestoconsider.Remove(vexists); //in any case we remove the vertex 
                }
            }
            if (merged.P1.X != merged.P2.X) { verticestoconsider.Add(merged); }
        }
        else { verticestoconsider.Add(v1); }







        //ADD V4
        Vertex2D v4 = p.Vertixes.Where(x => x.ID == "v4").ToList()[0];
        Vertex2D E_v4 = chosen.Initial_Space.Vertixes.Where(x => x.ID == "v4").ToList()[0];
        List<Vertex2D> Contains_v4 = Fetchcollinear_vertices(chosen.Spatial_Vertices, v4, v4.Orientation);


        if (Contains_v4.Count > 0)
        {
            Vertex2D merged = v4;
            foreach (Vertex2D vexists in Contains_v4.ToList())
            {
                if (!((vexists.P1.Y > v4.P2.Y) || (vexists.P2.Y < v4.P1.Y)))
                {

                    merged = MergeVertices(vexists, merged);
                    if (merged.P2.X == 0 && merged.P2.Y == 0) { continue; }
                    verticestoconsider.Remove(vexists);
                }

            }
            if (merged.P1.Y != merged.P2.Y) { verticestoconsider.Add(merged); }
        }

        else { verticestoconsider.Add(v4); }






        foreach (Vertex2D v in verticestoconsider.ToList()) //single points
        {
            switch (v.Orientation)
            {
                case "Vertical":
                    {
                        if (v.P1.Y == v.P2.Y) { verticestoconsider.Remove(v); }
                        break;
                    }
                case "Horizontal":
                    {
                        if (v.P1.X == v.P2.X) { verticestoconsider.Remove(v); }
                        break;
                    }
            }
        }

        for (int i = 0; i < verticestoconsider.ToList().Count; i++) //duplicates
        {
            Vertex2D v = verticestoconsider[i];
            List<Vertex2D> verticesexcept = verticestoconsider.Where(x => x != v).ToList();
            List<Vertex2D> internallist = Fetchcollinear_vertices(verticesexcept, v, v.Orientation);

            if (internallist.Count > 0)
            {
                for (int j = 0; j < internallist.ToList().Count; j++)
                {

                    Vertex2D v2 = internallist[j];

                    if ((v.P1.X == v2.P1.X) && (v.P1.Y == v2.P1.Y)
                        && (v.P2.X == v2.P2.X) && (v.P2.Y == v2.P2.Y)) { verticestoconsider.Remove(v2); i = 0; j = 0; }
                    else if (((v.P1.X == v2.P1.X) && (v.P1.Y == v2.P1.Y))
                        ^ ((v.P2.X == v2.P2.X) && (v.P2.Y == v2.P2.Y)))
                    {
                        Vertex2D v3 = MergeVertices(v, v2);
                        verticestoconsider.Remove(v);
                        verticestoconsider.Remove(v2);
                        verticestoconsider.Add(v3);
                    }


                }
            }
        }



        //irrelevant vertices must not be considered. 
        //if v1 hangs over E.Space v1 then the difference must be a vertex
        //vertices must be reconstructed so that every vertex is 100% exposed 



        return;
    }
    public Vertex2D MergeVertices(Vertex2D v1, Vertex2D v2) //(existing,entering)
    {
        Vertex2D v_out = new Vertex2D(new Point2D(0, 0, 0), new Point2D(0, 0, 0), v1.Orientation); //dummy rn
        if (v1.Orientation != v2.Orientation) { Errorlog.Add($"Merge Error between: {v1} and {v2} due to orientation mismatch"); }
        switch (v1.Orientation)
        {
            case "Vertical":
                {
                    if (v1.ID == "v4" | v2.ID == "v4") { v_out.ID = "v4"; } //to identify overlaps better
                    if (v1.P1.Y > v2.P1.Y) //if order is wrong it is corrected
                    {
                        Vertex2D temp = v1;
                        v1 = v2;
                        v2 = temp;
                    }
                    if (v1.P2.Y > v2.P2.Y) //if v1 covers v2 in any case
                    {
                        v_out = v1; break;
                    }
                    if (v1.P2.Y < v2.P1.Y) { Errorlog.Add($"Merge Error between: {v1} and {v2} due to noncoincidence"); break; } //break out of the switch due to the error
                    else
                    {
                        v_out = new Vertex2D(v1.P1, v2.P2, v1.Orientation);
                    }
                    break;
                }




            case "Horizontal":
                {
                    if (v1.ID == "v1" | v2.ID == "v1") { v_out.ID = "v1"; }
                    if (v1.P1.X > v2.P1.X) //if order is wrong it is corrected
                    {
                        Vertex2D temp = v1;
                        v1 = v2;
                        v2 = temp;
                    }
                    if (v1.P2.X > v2.P2.X) //if v1 covers v2 in any case
                    {
                        v_out = v1; break;
                    }
                    if (v1.P2.X < v2.P1.X) { Errorlog.Add($"Merge Error between: {v1} and {v2} due to noncoincidence"); break; } //break out of the switch due to the error
                    else
                    {
                        v_out = new Vertex2D(v1.P1, v2.P2, v1.Orientation);
                    }
                    break;
                }
        }


        return v_out;
    }
    public Vertex2D DeductVertices(Vertex2D v1, Vertex2D v2) //(existing, removing) //returns either shorter vertex v1 or Null, v2 gets removed from v1
    {
        Vertex2D v_out = new Vertex2D(new Point2D(0, 0, 0), new Point2D(0, 0, 0), v1.Orientation); //dummy rn
        if (v1.Orientation != v2.Orientation) { Errorlog.Add($"Deduct Error between: {v1} and {v2} due to orientation unmatch"); }
        switch (v1.Orientation)
        {

            case "Vertical":
                {
                    if (v1.P2.Y < v2.P1.Y) { Errorlog.Add($"Deduct Error between: {v1} and {v2} due to noncoincidence"); break; } //break out of the switch due to the error
                    else if ((v1.P1.Y == v2.P1.Y) && (v1.P2.Y == v2.P2.Y) && (v1.P1.X == v2.P1.X) && (v1.P2.X == v2.P2.X))
                    {
                        v1.Exposedsection = new Tuple<Point2D, Point2D>(new Point2D(0, 0, 0), new Point2D(0, 0, 0)); verticestoconsider.Remove(v1);
                    }
                    if (v1.P1.Y > v2.P1.Y) //if order is wrong it is corrected
                    {
                        Vertex2D temp = v1;
                        v1 = v2;
                        v2 = temp;
                    }
                    else if ((v1.P2.Y > v2.P2.Y) && (v1.P1.Y < v2.P1.Y)) //if v1 fully overcovers v2, meaning two new vertices will be created
                    {
                        v_out = new Vertex2D(v1.P1, v2.P1, v1.Orientation); //returns first half
                        if (v2.P2.Y != v1.P2.Y) { verticestoconsider.Add(new Vertex2D(v2.P2, v1.P2, v1.Orientation)); } //add second half already
                    }

                    else if ((v1.P2.Y >= v2.P2.Y) && (v1.P1.Y == v2.P1.Y)) //they start the same but v1 is longer. shorter end vertex is returned
                    {
                        v_out = new Vertex2D(v2.P2, v1.P2, v1.Orientation);
                    }
                    else if ((v1.P2.Y <= v2.P2.Y) && (v1.P1.Y == v2.P1.Y)) //they start the same but v2 is longer. shorter end vertex is returned
                    {
                        v_out = new Vertex2D(v1.P2, v2.P2, v1.Orientation);
                    }
                    else if ((v1.P1.Y < v2.P2.Y) && (v1.P2.Y <= v2.P2.Y))//they end the same so a shorter start vertex is returned
                    {
                        v_out = new Vertex2D(v1.P1, v2.P1, v1.Orientation);
                    }
                    break;
                }




            case "Horizontal":
                {
                    if (v1.P2.X < v2.P1.X) { Errorlog.Add($"Deduct Error between: {v1} and {v2} due to noncoincidence"); break; } //break out of the switch due to the error
                    else if ((v1.P1.Y == v2.P1.Y) && (v1.P2.Y == v2.P2.Y) && (v1.P1.X == v2.P1.X) && (v1.P2.X == v2.P2.X))
                    {
                        v1.Exposedsection = new Tuple<Point2D, Point2D>(new Point2D(0, 0, 0), new Point2D(0, 0, 0)); verticestoconsider.Remove(v1);
                    }
                    if (v1.P1.X > v2.P1.X) //if order is wrong it is corrected
                    {
                        Vertex2D temp = v1;
                        v1 = v2;
                        v2 = temp;
                    }
                    else if ((v1.P2.X > v2.P2.X) && (v1.P1.X < v2.P1.X)) //if v1 fullve overcovers v2, meaning two new vertices will be created
                    {
                        v_out = new Vertex2D(v1.P1, v2.P1, v1.Orientation); //returns first half
                        if (v2.P2.X != v1.P2.X) { verticestoconsider.Add(new Vertex2D(v2.P2, v1.P2, v1.Orientation)); } //add second half already
                    }

                    else if ((v1.P2.X >= v2.P2.X) && (v1.P1.X == v2.P1.X)) //they start the same but v1 is longer. shorter end vertex is returned
                    {
                        v_out = new Vertex2D(v2.P2, v1.P2, v1.Orientation);
                    }
                    else if ((v1.P2.X <= v2.P2.X) && (v1.P1.X == v2.P1.X)) //they start the same but v2 is longer. shorter end vertex is returned
                    {
                        v_out = new Vertex2D(v1.P2, v2.P2, v1.Orientation);
                    }
                    else if ((v1.P1.X < v2.P2.X) && (v1.P2.X <= v2.P2.X))//they end the same so a shorter start vertex is returned
                    {
                        v_out = new Vertex2D(v1.P1, v2.P1, v1.Orientation);
                    }
                    break;
                }
        }





        return v_out;
    }
    public ExtremePoint ShadowPoint(Package2D p, ExtremePoint chosen, string caseorientation)
    {
        double shadowsearchdim = Chosen_maxdim * Shadowsearchmultiplier;
        ExtremePoint Epoint = new ExtremePoint(Chosen_maxdim, 0, 0, 1000); //dummy
        switch (caseorientation)
        {
            case "Vertical": //dropping down
                {

                    Vertex2D v2 = p.Vertixes.Where(x => x.ID == "v2").ToList()[0];
                    Point2D P2 = v2.P1;
                    if (P2.Y - Chosen_maxdim < 0) //under the borders
                    { v2 = new Vertex2D(new Point2D(P2.X, 0, 2), v2.P2, v2.Orientation); }
                    else //not under the borders
                    { v2 = new Vertex2D(new Point2D(P2.X, (int)(P2.Y - shadowsearchdim), 2), v2.P2, v2.Orientation); }
                    Vertex2D nearestperpendicular = Fetchcrossover_vertex_nearest(verticestoconsider, v2, P2);
                    if ((nearestperpendicular.P1.Index == 0 && nearestperpendicular.P2.Index == 0) || (nearestperpendicular.P2.Y == P2.Y)) { break; }
                    Epoint = new ExtremePoint(Chosen_maxdim, P2.X, nearestperpendicular.P1.Y, P2.Index);
                    Epoint.Identifier = "Shadow";
                    Epoint.Shadowextension_Orientation = "Vertical";

                    break;
                }
            case "Horizontal":
                {
                    Vertex2D v3 = p.Vertixes.Where(x => x.ID == "v3").ToList()[0];
                    Point2D P1 = v3.P1;
                    if (P1.X - Chosen_maxdim < 0) //under the borders
                    { v3 = new Vertex2D(new Point2D(0, P1.Y, 2), v3.P2, v3.Orientation); }
                    else //not under the borders
                    { v3 = new Vertex2D(new Point2D((int)(P1.X - shadowsearchdim), P1.Y, 3), v3.P2, v3.Orientation); }
                    Vertex2D nearestperpendicular = Fetchcrossover_vertex_nearest(verticestoconsider, v3, P1);
                    if ((nearestperpendicular.P1.Index == 0 && nearestperpendicular.P2.Index == 0) || (nearestperpendicular.P2.X == P1.X)) { break; }
                    Epoint = new ExtremePoint(Chosen_maxdim, nearestperpendicular.P1.X, P1.Y, P1.Index);
                    Epoint.Identifier = "Shadow";
                    Epoint.Shadowextension_Orientation = "Horizontal";
                    break;
                }
        }

        Epoint.Original_Point = chosen.Original_Point;
        bool testout = Simple_Overlapcheck(Epoint);
        if (!testout) { Epoint = new ExtremePoint(Chosen_maxdim, 0, 0, 1000); }
        //else
        //{
        //    Vertex2D virtualnew = new Vertex2D(Epoint, Epoint.Original_Point, Epoint.Shadowextension_Orientation);
        //    Virtual_Vertices.Add(virtualnew);
        //}
        return Epoint;
    }

    public double Fitness(Package2D p, ExtremePoint chosen)
    {

        double output = 0;
        double a1 = A1; double a2 = A2; double a3 = A3; double a4 = A4; double beta = Beta; double gamma = Gamma; // 8;4;2;4;30;30 
        double priority = p.Priority; //if the package is larger than width of bin
        double overlaps = 0; double heightvalue = 0; double penalties = 0; double rewards = 0; bool rewarding = R; bool penalizing = true;
        bool volumeuse = VolumeUse;
        Vertex2D v1 = p.Vertixes.Where(x => x.ID == "v1").ToList()[0];
        Vertex2D v2 = p.Vertixes.Where(x => x.ID == "v2").ToList()[0];
        Vertex2D v3 = p.Vertixes.Where(x => x.ID == "v3").ToList()[0];
        Vertex2D v4 = p.Vertixes.Where(x => x.ID == "v4").ToList()[0];
        //OVERLAPS // left side has been fully covered
        {
            double bottom = 0;
            List<Vertex2D> bottomlist = Fetchcollinear_vertices(chosen.Spatial_Vertices, v1, v1.Orientation);//    incident_vertices(chosen.Spatial_Vertices, v4.P2, v3.Orientation).ToList();
            if (bottomlist.Count > 0)
            {
                foreach (Vertex2D b in bottomlist)
                {

                    if (!((b.P1.X >= v1.P2.X) || (b.P2.X <= v1.P1.X)))
                    {
                        if ((b.P1.X <= v1.P1.X) && (b.P2.X >= v1.P2.X)) // much larger, v1 is the fitness
                        { bottom += a1 * (v1.Length) / Chosen_maxdim; }
                        else if ((b.P1.X < v1.P1.X) && (b.P2.X <= v1.P2.X)) //coming from the left v1.p1 -c0.p2
                        { bottom += a1 * (b.P2.X - v1.P1.X) / Chosen_maxdim; }
                        else if ((b.P1.X >= v1.P1.X) && (b.P2.X >= v1.P2.X)) //in but shorter c0.p1-c0.p2
                        { bottom += a1 * (v1.P2.X - b.P1.X) / Chosen_maxdim; }
                        else if ((b.P1.X >= v1.P1.X) && (b.P2.X > v1.P2.X)) //going out from the right c0.p1-v1.p2
                        { bottom += a1 * (v1.P2.X - b.P1.X) / Chosen_maxdim; }
                    }
                    if ((b.P2.X == v1.P2.X) && (b.P1.X == v1.P1.X) && (rewarding))
                    {
                        rewards += 0.5;
                    }
                    else if ((b.P2.X == v1.P2.X) && (rewarding))
                    {
                        rewards += 0.2;
                    }
                }
            }
            overlaps += bottom;
        }

        if (a4 > 0)
        {
            double leftside = 0;
            List<Vertex2D> side = Fetchcollinear_vertices(chosen.Spatial_Vertices, v4, v4.Orientation);
            if (side.Count > 0)//&& side[0].P2.Y > v2.P1.X)
            {
                foreach (Vertex2D ls in side)
                {
                    if (!((ls.P1.Y >= v4.P2.Y) || (ls.P2.Y <= v4.P1.Y)))
                    {
                        if ((ls.P1.Y <= v4.P1.Y) && (ls.P2.Y >= v4.P2.Y)) // much larger, v4 is the fitness
                        { leftside += a4 * (v4.Length) / Chosen_maxdim; }
                        else if ((ls.P1.Y < v4.P1.Y) && (ls.P2.Y < v4.P2.Y)) //coming from the left v4.p1 -c0.p2
                        { leftside += a4 * (ls.P2.Y - v4.P1.Y) / Chosen_maxdim; }
                        else if ((ls.P1.Y >= v4.P1.Y) && (ls.P2.Y >= v4.P2.Y)) //in but shorter c0.p1-c0.p2
                        { leftside += a4 * (v4.P2.Y - ls.P1.Y) / Chosen_maxdim; }
                        else if ((ls.P1.Y > v4.P1.Y) && (ls.P2.Y > v4.P2.Y)) //going out from the right c0.p1-v4.p2
                        { leftside += a4 * (v4.P2.Y - ls.P1.Y) / Chosen_maxdim; }

                    }
                    if ((ls.P2.Y == v4.P2.Y) && (ls.P1.Y == v4.P1.Y) && (rewarding))
                    {
                        rewards += 0.5;
                    }
                    else if ((ls.P2.Y == v4.P2.Y) && (rewarding))
                    {
                        rewards += 0.2;
                    }
                }
            }
            overlaps += leftside;
        }

        if (a3 > 0)
        {
            double topside = 0;
            List<Vertex2D> ceiling = Fetchcollinear_vertices(chosen.Spatial_Vertices, v3, v3.Orientation);//    incident_vertices(chosen.Spatial_Vertices, v4.P2, v3.Orientation).ToList();

            if (ceiling.Count > 0)
            {
                foreach (Vertex2D c in ceiling)
                {
                    if (!((c.P1.X >= v3.P2.X) || (c.P2.X <= v3.P1.X)))
                    {
                        if ((c.P1.X <= v3.P1.X) && (c.P2.X >= v3.P2.X)) // much larger, v3 is the fitness
                        { topside += a3 * (v3.Length) / Chosen_maxdim; }
                        else if ((c.P1.X < v3.P1.X) && (c.P2.X < v3.P2.X)) //coming from the left v3.p1 -c0.p2
                        { topside += a3 * (c.P2.X - v3.P1.X) / Chosen_maxdim; }
                        else if ((c.P1.X >= v3.P1.X) && (c.P2.X >= v3.P2.X)) //in but shorter c0.p1-c0.p2
                        { topside += a3 * (v3.P2.X - c.P1.X) / Chosen_maxdim; }
                        else if ((c.P1.X > v3.P1.X) && (c.P2.X > v3.P2.X)) //going out from the right c0.p1-v3.p2
                        { topside += a3 * (v3.P2.X - c.P1.X) / Chosen_maxdim; }
                    }
                    if ((c.P2.X == v3.P2.X) && (c.P1.X == v3.P1.X) && (rewarding))
                    {
                        rewards += 0.2;
                    }
                    else if ((c.P2.X == v3.P2.X) && (rewarding))
                    {
                        rewards += 0.1;
                    }

                }

            }
            overlaps += topside;

        }
        if (a2 > 0)
        {
            double rightside = 0;
            List<Vertex2D> side = Fetchcollinear_vertices(chosen.Spatial_Vertices, v2, v2.Orientation);
            if (side.Count > 0)//&& side[0].P2.Y > v2.P1.X)
            {
                foreach (Vertex2D rs in side)
                {//(!((ls.P1.Y >= v4.P2.Y) || (ls.P2.Y <= v4.P1.Y)))
                    if (!((rs.P1.Y >= v2.P2.Y) || (rs.P2.Y <= v2.P1.Y)))
                    {
                        if ((rs.P1.Y <= v2.P1.Y) && (rs.P2.Y >= v2.P2.Y)) // much larger, v2 is the fitness
                        { rightside += a2 * (v2.Length) / Chosen_maxdim; }
                        else if ((rs.P1.Y < v2.P1.Y) && (rs.P2.Y < v2.P2.Y)) //coming from the left v2.p1 -c0.p2
                        { rightside += a2 * (rs.P2.Y - v2.P1.Y) / Chosen_maxdim; }
                        else if ((rs.P1.Y >= v2.P1.Y) && (rs.P2.Y >= v2.P2.Y)) //in but shorter c0.p1-c0.p2
                        { rightside += a2 * (v2.P2.Y - rs.P1.Y) / Chosen_maxdim; }
                        else if ((rs.P1.Y > v2.P1.Y) && (rs.P2.Y > v2.P2.Y)) //going out from the right c0.p1-v2.p2
                        { rightside += a2 * (v2.P2.Y - rs.P1.Y) / Chosen_maxdim; }
                    }
                    if ((rs.P2.Y == v2.P2.Y) && (rs.P1.Y == v2.P1.Y) && (rewarding))
                    {
                        rewards += 0.4;
                    }
                    else if ((rs.P2.Y == v2.P2.Y) && (rewarding))
                    {
                        rewards += 0.2;
                    }
                }
            }
            overlaps += rightside;

        }
        double raisepercentage = PenaltyCurve((chosen.Y + p.Length) / Opt);
        if ((penalizing) && ((chosen.Y + p.Length) > StripHeight)) { penalties += ((chosen.Y + p.Length) - StripHeight) * gamma * raisepercentage; }
        //HEIGHTVALUE
        List<ExtremePoint> orderedpoints = ActiveExtremePoints.OrderByDescending(x => x.Y).ToList();
        double y1 = orderedpoints.First().Y;
        double y2 = orderedpoints.Last().Y;
        heightvalue = ((y1 - chosen.Y) / (y1 - y2)) * beta;// * (p.Volume * raisepercentage); OR * p.Volume
        if (!(heightvalue > 0)) { heightvalue = 0; } //Commenting this out speeds up the process by filling bottom first
        if (volumeuse)
        {
            heightvalue *= (p.Volume);/// Largestvol) + 1;
        }
        //rewards /= (raisepercentage+0.5);
        output = (overlaps + rewards + heightvalue + priority - penalties);
        return output;
    }

    public double Fitness_simple(Package2D p, ExtremePoint chosen)
    {
        double output = 0;
        double a1 = 1; double a2 = 0; double a3 = 0; double a4 = 0.4; double beta = 3; double heightconstant = 10;
        double overlaps = 0; double heighvalue = 0; double penalties = 0; double rewards = 0;

        Vertex2D v1 = p.Vertixes.Where(x => x.ID == "v1").ToList()[0];
        Vertex2D v2 = p.Vertixes.Where(x => x.ID == "v2").ToList()[0];
        Vertex2D v3 = p.Vertixes.Where(x => x.ID == "v3").ToList()[0];
        Vertex2D v4 = p.Vertixes.Where(x => x.ID == "v4").ToList()[0];
        //OVERLAPS // left side has been fully covered
        {
            double bottom = 0;
            List<Vertex2D> bottomlist = Fetchcollinear_vertices(chosen.Spatial_Vertices, v1, v1.Orientation);//    incident_vertices(chosen.Spatial_Vertices, v4.P2, v3.Orientation).ToList();
            if (bottomlist.Count > 0)
            {
                foreach (Vertex2D b in bottomlist)
                {

                    if (!((b.P1.X >= v1.P2.X) || (b.P2.X <= v1.P1.X)))
                    {
                        if ((b.P1.X < v1.P1.X) && (b.P2.X > v1.P2.X)) // much larger, v1 is the fitness
                        { bottom += a1 * (v1.Length) / Chosen_maxdim; }
                        else if ((b.P1.X < v1.P1.X) && (b.P2.X < v1.P2.X)) //coming from the left v1.p1 -c0.p2
                        { bottom += a1 * (b.P2.X - v1.P1.X) / Chosen_maxdim; }
                        else if ((b.P1.X >= v1.P1.X) && (b.P2.X >= v1.P2.X)) //in but shorter c0.p1-c0.p2
                        { bottom += a1 * (b.P2.X - b.P1.X) / Chosen_maxdim; }
                        else if ((b.P1.X > v1.P1.X) && (b.P2.X > v1.P2.X)) //going out from the right c0.p1-v1.p2
                        { bottom += a1 * (v1.P2.X - b.P1.X) / Chosen_maxdim; }
                    }
                    if (b.P2.X == v1.P2.X)
                    {
                        //reward
                    }
                }
            }
            overlaps += bottom;
        }

        if (a4 > 0)
        {
            double leftside = 0;
            List<Vertex2D> side = Fetchcollinear_vertices(chosen.Spatial_Vertices, v4, v4.Orientation);
            if (side.Count > 0)//&& side[0].P2.Y > v2.P1.X)
            {
                foreach (Vertex2D ls in side)
                {
                    if (!((ls.P1.Y >= v4.P2.Y) || (ls.P2.Y <= v4.P1.Y)))
                    {
                        if ((ls.P1.Y < v4.P1.Y) && (ls.P2.Y > v4.P2.Y)) // much larger, v4 is the fitness
                        { leftside += a4 * (v4.Length) / Chosen_maxdim; }
                        else if ((ls.P1.Y < v4.P1.Y) && (ls.P2.Y < v4.P2.Y)) //coming from the left v4.p1 -c0.p2
                        { leftside += a4 * (ls.P2.Y - v4.P1.Y) / Chosen_maxdim; }
                        else if ((ls.P1.Y >= v4.P1.Y) && (ls.P2.Y >= v4.P2.Y)) //in but shorter c0.p1-c0.p2
                        { leftside += a4 * (ls.P2.Y - ls.P1.Y) / Chosen_maxdim; }
                        else if ((ls.P1.Y > v4.P1.Y) && (ls.P2.Y > v4.P2.Y)) //going out from the right c0.p1-v4.p2
                        { leftside += a4 * (v4.P2.Y - ls.P1.Y) / Chosen_maxdim; }

                    }
                    if (ls.P2.Y == v4.P2.Y)
                    {
                        //reward
                    }
                }
            }
            overlaps += leftside;
        }

        if (a3 > 0)
        {
            double topside = 0;
            List<Vertex2D> ceiling = Fetchcollinear_vertices(chosen.Spatial_Vertices, v3, v3.Orientation);//    incident_vertices(chosen.Spatial_Vertices, v4.P2, v3.Orientation).ToList();

            if (ceiling.Count > 0)
            {
                foreach (Vertex2D c in ceiling)
                {
                    if (!((c.P1.X >= v3.P2.X) || (c.P2.X <= v3.P1.X)))
                    {
                        if ((c.P1.X < v3.P1.X) && (c.P2.X > v3.P2.X)) // much larger, v3 is the fitness
                        { topside += a3 * (v3.Length) / Chosen_maxdim; }
                        else if ((c.P1.X < v3.P1.X) && (c.P2.X < v3.P2.X)) //coming from the left v3.p1 -c0.p2
                        { topside += a3 * (c.P2.X - v3.P1.X) / Chosen_maxdim; }
                        else if ((c.P1.X >= v3.P1.X) && (c.P2.X >= v3.P2.X)) //in but shorter c0.p1-c0.p2
                        { topside += a3 * (c.P2.X - c.P1.X) / Chosen_maxdim; }
                        else if ((c.P1.X > v3.P1.X) && (c.P2.X > v3.P2.X)) //going out from the right c0.p1-v3.p2
                        { topside += a3 * (v3.P2.X - c.P1.X) / Chosen_maxdim; }
                    }

                }

            }
            overlaps += topside;

        }
        if (a2 > 0)
        {
            double rightside = 0;
            List<Vertex2D> side = Fetchcollinear_vertices(chosen.Spatial_Vertices, v2, v2.Orientation);
            if (side.Count > 0)//&& side[0].P2.Y > v2.P1.X)
            {
                foreach (Vertex2D rs in side)
                {//(!((ls.P1.Y >= v4.P2.Y) || (ls.P2.Y <= v4.P1.Y)))
                    if (!((rs.P1.Y >= v2.P2.Y) || (rs.P2.Y <= v2.P1.Y)))
                    {
                        if ((rs.P1.Y < v2.P1.Y) && (rs.P2.Y > v2.P2.Y)) // much larger, v2 is the fitness
                        { rightside += a2 * (v2.Length) / Chosen_maxdim; }
                        else if ((rs.P1.Y < v2.P1.Y) && (rs.P2.Y < v2.P2.Y)) //coming from the left v2.p1 -c0.p2
                        { rightside += a2 * (rs.P2.Y - v2.P1.Y) / Chosen_maxdim; }
                        else if ((rs.P1.Y >= v2.P1.Y) && (rs.P2.Y >= v2.P2.Y)) //in but shorter c0.p1-c0.p2
                        { rightside += a2 * (rs.P2.Y - rs.P1.Y) / Chosen_maxdim; }
                        else if ((rs.P1.Y > v2.P1.Y) && (rs.P2.Y > v2.P2.Y)) //going out from the right c0.p1-v2.p2
                        { rightside += a2 * (v2.P2.Y - rs.P1.Y) / Chosen_maxdim; }
                    }
                }
            }
            overlaps += rightside;

        }
        if (a2 > 0 && a3 > 0) { }
        //HEIGHTVALUE
        List<ExtremePoint> orderedpoints = ActiveExtremePoints.OrderByDescending(x => x.Y).ToList();
        double y1 = orderedpoints.First().Y;
        double y2 = orderedpoints.Last().Y;
        heighvalue = (heightconstant - (1 / (y1 - y2)) * (chosen.Y - y2)) * beta;

        //Penalties


        //Rewards


        output = overlaps + rewards + heighvalue - penalties;
        return output;
    } //what do implement as obj proxy?
    public double Euclideandistance(Point2D p1, Point2D p2)
    {


        double result = Math.Sqrt(Math.Pow(p1.X - p2.X, 2) +
          Math.Pow(p1.Y - p2.Y, 2));

        return result;
    }

    public void Input_Analysis()
    {
        int mindim = int.MaxValue;
        int maxdim = 0;
        if (Input_packages.Count > 0)
        {
            foreach (Package2D p in Input_packages.ToList())
            {
                if (p.Width < mindim)
                {
                    mindim = p.Width;

                }
                if (p.Width > maxdim)
                {
                    maxdim = p.Width;
                }
                if (p.Length < mindim)
                {
                    mindim = p.Length;
                }
                if (p.Length > maxdim)
                {
                    maxdim = p.Length;
                }
            }

        }
        Chosen_maxdim = maxdim;
        Chosen_mindim = mindim;

        if (mindim == 1)
        {
            Multiplier = 2;
            foreach (Package2D p in Input_packages)
            {
                p.Width *= Multiplier; p.Length *= Multiplier;
                p.Volume *= Multiplier * Multiplier;

            }
            Bin = new Package2D(Bin.Width * Multiplier, Bin.Length * Multiplier);
            Opt *= Multiplier; Chosen_maxdim *= Multiplier; Chosen_mindim *= Multiplier;
        }

        Largestvol = Input_packages.OrderByDescending(x => x.Volume).ToList().Last().Volume;

        return;
    }

    public double PercentageDist(double x, double y)
    {
        return (1 - (2 / (2 + Math.Pow(Math.E, (-30 * x + 3.4 * Math.Pow(Math.E, 2))))));
    }
    /// <summary>
    /// input either E.Y/TotalHeight or E.Y/LB
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public double PenaltyCurve(double x)
    {
        double output = 0;
        switch (Curve)
        {
            case 0:
                {
                    output = (3 / (3 + Math.Pow(Math.E, (-12 * x + 1 * Math.Pow(Math.E, 2))))); // Softer Curve
                    break;
                }
            case 1:
                {
                    output = (3 / (3 + Math.Pow(Math.E, (-20 * x + 1 * Math.Pow(Math.E, 2.7))))); //Harder Curve
                    break;
                }
        }
        //(3 / (3 + Math.Pow(Math.E, (-12 * x + 1 * Math.Pow(Math.E, 2))))); // Softer Curve
        //(3 / (3 + Math.Pow(Math.E, (-20 * x + 1 * Math.Pow(Math.E, 2.7))))); //Harder Curve
        return output;
    }
    public double RatioDist(double x, double y)
    {
        return Math.Pow(y, 4);
    }
    public double DistributionPlane(double x, double y)
    {
        double result = RatioDist(x, y) + PercentageDist(x, y) - RatioDist(x, y) * PercentageDist(x, y);

        return result;
    }

    public void Tailflip()
    {
        int untouched = 0;
        List<Package2D> loadsofar = Load_order.ToList();
        loadsofar.Reverse();

        foreach (Package2D p in loadsofar.ToList())
        {
            if (untouched > Load_order.Count / 4) { break; }
            double randompick = rnd.NextDouble();
            double x = Load_order.IndexOf(p) / Load_order.Count - 1;
            double y = p.Width / p.Length;
            double position = DistributionPlane(x, y);

            if (randompick > position)
            {
                Package2D temp = new Package2D(p.Length, p.Width);
                temp.Indexes.Add("Instance", p.Indexes["Instance"]);
                temp.Rotationallowance["XY"] = false;
                int index = Load_order.IndexOf(p);
                Load_order.Remove(p);
                Load_order.Insert(index, temp);
            }
            else { untouched++; }

        }

        return;
    }
    public void Reset_Runs()
    {
        verticestoconsider.Clear();
        ActiveExtremePoints.Clear();
        Notallowed.Clear();
        Errorlog.Clear();
        Load_order.Clear();
        Inbetween_loadorder.Clear();
        Virtual_Vertices.Clear();
        Rules.Clear();
        StripHeight = 0;
        // rnd = new Random(rnd.Next(int.MaxValue));

        return;
    }
    public void Load_Parameters(Parameter[] parameters)
    {
        Reset_Runs();
        A1 = Convert.ToDouble(parameters[0].CurrentParval);
        A2 = Convert.ToDouble(parameters[1].CurrentParval);
        A4 = Convert.ToDouble(parameters[2].CurrentParval);
        A4 = Convert.ToDouble(parameters[3].CurrentParval);
        R = Convert.ToBoolean(parameters[4].CurrentParval);
        Opt = parameters[5].CurrentParval;

        return;
    }
    public int[] Extract_Parameters()
    {
        int rewarding = R ? 1 : 0;
        int[] parameters = new int[] { (int)A1, (int)A2, (int)A3, (int)A4, rewarding, Opt };
        return parameters;
    }
    public int RunNewPars(Parameter[] parameters)
    {
        Load_Parameters(parameters);
        Main_OffURPrep2();
        //Large_OffURPrep();
        //Main_OffUR2();
        return StripHeight;
    }

}
public class MasterRule : ICloneable
{
    public List<Point2D> Rulepoints { get; set; }
    public Point2D Center { get; set; }

    public bool TestPoints(List<Point2D> points)
    {

        bool tester = true;
        foreach (Point2D p in points)


        {
            if ((Rulepoints[0].X < p.X) && (p.X < Rulepoints[1].X)
                && (Rulepoints[1].Y < p.Y) && (p.Y < Rulepoints[2].Y))
            {
                tester = false; break;
            }

        }

        return tester;
    }
    public MasterRule(Package2D p)
    {
        Rulepoints = p.Pointslist.ToList();
        Center = new Point2D((p.Pointslist[0].X + p.Pointslist[2].X) / 2, (p.Pointslist[0].Y + p.Pointslist[2].Y) / 2, 5);
    }
    public override string ToString()
    {
        return Center.ToString();
    }
    public object Clone()
    {
        return this.MemberwiseClone();
    }
}
