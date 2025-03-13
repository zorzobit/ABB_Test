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
using System.Globalization;
using ABB.Robotics.RobotStudio.Stations;

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
                //var P10 = this.Controller.Rapid.GetRapidData("T_ROB1", "PR2ghj1");
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
        public void ReadNumData(RDItem rdi)
        {
            if (string.IsNullOrEmpty(rdi.Name)) return;
            try
            {
                RapidData rd = this.Controller.Rapid.GetRapidData("T_ROB1", rdi.Name); // Ensure module name is included
                if (rd.Value is Num numValue)  // Correctly cast to Num
                {
                    rdi.Value = rd.StringValue;  // Extract the double value
                }
            }
            catch (Exception)
            {
            }
        }
        public void WriteNumData(RDItem rdi)
        {
            double val = 0.0;
            RapidData rd = this.Controller.Rapid.GetRapidData("T_ROB1", rdi.Name);
            if (rd.Value is Num numValue)
            {
                double.TryParse(rdi.Value, out val);
                numValue.Value = val;
                try
                {
                    using (Mastership mastership = Mastership.Request(Controller.Rapid))
                    {
                        rd.Value = numValue;
                    }
                }
                catch (Exception ex)
                {
                    var ms = ex.Message;
                }
            }
        }

        public void ReadRobtargetData(RDItem rdi)
        {
            if (string.IsNullOrEmpty(rdi.Name)) return;
            try
            {
                RapidData rd = this.Controller.Rapid.GetRapidData("T_ROB1", rdi.Name); // Ensure module name is included
                if (rd.Value is RobTarget numValue)  // Correctly cast to Num
                {
                    rdi.Value = rd.StringValue;  // Extract the double value
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public void WriteRobtargetData(RDItem rdi)
        {
            try
            {
                RapidData rd = this.Controller.Rapid.GetRapidData("T_ROB1", rdi.Name); // Ensure correct module name

                if (rd.Value is RobTarget robTarget)
                {
                    using (Mastership mastership = Mastership.Request(this.Controller.Rapid))
                    {
                        // Expecting a robtarget string like "[[X,Y,Z],[Q1,Q2,Q3,Q4],[CF1,CF4,CF6,Cfx],[E1,E2,E3,E4,E5,E6]]"
                        string input = rdi.Value.Trim('[', ']'); // Remove outer brackets
                        string[] sections = input.Split(new[] { "],[" }, StringSplitOptions.None); // Split into 4 parts

                        if (sections.Length == 4)
                        {
                            // Parsing Translation (X, Y, Z)
                            string[] transValues = sections[0].Split(',');
                            if (transValues.Length == 3 &&
                                float.TryParse(transValues[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) &&
                                float.TryParse(transValues[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y) &&
                                float.TryParse(transValues[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
                            {
                                robTarget.Trans.X = x;
                                robTarget.Trans.Y = y;
                                robTarget.Trans.Z = z;
                            }
                            else
                            {
                                throw new FormatException("Invalid format for Translation (X, Y, Z).");
                            }

                            // Parsing Rotation (Q1, Q2, Q3, Q4)
                            string[] rotValues = sections[1].Split(',');
                            if (rotValues.Length == 4 &&
                                float.TryParse(rotValues[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float q1) &&
                                float.TryParse(rotValues[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float q2) &&
                                float.TryParse(rotValues[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float q3) &&
                                float.TryParse(rotValues[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float q4))
                            {
                                robTarget.Rot.Q1 = q1;
                                robTarget.Rot.Q2 = q2;
                                robTarget.Rot.Q3 = q3;
                                robTarget.Rot.Q4 = q4;
                            }
                            else
                            {
                                throw new FormatException("Invalid format for Rotation (Q1, Q2, Q3, Q4).");
                            }

                            // Parsing Configuration (CF1, CF4, CF6, Cfx)
                            string[] confValues = sections[2].Split(',');
                            if (confValues.Length == 4 &&
                                int.TryParse(confValues[0], out int cf1) &&
                                int.TryParse(confValues[1], out int cf4) &&
                                int.TryParse(confValues[2], out int cf6) &&
                                int.TryParse(confValues[3], out int cfx))
                            {
                                robTarget.Robconf.Cf1 = cf1;
                                robTarget.Robconf.Cf4 = cf4;
                                robTarget.Robconf.Cf6 = cf6;
                                robTarget.Robconf.Cfx = cfx;
                            }
                            else
                            {
                                throw new FormatException("Invalid format for Configuration (CF1, CF4, CF6, Cfx).");
                            }

                            // Parsing External Axes (E1-E6)
                            string[] extAxesValues = sections[3].Split(',');
                            if (extAxesValues.Length == 6)
                            {
                                for (int i = 0; i < 6; i++)
                                {
                                    if (float.TryParse(extAxesValues[i], NumberStyles.Float, CultureInfo.InvariantCulture, out float extVal))
                                    {
                                        robTarget.Extax.Eax_a = extVal;
                                    }
                                }
                            }
                            else
                            {
                                throw new FormatException("Invalid format for External Axes (E1-E6).");
                            }

                            // Assign modified robTarget back to RAPID
                            rd.Value = robTarget;
                        }
                        else
                        {
                            throw new FormatException("Invalid robtarget format.");
                        }
                    }
                }
                else
                {
                    throw new InvalidOperationException($"The variable {rdi.Name} is not of type 'robtarget'.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
        public void ReadIOData(RDItem rDItem)
        {
            string signalName = rDItem.Name;
            try
            {
                ABB.Robotics.Controllers.IOSystemDomain.Signal signal = this.Controller.IOSystem.GetSignal(signalName);
                rDItem.IsON = Convert.ToBoolean(signal.Value);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading signal {signalName}: {ex.Message}");
            }
        }
        public void WriteIOData(RDItem rDItem)
        {
            string signalName = rDItem.Name;
            bool value = rDItem.IsON;
            try
            {
                ABB.Robotics.Controllers.IOSystemDomain.Signal signal = this.Controller.IOSystem.GetSignal(signalName);
                signal.Value = Convert.ToUInt64(value); // Set signal HIGH (true) or LOW (false)
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing signal {signalName}: {ex.Message}");
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
                if(task!=null && task.ProgramPointer != null)
                {
                    var pp = task.ProgramPointer;
                    return pp.Module + "/" + pp.Routine + "/" + pp.Range.Begin.Column + ":" + pp.Range.Begin.Row;
                }
            }
            catch (Exception)
            {
            }
                return "";
        }
    }
}
