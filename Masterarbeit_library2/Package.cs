using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using CsvHelper.Configuration;

namespace Masterarbeit_library2;

//PACKAGE DIMENSIONS ARE INT!!! VOLUME IS INT

public class Infoclassmap : ClassMap<Package>
{
    public Infoclassmap()
    {   
        
        Map(x => x.Length).Name("Length");
        Map(x => x.Width).Name("Width");
        Map(x => x.Height).Name("Height");
        Map(x => x.Size).Name("Size");

    }
}
public class Package
{
    public int Length { get; set; } //x
    public int LengthOG { get; set; }
    public int Width { get; set; } //y
    public int WidthOG { get; set; }
    public int Height { get; set; } //z
    public int HeightOG { get; set; }
    public double Weight { get; set; }
    public string Size { get; set; }

    public Dictionary<string, int> Indexes { get; set; } = new Dictionary<string, int>(); //index in tsp input, index in route, index in load order, index in section etc. 

    public bool IsLoaded { get; set; }

    public string Error { get; set; }

    public int Volume { get; set; }
    public int Fragility { get; set; }

    public int Stackability { get; set; }
    public Dictionary<string, bool> Rotationallowance { get; set; } = new Dictionary<string, bool>(); //YZ , XZ, XY 

    public List<Point> Pointslist { get; set; }
    public List<Vertex> Vertixes { get; set; } = new List<Vertex>();
    public Package(int length, int width, int height)
    {
        Length = length;
        LengthOG = length;
        Width = width;
        WidthOG = width;
        Height = height;
        HeightOG = height;

        Pointslist = new List<Point>();
        if (length > 0 && width > 0 && height > 0)
        {
            Volume = length * width * height;
            int startingindex = 1;
            while (Pointslist.Count < 8)
            {

                Point p = new Point(0, 0, 0, startingindex); Pointslist.Add(p);
                startingindex++;


            }
            List<Vertex> verts = new List<Vertex>();
            for (int i = 1; i <= 8; i++)
            {
                string h1 = "HorizontalX";
                string h2 = "HorizontalY";
                string v = "Vertical";
                switch (i) //indicate also direction as a vector for later calculation using vertices
                {
                    case 1:
                        {
                            Vertex v1 = new Vertex(Pointslist[i - 1], Pointslist[i], h1); // 1-2
                            Vertex v2 = new Vertex(Pointslist[i - 1], Pointslist[i + 2], h2); //1-4
                            Vertex v3 = new Vertex(Pointslist[i - 1], Pointslist[i + 3], v); //1-5
                            verts.Add(v1); verts.Add(v2); verts.Add(v3);
                            break;
                        }
                    case 2:
                        {
                            Vertex v1 = new Vertex(Pointslist[i - 1], Pointslist[i], h2); // 2-3
                            Vertex v2 = new Vertex(Pointslist[i - 1], Pointslist[i + 3], v); //2-6
                            verts.Add(v1); verts.Add(v2);
                            break;
                        }
                    case 3:
                        {
                            Vertex v1 = new Vertex(Pointslist[i], Pointslist[i - 1], h1); // 4-3 vector
                            Vertex v2 = new Vertex(Pointslist[i - 1], Pointslist[i + 3], v); //3-7
                            verts.Add(v1); verts.Add(v2);
                            break;
                        }
                    case 4:
                        {
                            Vertex v1 = new Vertex(Pointslist[i - 1], Pointslist[i + 3], v); //4-8
                            verts.Add(v1);
                            break;
                        }
                    case 5:
                        {
                            Vertex v1 = new Vertex(Pointslist[i - 1], Pointslist[i], h1); // 5-6
                            Vertex v2 = new Vertex(Pointslist[i - 1], Pointslist[i + 2], h2); //5-8
                            verts.Add(v1); verts.Add(v2);
                            break;
                        }
                    case 6:
                        {
                            Vertex v1 = new Vertex(Pointslist[i - 1], Pointslist[i], h2); // 6-7
                            verts.Add(v1);
                            break;
                        }
                    case 7:
                        {
                            Vertex v1 = new Vertex(Pointslist[i], Pointslist[i - 1], h1); // 8-7 vector
                            verts.Add(v1);
                            break;
                        }
                    case 8:
                        {

                            break;
                        }

                }

            }
            foreach (Vertex v in verts)
            {
                if (v.Orientation == "Vertical") { v.Length = Height; }
                else if (v.Orientation == "HorizontalX") { v.Length = Length; }
                else if (v.Orientation == "HorizontalY") { v.Length = Width; }
            }
            Vertixes = verts;

        }

        Rotationallowance.Add("XZ", false);
        Rotationallowance.Add("YZ", false);
        Rotationallowance.Add("XY", false);

    }
    public override string ToString()
    {
        return $"L: {Length.ToString().PadRight(4)}; W: {Width.ToString().PadRight(4)}; H: {Height.ToString().PadRight(4)}; Loaded: {IsLoaded.ToString().PadRight(5)}; Size: {Size.ToString().PadRight(5)}";
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
        string output = $"{Length + Separator + Width + Separator + Height + Separator + Weight}";
        return output;
    }
    public void OverwritePosition(double X, double Y, double Z, int index)//assign all the points coordinates with one coordinate given
    {
        int i = index;
        int p1 = 0;
        int p2 = 0;
        int p3 = 0;
        List<int> roundslist = new List<int>();
        List<Point> Plistcopy = Pointslist.ToList();
        if (Plistcopy[i - 1].Index == i)
        {

            Plistcopy[i - 1].X = X; Plistcopy[i - 1].Y = Y; Plistcopy[i - 1].Z = Z;
        }
        else
        {
            Console.WriteLine($"index {i} does not overlap with the index of the point in plistcopy ");
        }
        List<Vertex> Vlistcopy = Vertixes;
        Connectionfinder(index, out p1, out p2, out p3, ref Vlistcopy, ref Plistcopy); //round 1 with the given initial index
        roundslist.Add(p1); roundslist.Add(p2); roundslist.Add(p3);
        int p4 = 0; int p5 = 0; int p6 = 0; int p7;
        foreach (int v in roundslist)
        {
            if (v > 0)

            {
                Connectionfinder(v, out p4, out p5, out p6, ref Vlistcopy, ref Plistcopy); //round 2 with p1,p2,p3, note that these 3 points can only find 3 new points in total (make a diagram and see)
            }


        }//when this is finished there are only 3 vertices left leading the opposite corner point of index input
        Connectionfinder(p4, out p7, out p7, out p7, ref Vlistcopy, ref Plistcopy); //the final point is assigned
        Pointslist = Plistcopy;
        Constructnewvertixes();
        
    }
    public void Connectionfinder(int index, out int p1, out int p2, out int p3, ref List<Vertex> Vlistcopy, ref List<Point> Plistcopy) //every point is connected to three points. here we check if there are any connections to explore and points to assign. 
    {
        p1 = 0;
        p2 = 0;
        p3 = 0;

        if (Vlistcopy.Count > 0 && ((p1 == 0) && (p2 == 0) && (p3 == 0)))
        {
            foreach (Vertex v in Vlistcopy.ToList()) //check all the vertices remaining
            {
                if (v.P1.Index == index) //find out if our assignment point is sending or recieving the vector, p1 is sender p2 is receiver
                {

                    if (v.Orientation == "Vertical")//if the connection is vertical we add the height
                    {
                        Plistcopy[v.P2.Index - 1].X = Plistcopy[index - 1].X;
                        Plistcopy[v.P2.Index - 1].Y = Plistcopy[index - 1].Y;
                        Plistcopy[v.P2.Index - 1].Z = Plistcopy[index - 1].Z + Height;

                        if (p1 == 0) { p1 = v.P2.Index; } //if there is an unassigned index left assign it
                        else if (p2 == 0) { p2 = v.P2.Index; }
                        else if (p3 == 0) { p3 = v.P2.Index; }


                        Vlistcopy.Remove(v); //remove the connection of the two points

                    }
                    else if (v.Orientation == "HorizontalX")
                    {
                        Plistcopy[v.P2.Index - 1].X = Plistcopy[index - 1].X + Length;
                        Plistcopy[v.P2.Index - 1].Y = Plistcopy[index - 1].Y;
                        Plistcopy[v.P2.Index - 1].Z = Plistcopy[index - 1].Z;

                        if (p1 == 0) { p1 = v.P2.Index; }
                        else if (p2 == 0) { p2 = v.P2.Index; }
                        else if (p3 == 0) { p3 = v.P2.Index; }


                        Vlistcopy.Remove(v);

                    }
                    else if (v.Orientation == "HorizontalY")
                    {
                        Plistcopy[v.P2.Index - 1].X = Plistcopy[index - 1].X;
                        Plistcopy[v.P2.Index - 1].Y = Plistcopy[index - 1].Y + Width;
                        Plistcopy[v.P2.Index - 1].Z = Plistcopy[index - 1].Z;

                        if (p1 == 0) { p1 = v.P2.Index; }
                        else if (p2 == 0) { p2 = v.P2.Index; }
                        else if (p3 == 0) { p3 = v.P2.Index; }


                        Vlistcopy.Remove(v);

                    }

                }
                else if (v.P2.Index == index)
                {

                    if (v.Orientation == "Vertical")//if the connection is vertical we add the height
                    {
                        Plistcopy[v.P1.Index - 1].X = Plistcopy[index - 1].X;
                        Plistcopy[v.P1.Index - 1].Y = Plistcopy[index - 1].Y;
                        Plistcopy[v.P1.Index - 1].Z = Plistcopy[index - 1].Z - Height;

                        if (p1 == 0) { p1 = v.P1.Index; } //if there is an unassigned index left assign it
                        else if (p2 == 0) { p2 = v.P1.Index; }
                        else if (p3 == 0) { p3 = v.P1.Index; }


                        Vlistcopy.Remove(v); //remove the connection of the two points

                    }
                    else if (v.Orientation == "HorizontalX")
                    {
                        Plistcopy[v.P1.Index - 1].X = Plistcopy[index - 1].X - Length;
                        Plistcopy[v.P1.Index - 1].Y = Plistcopy[index - 1].Y;
                        Plistcopy[v.P1.Index - 1].Z = Plistcopy[index - 1].Z;

                        if (p1 == 0) { p1 = v.P1.Index; }
                        else if (p2 == 0) { p2 = v.P1.Index; }
                        else if (p3 == 0) { p3 = v.P1.Index; }


                        Vlistcopy.Remove(v);

                    }
                    else if (v.Orientation == "HorizontalY")
                    {
                        Plistcopy[v.P1.Index - 1].X = Plistcopy[index - 1].X;
                        Plistcopy[v.P1.Index - 1].Y = Plistcopy[index - 1].Y - Width;
                        Plistcopy[v.P1.Index - 1].Z = Plistcopy[index - 1].Z;

                        if (p1 == 0) { p1 = v.P1.Index; }
                        else if (p2 == 0) { p2 = v.P1.Index; }
                        else if (p3 == 0) { p3 = v.P1.Index; }


                        Vlistcopy.Remove(v);

                    }

                }
                else if (!(v.P2.Index == index) && !(v.P2.Index == index))//if a vertex does not contain the point we move on. 
                {
                    continue;
                }
            }
        }
        return; //eventually in the 3rd run there should be no vertices left and all points assigned. 

    }
    public void Rotate(double a1, double a2) //note only 90 degree turns are possible, no diagonals. 
    {
        if (a1 == a2)
        {
            Console.WriteLine("error while turning: input values are the same, rotation not needed");
            return;
        }
        else if ((a1 == Height && a2 == Width) | (a2 == Height && a1 == Width)) //ZY
        {
            if ((Rotationallowance.ContainsKey("YZ")) && (Rotationallowance["YZ"] = true))
            {
                int temporary = Height;
                Height = Width;
                Width = temporary; //saved as temp variable 
                OverwritePosition(Pointslist[0].X, Pointslist[0].Y, Pointslist[0].Z, Pointslist[0].Index); //refreshes with new dimensions
            }
            else if (!Rotationallowance.ContainsKey("YZ"))
            { Console.WriteLine("error while turning: rotation allowance key missing"); }

        }
        else if ((a1 == Height && a2 == Length) | (a2 == Height && a1 == Length)) //ZX
        {
            if ((Rotationallowance.ContainsKey("XZ")) && (Rotationallowance["XZ"] = true))
            {
                int temporary = Height;
                Height = Length;
                Length = temporary; //saved as temp variable 
                OverwritePosition(Pointslist[0].X, Pointslist[0].Y, Pointslist[0].Z, Pointslist[0].Index); //refreshes with new dimensions
            }
            else if (!Rotationallowance.ContainsKey("XZ"))
            { Console.WriteLine("error while turning: rotation allowance key missing"); }
        }
        else if ((a1 == Width && a2 == Length) | (a2 == Width && a1 == Length)) //XY
        {
            if ((Rotationallowance.ContainsKey("XY")) && (Rotationallowance["XY"] = true))
            {
                int temporary = Width;
                Width = Length;
                Length = temporary; //saved as temp variable 
                OverwritePosition(Pointslist[0].X, Pointslist[0].Y, Pointslist[0].Z, Pointslist[0].Index); //refreshes with new dimensions
            }
            else if (!Rotationallowance.ContainsKey("XY"))
            { Console.WriteLine("error while turning: rotation allowance key missing"); }

        }
        else if (((a1 == Height && a2 == Width) | (a2 == Height && a1 == Width)) | ((a1 == Height && a2 == Length) | (a2 == Height && a1 == Length)) | ((a1 == Width && a2 == Length) | (a2 == Width && a1 == Length)))
        {
            Console.WriteLine("error while turning: input values dont match package dimensions");
        }
        return;
    }

    public void Constructnewvertixes()
    {
        List<Vertex> verts = new List<Vertex>();
        for (int i = 1; i <= 8; i++)
        {
            string h1 = "HorizontalX";
            string h2 = "HorizontalY";
            string v = "Vertical";
            switch (i) //indicate also direction as a vector for later calculation using vertices
            {
                case 1:
                    {
                        Vertex v1 = new Vertex(Pointslist[i - 1], Pointslist[i], h1); // 1-2
                        Vertex v2 = new Vertex(Pointslist[i - 1], Pointslist[i + 2], h2); //1-4
                        Vertex v3 = new Vertex(Pointslist[i - 1], Pointslist[i + 3], v); //1-5
                        verts.Add(v1); verts.Add(v2); verts.Add(v3);
                        break;
                    }
                case 2:
                    {
                        Vertex v1 = new Vertex(Pointslist[i - 1], Pointslist[i], h2); // 2-3
                        Vertex v2 = new Vertex(Pointslist[i - 1], Pointslist[i + 3], v); //2-6
                        verts.Add(v1); verts.Add(v2);
                        break;
                    }
                case 3:
                    {
                        Vertex v1 = new Vertex(Pointslist[i], Pointslist[i - 1], h1); // 4-3 vector
                        Vertex v2 = new Vertex(Pointslist[i - 1], Pointslist[i + 3], v); //3-7
                        verts.Add(v1); verts.Add(v2);
                        break;
                    }
                case 4:
                    {
                        Vertex v1 = new Vertex(Pointslist[i - 1], Pointslist[i + 3], v); //4-8
                        verts.Add(v1);
                        break;
                    }
                case 5:
                    {
                        Vertex v1 = new Vertex(Pointslist[i - 1], Pointslist[i], h1); // 5-6
                        Vertex v2 = new Vertex(Pointslist[i - 1], Pointslist[i + 2], h2); //5-8
                        verts.Add(v1); verts.Add(v2);
                        break;
                    }
                case 6:
                    {
                        Vertex v1 = new Vertex(Pointslist[i - 1], Pointslist[i], h2); // 6-7
                        verts.Add(v1);
                        break;
                    }
                case 7:
                    {
                        Vertex v1 = new Vertex(Pointslist[i], Pointslist[i - 1], h1); // 8-7 vector
                        verts.Add(v1);
                        break;
                    }
                case 8:
                    {

                        break;
                    }

            }

        }
        foreach (Vertex v in verts)
        {
            if (v.Orientation == "Vertical") { v.Length = Height; }
            else if (v.Orientation == "HorizontalX") { v.Length = Length; }
            else if (v.Orientation == "HorizontalY") { v.Length = Width; }
        }
        Vertixes = verts;
        return;

    }
    public void ResetDimensions()
    {
        Length = LengthOG;
        Width = WidthOG;
        Height = HeightOG;
        return;

    }
}

public class Point
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public int Index { get; set; }

    public string TopOrBottom { get; set; }

    public Point(double x, double y, double z, int index)
    {
        X = x;
        Y = y;
        Z = z;
        Index = index;
        if (index <= 4) { TopOrBottom = "Bottom"; }
        else if (index <= 8 && index > 4) { TopOrBottom = "Top"; }

    }
    public override string ToString()
    {
        return $"X: {X.ToString().PadLeft(3)} Y: {Y.ToString().PadLeft(3)} Z: {Z.ToString().PadLeft(3)} Index: P{Index}";
    }

}
public class Vertex
{
    public Point P1 { get; set; }
    public Point P2 { get; set; }
    public double Length { get; set; }
    public string Orientation { get; set; }

    public Vertex(Point p1, Point p2, string orientation)
    {
        P1 = p1;
        P2 = p2;
        Orientation = orientation;

    }
    public override string ToString()
    {
        return $"P1: {P1.Index}; P2: {P2.Index}; Length: {Length}; Orientation: {Orientation}";
    }

}



