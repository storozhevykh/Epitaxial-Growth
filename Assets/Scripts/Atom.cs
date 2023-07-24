using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Atom
{
    public GameObject atomObject;
    public int atomType;
    public int strongBonds;
    public int dimerBonds;
    public List<Atom> nearAtoms;
    public List<Vector3> positionsList = new List<Vector3>();
    public int depth;

    public Atom(GameObject atomObject, int atomType, int strongBonds, int dimerBonds, List<Atom> nearAtoms, int depth)
    {
        this.atomObject = atomObject;
        this.atomType = atomType;
        this.strongBonds = strongBonds;
        this.dimerBonds = dimerBonds;
        this.nearAtoms = nearAtoms;
        this.depth = depth;
    }

    public override bool Equals(object obj)
    {
        // If the passed object is null, return False
        if (obj == null)
        {
            return false;
        }
        // If the passed object is not Customer Type, return False
        if (!(obj is Atom))
        {
            return false;
        }
        return atomObject.Equals(((Atom)obj).atomObject);
    }
}
