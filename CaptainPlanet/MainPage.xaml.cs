using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Plugin.Media;
using Plugin.Media.Abstractions;
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
                DrawPredictions(vm, canvas, left, top, scale);
            }
        }

        static void DrawPredictions(MainViewModel vm, SKCanvas canvas, float left, float top, float scaleFactor)
        {
            if (vm.Predictions == null) return;

            if (!vm.Predictions.Any())
            {
                LabelPrediction(canvas, "Nothing detected", new BoundingRect(0, 0, 1, 1), left, top, scaleFactor, false);
            }
            else if (vm.Predictions.All(p => p.Rectangle != null))
            {
                foreach (var prediction in vm.Predictions)
                {
                    LabelPrediction(canvas, prediction.ObjectProperty, prediction.Rectangle, left, top, scaleFactor);
                }
            }
            else
            {
                var best = vm.Predictions.OrderByDescending(p => p.Confidence).First();
                LabelPrediction(canvas, best.ObjectProperty, new BoundingRect(0, 0, 1, 1), left, top, scaleFactor, false);
            }
        }

        static void ClearCanvas(SKImageInfo info, SKCanvas canvas)
        {
            var paint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = SKColors.White
            };

            canvas.DrawRect(info.Rect, paint);
        }

        static void LabelPrediction(SKCanvas canvas, string tag, BoundingRect box, float left, float top, float scaleFactor, bool addBox = true)
        {
            var scaledBoxLeft = left + (scaleFactor * box.X);
            var scaledBoxWidth = scaleFactor * box.W;
            var scaledBoxTop = top + (scaleFactor * box.Y);
            var scaledBoxHeight = scaleFactor * box.H;

            if (addBox)
                DrawBox(canvas, scaledBoxLeft, scaledBoxTop, scaledBoxWidth, scaledBoxHeight);

            DrawText(canvas, tag, scaledBoxLeft, scaledBoxTop, scaledBoxWidth, scaledBoxHeight);
        }

        static void DrawText(SKCanvas canvas, string tag, float startLeft, float startTop, float scaledBoxWidth, float scaledBoxHeight)
        {
            var textPaint = new SKPaint
            {
                IsAntialias = true,
                Color = SKColors.White,
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
                Style = SKPaintStyle.Fill,
                Color = SKColors.Green
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

        static void DrawBox(SKCanvas canvas, float startLeft, float startTop, float scaledBoxWidth, float scaledBoxHeight)
        {
            var strokePaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                Color = SKColors.White,
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

        private bool IsCompostable(MainViewModel vm)
        {
            var compostableCategories = vm.Categories.Where(c =>
                c.Name.StartsWith("food_", StringComparison.InvariantCulture) ||
                c.Name.StartsWith("plant_", StringComparison.InvariantCulture) ||
                c.Name.Equals("outdoor_grass"));

            // TODO
            //if (!compostableCategories.Any())
            //{
            //    // Call ML REST API
            //}

            return compostableCategories.Any();
        }

        private bool IsRecyclable(MainViewModel vm)
        {
            var recyclableCategories = vm.Categories.Where(c => c.Name.Equals("drink_can"));

            // TODO
            //if (!recyclableCategories.Any())
            //{
            //    // Call ML REST API
            //}

            return recyclableCategories.Any();
        }
    }
}
