using J2i.Net.XInputWrapper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.VideoSurveillance;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using Emgu.CV.Structure;
using System.IO.Ports;

namespace Turret
{
    public partial class Form1 : Form
    {
        XboxController _selectedController;

        private Capture _capture;
        private MotionHistory _motionHistory;
        private BackgroundSubtractor _forgroundDetector;
        //Threshold to define a motion area, reduce the value to detect smaller motion
        double minArea = 1000;
        double minMotionPixelsPct = 0.05;
        bool onlyUseLargest = true;
        private Mat _segMask = new Mat();
        private Mat _forgroundMask = new Mat();
        private int fireRate = 1;
        private bool autoTracking = false;
        int skipCounter = 0;
        Timer t = null;

        public Form1()
        {
            InitializeComponent();
        }

        ArduinoControllerMain controller = new ArduinoControllerMain();
        private void button1_Click(object sender, EventArgs e)
        {
            SetComPort(comboBox1.SelectedItem.ToString());
        }

        private void InitCamera()
        {
            //try to create the capture
            if (_capture == null)
            {
                try
                {
                    _capture = new Capture(Convert.ToInt32(textBox2.Text));
                }
                catch (NullReferenceException excpt)
                {   //show errors if there is any
                    MessageBox.Show(excpt.Message);
                }
            }

            if (_capture != null) //if camera capture has been successfully created
            {
                _motionHistory = new MotionHistory(
                    1.0, //in second, the duration of motion history you wants to keep
                    0.05, //in second, maxDelta for cvCalcMotionGradient
                    0.5); //in second, minDelta for cvCalcMotionGradient

                _capture.ImageGrabbed += _capture_ImageGrabbed; ;
                _capture.Start();
            }
        }


        private void SetComPort()
        {
            if (richTextBox1.InvokeRequired)
            {
                this.Invoke(new Action(() => { SetComPort(); }));
            }
            else
            {
                controller.SetComPort(comboBox1.SelectedItem.ToString());
            }
        }

        private void SetComPort(int comPort)
        {

            if (richTextBox1.InvokeRequired)
            {
                richTextBox1.Invoke(new Action<int>(SetComPort), comPort);
            }
            else
            {
                controller.SetComPort(comPort);
            }
        }

        private void SetComPort(string comPort)
        {

            if (richTextBox1.InvokeRequired)
            {
                richTextBox1.Invoke(new Action<string>(SetComPort), comPort);
            }
            else
            {
                controller.SetComPort(comPort);
            }
        }


        private void button2_Click(object sender, EventArgs e)
        {
            if (richTextBox1.InvokeRequired)
            {
                richTextBox1.Invoke(new Action(FirePrep), null);
            }
            else
            {
                FirePrep();
            }

        }

        private void FirePrep()
        {
            Taunt();
            controller.SendFirePrep();
        }

        private void Taunt()
        {
            var path = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), 
                "abouttodrophammer.wav");
            new SoundPlayer(path).Play();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (richTextBox1.InvokeRequired)
            {
                richTextBox1.Invoke(new Action(Fire), null);
            }
            else
            {
                Fire();
            }


        }

        private void Fire()
        {
            var path = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                //"engage.wav");
                "sayhellotofriend.wav");
            (new SoundPlayer(path)).Play();
            System.Threading.Thread.Sleep(100);
            controller.SendFire();


            t = new Timer();
            t.Interval = 500;
            t.Tick += (s, e) => 
            {
                if (this.BackColor == Color.Red)
                    this.BackColor = Color.LightGray;
                else
                    this.BackColor = Color.Red;
            };
            t.Start();

        }

        private void button4_Click(object sender, EventArgs e)
        {
            var pos = Convert.ToByte(textBox1.Text);
            controller.SendMove(pos);
        }



        private void button5_Click(object sender, EventArgs e)
        {
            if (richTextBox1.InvokeRequired)
            {
                richTextBox1.Invoke(new Action(MoveLeft), null);
            }
            else
            {
                MoveLeft();
            }


        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (richTextBox1.InvokeRequired)
            {
                richTextBox1.Invoke(new Action(MoveRight), null);
            }
            else
            {
                MoveRight();
            }
        }

        private void MoveLeft()
        {
            if (button4.Enabled)
            {
                //left
                textBox1.Text = (Convert.ToInt32(textBox1.Text) - 5).ToString();
                button4_Click(this, new EventArgs());

                button4.Enabled = (Convert.ToInt32(textBox1.Text) > 0);
                button6.Enabled = (Convert.ToInt32(textBox1.Text) < 180);

                var path = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                    "inputcoordinates.wav");
                (new SoundPlayer(path)).Play();
            }
        }

        private void MoveRight()
        {
            if (button6.Enabled)
            {
                //right
                textBox1.Text = (Convert.ToInt32(textBox1.Text) + 5).ToString();
                button4_Click(this, new EventArgs());

                button4.Enabled = (Convert.ToInt32(textBox1.Text) > 0);
                button6.Enabled = (Convert.ToInt32(textBox1.Text) < 180);

                var path = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                    "inputcoordinates.wav");
                (new SoundPlayer(path)).Play();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            comboBox1.Items.AddRange(SerialPort.GetPortNames());
            comboBox1.SelectedIndex = 0;

            controller.OnData += Controller_OnData;
            controller.OnCommLink += Controller_OnCommLink;


            _selectedController = XboxController.RetrieveController(0);
            _selectedController.StateChanged += _selectedController_StateChanged; ;
            XboxController.StartPolling();

        }

        private void _capture_ImageGrabbed(object sender, EventArgs e)
        {
            
            Mat image = new Mat();

            _capture.Retrieve(image);
            if (_forgroundDetector == null)
            {
                _forgroundDetector = new BackgroundSubtractorMOG2();
            }

            _forgroundDetector.Apply(image, _forgroundMask);

            //update the motion history
            _motionHistory.Update(_forgroundMask);

            #region get a copy of the motion mask and enhance its color
            double[] minValues, maxValues;
            System.Drawing.Point[] minLoc, maxLoc;
            _motionHistory.Mask.MinMax(out minValues, out maxValues, out minLoc, out maxLoc);
            Mat motionMask = new Mat();
            using (ScalarArray sa = new ScalarArray(255.0 / maxValues[0]))
                CvInvoke.Multiply(_motionHistory.Mask, sa, motionMask, 1, DepthType.Cv8U);
            //Image<Gray, Byte> motionMask = _motionHistory.Mask.Mul(255.0 / maxValues[0]);
            #endregion

            //create the motion image 
            Mat motionImage = new Mat(motionMask.Size.Height, motionMask.Size.Width, DepthType.Cv8U, 3);
            //display the motion pixels in blue (first channel)
            //motionImage[0] = motionMask;
            CvInvoke.InsertChannel(motionMask, motionImage, 0);



            //storage.Clear(); //clear the storage
            Rectangle[] rects;
            using (VectorOfRect boundingRect = new VectorOfRect())
            {
                _motionHistory.GetMotionComponents(_segMask, boundingRect);
                rects = boundingRect.ToArray();
            }


            //iterate through each of the motion component
            var orderedRects = rects.OrderByDescending(r => r.Width * r.Height).ToArray();
            if (onlyUseLargest)
                orderedRects = new Rectangle[] { orderedRects.FirstOrDefault() };

            foreach (Rectangle comp in orderedRects)
            {
                int area = comp.Width * comp.Height;
                //reject the components that have small area;
                if (area < minArea) continue;

                // find the angle and motion pixel count of the specific area
                double angle, motionPixelCount;
                _motionHistory.MotionInfo(_forgroundMask, comp, out angle, out motionPixelCount);

                //reject the area that contains too few motion
                if (motionPixelCount < area * minMotionPixelsPct) continue;

                //Draw each individual motion in red
                DrawMotion(motionImage, comp, angle, new Bgr(Color.Red));

                if (autoTracking)
                {
                    if (onlyUseLargest)
                        PublishX(comp, image.Width);
                }
            }

            // find and draw the overall motion angle
            double overallAngle, overallMotionPixelCount;

            _motionHistory.MotionInfo(_forgroundMask, new Rectangle(System.Drawing.Point.Empty, motionMask.Size), out overallAngle, out overallMotionPixelCount);
            DrawMotion(motionImage, new Rectangle(System.Drawing.Point.Empty, motionMask.Size), overallAngle, new Bgr(Color.Green));
            
            if (this.Disposing || this.IsDisposed)
                return;

            imageBox1.Image = image;
            //imageBox2.Image = _forgroundMask;

            //Display the image of the motion
            imageBox3.Image = motionImage;

        }

        bool isAutoFiring = false;
        private void PublishX(Rectangle r, int imageWidth)
        {
            skipCounter++;
            if (skipCounter >= fireRate)
            {
                var mid = (r.X + (r.Width / 2));
                int servoPos = ConvertXToServoPos(mid, imageWidth);

                //Display the amount of motions found on the current image
                var txt = "mid:  " + mid + "      servo_angle: " + servoPos + "      x: " + r.X;
                if (richTextBox1.InvokeRequired)
                {
                    richTextBox1.Invoke(new Action<string>(UpdateText), txt);
                }
                else
                {
                    UpdateText(txt);
                }

                skipCounter = 0;
                controller.SendMove(servoPos);

                if (!isAutoFiring)
                {
                    richTextBox1.Invoke(new Action<string>(UpdateText), "Auto Fire ON ****");
                    isAutoFiring = true;
                    var path = System.IO.Path.Combine(
                       System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                       "identifyyourself.wav");
                    (new SoundPlayer(path)).Play();
                    controller.SendFirePrep();
                    System.Threading.Thread.Sleep(50);
                    controller.SendFire();
                }
            }
        }

        int calibratedMinServoPos = 0;
        int calibratedMaxServoPos = 180;
        int calibratedRangeServoPos = 180;

        private int ConvertXToServoPos(int mid, int imageWidth)
        {
            var pctXThroughImage = (double)mid / (double)imageWidth;
            var deltaAngleServoPos = calibratedRangeServoPos * pctXThroughImage;
            var finalServoPos = deltaAngleServoPos + calibratedMinServoPos;
            return (int)finalServoPos;
        }

        private static void DrawMotion(IInputOutputArray image, Rectangle motionRegion, double angle, Bgr color)
        {
            //CvInvoke.Rectangle(image, motionRegion, new MCvScalar(255, 255, 0));
            float circleRadius = (motionRegion.Width + motionRegion.Height) >> 2;
            System.Drawing.Point center = new System.Drawing.Point(motionRegion.X + (motionRegion.Width >> 1), motionRegion.Y + (motionRegion.Height >> 1));

            CircleF circle = new CircleF(
               center,
               circleRadius);

            int xDirection = (int)(Math.Cos(angle * (Math.PI / 180.0)) * circleRadius);
            int yDirection = (int)(Math.Sin(angle * (Math.PI / 180.0)) * circleRadius);
            System.Drawing.Point pointOnCircle = new System.Drawing.Point(
                center.X + xDirection,
                center.Y - yDirection);
            LineSegment2D line = new LineSegment2D(center, pointOnCircle);
            CvInvoke.Circle(image, System.Drawing.Point.Round(circle.Center), (int)circle.Radius, color.MCvScalar);
            CvInvoke.Line(image, line.P1, line.P2, color.MCvScalar);

        }

        private void _selectedController_StateChanged(object sender, XboxControllerStateChangedEventArgs e)
        {
            if (e.CurrentInputState.Gamepad.sThumbLX < -10000)
                button5_Click(this, new EventArgs());
            if (e.CurrentInputState.Gamepad.sThumbLX > 10000)
                button6_Click(this, new EventArgs());
            if (Convert.ToBoolean(e.CurrentInputState.Gamepad.bRightTrigger))
                Fire();
            if (Convert.ToBoolean(e.CurrentInputState.Gamepad.bLeftTrigger))
                FirePrep();
            if (e.CurrentInputState.Gamepad.wButtons == 8192) // B button
                Quit();
            if (e.CurrentInputState.Gamepad.wButtons == 16384)
                Taunt();
            if (e.CurrentInputState.Gamepad.wButtons == -32768)
                SetComPort();
        }
        

        private void Controller_OnCommLink(object sender, EventArgs e)
        {
            var path = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                    "commlinkonline.wav");
            (new SoundPlayer(path)).Play();
            button7.Enabled = button2.Enabled = button3.Enabled = button4.Enabled = button5.Enabled = button6.Enabled = textBox1.Enabled = true;
        }

        private void Controller_OnData(object sender, ArduinoControllerMain.StringEventArgs e)
        {
            if (richTextBox1.InvokeRequired)
            {
                richTextBox1.Invoke(new Action<string>(UpdateText2), e.Data);
            }
            else
            {
                UpdateText2(e.Data);
            }
        }

        private void UpdateText(string data)
        {
            richTextBox1.AppendText(data);
            richTextBox1.AppendText(Environment.NewLine);
            richTextBox1.ScrollToCaret();
        }

        private void UpdateText2(string data)
        {
            richTextBox2.AppendText(data);
            richTextBox2.AppendText(Environment.NewLine);
            richTextBox2.ScrollToCaret();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            if (richTextBox1.InvokeRequired)
            {
                richTextBox1.Invoke(new Action(Quit), null);
            }
            else
            {
                Quit();
            }

        }

        private void Quit()
        {
            if (t != null)
            {
                //this.BackColor = Color.LightGray;
                t.Stop();
                t.Dispose();
                t = null;
            }

            richTextBox1.Invoke(new Action<string>(UpdateText), "Auto Fire OFF ****");
            isAutoFiring = false;
            
            controller.SendQuit();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            controller.Shutdown();
            controller = null;
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            minArea = trackBar1.Value;
            label2.Text = minArea.ToString();
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            minMotionPixelsPct = (trackBar2.Value / 100.0);
            label3.Text = ((int)(minMotionPixelsPct * 100.0)).ToString();
        }

        private void trackBar3_Scroll(object sender, EventArgs e)
        {
            fireRate = trackBar3.Value;
            label6.Text = fireRate.ToString();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            InitCamera();
        }

        private void button9_Click(object sender, EventArgs e)
        {
            if (_capture != null)
            {
                _capture.Stop();
                _capture.Dispose();
                _capture = null;
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            richTextBox1.Text = String.Empty;
        }

        private void button11_Click(object sender, EventArgs e)
        {
            richTextBox2.Text = string.Empty;
        }

        private void button12_Click(object sender, EventArgs e)
        {
            calibratedMinServoPos = Convert.ToInt32(textBox3.Text);
            calibratedMaxServoPos = Convert.ToInt32(textBox4.Text);
            calibratedRangeServoPos = calibratedMaxServoPos - calibratedMinServoPos;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            autoTracking = (checkBox1.Checked);
        }
    }
}
