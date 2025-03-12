using ABB.Robotics.Controllers;
using ABB.Robotics.Controllers.Discovery;
using ABB.Robotics.Controllers.MotionDomain;
using ABB.Robotics.Controllers.RapidDomain;
using System.Threading.Tasks;
using System.Windows;
using Task = ABB.Robotics.Controllers.RapidDomain.Task;

namespace ABB_Test
{
    public class ABB_interface
    {
        public Controller Controller { get; set; }
        string sysID = "";
        MechanicalUnit mechUnit;
        MotionSystem motionSystem;
        Task task;
        public void Connect()
        {
            NetworkScanner scanner = new NetworkScanner();
            ControllerInfo[] controllers = scanner.GetControllers(NetworkScannerSearchCriterias.Virtual);
            if (controllers.Length > 0)
            {
                sysID = controllers[0].SystemId.ToString();
                this.Controller = Controller.Connect(controllers[0].SystemId, ConnectionType.Standalone);
                this.Controller.Logon(UserInfo.DefaultUser);
                mechUnit = this.Controller.MotionSystem.ActiveMechanicalUnit;
                motionSystem = this.Controller.MotionSystem;
                task = this.Controller.Rapid.GetTask("T_ROB1");
                IsConnected = true;
            }
        }
        public bool IsConnected { get; set; } = false;
        internal void DisConnect()
        {
            this.Controller.Logoff();
            this.Controller?.Dispose();
            IsConnected = false;
        }
        public JointTarget GetPosJ()
        {
            return mechUnit.GetPosition();
        }
        public RobTarget GetPosX()
        {
            return mechUnit.GetPosition(CoordinateSystemType.Base);
        }
        public ControllerOperatingMode OperationStatus()
        {
            return Controller.OperatingMode;
        }
        public int GetOverride()
        {
            var rt = motionSystem.SpeedRatio;
            return rt;
        }
        public string GetTaskStatus()
        {
            return task.ExecutionStatus.ToString();
        }
        public void SetOverride(int val)
        {
            if (Controller.OperatingMode == ControllerOperatingMode.Auto)
            {
                using (Mastership mastership = Mastership.Request(Controller.Rapid))
                {
                    motionSystem.SpeedRatio = val;
                }
            }
        }

        internal string GetModules()
        {
            string result="";
            foreach(var item in task.GetModules())
            {
                result += item.Name.ToString() + "\n";
            }
            return result;
        }
        public void Start()
        {
            var stat = task.ExecutionStatus;
            if (stat == TaskExecutionStatus.Stopped)
            {
                using (Mastership mastership = Mastership.Request(Controller.Rapid))
                {
                    Controller.Rapid.Start();
                }
            }

        }
        public void Stop()
        {
            using (Mastership mastership = Mastership.Request(Controller.Rapid))
            {
                task.Stop(StopMode.Immediate);
            }
        }
        public void Abort()
        {
            using (Mastership mastership = Mastership.Request(Controller.Rapid))
            {
                task.Stop(StopMode.Immediate);
                task.ResetProgramPointer();
            }
        }
        public void Reset()
        {
            using (Mastership mastership = Mastership.Request(Controller.Rapid))
            { 

            }

        }
    }
}
