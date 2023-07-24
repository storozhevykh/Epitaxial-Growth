using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Globalization;

public class Preparation : MonoBehaviour
{
    [SerializeField] private GameObject Si_Atom_Prefab;
    [SerializeField] private GameObject Position_Prefab;
    [SerializeField] private int Translations_X = 0;
    [SerializeField] private int Translations_Y = 0;
    [SerializeField] private int Translations_Z = 0;
    [SerializeField] private int Translations_Above_Y = 0;
    [SerializeField] private float Si_bond_energy = 1.97f;
    [SerializeField] private float SiGe_bond_energy = 1.97f;
    [SerializeField] private float Ge_bond_energy = 1.97f;
    [SerializeField] private float Dimer_bond_energy = 1.0f;
    [SerializeField] private int Strain_Depth = 1;
    [SerializeField] private int Chosen_Layer_Num = 0;
    private List<float> Max_Displacement = new List<float>();
    private List<int> Steps_Number = new List<int>();
    [SerializeField] private GameObject Parameters_Panel;
    [SerializeField] Button Create_Substr_btn;
    [SerializeField] Button Throw_Atom_btn;
    [SerializeField] Button Throw_ManyAtoms_btn;
    [SerializeField] Button Set_Params_btn;
    [SerializeField] Button Search_Position_btn;

    [SerializeField] TMP_InputField Substr_Height_input;
    [SerializeField] TMP_InputField Substr_Length_input;
    [SerializeField] TMP_InputField Substr_Width_input;
    [SerializeField] TMP_InputField Si_bond_energy_input;
    [SerializeField] TMP_InputField SiGe_bond_energy_input;
    [SerializeField] TMP_InputField Ge_bond_energy_input;
    [SerializeField] TMP_InputField Dimer_bond_energy_input;
    [SerializeField] TMP_InputField Strain_Depth_input;
    [SerializeField] TMP_Dropdown Layer_Number_dropdown;
    [SerializeField] TMP_InputField Max_Displacement_input;
    [SerializeField] TMP_InputField Steps_Number_input;

    private List<string> Layer_Number_dropdown_options = new List<string>();
    private List<Vector3> AtomPositionsInSubstrate = new List<Vector3>();
    private List<Vector3> GridPositions = new List<Vector3>();
    private float a;
    private float a_Ge;
    private GameObject atom;
    // Start is called before the first frame update
    void Start()
    {
        a = Mathf.Sin((Mathf.PI / 180) * Parameters_Storage.Si_Si_angle / 2) * Parameters_Storage.Si_Si_bond_length * Mathf.Sqrt(2) * 2;
        a_Ge = Mathf.Sin((Mathf.PI / 180) * Parameters_Storage.Si_Si_angle / 2) * Parameters_Storage.Ge_Ge_bond_length * Mathf.Sqrt(2) * 2;
        print("Постоянная решётки Si: " + a);
        print("Постоянная решётки Ge: " + a_Ge);

        LoadSettings();
    }

    public void CreateSubstrate()
    {
        if (!StaticStorage.SubstrateCreated)
        {
            for (int i = 0; i <= Translations_X; i++)
            {
                for (int j = 0; j <= Translations_Y; j++)
                {
                    for (int k = 0; k <= Translations_Z; k++)
                    {
                        CreateCell(a, a, i, j, k, AtomPositionsInSubstrate);
                    }
                }
            }
            CreateGrid(a, a_Ge);
            ShowSubstrate();
            ShowPositions();
            StaticStorage.SubstrateAtoms = AtomPositionsInSubstrate;
            StaticStorage.AtomPositions = GridPositions;
            StaticStorage.SubstrateCreated = true;
            Throw_Atom_btn.interactable = true;
            Throw_ManyAtoms_btn.interactable = true;
            Create_Substr_btn.interactable = false;
        }
        StaticStorage.DepositedAtoms = new List<Atom>();
    }
    private void CreateGrid(float a, float a_Y)
    {
        for (int i = 0; i <= Translations_X; i++)
        {
            for (int j = Translations_Y + 1; j <= Translations_Y + 1 + Translations_Above_Y; j++)
            {
                for (int k = 0; k <= Translations_Z; k++)
                {
                    CreateCell(a, a_Y, i, j, k, GridPositions);
                }
            }
        }
    }
    private void CreateCell(float a, float a_Y, float Translation_X, float Translation_Y, float Translation_Z, List<Vector3> positionsList)
    {
        Translation_X = Translation_X * a;
        if (a == a_Y) Translation_Y = Translation_Y * a;
        else Translation_Y = Translations_Y * a + (Translation_Y - Translations_Y) * a_Y;
        Translation_Z = Translation_Z * a;

        float x1 = 0 + Translation_X;
        float y1 = 0 + Translation_Y;
        float z1 = 0 + Translation_Z;
        float x2 = a / 2 + Translation_X;
        float y2 = 0 + Translation_Y;
        float z2 = a / 2 + Translation_Z;
        float x3 = 0 + Translation_X;
        float y3 = a_Y / 2 + Translation_Y;
        float z3 = a / 2 + Translation_Z;
        float x4 = a / 2 + Translation_X;
        float y4 = a_Y / 2 + Translation_Y;
        float z4 = 0 + Translation_Z;
        float x5 = a / 4 + Translation_X;
        float y5 = a_Y / 4 + Translation_Y;
        float z5 = a / 4 + Translation_Z;

        positionsList.Add(new Vector3(x1, y1, z1));
        positionsList.Add(new Vector3(x2, y2, z2));
        positionsList.Add(new Vector3(x3, y3, z3));
        positionsList.Add(new Vector3(x4, y4, z4));
        positionsList.Add(new Vector3(x5, y5, z5));
        positionsList.Add(new Vector3(x5 + a / 2, y5, z5 + a / 2));
        positionsList.Add(new Vector3(x5, y5 + a_Y / 2, z5 + a / 2));
        positionsList.Add(new Vector3(x5 + a / 2, y5 + a_Y / 2, z5));
    }

    private void ShowSubstrate()
    {
        StaticStorage.SubstrateAtoms_Objects = new List<GameObject>();
        foreach (Vector3 element in AtomPositionsInSubstrate)
        {
            atom = GameObject.Instantiate(Si_Atom_Prefab, element, Quaternion.identity);
            StaticStorage.SubstrateAtoms_Objects.Add(atom);
        }
    }
    private void ShowPositions()
    {
        StaticStorage.AtomPositions_Objects = new List<GameObject>();
        foreach (Vector3 element in GridPositions)
        {
            StaticStorage.AtomPositions_Objects.Add(GameObject.Instantiate(Position_Prefab, element, Quaternion.identity));
        }
    }
    public void SetParameters(int mode)
    {
        switch (mode)
        {
            case 0:
                Parameters_Panel.SetActive(true);
                Create_Substr_btn.interactable = false;
                Throw_Atom_btn.interactable = false;
                Set_Params_btn.interactable = false;
                Search_Position_btn.interactable = false;
                break;

            case 1:
                Translations_Y = int.Parse(Substr_Height_input.text);
                break;

            case 2:
                Translations_X = int.Parse(Substr_Length_input.text);
                break;

            case 3:
                Translations_Z = int.Parse(Substr_Width_input.text);
                break;

            case 4:
                Si_bond_energy = float.Parse(Si_bond_energy_input.text);
                break;

            case 5:
                SiGe_bond_energy = float.Parse(SiGe_bond_energy_input.text);
                break;

            case 6:
                Ge_bond_energy = float.Parse(Ge_bond_energy_input.text);
                break;

            case 7:
                Dimer_bond_energy = float.Parse(Dimer_bond_energy_input.text);
                break;

            case 8:
                Strain_Depth = int.Parse(Strain_Depth_input.text);
                Layer_Number_dropdown_options.Clear();

                for (int i = 0; i <= Strain_Depth; i++)
                {
                    Layer_Number_dropdown_options.Add(i.ToString());
                }
                Layer_Number_dropdown.ClearOptions();
                Layer_Number_dropdown.AddOptions(Layer_Number_dropdown_options);

                if (Max_Displacement.Count < Strain_Depth + 1)
                {
                    for (int i = Max_Displacement.Count; i <= Strain_Depth; i++)
                    {
                        Max_Displacement.Add(Max_Displacement[i - 1]);
                        Steps_Number.Add(Steps_Number[i - 1]);
                    }
                }
                break;

            case 9:
                Chosen_Layer_Num = Layer_Number_dropdown.value;
                Max_Displacement_input.text = Max_Displacement[Chosen_Layer_Num].ToString();
                Steps_Number_input.text = Steps_Number[Chosen_Layer_Num].ToString();
                break;

            case 10:
                Max_Displacement[Chosen_Layer_Num] = float.Parse(Max_Displacement_input.text);
                break;

            case 11:
                Steps_Number[Chosen_Layer_Num] = int.Parse(Steps_Number_input.text);
                break;

            case 12:
                Parameters_Panel.SetActive(false);
                Create_Substr_btn.interactable = true;
                Throw_Atom_btn.interactable = true;
                Set_Params_btn.interactable = true;
                Search_Position_btn.interactable = true;
                SaveSettings();
                break;

            case 13:
                ResetSettings();
                break;
        }

        SaveInStorage();
    }

    private void SaveSettings()
    {
        PlayerPrefs.SetInt("Substr_Height", Translations_Y);
        PlayerPrefs.SetInt("Substr_Length", Translations_X);
        PlayerPrefs.SetInt("Substr_Width", Translations_Z);
        PlayerPrefs.SetFloat("Si_bond_energy", Si_bond_energy);
        PlayerPrefs.SetFloat("SiGe_bond_energy", SiGe_bond_energy);
        PlayerPrefs.SetFloat("Ge_bond_energy", Ge_bond_energy);
        PlayerPrefs.SetFloat("Dimer_bond_energy", Dimer_bond_energy);
        PlayerPrefs.SetInt("Strain_Depth", Strain_Depth);

        for (int i = 0; i < Max_Displacement.Count; i++)
        {
            PlayerPrefs.SetFloat("Max_Displacement_" + i, Max_Displacement[i]);
            PlayerPrefs.SetInt("Steps_Number_" + i, Steps_Number[i]);
        }
    }
    private void LoadSettings()
    {
        Translations_Y = PlayerPrefs.GetInt("Substr_Height", 1);
        Translations_X = PlayerPrefs.GetInt("Substr_Length", 10);
        Translations_Z = PlayerPrefs.GetInt("Substr_Width", 10);
        Si_bond_energy = PlayerPrefs.GetFloat("Si_bond_energy", 1.97f);
        SiGe_bond_energy = PlayerPrefs.GetFloat("SiGe_bond_energy", 1.97f);
        Ge_bond_energy = PlayerPrefs.GetFloat("Ge_bond_energy", 1.97f);
        Dimer_bond_energy = PlayerPrefs.GetFloat("Dimer_bond_energy", 1f);
        Strain_Depth = PlayerPrefs.GetInt("Strain_Depth", 2);

        Layer_Number_dropdown_options.Clear();
        for (int i = 0; i <= Strain_Depth; i++)
        {
            Max_Displacement.Add(PlayerPrefs.GetFloat("Max_Displacement_" + i, 0.5f));
            Steps_Number.Add(PlayerPrefs.GetInt("Steps_Number_" + i, 100));
            Layer_Number_dropdown_options.Add(i.ToString());
        }
        Layer_Number_dropdown.ClearOptions();
        Layer_Number_dropdown.AddOptions(Layer_Number_dropdown_options);

        Substr_Height_input.text = Translations_Y.ToString();
        Substr_Length_input.text = Translations_X.ToString();
        Substr_Width_input.text = Translations_Z.ToString();
        Si_bond_energy_input.text = Si_bond_energy.ToString();
        SiGe_bond_energy_input.text = SiGe_bond_energy.ToString();
        Ge_bond_energy_input.text = Ge_bond_energy.ToString();
        Dimer_bond_energy_input.text = Dimer_bond_energy.ToString();
        Strain_Depth_input.text = Strain_Depth.ToString();
        Max_Displacement_input.text = Max_Displacement[0].ToString();
        Steps_Number_input.text = Steps_Number[0].ToString();
    }

    private void ResetSettings()
    {
        Translations_Y = 1;
        Translations_X = 10;
        Translations_Z = 10;
        Si_bond_energy = 1.97f;
        SiGe_bond_energy = 1.97f;
        Ge_bond_energy = 1.97f;
        Dimer_bond_energy = 1f;
        Strain_Depth = 2;
        Max_Displacement.Clear();
        Steps_Number.Clear();
        Max_Displacement.Add(0.5f);
        Steps_Number.Add(100);

        SaveSettings();
        LoadSettings();
    }
    private void SaveInStorage()
    {
        Parameters_Storage.Si_bond_energy = Si_bond_energy;
        Parameters_Storage.SiGe_bond_energy = SiGe_bond_energy;
        Parameters_Storage.Ge_bond_energy = Ge_bond_energy;
        Parameters_Storage.Dimer_bond_energy = Dimer_bond_energy;
        Parameters_Storage.Strain_Depth = Strain_Depth;
        Parameters_Storage.Max_Displacement = Max_Displacement;
        Parameters_Storage.Steps_Number = Steps_Number;
}
}
