using Google.OrTools.ConstraintSolver;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Masterarbeit_library2;

public class Extreme_Algorithms
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
    internal List<Vertex2D> verticestoconsider = new List<Vertex2D>();
    internal List<ExtremePoint> ActiveExtremePoints = new List<ExtremePoint>();
    internal int Chosen_maxdim { get; set; } = 100;
    public List<string> Errorlog { get; set; } = new List<string>();
    public List<Package2D> Input_packages { get; set; } = new List<Package2D>();
    public List<Package2D> Load_order { get; set; } = new List<Package2D>();
    public Dictionary<Point2D, MasterRule> Rules { get; set; } = new Dictionary<Point2D, MasterRule>();
    public Package2D Bin { get; set; } = new Package2D(200, 400); //needs to be adjusted
    public void Main_SU()
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
                //if (input.ToList().Count == 194)//&& E.X == 19 && E.Y == 75)
                //{
                //    string here = string.Empty;
                //}
                E.Create_Space(Fetchrelevant_vertices(E));
                Package2D current_pack = input.First();
                current_pack.OverwritePosition(E.X, E.Y, 1); //move package to this Extreme Point, index is handle= P1
                Package2D current_pack_rotated = new Package2D(current_pack.Length, current_pack.Width);
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
            MasterRule r2 = new MasterRule(chosenpack);
            Rules.Add(r2.Center, r2);


            if (input.ToList().Count == 150)
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


    public List<Vertex2D> Fetchrelevant_vertices(ExtremePoint E) //returns vertices inside the packing space.
    {
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
                            if ((v.P1.X < E.X && v.P2.X <= E.X) |
                             (v.P1.X >= v2.P1.X && v.P2.X > v2.P1.X)) //either too far left or too far right 
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
                            if ((v.P1.Y < E.Y && v.P2.Y <= E.Y) |
                            (v.P1.Y >= v3.P1.Y && v.P2.Y > v3.P1.Y)) //either too high or too low
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

        ActiveExtremePoints.Remove(chosen);

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

                        if (verticesofpoint.Count > 1) { Errorlog.Add($"Merge Error: too many overlapping vertixes with same direction over point {e}"); break; }
                        else if (verticesofpoint.Count == 0)
                        { //add shadows here
                            Vertex2D E_v1 = chosen.Initial_Space.Vertixes.Where(x => x.ID == "v1").ToList()[0];
                            if (E_v1.Realsection.Item2.X < v2.P1.X) //hanging
                            {
                                ExtremePoint ShadowVertical = ShadowPoint(p, chosen, v2.Orientation);
                                if (ShadowVertical.Index != 1000) { ActiveExtremePoints.Add(ShadowVertical); }
                            }
                            else if (E_v1.Realsection.Item1.X - 1 > v2.P2.X) //non overlap  (in the air)
                            {
                                ExtremePoint ShadowVertical = ShadowPoint(p, chosen, v2.Orientation);
                                if (ShadowVertical.Index != 1000) { ActiveExtremePoints.Add(ShadowVertical); }
                            }
                            ActiveExtremePoints.Add(e); verticestoconsider.Add(v2);
                        }//if not on an existing vertex add to extreme points

                        else if (verticesofpoint.Count > 0)
                        {
                            Vertex2D newvert = MergeVertices(verticesofpoint[0], v2);
                            if (newvert.P2.X == 0 && newvert.P2.Y == 0) { Errorlog.Add($"Merge Error between: {verticesofpoint[0]} and {v2}"); }
                            else if (newvert.P1.Y != newvert.P2.Y) { verticestoconsider.Remove(verticesofpoint[0]); verticestoconsider.Add(newvert); }
                        }
                        break;
                    }
                //merge v3 with the found vertex HORIZONTAL OUTPUT
                case 4:
                    {
                        verticesofpoint = Fetchcoincident_vertices(verticestoconsider, e, "Horizontal").ToList();
                        Vertex2D v3 = p.Vertixes.Where(x => x.ID == "v3").ToList()[0];
                        if (verticesofpoint.Count > 1) { Errorlog.Add($"Merge Error: too many overlapping vertixes with same direction over point {e}"); break; }
                        else if (verticesofpoint.Count == 0)
                        {
                            Vertex2D E_v4 = chosen.Initial_Space.Vertixes.Where(x => x.ID == "v4").ToList()[0];
                            if (E_v4.Realsection.Item2.Y < v3.P1.Y) //stepping up
                            {
                                ExtremePoint ShadowHorizontal = ShadowPoint(p, chosen, v3.Orientation);
                                if (ShadowHorizontal.Index != 1000) { ActiveExtremePoints.Add(ShadowHorizontal); }
                            }
                            else if (E_v4.Realsection.Item1.Y - 1 > v3.P2.Y) //non overlap  (in the air)
                            {
                                ExtremePoint ShadowHorizontal = ShadowPoint(p, chosen, v3.Orientation); //need to add method to create float extremes
                                if (ShadowHorizontal.Index != 1000) { ActiveExtremePoints.Add(ShadowHorizontal); }
                            }
                            ActiveExtremePoints.Add(e); verticestoconsider.Add(v3);
                        }//if not on an existing vertex add to extreme points
                        else if (verticesofpoint.Count > 0)
                        {
                            Vertex2D newvert = MergeVertices(verticesofpoint[0], v3);
                            if (newvert.P2.X == 0 && newvert.P2.Y == 0) { Errorlog.Add($"Merge Error between: {verticesofpoint[0]} and {v3}"); }
                            else if (newvert.P1.X != newvert.P2.X) { verticestoconsider.Remove(verticesofpoint[0]); verticestoconsider.Add(newvert); }
                        }
                        break;
                    }
            }

        }
        foreach (ExtremePoint E in ActiveExtremePoints.ToList()) //checks overlaps of Extreme points
        {
            if (E.Overlapcheck == null)
            {
                Package2D Perimeter = new Package2D(Chosen_maxdim, Chosen_maxdim);
                Perimeter.OverwritePosition(E.X - Chosen_maxdim / 2, E.Y - Chosen_maxdim / 2, 1);
                MasterRule Extreme_inside = new MasterRule(Perimeter);
                Extreme_inside.Center = E;
                E.Overlapcheck = Extreme_inside;
            }
            List<MasterRule> relevantones = Fetch_Relevant_Masterrules(E);
            if (relevantones.Count > 0)
            {
                bool fitsmaster = true;
                List<Point2D> inputofrule = new List<Point2D> { E, new Point2D(E.X + 1, E.Y + 1, 0) };
                foreach (MasterRule rule in relevantones)
                {
                    fitsmaster = rule.TestPoints(inputofrule);
                    if (fitsmaster == false) { fitsmaster = false; break; }
                }
                if (fitsmaster == false) { ActiveExtremePoints.Remove(E); }
            }


        }

        return;
    }
    public void Refresh_Vertices(Package2D p, ExtremePoint chosen) //after packing
    {   //overlapping vertices must be merged (v2,v3) done
        Vertex2D v1 = p.Vertixes.Where(x => x.ID == "v1").ToList()[0];
        Vertex2D E_v1 = chosen.Initial_Space.Vertixes.Where(x => x.ID == "v1").ToList()[0];
        if (E_v1.Realsection.Item1.Index != 0 | E_v1.Realsection.Item2.Index != 0)
        {
            Vertex2D E_v1_real = new Vertex2D(E_v1.Realsection.Item1, E_v1.Realsection.Item2, E_v1.Orientation); //this is virtual
            List<Vertex2D> Contains_v1 = Fetchcollinear_vertices(verticestoconsider, v1, v1.Orientation);
            if ((v1.P2.X > E_v1_real.P2.X) | ((v1.P2.X <= E_v1_real.P2.X) && (v1.P2.X >= E_v1_real.P1.X)))
            //hanging over (touching) or
            //not hanging over, v1 is shorter but still touching
            {

                if (Contains_v1.Count > 0) //should be positive if there are real vertices
                {
                    Vertex2D merged = v1;
                    foreach (Vertex2D vexists in Contains_v1.ToList())
                    {
                        merged = MergeVertices(vexists, merged); //cumulate all vertices
                        if (merged.P2.X == 0 && merged.P2.Y == 0) { continue; }
                        //else we add the shorter vertex output

                        verticestoconsider.Remove(vexists); //in any case we remove the vertex 
                    }
                    if (merged.P1.X != merged.P2.X) { verticestoconsider.Add(merged); }
                }
                else { verticestoconsider.Add(v1); }
                //v1_real should already be in the vertices somewhere

            }

            else  //not touching 
            {

                if (Contains_v1.Count > 0)
                {
                    Vertex2D merged = v1;
                    foreach (Vertex2D vexists in Contains_v1.ToList())
                    {
                        merged = MergeVertices(vexists, merged); //cumulate all vertices
                        if (merged.P2.X == 0 && merged.P2.Y == 0) { continue; }
                        //else we add the shorter vertex output

                        verticestoconsider.Remove(vexists); //in any case we remove the vertex 
                    }
                    if (merged.P1.X != merged.P2.X) { verticestoconsider.Add(merged); }
                }
                else { verticestoconsider.Add(v1); }

            }
        }
        else { verticestoconsider.Add(v1); }
        Vertex2D v4 = p.Vertixes.Where(x => x.ID == "v4").ToList()[0];
        Vertex2D E_v4 = chosen.Initial_Space.Vertixes.Where(x => x.ID == "v4").ToList()[0];
        if (E_v4.Realsection.Item1.Index != 0 | E_v4.Realsection.Item2.Index != 0)
        {
            Vertex2D E_v4_real = new Vertex2D(E_v4.Realsection.Item1, E_v4.Realsection.Item2, E_v4.Orientation);
            List<Vertex2D> Contains_v4 = Fetchcollinear_vertices(verticestoconsider, v4, v4.Orientation);
            if ((v4.P2.Y > E_v4_real.P2.Y) | ((v4.P2.Y <= E_v4_real.P2.Y) && (v1.P2.Y >= E_v4_real.P1.Y)))
            {

                if (Contains_v4.Count > 0)
                {
                    Vertex2D merged = v4;
                    foreach (Vertex2D vexists in Contains_v4.ToList())
                    {
                        merged = MergeVertices(vexists, merged);
                        if (merged.P2.X == 0 && merged.P2.Y == 0) { continue; }
                        verticestoconsider.Remove(vexists);

                    }
                    if (merged.P1.Y != merged.P2.Y) { verticestoconsider.Add(merged); }
                }

                else { verticestoconsider.Add(v4); }


            }
            else //not touching
            {

                if (Contains_v4.Count > 0)
                {
                    Vertex2D merged = v4;
                    foreach (Vertex2D vexists in Contains_v4.ToList())
                    {
                        merged = MergeVertices(vexists, merged);
                        if (merged.P2.X == 0 && merged.P2.Y == 0) { continue; }
                        verticestoconsider.Remove(vexists);

                    }
                    if (merged.P1.Y != merged.P2.Y) { verticestoconsider.Add(merged); }
                }
                else { verticestoconsider.Add(v4); }
            }
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
                    if (v1.P2.Y < v1.P1.Y) { Errorlog.Add($"Merge Error between: {v1} and {v2} due to noncoincidence"); break; } //break out of the switch due to the error
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
                    if (v1.P2.X < v1.P1.X) { Errorlog.Add($"Merge Error between: {v1} and {v2} due to noncoincidence"); break; } //break out of the switch due to the error
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
                    { v2 = new Vertex2D(new Point2D(P2.X, P2.Y - Chosen_maxdim, 2), v2.P2, v2.Orientation); }
                    Vertex2D nearestperpendicular = Fetchcrossover_vertex_nearest(verticestoconsider, v2, P2);
                    if ((nearestperpendicular.P1.Index == 0 && nearestperpendicular.P2.Index == 0) | (nearestperpendicular.P2.X == P2.X)) { break; }
                    Epoint = new ExtremePoint(Chosen_maxdim, P2.X, nearestperpendicular.P1.Y, P2.Index);
                    Epoint.Identifier = "Shadow";

                    break;
                }
            case "Horizontal":
                {
                    Vertex2D v3 = p.Vertixes.Where(x => x.ID == "v3").ToList()[0];
                    Point2D P1 = v3.P1;
                    if (P1.X - Chosen_maxdim < 0) //under the borders
                    { v3 = new Vertex2D(new Point2D(0, P1.Y, 2), v3.P2, v3.Orientation); }
                    else //not under the borders
                    { v3 = new Vertex2D(new Point2D(P1.X - Chosen_maxdim, P1.Y, 3), v3.P2, v3.Orientation); }
                    Vertex2D nearestperpendicular = Fetchcrossover_vertex_nearest(verticestoconsider, v3, P1);
                    if ((nearestperpendicular.P1.Index == 0 && nearestperpendicular.P2.Index == 0) | (nearestperpendicular.P2.Y == P1.Y)) { break; }
                    Epoint = new ExtremePoint(Chosen_maxdim, nearestperpendicular.P1.X, P1.Y, P1.Index);
                    Epoint.Identifier = "Shadow";
                    break;
                }
        }




        return Epoint;
    }

    public double Fitness(Package2D p, ExtremePoint chosen)
    {
        double output = 0;
        double a1 = 1.2; double a2 = 0; double a3 = 0; double a4 = 0.4; double beta = 0.6;
        double overlaps = 0; double heighvalue = 0; double penalties = 0; double rewards = 0;

        Vertex2D v1 = p.Vertixes.Where(x => x.ID == "v1").ToList()[0];
        Vertex2D E_v1 = chosen.Initial_Space.Vertixes.Where(x => x.ID == "v1").ToList()[0];
        Vertex2D v2 = p.Vertixes.Where(x => x.ID == "v2").ToList()[0];
        Vertex2D E_v2 = chosen.Initial_Space.Vertixes.Where(x => x.ID == "v2").ToList()[0];
        Vertex2D v3 = p.Vertixes.Where(x => x.ID == "v3").ToList()[0];
        Vertex2D E_v3 = chosen.Initial_Space.Vertixes.Where(x => x.ID == "v3").ToList()[0];
        Vertex2D v4 = p.Vertixes.Where(x => x.ID == "v4").ToList()[0];
        Vertex2D E_v4 = chosen.Initial_Space.Vertixes.Where(x => x.ID == "v4").ToList()[0];

        //OVERLAPS
        if (E_v1.Realsection.Item2.X + E_v1.Realsection.Item2.Y != 0)
        {
            double bottom = 0;
            if (v1.P2.X < E_v1.Realsection.Item1.X) { bottom = 0; }
            if ((v1.P2.X < E_v1.Realsection.Item2.X) && (v1.P2.X >= E_v1.Realsection.Item1.X) && (v1.P1.X < E_v1.Realsection.Item1.X))
            {
                bottom = a1 * (2 * (v1.P2.X - E_v1.Realsection.Item1.X)) / ((E_v1.Realsection.Item2.X - E_v1.Realsection.Item1.X) + E_v1.Length);
                if (v1.P2.X == E_v1.Realsection.Item2.X)
                { //reward and try overrite E_v2 
                    List<Vertex2D> rightside = Fetchcoincident_vertices(chosen.Spatial_Vertices, v1.P2, v2.Orientation).ToList();
                    if(rightside.Count>0 && rightside[0].P1.Y > v1.P2.Y) { E_v2 = rightside[0]; E_v2.Realsection = new Tuple<Point2D, Point2D>(E_v2.P1, E_v2.P2); E_v2.Length = (double)E_v4.Length; a2 = 1; }
                }
            }
            else if ((v1.P2.X <= E_v1.Realsection.Item2.X) && (v1.P1.X >= E_v1.Realsection.Item1.X))
            {
                bottom = a1 * (2 * v1.Length) / ((E_v1.Realsection.Item2.X - E_v1.Realsection.Item1.X) + E_v1.Length);
                if (bottom == 1) { a2 = 1.5; }
            }
            else if ((v1.P2.X > E_v1.Realsection.Item2.X) && (v1.P1.X <= E_v1.Realsection.Item2.X))//package is larger than the surface under 
            { bottom = a1 * (E_v1.Realsection.Item2.X - E_v1.Realsection.Item1.X) / E_v1.Length; }
            overlaps += bottom;

        }
        if (E_v4.Realsection.Item2.X + E_v4.Realsection.Item2.Y != 0)
        {
            double leftside = 0;
            if(v4.P2.Y<E_v4.Realsection.Item1.Y) { leftside = 0; } //not reaching at all
            else if ((v4.P2.Y < E_v4.Realsection.Item2.Y) && (v4.P2.Y >= E_v4.Realsection.Item1.Y) && (v4.P1.Y < E_v4.Realsection.Item1.Y)) // #1 : if theres slight overlap, realsection far out but still touching
            {
                leftside = a4 * (2 * (v4.P2.Y - E_v4.Realsection.Item1.Y)) / ((E_v4.Realsection.Item2.Y - E_v4.Realsection.Item1.Y) + E_v4.Length);
                if (v4.P2.Y == E_v4.Realsection.Item2.Y)
                { //reward and try overrite E_v3
                    List<Vertex2D> ceiling = Fetchcoincident_vertices(chosen.Spatial_Vertices, v4.P2, v3.Orientation).ToList();
                    if (ceiling.Count > 0 && ceiling[0].P2.X > v4.P2.X) { E_v3 = ceiling[0]; E_v3.Realsection = new Tuple<Point2D, Point2D>(E_v3.P1, E_v3.P2); E_v3.Length = (double)E_v1.Length; a3 = 1; }
                }
            }
            else if ((v4.P2.Y <= E_v4.Realsection.Item2.Y) && (v4.P1.Y >= E_v4.Realsection.Item1.Y)) //#2: if theres full overlap for v4
            {
                leftside = a4 * (2 * v4.Length) / ((E_v4.Realsection.Item2.Y - E_v4.Realsection.Item1.Y) + E_v4.Length);
                if (leftside == 1) { a3 = 1.5; }
            }
            else if ((v4.P2.Y > E_v4.Realsection.Item2.Y) && (v4.P1.Y <= E_v4.Realsection.Item2.Y)) //#3: stepping up
            {
                leftside = a4 * ((E_v4.Realsection.Item2.Y - E_v4.Realsection.Item1.Y) / E_v4.Length);
            }
            overlaps += leftside;

        }
        //need to modify this overlap section to check all vertices for possible overlap 
        if (a3 > 0 && a2 ==0) // left side has been fully covered
        {
            double topside = 0;
            if (E_v3.Realsection.Item2.X + E_v3.Realsection.Item2.Y != 0)
            {
                if (v3.P2.X < E_v3.Realsection.Item1.X) { topside = 0; }
                if ((v3.P2.X < E_v3.Realsection.Item2.X) && (v3.P2.X >= E_v3.Realsection.Item1.X) && (v3.P1.X < E_v3.Realsection.Item1.X))
                {
                    topside = a3 * (2 * (v3.P2.X - E_v3.Realsection.Item1.X)) / ((E_v3.Realsection.Item2.X - E_v3.Realsection.Item1.X) + E_v3.Length);
                }
                else if ((v3.P2.X < E_v3.Realsection.Item2.X) && (v3.P1.X >= E_v3.Realsection.Item1.X))
                {
                    topside = a3 * (2 * v3.Length) / ((E_v3.Realsection.Item2.X - E_v3.Realsection.Item1.X) + E_v3.Length);

                }
                else if ((v3.P2.X > E_v3.Realsection.Item2.X) && (v3.P1.X <= E_v3.Realsection.Item2.X))
                {
                    topside = a3 * (E_v3.Realsection.Item2.X - E_v3.Realsection.Item1.X) / E_v3.Length;
                }
                overlaps += topside;
            }

        }
        if (a2 > 0 && a3 ==0) // bottom has been fully covered
        {
            double rightside = 0;
            if (v2.P2.Y < E_v2.Realsection.Item1.Y) { rightside = 0; }
            if (E_v2.Realsection.Item2.X + E_v2.Realsection.Item2.Y != 0)
            {
                if ((v2.P2.Y < E_v2.Realsection.Item2.Y) && (v2.P2.Y >= E_v2.Realsection.Item1.Y) && (v2.P1.Y < E_v2.Realsection.Item1.Y)) //if theres slight overlap, realsection far out but still touching
                {
                    rightside = a2 * (2 * (v2.P2.Y - E_v2.Realsection.Item1.Y)) / ((E_v2.Realsection.Item2.Y - E_v2.Realsection.Item1.Y) + E_v2.Length);
                }
                else if ((v2.P2.Y < E_v2.Realsection.Item2.Y) && (v2.P1.Y >= E_v2.Realsection.Item1.Y)) //if theres full overlap for v4
                {
                    rightside = a2 * (2 * v2.Length) / ((E_v2.Realsection.Item2.Y - E_v2.Realsection.Item1.Y) + E_v2.Length);
                    
                }
                else if ((v2.P2.Y > E_v2.Realsection.Item2.Y) && (v2.P1.Y <= E_v2.Realsection.Item2.Y))
                {
                    rightside = a2 * (E_v2.Realsection.Item2.Y - E_v2.Realsection.Item1.Y) / E_v2.Length;
                }
                overlaps += rightside;
            }

        }
        if (a2 > 0 && a3 > 0) { }
        //HEIGHTVALUE
        List<ExtremePoint> orderedpoints = ActiveExtremePoints.OrderByDescending(x => x.Y).ToList();
        double y1 = orderedpoints.First().Y;
        double y2 = orderedpoints.Last().Y;
        heighvalue = (1 / (y1 -y2)) * (chosen.Y - y2)* beta;

        //Penalties


        //Rewards


        output = overlaps + rewards- heighvalue - penalties ;
        return output;
    }
    public double Euclideandistance(Point2D p1, Point2D p2)
    {


        double result = Math.Sqrt(Math.Pow(p1.X - p2.X, 2) +
          Math.Pow(p1.Y - p2.Y, 2));

        return result;
    }





}
public class MasterRule
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
}
