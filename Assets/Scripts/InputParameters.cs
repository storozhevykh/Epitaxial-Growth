using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputParameters : MonoBehaviour
{
    [SerializeField] private float Si_Si_bond_length = 2.35156f;
    [SerializeField] private float Ge_Ge_bond_length = 2.44f;
    [SerializeField] private float Si_Ge_bond_length = 2.39578f;
    [SerializeField] private float Si_Si_angle = 109.471f;
    void Start()
    {
        Parameters_Storage.Si_Si_bond_length = Si_Si_bond_length;
        Parameters_Storage.Ge_Ge_bond_length = Ge_Ge_bond_length;
        Parameters_Storage.Si_Ge_bond_length = Si_Ge_bond_length;
        Parameters_Storage.Si_Si_angle = Si_Si_angle;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
