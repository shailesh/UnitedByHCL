using System;
using System.IO.Ports;
using System.Threading;
using System.Windows.Forms;
using ThinkGearNET;


namespace ThinkGearNETTest
{
	public partial class Form1 : Form
	{
		private ThinkGearWrapper _thinkGearWrapper = new ThinkGearWrapper();
        int x = 1;
        int directioncase = 0;
        string raw;
        string delta1;
        
		public Form1()
		{
			InitializeComponent();
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			foreach(string port in SerialPort.GetPortNames())
				cboPort.Items.Add(port);
			cboPort.SelectedIndex = 0;
            btnEnableBlink.Visible = false;
            btnDisableBlink.Visible = false;
            
		}

		private void btnConnect_Click(object sender, EventArgs e)
		{
			_thinkGearWrapper = new ThinkGearWrapper();

			// setup the event
			_thinkGearWrapper.ThinkGearChanged += _thinkGearWrapper_ThinkGearChanged;            

			// connect to the device on the specified COM port at 57600 baud
			if(!_thinkGearWrapper.Connect("COM5", 57600, true))
				MessageBox.Show("Could not connect to headset.");
		}

		void _thinkGearWrapper_ThinkGearChanged(object sender, ThinkGearChangedEventArgs e)
		{
			// update the textbox and sleep for a tiny bit
			BeginInvoke(new MethodInvoker(delegate 
				{
					lblAttention.Text = "Attention: " + e.ThinkGearState.Attention;
					lblMeditation.Text = "Meditation: " + e.ThinkGearState.Meditation;
					txtState.Text = e.ThinkGearState.ToString();
                    //delta1 = e.ThinkGearState.Delta.ToString();
                    //raw = e.ThinkGearState.Raw.ToString(); 
                   
                    //System.IO.File.AppendAllText(@"F:\BreakoutCSharp Data Collection using WBT\Delta\bottom.txt", delta1 + Environment.NewLine);
                    //System.IO.File.AppendAllText(@"F:\BreakoutCSharp Data Collection using WBT\Delta\rawbottom.txt", raw + Environment.NewLine);
                    
                    x = (int)e.ThinkGearState.Attention;
                    x = (x % 10);
                    switch (directioncase)
                    {
                        case 1:
                            button1.Left = button1.Left - x;
                            if(button1.Left<40)
                            { directioncase = 5; }
                            break; /* optional */
                        case 2:
                            button1.Top = button1.Top - x;
                            if (button1.Top < 24)
                            { directioncase = 5; }
                            break; /* optional */
                        case 3:
                            button1.Left = button1.Left + x;
                            if (button1.Left > 580)
                            { directioncase = 5; }
                            break; /* optional */
                        case 4:
                            button1.Top = button1.Top + x;
                            if (button1.Top > 480)
                            { directioncase = 5; }
                            break; /* optional */           
                        case 5:                            
                            break;
                    }                   

				}));
			Thread.Sleep(10);
		}

		private void Form1_FormClosing(object sender, FormClosingEventArgs e)
		{
			_thinkGearWrapper.Disconnect();
		}

		private void btnDisconnect_Click(object sender, EventArgs e)
		{
			_thinkGearWrapper.Disconnect();
		}

		private void btnEnableBlink_Click(object sender, EventArgs e)
		{
			_thinkGearWrapper.EnableBlinkDetection(true);
		}

		private void btnDisableBlink_Click(object sender, EventArgs e)
		{
			_thinkGearWrapper.EnableBlinkDetection(false);
		}

        private void button2_Click(object sender, EventArgs e)
        {
            directioncase = 2;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            directioncase = 1;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            directioncase = 3;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            directioncase = 4;
        }
	}
}
