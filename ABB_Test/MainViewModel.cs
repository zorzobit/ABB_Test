using ABB.Robotics.Controllers.RapidDomain;
using ABB.Robotics.RobotStudio.Stations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

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

                    Q1 = Math.Round(tcpTarget.Rot.Q1, 3),
                    Q2 = Math.Round(tcpTarget.Rot.Q2, 3),
                    Q3 = Math.Round(tcpTarget.Rot.Q3, 3),
                    Q4 = Math.Round(tcpTarget.Rot.Q4, 3)
                };

            }
        }

        public string ConnectionStatus { get; set; }
        public string ConnectButtonContext { get; set; }
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

        public double Q1 { get; set; }
        public double Q2 { get; set; }
        public double Q3 { get; set; }
        public double Q4 { get; set; }
    }
}
