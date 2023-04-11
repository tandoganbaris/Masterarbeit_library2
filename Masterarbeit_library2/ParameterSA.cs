using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Masterarbeit_library2;

public class ParameterSA
{
    public ParameterSA() { }
    public Parameter[] initialparameters { get; set; } = new Parameter[] { };
    public int Bestval { get; set; }
    public double InitialTemp { get; set; }

    public Dictionary<int, int> Neighborhood_sofar = new Dictionary<int, int>();
    public Extreme_Algorithms Solver { get; set; }
    Random rnd = new Random();
    public ParameterSA(int[] parameters, Extreme_Algorithms algos)
    {
        Parameter a1 = new Parameter(parameters[0], algos.StripHeight, new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 5, 0.3, 2);
        Parameter a2 = new Parameter(parameters[1], algos.StripHeight, new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 5, 0.2, 2);
        Parameter a3 = new Parameter(parameters[2], algos.StripHeight, new int[] { 0, 1, 2, 3, 4, 5 }, 3, 0.1, 1);
        Parameter a4 = new Parameter(parameters[3], algos.StripHeight, new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 5, 0.2, 2);
        Parameter rewarding = new Parameter(parameters[4], algos.StripHeight, new int[] { 0, 1 }, 1, 0, 1);
        Parameter opt = new Parameter(parameters[5], algos.StripHeight, new int[] { parameters[5] - 20, parameters[5] - 10, parameters[5], parameters[5] + 10, parameters[5] + 20 }, 3, 0.2, 1);
        initialparameters = new Parameter[] { a1, a2, a3, a4, rewarding, opt };
        Bestval = algos.StripHeight;

        Solver = algos;
        Add_globalhistory(initialparameters, Bestval);

    }
    public int Par_asKey(Parameter[] parameters)
    {
        string key = string.Empty;
        foreach (Parameter p in parameters)
        {
            key.Concat(Convert.ToString(p.CurrentParval));
        }
        int parkey = int.Parse(key);

        return parkey;
    }

    public Parameter[] Key_asPar(int input, int objval)
    {
        string inputasstring = input.ToString();

        Parameter a1 = new Parameter((int)inputasstring[0], objval, new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 5, 0.3, 2); inputasstring = inputasstring.Substring(1);
        Parameter a2 = new Parameter((int)inputasstring[0], objval, new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 5, 0.2, 2); inputasstring = inputasstring.Substring(1);
        Parameter a3 = new Parameter((int)inputasstring[0], objval, new int[] { 0, 1, 2, 3, 4, 5 }, 3, 0.1, 1); inputasstring = inputasstring.Substring(1);
        Parameter a4 = new Parameter((int)inputasstring[0], objval, new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 5, 0.2, 2); inputasstring = inputasstring.Substring(1);
        Parameter rewarding = new Parameter((int)inputasstring[0], objval, new int[] { 0, 1 }, 1, 0, 1); inputasstring = inputasstring.Substring(1);
        Parameter opt = new Parameter((int)input, objval, new int[] { (int)input - 20, (int)input - 10, (int)input, (int)input + 10, (int)input + 20 }, 3, 0.2, 1);
        Parameter[] output = new Parameter[] { a1, a2, a3, a4, rewarding, opt };

        return output;

    }
    public Parameter[] Random_Par()
    {
   

        Parameter a1 = new Parameter(rnd.Next(0,10), int.MaxValue, new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 5, 0.3, 2);
        Parameter a2 = new Parameter(rnd.Next(0, 10), int.MaxValue, new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 5, 0.2, 2); 
        Parameter a3 = new Parameter(rnd.Next(0, 10), int.MaxValue, new int[] { 0, 1, 2, 3, 4, 5 }, 3, 0.1, 1); 
        Parameter a4 = new Parameter(rnd.Next(0, 10), int.MaxValue, new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 5, 0.2, 2); 
        Parameter rewarding = new Parameter(rnd.Next(0, 10), int.MaxValue,  new int[] { 0, 1 }, 1, 0, 1);
        int inputopt = initialparameters.Last().CurrentParval;
        int[] optval = new int[] { inputopt - 20, inputopt - 10, inputopt, inputopt + 10, inputopt + 20 };
        Parameter opt = new Parameter(optval[rnd.Next(0,optval.Length)], int.MaxValue, optval, 3, 0.2, 1);
        Parameter[] output = new Parameter[] { a1, a2, a3, a4, rewarding, opt };

        return output;

    }
    public void Add_globalhistory(Parameter[] parameters, int objval)
    {


        int parkey = Par_asKey(parameters.ToArray());

        Neighborhood_sofar.Add(parkey, objval);
    }

    public void SA()
    {
        int iteration = 0;
        int limit = 100;
        double Temperature = 5;
        InitialTemp = Temperature;
        double alpha = 0.99;
        Parameter[] par_internal = initialparameters.ToArray();
        int startobjval = Bestval;
        int incumbentobjval = Bestval;
        while (iteration < limit)
        {
            int incumbentobjvalcopy = (int)incumbentobjval;
            Parameter[] par_internalcopy = par_internal.ToArray();
            LS(ref par_internalcopy, ref incumbentobjvalcopy, Temperature); //do local search
            int Key_value = Par_asKey(par_internal);
            if (!Neighborhood_sofar.ContainsKey(Key_value)) { Add_globalhistory(par_internal, incumbentobjval); } //add to history 
            if (incumbentobjvalcopy < incumbentobjval)
            {
                incumbentobjval = incumbentobjvalcopy;
                par_internal = par_internalcopy;
            }
            else
            {
                double decision = rnd.NextDouble();
                double diff = Convert.ToDouble(incumbentobjvalcopy - incumbentobjval);
                double currentstate = Math.Pow(Math.E, (-diff/Temperature));
                if(decision<= currentstate)
                {
                    incumbentobjval = incumbentobjvalcopy;
                    par_internal = par_internalcopy;
                }
                else
                {
                    if(Neighborhood_sofar.Keys.Count>20)//choose random neighbor
                    { int index = rnd.Next(0, Neighborhood_sofar.Keys.Count);
                        int key = Neighborhood_sofar.ElementAt(index).Key ;
                        int val = Neighborhood_sofar.ElementAt(index).Value;
                        Parameter[] randomneighbor = Key_asPar(key, val);
                        incumbentobjval = val;
                        par_internal = randomneighbor;

                    }
                    else
                    {
                        incumbentobjval = int.MaxValue;
                        par_internal = Random_Par();

                    }
                }
            }
           
            
        }
        return;
    }
    public void LS(ref Parameter[] parameters,  ref int currentval, double Temperature)
    {
        Parameter[] par_copy = parameters.ToArray();
        int internal_Bestval = currentval;
        Parameter[] Bestarray = par_copy.ToArray();
        while (par_copy.Where(x => x.LSround_completed != true).ToArray().Length > 0)
        {
            double choice = rnd.NextDouble();
            int index = Choose(par_copy.ToArray(), choice);
            int iteration = 0;
            int maxiteration = par_copy[index].Iterations_allowed;
            int direction = rnd.Next(0, 2); //eiher 0 minus or 1 plus
            int directionchange = 0; //have we changed direction so far for the parameter in this round of local search
            int stepsize = 1; //how many steps of move within the range
            while (iteration < maxiteration)
            {

                int oldobj = par_copy[index].CurrentObjval;
                par_copy[index].UpdateVal(direction, stepsize);
                int newobj = Solver.RunNewPars(par_copy);
                par_copy[index].CurrentObjval = newobj;

                if (newobj < oldobj)//if better
                {

                    if (newobj < internal_Bestval)
                    {
                        internal_Bestval = newobj;
                        Bestarray = par_copy.ToArray();
                        if (newobj < Bestval)
                        {
                            Bestval = newobj;
                            Add_globalhistory(par_copy.ToArray(), Bestval);
                            iteration = maxiteration; break;
                        }

                    }
                    else
                    {

                    }
                }
                else if (newobj == oldobj)//if same
                {   
                    double whattodo = (Temperature / InitialTemp);
                    if (whattodo < 0.60) //we are entering if temp is under 50 percent of initial
                    {                      
                        
                        int refusalmove = rnd.Next(0, 10); //randomly choose whether to change direction 
                        if (refusalmove == 0)
                        {
                            par_copy[index].UndoLast();
                            if (directionchange == 0) //if direction has never been changed on this parameter in this round so far
                            {
                                direction = direction == 0 ? 1 : 0;
                                directionchange++;
                            }//change the direction
                            else //avoid going back and forth if the solution isnt improving
                            {
                                iteration = maxiteration;
                            }
                        }
                    }
                    else { if (stepsize > 1) { stepsize--; }}
                    
                }
                else if (oldobj < newobj)//if worse
                {    //choose what to do depending on temperature
                    double diff = (double)(newobj - oldobj);
                    double whattodo = ((double)(oldobj - diff) / (double)oldobj) * (Temperature / InitialTemp);
                    //here
                    if (whattodo < 0.75) //we are refusing worse value (example 5 percent worse solution and already 0.9 og temp)
                    {
                        par_copy[index].UndoLast();
                        if (stepsize < par_copy[index].Maxstepsize)
                        {
                            int refusalmove = rnd.Next(0, 2); //randomly choose either to increase stepsize or to change direction 

                            if (refusalmove == 0)
                            {
                                stepsize++;
                            }
                            else if ((refusalmove == 1) &&(directionchange !=0)) //if direction has been changed on this parameter in this round so far we force step increase
                            {

                                stepsize++;

                            }
                            else if ((refusalmove == 1) && (directionchange == 0))//if direction has never been changed on this parameter in this round so far
                            {

                                direction = direction == 0 ? 1 : 0;
                                directionchange++;

                            }
                            else //avoid going back and forth if the solution isnt improving
                            {
                                iteration = maxiteration;
                            }
                            //either increase stepsize or reverse direction 
                        }
                        else //if step increase is not an option
                        {
                            if(stepsize > 1) { stepsize --; }
                            if (directionchange == 0) //if direction has never been changed on this parameter in this round so far
                            {
                                direction = direction == 0 ? 1 : 0;
                                directionchange++;
                            }//change the direction
                            else //avoid going back and forth if the solution isnt improving
                            {
                                iteration = maxiteration;
                            }
                        }

                    }
                    else { stepsize = 1; }

                }
                iteration++;


            }
            par_copy[index].LSround_completed = true;
        }
        parameters = Bestarray.ToArray();
        currentval = internal_Bestval;
        return;


    }

    public int Choose(Parameter[] parameters, double randomchoice)
    {
        SortedList<double, Parameter> SL = new SortedList<double, Parameter>();
        Parameter[] internalpar = parameters.Where(x => x.LSround_completed != true).ToArray();
        double sf = 0;
        int index = 0;
        foreach (Parameter p in internalpar)
        {
            if (p.Quality != 0) { SL.Add(sf + p.Selection_Weight + p.Quality, p); }
            else { SL.Add(sf + p.Selection_Weight, p); }

        }

        foreach (double key in SL.Keys)
        {
            SL.Keys[SL.Keys.IndexOf(key)] = key / SL.Keys.Last();
        }
        for (int i = 0; i < SL.Keys.Count - 1; i++)
        {
            if (SL.Keys[i + 1] >= randomchoice)
            {
                index = i;
                break;
            }
        }
        Parameter output = SL.Values[index];
        int outindex = Array.IndexOf(parameters, output);


        return outindex;
    }
}
public class Parameter
{
    public int CurrentParval { get; set; } = int.MaxValue;
    public double Selection_Weight { get; set; } = 0;
    public int CurrentObjval { get; set; } = int.MaxValue; //after moving and running, this needs update
    public int HistoryLength { get; set; } = 10;
    public bool Fixed { get; set; } = false;
    public int Iterations_allowed { get; set; } = 2;
    public bool LSround_completed { get; set; } = false;
    public int Average { get; set; } = 0;
    public int Maxstepsize { get; set; } = 1;
    public double No_used { get; set; } = 0;
    public double No_Improved { get; set; } = 0;
    public double Quality { get; set; } = 0;
    public Dictionary<int, int> History { get; set; } = new Dictionary<int, int>(); // The parameter value, objective value 
    public int[] Range { get; set; } = new int[10];

    public Parameter() { }
    public Parameter(int input, int currentobjval, int[] range, int iterations_allowed, double selectionweight, int maxstepsize)
    {
        HistoryLength = range.Length * 30;
        Range = range;
        CurrentParval = input;
        CurrentObjval = currentobjval;
        Iterations_allowed = iterations_allowed;
        Maxstepsize = maxstepsize;
    }
    public void UpdateVal(int direction, int stepsize)
    {
        switch (direction)
        {
            case 0: //minus
                { //add to history, change current val, update average and quality, 

                    if (CurrentObjval < History.Last().Value) { No_Improved++; }
                    Archive(CurrentParval, CurrentObjval);
                    if (Array.IndexOf(Range, CurrentParval) >= stepsize)
                    {
                        CurrentParval = Range[Array.IndexOf(Range, CurrentParval) - stepsize];
                    }
                    else if (Array.IndexOf(Range, CurrentParval) < stepsize)
                    {
                        CurrentParval = Range[Range.Length - (stepsize - Array.IndexOf(Range, CurrentParval))];

                    }

                    Average = (int)History.Keys.Average();
                    CurrentObjval = int.MaxValue;

                    No_used++;
                    if (Selection_Weight != 0) { Quality = No_Improved / No_used; }
                    break;
                }
            case 1: //plus
                {
                    if (CurrentObjval < History.Last().Value) { No_Improved++; }
                    Archive(CurrentParval, CurrentObjval);
                    if (Array.IndexOf(Range, CurrentParval) <= Range.Length - 1 - stepsize)
                    {
                        CurrentParval = Range[Array.IndexOf(Range, CurrentParval) + stepsize];
                    }
                    else if (Array.IndexOf(Range, CurrentParval) > Range.Length - 1 - stepsize)
                    {
                        int plusindex = stepsize - (Range.Length - 1 - Array.IndexOf(Range, CurrentParval)) - 1; // (stepsize - difference to top) -1   (as it starts at 0)
                        CurrentParval = Range[plusindex];
                    }
                    Average = (int)History.Keys.Average();
                    CurrentObjval = int.MaxValue;
                    No_used++;
                    if (Selection_Weight != 0) { Quality = No_Improved / No_used; }
                    break;
                }
        }
        return;
    }
    public void UndoLast()
    {
        CurrentObjval = History.Last().Value;
        CurrentParval = History.Last().Key;
        History.Remove(History.Last().Key);

    }
    public void Archive(int input, int objval)
    {
        if (History.Keys.Count >= HistoryLength + 1)
        {
            History.Remove(History.First().Key);
            History.Add(input, objval);
        }
        else
        {
            History.Add(input, objval);
        }
    }
}

