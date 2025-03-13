using ABB.Robotics.Controllers;
using ABB.Robotics.Controllers.Discovery;
using ABB.Robotics.Controllers.EventLogDomain;
using ABB.Robotics.Controllers.MotionDomain;
using ABB.Robotics.Controllers.RapidDomain;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using Task = ABB.Robotics.Controllers.RapidDomain.Task;
using EventLog = ABB.Robotics.Controllers.EventLogDomain.EventLog;
using System.Security.Claims;
using System.Collections.ObjectModel;

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
                this.Controller.StateChanged += Controller_StateChanged;
                this.Controller.OperatingModeChanged += Controller_OperatingModeChanged;
                this.Controller.Rapid.TaskEnabledChanged += Rapid_TaskEnabledChanged;
                this.Controller.Rapid.ExecutionStatusChanged += Rapid_ExecutionStatusChanged;
                this.Controller.Rapid.RapidDataResolve += Rapid_RapidDataResolve;
                this.Controller.Rapid.ExecutionCycleChanged += Rapid_ExecutionCycleChanged;
                var P10 = this.Controller.Rapid.GetRapidData("T_ROB1", "P10");
                var cyc = this.Controller.Rapid.Cycle;
                var rem = this.Controller.Rapid.RemainingCycles;
                IsConnected = true;
            }
        }

        private void Rapid_ExecutionCycleChanged(object sender, EventArgs e)
        {
            var cy=e.ToString();
        }

        private void Controller_OperatingModeChanged(object sender, OperatingModeChangeEventArgs e)
        {
            var mdm = e.NewMode;
        }

        private void Controller_StateChanged(object sender, StateChangedEventArgs e)
        {
            var std = e.NewState; 
            //ControllerState State;
        }

        private IRapidData Rapid_RapidDataResolve(object sender, ResolveEventArgs e)
        {
            var sdd = e.Name;
            IRapidData rapidData;
            return null;
        }

        private void Rapid_ExecutionStatusChanged(object sender, ExecutionStatusChangedEventArgs e)
        {
            var sfd = e.Status;

        }

        private void Rapid_TaskEnabledChanged(object sender, TaskEnabledChangedEventArgs e)
        {
            var hhsz = e.Enabled;
        }

        public bool IsConnected { get; set; } = false;
        internal void DisConnect()
        {
            this.Controller.Logoff();
            this.Controller?.Dispose();
            IsConnected = false;
        }
        private List<string> Messages;
        public List<string> GetMessages()
        {
            if(Messages == null)Messages = new List<string>();
            if (Controller == null || !Controller.Connected)
                {
                    Messages.Add("Disconnected from controller");
                    return Messages;
                }

    EventLog eventLog = Controller.EventLog;
    EventLogCategory logs = eventLog.GetCategory(CategoryType.Common);
    var logArray = logs.Messages
                        .OrderByDescending(x => x.Timestamp) // Sort by newest first
                        .Take(10) // Limit to last 10 messages
                        .ToArray();

                foreach (var message in logArray)
                {
                    string logEntry = $"Date: {message.Timestamp}, Num: {message.Number}, Message: {message.Title}";

                    // Only add if it doesn't already exist
                    if (!Messages.Any(m => m.Contains($"Date: {message.Timestamp}") && m.Contains($"Num: {message.Number}")))
                    {
                        Messages.Add(logEntry);
                    }
                }
                return Messages;
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
            var stat = Controller.Rapid.ExecutionStatus;
            return task.ExecutionStatus.ToString() + "/Type:" + task.ExecutionType.ToString();
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

        internal string GetProgramPos()
        {
            try
            {
                var pp = task.ProgramPointer;
                return pp.Module + "/" + pp.Routine + "/" + pp.Range.Begin.Column + ":" + pp.Range.Begin.Row;
            }
            catch (Exception)
            {
                return "";
            }
        }
    }
}
