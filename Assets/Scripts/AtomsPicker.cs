using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AtomsPicker : MonoBehaviour
{
    [SerializeField] private GameObject CameraController;
    [SerializeField] private Camera cam;
    private float mouse_x;
    private float mouse_y;
    private RectTransform rect;
    private float dist2D, minDist2D;
    private GameObject newTarget;
    // Start is called before the first frame update
    void Start()
    {
        //cam = GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            minDist2D = 99999f;
            Vector2 mousePos = Input.mousePosition;
            mouse_x = mousePos.x;
            mouse_y = mousePos.y;

            Transform prevTarget = CameraController.GetComponent<MSCameraController>().target;
            Vector3 prevPosition = prevTarget.position;

            foreach (GameObject atomObject in StaticStorage.SubstrateAtoms_Objects)
            {
                Vector3 atomPos = atomObject.transform.position;
                Vector2 atomPos2D = cam.WorldToScreenPoint(atomPos);
                dist2D = Vector2.Distance(mousePos, atomPos2D);
                if (dist2D < minDist2D)
                {
                    minDist2D = dist2D;
                    newTarget = atomObject;
                }
            }

            foreach (Atom atom in StaticStorage.DepositedAtoms)
            {
                Vector3 atomPos = atom.atomObject.transform.position;
                Vector2 atomPos2D = cam.WorldToScreenPoint(atomPos);
                dist2D = Vector2.Distance(mousePos, atomPos2D);
                if (dist2D < minDist2D)
                {
                    minDist2D = dist2D;
                    newTarget = atom.atomObject;
                }
            }

            CameraController.GetComponent<MSCameraController>().target = newTarget.transform;
        }
    }
}
