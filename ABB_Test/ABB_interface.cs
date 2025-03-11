using ABB.Robotics.Controllers.Discovery;
using ABB.Robotics.Controllers;
using ABB.Robotics.Controllers.MotionDomain;
using ABB.Robotics.Controllers.RapidDomain;
using System.Buffers;

namespace ABB_Test
{
    public class ABB_interface
    {
        public Controller Controller { get; set; }
        string sysID = "";
        MechanicalUnit mechUnit;
        MotionSystem motionSystem ;
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
    }
}
