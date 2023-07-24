using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SW
{
    private static float A = 6.865f;
    private static float B = 0.611f;
    private static float p = 4f;
    private static float q = 0f;
    private static float a = 3.77f;
    private static float L = 9.11f;
    private static float G = 1.2f;
    private static float Q0 = 1.9106328761f;
    private static float r, r12, r13, r23;
    private static float Q123, Q132, Q213;
    private static float h123, h132, h213;
    public static float Potential_2(Vector3 vector1, Vector3 vector2, float bondLength)
    {
        r = Vector3.Distance(vector1, vector2) / bondLength;
        float f = A * (B * Mathf.Pow(r, -p) - Mathf.Pow(r, -q)) * Mathf.Exp(1 / (r - a));
        return (f);
    }

    public static float Potential_3(Vector3 vector1, Vector3 vector2, Vector3 vector3, float bondLength12, float bondLength13/*, float bondLength23*/)
    {
        r12 = Vector3.Distance(vector1, vector2);
        r13 = Vector3.Distance(vector1, vector3);
        //r23 = Vector3.Distance(vector2, vector3);
        float x1 = vector1.x;
        float y1 = vector1.y;
        float z1 = vector1.z;
        float x2 = vector2.x;
        float y2 = vector2.y;
        float z2 = vector2.z;
        float x3 = vector3.x;
        float y3 = vector3.y;
        float z3 = vector3.z;

        /*Q123 = Mathf.Acos(((x1 - x2) * (x3 - x2) + (y1 - y2) * (y3 - y2) + (z1 - z2) * (z3 - z2)) / (r12 * r23));
        Q132 = Mathf.Acos(((x1 - x3) * (x2 - x3) + (y1 - y3) * (y2 - y3) + (z1 - z3) * (z2 - z3)) / (r13 * r23));*/
        Q213 = Mathf.Acos(((x2 - x1) * (x3 - x1) + (y2 - y1) * (y3 - y1) + (z2 - z1) * (z3 - z1)) / (r12 * r13));

        /*h123 = L * Mathf.Exp(G / (r12 / bondLength12 - a) + G / (r23 / bondLength23 - a)) * Mathf.Cos(Q123 - 1 / 3);
        h132 = L * Mathf.Exp(G / (r13 / bondLength13 - a) + G / (r23 / bondLength23 - a)) * Mathf.Cos(Q132 - 1 / 3);*/
        h213 = L * Mathf.Exp(G / (r12 / bondLength12 - a) + G / (r13 / bondLength13 - a)) * Mathf.Cos(Q213 - 1 / 3);

        float f = /*h123 + h132 + */h213;
        return (f);
    }

    public static float Derivative_2(Vector3 vector1, Vector3 vector2, float bondLength, int varNum)
    {
        r = Vector3.Distance(vector1, vector2) / bondLength;
        float x1 = vector1.x;
        float y1 = vector1.y;
        float z1 = vector1.z;
        float x2 = vector2.x;
        float y2 = vector2.y;
        float z2 = vector2.z;
        float res = 0;

        switch (varNum)
        {
            case 1:
                res = A * Mathf.Exp(1 / (-a + r)) * ((-B * p * (x1 - x2) * Mathf.Pow(r, -2 - p) + q * (x1 - x2) * Mathf.Pow(r, -2 - q)) - B * (x1 - x2)) * (Mathf.Pow(r, -p) - Mathf.Pow(r, -q)) / (Mathf.Pow(-a + r, 2) * r);
                break;

            case 2:
                res = A * Mathf.Exp(1 / (-a + r)) * ((-B * p * (y1 - y2) * Mathf.Pow(r, -2 - p) + q * (y1 - y2) * Mathf.Pow(r, -2 - q)) - B * (y1 - y2)) * (Mathf.Pow(r, -p) - Mathf.Pow(r, -q)) / (Mathf.Pow(-a + r, 2) * r);
                break;

            case 3:
                res = A * Mathf.Exp(1 / (-a + r)) * ((-B * p * (z1 - z2) * Mathf.Pow(r, -2 - p) + q * (z1 - z2) * Mathf.Pow(r, -2 - q)) - B * (z1 - z2)) * (Mathf.Pow(r, -p) - Mathf.Pow(r, -q)) / (Mathf.Pow(-a + r, 2) * r);
                break;

            case 4:
                res = A * Mathf.Exp(1 / (-a + r)) * ((-B * p * (x2 - x1) * Mathf.Pow(r, -2 - p) + q * (x2 - x1) * Mathf.Pow(r, -2 - q)) - B * (x2 - x1)) * (Mathf.Pow(r, -p) - Mathf.Pow(r, -q)) / (Mathf.Pow(-a + r, 2) * r);
                break;

            case 5:
                res = A * Mathf.Exp(1 / (-a + r)) * ((-B * p * (y2 - y1) * Mathf.Pow(r, -2 - p) + q * (y2 - y1) * Mathf.Pow(r, -2 - q)) - B * (y2 - y1)) * (Mathf.Pow(r, -p) - Mathf.Pow(r, -q)) / (Mathf.Pow(-a + r, 2) * r);
                break;

            case 6:
                res = A * Mathf.Exp(1 / (-a + r)) * ((-B * p * (z2 - z1) * Mathf.Pow(r, -2 - p) + q * (z2 - z1) * Mathf.Pow(r, -2 - q)) - B * (z2 - z1)) * (Mathf.Pow(r, -p) - Mathf.Pow(r, -q)) / (Mathf.Pow(-a + r, 2) * r);
                break;
        }

        return (res);
    }

    public static float Derivative_3(Vector3 vector1, Vector3 vector2, Vector3 vector3, float bondLength12, float bondLength13, /*float bondLength23, */int varNum)
    {
        r12 = Vector3.Distance(vector1, vector2);
        r13 = Vector3.Distance(vector1, vector3);
        r23 = Vector3.Distance(vector2, vector3);
        float x1 = vector1.x;
        float y1 = vector1.y;
        float z1 = vector1.z;
        float x2 = vector2.x;
        float y2 = vector2.y;
        float z2 = vector2.z;
        float x3 = vector3.x;
        float y3 = vector3.y;
        float z3 = vector3.z;
        float res = 0;
        float D_h123_1 = 0, D_h123_2 = 0, D_h132_1 = 0, D_h132_2 = 0, D_h213_1 = 0, D_h213_2 = 0;

        /*float Mult123 = (x1 - x2) * (x3 - x2) + (y1 - y2) * (y3 - y2) + (z1 - z2) * (z3 - z2);
        float Mult132 = (x1 - x3) * (x2 - x3) + (y1 - y3) * (y2 - y3) + (z1 - z3) * (z2 - z3);*/
        float Mult213 = (x2 - x1) * (x3 - x1) + (y2 - y1) * (y3 - y1) + (z2 - z1) * (z3 - z1);

        /*Q123 = Mathf.Acos(Mult123 / (r12 * r23));
        Q132 = Mathf.Acos(Mult132 / (r13 * r23));*/
        Q213 = Mathf.Acos(Mult213 / (r12 * r13));

       /* float h123_1 = L * Mathf.Exp(G / (r12 / bondLength12 - a) + G / (r23 / bondLength23 - a));
        float h123_2 = Mathf.Cos(Q123 - 1 / 3);
        float h132_1 = L * Mathf.Exp(G / (r13 / bondLength13 - a) + G / (r23 / bondLength23 - a));
        float h132_2 = Mathf.Cos(Q132 - 1 / 3);*/
        float h213_1 = L * Mathf.Exp(G / (r12 / bondLength12 - a) + G / (r13 / bondLength13 - a));
        float h213_2 = Mathf.Cos(Q213 - Q0);

        //Debug.Log("Mult123 = " + Mult123 + ", Q123 = " + Q123 + ", h123_1 = " + h123_1 + ", h123_2 = " + h123_2 + ", Mathf.Acos = " + Mathf.Acos(Mult123 / (r12 * r23)));
        //Debug.Log("Q123 = " + Q123 + ", Q132 = " + Q132 + ", Q213 = " + Q213);
        //Debug.Log("Mult123 = " + Mult123 + ", Mult132 = " + Mult132 + ", Mult213 = " + Mult213);
        //Debug.Log("r12 * r23 = " + (r12 * r23) + ", r13 * r23 = " + (r13 * r23) + ", r12 * r13 = " + (r12 * r13));

        /*h123 = h123_1 * h123_2;
        h132 = h132_1 * h132_2;*/
        h213 = h213_1 * h213_2;

        switch (varNum)
        {
            case 1:
                /*D_h123_1 = -G * L * (x1 - x2) * Mathf.Exp(G / ((-a + r12) / bondLength12) + G / ((-a + r23) / bondLength23)) / (bondLength12 * (r12 * Mathf.Pow(-a + r12 / bondLength12, 2)));
                D_h123_2 = -2 * ((x3 - x2) / (r12 * r23) - (x1 - x2) * Mult123 / (Mathf.Pow(r12, 3) * r23))
                    * Mathf.Cos(1 / 3 - Q123) * Mathf.Sin(1 / 3 - Q123)
                    * Mathf.Sqrt(1 - Mathf.Pow(Mult123, 2) / (Mathf.Pow(r12, 2) * Mathf.Pow(r23, 2)));

                D_h132_1 = -G * L * (x1 - x3) * Mathf.Exp(G / ((-a + r13) / bondLength13) + G / ((-a + r23) / bondLength23)) / (bondLength13 * (r13 * Mathf.Pow(-a + r13 / bondLength13, 2)));
                D_h132_2 = -2 * ((x2 - x3) / (r13 * r23) - (x1 - x3) * Mult132 / (Mathf.Pow(r13, 3) * r23))
                    * Mathf.Cos(1 / 3 - Q132) * Mathf.Sin(1 / 3 - Q132)
                    * Mathf.Sqrt(1 - Mathf.Pow(Mult132, 2) / (Mathf.Pow(r13, 2) * Mathf.Pow(r23, 2)));*/

                D_h213_1 = Mathf.Exp(G / ((-a + r12) / bondLength12) + G / ((-a + r13) / bondLength13)) * L
                    * (-G * (x1 - x2) / (bondLength12 * Mathf.Pow(-a + r12 / bondLength12, 2) * r12) - G * (x1 - x3) / (bondLength13 * Mathf.Pow(-a + r13 / bondLength13, 2) * r13));
                D_h213_2 = -2 * ((2 * x1 - x2 - x3) / (r12 * r13) - (x1 - x3) * Mult213 / (r12 * Mathf.Pow(r13, 3)) - (x1 - x2) * Mult213 / (r13 * Mathf.Pow(r12, 3)))
                    * Mathf.Cos(Q213 - Q0) * Mathf.Sin(Q213 - Q0)
                    * Mathf.Sqrt(1 - Mathf.Pow(Mult213, 2) / (Mathf.Pow(r13, 2) * Mathf.Pow(r23, 2)));
                //Debug.Log("case 1: " + "D_h123_1 = " + D_h123_1 + ", D_h123_2 = " + D_h123_2 + ", D_h132_1 = " + D_h132_1 + ", D_h132_2 = " + D_h132_2 + ", D_h213_1 = " + D_h213_1 + ", D_h213_2 = " + D_h213_2);
                //Debug.Log("x3 - x2 = " + (x3 - x2));
                //Debug.Log("x1 - x2 = " + (x1 - x2));
                //Debug.Log("2 * x1 - x2 - x3 = " + (2 * x1 - x2 - x3));
                break;

            case 2:
                /*D_h123_1 = -G * L * (y1 - y2) * Mathf.Exp(G / ((-a + r12) / bondLength12) + G / ((-a + r23) / bondLength23)) / (bondLength12 * (r12 * Mathf.Pow(-a + r12 / bondLength12, 2)));
                D_h123_2 = -2 * ((y3 - y2) / (r12 * r23) - (y1 - y2) * Mult123 / (Mathf.Pow(r12, 3) * r23))
                    * Mathf.Cos(1 / 3 - Q123) * Mathf.Sin(1 / 3 - Q123)
                    * Mathf.Sqrt(1 - Mathf.Pow(Mult123, 2) / (Mathf.Pow(r12, 2) * Mathf.Pow(r23, 2)));

                D_h132_1 = -G * L * (y1 - y3) * Mathf.Exp(G / ((-a + r13) / bondLength13) + G / ((-a + r23) / bondLength23)) / (bondLength13 * (r13 * Mathf.Pow(-a + r13 / bondLength13, 2)));
                D_h132_2 = -2 * ((y2 - y3) / (r13 * r23) - (y1 - y3) * Mult132 / (Mathf.Pow(r13, 3) * r23))
                    * Mathf.Cos(1 / 3 - Q132) * Mathf.Sin(1 / 3 - Q132)
                    * Mathf.Sqrt(1 - Mathf.Pow(Mult132, 2) / (Mathf.Pow(r13, 2) * Mathf.Pow(r23, 2)));*/

                D_h213_1 = Mathf.Exp(G / ((-a + r12) / bondLength12) + G / ((-a + r13) / bondLength13)) * L
                    * (-G * (y1 - y2) / (bondLength12 * Mathf.Pow(-a + r12 / bondLength12, 2) * r12) - G * (y1 - y3) / (bondLength13 * Mathf.Pow(-a + r13 / bondLength13, 2) * r13));
                D_h213_2 = -2 * ((2 * y1 - y2 - y3) / (r12 * r13) - (y1 - y3) * Mult213 / (r12 * Mathf.Pow(r13, 3)) - (y1 - y2) * Mult213 / (r13 * Mathf.Pow(r12, 3)))
                    * Mathf.Cos(Q213 - Q0) * Mathf.Sin(Q213 - Q0)
                    * Mathf.Sqrt(1 - Mathf.Pow(Mult213, 2) / (Mathf.Pow(r13, 2) * Mathf.Pow(r23, 2)));
                //Debug.Log("case 2: " + "D_h123_1 = " + D_h123_1 + ", D_h123_2 = " + D_h123_2 + ", D_h132_1 = " + D_h132_1 + ", D_h132_2 = " + D_h132_2 + ", D_h213_1 = " + D_h213_1 + ", D_h213_2 = " + D_h213_2);
                //Debug.Log("y3 - y2 = " + (y3 - y2));
                //Debug.Log("y1 - y2 = " + (y1 - y2));
                //Debug.Log("2 * y1 - y2 - y3 = " + (2 * y1 - y2 - y3));
                break;

            case 3:
                /*D_h123_1 = -G * L * (z1 - z2) * Mathf.Exp(G / ((-a + r12) / bondLength12) + G / ((-a + r23) / bondLength23)) / (bondLength12 * (r12 * Mathf.Pow(-a + r12 / bondLength12, 2)));
                D_h123_2 = -2 * ((z3 - z2) / (r12 * r23) - (z1 - z2) * Mult123 / (Mathf.Pow(r12, 3) * r23))
                    * Mathf.Cos(1 / 3 - Q123) * Mathf.Sin(1 / 3 - Q123)
                    * Mathf.Sqrt(1 - Mathf.Pow(Mult123, 2) / (Mathf.Pow(r12, 2) * Mathf.Pow(r23, 2)));

                D_h132_1 = -G * L * (z1 - z3) * Mathf.Exp(G / ((-a + r13) / bondLength13) + G / ((-a + r23) / bondLength23)) / (bondLength13 * (r13 * Mathf.Pow(-a + r13 / bondLength13, 2)));
                D_h132_2 = -2 * ((z2 - z3) / (r13 * r23) - (z1 - z3) * Mult132 / (Mathf.Pow(r13, 3) * r23))
                    * Mathf.Cos(1 / 3 - Q132) * Mathf.Sin(1 / 3 - Q132)
                    * Mathf.Sqrt(1 - Mathf.Pow(Mult132, 2) / (Mathf.Pow(r13, 2) * Mathf.Pow(r23, 2)));*/

                D_h213_1 = Mathf.Exp(G / ((-a + r12) / bondLength12) + G / ((-a + r13) / bondLength13)) * L
                    * (-G * (z1 - z2) / (bondLength12 * Mathf.Pow(-a + r12 / bondLength12, 2) * r12) - G * (z1 - z3) / (bondLength13 * Mathf.Pow(-a + r13 / bondLength13, 2) * r13));
                D_h213_2 = -2 * ((2 * z1 - z2 - z3) / (r12 * r13) - (z1 - z3) * Mult213 / (r12 * Mathf.Pow(r13, 3)) - (z1 - z2) * Mult213 / (r13 * Mathf.Pow(r12, 3)))
                    * Mathf.Cos(Q213 - Q0) * Mathf.Sin(Q213 - Q0)
                    * Mathf.Sqrt(1 - Mathf.Pow(Mult213, 2) / (Mathf.Pow(r13, 2) * Mathf.Pow(r23, 2)));
                //Debug.Log("case 3: " + "D_h123_1 = " + D_h123_1 + ", D_h123_2 = " + D_h123_2 + ", D_h132_1 = " + D_h132_1 + ", D_h132_2 = " + D_h132_2 + ", D_h213_1 = " + D_h213_1 + ", D_h213_2 = " + D_h213_2);
                break;
        }



        /*float D_h123 = D_h123_1 * h123_2 + D_h123_2 * h123_1;
        float D_h132 = D_h132_1 * h132_2 + D_h132_2 * h132_1;*/
        float D_h213 = D_h213_1 * h213_2 + D_h213_2 * h213_1;

        res = /*D_h123 + D_h132 + */D_h213;

        return (res);
    }
}
