﻿using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Masterarbeit_library2;
//WPF 2d packages rectangle data binding 
public class Infoclassmap2 : ClassMap<Package2D>
{
    public Infoclassmap2()
    {
        try { Map(x => x.Indexes["Instance"].ToString()).Name("ID"); } catch { }
        Map(x => x.Pointslist[0].CSVFormat()).Name("P1");
        Map(x => x.Pointslist[1].CSVFormat()).Name("P2");
        Map(x => x.Pointslist[2].CSVFormat()).Name("P3");
        Map(x => x.Pointslist[3].CSVFormat()).Name("P4");
    }
}
public class Package2D : ICloneable
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



    public int Length { get; set; } //x
    public int LengthOG { get; set; } //OG refers to original for resetting
    public int Width { get; set; } //y
    public int WidthOG { get; set; }
    public int Largestdim { get; set; }
    public int Priority { get; set; } = 0;
    public int Perimeter { get; set; }
    public double Weight { get; set; }
    public string Size { get; set; }

    public Dictionary<string, int> Indexes { get; set; } = new Dictionary<string, int>(); //index in tsp input, index in route, index in load order, index in section etc. 

    public bool IsLoaded { get; set; }

    public string Error { get; set; }

    public int Volume { get; set; }

    public Dictionary<string, bool> Rotationallowance { get; set; } = new Dictionary<string, bool>(); // XY 

    public List<Point2D> Pointslist { get; set; } = new List<Point2D>();
    public List<Vertex2D> Vertixes { get; set; } = new List<Vertex2D>();
    public Package2D(int width, int length)
    {    //    4-------v3-------3
         //    |                |
         //    |                |
         //    v4               v2
         //    |                |
         //    |                | 
         //    1-------v1-------2

        // length is Y
        // width  is X
        // all vertices start with P1 and end with P2, dimensions increase so P1<P2 on the relevant axis
        Length = length;
        LengthOG = length;
        Width = width;
        WidthOG = width;
        Volume = LengthOG * WidthOG;
        Largestdim = (LengthOG > WidthOG ? LengthOG : WidthOG);
        Perimeter = (LengthOG * 2) + (WidthOG* 2);

        Pointslist = new List<Point2D>();
        if (length > 0 && width > 0)
        {
            //Volume = length * width;
            int startingindex = 1;
            while (Pointslist.Count < 4)
            {

                Point2D p = new Point2D(0, 0, startingindex); Pointslist.Add(p);
                startingindex++;


            }
            List<Vertex2D> verts = new List<Vertex2D>();
            for (int i = 1; i <= 4; i++)
            {
                string h1 = "Horizontal";
                string h2 = "Vertical";

                switch (i) //indicate also direction as a vector for later calculation using vertices
                {
                    case 1:
                        {
                            Vertex2D v1 = new Vertex2D(Pointslist[i - 1], Pointslist[i], h1); // 1-2
                            Vertex2D v4 = new Vertex2D(Pointslist[i - 1], Pointslist[i + 2], h2); //1-4
                            v1.ID = "v1";
                            v4.ID = "v4";


                            verts.Add(v1); verts.Add(v4);
                            break;
                        }
                    case 2:
                        {
                            Vertex2D v2 = new Vertex2D(Pointslist[i - 1], Pointslist[i], h2); // 2-3
                            v2.ID = "v2";
                            verts.Add(v2);
                            break;
                        }
                    case 3:
                        {
                            Vertex2D v3 = new Vertex2D(Pointslist[i], Pointslist[i - 1], h1); // 4-3 vector
                            v3.ID = "v3";
                            verts.Add(v3);
                            break;
                        }
                    case 4:
                        {
                            break;
                        }

                }


            }
            foreach (Vertex2D v in verts)
            {
                if (v.Orientation == "Horizontal") { v.Length = Width; }
                else if (v.Orientation == "Vertical") { v.Length = Length; }
            }
            Vertixes = verts;


            Rotationallowance.Add("XY", true);

        }
        //this.ToString();
    }

    public override string ToString()
    {
        return $"L: {Length.ToString().PadRight(4)}; W: {Width.ToString().PadRight(4)}; ID: {Indexes["Instance"].ToString().PadRight(5)} Loaded: {IsLoaded.ToString().PadRight(5)}";
    }
    public object Clone()
    {
        Package2D output = new Package2D(this.Width, this.Length);
        output.Indexes.Add(this.Indexes.First().Key, this.Indexes.First().Value);
        return output;
    }

    //Methods related to packages
    public void Rotationallowancedisplay()
    {
        foreach (var v in Rotationallowance)
        {
            Console.WriteLine($"{v.Key}: {v.Value}");
        }

    }
    public String SeparatedFormat(char Separator)
    {
        string output = $"{Length + Separator + Width + Separator + Indexes["Instance"]}"; //index given in the literature instance
        return output;
    }
    public void OverwritePosition(int X, int Y, int index)//assign all the points coordinates with one coordinate given
    {
        int i = index;
        int p1 = 0;
        int p2 = 0;

        List<int> roundslist = new List<int>();
        List<Point2D> Plistcopy = Pointslist.ToList();
        if (Plistcopy[i - 1].Index == i)
        {

            Plistcopy[i - 1].X = X; Plistcopy[i - 1].Y = Y;
        }
        else
        {
            //Console.WriteLine($"index {i} does not overlap with the index of the point in plistcopy ");
        }
        List<Vertex2D> Vlistcopy = Vertixes.ToList();
        Connectionfinder(index, out p1, out p2, ref Vlistcopy, ref Plistcopy); //round 1 with the given initial index
        roundslist.Add(p1); roundslist.Add(p2);
        int p3 = 0; int p4 = 0;
        foreach (int v in roundslist)
        {
            if (v > 0)

            {
                Connectionfinder(v, out p3, out p4, ref Vlistcopy, ref Plistcopy); //round 2 with p1,p2 note that these 2 points can only find 2 new points in total (make a diagram and see)
            }


        }
        Pointslist = Plistcopy;
        Constructnewvertixes();

    }
    public void Connectionfinder(int index, out int p1, out int p2, ref List<Vertex2D> Vlistcopy, ref List<Point2D> Plistcopy) //every point is connected to three points. here we check if there are any connections to explore and points to assign. 
    {
        p1 = 0;
        p2 = 0;


        if (Vlistcopy.Count > 0 && ((p1 == 0) && (p2 == 0)))
        {
            foreach (Vertex2D v in Vlistcopy.ToList()) //check all the vertices remaining
            {
                if (v.P1.Index == index) //find out if our assignment point is sending or recieving the vector, p1 is sender p2 is receiver
                {

                    if (v.Orientation == "Vertical")//if the connection is vertical we add the height
                    {
                        Plistcopy[v.P2.Index - 1].X = Plistcopy[index - 1].X;
                        Plistcopy[v.P2.Index - 1].Y = Plistcopy[index - 1].Y + Length;


                        if (p1 == 0) { p1 = v.P2.Index; } //if there is an unassigned index left assign it
                        else if (p2 == 0) { p2 = v.P2.Index; }


                        Vlistcopy.Remove(v); //remove the connection of the two points

                    }

                    else if (v.Orientation == "Horizontal")
                    {
                        Plistcopy[v.P2.Index - 1].X = Plistcopy[index - 1].X + Width;
                        Plistcopy[v.P2.Index - 1].Y = Plistcopy[index - 1].Y;

                        if (p1 == 0) { p1 = v.P2.Index; }
                        else if (p2 == 0) { p2 = v.P2.Index; }


                        Vlistcopy.Remove(v);

                    }

                }
                else if (v.P2.Index == index)
                {

                    if (v.Orientation == "Vertical")//if the connection is vertical we add the height
                    {
                        Plistcopy[v.P1.Index - 1].X = Plistcopy[index - 1].X;
                        Plistcopy[v.P1.Index - 1].Y = Plistcopy[index - 1].Y - Length;

                        if (p1 == 0) { p1 = v.P1.Index; } //if there is an unassigned index left assign it
                        else if (p2 == 0) { p2 = v.P1.Index; }


                        Vlistcopy.Remove(v); //remove the connection of the two points

                    }

                    else if (v.Orientation == "Horizontal")
                    {
                        Plistcopy[v.P1.Index - 1].X = Plistcopy[index - 1].X - Width;
                        Plistcopy[v.P1.Index - 1].Y = Plistcopy[index - 1].Y;

                        if (p1 == 0) { p1 = v.P1.Index; }
                        else if (p2 == 0) { p2 = v.P1.Index; }


                        Vlistcopy.Remove(v);

                    }

                }
                else if (!(v.P2.Index == index) && !(v.P2.Index == index))//if a Vertex2D does not contain the point we move on. 
                {
                    continue;
                }
            }
        }
        return; //eventually in the 2nd run there should be no vertices left and all points assigned. 

    }
    public void Rotate() //note only 90 degree turns are possible, no diagonals. 
    {

        if ((Rotationallowance.ContainsKey("XY")) && (Rotationallowance["XY"] = true))
        {
            int temporary = Width;
            Width = Length;
            Length = temporary; //saved as temp variable 
            OverwritePosition(Pointslist[0].X, Pointslist[0].Y, Pointslist[0].Index); //refreshes with new dimensions
        }
        //else if (!Rotationallowance.ContainsKey("XY"))
        //{ Console.WriteLine("error while turning: rotation allowance key missing"); }



        return;
    }

    public void Constructnewvertixes()
    {
        List<Vertex2D> verts = new List<Vertex2D>();
        for (int i = 1; i <= 4; i++)
        {
            string h1 = "Horizontal";
            string h2 = "Vertical";

            switch (i) //indicate also direction as a vector for later calculation using vertices
            {
                case 1:
                    {
                        Vertex2D v1 = new Vertex2D(Pointslist[i - 1], Pointslist[i], h1); // 1-2
                        Vertex2D v4 = new Vertex2D(Pointslist[i - 1], Pointslist[i + 2], h2); //1-4
                        v1.ID = "v1";
                        v4.ID = "v4";
                        verts.Add(v1); verts.Add(v4);
                        break;
                    }
                case 2:
                    {
                        Vertex2D v2 = new Vertex2D(Pointslist[i - 1], Pointslist[i], h2); // 2-3
                        v2.ID = "v2";
                        verts.Add(v2);
                        break;
                    }
                case 3:
                    {
                        Vertex2D v3 = new Vertex2D(Pointslist[i], Pointslist[i - 1], h1); // 4-3 vector
                        v3.ID = "v3";
                        verts.Add(v3);
                        break;
                    }
                case 4:
                    {
                        break;
                    }

            }


        }
        foreach (Vertex2D v in verts)
        {
            if (v.Orientation == "Horizontal") { v.Length = Width; }
            else if (v.Orientation == "Vertical") { v.Length = Length; }
        }
        Vertixes = verts;


        return;

    }
    public void ResetDimensions()
    {
        Length = LengthOG;
        Width = WidthOG;

        return;

    }
    public void ResetPositions()
    {
        foreach (Point2D p in Pointslist)
        {
            p.X = 0; p.Y = 0;
        }
        Constructnewvertixes();
        return;

    }
}
public class ExtremePoint : Point2D , ICloneable
{
    public List<Rule> Space = new List<Rule>(); //rules created from the chosen vertices
    public List<Vertex2D> Spatial_Vertices = new List<Vertex2D>(); //chosen vertices for the space
    public Point2D Original_Point { get; set; }
    public string Shadowextension_Orientation { get; set; }
    public Package2D Initial_Space { get; set; }
    public MasterRule Overlapcheck { get; set; }
    public bool Used { get; set; } = false;
    public string Identifier { get; set; }
    public List<string> Errorlog { get; set; } = new List<string>();
    public ExtremePoint(int maxdim, int x, int y, int index) : base(x, y, index)
    {
        Package2D initial = new Package2D(maxdim, maxdim);
        initial.OverwritePosition(this.X, this.Y, 1);
        Initial_Space = initial;
        Original_Point = new Point2D(x, y, index);

    }
    public void Create_Space(List<Vertex2D> relevantvertices) //create rules for the packages, need to pass relevant vertices


    {
        Space.Clear(); Spatial_Vertices.Clear(); Spatial_Vertices.AddRange(relevantvertices);
        Extreme_Algorithms algo = new Extreme_Algorithms();
        Vertex2D v2 = Initial_Space.Vertixes.Where(x => x.ID == "v2").ToList()[0]; Space.Add(new Rule(v2, this));
        Vertex2D v3 = Initial_Space.Vertixes.Where(x => x.ID == "v3").ToList()[0]; Space.Add(new Rule(v3, this));
        if (relevantvertices.Count > 0) //there must be some vertices as we dont load in the air
        {


            foreach (Vertex2D v in relevantvertices) //note that rules omit collinearity on bottom and left
            {
                switch (v.Orientation)
                {
                    case "Vertical":
                        {
                            if (v.P1.X == 0 && v.P1.Y == 0) { break; }
                            else if ((v.P1.X != this.X) && (v.P2.Y != this.Y)) 
                            {
                                Rule r = new Rule(v, this);
                                Space.Add(r);
                            }

                            else if ((v.P1.X == this.X) && (v.ID == "v4"))
                            {
                                if (v.P1.X == 0 && v.P2.Y == 0) { break; }
                                Rule r = new Rule(v, this);
                                Space.Add(r);

                            }

                            break;
                        }
                    case "Horizontal":
                        {
                            if (v.P1.X == 0 && v.P1.Y == 0) { break; }
                            else if ((v.P1.Y != this.Y) && (v.P2.X != this.X))
                            {
                                Rule r = new Rule(v, this);
                                Space.Add(r);
                            }
                            else if ((v.P1.Y == this.Y) && (v.ID == "v1"))
                            {   if(v.P1.X == 0 && v.P2.Y == 0){ break; }
                                Rule r = new Rule(v, this);
                                Space.Add(r);
                            }
                            break;
                        }

                }

            }
          

            
        }
  

        return;
    }
    public bool Fitsinspace1(List<Point2D> points) //pass all points of package to see if they fit
                                                   //with max value this is useless.
                                                   //can be used as trigger to expand space if another dimension is chosen
    {
        bool fits = true;
        foreach (Point2D p in points)
        {
            Vertex2D v2 = Initial_Space.Vertixes.Where(x => x.ID == "v2").ToList()[0];
            Vertex2D v3 = Initial_Space.Vertixes.Where(x => x.ID == "v3").ToList()[0];
            if (p.X > v2.P1.X) { fits = false; }
            if (p.Y > v3.P1.Y) { fits = false; }
        }
        return fits;
    }
    public bool Fitsinspace2(List<Point2D> points) //pass all points of package to see if they fit
    {
        bool fits = true;
        if (Space.Count > 0)
        {
            foreach (Rule r in Space)
            {
                bool loopover = r.TestPoints(points);
                if (loopover == false)
                {
                    Errorlog.Add("this rule was the breaking point" + r); fits = false; break;
                }
            }
        }
        return fits;
    }
    public object Clone()
    {
        return this.MemberwiseClone();
    }
   
}

public class Rule : ICloneable
{
    public Vertex2D Rulevertex { get; set; }
    public ExtremePoint RulePoint { get; set; }
    public bool TestPoints(List<Point2D> points)
    {
        bool tester = true;
        foreach (Point2D p in points)

        {
            if (!(p.X == RulePoint.X && p.Y == RulePoint.Y)) //if its not the handle
            {
                switch (Rulevertex.Orientation)
                {
                    case "Horizontal":
                        {
                            if (((p.Index == 2) | (p.Index == 3)) &&(Rulevertex.P1.X == p.X)){ break; }
                            else if ((p.Index == 4) && (Rulevertex.P2.X == p.X)) { break; }
                            else if ((Rulevertex.P1.X <= p.X && p.X <= Rulevertex.P2.X) |
                                (Rulevertex.P1.X >= p.X && p.X >= Rulevertex.P2.X))
                            {
                                if (Rulevertex.P1.Y < p.Y) //if point is outside
                                {
                                    tester = false;
                                }
                            }


                            break;
                        }
                    case "Vertical":
                        {
                            if ((p.Index == 2) && (Rulevertex.P2.Y == p.Y)) { break; }
                            else if (((p.Index == 4) | (p.Index == 3)) && (Rulevertex.P1.Y == p.Y)) { break; }
                            else if ((Rulevertex.P1.Y <= p.Y && p.Y <= Rulevertex.P2.Y) |
                                (Rulevertex.P1.Y >= p.Y && p.Y >= Rulevertex.P2.Y))
                            {
                                if (Rulevertex.P1.X < p.X) //if point is outside
                                {
                                    tester = false;
                                }
                            }


                            break;
                        }
                }
                if (!tester) { break; }
            }

        }

        return tester;
    }
    public Rule(Vertex2D v, ExtremePoint p)
    {
        Rulevertex = v;
        RulePoint = p;
    }
    public override string ToString()
    {
        return this.Rulevertex.ToString();
    }
    public object Clone()
    {
        return this.MemberwiseClone();
    }
}

public class Point2D : ICloneable
{

    public int X { get; set; }
    public int Y { get; set; }

    public int Index { get; set; }


    public Point2D(int x, int y, int index)
    {
        X = x;
        Y = y;

        Index = index;


    }
    public override string ToString()
    {
        return $"X: {X.ToString().PadLeft(3)} Y: {Y.ToString().PadLeft(3)} Index: P{Index}";
    }
    public string CSVFormat()
    {
        return $"({X}/{Y})";
    }
    public object Clone()
    {
        Point2D output = new Point2D(this.X, this.Y, this.Index);
        return output;
    }
}


public class Vertex2D : ICloneable
{
    //if horizontal p1.x < p2.x
    //if vertical p1.y < p2.y
    //usually has id to indicate which vertex it represented in a package, but for future sorting can be changed

    public Point2D P1 { get; set; }
    public Point2D P2 { get; set; }

    
    public Tuple<Point2D, Point2D> Exposedsection { get; set; }
    public double Length { get; set; }
    public string Orientation { get; set; }
    public string ID { get; set; }
    public Vertex2D(Point2D p1, Point2D p2, string orientation)
    {
        P1 = p1;
        P2 = p2;
        Orientation = orientation;

    }
    public override string ToString()
    {
        return $"P1: {P1.X}/{P1.Y}; P2: {P2.X}/{P2.Y}; Length: {Length}; Orientation: {Orientation}; ID: {ID}";
    }
    public object Clone()
    {
        Vertex2D Output = new Vertex2D(this.P1, this.P2, this.Orientation);
        return Output;
    }

}





