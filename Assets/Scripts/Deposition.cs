using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TMPro;

public class Deposition : MonoBehaviour
{
    [SerializeField] private GameObject Atom_Prefab;
    [SerializeField] private Material Depositing_Material;
    [SerializeField] private Material FreeRange_Material;
    [SerializeField] private Material Positions_Material;
    [SerializeField] private Material Si_Atom_Material;
    [SerializeField] private Material Ge_Atom_Material;
    [SerializeField] private Material Stressed_Material;
    [SerializeField] private float FreePathLength = 20;
    [SerializeField] private Button Throw_Atom_btn;
    [SerializeField] private Button Throw_ManyAtoms_btn;
    [SerializeField] private TMP_InputField AtomsAmount_input;
    [SerializeField] private Button Search_Position_btn;
    [SerializeField] private Button Adjust_Position_btn;
    [SerializeField] private Button Stop_Calc_btn;
    [SerializeField] private GameObject CameraController;
    [SerializeField] private TextMeshProUGUI Iterations_text;
    [SerializeField] private int Batch_size = 10000000;
    private GameObject atom;
    private CollisionsDetector collisionsDetector;
    private List<GameObject> ProbablePositions_Objects = new List<GameObject>();
    private List<GameObject> NearSubstrateAtoms_Objects = new List<GameObject>();
    private List<Vector3> ProbablePositions = new List<Vector3>();
    private List<Atom> PossibleNearAtoms = new List<Atom>();
    private int bestNearAtoms;
    private int bestDimerBonds;
    private int dimerBonds;
    private Vector3 bestPosition = Vector3.zero;
    private float speed = 10f;
    private Camera camera;
    private bool moveToBestPosition = false;
    private float totalEnergy = 0f;
    private List<Atom> nearAtoms;
    private List<Atom> nearAtoms_temp;
    private List<Vector3> nearAtomsPositions;
    private Atom atom_temp, nearAtom1, nearAtom2;
    private int curDepth = 0;
    private List<List<Vector3>> PositionsChanges = new List<List<Vector3>>();
    private List<Atom> movingAtomsList;
    private List<Atom> calcAtomsList;
    private List<Atom> movingAtomsListWithNears = new List<Atom>();
    private Atom[] atoms = new Atom[100];
    private Vector3[] atPositions = new Vector3[100];
    private Vector3[] bestPositions = new Vector3[100];
    private Vector3[] newPositions = new Vector3[100];
    private List<Vector3>[] listOfNears = new List<Vector3>[100];
    private int iteration;
    private int iterNumbers;
    private Thread myThread1;
    private List<ThreeAtomsPos> ThreeAtomsPosList = new List<ThreeAtomsPos>();
    private ThreeAtomsPos[] ThreeAtomsPosArr;
    private List<List<ThreeAtomsPos>> ThreeAtomsPosListsForGPU = new List<List<ThreeAtomsPos>>();

    private ComputeShader _shader;
    private int kernel;
    private ComputeBuffer PosIncrementArr_Buf, resultBuf;
    private ComputeBuffer Atom0_1_2_coords_Buf, Atom1_1_coords_Buf, Atom1_2_coords_Buf, Atom1_3_coords_Buf, Atom2_1_coords_Buf, Atom2_2_coords_Buf, Atom2_3_coords_Buf;
    private float[] resultEnergiesGPU;
    private List<float> resultEnergiesGPUList = new List<float>();
    private List<float> resultEnergiesList = new List<float>();
    private int prev_i0 = 0, prev_i1 = 0, prev_i2 = 0;
    private bool allPositionsUsed = false;
    private float bondLength = 0f;
    private float bondLength1 = 0f;
    private float bondLength2 = 0f;
    private float minEnergy = 999999f;
    private float energy;

    public void StartFullCycle()
    {
        /*myThread1 = new Thread(() => StartCoroutine(SearchForBestPosition(1, int.Parse(AtomsAmount_input.text))));
        myThread1.Start();*/

        StartCoroutine(SearchForBestPosition(1, int.Parse(AtomsAmount_input.text)));
    }

    public void ThrowAtom(int mode)
    {
        List<GameObject> AtomPositions_Objects = StaticStorage.AtomPositions_Objects;
        Vector3 StartPosition = AtomPositions_Objects[Random.Range(0, AtomPositions_Objects.Count)].transform.position;
        bestPosition = StartPosition;

        //print("ProbablePositions_Objects count = " + ProbablePositions_Objects.Count);
        //print("AtomPositions_Objects count = " + AtomPositions_Objects.Count);

        atom = GameObject.Instantiate(Atom_Prefab, StartPosition, Quaternion.identity);
        if (mode == 0)
        atom.GetComponent<Renderer>().material = Depositing_Material;

        for (int i = 0; i < AtomPositions_Objects.Count; i++)
        {
            Vector3 atomPosition = AtomPositions_Objects[i].transform.position;
            Vector3 XZ_Position = new Vector3(atomPosition.x, StartPosition.y, atomPosition.z);
            if (Vector3.Distance(StartPosition, XZ_Position) <= FreePathLength)
            {
                if (mode == 0)
                StaticStorage.AtomPositions_Objects[i].GetComponent<Renderer>().material = FreeRange_Material;
                ProbablePositions_Objects.Add(AtomPositions_Objects[i]);
            }
        }

        foreach (Atom depositedAtom in StaticStorage.DepositedAtoms)
        {
            Vector3 atomPosition = depositedAtom.atomObject.transform.position;
            Vector3 XZ_Position = new Vector3(atomPosition.x, StartPosition.y, atomPosition.z);
            if (Vector3.Distance(StartPosition, XZ_Position) <= FreePathLength + 10)
            {
                /*if (mode == 0)
                depositedAtom.atomObject.GetComponent<Renderer>().material = Depositing_Material;*/
                PossibleNearAtoms.Add(depositedAtom);
            }
        }

        foreach (GameObject curObject in StaticStorage.SubstrateAtoms_Objects)
        {
            Vector3 atomPosition = curObject.transform.position;
            Vector3 XZ_Position = new Vector3(atomPosition.x, StartPosition.y, atomPosition.z);
            if (Vector3.Distance(StartPosition, XZ_Position) <= FreePathLength + 10)
            {
                NearSubstrateAtoms_Objects.Add(curObject);
            }
        }

            if (mode == 0)
        {
            Throw_Atom_btn.interactable = false;
            Search_Position_btn.interactable = true;
            Stop_Calc_btn.interactable = false;
            StaticStorage.StopCalculation = false;
            iteration = 0;
            CameraController.GetComponent<MSCameraController>().target = atom.transform;
        }
    }

    public void SearchStart()
    {
        StartCoroutine(SearchForBestPosition(0, 1));
    }
    IEnumerator SearchForBestPosition(int mode, int atomsAmount)
    {
        int atomsCount = 1;
        while (atomsCount <= atomsAmount)
        {
            print("Atom number: " + atomsCount);
            if (mode == 1) ThrowAtom(1);

            bestNearAtoms = -1;
            bestDimerBonds = -1;
            foreach (GameObject curPosition_Object in NearSubstrateAtoms_Objects)
            {
                curPosition_Object.GetComponent<SphereCollider>().enabled = true;
                collisionsDetector = curPosition_Object.GetComponent<CollisionsDetector>();
                collisionsDetector.NearAtoms.Clear();
                collisionsDetector.NearAtomsPositions.Clear();
            }
            foreach (Atom curPosition_Object in PossibleNearAtoms)
            {
                collisionsDetector = curPosition_Object.atomObject.GetComponent<CollisionsDetector>();
                curPosition_Object.atomObject.GetComponent<SphereCollider>().enabled = true;
                collisionsDetector.NearAtoms.Clear();
                collisionsDetector.NearAtomsPositions.Clear();
            }
            foreach (GameObject curPosition_Object in ProbablePositions_Objects)
            {
                collisionsDetector = curPosition_Object.GetComponent<CollisionsDetector>();
                curPosition_Object.GetComponent<SphereCollider>().enabled = true;
                collisionsDetector.NearAtoms.Clear();
                collisionsDetector.NearAtomsPositions.Clear();
            }

            yield return new WaitForSeconds(0.1f);

            foreach (GameObject curPosition_Object in NearSubstrateAtoms_Objects)
                curPosition_Object.GetComponent<SphereCollider>().enabled = false;
            foreach (Atom curPosition_Object in PossibleNearAtoms)
                curPosition_Object.atomObject.GetComponent<SphereCollider>().enabled = false;
            foreach (GameObject curPosition_Object in ProbablePositions_Objects)
                curPosition_Object.GetComponent<SphereCollider>().enabled = false;

            minEnergy = 9999f;
            foreach (GameObject curPosition_Object in ProbablePositions_Objects)
            {
                collisionsDetector = curPosition_Object.GetComponent<CollisionsDetector>();
                Vector3 curPosition = curPosition_Object.transform.position;
                nearAtoms_temp = collisionsDetector.NearAtoms;
                //atom.transform.position = curPosition_Object.transform.position;
                //print(collisionsDetector.NearAtoms.Count);
                if (collisionsDetector.NearAtomsPositions.Count > bestNearAtoms)
                {
                    bestNearAtoms = nearAtoms_temp.Count;
                    bestPosition = curPosition_Object.transform.position;
                    nearAtomsPositions = collisionsDetector.NearAtomsPositions.ToList();
                    nearAtoms = nearAtoms_temp.ToList();
                    atom_temp = new Atom(new GameObject(), 1, 0, 0, null, 0);
                    atom_temp.atomObject.transform.position = bestPosition;
                    //print("bestNearAtoms count = " + bestNearAtoms);
                }
                else if (collisionsDetector.NearAtomsPositions.Count == bestNearAtoms)
                {

                    //print("bestNearAtoms count = " + bestNearAtoms);

                    /*atom_temp = new Atom(new GameObject(), 0, 0, 0, null, 0);
                    atom_temp.atomObject.transform.position = curPosition_Object.transform.position;
                    for (int k = 0; k < 100; k++)
                    {
                        energy = 0f;
                        for (int j = 0; j < nearAtoms_temp.Count; j++)
                        {
                            Atom nearAtom = nearAtoms_temp[j];

                            CalcAndMove_2(atom_temp, nearAtom);
                            energy += SW.Potential_2(atom_temp.atomObject.transform.position, nearAtom.atomObject.transform.position, bondLength);

                        }

                        switch (nearAtoms_temp.Count)
                        {
                            case 2:
                                nearAtom1 = nearAtoms_temp[0];
                                nearAtom2 = nearAtoms_temp[1];
                                CalcAndMove_3(atom_temp, nearAtom1, nearAtom2);
                                energy += SW.Potential_3(atom_temp.atomObject.transform.position, nearAtom1.atomObject.transform.position, nearAtom2.atomObject.transform.position, bondLength1, bondLength2);
                                break;

                            case 3:
                                nearAtom1 = nearAtoms_temp[0];
                                nearAtom2 = nearAtoms_temp[1];
                                CalcAndMove_3(atom_temp, nearAtom1, nearAtom2);
                                energy += SW.Potential_3(atom_temp.atomObject.transform.position, nearAtom1.atomObject.transform.position, nearAtom2.atomObject.transform.position, bondLength1, bondLength2);

                                nearAtom1 = nearAtoms_temp[0];
                                nearAtom2 = nearAtoms_temp[2];
                                CalcAndMove_3(atom_temp, nearAtom1, nearAtom2);
                                energy += SW.Potential_3(atom_temp.atomObject.transform.position, nearAtom1.atomObject.transform.position, nearAtom2.atomObject.transform.position, bondLength1, bondLength2);

                                nearAtom1 = nearAtoms_temp[1];
                                nearAtom2 = nearAtoms_temp[2];
                                CalcAndMove_3(atom_temp, nearAtom1, nearAtom2);
                                energy += SW.Potential_3(atom_temp.atomObject.transform.position, nearAtom1.atomObject.transform.position, nearAtom2.atomObject.transform.position, bondLength1, bondLength2);
                                break;
                        }

                        if (k == 0) print("Start energy Si = " + energy);
                    }*/

                    //print("Final energy Si = " + energy);

                    atom_temp = new Atom(new GameObject(), 1, 0, 0, null, 0);
                    atom_temp.atomObject.transform.position = curPosition_Object.transform.position;
                    for (int k = 0; k < 1000; k++)
                    {
                        energy = 0f;
                        for (int j = 0; j < nearAtoms_temp.Count; j++)
                        {
                            Atom nearAtom = nearAtoms_temp[j];

                            CalcAndMove_2(atom_temp, nearAtom);
                            energy += SW.Potential_2(atom_temp.atomObject.transform.position, nearAtom.atomObject.transform.position, bondLength);
                        }

                        switch (nearAtoms_temp.Count)
                        {
                            case 2:
                                nearAtom1 = nearAtoms_temp[0];
                                nearAtom2 = nearAtoms_temp[1];
                                CalcAndMove_3(atom_temp, nearAtom1, nearAtom2);
                                energy += SW.Potential_3(atom_temp.atomObject.transform.position, nearAtom1.atomObject.transform.position, nearAtom2.atomObject.transform.position, bondLength1, bondLength2);
                                break;

                            case 3:
                                nearAtom1 = nearAtoms_temp[0];
                                nearAtom2 = nearAtoms_temp[1];
                                CalcAndMove_3(atom_temp, nearAtom1, nearAtom2);
                                energy += SW.Potential_3(atom_temp.atomObject.transform.position, nearAtom1.atomObject.transform.position, nearAtom2.atomObject.transform.position, bondLength1, bondLength2);

                                nearAtom1 = nearAtoms_temp[0];
                                nearAtom2 = nearAtoms_temp[2];
                                CalcAndMove_3(atom_temp, nearAtom1, nearAtom2);
                                energy += SW.Potential_3(atom_temp.atomObject.transform.position, nearAtom1.atomObject.transform.position, nearAtom2.atomObject.transform.position, bondLength1, bondLength2);

                                nearAtom1 = nearAtoms_temp[1];
                                nearAtom2 = nearAtoms_temp[2];
                                CalcAndMove_3(atom_temp, nearAtom1, nearAtom2);
                                energy += SW.Potential_3(atom_temp.atomObject.transform.position, nearAtom1.atomObject.transform.position, nearAtom2.atomObject.transform.position, bondLength1, bondLength2);
                                break;
                        }

                        //if (k == 0) print("Start energy = " + energy);
                    }

                    //print("Final energy = " + energy);
                    //print("PossibleNearAtoms Count = " + PossibleNearAtoms.Count);

                    dimerBonds = 0;
                    foreach (Atom at in PossibleNearAtoms)
                    {
                        Vector3 atPosition = at.atomObject.transform.position;
                        if (atPosition.y < curPosition.y + 1f && atPosition.y > curPosition.y - 1f)
                        {
                            //print("atPosition = " + atPosition);
                            if (Vector3.Distance(curPosition, atPosition) < 4.1f && at.strongBonds + at.dimerBonds < 4)
                            {
                                dimerBonds++;
                                energy -= Parameters_Storage.Dimer_bond_energy;
                                //print("Vector3.Distance(curPosition, atPosition) = " + Vector3.Distance(curPosition, atPosition));
                            }
                        }
                    }
                    /*if (dimerBonds > bestDimerBonds && dimerBonds < 3)
                    {
                        bestDimerBonds = dimerBonds;
                        bestPosition = curPosition_Object.transform.position;
                        nearAtomsPositions = collisionsDetector.NearAtomsPositions.ToList();
                        nearAtoms = collisionsDetector.NearAtoms.ToList();
                        atom_temp = new Atom(new GameObject(), 1, 0, 0, null, 0);
                        atom_temp.atomObject.transform.position = bestPosition;
                    }*/
                    

                    if (energy < minEnergy)
                    {
                        print("dimerBonds = " + dimerBonds);
                        print("Final energy after dimers = " + energy);
                        minEnergy = energy;
                        bestPosition = curPosition_Object.transform.position;
                        nearAtomsPositions = collisionsDetector.NearAtomsPositions.ToList();
                        nearAtoms = collisionsDetector.NearAtoms.ToList();
                        bestDimerBonds = dimerBonds;
                        bestNearAtoms = nearAtoms.Count;
                        atom_temp = new Atom(new GameObject(), 1, 0, 0, null, 0);
                        atom_temp.atomObject.transform.position = bestPosition;
                    }

                }
            }
            //print("nearAtoms count = " + nearAtoms.Count);
            //atom.transform.position = bestPosition;
            Search_Position_btn.interactable = false;
            if (bestPosition != Vector3.zero && mode == 0) moveToBestPosition = true;
            if (bestPosition != Vector3.zero && mode == 1)
            {
                atom.transform.position = bestPosition;
                FinishSearchingPosition(mode);
                AdjustPosition(mode);
            }
            atomsCount++;
        }
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (moveToBestPosition)
        {
            float step = speed * Time.deltaTime;
            atom.transform.position = Vector3.MoveTowards(atom.transform.position, bestPosition, step);
            if (atom.transform.position == bestPosition) FinishSearchingPosition(0);
        }
        Iterations_text.text = iteration.ToString();
    }
    private void FinishSearchingPosition(int mode)
    {
        moveToBestPosition = false;
        if (mode == 0)
        {
            foreach (GameObject curPosition_Object in ProbablePositions_Objects)
                curPosition_Object.GetComponent<Renderer>().material = Positions_Material;
        }
        atom.GetComponent<Renderer>().material = Ge_Atom_Material;
        ProbablePositions_Objects.Clear();
        PossibleNearAtoms.Clear();
        NearSubstrateAtoms_Objects.Clear();
        GameObject excludePosition = StaticStorage.AtomPositions_Objects.Find((x) => x.transform.position == atom.transform.position);
        StaticStorage.AtomPositions_Objects.Remove(excludePosition);
        Throw_Atom_btn.interactable = true;
        Search_Position_btn.interactable = false;
        StaticStorage.DepositedAtoms.Add(new Atom(atom, 1, bestNearAtoms, bestDimerBonds, nearAtoms, 0));
        totalEnergy = totalEnergy - bestNearAtoms * Parameters_Storage.SiGe_bond_energy - bestDimerBonds * Parameters_Storage.Dimer_bond_energy;
        Adjust_Position_btn.interactable = true;
    }
    public void AdjustPosition(int mode)
    {
        Adjust_Position_btn.interactable = false;
        //CreatePositionChanges();
        Vector3 atPosition = atom.transform.position;
        
        movingAtomsList = new List<Atom>();
        movingAtomsListWithNears.Clear();
        List<Atom> lastNearAtoms = new List<Atom>();
        lastNearAtoms.Add(new Atom(atom, 1, bestNearAtoms, bestDimerBonds, nearAtoms, 0));

        for (int i = 0; i <= Parameters_Storage.Strain_Depth; i++)
        {
            List<Atom> nextNearAtoms = new List<Atom>();
            foreach (Atom curAtom in lastNearAtoms)
            {
                movingAtomsList.Add(curAtom);
                movingAtomsListWithNears.Add(curAtom);
                nearAtoms = movingAtomsListWithNears[movingAtomsListWithNears.Count - 1].nearAtoms;
                if (i > 0)
                {
                    collisionsDetector = curAtom.atomObject.GetComponent<CollisionsDetector>();
                    nearAtoms.AddRange(collisionsDetector.NearAtoms);
                    if (i == 1)
                        nearAtoms.Add(new Atom(atom, 1, bestNearAtoms, bestDimerBonds, nearAtoms, 0));
                }
                foreach (Atom nearAtom in nearAtoms)
                {
                    //print("nearAtoms.Count = " + nearAtoms.Count);
                    if (!movingAtomsList.Contains(nearAtom))
                    {
                        nextNearAtoms.Add(nearAtom);
                        /*if (!movingAtomsListWithNears[movingAtomsListWithNears.Count - 1].nearAtoms.Contains(nearAtom))
                        movingAtomsListWithNears[movingAtomsListWithNears.Count - 1].nearAtoms.Add(nearAtom);*/
                    }
                    //print("nearAtoms.Count = " + nearAtoms.Count);
                }
            }
            lastNearAtoms = nextNearAtoms.ToList();
        }
        calcAtomsList = movingAtomsList.ToList();
        calcAtomsList.AddRange(lastNearAtoms);

        //print("movingAtomsListWithNears count = " + movingAtomsListWithNears.Count);

        iterNumbers = 1;
        for (int i = 0; i <  movingAtomsListWithNears.Count; i++)
        {
            Atom a = movingAtomsListWithNears[i];
            if (mode == 0)
            {
                Material mat = a.atomObject.GetComponent<Renderer>().material;
                mat.color = Color.blue;
            }
            if (mode == 1 && i > 0 && a.atomObject.tag == "Si_Atom")
            {
                a.atomObject.GetComponent<Renderer>().material = Stressed_Material;
            }
            atoms[i] = a;
            atPositions[i] = a.atomObject.transform.position;
            listOfNears[i] = new List<Vector3>();
            if (a.nearAtoms.Count > 0) foreach (Atom near in a.nearAtoms) listOfNears[i].Add(near.atomObject.transform.position);
            /*List<Vector3> changePosList = PositionsChanges[a.depth].ToList();
            foreach (Vector3 pos in changePosList)
            {
                a.positionsList.Add(atPositions[i] + pos);
            }*/
            iterNumbers *= a.positionsList.Count;
        }

        //ThreeAtomsPosArr = new ThreeAtomsPos[atoms[0].positionsList.Count * atoms[1].positionsList.Count * atoms[2].positionsList.Count];
        int index = 0;

        allPositionsUsed = false;
        float startTime = Time.realtimeSinceStartup;
        while (!allPositionsUsed)
        {
            ThreeAtomsPosList.Clear();

            //Create_ThreeAtomsPosList();
            //print("ThreeAtomsPosList length: " + ThreeAtomsPosList.Count);
            AtomMovingOld();
            Stop_Calc_btn.interactable = true;
            System.GC.Collect();
            allPositionsUsed = true;
        }
        //print("Time elapsed: " + (Time.realtimeSinceStartup - startTime).ToString());
    }

    private void Create_ThreeAtomsPosList()
    {
        ThreeAtomsPos curAtomsPos;
        int controlIndex = 0;
        for (int i0 = prev_i0; i0 < PositionsChanges[0].Count; i0++)
        {
            for (int i1 = 0; i1 < PositionsChanges[1].Count; i1++)
            {
                for (int i2 = 0; i2 < PositionsChanges[1].Count; i2++)
                {
                    if (controlIndex == 0)
                    {
                        i1 = prev_i1;
                        i2 = prev_i2;
                        if (i2 == PositionsChanges[1].Count)
                        {
                            i2 = 0;
                            i1++;
                            if (i1 >= PositionsChanges[1].Count)
                            {
                                i1 = 0;
                                i0++;
                            }
                        }
                    }

                    controlIndex++;

                    curAtomsPos = new ThreeAtomsPos(PositionsChanges[0][i0].x, PositionsChanges[0][i0].y, PositionsChanges[0][i0].z,
                        PositionsChanges[1][i1].x, PositionsChanges[1][i1].y, PositionsChanges[1][i1].z,
                        PositionsChanges[1][i2].x, PositionsChanges[1][i2].y, PositionsChanges[1][i2].z);
                    ThreeAtomsPosList.Add(curAtomsPos);

                    if (ThreeAtomsPosList.Count >= Batch_size)
                    {
                        prev_i0 = i0;
                        prev_i1 = i1;
                        prev_i2 = i2 + 1;
                        return;
                    }
                }
            }
        }
        allPositionsUsed = true;
    }

    private void AtomMovingOld()
    {
        //print("Atoms to move: " + movingAtomsList.Count);
        float startTime = Time.timeSinceLevelLoad;
        float deltaCoeff = 0.001f;
        float delta_x1, delta_y1, delta_z1, delta_x2, delta_y2, delta_z2;

        switch (movingAtomsList.Count)
            {
            /*case 1:
                foreach (Vector3 newPosition0 in atoms[0].positionsList)
                {
                    energy = 0f;
                    foreach (Vector3 otherAtomPos in listOfNears[0])
                    {
                        energy += CalcEnergy(Vector3.Distance(newPosition0, otherAtomPos));
                    }
                    energy += 0.5f * Vector3.Distance(newPosition0, atPositions[0]);
                    if (energy < minEnergy)
                    {
                        minEnergy = energy;
                        bestPositions[0] = newPosition0;
                    }
                    iteration++;
                    if (StaticStorage.StopCalculation) yield return null;
                }
                atoms[0].atomObject.transform.position = bestPositions[0];
                break;

            case 2:
                for (int i0 = 0; i0 <= atoms[0].positionsList.Count; i0++)
                {
                    for (int i1 = 0; i1 <= atoms[1].positionsList.Count; i1++)
                    {
                        energy = 0f;
                        newPositions[0] = atoms[0].positionsList[i0];
                        newPositions[1] = atoms[1].positionsList[i1];
                        for (int i = 0; i <= 1; i++)
                        {
                            foreach (Vector3 otherAtomPos in listOfNears[i])
                            {
                                energy += CalcEnergy(Vector3.Distance(newPositions[i], otherAtomPos));
                            }
                            energy += 0.5f * Vector3.Distance(newPositions[i], atPositions[i]);
                        }
                        if (energy < minEnergy)
                        {
                            minEnergy = energy;
                            bestPositions[0] = newPositions[0];
                            bestPositions[1] = newPositions[1];
                        }
                        iteration++;
                        if (StaticStorage.StopCalculation) yield return null;
                    }
                }
                atoms[0].atomObject.transform.position = bestPositions[0];
                atoms[1].atomObject.transform.position = bestPositions[1];
                break;*/

            default:
                Atom curAtom = movingAtomsListWithNears[0];

                /*for (int k = 0; k < 100; k++)
                {
                    energy = 0f;
                    for (int j = 0; j < curAtom.nearAtoms.Count; j++)
                    {
                        Atom nearAtom = curAtom.nearAtoms[j];
                        CalcAndMove_2(curAtom, nearAtom);
                        energy += SW.Potential_2(curAtom.atomObject.transform.position, nearAtom.atomObject.transform.position, bondLength);
                    }
                    print("Energy = " + energy);
                }

                for (int k = 0; k < 100; k++)
                {
                    delta_x1 = 0; delta_y1 = 0; delta_z1 = 0;
                    delta_x2 = 0; delta_y2 = 0; delta_z2 = 0;
                    energy = 0f;
                    for (int i = 0; i < 1; i++)
                    {
                        curAtom = movingAtomsListWithNears[i];
                        Atom nearAtom1 = curAtom.nearAtoms[0];
                        Atom nearAtom2 = curAtom.nearAtoms[1];

                        CalcAndMove_3(curAtom, nearAtom1, nearAtom2);

                        energy += SW.Potential_3(curAtom.atomObject.transform.position, nearAtom1.atomObject.transform.position, nearAtom2.atomObject.transform.position, bondLength1, bondLength2);
                    }
                
                }
                print("Final y = " + curAtom.atomObject.transform.position.y);*/

                //print("----NEIGHBOURS MOVING----");

                for (int k = 0; k < 1000; k++)
                {
                    energy = 0f;
                    for (int i = 0; i < movingAtomsListWithNears.Count; i++)
                    {
                        delta_x1 = 0; delta_y1 = 0; delta_z1 = 0;
                        delta_x2 = 0; delta_y2 = 0; delta_z2 = 0;
                        curAtom = movingAtomsListWithNears[i];
                        /*if (k == 0)
                        print("i = " + i + ", curAtom.nearAtoms.Count = " + curAtom.nearAtoms.Count);*/
                        for (int j = 0; j < curAtom.nearAtoms.Count; j++)
                        {
                            Atom nearAtom = curAtom.nearAtoms[j];
                            
                            CalcAndMove_2(curAtom, nearAtom);
                            energy += SW.Potential_2(curAtom.atomObject.transform.position, nearAtom.atomObject.transform.position, bondLength);
                            //if (k == 0) print("bondLength = " + bondLength);
                        }

                        switch (curAtom.nearAtoms.Count)
                        {
                            case 2:
                                nearAtom1 = curAtom.nearAtoms[0];
                                nearAtom2 = curAtom.nearAtoms[1];
                                CalcAndMove_3(curAtom, nearAtom1, nearAtom2);
                                energy += SW.Potential_3(curAtom.atomObject.transform.position, nearAtom1.atomObject.transform.position, nearAtom2.atomObject.transform.position, bondLength1, bondLength2);
                                break;

                            case 3:
                                nearAtom1 = curAtom.nearAtoms[0];
                                nearAtom2 = curAtom.nearAtoms[1];
                                CalcAndMove_3(curAtom, nearAtom1, nearAtom2);
                                energy += SW.Potential_3(curAtom.atomObject.transform.position, nearAtom1.atomObject.transform.position, nearAtom2.atomObject.transform.position, bondLength1, bondLength2);

                                nearAtom1 = curAtom.nearAtoms[0];
                                nearAtom2 = curAtom.nearAtoms[2];
                                CalcAndMove_3(curAtom, nearAtom1, nearAtom2);
                                energy += SW.Potential_3(curAtom.atomObject.transform.position, nearAtom1.atomObject.transform.position, nearAtom2.atomObject.transform.position, bondLength1, bondLength2);

                                nearAtom1 = curAtom.nearAtoms[1];
                                nearAtom2 = curAtom.nearAtoms[2];
                                CalcAndMove_3(curAtom, nearAtom1, nearAtom2);
                                energy += SW.Potential_3(curAtom.atomObject.transform.position, nearAtom1.atomObject.transform.position, nearAtom2.atomObject.transform.position, bondLength1, bondLength2);
                                break;

                            case 4:
                                nearAtom1 = curAtom.nearAtoms[0];
                                nearAtom2 = curAtom.nearAtoms[1];
                                CalcAndMove_3(curAtom, nearAtom1, nearAtom2);
                                energy += SW.Potential_3(curAtom.atomObject.transform.position, nearAtom1.atomObject.transform.position, nearAtom2.atomObject.transform.position, bondLength1, bondLength2);

                                nearAtom1 = curAtom.nearAtoms[0];
                                nearAtom2 = curAtom.nearAtoms[2];
                                CalcAndMove_3(curAtom, nearAtom1, nearAtom2);
                                energy += SW.Potential_3(curAtom.atomObject.transform.position, nearAtom1.atomObject.transform.position, nearAtom2.atomObject.transform.position, bondLength1, bondLength2);

                                nearAtom1 = curAtom.nearAtoms[0];
                                nearAtom2 = curAtom.nearAtoms[3];
                                CalcAndMove_3(curAtom, nearAtom1, nearAtom2);
                                energy += SW.Potential_3(curAtom.atomObject.transform.position, nearAtom1.atomObject.transform.position, nearAtom2.atomObject.transform.position, bondLength1, bondLength2);

                                nearAtom1 = curAtom.nearAtoms[1];
                                nearAtom2 = curAtom.nearAtoms[2];
                                CalcAndMove_3(curAtom, nearAtom1, nearAtom2);
                                energy += SW.Potential_3(curAtom.atomObject.transform.position, nearAtom1.atomObject.transform.position, nearAtom2.atomObject.transform.position, bondLength1, bondLength2);

                                nearAtom1 = curAtom.nearAtoms[1];
                                nearAtom2 = curAtom.nearAtoms[3];
                                CalcAndMove_3(curAtom, nearAtom1, nearAtom2);
                                energy += SW.Potential_3(curAtom.atomObject.transform.position, nearAtom1.atomObject.transform.position, nearAtom2.atomObject.transform.position, bondLength1, bondLength2);

                                nearAtom1 = curAtom.nearAtoms[2];
                                nearAtom2 = curAtom.nearAtoms[3];
                                CalcAndMove_3(curAtom, nearAtom1, nearAtom2);
                                energy += SW.Potential_3(curAtom.atomObject.transform.position, nearAtom1.atomObject.transform.position, nearAtom2.atomObject.transform.position, bondLength1, bondLength2);
                                break;
                        }

                    }
                    //print("Energy = " + energy);
                    //if (Mathf.Abs(energy) < 1) break;
                }
                //print("Final y = " + curAtom.atomObject.transform.position.y);

                /*for (int i0 = 0; i0 < atoms[0].positionsList.Count; i0++)
                {
                   for (int i1 = 0; i1 < atoms[1].positionsList.Count; i1++)
                   {
                       for (int i2 = 0; i2 < atoms[2].positionsList.Count; i2++)
                       {
                           energy = 0f;
                           newPositions[0] = atoms[0].positionsList[i0];
                           newPositions[1] = atoms[1].positionsList[i1];
                           newPositions[2] = atoms[2].positionsList[i1];
                           for (int i = 0; i <= 2; i++)
                           {
                               foreach (Vector3 otherAtomPos in listOfNears[i])
                               {
                                   energy += CalcEnergy(Vector3.Distance(newPositions[i], otherAtomPos));
                               }
                               energy += 0.5f * Vector3.Distance(newPositions[i], atPositions[i]);
                           }
                           if (energy < minEnergy)
                           {
                               minEnergy = energy;
                               bestPositions[0] = newPositions[0];
                               bestPositions[1] = newPositions[1];
                               bestPositions[2] = newPositions[2];
                           }
                           iteration++;
                           if (StaticStorage.StopCalculation) yield return null;
                       }
                   }
               }*/
                startTime = Time.realtimeSinceStartup;

                /*for (int i0 = 0; i0 < ThreeAtomsPosList.Count; i0++)
                {
                    energy = 0f;
                    newPositions[0] = new Vector3(atoms[0].atomObject.transform.position.x + ThreeAtomsPosList[i0].Atom0_x,
                        atoms[0].atomObject.transform.position.y + ThreeAtomsPosList[i0].Atom0_y,
                        atoms[0].atomObject.transform.position.z + ThreeAtomsPosList[i0].Atom0_z);
                    newPositions[1] = new Vector3(atoms[1].atomObject.transform.position.x + ThreeAtomsPosList[i0].Atom1_x,
                        atoms[1].atomObject.transform.position.y + ThreeAtomsPosList[i0].Atom1_y,
                        atoms[1].atomObject.transform.position.z + ThreeAtomsPosList[i0].Atom1_z);
                    newPositions[2] = new Vector3(atoms[2].atomObject.transform.position.x + ThreeAtomsPosList[i0].Atom2_x,
                        atoms[2].atomObject.transform.position.y + ThreeAtomsPosList[i0].Atom2_y,
                        atoms[2].atomObject.transform.position.z + ThreeAtomsPosList[i0].Atom2_z);

                    if (i0 == 0)
                    {
                        print("newPositions[0] = " + newPositions[0]);
                        foreach (Vector3 otherAtomPos in listOfNears[0])
                            print("otherAtomPos = " + otherAtomPos);

                        print("newPositions[1] = " + newPositions[1]);
                        foreach (Vector3 otherAtomPos in listOfNears[1])
                            print("otherAtomPos = " + otherAtomPos);

                        print("newPositions[2] = " + newPositions[2]);
                        foreach (Vector3 otherAtomPos in listOfNears[2])
                            print("otherAtomPos = " + otherAtomPos);
                    }

                    energy += CalcEnergy(Vector3.Distance(newPositions[0], newPositions[1]));
                    energy += CalcEnergy(Vector3.Distance(newPositions[0], newPositions[2]));

                    for (int i = 1; i <= 2; i++)
                    {
                        foreach (Vector3 otherAtomPos in listOfNears[i])
                        {
                            energy += CalcEnergy(Vector3.Distance(newPositions[i], otherAtomPos));
                        }
                        energy += 0.5f * Vector3.Distance(newPositions[i], atPositions[i]);
                    }
                    resultEnergiesList.Add(energy);
                    if (energy < minEnergy)
                    {
                        minEnergy = energy;
                        bestPositions[0] = newPositions[0];
                        bestPositions[1] = newPositions[1];
                        bestPositions[2] = newPositions[2];
                    }
                    iteration++;
                }
                print(resultEnergiesList[0] + ", " + resultEnergiesList[12] + ", " + resultEnergiesList[1654] + ", " + resultEnergiesList[75558] + ", " + resultEnergiesList[49999999] + ", " + resultEnergiesList[ThreeAtomsPosList.Count - 2] + ", " + resultEnergiesList[ThreeAtomsPosList.Count - 1]);
                print("Time elapsed: " + (Time.realtimeSinceStartup - startTime).ToString());*/

                /*print("Copying lists");
                startTime = Time.realtimeSinceStartup;

                int initial_copyRange = 50000000;
                int alreadyCopied = 0;
                int index = 0;
                while (alreadyCopied < ThreeAtomsPosList.Count)
                {
                    int copyRange = Mathf.Min(initial_copyRange, ThreeAtomsPosList.Count - alreadyCopied);
                    ThreeAtomsPosListsForGPU.Add(ThreeAtomsPosList.GetRange((initial_copyRange) * index, copyRange));
                    alreadyCopied += copyRange;
                    index++;
                }*/
                //ThreeAtomsPosList.Clear();
                /*print("ThreeAtomsPosList[49999999]: {" + ThreeAtomsPosList[49999999].Atom0_x + ", " + ThreeAtomsPosList[49999999].Atom0_y + ", " + ThreeAtomsPosList[49999999].Atom0_z
                     + "}, {" + ThreeAtomsPosList[49999999].Atom1_x + ", " + ThreeAtomsPosList[49999999].Atom1_y + ", " + ThreeAtomsPosList[49999999].Atom1_z + "}, {" +
                     ThreeAtomsPosList[49999999].Atom2_x + ", " + ThreeAtomsPosList[49999999].Atom2_y + ", " + ThreeAtomsPosList[49999999].Atom2_z);
                print("ThreeAtomsPosListsForGPU[49999999]: {" + ThreeAtomsPosListsForGPU[0][49999999].Atom0_x + ", " + ThreeAtomsPosListsForGPU[0][49999999].Atom0_y + ", " + ThreeAtomsPosListsForGPU[0][49999999].Atom0_z
                     + "}, {" + ThreeAtomsPosListsForGPU[0][49999999].Atom1_x + ", " + ThreeAtomsPosListsForGPU[0][49999999].Atom1_y + ", " + ThreeAtomsPosListsForGPU[0][49999999].Atom1_z + "}, {" +
                     ThreeAtomsPosListsForGPU[0][49999999].Atom2_x + ", " + ThreeAtomsPosListsForGPU[0][49999999].Atom2_y + ", " + ThreeAtomsPosListsForGPU[0][49999999].Atom2_z);
                print("ThreeAtomsPosList[Count - 1]: {" + ThreeAtomsPosList[ThreeAtomsPosList.Count - 1].Atom0_x + ", " + ThreeAtomsPosList[ThreeAtomsPosList.Count - 1].Atom0_y + ", " + ThreeAtomsPosList[ThreeAtomsPosList.Count - 1].Atom0_z
                     + "}, {" + ThreeAtomsPosList[ThreeAtomsPosList.Count - 1].Atom1_x + ", " + ThreeAtomsPosList[ThreeAtomsPosList.Count - 1].Atom1_y + ", " + ThreeAtomsPosList[ThreeAtomsPosList.Count - 1].Atom1_z + "}, {" +
                     ThreeAtomsPosList[ThreeAtomsPosList.Count - 1].Atom2_x + ", " + ThreeAtomsPosList[ThreeAtomsPosList.Count - 1].Atom2_y + ", " + ThreeAtomsPosList[ThreeAtomsPosList.Count - 1].Atom2_z);
                print("ThreeAtomsPosList[Count - 2]: {" + ThreeAtomsPosList[ThreeAtomsPosList.Count - 2].Atom0_x + ", " + ThreeAtomsPosList[ThreeAtomsPosList.Count - 2].Atom0_y + ", " + ThreeAtomsPosList[ThreeAtomsPosList.Count - 2].Atom0_z
                     + "}, {" + ThreeAtomsPosList[ThreeAtomsPosList.Count - 2].Atom1_x + ", " + ThreeAtomsPosList[ThreeAtomsPosList.Count - 2].Atom1_y + ", " + ThreeAtomsPosList[ThreeAtomsPosList.Count - 2].Atom1_z + "}, {" +
                     ThreeAtomsPosList[ThreeAtomsPosList.Count - 2].Atom2_x + ", " + ThreeAtomsPosList[ThreeAtomsPosList.Count - 2].Atom2_y + ", " + ThreeAtomsPosList[ThreeAtomsPosList.Count - 2].Atom2_z);
                print("ThreeAtomsPosListsForGPU[Count - 1]: {" + ThreeAtomsPosListsForGPU[1][ThreeAtomsPosListsForGPU[1].Count - 1].Atom0_x + ", " + ThreeAtomsPosListsForGPU[1][ThreeAtomsPosListsForGPU[1].Count - 1].Atom0_y + ", " + ThreeAtomsPosListsForGPU[1][ThreeAtomsPosListsForGPU[1].Count - 1].Atom0_z
                     + "}, {" + ThreeAtomsPosList[ThreeAtomsPosList.Count - 1].Atom1_x + ", " + ThreeAtomsPosListsForGPU[1][ThreeAtomsPosListsForGPU[1].Count - 1].Atom1_y + ", " + ThreeAtomsPosListsForGPU[1][ThreeAtomsPosListsForGPU[1].Count - 1].Atom1_z + "}, {" +
                     ThreeAtomsPosListsForGPU[1][ThreeAtomsPosListsForGPU[1].Count - 1].Atom2_x + ", " + ThreeAtomsPosList[ThreeAtomsPosList.Count - 1].Atom2_y + ", " + ThreeAtomsPosList[ThreeAtomsPosList.Count - 1].Atom2_z);
                print("ThreeAtomsPosListsForGPU[Count - 2]: {" + ThreeAtomsPosListsForGPU[1][ThreeAtomsPosListsForGPU[1].Count - 2].Atom0_x + ", " + ThreeAtomsPosListsForGPU[1][ThreeAtomsPosListsForGPU[1].Count - 2].Atom0_y + ", " + ThreeAtomsPosListsForGPU[1][ThreeAtomsPosListsForGPU[1].Count - 2].Atom0_z
                     + "}, {" + ThreeAtomsPosList[ThreeAtomsPosList.Count - 2].Atom1_x + ", " + ThreeAtomsPosListsForGPU[1][ThreeAtomsPosListsForGPU[1].Count - 2].Atom1_y + ", " + ThreeAtomsPosListsForGPU[1][ThreeAtomsPosListsForGPU[1].Count - 2].Atom1_z + "}, {" +
                     ThreeAtomsPosListsForGPU[1][ThreeAtomsPosListsForGPU[1].Count - 2].Atom2_x + ", " + ThreeAtomsPosList[ThreeAtomsPosList.Count - 2].Atom2_y + ", " + ThreeAtomsPosList[ThreeAtomsPosList.Count - 2].Atom2_z);*/
                //print("Time elapsed: " + (Time.realtimeSinceStartup - startTime).ToString());

                //print("Calculation on GPU");
                startTime = Time.realtimeSinceStartup;

                /*foreach (List<ThreeAtomsPos> list in ThreeAtomsPosListsForGPU)
                {
                    print("Elements in list: " + list.Count);
                    InitShaderAndBuffers(list);
                    CalcEnergy_GPU(list);
                    resultEnergiesGPUList.AddRange(resultEnergiesGPU);
                }*/

                /*InitShaderAndBuffers(ThreeAtomsPosList);
                CalcEnergy_GPU(ThreeAtomsPosList);
                resultEnergiesGPUList = resultEnergiesGPU.ToList();

                print("resultEnergiesGPUList size: " + resultEnergiesGPUList.Count);
                print(resultEnergiesGPUList[0] + ", " + resultEnergiesGPUList[12] + ", " + resultEnergiesGPUList[1654] + ", " + resultEnergiesGPUList[75558] + ", " + resultEnergiesGPUList[resultEnergiesGPUList.Count - 487] + ", " + resultEnergiesGPUList[resultEnergiesGPUList.Count - 2] + ", " + resultEnergiesGPUList[resultEnergiesGPUList.Count - 1]);*/
                //print("Time elapsed: " + (Time.realtimeSinceStartup - startTime).ToString());

                /*atoms[0].atomObject.transform.position = bestPositions[0];
                atoms[1].atomObject.transform.position = bestPositions[1];
                atoms[2].atomObject.transform.position = bestPositions[2];*/
                break;
        }

        //print("startPosition = " + atPosition);
        //print("Time elapsed: " + (Time.timeSinceLevelLoad - startTime).ToString());
        //print("bestPosition = " + bestPosition[0]);
        //print("minEnergy = " + minEnergy);
        //yield return null;
    }
    
    public void CalcAndMove_2(Atom curAtom, Atom nearAtom)
    {
        float deltaCoeff = 0.001f;
        float delta_x1, delta_y1, delta_z1, delta_x2, delta_y2, delta_z2;

        if (curAtom.atomType == 0 && nearAtom.atomType == 0) bondLength = Parameters_Storage.Si_Si_bond_length;
        else if (curAtom.atomType == 1 && nearAtom.atomType == 1) bondLength = Parameters_Storage.Ge_Ge_bond_length;
        else bondLength = Parameters_Storage.Si_Ge_bond_length;

        //print("cur position: " + curAtom.atomObject.transform.position + ", near position: " + nearAtom.atomObject.transform.position);

        delta_x1 = -deltaCoeff * SW.Derivative_2(curAtom.atomObject.transform.position, nearAtom.atomObject.transform.position, bondLength, 1);
        delta_y1 = -deltaCoeff * SW.Derivative_2(curAtom.atomObject.transform.position, nearAtom.atomObject.transform.position, bondLength, 2);
        delta_z1 = -deltaCoeff * SW.Derivative_2(curAtom.atomObject.transform.position, nearAtom.atomObject.transform.position, bondLength, 3);
        /*delta_x2 = -deltaCoeff * SW.Derivative_2(curAtom.atomObject.transform.position, nearAtom.atomObject.transform.position, bondLength, 4);
        delta_y2 = -deltaCoeff * SW.Derivative_2(curAtom.atomObject.transform.position, nearAtom.atomObject.transform.position, bondLength, 5);
        delta_z2 = -deltaCoeff * SW.Derivative_2(curAtom.atomObject.transform.position, nearAtom.atomObject.transform.position, bondLength, 6);*/

        curAtom.atomObject.transform.position = new Vector3(
            curAtom.atomObject.transform.position.x + delta_x1,
            curAtom.atomObject.transform.position.y + delta_y1,
            curAtom.atomObject.transform.position.z + delta_z1);
        /*nearAtom.atomObject.transform.position = new Vector3(
            nearAtom.atomObject.transform.position.x + delta_x2,
            nearAtom.atomObject.transform.position.y + delta_y2,
            nearAtom.atomObject.transform.position.z + delta_z2);*/
        //print("Energy after = " + SW.Potential_2(curAtom.atomObject.transform.position, nearAtom.transform.position));
    }

    public void CalcAndMove_3(Atom curAtom, Atom nearAtom1, Atom nearAtom2)
    {
        float deltaCoeff = 0.001f;
        float delta_x1, delta_y1, delta_z1, delta_x2, delta_y2, delta_z2;

        if (curAtom.atomType == 0 && nearAtom1.atomType == 0) bondLength1 = Parameters_Storage.Si_Si_bond_length;
        else if (curAtom.atomType == 1 && nearAtom1.atomType == 1) bondLength1 = Parameters_Storage.Ge_Ge_bond_length;
        else bondLength1 = Parameters_Storage.Si_Ge_bond_length;

        if (curAtom.atomType == 0 && nearAtom2.atomType == 0) bondLength2 = Parameters_Storage.Si_Si_bond_length;
        else if (curAtom.atomType == 1 && nearAtom2.atomType == 1) bondLength2 = Parameters_Storage.Ge_Ge_bond_length;
        else bondLength2 = Parameters_Storage.Si_Ge_bond_length;

        delta_x1 = -deltaCoeff * SW.Derivative_3(curAtom.atomObject.transform.position, nearAtom1.atomObject.transform.position, nearAtom2.atomObject.transform.position, bondLength1, bondLength2, 1);
        delta_y1 = -deltaCoeff * SW.Derivative_3(curAtom.atomObject.transform.position, nearAtom1.atomObject.transform.position, nearAtom2.atomObject.transform.position, bondLength1, bondLength2, 2);
        delta_z1 = -deltaCoeff * SW.Derivative_3(curAtom.atomObject.transform.position, nearAtom1.atomObject.transform.position, nearAtom2.atomObject.transform.position, bondLength1, bondLength2, 3);

        //print("delta_x1 = " + delta_x1 + ", delta_y1 = " + delta_y1 + ", delta_z1 = " + delta_z1);
        //print("bondLength1 = " + bondLength1 + ", bondLength2 = " + bondLength2 + ", bondLength3 = " + bondLength3);

        curAtom.atomObject.transform.position = new Vector3(
                    curAtom.atomObject.transform.position.x + delta_x1,
                    curAtom.atomObject.transform.position.y + delta_y1,
                    curAtom.atomObject.transform.position.z + delta_z1);
    }

        public float CalcEnergy(float dist)
    {
        float energy = Mathf.Abs(Mathf.Pow(dist / Parameters_Storage.Si_Ge_bond_length, 12) - Mathf.Pow(dist / Parameters_Storage.Si_Ge_bond_length, 6));
        return energy;
    }
    private void CreatePositionChanges()
    {
        for (int i = 0; i <= Parameters_Storage.Strain_Depth; i++)
        {
            List<Vector3> posList = new List<Vector3>();
            float X_min = -Parameters_Storage.Max_Displacement[i];
            float X_max = Parameters_Storage.Max_Displacement[i];
            float X_step = (X_max - X_min) / Parameters_Storage.Steps_Number[i];
            float Y_min = -Parameters_Storage.Max_Displacement[i];
            float Y_max = Parameters_Storage.Max_Displacement[i];
            float Y_step = (Y_max - Y_min) / Parameters_Storage.Steps_Number[i];
            float Z_min = -Parameters_Storage.Max_Displacement[i];
            float Z_max = Parameters_Storage.Max_Displacement[i];
            float Z_step = (Z_max - Z_min) / Parameters_Storage.Steps_Number[i];
            for (float x = X_min; x <= X_max; x += X_step)
            {
                for (float z = Z_min; z <= Z_max; z += Z_step)
                {
                    for (float y = Y_min; y <= Y_max; y += Y_step)
                    {
                        posList.Add(new Vector3(x, y, z));
                    }
                }
            }
            PositionsChanges.Add(posList);
        }
    }

    private void InitShaderAndBuffers(List<ThreeAtomsPos> Cur_ThreeAtomsPosList)
    {
        _shader = Resources.Load<ComputeShader>("ComputeShader");
        kernel = _shader.FindKernel("ShaderCalc");
        PosIncrementArr_Buf = new ComputeBuffer(Cur_ThreeAtomsPosList.Count, sizeof(float) * 9);
        Atom0_1_2_coords_Buf = new ComputeBuffer(9, sizeof(float));
        Atom1_1_coords_Buf = new ComputeBuffer(3, sizeof(float));
        Atom1_2_coords_Buf = new ComputeBuffer(3, sizeof(float));
        Atom1_3_coords_Buf = new ComputeBuffer(3, sizeof(float));
        Atom2_1_coords_Buf = new ComputeBuffer(3, sizeof(float));
        Atom2_2_coords_Buf = new ComputeBuffer(3, sizeof(float));
        Atom2_3_coords_Buf = new ComputeBuffer(3, sizeof(float));
        resultBuf = new ComputeBuffer(Cur_ThreeAtomsPosList.Count, sizeof(float));
        resultEnergiesGPU = new float[Cur_ThreeAtomsPosList.Count];

        PosIncrementArr_Buf.SetData(Cur_ThreeAtomsPosList.ToArray());
        Atom0_1_2_coords_Buf.SetData(new float[] { atoms[0].atomObject.transform.position.x, atoms[0].atomObject.transform.position.y, atoms[0].atomObject.transform.position.z,
        atoms[1].atomObject.transform.position.x, atoms[1].atomObject.transform.position.y, atoms[1].atomObject.transform.position.z,
        atoms[2].atomObject.transform.position.x, atoms[2].atomObject.transform.position.y, atoms[2].atomObject.transform.position.z });
        Atom1_1_coords_Buf.SetData(new float[] { atoms[1].nearAtoms[0].atomObject.transform.position.x, atoms[1].nearAtoms[0].atomObject.transform.position.y, atoms[1].nearAtoms[0].atomObject.transform.position.z });
        Atom1_2_coords_Buf.SetData(new float[] { atoms[1].nearAtoms[1].atomObject.transform.position.x, atoms[1].nearAtoms[1].atomObject.transform.position.y, atoms[1].nearAtoms[1].atomObject.transform.position.z });
        Atom2_1_coords_Buf.SetData(new float[] { atoms[2].nearAtoms[0].atomObject.transform.position.x, atoms[2].nearAtoms[0].atomObject.transform.position.y, atoms[2].nearAtoms[0].atomObject.transform.position.z });
        Atom2_2_coords_Buf.SetData(new float[] { atoms[2].nearAtoms[1].atomObject.transform.position.x, atoms[2].nearAtoms[1].atomObject.transform.position.y, atoms[2].nearAtoms[1].atomObject.transform.position.z });

        _shader.SetBuffer(kernel, "PosIncrementArr", PosIncrementArr_Buf);
        _shader.SetBuffer(kernel, "Atom0_1_2_coords", Atom0_1_2_coords_Buf);
        _shader.SetBuffer(kernel, "Atom1_1_coords", Atom1_1_coords_Buf);
        _shader.SetBuffer(kernel, "Atom1_2_coords", Atom1_2_coords_Buf);
        _shader.SetBuffer(kernel, "Atom2_1_coords", Atom2_1_coords_Buf);
        _shader.SetBuffer(kernel, "Atom2_2_coords", Atom2_2_coords_Buf);
        _shader.SetBuffer(kernel, "result", resultBuf);

        /*print("atoms[0].atomObject.transform.position = " + atoms[0].atomObject.transform.position);

        print("atoms[1].atomObject.transform.position = " + atoms[1].atomObject.transform.position);
        print("Atom1_1_coords = " + atoms[1].nearAtoms[0].transform.position.x + ", " + atoms[1].nearAtoms[0].transform.position.y + ", " + atoms[1].nearAtoms[0].transform.position.z);
        print("Atom1_2_coords = " + atoms[1].nearAtoms[1].transform.position.x + ", " + atoms[1].nearAtoms[1].transform.position.y + ", " + atoms[1].nearAtoms[1].transform.position.z);

        print("atoms[2].atomObject.transform.position = " + atoms[2].atomObject.transform.position);
        print("Atom2_1_coords = " + atoms[2].nearAtoms[0].transform.position.x + ", " + atoms[2].nearAtoms[0].transform.position.y + ", " + atoms[2].nearAtoms[0].transform.position.z);
        print("Atom2_2_coords = " + atoms[2].nearAtoms[1].transform.position.x + ", " + atoms[2].nearAtoms[1].transform.position.y + ", " + atoms[2].nearAtoms[1].transform.position.z);*/
    }
    public void CalcEnergy_GPU(List<ThreeAtomsPos> Cur_ThreeAtomsPosList)
    {
        
        float shaderCalls = Cur_ThreeAtomsPosList.Count / 1000f;
        print("shaderCalls = " + shaderCalls);
        _shader.Dispatch(kernel, Mathf.CeilToInt(shaderCalls), 1, 1);
        resultBuf.GetData(resultEnergiesGPU);

        PosIncrementArr_Buf.Release();
        resultBuf.Release();
        Atom0_1_2_coords_Buf.Release();
        Atom1_1_coords_Buf.Release();
        Atom1_2_coords_Buf.Release();
        Atom1_3_coords_Buf.Release();
        Atom2_1_coords_Buf.Release();
        Atom2_2_coords_Buf.Release();
        Atom2_3_coords_Buf.Release();

        //print("Time elapsed: " + (Time.realtimeSinceStartup - startTime).ToString());

    }
}
