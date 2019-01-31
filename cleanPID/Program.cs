// ------------------------------------------------------------ 
// Blog post code sample! Read the post on http://en.morzel.net
// ------------------------------------------------------------

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Timers;




namespace EmguApp
{
    class Program
    {
        // Determines boundary of brightness while turning grayscale image to binary (black-white) image
        private const int Threshold = 30;

        // Erosion to remove noise (reduce white pixel zones)
        private const int ErodeIterations = 3;

        // Dilation to enhance erosion survivors (enlarge white pixel zones)
        private const int DilateIterations = 3;

        // Window names used in CvInvoke.Imshow calls
        
        private const string FinalFrameWindowName = "Final Frame";

        // Containers for images demonstrating different phases of frame processing 
        private static Mat rawFrame = new Mat(); // Frame as obtained from video
        private static Mat backgroundFrame = new Mat(); // Frame used as base for change detection
        private static Mat diffFrame = new Mat(); // Image showing differences between background and raw frame
        private static Mat grayscaleDiffFrame = new Mat(); // Image showing differences in 8-bit color depth
        private static Mat binaryDiffFrame = new Mat(); // Image showing changed areas in white and unchanged in black
        private static Mat denoisedDiffFrame = new Mat(); // Image with irrelevant changes removed with opening operation
        private static Mat finalFrame = new Mat(); // Video frame with detected object marked

        private static MCvScalar drawingColor = new Bgr(Color.Red).MCvScalar;

        // variables related to the control 
        private static byte Voltage = 0;
        private static readonly int u = 200;
        private static SerialPort _serialPort = new SerialPort("COM8");
        private static int Initialiser = 0;

        private static int e = 0, r = 0;
        private static double Kp = 0.07, Ki = 0.0002, du = 0;
        private static int Integral = 0;
        private static long temps = 0;
     
        static void Main(string[] args)
        {
            /***********************Serial communication setup ********************/
            _serialPort.Open();
            _serialPort.ReadTimeout = 500;
            var dataArray = new byte[] { Voltage };
            _serialPort.Write(dataArray, 0, 1);
            /***************************/

            int videoFile = 0; // 0 to use the default camera
            
            using (var capture = new VideoCapture(videoFile)) // launching the streaming
            {
                if (capture.IsOpened)
                {
                    // loading the background frame 
                    
                    backgroundFrame = new Mat(@"C:\Users\My PC\Desktop\MyBackground.jpg");
                   

                    // Handling video frames (image processing and contour detection)

                    VideoProcessingLoop(capture, backgroundFrame);
                }
                else
                {
                    Console.WriteLine($"Unable to open {videoFile}");
                }
            }
        }

        private static void VideoProcessingLoop(VideoCapture capture, Mat backgroundFrame)
        {           
            var time = new Stopwatch();
            time.Restart();

            int frameNumber = 1;
            while (true) // Loop video
            {
                temps = time.ElapsedMilliseconds;

                rawFrame = capture.QueryFrame(); // Getting next frame (null is returned if no further frame exists)

                
                
                    frameNumber++;

                    ProcessFrame(backgroundFrame, Threshold, ErodeIterations, DilateIterations);

                    WriteFrameInfo( frameNumber);
                    ShowWindowsWithImageProcessingStages();

                    
                    int key = CvInvoke.WaitKey(40);
                    // Close program if Esc key was pressed (any other key moves to next frame)

                    //manual controller
                    if (key == 13) // switch to manual if Enter is pressed
                        while (true) // still in manual till Enter is pressed again 
                        {
                            Integral = 0;
                            key = CvInvoke.WaitKey(0);
                            if (key == 113) du = 90;
                            if (key == 119) du = 100;
                            if (key == 101) du = 110;
                            if (key == 114) du = 130;
                            if (key == 116) du = 150;
                            if (key == 122) du = 0;
                            if (key == 13) break;

                            Voltage = Convert.ToByte(du);
                            var dataArray = new byte[] { Voltage };
                            _serialPort.Write(dataArray, 0, 1);


                            if (key == 27) Environment.Exit(0);
                        }

                    if (key == 27)
                        Environment.Exit(0);

                while(time.ElapsedMilliseconds - temps < 100) // to work on evenly spaced intervals 
                { }
                
            }
        }

        private static void ProcessFrame(Mat backgroundFrame, int threshold, int erodeIterations, int dilateIterations)
        {
            // Find difference between background (first) frame and current frame
            CvInvoke.AbsDiff(backgroundFrame, rawFrame, diffFrame);

            // Apply binary threshold to grayscale image (white pixel will mark difference)
            CvInvoke.CvtColor(diffFrame, grayscaleDiffFrame, ColorConversion.Bgr2Gray);
            CvInvoke.Threshold(grayscaleDiffFrame, binaryDiffFrame, threshold, 255, ThresholdType.Binary);

            // Remove noise with opening operation (erosion followed by dilation)
            CvInvoke.Erode(binaryDiffFrame, denoisedDiffFrame, null, new Point(-1, -1), erodeIterations, BorderType.Default, new MCvScalar(1));
            CvInvoke.Dilate(denoisedDiffFrame, denoisedDiffFrame, null, new Point(-1, -1), dilateIterations, BorderType.Default, new MCvScalar(1));

            rawFrame.CopyTo(finalFrame);
            DetectObject(denoisedDiffFrame, finalFrame);
        }

        private static void DetectObject(Mat detectionFrame, Mat displayFrame)
        {
            using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
            {
                // Build list of contours
                CvInvoke.FindContours(detectionFrame, contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);

                // Selecting largest contour
                if (contours.Size > 0)
                {
                    double maxArea = 0;
                    int chosen = 0;
                    for (int i = 0; i < contours.Size; i++)
                    {
                        VectorOfPoint contour = contours[i];

                        double area = CvInvoke.ContourArea(contour);
                        if (area > maxArea)
                        {
                            maxArea = area;
                            chosen = i;
                        }
                    }

                    // Draw on a frame
                    MarkDetectedObject(displayFrame, contours[chosen], maxArea);
                }
            }
        }

        private static void WriteFrameInfo( int frameNumber)
        {
            var info = new string[] {
                $"Frame Number: {frameNumber}",
                
            };

            WriteMultilineText(finalFrame, info, new Point(5, 10));
        }

        private static void ShowWindowsWithImageProcessingStages()
        {
            CvInvoke.Imshow(FinalFrameWindowName, finalFrame);
        }

        private static void MarkDetectedObject(Mat frame, VectorOfPoint contour, double area)
        {
            // Getting minimal rectangle which contains the contour
            Rectangle box = CvInvoke.BoundingRectangle(contour);

            // Drawing contour and box around it
            CvInvoke.Polylines(frame, contour, true, drawingColor);
            CvInvoke.Rectangle(frame, box, drawingColor);

            // Write information next to marked object
            Point center = new Point(box.X + box.Width / 2, box.Y + box.Height / 2);

            //the controller :
            Initialiser++; 
            if (Initialiser > 50) // this condition is to wait for the camera to be stable
            {
                
                // PI controller :
                e = center.Y - u;

                if (Ki * Integral <= 10) Integral += e;
                
                du = Kp * e + Ki * Integral;

                r = (int)(160 + du);
                if (r > 255) r = 255;
                if (r < 0) r = 0;

                Voltage = Convert.ToByte(r);
                if (Voltage > 200) Voltage = 200;
                if (Voltage < 110) Voltage = 110;
                //printing altitude measurement and input value.
                Console.WriteLine("y(" + Initialiser + ") = " + (480 - center.Y) + ";" + "t(" + Initialiser + ") =" + temps + ";" + "Voltage(" + Initialiser + ") =" + Voltage + ";" );
                //end of PI controller
            }
            else Voltage = 0;
           
            //serial communication launch
            var dataArray = new byte[] { Voltage };
            _serialPort.Write(dataArray, 0, 1);

            //end of serial communication
            var info = new string[] {
                $"Area: {area}",
                $"Position: {center.X}, {center.Y}"
            };

            WriteMultilineText(frame, info, new Point(box.Right + 5, center.Y));
        }

        private static void WriteMultilineText(Mat frame, string[] lines, Point origin)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                int y = i * 10 + origin.Y; // Moving down on each line
                CvInvoke.PutText(frame, lines[i], new Point(origin.X, y), FontFace.HersheyPlain, 0.8, drawingColor);
            }
        }
    }
}
