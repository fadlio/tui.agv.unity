using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using Unity.Robotics.UrdfImporter.Control;
using System.Text.RegularExpressions;

namespace RosSharp.Control
{
    public enum ControlMode { Keyboard, ROS };

    public class AGVController : MonoBehaviour
    {
        public string Namespace = "agv0";
        public GameObject Wheel1;
        public GameObject Wheel2;
        public ControlMode Mode = ControlMode.ROS;
        public float MaxLinearSpeed = 2; //  m/s
        public float MaxRotationalSpeed = 1;//
        public float WheelRadius = 0.033f; //meters
        public float TrackWidth = 0.288f; // meters Distance between tyres
        public float ForceLimit = 10;
        public float Damping = 10;
        public float ROSTimeout = 0.5f;

        private ArticulationBody _wA1;
        private ArticulationBody _wA2;
        private float _lastCmdReceived = 0f;

        private ROSConnection _ros;
        private RotationDirection _direction;
        private float _linearSpeed = 0f;
        private float _angularSpeed = 0f;

        void Start()
        {
            _wA1 = Wheel1.GetComponent<ArticulationBody>();
            _wA2 = Wheel2.GetComponent<ArticulationBody>();
            SetParameters(_wA1);
            SetParameters(_wA2);
            _ros = ROSConnection.GetOrCreateInstance();
            if (!int.TryParse(gameObject.name, out int nameIndex))
            {
                nameIndex = 0;
            }
            _ros.Subscribe<TwistMsg>($"/{(Namespace != "" ? Namespace + "/" : "")}cmd_vel", ReceiveROSCmd);
        }

        void ReceiveROSCmd(TwistMsg cmdVel)
        {
            _linearSpeed = (float)cmdVel.linear.x;
            _angularSpeed = (float)cmdVel.angular.z;
            _lastCmdReceived = Time.time;
        }

        void FixedUpdate()
        {
            if (Mode == ControlMode.Keyboard)
            {
                KeyBoardUpdate();
            }
            else if (Mode == ControlMode.ROS)
            {
                ROSUpdate();
            }
        }

        private void SetParameters(ArticulationBody joint)
        {
            ArticulationDrive drive = joint.xDrive;
            drive.forceLimit = ForceLimit;
            drive.damping = Damping;
            joint.xDrive = drive;
        }

        private void SetSpeed(ArticulationBody joint, float wheelSpeed = float.NaN)
        {
            ArticulationDrive drive = joint.xDrive;
            if (float.IsNaN(wheelSpeed))
            {
                drive.targetVelocity = ((2 * MaxLinearSpeed) / WheelRadius) * Mathf.Rad2Deg * (int)_direction;
            }
            else
            {
                drive.targetVelocity = wheelSpeed;
            }
            joint.xDrive = drive;
        }

        private void KeyBoardUpdate()
        {
            float moveDirection = Input.GetAxis("Vertical");
            float inputSpeed;
            float inputRotationSpeed;
            if (moveDirection > 0)
            {
                inputSpeed = MaxLinearSpeed;
            }
            else if (moveDirection < 0)
            {
                inputSpeed = MaxLinearSpeed * -1;
            }
            else
            {
                inputSpeed = 0;
            }

            float turnDirction = Input.GetAxis("Horizontal");
            if (turnDirction > 0)
            {
                inputRotationSpeed = MaxRotationalSpeed;
            }
            else if (turnDirction < 0)
            {
                inputRotationSpeed = MaxRotationalSpeed * -1;
            }
            else
            {
                inputRotationSpeed = 0;
            }
            RobotInput(inputSpeed, inputRotationSpeed);
        }


        private void ROSUpdate()
        {
            if (Time.time - _lastCmdReceived > ROSTimeout)
            {
                _linearSpeed = 0f;
                _angularSpeed = 0f;
            }
            RobotInput(_linearSpeed, -_angularSpeed);
        }

        private void RobotInput(float speed, float rotSpeed) // m/s and rad/s
        {
            if (speed > MaxLinearSpeed)
            {
                speed = MaxLinearSpeed;
            }
            if (rotSpeed > MaxRotationalSpeed)
            {
                rotSpeed = MaxRotationalSpeed;
            }
            float wheel1Rotation = (speed / WheelRadius);
            float wheel2Rotation = wheel1Rotation;
            float wheelSpeedDiff = ((rotSpeed * TrackWidth) / WheelRadius);
            if (rotSpeed != 0)
            {
                wheel1Rotation = (wheel1Rotation + (wheelSpeedDiff / 1)) * Mathf.Rad2Deg;
                wheel2Rotation = (wheel2Rotation - (wheelSpeedDiff / 1)) * Mathf.Rad2Deg;
            }
            else
            {
                wheel1Rotation *= Mathf.Rad2Deg;
                wheel2Rotation *= Mathf.Rad2Deg;
            }
            SetSpeed(_wA1, wheel1Rotation);
            SetSpeed(_wA2, wheel2Rotation);
        }
    }
}
