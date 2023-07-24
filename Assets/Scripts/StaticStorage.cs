using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public static class StaticStorage
{
    public static List<Vector3> SubstrateAtoms;
    public static List<GameObject> SubstrateAtoms_Objects;
    public static List<Vector3> AtomPositions;
    public static List<Atom> DepositedAtoms;
    public static List<GameObject> AtomPositions_Objects;
    public static bool SubstrateCreated = false;
    public static bool StopCalculation = false;
}
