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
    internal List<Vertex2D> verticestoconsider = new List<Vertex2D>();





    private List<Vertex2D> Fetchrelevant_vertices(ExtremePoint E)
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
                        if (((v.P1.Y >= E.Y) && (v.P1.Y <= v3.P1.Y)) && //within the vertical bounds
                        ((v.P1.X <= E.X && v.P2.X <= E.X) |
                        (v.P1.X >= v2.P1.X && v.P2.X >= v2.P1.X))) //either too far left or too far right 
                        { continue; }                                //(((v.P1.X> E.X) &&( v2.P1.X>v.P2.X))| //both sides in
                                                                     //((v.P1.X < v2.P1.X )&& (v2.P1.X<v.P2.X)) |//left side is in
                                                                     //((v.P1.X < E.X) && (v.P2.X > E.X) && (v2.P1.X > v.P2.X))) //right side in

                        else if ((v.P1.Y >= E.Y) && (v.P1.Y <= v3.P1.Y))//within the vertical bounds
                        { foundvertices.Add(v); }
                        break;
                    }
                case "Vertical":
                    {
                        if (((v.P1.X >= E.X) && (v.P1.X <= v2.P1.X)) && //within the horizontal bounds
                        ((v.P1.Y <= E.Y && v.P2.Y <= E.Y) |
                        (v.P1.Y >= v3.P1.Y && v.P2.Y >= v3.P1.Y))) //either too high or too low
                        { continue; }

                        else if ((v.P1.X >= E.X) && (v.P1.X <= v2.P1.X))//within the horizontal bounds
                        { foundvertices.Add(v); }



                        break;

                    }


            }

        }



        return foundvertices;
    }










}
