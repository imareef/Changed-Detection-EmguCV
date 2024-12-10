using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Drawing;
using Timer = System.Windows.Forms.Timer;

namespace ObjectDetection
{
    public partial class MainForm : Form
    {

        private VideoCapture _videoCapture;
        private Mat _currentFrame = new Mat();
        private Mat _backgroundFrame = new Mat();
        private BackgroundSubtractorMOG2 _backgroundSubtractor;
        private Timer _timer;
        private bool _isObjectDetected = false;

        private int _frameCounter = 0; 
        private const int FrameSkipThreshold = 10;

        public MainForm()
        {
            InitializeComponent();
            InitializeVideoProcessing();
        }

        private void InitializeVideoProcessing()
        {
            _backgroundSubtractor = new BackgroundSubtractorMOG2(500, 16, true);
            _timer = new Timer();
            _timer.Interval = 30; 
            _timer.Tick += ProcessFrame;
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Video Files|*.mp4;*.avi;*.mov"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                _videoCapture = new VideoCapture(openFileDialog.FileName);
                _backgroundFrame = null; // Reset background frame
                _timer.Start();
            }
        }

        private void ProcessFrame(object sender, EventArgs e)
        {
            if (_videoCapture == null || !_videoCapture.IsOpened) return;

            var frame = new Mat();
            _videoCapture.Read(frame);

            if (frame.IsEmpty)
            {
                _timer.Stop(); 
                return;
            
            
            }

            _frameCounter++;

            pictureBox1.Image = frame.ToBitmap();

            var foregroundMask = new Mat();
            _backgroundSubtractor.Apply(frame, foregroundMask);

            if (foregroundMask == null || foregroundMask.IsEmpty)
            {
                return; 
            }

            Mat kernel = CvInvoke.GetStructuringElement(Emgu.CV.CvEnum.ElementShape.Rectangle, new Size(5, 5), new Point(-1, -1));
            CvInvoke.MorphologyEx(foregroundMask, foregroundMask, Emgu.CV.CvEnum.MorphOp.Open, kernel, new Point(-1, -1), 2, Emgu.CV.CvEnum.BorderType.Default, new MCvScalar());
            CvInvoke.MorphologyEx(foregroundMask, foregroundMask, Emgu.CV.CvEnum.MorphOp.Close, kernel, new Point(-1, -1), 2, Emgu.CV.CvEnum.BorderType.Default, new MCvScalar());

            bool objectDetectedInCurrentFrame = false;


            using (var contours = new VectorOfVectorOfPoint())
            {
                CvInvoke.FindContours(foregroundMask, contours, null, Emgu.CV.CvEnum.RetrType.External, Emgu.CV.CvEnum.ChainApproxMethod.ChainApproxSimple);

                foreach (var contour in contours.ToArrayOfArray())
                {
                    double area = CvInvoke.ContourArea(new VectorOfPoint(contour));

                    if (area < 5000) continue; 

                    var boundingRect = CvInvoke.BoundingRectangle(new VectorOfPoint(contour));

                    if (boundingRect.Width < 50 || boundingRect.Height < 50) continue;

                    objectDetectedInCurrentFrame = true;

                    CvInvoke.Rectangle(frame, boundingRect, new MCvScalar(0, 255, 0), 2);
                    CvInvoke.PutText(frame, "Object", new Point(boundingRect.X, boundingRect.Y - 10),
                        Emgu.CV.CvEnum.FontFace.HersheySimplex, 0.5, new MCvScalar(0, 255, 0), 2);
                }
            }

            if (objectDetectedInCurrentFrame && !_isObjectDetected)
            {
                LogDetectionMessage(); 
            }

            _isObjectDetected = objectDetectedInCurrentFrame;

            pictureBox2.Image = frame.ToBitmap();
        }

        private void LogDetectionMessage()
        {
            if (_frameCounter == 1)
                return;
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string message = $"Changed at {timestamp}";
            listBoxlog.Items.Add(message); 
        }

    }
}
