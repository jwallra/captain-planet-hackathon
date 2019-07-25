using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Newtonsoft.Json.Linq;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using Xamarin.Forms;

namespace CaptainPlanet
{
    public partial class MainPage : ContentPage
    {
        const float radius = 2.0f;
        const float xDrop = 2.0f;
        const float yDrop = 2.0f;

        public MainPage()
        {
            InitializeComponent();

            var vm = (MainViewModel)BindingContext;
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.Image) || e.PropertyName == nameof(vm.Predictions))
                {
                    ImageCanvas.InvalidateSurface();
                }
            };
        }

        public void OnCanvasViewPaintSurface(object sender, SKPaintSurfaceEventArgs args)
        {
            var vm = (MainViewModel)BindingContext;

            var info = args.Info;
            var canvas = args.Surface.Canvas;

            ClearCanvas(info, canvas);

            if (vm.Image != null)
            {
                var scale = Math.Min((float)info.Width / (float)vm.Image.Width, (float)info.Height / (float)vm.Image.Height);

                var scaleHeight = scale * vm.Image.Height;
                var scaleWidth = scale * vm.Image.Width;

                var top = (info.Height - scaleHeight) / 2;
                var left = (info.Width - scaleWidth) / 2;

                canvas.DrawBitmap(vm.Image, new SKRect(left, top, left + scaleWidth, top + scaleHeight));
                DrawBorder(canvas, left, top, scaleWidth, scaleHeight);
                DrawPredictionsAsync(vm, canvas, left, top, scale);
            }
        }

        static void DrawPredictionsAsync(MainViewModel vm, SKCanvas canvas, float left, float top, float scaleFactor)
        {
            if (vm.Predictions == null) return;

            if (!vm.Predictions.Any())
            {
                LabelPrediction(canvas, "Nothing detected", new BoundingRect(0, 0, 1, 1), left, top, scaleFactor, SKColors.DarkGray, false);
            }
            else if (vm.Predictions.All(p => p.Rectangle != null))
            {
                foreach (var prediction in vm.Predictions)
                {
                    SKColor predictionColor;
                    if (IsCompostable(vm))
                    {
                        predictionColor = SKColors.Green;
                    }
                    else if (IsRecyclable(vm))
                    {
                        predictionColor = SKColors.Blue;
                    }
                    else
                    {
                        predictionColor = SKColors.Red;
                    }
                    LabelPrediction(canvas, prediction.ObjectProperty, prediction.Rectangle, left, top, scaleFactor, predictionColor);
                }
            }
            else
            {
                var best = vm.Predictions.OrderByDescending(p => p.Confidence).First();
                LabelPrediction(canvas, best.ObjectProperty, new BoundingRect(0, 0, 1, 1), left, top, scaleFactor, SKColors.DarkGray, false);
            }
        }

        static void ClearCanvas(SKImageInfo info, SKCanvas canvas)
        {
            var paint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = new SKColor(0, 0, 0, 0)
            };
            canvas.Clear();
            canvas.DrawRect(info.Rect, paint);
        }

        static void LabelPrediction(SKCanvas canvas, string tag, BoundingRect box, float left, float top, float scaleFactor, SKColor color, bool addBox = true)
        {
            var scaledBoxLeft = left + (scaleFactor * box.X);
            var scaledBoxWidth = scaleFactor * box.W;
            var scaledBoxTop = top + (scaleFactor * box.Y);
            var scaledBoxHeight = scaleFactor * box.H;

            if (addBox)
                DrawBox(canvas, scaledBoxLeft, scaledBoxTop, scaledBoxWidth, scaledBoxHeight, color);

            DrawText(canvas, tag, scaledBoxLeft, scaledBoxTop, scaledBoxWidth, scaledBoxHeight, color);
        }

        static void DrawText(SKCanvas canvas, string tag, float startLeft, float startTop, float scaledBoxWidth, float scaledBoxHeight, SKColor color)
        {
            var textPaint = new SKPaint
            {
                IsAntialias = true,
                Color = color,
                Style = SKPaintStyle.Fill,
                Typeface = SKTypeface.FromFamilyName("Arial")
            };

            var text = tag;

            var textWidth = textPaint.MeasureText(text);
            textPaint.TextSize = 0.9f * scaledBoxWidth * textPaint.TextSize / textWidth;

            var textBounds = new SKRect();
            textPaint.MeasureText(text, ref textBounds);

            var xText = (startLeft + (scaledBoxWidth / 2)) - textBounds.MidX;
            var yText = (startTop + (scaledBoxHeight / 2)) + textBounds.MidY;

            var paint = new SKPaint
            {
                Style = SKPaintStyle.Stroke
            };

            var backgroundRect = textBounds;
            backgroundRect.Offset(xText, yText);
            backgroundRect.Inflate(10, 10);

            canvas.DrawRoundRect(backgroundRect, 5, 5, paint);

            canvas.DrawText(text,
                            xText,
                            yText,
                            textPaint);
        }

        static void DrawBox(SKCanvas canvas, float startLeft, float startTop, float scaledBoxWidth, float scaledBoxHeight, SKColor color)
        {
            var strokePaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                // Color = color,
                StrokeWidth = 5,
                PathEffect = SKPathEffect.CreateDash(new[] { 20f, 20f }, 20f)
            };
            DrawBox(canvas, strokePaint, startLeft, startTop, scaledBoxWidth, scaledBoxHeight);

            var blurStrokePaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 5,
                PathEffect = SKPathEffect.CreateDash(new[] { 20f, 20f }, 20f),
                IsAntialias = true,
                MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 0.57735f * radius + 0.5f)
            };
            DrawBox(canvas, blurStrokePaint, startLeft, startTop, scaledBoxWidth, scaledBoxHeight);
        }

        static void DrawBorder(SKCanvas canvas, float startLeft, float startTop, float scaledBoxWidth, float scaledBoxHeight)
        {
            var strokePaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                Color = SKColors.DarkSlateBlue,
                StrokeWidth = 1
            };

            DrawBox(canvas, strokePaint, startLeft, startTop, scaledBoxWidth, scaledBoxHeight);
        }

        static void DrawBox(SKCanvas canvas, SKPaint paint, float startLeft, float startTop, float scaledBoxWidth, float scaledBoxHeight)
        {
            var path = CreateBoxPath(startLeft, startTop, scaledBoxWidth, scaledBoxHeight);
            canvas.DrawPath(path, paint);
        }

        static SKPath CreateBoxPath(float startLeft, float startTop, float scaledBoxWidth, float scaledBoxHeight)
        {
            var path = new SKPath();
            path.MoveTo(startLeft, startTop);

            path.LineTo(startLeft + scaledBoxWidth, startTop);
            path.LineTo(startLeft + scaledBoxWidth, startTop + scaledBoxHeight);
            path.LineTo(startLeft, startTop + scaledBoxHeight);
            path.LineTo(startLeft, startTop);

            return path;
        }

        private static bool IsCompostable(MainViewModel vm)
        {
            var compostableObjects = vm.Predictions.Where(c => MainViewModel.CompostableWords.Contains(c.ObjectProperty.ToLower()));

            if (!compostableObjects.Any())
            {
                HttpResponseMessage response;
                var bestCategory = "trash";
                using (var httpClient = new HttpClient())
                {
                    httpClient.BaseAddress = new Uri("http://bananapi.westus.cloudapp.azure.com:5000");

                    using (MemoryStream memStream = new MemoryStream())
                    using (SKManagedWStream wstream = new SKManagedWStream(memStream)) {
                        var result = vm.Image.PeekPixels().Encode(wstream, SKEncodedImageFormat.Jpeg, 100);
                    }

                    var byteArrayContent = new ByteArrayContent(vm.Image.Bytes, 0, vm.Image.Bytes.Length);
                    byteArrayContent.Headers.Add("Content-Type", "multipart/form-data");

                    var form = new MultipartFormDataContent();
                    form.Add(byteArrayContent, "image", "pic.jpg");

                    try
                    {
                        response = httpClient.PostAsync("/predict", form).Result;
                    }
                    catch (Exception e)
                    {
                        response = new HttpResponseMessage();
                        response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                    }
                }
                if (response.IsSuccessStatusCode)
                {
                    var responseString = response.Content.ReadAsStringAsync().Result;
                    var responseJson = JObject.Parse(responseString).ToObject<Dictionary<string, double>>();

                    var highestConfidence = -1.0;
                    foreach (string category in responseJson.Keys)
                    {
                        var value = responseJson[category];
                        if (value > highestConfidence)
                        {
                            highestConfidence = value;
                            bestCategory = category;
                        }
                    }
                }

                if (bestCategory.Equals("cardboard") || bestCategory.Equals("paper"))
                {
                    return true;
                }
            }

            return compostableObjects.Any();
        }

        private static bool IsRecyclable(MainViewModel vm)
        {
            HttpResponseMessage response;
            var bestCategory = "trash";
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri("http://bananapi.westus.cloudapp.azure.com:5000");

                using (MemoryStream memStream = new MemoryStream())
                using (SKManagedWStream wstream = new SKManagedWStream(memStream))
                {
                    var result = vm.Image.PeekPixels().Encode(wstream, SKEncodedImageFormat.Jpeg, 100);
                }

                var byteArrayContent = new ByteArrayContent(vm.Image.Bytes, 0, vm.Image.Bytes.Length);
                byteArrayContent.Headers.Add("Content-Type", "multipart/form-data");

                var form = new MultipartFormDataContent();
                form.Add(byteArrayContent, "image", "pic.jpg");

                try
                {
                    response = httpClient.PostAsync("/predict", form).Result;
                }
                catch (Exception e)
                {
                    response = new HttpResponseMessage();
                    response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                }
            }
            if (response.IsSuccessStatusCode)
            {
                var responseString = response.Content.ReadAsStringAsync().Result;
                var responseJson = JObject.Parse(responseString).ToObject<Dictionary<string, double>>();

                var highestConfidence = -1.0;
                foreach (string category in responseJson.Keys)
                {
                    var value = responseJson[category];
                    if (value > highestConfidence)
                    {
                        highestConfidence = value;
                        bestCategory = category;
                    }
                }
            }

            if (bestCategory.Equals("glass") || bestCategory.Equals("metal") || bestCategory.Equals("plastic"))
            {
                return true;
            }
            return false;
        }
    }
}
