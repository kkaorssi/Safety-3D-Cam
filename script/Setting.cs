using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;


public class Setting : MonoBehaviour
{
    public InputField warn_Input;
    public InputField stop_Input;

    public InputField O_X;
    public InputField O_Y;
    public InputField O_Z;

    public static double warn_distance;
    public static double stop_distance;

    public static double O_XL;
    public static double O_YL;
    public static double O_ZL;

    private void Awake()
    {
        //DontDestroyOnLoad(gameObject);
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Tool_Mode()
    {

        warn_distance = Convert.ToDouble(warn_Input.text);
        stop_distance = Convert.ToDouble(stop_Input.text);

        O_XL = Convert.ToDouble(O_X.text);
        O_YL = Convert.ToDouble(O_Y.text);
        O_ZL = Convert.ToDouble(O_Z.text);


        if (warn_distance <= stop_distance)
        {

        }
        else
        {
            SceneManager.LoadScene("Cobot_mode");
        }
    }

    public void Center_Mode()
    {

        warn_distance = Convert.ToDouble(warn_Input.text);
        stop_distance = Convert.ToDouble(stop_Input.text);

        O_XL = Convert.ToDouble(O_X.text);
        O_YL = Convert.ToDouble(O_Y.text);
        O_ZL = Convert.ToDouble(O_Z.text);

        if (warn_distance <= stop_distance)
        {

        }
        else
        {
            SceneManager.LoadScene("skeleton hand tracker");
        }

    }
}
