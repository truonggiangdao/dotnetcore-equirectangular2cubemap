using System;
using System.IO;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Advanced;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace dotnetTestApp
{
    class Program
    {
        static string path = "entrance_hall.png";
        // static string path = "flower_hillside_8k.png";
        static int outWidth = 512;
        static string outExt = "png";
        // static string outExt = "jpg";

        static bool smoothNearest = true;

        static void Main(string[] args)
        {
            var img = loadByUsingImageSharp(path);

            var now = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

            // directory
            string unixTimestamp = now + "";
            string folderPath = "output/" + unixTimestamp;
            Directory.CreateDirectory(folderPath);

            // save original file for comparison
            var imageOriginal = Image.LoadPixelData<Rgba32>(img.Data, img.Width, img.Height);
            FileStream oriStream = File.Create(folderPath + "/ori." + outExt);
            if (outExt == "png") {
                var oriEncoder = new PngEncoder();
                oriEncoder.BitDepth = PngBitDepth.Bit16;
                imageOriginal.SaveAsPng(oriStream, oriEncoder);
            } else {
                var oriEncoder = new JpegEncoder();
                oriEncoder.Quality = 100;
                imageOriginal.SaveAsJpeg(oriStream, oriEncoder);
            }
            // return;
            var outputs = TransformToCubeFaces(img.Data, img.Width, img.Height);
            Console.WriteLine("outputs.Length " + outputs.Length);
            string[] mapIndex = new string[]{ "px","nx","py","ny","pz", "nz" };
            for (int i = 0; i <= 5; i++) {
                var name = mapIndex[i];
                // File.WriteAllBytes(name + ".png", outputs[i].Data);
                var image = Image.LoadPixelData<Rgba32>(outputs[i].Data, outWidth, outWidth);
                FileStream DestinationStream = File.Create(folderPath + "/" + name + "." + outExt);
                if (outExt == "png") {
                    var encoder = new PngEncoder();
                    // encoder.BitDepth
                    image.SaveAsPng(DestinationStream, encoder);
                } else {
                    var encoder = new JpegEncoder();
                    encoder.Quality = 100;
                    image.SaveAsJpeg(DestinationStream, encoder);
                }
            }
            var now2 = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            Console.WriteLine("Total time: " + (now2 - now) + "s");
        }
        public static byte[] imageConversion(string imageName)
        {
            //Initialize a file stream to read the image file
            FileStream fs = new FileStream(imageName, FileMode.Open, FileAccess.Read);

            //Initialize a byte array with size of stream
            byte[] imgByteArr = new byte[fs.Length];

            //Read data from the file stream and put into the byte array
            fs.Read(imgByteArr, 0, Convert.ToInt32(fs.Length));

            //Close a file stream
            fs.Close();

            return imgByteArr;
        }

        public static ImageOutput loadByUsingImageSharp(string imageName)
        {
            var image = Image.Load<Rgba32>(imageName);
            var imgOut = new ImageOutput();
            Console.WriteLine("image.Metadata" + image.Metadata.ToString());
            imgOut.Width = image.Width;
            imgOut.Height = image.Height;
            imgOut.Data = MemoryMarshal.AsBytes(image.GetPixelSpan()).ToArray();
            Console.WriteLine("rgbaBytes " + imgOut.Data.Length);
            return imgOut;

            // using (var image = Image.Load<Rgba32>(imageName, out format))
            // {
            //     Console.WriteLine(image.Width + " " + image.Height);
            //     // using (var ms = new MemoryStream())
            //     // {
            //     //     image.Save(ms, format);
            //     //     var buff = ms.ToArray();
            //     //     Console.WriteLine(format);
            //     //     Console.WriteLine(buff.Length);
            //     // }
            //     return image;
            // }
        }

        static byte[] loadRaw()
        {
            byte[] array = File.ReadAllBytes(path);
            return array;
        }

        static byte[] loadImage()
        {
            System.Drawing.Image img = System.Drawing.Image.FromFile(path);
            using (var ms = new MemoryStream())
            {
                img.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                var buff = ms.ToArray();
                Console.WriteLine(buff.Length);
                return buff;
            }
        }

        private static byte[] ReadToEnd(Stream stream)
        {
            var memoryStream = new MemoryStream();

            stream.CopyTo(memoryStream);            

            return memoryStream.ToArray();            
        }

        public class ImageOutput
        {
            public int Width { get; set; }
            public int Height { get; set; }
            public byte[] Data { get; set; }
        }

        static ImageOutput[] TransformToCubeFaces(byte[] srcStream, int width, int height)
        {
            List<ImageOutput> imageOutputs = new List<ImageOutput>();
            
            for(int i = 0; i <= 5; i++)
            {
                ImageOutput imageOutPut = new ImageOutput
                {
                    Height = outWidth,
                    Width = outWidth,
                    Data = new byte[4 * outWidth * outWidth],
                };
                imageOutputs.Add(imageOutPut);
            }

            imageOutputs.ToArray();

            List<ImageOutput> listOutPut = new List<ImageOutput>();
            
            for (int i=0; i < 6; i++)
            {
                ImageOutput result = ConvertToCubeFaces(width, height, srcStream, imageOutputs[i], i);

                listOutPut.Add(result);

            }
            return listOutPut.ToArray();
        }

        static ImageOutput ConvertToCubeFaces(int width, int height, byte[] dataImage, ImageOutput imageOutput, int faceIdx)
        {
            try
            {
                var thetaFlip = 1;
                var edge = imageOutput.Width|0;
                var inWidth = width|0;
                var inHeight = height|0;
                var inData = dataImage;

                // var smoothNearest = false;

                var faceData = imageOutput.Data;
                var faceWidth = imageOutput.Width|0;
                var faceHeight = imageOutput.Height|0;
                var face = faceIdx|0;

                var iFaceWidth2 = 2.0 / faceWidth;
                var iFaceHeight2 = 2.0 / faceHeight;
                Console.WriteLine("width " + width + " height " + height + " Length " + dataImage.Length);

                for (var j = 0; j < faceHeight; ++j)
                {
                    for (var i = 0; i < faceWidth; ++i)
                    {
                        var a = iFaceWidth2 * i;
                        var b = iFaceHeight2 * j;
                        var outPos = (i + (j * edge)) << 2;
                        var x = 0.0;
                        var y = 0.0;
                        var z = 0.0;

                        switch (face)
                        {
                            case 0:
                                x = 1.0 - a; y = 1.0; z = 1.0 - b;
                                break; // right  (+x)
                            case 1:
                                x = a - 1.0; y = -1.0; z = 1.0 - b;
                                break; // left   (-x)
                            case 2:
                                x = b - 1.0; y = a - 1.0; z = 1.0;
                                break; // top    (+y)
                            case 3:
                                x = 1.0 - b; y = a - 1.0; z = -1.0;
                                break; // bottom (-y)
                            case 4:
                                x = 1.0; y = a - 1.0; z = 1.0 - b;
                                break; // front  (+z)
                            case 5:
                                x = -1.0; y = 1.0 - a; z = 1.0 - b;
                                break; // back   (-z)
                        }

                        var theta = thetaFlip * Math.Atan2(y, x);
                        var rad = Math.Sqrt((x * x) + (y * y));
                        var phi = Math.Atan2(z, rad);

                        var uf = 2.0 * (inWidth / 4) * (theta + Math.PI) / Math.PI;
                        var vf = 2.0 * (inWidth / 4) * ((Math.PI / 2) - phi) / Math.PI;
                        var ui = Convert.ToInt32(Math.Floor(uf))|0;
                        var vi = Convert.ToInt32(Math.Floor(vf))|0;

                        if (smoothNearest) {
                            var inPos = Convert.ToInt32(((ui % inWidth) + inWidth * Clamp(vi, 0, inHeight-1))) << 2;
                            faceData[outPos + 0] = Convert.ToByte(inData[inPos + 0] | 0);
                            faceData[outPos + 1] = Convert.ToByte(inData[inPos + 1] | 0);
                            faceData[outPos + 2] = Convert.ToByte(inData[inPos + 2] | 0);
                            faceData[outPos + 3] = Convert.ToByte(inData[inPos + 3] | 0);
                        } else {

                            // bilinear blend
                            var u2 = ui + 1;
                            var v2 = vi + 1;
                            var mu = uf - ui;
                            var nu = vf - vi;
                            // Console.WriteLine("i " + i + " j " + j);
                            var pA = Convert.ToInt32((ui % inWidth) + inWidth * Clamp(vi, 0, (inHeight - 1))) << 2;
                            var pB = Convert.ToInt32((u2 % inWidth) + inWidth * Clamp(vi, 0, (inHeight - 1))) << 2;
                            var pC = Convert.ToInt32((ui % inWidth) + inWidth * Clamp(v2, 0, (inHeight - 1))) << 2;
                            var pD = Convert.ToInt32((u2 % inWidth) + inWidth * Clamp(v2, 0, (inHeight - 1))) << 2;
                            // Console.WriteLine("pA " + pA + " pB " + pB + " pC " + pC + " pD " + pD);

                            var aA = (inData[pA + 3]|0) * (1.0 / 255.0);
                            var aB = (inData[pB + 3]|0) * (1.0 / 255.0);
                            var aC = (inData[pC + 3]|0) * (1.0 / 255.0);
                            var aD = (inData[pD + 3]|0) * (1.0 / 255.0);
                            // Console.WriteLine(" aA " + aA + " aB " + aB + " aC " + aC + " aD " + aD);

                            // Do the bilinear blend in linear space.
                            var rA = SrgbToLinear(inData[pA + 0]|0) * aA;
                            var gA = SrgbToLinear(inData[pA + 1]|0) * aA;
                            var bA = SrgbToLinear(inData[pA + 2]|0) * aA;

                            var rB = SrgbToLinear(inData[pB + 0]|0) * aB;
                            var gB = SrgbToLinear(inData[pB + 1]|0) * aB;
                            var bB = SrgbToLinear(inData[pB + 2]|0) * aB;

                            var rC = SrgbToLinear(inData[pC + 0]|0) * aC;
                            var gC = SrgbToLinear(inData[pC + 1]|0) * aC;
                            var bC = SrgbToLinear(inData[pC + 2]|0) * aC;

                            var rD = SrgbToLinear(inData[pD + 0]|0) * aD;
                            var gD = SrgbToLinear(inData[pD + 1]|0) * aD;
                            var bD = SrgbToLinear(inData[pD + 2]|0) * aD;

                            var r = (rA * (1.0 - mu) * (1.0 - nu) + rB * mu * (1.0 - nu) + rC * (1.0 - mu) * nu + rD * mu * nu);
                            var g = (gA * (1.0 - mu) * (1.0 - nu) + gB * mu * (1.0 - nu) + gC * (1.0 - mu) * nu + gD * mu * nu);
                            var c = (bA * (1.0 - mu) * (1.0 - nu) + bB * mu * (1.0 - nu) + bC * (1.0 - mu) * nu + bD * mu * nu);
                            var d = (aA * (1.0 - mu) * (1.0 - nu) + aB * mu * (1.0 - nu) + aC * (1.0 - mu) * nu + aD * mu * nu);
                            var ia = 1.0 / d;
                            faceData[outPos + 0] = Convert.ToByte(LinearToSRGB((r * ia)) |0);
                            faceData[outPos + 1] = Convert.ToByte(LinearToSRGB((g * ia)) |0);
                            faceData[outPos + 2] = Convert.ToByte(LinearToSRGB((c * ia)) |0);
                            faceData[outPos + 3] = Convert.ToByte(Convert.ToInt32((d * 255.0)) |0);
                        }

                        imageOutput.Data = faceData;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return imageOutput;
        }

        static double Clamp(double v, double lo, double hi)
        {
            return Math.Min(hi, Math.Max(lo, v));
        }

        static double SrgbToLinear(double v)
        {
            var component = (+v * (1.0 / 255.0));
            return component * component;
        }

        static int LinearToSRGB(double v)
        {
            var val = Math.Sqrt(v) * 255.0;
            var doVal = Convert.ToInt32(val);
            return doVal;
        }

    }
}
