using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionsDetector : MonoBehaviour
{
    public List<Vector3> NearAtomsPositions = new List<Vector3>();
    public List<Atom> NearAtoms = new List<Atom>();

    private void OnTriggerStay(Collider other)
    {
        Vector3 otherPosition = other.transform.position;
        if (other.tag == "Si_Atom" && !NearAtomsPositions.Contains(otherPosition))
        {
            NearAtomsPositions.Add(otherPosition);
            NearAtoms.Add(new Atom(other.gameObject, 0, 0, 0, new List<Atom>(), 0));
        }

        if (other.tag == "Ge_Atom" && !NearAtomsPositions.Contains(otherPosition))
        {
            NearAtomsPositions.Add(otherPosition);
            NearAtoms.Add(new Atom(other.gameObject, 1, 0, 0, new List<Atom>(), 0));
        }
        //print("EnteredTrigger, NearAtoms = " + NearAtoms.Count);
    }
}
