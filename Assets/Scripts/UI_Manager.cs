using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_Manager : MonoBehaviour
{
    [SerializeField] Button Create_Substr_btn;
    [SerializeField] Button Throw_Atom_btn;
    [SerializeField] Button Search_Position_btn;
    [SerializeField] Button Adjust_Position_btn;
    [SerializeField] Button Throw_ManyAtoms_btn;
    [SerializeField] Button Stop_Calc_btn;
    // Start is called before the first frame update
    void Start()
    {
        Throw_Atom_btn.interactable = false;
        Throw_ManyAtoms_btn.interactable = false;
        Search_Position_btn.interactable = false;
        Adjust_Position_btn.interactable = false;
        Stop_Calc_btn.interactable = false;
    }

    public void StopCalc()
    {
        StaticStorage.StopCalculation = true;
    }
}
