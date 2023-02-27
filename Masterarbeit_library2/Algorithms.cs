using Google.OrTools.ConstraintSolver;
using System;
using System.Collections.Generic;
using System.Linq;
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
    internal int Chosen_maxdim { get; set; }
    public List<string> Errorlog { get; set; } = new List<string>();



    private List<Vertex2D> Fetchrelevant_vertices(ExtremePoint E) //returns vertices inside the packing space.
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
    public List<Vertex2D> Fetchcollinear_vertices(List<Vertex2D> verticestoconsider, Vertex2D vertexin, string orientation) //returns vertices that touch each other and have the same direction
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
                        if (v.P1.Y == vertexin.P1.Y)
                        { //within the vertical bounds
                            if ((v.P1.Y < vertexin.P1.Y && v.P2.Y < vertexin.P1.Y) |
                             (v.P1.Y > vertexin.P2.Y && v.P2.Y > vertexin.P2.Y)) //either too far left or too far right 
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
                            if ((v.P1.X < vertexin.P1.X && v.P2.X < vertexin.P1.X) |
                             (v.P1.X > vertexin.P2.X && v.P2.X > vertexin.P2.X)) //either too far left or too far right 
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
    public void Refresh_ExtremePoints(Package2D p, ExtremePoint chosen)
    //first refresh then load as it can cause confusion with vertices
    {

        ActiveExtremePoints.Remove(chosen);

        var E_toappend = from point in p.Pointslist //fetch the 2nd and 4th points
                         where point.Index == 2
                         where point.Index == 4
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
            {   //merge v2 with the found vertex
                case 2:
                    {
                        verticesofpoint = Fetchcoincident_vertices(verticestoconsider, e, "Vertical").ToList();
                        Vertex2D v2 = p.Vertixes.Where(x => x.ID == "v2").ToList()[0];
                        if (verticesofpoint.Count > 1) { Errorlog.Add($"Merge Error: too many overlapping vertixes with same direction over point {e}"); break; }
                        else if (verticesofpoint.Count == 0) { ActiveExtremePoints.Add(e); verticestoconsider.Add(v2); }//if not on an existing vertex add to extreme points


                        Vertex2D newvert = MergeVertices(verticesofpoint[0], v2);
                        if (newvert.P2.X == 0 && newvert.P2.Y == 0) { Errorlog.Add($"Merge Error between: {verticesofpoint[0]} and {v2}"); }
                        else { verticestoconsider.Add(newvert); }
                        break;
                    }
                //merge v3 with the found vertex
                case 4:
                    {
                        verticesofpoint = Fetchcoincident_vertices(verticestoconsider, e, "Horizontal").ToList();
                        Vertex2D v3 = p.Vertixes.Where(x => x.ID == "v3").ToList()[0];
                        if (verticesofpoint.Count > 1) { Errorlog.Add($"Merge Error: too many overlapping vertixes with same direction over point {e}"); break; }
                        else if (verticesofpoint.Count == 0) { ActiveExtremePoints.Add(e); verticestoconsider.Add(v3); }//if not on an existing vertex add to extreme points

                        Vertex2D newvert = MergeVertices(verticesofpoint[0], v3);
                        if (newvert.P2.X == 0 && newvert.P2.Y == 0) { Errorlog.Add($"Merge Error between: {verticesofpoint[0]} and {v3}"); }
                        else { verticestoconsider.Add(newvert); }
                        break;
                    }
            }

        }

        return;
    }
    public void Refresh_Vertices(Package2D p, ExtremePoint chosen) //after packing
    {   //overlapping vertices must be merged (v2,v3) done
        Vertex2D v1 = p.Vertixes.Where(x => x.ID == "v1").ToList()[0];
        Vertex2D E_v1 = chosen.Initial_Space.Vertixes.Where(x => x.ID == "v1").ToList()[0];
        if (E_v1.Realsection.Item2.Index != 0 | E_v1.Realsection.Item2.Index != 0)
        {
            Vertex2D E_v1_real = new Vertex2D(E_v1.Realsection.Item1, E_v1.Realsection.Item2, E_v1.Orientation);

            List<Vertex2D> ContainsE_v1_real = Fetchcollinear_vertices(verticestoconsider, E_v1_real, E_v1_real.Orientation).ToList();
            if (v1.P2.X > E_v1_real.P2.X)
            {

                if (ContainsE_v1_real.Count > 0)
                {
                    foreach (Vertex2D vexists in ContainsE_v1_real.ToList())
                    {
                        Vertex2D deducted = DeductVertices(vexists, E_v1_real);
                        verticestoconsider.Remove(vexists);
                        verticestoconsider.Add(deducted);
                    }
                }
                Vertex2D hangingside = DeductVertices(v1, E_v1_real);
                verticestoconsider.Add(hangingside);


            }
        }
        else //this shouldnt even be entered as any collinear vertex should have been in the real section of the space vertex
        {
            List<Vertex2D> Contains_v1 = Fetchcollinear_vertices(verticestoconsider, v1, v1.Orientation).ToList();
            if (Contains_v1.Count > 0)
            {

            }

        }

        Vertex2D v4 = p.Vertixes.Where(x => x.ID == "v4").ToList()[0];
        Vertex2D E_v4 = chosen.Initial_Space.Vertixes.Where(x => x.ID == "v4").ToList()[0];
        Vertex2D E_v4_real = new Vertex2D(E_v4.Realsection.Item1, E_v4.Realsection.Item2, E_v4.Orientation);
        List<Vertex2D> ContainsE_v4_real = Fetchcollinear_vertices(verticestoconsider, E_v4_real, E_v4_real.Orientation).ToList();
        if (v4.P2.Y > E_v4_real.P2.Y)
        {

            if (ContainsE_v4_real.Count > 0)
            {
                foreach (Vertex2D vexists in ContainsE_v4_real.ToList())
                {
                    Vertex2D deducted = DeductVertices(vexists, E_v4_real);
                    verticestoconsider.Remove(vexists);
                    verticestoconsider.Add(deducted);
                }
            }
            Vertex2D stepup = DeductVertices(v4, E_v4_real);
            verticestoconsider.Add(stepup);
 

        }





        List<Vertex2D> relevantverticesv1 = Fetchcollinear_vertices(verticestoconsider, v1, "Vertical");
        if (relevantverticesv1.Count > 0)
        {
            foreach (Vertex2D v in relevantverticesv1)
            {
                v1 = MergeVertices(v1, v);
                verticestoconsider.Remove(v);
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
        if (v1.Orientation != v2.Orientation) { Errorlog.Add($"Merge Error between: {v1} and {v2} due to orientation unmatch"); }
        switch (v1.Orientation)
        {
            case "Vertical":
                {
                    if (v1.P1.Y > v2.P1.Y) //if order is wrong it is corrected
                    {
                        Vertex2D temp = v1;
                        v1 = v2;
                        v2 = temp;
                    }
                    if (v1.P2.Y > v2.P2.Y) //if v1 covers v2 in any case
                    {
                        v_out = v1;
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
                    if (v1.P1.X > v2.P1.X) //if order is wrong it is corrected
                    {
                        Vertex2D temp = v1;
                        v1 = v2;
                        v2 = temp;
                    }
                    if (v1.P2.X > v2.P2.X) //if v1 covers v2 in any case
                    {
                        v_out = v1;
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
    public Vertex2D DeductVertices(Vertex2D v1, Vertex2D v2) //(existing, removing) //returns either shorter vertex or Null 
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
                    else if ((v1.P2.Y > v2.P2.Y) && (v1.P1.Y < v2.P1.Y)) //if v1 fullve overcovers v2, meaning two new vertices will be created
                    {
                        v_out = new Vertex2D(v1.P1, v2.P1, v1.Orientation); //returns first half
                        verticestoconsider.Add(new Vertex2D(v2.P2, v1.P2, v1.Orientation)); //add second half already
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
                        verticestoconsider.Add(new Vertex2D(v2.P2, v1.P2, v1.Orientation)); //add second half already
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









}
