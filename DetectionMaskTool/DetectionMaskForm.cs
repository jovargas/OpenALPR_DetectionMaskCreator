/*  
    Detection Mask Tool for OpenALPR 
    Created by Joel Vargas
    CC 2017    
 */

using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using System.Windows.Forms;
using System.Drawing.Imaging;
using Emgu.CV.Util;

namespace DetectionMaskTool
{
    public partial class DetectionMaskForm : Form
    {
        private List<List<Point>> Polygons;
        private List<Point> NewPolygon;
        private Point NewPoint;
        Image referenceImage;
        Image<Gray, byte> maskImage;
        public float scaleFactor;
        public DetectionMaskForm()
        {
            InitializeComponent();            
        }

        private void DetectionMaskForm_Load(object sender, EventArgs e)
        {
            Polygons = new List<List<Point>>();
            NewPolygon = null;
            NewPoint = new Point();
            referenceImage = null;
            maskImage = null;                  
        }

        private void pcbMask_MouseClick(object sender, MouseEventArgs e)
        {         
        }

        private void pcbMask_MouseDown(object sender, MouseEventArgs e)
        {
            // See if we are already drawing a polygon.
            if (NewPolygon != null)
            {
                // We are already drawing a polygon.
                // If it's the right mouse button, finish this polygon.
                if (e.Button == MouseButtons.Right)
                {
                    // Finish this polygon.
                    if (NewPolygon.Count > 2) Polygons.Add(NewPolygon);
                    NewPolygon = null;
                }
                else
                {
                    // Add a point to this polygon.
                    if (NewPolygon[NewPolygon.Count - 1] != e.Location)
                    {
                        NewPolygon.Add(e.Location);
                    }
                }
            }
            else
            {
                // Start a new polygon.
                NewPolygon = new List<Point>();
                NewPoint = e.Location;
                NewPolygon.Add(e.Location);
            }

            // Redraw.
            pcbMask.Invalidate();
        }

        private void pcbMask_MouseMove(object sender, MouseEventArgs e)
        {
            if (NewPolygon == null) return;
            NewPoint = e.Location;
            pcbMask.Invalidate();
        }

        private void pcbMask_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            //e.Graphics.Clear(pcbMask.BackColor);

            // Draw the old polygons.
            foreach (List<Point> polygon in Polygons)
            {
                SolidBrush semiTransBrush = new SolidBrush(Color.FromArgb(128, 0, 0, 255));
                e.Graphics.FillPolygon(semiTransBrush, polygon.ToArray(), FillMode.Alternate);
                e.Graphics.DrawPolygon(new Pen(Color.Magenta, 2), polygon.ToArray());
            }

            // Draw the new polygon.
            if (NewPolygon != null)
            {
                // Draw the new polygon.
                if (NewPolygon.Count > 1)
                {                    
                    e.Graphics.DrawLines(new Pen(Color.Magenta, 2), NewPolygon.ToArray());
                }

                // Draw the newest edge.
                if (NewPolygon.Count > 0)
                {
                    using (Pen dashed_pen = new Pen(Color.Magenta))
                    {
                        dashed_pen.DashPattern = new float[] { 3, 3 };
                        dashed_pen.Width = 2;
                        e.Graphics.DrawLine(dashed_pen, NewPolygon[NewPolygon.Count - 1], NewPoint);
                    }
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {                                        
            DialogResult dialogResult = MessageBox.Show("Save the mask?", "Save confirmation", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                if (Polygons.Count > 0)
                    maskImage = new Image<Gray, byte>(pcbMask.Width, pcbMask.Height, new Gray(0.0));
                else
                    maskImage = new Image<Gray, byte>(pcbMask.Width, pcbMask.Height, new Gray(255));
                foreach (var polygon in Polygons)
                {
                    Point[] points = new Point[polygon.Count];
                    for (int i = 0; i < polygon.Count; i++)
                    {
                        points[i] = polygon.ElementAt(i);
                    }
                    maskImage.Draw(points, new Gray(255), -1, LineType.AntiAlias, default(Point));
                }
                maskImage = maskImage.Resize(referenceImage.Width, referenceImage.Height, Inter.Linear);
                Rectangle roi = GetMaskROI(maskImage.Clone());
                var referenceImg = new Image<Bgr, byte>((Bitmap)referenceImage);
                var referenceImg2 = new Image<Bgr, byte>((Bitmap)referenceImage);
                CvInvoke.Rectangle(referenceImg, roi, new MCvScalar(255, 0, 255), 2);
                if (chbxDebugImgs.Checked)
                {
                    CvInvoke.Imshow("Mask ROI - Size: " + roi.Width.ToString() + "x" + roi.Height.ToString(), referenceImg);
                    ShowMaskedImage(referenceImg2, maskImage, roi);
                }                
                saveFileDialog1.ShowDialog();
                if (saveFileDialog1.FileName != "mask.jpg")
                {                    
                    FileStream fs = (FileStream)saveFileDialog1.OpenFile();
                    maskImage.ToBitmap().Save(fs, ImageFormat.Jpeg);
                    fs.Close();
                }                
            }          
        }

        private void saveJpeg(string path, Image img, long quality)
        {
            // Encoder parameter for image quality

            EncoderParameter qualityParam = new EncoderParameter(Encoder.Quality, quality);

            // Jpeg image codec
            ImageCodecInfo jpegCodec = getEncoderInfo("image/jpeg");

            if (jpegCodec == null)
                return;

            EncoderParameters encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = qualityParam;

            img.Save(path, jpegCodec, encoderParams);
        }
        private ImageCodecInfo getEncoderInfo(string mimeType)
        {            
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
                        
            for (int i = 0; i < codecs.Length; i++)
                if (codecs[i].MimeType == mimeType)
                    return codecs[i];
            return null;
        }

        delegate void SetPicBoxMaskCallback(Image img);
        public void SetPicBoxMaskImg(Image img)
        {
            if (pcbMask.InvokeRequired)
            {
                SetPicBoxMaskCallback d = new SetPicBoxMaskCallback(SetPicBoxMaskImg);
                Invoke(d, new object[] { img });
            }
            else
            {
                pcbMask.Image = img;
                pcbMask.Refresh();
            }
        }

        delegate void SetPicBoxMaskSizeCallback(int width, int height);
        private void SetPicBoxMaskSize(int width, int height)
        {
            if (pcbMask.InvokeRequired)
            {
                SetPicBoxMaskSizeCallback d = new SetPicBoxMaskSizeCallback(SetPicBoxMaskSize);
                Invoke(d, new object[] { width, height });
            }
            else
            {
                pcbMask.Size = new Size(width,height);                
            }
        }
        delegate void SetFormSizeCallback(int width, int height);
        private void SetFormSize(int width, int height)
        {
            if (pcbMask.InvokeRequired)
            {
                SetFormSizeCallback d = new SetFormSizeCallback(SetFormSize);
                Invoke(d, new object[] { width, height });
            }
            else
            {
                this.Size = new Size(width, height);
            }
        }
        private void btnClearMask_Click(object sender, EventArgs e)
        {            
            Polygons = new List<List<Point>>();
            pcbMask.Refresh();            
        }
        private void btnLoadImage_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                referenceImage = new Bitmap(openFileDialog1.OpenFile());
                Polygons = new List<List<Point>>();
                pcbMask.Refresh();
            }
            else
            {
                return;
            }
            scaleFactor = ((float)referenceImage.Width / (float)referenceImage.Height);
            SetPicBoxMaskImg(referenceImage);            
            btnClearMask.Enabled = true;
            btnSave.Enabled = true;            
        }
        private Rectangle GetMaskROI(Image<Gray, byte> mask)
        {
            Rectangle roi = new Rectangle();
            CvInvoke.Threshold(mask, mask, 55, 255, ThresholdType.Binary);
            VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
            //var hierarchy = new Mat();
            //double largest_area = 0;            
            CvInvoke.FindContours(mask, contours, null, RetrType.Ccomp, ChainApproxMethod.ChainApproxSimple);
            // iterate through each contour.
            int right = 0, left = 999999, top = 9999999, bottom = 0;
            for (int i = 0; i < contours.Size; i++)
            {

                ////  Find the area of contour
                //double a = CvInvoke.ContourArea(contours[i], false);
                ////if (a > largest_area)                
                //{
                //    // Find the bounding rectangle for biggest contour
                //    roi = CvInvoke.BoundingRectangle(contours[i]);
                //}
                roi = CvInvoke.BoundingRectangle(contours[i]);
                if (roi.Y < top)
                    top = roi.Y;
                if (roi.X < left)
                    left = roi.X;
                if ((roi.X + roi.Width) > right)
                    right = roi.X + roi.Width;
                if ((roi.Y + roi.Height) > bottom)
                    bottom = roi.Y + roi.Height;
            }
            roi = new Rectangle(left, top, right - left, bottom - top);
            //===================================================
            // Calculate the biggest rectangle that covers all the whitespace
            // Rather than using contours, go row by row, column by column until you hit
            // a white pixel and stop.  Should be faster and a little simpler
            //int top_bound = 0, bottom_bound = 0, left_bound = 0, right_bound = 0;
            //Mat maskMat = new Mat(mask.Mat, mask.ROI);
            //int i;
            //for (top_bound = 0; top_bound < maskMat.Rows; top_bound++)
            //    if (CvInvoke.CountNonZero(maskMat.GetRow(top_bound)) > 0) break;
            //for (bottom_bound = maskMat.Rows - 1; bottom_bound >= 0; bottom_bound--)
            //    if (CvInvoke.CountNonZero(maskMat.GetRow(bottom_bound)) > 0) break;            
            //for (left_bound = 0; left_bound < maskMat.Cols; left_bound++)
            //    if (CvInvoke.CountNonZero(maskMat.) > 0) break;            
            //for (right_bound = maskMat.Cols - 1; right_bound >= 0; right_bound--)
            //    if (CvInvoke.CountNonZero(mask.GetCol(right_bound)) > 0) break;

            //if (left_bound >= right_bound || top_bound >= bottom_bound)
            //{
            //    // Invalid mask, set it to 0 width/height
            //    roi.X = 0;
            //    roi.Y = 0;
            //    roi.Width = 0;
            //    roi.Height = 0;
            //}
            //else
            //{
            //    roi.X = left_bound;
            //    roi.Y = top_bound;
            //    roi.Width = right_bound - left_bound;
            //    roi.Height = bottom_bound - top_bound;
            //}
            ////===================================================

            return roi;
        }
        private void ShowMaskedImage(Image<Bgr, byte> img, Image<Gray, byte> mask, Rectangle maskRoi)
        {
            Image<Bgr, float> maskF = mask.Convert<Bgr, float>();            
            Image<Bgr, float> imageF = img.Convert<Bgr, float>();
            img = imageF.Mul(maskF).Convert<Bgr, byte>();

            CvInvoke.Imshow("Mask", mask);
            img.ROI = maskRoi;            

            CvInvoke.Imshow("Masked image", img);
            
            maskF.Dispose();            
            imageF.Dispose();
        }
        private void btnClose_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("Are you sure?", "Close confirmation", MessageBoxButtons.YesNo);
            if(dialogResult == DialogResult.Yes)
                Close();
        }
    }
}
