using ABB.Robotics.Controllers.MotionDomain;
using ABB.Robotics.Controllers.RapidDomain;
using ABB.Robotics.RobotStudio.Stations;
using RobotStudio.Services.RobApi;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace ABB_Test
{
    public class MainViewModel : BaseViewModel
    {
        MainWindow mainWindow;
        ABB_interface abb_interface;
        BackgroundWorker DataCheck;
        internal void Loaded(MainWindow mWindow)
        {
            this.mainWindow = mWindow;
            abb_interface = new ABB_interface();
            ConnectionStatus = "No controller";
            ConnectButtonContext = "Connect";
            DataCheck = new BackgroundWorker();
            DataCheck.DoWork += DataCheck_DoWork;
            DataCheck.RunWorkerCompleted += DataCheck_RunWorkerCompleted;
            mainWindow.OverrideSlider.PreviewMouseDown += OverrideSlider_MouseDown;
            mainWindow.OverrideSlider.PreviewMouseUp += OverrideSlider_MouseUp;
        }
        bool overrideHold = false;
        private void OverrideSlider_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if(abb_interface.IsConnected)
                abb_interface.SetOverride(Override);
            overrideHold = false;
        }

        private void OverrideSlider_MouseDown(object sender, MouseButtonEventArgs e)
        {
            overrideHold = true;
        }

        public PositionModel RobotPosition { get; set; }
        private void DataCheck_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
        {
            DataCheck.RunWorkerAsync();
        }

        private void DataCheck_DoWork(object? sender, DoWorkEventArgs e)
        {
            if (abb_interface.IsConnected)
            {
                var jointTarget = abb_interface.GetPosJ();
                var tcpTarget = abb_interface.GetPosX();
                double qw = tcpTarget.Rot.Q1;
                double qx = tcpTarget.Rot.Q2;
                double qy = tcpTarget.Rot.Q3;
                double qz = tcpTarget.Rot.Q4;

                // roll (x-axis rotation)
                double sinr = +2.0 * (qw * qx + qy * qz);
                double cosr = +1.0 - 2.0 * (qx * qx + qy * qy);
                double rx = Math.Atan2(sinr, cosr) * 180 / Math.PI;

                // pitch (y-axis rotation)
                double sinp = +2.0 * (qw * qy - qz * qx);
                double ry;
                if (Math.Abs(sinp) >= 1)
                    ry = Math.Sign(sinp) * 180 / Math.PI;//(Math.PI / 2, sinp); // use 90 degrees if out of range
                else
                    ry = Math.Asin(sinp) * 180 / Math.PI;

                // yaw (z-axis rotation)
                double siny = +2.0 * (qw * qz + qx * qy);
                double cosy = +1.0 - 2.0 * (qy * qy + qz * qz);
                double rz = Math.Atan2(siny, cosy) * 180 / Math.PI;
                RobotPosition = new PositionModel
                {
                    J1 = Math.Round(jointTarget.RobAx.Rax_1, 3),
                    J2 = Math.Round(jointTarget.RobAx.Rax_2, 3),
                    J3 = Math.Round(jointTarget.RobAx.Rax_3, 3),
                    J4 = Math.Round(jointTarget.RobAx.Rax_4, 3),
                    J5 = Math.Round(jointTarget.RobAx.Rax_5, 3),
                    J6 = Math.Round(jointTarget.RobAx.Rax_6, 3),

                    X = Math.Round(tcpTarget.Trans.X, 3),
                    Y = Math.Round(tcpTarget.Trans.Y, 3),
                    Z = Math.Round(tcpTarget.Trans.Z, 3),

                    RX = Math.Round(rx, 3),
                    RY = Math.Round(ry, 3),
                    RZ = Math.Round(rz, 3),



                    Q1 = Math.Round(tcpTarget.Rot.Q1, 3),
                    Q2 = Math.Round(tcpTarget.Rot.Q2, 3),
                    Q3 = Math.Round(tcpTarget.Rot.Q3, 3),
                    Q4 = Math.Round(tcpTarget.Rot.Q4, 3)
                };
                OperationStatus = abb_interface.OperationStatus() == ABB.Robotics.Controllers.ControllerOperatingMode.Auto ? "AUTO" : "MANUAL";
                if(!overrideHold)
                    Override = abb_interface.GetOverride();
                ActiveTask=abb_interface.GetTaskStatus();
                Modules = abb_interface.GetModules();
                ProgramPosition = abb_interface.GetProgramPos();
                Messages = new ObservableCollection<string>(abb_interface.GetMessages());
            }
        }
        public int Override {  get; set; }
        public string OperationStatus { get; set; }
        public string ConnectionStatus { get; set; }
        public string ConnectButtonContext { get; set; }
        public string ActiveTask {  get; set; }
        public string Modules { get; set; }
        public string ProgramPosition { get; set; }
        public ObservableCollection<string> Messages { get; set; }
        public ICommand ConnectButtonClick
        {
            get
            {
                return new RelayCommand(o =>
                {
                    if (ConnectButtonContext == "Connect")
                    {
                        abb_interface.Connect();
                        if (abb_interface.Controller != null)
                        {
                            ConnectionStatus = abb_interface.Controller.IPAddress.ToString();
                            ConnectButtonContext = "Disconnect"; 
                            DataCheck.RunWorkerAsync();
                        }
                    }
                    else
                    {
                        abb_interface.DisConnect(); 
                        ConnectionStatus = "No controller";
                        ConnectButtonContext = "Connect";
                    }
                }, o => true);
            }
        }
        public ICommand Start
        {
            get
            {
                return new RelayCommand(o =>
                {
                    if(abb_interface.IsConnected)
                        abb_interface.Start();
                }, o => true);
            }
        }
        public ICommand Stop
        {
            get
            {
                return new RelayCommand(o =>
                {
                    if (abb_interface.IsConnected)
                        abb_interface.Stop();
                }, o => true);
            }
        }
        public ICommand Abort
        {
            get
            {
                return new RelayCommand(o =>
                {
                    if (abb_interface.IsConnected)
                        abb_interface.Abort();
                }, o => true);
            }
        }
        public ICommand Reset
        {
            get
            {
                return new RelayCommand(o =>
                {
                    if (abb_interface.IsConnected)
                        abb_interface.Reset();
                }, o => true);
            }
        }
    }
    public class PositionModel
    {
        public double J1 { get; set; }
        public double J2 { get; set; }
        public double J3 { get; set; }
        public double J4 { get; set; }
        public double J5 { get; set; }
        public double J6 { get; set; }

        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double RX { get; set; }
        public double RY { get; set; }
        public double RZ { get; set; }



        public double Q1 { get; set; }
        public double Q2 { get; set; }
        public double Q3 { get; set; }
        public double Q4 { get; set; }
    }
}
