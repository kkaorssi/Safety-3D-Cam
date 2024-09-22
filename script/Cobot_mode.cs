using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
public class Cobot_mode : MonoBehaviour
{


    enum jointnum : int
    {
        Head = 0,
        Neck = 1,
        Left_Collar = 2,
        Torso = 3,
        Waist = 4,
        Left_Shoulder = 5,
        Right_Shoulder = 6,
        Left_Elbow = 7,
        Right_Elbow = 8,
        Left_Wrist = 9,
        Right_Wrist = 10,
        Left_Hand = 11,
        Right_Hand = 12,
        Left_Hip = 13,
        Right_Hip = 14,
        Left_Knee = 15,
        Right_Knee = 16,
        Left_Ankle = 17,
        Right_Ankle = 18
    };

    //시각화를 위한 변수 설정
    public Text timerText;
    //시작 시간 변수
    private float startTime;

    //시각화를 위한 거리 변수
    public Text Visual_Distance;
    public Text Visual_Stop;
    public Text Visual_Warn;

    //스켈레톤 감지 여부를 알려주는 텍스트 변수
    public Text Detected;


    //nuitrackSDK를 사용하여 JOINT 오브젝트 변수 생성
    public nuitrack.JointType[] typeJoint;
    GameObject[] CreatedJoint;
    public GameObject PrefabJoint;

    double[] O_ToolLocation;
    double[] C_ToolLocation;
    private Vector3 posit;

    // joint to Tool distance
    double[] JointWarnDistance;

    double[] O_Location;

    double distance = 0;

    // safe : 0 warn : 1 caution : 2 stop : 3
    int state = 0;

    //Socket variable
    Socket sock;
    byte[] rec;

    static readonly object locker = new object();

    double warn_d = 0;
    double stop_d = 0;

    public SpriteRenderer SR_Lhand;
    public SpriteRenderer SR_Rhand;
    public SpriteRenderer SR_head;
    public SpriteRenderer SR_Lshoulder;
    public SpriteRenderer SR_Rshoulder;

    public SpriteRenderer SR_Robot;

    public Transform T_Lhand;
    public Transform T_Rhand;
    public Transform T_head;
    public Transform T_Lshoulder;
    public Transform T_Rshoulder;


    float x_Lhand = 472f;
    float x_Rhand = 472f;
    float x_head = 472f;
    float x_Lshoulder = 472f;
    float x_Rshoulder = 472f;

    // Use this for initialization
    void Start()
    {
        SR_Lhand.color = new Color(0, 1, 0, 1);
        SR_Rhand.color = new Color(0, 1, 0, 1);
        SR_head.color = new Color(0, 1, 0, 1);
        SR_Rshoulder.color = new Color(0, 1, 0, 1);
        SR_Lshoulder.color = new Color(0, 1, 0, 1);

        SR_Robot.color = new Color(1, 1, 1, 1);

        warn_d = Setting.warn_distance;
        stop_d = Setting.stop_distance;

        Visual_Stop.text = stop_d.ToString();
        Visual_Warn.text = warn_d.ToString();

        //조인트 생성
        CreatedJoint = new GameObject[typeJoint.Length];

        //시작시간 변수 생성
        startTime = Time.time;

        JointWarnDistance = new double[typeJoint.Length];

        C_ToolLocation = new double[3];
        O_ToolLocation = new double[3];
        O_Location = new double[3];

        //O_tool Location ### streaming data
        O_ToolLocation[0] = 0; //x
        O_ToolLocation[1] = 0; //y
        O_ToolLocation[2] = 0; //z

        //Camera_tool Location
        C_ToolLocation[0] = 0; //x
        C_ToolLocation[1] = 0; //y
        C_ToolLocation[2] = 0; //z

        //Robot_O Location
        O_Location[0] = Setting.O_XL;
        O_Location[1] = Setting.O_YL;
        O_Location[2] = Setting.O_ZL;

        
        //부위별 오브젝트 생성
        for (int q = 0; q < typeJoint.Length; q++)
        {
            CreatedJoint[q] = Instantiate(PrefabJoint);
            CreatedJoint[q].transform.SetParent(transform);
        }

        ThreadStart th = new ThreadStart(socket_connect);
        Thread t = new Thread(th);
        t.Start();

    }


    // Update is called once per frame
    void Update()
    {

        float ProcessTime = Time.time - startTime;

        string minutes = ((int)ProcessTime / 60).ToString();
        string seconds = (ProcessTime % 60).ToString("f2");

        //시간 텍스트 시각화

        timerText.text = "TIMER  " + minutes + ":" + seconds;

        C_ToolLocation = tool(O_Location);


        //스켈레톤이 인식되었을때 조건문
        if (CurrentUserTracker.CurrentUser != 0)
        {
            //스켈레톤이 인식 될 경우 텍스트 시각화
            Detected.text = "<color=#00ff00>" + "Detected" + "</color>";

            nuitrack.Skeleton skeleton = CurrentUserTracker.CurrentSkeleton;

            for (int q = 0; q < typeJoint.Length; q++)
            {
                nuitrack.Joint joint = skeleton.GetJoint(typeJoint[q]);
                Vector3 newPosition = 0.001f * joint.ToVector3();
                CreatedJoint[q].transform.localPosition = newPosition;

                JointWarnDistance[q] = Tool_distance(q, newPosition, C_ToolLocation);

                distance = JointWarnDistance[0];
                for (int p = 0; p < typeJoint.Length; p++)
                {
                    if (JointWarnDistance[p] < distance)
                    {
                        distance = JointWarnDistance[p];
                        Visual_Distance.text = "Distance : " + Math.Round(distance, 2);
                    }
                }
                /*
                 * receive x, y, z x = x - x_location / y = y - y_location / z = z - z_location
                 * if (x,y,z) , (newX, newY, newZ) distance -> warn, caution
                 */

                setLeftVisual(JointWarnDistance[(int)jointnum.Left_Hand]);
                setRightVisual(JointWarnDistance[(int)jointnum.Right_Hand]);
                setHeadVisual(JointWarnDistance[(int)jointnum.Head]);
                setRSVisual(JointWarnDistance[(int)jointnum.Right_Shoulder]);
                setLSVisual(JointWarnDistance[(int)jointnum.Left_Shoulder]);

                if (distance < stop_d)
                {
                    lock (locker)
                    {
                        state = 2;//stop
                    }
                    SR_Robot.color = new Color(1, 0, 0, 1);
                    //Debug.Log("state is stop!");
                }
                else if ((distance > stop_d & distance < warn_d))
                {
                    lock (locker)
                    {
                        state = 1; //warn(Slow)
                    }
                    SR_Robot.color = new Color(1, 1, 1, 1);
                    //Debug.Log("state is warn!");
                }
                else
                {
                    lock (locker)
                    {
                        state = 0;//safe
                    }
                    SR_Robot.color = new Color(1, 1, 1, 1);
                    //Debug.Log("state is safe!");
                }

                
            }
        }

        else
        {
            Detected.text = "<color=#ff0000>" + "Not Found" + "</color>";
        }

    }


    void socket_connect()
    {
        //connect to server
        sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        var ep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8888);
        sock.Connect(ep);

        rec = new byte[1024];

        while (true)
        {

            int n = sock.Receive(rec);
            String data = Encoding.UTF8.GetString(rec, 0, n);
            string[] spstring = data.Split(',');
            O_ToolLocation[0] = Convert.ToDouble(spstring[0]);
            O_ToolLocation[1] = Convert.ToDouble(spstring[1]);
            O_ToolLocation[2] = Convert.ToDouble(spstring[2]);

            if (state == 2) //stop!
                sock.Send(Encoding.UTF8.GetBytes("Stop"), SocketFlags.None);
            else if (state == 1) //Warn state
                sock.Send(Encoding.UTF8.GetBytes("Warn"), SocketFlags.None);
            else //Safe state
                sock.Send(Encoding.UTF8.GetBytes("Safe"), SocketFlags.None);
        }
    }
    /*double Location_distance(int jointnum, Vector3 newPosition)
    {
        Vector3 distance;
        distance = newPosition;

        double newX = distance[0];
        double newY = distance[1];
        double newZ = distance[2];

        double result = 1000 * Math.Sqrt(newX * newX + newY * newY + newZ * newZ );
        return result;
    }*/

    double Tool_distance(int jointnum, Vector3 newPosition, double[] C_ToolLocation)
    {
        Vector3 distance;
        distance = newPosition;

        double newX = distance[0];
        double newY = distance[1];
        double newZ = distance[2];

        double result = Math.Sqrt(
            (1000 * newX - C_ToolLocation[0]) * (1000 * newX - C_ToolLocation[0])
            + (1000 * newY - C_ToolLocation[1]) * (1000 * newY - C_ToolLocation[1])
            + (1000 * newZ - C_ToolLocation[2]) * (1000 * newZ - C_ToolLocation[2]));

        return result;
    }

    double[] tool(double[] O_Location)
    {
        C_ToolLocation[0] = (-1) * O_ToolLocation[0] + O_Location[0];
        C_ToolLocation[1] = O_ToolLocation[2] + O_Location[1];
        C_ToolLocation[2] = (-1) * O_ToolLocation[1] + O_Location[2];

        return C_ToolLocation;
    }
    void OnGUI()
    {
        GUI.color = Color.red;
        GUI.skin.label.fontSize = 50;
    }

    void OnApplicationQuit()
    {
        lock (locker)
        {
            sock.Send(Encoding.UTF8.GetBytes("End"), SocketFlags.None);
            //sock.Close();
        }
    }


    
    /*-------------------------------------------------- Set Visual-------------------------------------------------------------*/

    //Left Hand
    void setLeftVisual(double distance)
    {
        //Very Safe distance
        if (distance > warn_d + 2.5 * (warn_d - stop_d))
        {
            SR_Lhand.color = new Color(0, 1, 0, 1); //Green
            x_Lhand = 472;
        }
        //Safe distance
        else if (distance > warn_d & distance < warn_d + 2.5 * (warn_d - stop_d))
        {
            SR_Lhand.color = new Color(0, 1, 0, 1); //Green
            x_Lhand = 495f - 23f * (Convert.ToSingle(distance) - Convert.ToSingle(warn_d)) / (4 * Convert.ToSingle(stop_d));
        }
        //Warn distance
        else if (distance > stop_d & distance < warn_d)
        {
            SR_Lhand.color = new Color(1, 0.92f, 0.016f, 1); //yellow
            x_Lhand = 510f - 15f * (Convert.ToSingle(distance) - Convert.ToSingle(stop_d)) / (Convert.ToSingle(warn_d) - Convert.ToSingle(stop_d));
        }
        //Stop distance
        else if (distance < stop_d)
        {
            SR_Lhand.color = new Color(1, 0, 0, 1); //red
            x_Lhand = 515f - 5f * Convert.ToSingle(distance) / Convert.ToSingle(stop_d);
        }
        T_Lhand.position = new Vector3(x_Lhand, 374, -930);
    }


    //Right Hand
    void setRightVisual(double distance)
    {
        //Very Safe distance
        if (distance > warn_d + 2.5 * (warn_d - stop_d))
        {
            SR_Rhand.color = new Color(0, 1, 0, 1); //Green
            x_Rhand = 472f;
        }
        //Safe distance
        else if (distance > warn_d & distance < warn_d + 2.5 * (warn_d - stop_d))
        {
            SR_Rhand.color = new Color(0, 1, 0, 1); //Green
            x_Rhand = 495f - 23f * (Convert.ToSingle(distance) - Convert.ToSingle(warn_d)) / (4 * Convert.ToSingle(stop_d));
        }
        //Warn distance
        else if (distance > stop_d & distance < warn_d)
        {
            SR_Rhand.color = new Color(1, 0.92f, 0.016f, 1); //yellow
            x_Rhand = 511f - 16f * (Convert.ToSingle(distance) - Convert.ToSingle(stop_d)) / (Convert.ToSingle(warn_d) - Convert.ToSingle(stop_d));
        }
        //Stop distance
        else if (distance < stop_d)
        {
            SR_Rhand.color = new Color(1, 0, 0, 1); //red
            x_Rhand = 516f - 5f * Convert.ToSingle(distance) / Convert.ToSingle(stop_d);
        }
        T_Rhand.position = new Vector3(x_Rhand, 355, -930);
    }

    //Head
    void setHeadVisual(double distance)
    {
        //Very Safe distance
        if (distance > warn_d + 2.5 * (warn_d - stop_d))
        {
            SR_head.color = new Color(0, 1, 0, 1); //Green
            x_head = 472f;
        }
        //Safe distance
        else if (distance > warn_d & distance < warn_d + 2.5 * (warn_d - stop_d))
        {
            SR_head.color = new Color(0, 1, 0, 1); //Green
            x_head = 493f - 21f * (Convert.ToSingle(distance) - Convert.ToSingle(warn_d)) / (4 * Convert.ToSingle(stop_d));
        }
        //Warn distance
        else if (distance > stop_d & distance < warn_d)
        {
            SR_head.color = new Color(1, 0.92f, 0.016f, 1); //yellow
            x_head = 509f - 16f * (Convert.ToSingle(distance) - Convert.ToSingle(stop_d)) / (Convert.ToSingle(warn_d) - Convert.ToSingle(stop_d));
        }
        //Stop distance
        else if (distance < stop_d)
        {
            SR_head.color = new Color(1, 0, 0, 1); //red
            x_head = 515f - 6f * Convert.ToSingle(distance) / Convert.ToSingle(stop_d);
        }
        T_head.position = new Vector3(x_head, 364, -930);
    }

    //Right Shoulder
    void setRSVisual(double distance)
    {
        //Very Safe distance
        if (distance > warn_d + 2.5 * (warn_d - stop_d))
        {
            SR_Rshoulder.color = new Color(0, 1, 0, 1); //Green
            x_Rshoulder = 472f;
        }
        //Safe distance
        else if (distance > warn_d & distance < warn_d + 2.5 * (warn_d - stop_d))
        {
            SR_Rshoulder.color = new Color(0, 1, 0, 1); //Green
            x_Rshoulder = 493f - 21f * (Convert.ToSingle(distance) - Convert.ToSingle(warn_d)) / (4 * Convert.ToSingle(stop_d));
        }
        //Warn distance
        else if (distance > stop_d & distance < warn_d)
        {
            SR_Rshoulder.color = new Color(1, 0.92f, 0.016f, 1); //yellow
            x_Rshoulder = 509f - 16f * (Convert.ToSingle(distance) - Convert.ToSingle(stop_d)) / (Convert.ToSingle(warn_d) - Convert.ToSingle(stop_d));
        }
        //Stop distance
        else if (distance < stop_d)
        {
            SR_Rshoulder.color = new Color(1, 0, 0, 1); //red
            x_Rshoulder = 515f - 6f * Convert.ToSingle(distance) / Convert.ToSingle(stop_d);
        }
        T_Rshoulder.position = new Vector3(x_Rshoulder, 359, -930);
    }

    //Left Shoulder
    void setLSVisual(double distance)
    {
        //Very Safe distance
        if (distance > warn_d + 2.5 * (warn_d - stop_d))
        {
            SR_Lshoulder.color = new Color(0, 1, 0, 1); //Green
            x_Lshoulder = 472f;
        }
        //Safe distance
        else if (distance > warn_d & distance < warn_d + 2.5 * (warn_d - stop_d))
        {
            SR_Lshoulder.color = new Color(0, 1, 0, 1); //Green
            x_Lshoulder = 493f - 21f * (Convert.ToSingle(distance) - Convert.ToSingle(warn_d)) / (4 * Convert.ToSingle(stop_d));
        }
        //Warn distance
        else if (distance > stop_d & distance < warn_d)
        {
            SR_Lshoulder.color = new Color(1, 0.92f, 0.016f, 1); //yellow
            x_Lshoulder = 509f - 16f * (Convert.ToSingle(distance) - Convert.ToSingle(stop_d)) / (Convert.ToSingle(warn_d) - Convert.ToSingle(stop_d));
        }
        //Stop distance
        else if (distance < stop_d)
        {
            SR_Lshoulder.color = new Color(1, 0, 0, 1); //red
            x_Lshoulder = 515f - 6f * Convert.ToSingle(distance) / Convert.ToSingle(stop_d);
        }
        T_Lshoulder.position = new Vector3(x_Lshoulder, 369, -930);
    }
}