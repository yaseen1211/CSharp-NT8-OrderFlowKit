#region Using declarations
using NinjaTrader.Cbi;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Data;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Gui;
using NinjaTrader.NinjaScript.AddOns.SightEngine;
using NinjaTrader.NinjaScript.AddOns.WyckoffRenderUtils;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows;
using System.Xml.Serialization;
using System;
#endregion

// =====================================================================================
// WyckoffZen Order Flow Kit - single file build
// Order Flow + Volume Analysis Profile + Cumulative Delta + motores SE/WyckoffRender
// Los 3 indicadores usan el namespace estandar NinjaTrader.NinjaScript.Indicators y
// sus enums son globales: el generador de codigo de NT no cualifica los tipos.
// =====================================================================================

// --- SOURCE: AddOns/WyckoffRender.cs ---
namespace NinjaTrader.NinjaScript.AddOns
{
	namespace WyckoffRenderUtils
	{
		public static class Debug{ public static void toFile(string info){ File.AppendAllText(NinjaTrader.Core.Globals.UserDataDir + "Debug.txt", info + Environment.NewLine); } }
		
		public class WyckoffRenderControl
		{
			protected ChartControl CHART_CONTROL;
			protected ChartScale CHART_SCALE;
			protected ChartBars CHART_BARS;
			protected SharpDX.Direct2D1.RenderTarget RENDER_TARGET;
			
			protected float W;
			protected float H;
			protected float PanelH;
			protected float PanelW;
			
			public void setRenderTarget(ChartControl chartControl, ChartScale chartScale, ChartBars chartBars, SharpDX.Direct2D1.RenderTarget renderTarget)
			{
				this.CHART_CONTROL = chartControl;
				this.CHART_SCALE = chartScale;
				this.CHART_BARS = chartBars;
				this.RENDER_TARGET = renderTarget;
			}
			public void setHW(float H, float W)
			{
				this.H = H;
				this.W = W;
			}
			public void setChartPanelHW(float PanelH, float PanelW)
			{
				this.PanelH = PanelH;
				this.PanelW = PanelW;
			}
			protected void setFontStyle(SimpleFont font, out SharpDX.DirectWrite.TextFormat textFormat)
			{
				SharpDX.DirectWrite.FontWeight fw;
				SharpDX.DirectWrite.FontStyle fs;
				
				if( font.Bold == true ) fw = SharpDX.DirectWrite.FontWeight.Bold;
				else fw = SharpDX.DirectWrite.FontWeight.Normal;
				
				if( font.Italic == true ) fs = SharpDX.DirectWrite.FontStyle.Italic;
				else fs = SharpDX.DirectWrite.FontStyle.Normal;
				
				textFormat = new SharpDX.DirectWrite.TextFormat(
					NinjaTrader.Core.Globals.DirectWriteFactory,
					font.ToString(),
					fw,
					fs,
					SharpDX.DirectWrite.FontStretch.Normal,
					(float)font.Size
				);
				textFormat.ParagraphAlignment = SharpDX.DirectWrite.ParagraphAlignment.Center; 
				textFormat.TextAlignment = SharpDX.DirectWrite.TextAlignment.Center;
			}
			protected SharpDX.Direct2D1.DashStyle DashStyleHelperToDX(DashStyleHelper dashStyle)
			{
				switch( dashStyle )
				{
					case DashStyleHelper.Dash:
					{
						return SharpDX.Direct2D1.DashStyle.Dash;
					}
					case DashStyleHelper.DashDot:
					{
						return SharpDX.Direct2D1.DashStyle.DashDot;
					}
					case DashStyleHelper.DashDotDot:
					{
						return SharpDX.Direct2D1.DashStyle.DashDotDot;
					}
					case DashStyleHelper.Dot:
					{
						return SharpDX.Direct2D1.DashStyle.Dot;
					}
				}
				return SharpDX.Direct2D1.DashStyle.Solid;
			}
			
			protected void myDrawText(string text, ref SharpDX.RectangleF rect, SharpDX.Color color, float fontWidth, float fontHeight, SharpDX.DirectWrite.TextFormat textFormat, float brushOpacity)
			{
				if( this.W >= fontWidth && this.H >= fontHeight ){
					SharpDX.Direct2D1.Brush dxBrush = new SharpDX.Direct2D1.SolidColorBrush(RENDER_TARGET, color);
					if( !dxBrush.IsDisposed ){
						dxBrush.Opacity = brushOpacity;
						RENDER_TARGET.DrawText(text, textFormat, rect, dxBrush);
						dxBrush.Dispose();
					}
				}
			}
			protected void myFillRectangle(ref SharpDX.RectangleF rect, SharpDX.Color color, float opacity)
			{
				SharpDX.Direct2D1.Brush dxBrush = new SharpDX.Direct2D1.SolidColorBrush(RENDER_TARGET, color);
				if( !dxBrush.IsDisposed ){
					dxBrush.Opacity = opacity;
					RENDER_TARGET.FillRectangle(rect, dxBrush);	
					dxBrush.Dispose();
				}
			}
			protected void myDrawRectangle(ref SharpDX.RectangleF rect, SharpDX.Color color, float opacity, float strokeWidth)
			{
				SharpDX.Direct2D1.Brush dxBrush = new SharpDX.Direct2D1.SolidColorBrush(RENDER_TARGET, color);
				if( !dxBrush.IsDisposed ){
					dxBrush.Opacity = opacity;
					RENDER_TARGET.DrawRectangle(rect, dxBrush, strokeWidth);
					dxBrush.Dispose();
				}
			}
			protected void myDrawEllipse(ref SharpDX.Direct2D1.Ellipse ellipse, SharpDX.Color color, float opacity, float strokeWidth)
			{
				SharpDX.Direct2D1.Brush dxBrush = new SharpDX.Direct2D1.SolidColorBrush(RENDER_TARGET, color);
				if( !dxBrush.IsDisposed ){
					dxBrush.Opacity = opacity;
					RENDER_TARGET.DrawEllipse(ellipse, dxBrush, strokeWidth);
					dxBrush.Dispose();
				}
			}
			protected void myFillEllipse(ref SharpDX.Direct2D1.Ellipse ellipse, SharpDX.Color color, float opacity)
			{
				SharpDX.Direct2D1.Brush dxBrush = new SharpDX.Direct2D1.SolidColorBrush(RENDER_TARGET, color);
				if( !dxBrush.IsDisposed ){
					dxBrush.Opacity = opacity;
					RENDER_TARGET.FillEllipse(ellipse, dxBrush);
					dxBrush.Dispose();
				}
			}
			protected void myDrawLine(ref SharpDX.Vector2 startVec, ref SharpDX.Vector2 endVec,
				SharpDX.Color color, float opacity,
				float strokeWidth, SharpDX.Direct2D1.StrokeStyle strokeStyle)
			{
				SharpDX.Direct2D1.Brush dxBrush = new SharpDX.Direct2D1.SolidColorBrush(RENDER_TARGET, color);
				if( !dxBrush.IsDisposed ){
					dxBrush.Opacity = opacity;
					RENDER_TARGET.DrawLine(startVec, endVec, dxBrush, strokeWidth, strokeStyle);
					dxBrush.Dispose();
				}
			}
			protected void myDrawLine(ref SharpDX.Vector2 startVec, ref SharpDX.Vector2 endVec, SharpDX.Color color)
			{
				this.myDrawLine(ref startVec, ref endVec, color, 1.0f, 1.0f, null);
			}
			
			public static SharpDX.Color BrushToColor(Brush brushToConvert)
			{
				string brushColor= brushToConvert.ToString();
				// # A
				if( brushColor.Equals(Brushes.AliceBlue.ToString()) ) return SharpDX.Color.AliceBlue;
				if( brushColor.Equals(Brushes.AntiqueWhite.ToString()) ) return SharpDX.Color.AntiqueWhite;
				if( brushColor.Equals(Brushes.Aqua.ToString()) ) return SharpDX.Color.Aqua;
				if( brushColor.Equals(Brushes.Aquamarine.ToString()) ) return SharpDX.Color.Aquamarine;
				if( brushColor.Equals(Brushes.Azure.ToString()) ) return SharpDX.Color.Azure;
				// # B
				if( brushColor.Equals(Brushes.Beige.ToString()) ) return SharpDX.Color.Beige;
				if( brushColor.Equals(Brushes.Bisque.ToString()) ) return SharpDX.Color.Bisque;
				if( brushColor.Equals(Brushes.Black.ToString()) ) return SharpDX.Color.Black;
				if( brushColor.Equals(Brushes.BlanchedAlmond.ToString()) ) return SharpDX.Color.BlanchedAlmond;
				if( brushColor.Equals(Brushes.Blue.ToString()) ) return SharpDX.Color.Blue;
				if( brushColor.Equals(Brushes.BlueViolet.ToString()) ) return SharpDX.Color.BlueViolet;
				if( brushColor.Equals(Brushes.Brown.ToString()) ) return SharpDX.Color.Brown;
				if( brushColor.Equals(Brushes.BurlyWood.ToString()) ) return SharpDX.Color.BurlyWood;
				// # C
				if( brushColor.Equals(Brushes.CadetBlue.ToString()) ) return SharpDX.Color.CadetBlue;
				if( brushColor.Equals(Brushes.Chartreuse.ToString()) ) return SharpDX.Color.Chartreuse;
				if( brushColor.Equals(Brushes.Chocolate.ToString()) ) return SharpDX.Color.Chocolate;
				if( brushColor.Equals(Brushes.Coral.ToString()) ) return SharpDX.Color.Coral;
				if( brushColor.Equals(Brushes.CornflowerBlue.ToString()) ) return SharpDX.Color.CornflowerBlue;
				if( brushColor.Equals(Brushes.Cornsilk.ToString()) ) return SharpDX.Color.Cornsilk;
				if( brushColor.Equals(Brushes.Crimson.ToString()) ) return SharpDX.Color.Crimson;
				if( brushColor.Equals(Brushes.Cyan.ToString()) ) return SharpDX.Color.Cyan;
				// # D
				if( brushColor.Equals(Brushes.DarkBlue.ToString()) ) return SharpDX.Color.DarkBlue;
				if( brushColor.Equals(Brushes.DarkCyan.ToString()) ) return SharpDX.Color.DarkCyan;
				if( brushColor.Equals(Brushes.DarkGoldenrod.ToString()) ) return SharpDX.Color.DarkGoldenrod;
				if( brushColor.Equals(Brushes.DarkGray.ToString()) ) return SharpDX.Color.DarkGray;
				if( brushColor.Equals(Brushes.DarkGreen.ToString()) ) return SharpDX.Color.DarkGreen;
				if( brushColor.Equals(Brushes.DarkKhaki.ToString()) ) return SharpDX.Color.DarkKhaki;
				if( brushColor.Equals(Brushes.DarkMagenta.ToString()) ) return SharpDX.Color.DarkMagenta;
				if( brushColor.Equals(Brushes.DarkOliveGreen.ToString()) ) return SharpDX.Color.DarkOliveGreen;
				if( brushColor.Equals(Brushes.DarkOrange.ToString()) ) return SharpDX.Color.DarkOrange;
				if( brushColor.Equals(Brushes.DarkOrchid.ToString()) ) return SharpDX.Color.DarkOrchid;
				if( brushColor.Equals(Brushes.DarkRed.ToString()) ) return SharpDX.Color.DarkRed;
				if( brushColor.Equals(Brushes.DarkSalmon.ToString()) ) return SharpDX.Color.DarkSalmon;
				if( brushColor.Equals(Brushes.DarkSeaGreen.ToString()) ) return SharpDX.Color.DarkSeaGreen;
				if( brushColor.Equals(Brushes.DarkSlateBlue.ToString()) ) return SharpDX.Color.DarkSlateBlue;
				if( brushColor.Equals(Brushes.DarkSlateGray.ToString()) ) return SharpDX.Color.DarkSlateGray;
				if( brushColor.Equals(Brushes.DarkTurquoise.ToString()) ) return SharpDX.Color.DarkTurquoise;
				if( brushColor.Equals(Brushes.DarkViolet.ToString()) ) return SharpDX.Color.DarkViolet;
				if( brushColor.Equals(Brushes.DeepPink.ToString()) ) return SharpDX.Color.DeepPink;
				if( brushColor.Equals(Brushes.DeepSkyBlue.ToString()) ) return SharpDX.Color.DeepSkyBlue;
				if( brushColor.Equals(Brushes.DimGray.ToString()) ) return SharpDX.Color.DimGray;
				if( brushColor.Equals(Brushes.DodgerBlue.ToString()) ) return SharpDX.Color.DodgerBlue;
				// # E
				if( brushColor.Equals(Brushes.Firebrick.ToString()) ) return SharpDX.Color.Firebrick;
				if( brushColor.Equals(Brushes.FloralWhite.ToString()) ) return SharpDX.Color.FloralWhite;
				if( brushColor.Equals(Brushes.ForestGreen.ToString()) ) return SharpDX.Color.ForestGreen;
				if( brushColor.Equals(Brushes.Fuchsia.ToString()) ) return SharpDX.Color.Fuchsia;
				// # G
				if( brushColor.Equals(Brushes.Gainsboro.ToString()) ) return SharpDX.Color.Gainsboro;
				if( brushColor.Equals(Brushes.GhostWhite.ToString()) ) return SharpDX.Color.GhostWhite;
				if( brushColor.Equals(Brushes.Gold.ToString()) ) return SharpDX.Color.Gold;
				if( brushColor.Equals(Brushes.Goldenrod.ToString()) ) return SharpDX.Color.Goldenrod;
				if( brushColor.Equals(Brushes.Gray.ToString()) ) return SharpDX.Color.Gray;
				if( brushColor.Equals(Brushes.Green.ToString()) ) return SharpDX.Color.Green;
				if( brushColor.Equals(Brushes.GreenYellow.ToString()) ) return SharpDX.Color.GreenYellow;
				// # E
				if( brushColor.Equals(Brushes.Honeydew.ToString()) ) return SharpDX.Color.Honeydew;
				if( brushColor.Equals(Brushes.HotPink.ToString()) ) return SharpDX.Color.HotPink;
				// # I
				if( brushColor.Equals(Brushes.IndianRed.ToString()) ) return SharpDX.Color.IndianRed;
				if( brushColor.Equals(Brushes.Indigo.ToString()) ) return SharpDX.Color.Indigo;
				if( brushColor.Equals(Brushes.Ivory.ToString()) ) return SharpDX.Color.Ivory;
				// # K
				if( brushColor.Equals(Brushes.Khaki.ToString()) ) return SharpDX.Color.Khaki;
				// # L
				if( brushColor.Equals(Brushes.Lavender.ToString()) ) return SharpDX.Color.Lavender;
				if( brushColor.Equals(Brushes.LavenderBlush.ToString()) ) return SharpDX.Color.LavenderBlush;
				if( brushColor.Equals(Brushes.LawnGreen.ToString()) ) return SharpDX.Color.LawnGreen;
				if( brushColor.Equals(Brushes.LemonChiffon.ToString()) ) return SharpDX.Color.LemonChiffon;
				if( brushColor.Equals(Brushes.LightBlue.ToString()) ) return SharpDX.Color.LightBlue;
				if( brushColor.Equals(Brushes.LightCoral.ToString()) ) return SharpDX.Color.LightCoral;
				if( brushColor.Equals(Brushes.LightCyan.ToString()) ) return SharpDX.Color.LightCyan;
				if( brushColor.Equals(Brushes.LightGoldenrodYellow.ToString()) ) return SharpDX.Color.LightGoldenrodYellow;
				if( brushColor.Equals(Brushes.LightGray.ToString()) ) return SharpDX.Color.LightGray;
				if( brushColor.Equals(Brushes.LightGreen.ToString()) ) return SharpDX.Color.LightGreen;
				if( brushColor.Equals(Brushes.LightPink.ToString()) ) return SharpDX.Color.LightPink;
				if( brushColor.Equals(Brushes.LightSalmon.ToString()) ) return SharpDX.Color.LightSalmon;
				if( brushColor.Equals(Brushes.LightSeaGreen.ToString()) ) return SharpDX.Color.LightSeaGreen;
				if( brushColor.Equals(Brushes.LightSkyBlue.ToString()) ) return SharpDX.Color.LightSkyBlue;
				if( brushColor.Equals(Brushes.LightSlateGray.ToString()) ) return SharpDX.Color.LightSlateGray;
				if( brushColor.Equals(Brushes.LightSteelBlue.ToString()) ) return SharpDX.Color.LightSteelBlue;
				if( brushColor.Equals(Brushes.LightYellow.ToString()) ) return SharpDX.Color.LightYellow;
				if( brushColor.Equals(Brushes.Lime.ToString()) ) return SharpDX.Color.Lime;
				if( brushColor.Equals(Brushes.LimeGreen.ToString()) ) return SharpDX.Color.LimeGreen;
				if( brushColor.Equals(Brushes.Linen.ToString()) ) return SharpDX.Color.Linen;
				// # M
				if( brushColor.Equals(Brushes.Magenta.ToString()) ) return SharpDX.Color.Magenta;
				if( brushColor.Equals(Brushes.Maroon.ToString()) ) return SharpDX.Color.Maroon;
				if( brushColor.Equals(Brushes.MediumAquamarine.ToString()) ) return SharpDX.Color.MediumAquamarine;
				if( brushColor.Equals(Brushes.MediumBlue.ToString()) ) return SharpDX.Color.MediumBlue;
				if( brushColor.Equals(Brushes.MediumOrchid.ToString()) ) return SharpDX.Color.MediumOrchid;
				if( brushColor.Equals(Brushes.MediumPurple.ToString()) ) return SharpDX.Color.MediumPurple;
				if( brushColor.Equals(Brushes.MediumSeaGreen.ToString()) ) return SharpDX.Color.MediumSeaGreen;
				if( brushColor.Equals(Brushes.MediumSlateBlue.ToString()) ) return SharpDX.Color.MediumSlateBlue;
				if( brushColor.Equals(Brushes.MediumSpringGreen.ToString()) ) return SharpDX.Color.MediumSpringGreen;
				if( brushColor.Equals(Brushes.MediumTurquoise.ToString()) ) return SharpDX.Color.MediumTurquoise;
				if( brushColor.Equals(Brushes.MediumVioletRed.ToString()) ) return SharpDX.Color.MediumVioletRed;
				if( brushColor.Equals(Brushes.MidnightBlue.ToString()) ) return SharpDX.Color.MidnightBlue;
				if( brushColor.Equals(Brushes.MintCream.ToString()) ) return SharpDX.Color.MintCream;
				if( brushColor.Equals(Brushes.MistyRose.ToString()) ) return SharpDX.Color.MintCream;
				if( brushColor.Equals(Brushes.Moccasin.ToString()) ) return SharpDX.Color.MintCream;
				// # N
				if( brushColor.Equals(Brushes.NavajoWhite.ToString()) ) return SharpDX.Color.NavajoWhite;
				if( brushColor.Equals(Brushes.Navy.ToString()) ) return SharpDX.Color.Navy;
				// # O
				if( brushColor.Equals(Brushes.OldLace.ToString()) ) return SharpDX.Color.OldLace;
				if( brushColor.Equals(Brushes.Olive.ToString()) ) return SharpDX.Color.Olive;
				if( brushColor.Equals(Brushes.OliveDrab.ToString()) ) return SharpDX.Color.OliveDrab;
				if( brushColor.Equals(Brushes.Orange.ToString()) ) return SharpDX.Color.Orange;
				if( brushColor.Equals(Brushes.OrangeRed.ToString()) ) return SharpDX.Color.OrangeRed;
				if( brushColor.Equals(Brushes.Orchid.ToString()) ) return SharpDX.Color.Orchid;
				// # P
				if( brushColor.Equals(Brushes.PaleGoldenrod.ToString()) ) return SharpDX.Color.PaleGoldenrod;
				if( brushColor.Equals(Brushes.PaleGreen.ToString()) ) return SharpDX.Color.PaleGreen;
				if( brushColor.Equals(Brushes.PaleTurquoise.ToString()) ) return SharpDX.Color.PaleTurquoise;
				if( brushColor.Equals(Brushes.PaleVioletRed.ToString()) ) return SharpDX.Color.PaleVioletRed;
				if( brushColor.Equals(Brushes.PapayaWhip.ToString()) ) return SharpDX.Color.PapayaWhip;
				if( brushColor.Equals(Brushes.PeachPuff.ToString()) ) return SharpDX.Color.PeachPuff;
				if( brushColor.Equals(Brushes.Peru.ToString()) ) return SharpDX.Color.Peru;
				if( brushColor.Equals(Brushes.Pink.ToString()) ) return SharpDX.Color.Pink;
				if( brushColor.Equals(Brushes.Plum.ToString()) ) return SharpDX.Color.Plum;
				if( brushColor.Equals(Brushes.PowderBlue.ToString()) ) return SharpDX.Color.PowderBlue;
				if( brushColor.Equals(Brushes.Purple.ToString()) ) return SharpDX.Color.Purple;
				// # R
				if( brushColor.Equals(Brushes.Red.ToString()) ) return SharpDX.Color.Red;
				if( brushColor.Equals(Brushes.RosyBrown.ToString()) ) return SharpDX.Color.RosyBrown;
				if( brushColor.Equals(Brushes.RoyalBlue.ToString()) ) return SharpDX.Color.RoyalBlue;
				// # S
				if( brushColor.Equals(Brushes.SaddleBrown.ToString()) ) return SharpDX.Color.SaddleBrown;
				if( brushColor.Equals(Brushes.Salmon.ToString()) ) return SharpDX.Color.Salmon;
				if( brushColor.Equals(Brushes.SandyBrown.ToString()) ) return SharpDX.Color.SandyBrown;
				if( brushColor.Equals(Brushes.SeaGreen.ToString()) ) return SharpDX.Color.SeaGreen;
				if( brushColor.Equals(Brushes.SeaShell.ToString()) ) return SharpDX.Color.SeaShell;
				if( brushColor.Equals(Brushes.Sienna.ToString()) ) return SharpDX.Color.Sienna;
				if( brushColor.Equals(Brushes.Silver.ToString()) ) return SharpDX.Color.Silver;
				if( brushColor.Equals(Brushes.SkyBlue.ToString()) ) return SharpDX.Color.SkyBlue;
				if( brushColor.Equals(Brushes.SlateBlue.ToString()) ) return SharpDX.Color.SlateBlue;
				if( brushColor.Equals(Brushes.SlateGray.ToString()) ) return SharpDX.Color.SlateGray;
				if( brushColor.Equals(Brushes.Snow.ToString()) ) return SharpDX.Color.Snow;
				if( brushColor.Equals(Brushes.SpringGreen.ToString()) ) return SharpDX.Color.SpringGreen;
				if( brushColor.Equals(Brushes.SteelBlue.ToString()) ) return SharpDX.Color.SteelBlue;
				// # T
				if( brushColor.Equals(Brushes.Tan.ToString()) ) return SharpDX.Color.Tan;
				if( brushColor.Equals(Brushes.Teal.ToString()) ) return SharpDX.Color.Teal;
				if( brushColor.Equals(Brushes.Thistle.ToString()) ) return SharpDX.Color.Thistle;
				if( brushColor.Equals(Brushes.Tomato.ToString()) ) return SharpDX.Color.Tomato;
				//if( brushToConvert == Brushes.Transparent ) return SharpDX.Color.Transparent;
				if( brushColor.Equals(Brushes.Turquoise.ToString()) ) return SharpDX.Color.Turquoise;
				// # V
				if( brushColor.Equals(Brushes.Violet.ToString()) ) return SharpDX.Color.Violet;
				// # W
				if( brushColor.Equals(Brushes.Wheat.ToString()) ) return SharpDX.Color.Wheat;
				if( brushColor.Equals(Brushes.White.ToString()) ) return SharpDX.Color.White;
				if( brushColor.Equals(Brushes.WhiteSmoke.ToString()) ) return SharpDX.Color.WhiteSmoke;
				// # Y
				if( brushColor.Equals(Brushes.Yellow.ToString()) ) return SharpDX.Color.Yellow;
				if( brushColor.Equals(Brushes.YellowGreen.ToString()) ) return SharpDX.Color.YellowGreen;
				
				return SharpDX.Color.Transparent;
			}
			
			
		}
	}
}

// --- SOURCE: AddOns/SE.cs ---
//using NinjaTrader.NinjaScript.AddOns.ZMath;

namespace NinjaTrader.NinjaScript.AddOns
{
	#region SIGHT_ENGINE
	
	namespace SightEngine
	{
		//public static class Debug{ public static void toFile(string info){ File.AppendAllText(NinjaTrader.Core.Globals.UserDataDir + "Debug.txt", info + Environment.NewLine); } }
		
		public enum MarketType
		{
			BEARISH, BULLISH
		}
		public enum BarType
		{
			BUY, SELL, INCERTITUDE, UNKNOWN
		}
		public enum BarClass
		{
			ENGULFING, SWALLOWED, BULLISH, BEARISH, UNKNOWN
		}
		public static class TimePeriod
		{
			public delegate bool TIME(DateTime t1, DateTime t2, int p);
			public static bool MINUTES(DateTime t1, DateTime t2, int Period)
			{
				return Math.Abs((t2 - t1).TotalMinutes) >= Period;
			}
			public static bool HOURS(DateTime t1, DateTime t2, int Period)
			{
				return Math.Abs((t2 - t1).TotalHours) >= Period;
			}
			public static bool DAYS(DateTime t1, DateTime t2, int Period)
			{
				return Math.Abs((t2 - t1).TotalDays) >= Period;
			}
		}
		
		#region MATH2
		
		public static class Math2
		{
			public static bool isInRange<T>(T Value, T Begin, T End) where T : IComparable
			{
				return (Value.CompareTo(Begin) > 0 && Value.CompareTo(End) < 0);
			}
			public static bool isBounded<T>(T Value, T Begin, T End) where T : IComparable
			{
				return (Value.CompareTo(Begin) >= 0 && Value.CompareTo(End) <= 0);
			}
//			public static bool isGreater<T>(T Value1, T Value2) where T : IComparable
//			{
//				return Value2.CompareTo(Value1) > 0;
//			}
			public static bool isLess<T>(T Value1, T Value2) where T : IComparable
			{
				return Value2.CompareTo(Value1) < 0;
			}
			public static double neverLessThanZero(double Value)
			{
				if(Value < 0)
					Value = 0;
				return Value;
			}
			public static int getMaximunValue(int Value, int maximunValue)
			{
				if(Value > maximunValue)
					Value = maximunValue;
				return Value;
			}
			public static double calculateDistance(double Begin, double End)
			{
				return Math.Abs(Begin - End);
			}
			public static double Percent(double Total, double Cuantity, int roundDigits)
			{ 
				return Math.Round((Cuantity*100)/Total, roundDigits);
			}
			public static double Percent(double Total, double Cuantity)
			{ 
				return Percent(Total, Cuantity, 2);
			}
			public static bool isPercent(double Percent)
			{
				return isBounded(Percent, 0, 100);
			}
			public static int boolToInt(bool v)
			{
				return v ? 1 : 0;
			}
			public static double Atan2inDeg(double y, double x)
			{
				return Math.Round(Math.Atan2(y, x) * (180/3.14), 2);
			}
			public class BoundedValue<T>
			{
				public BoundedValue(){}
				public BoundedValue(T Min, T Max)
				{
					this.Min = Min;
					this.Max = Max;
				}
				
				public T Min, Max;
			}
//			public class Decimal
//			{
//				public decimal Value;
//				public decimal Error;
//			}
		}
		
		#endregion
		#region STR_UTILS
		
		public static class StrUtils
		{
			public static int extractInfo(string info, out string extracted_info, char first_key, char end_key, int start_position)
			{
				extracted_info= string.Empty;
				int str_len = info.Length;
				if( start_position >= str_len )
					return -1;
				for(;start_position < str_len; start_position++){
					if( info[start_position] == first_key ){
						if( start_position > 0)
							start_position++;
						for(;start_position < str_len; start_position++)
						{
							if( info[start_position] == end_key ){
								return start_position;
							}
							extracted_info+= info[start_position];
						}
					}
				}
				
				return -1;
			}
			public static int extractInfo(string info, out string extracted_info, char first_key, char end_key)
			{
				return extractInfo(info, out extracted_info, first_key, end_key, 0);
			}
			public static int extractInfo(string info, out string extracted_info, char end_key, int start_position)
			{
				return extractInfo(info, out extracted_info, info[start_position], end_key, start_position);
			}
			public static int extractInfo(string info, out string extracted_info, char end_key)
			{
				return extractInfo(info, out extracted_info, info[0], end_key, 0);
			}
		}
		
		#endregion
		#region MAIN
		
		#region CORE_CLASS
		
		public interface ICore{}
		// !- Me permite hace mis propias implementaciones
		public class SECore<K, C>//: Dictionary<int, C>, IEnumerable<KeyValuePair<int, C>>
		{
			private SortedDictionary<K, C> This;
			
			public SECore()
			{
				this.This = new SortedDictionary<K, C>();
			}
			public SECore(Dictionary<K, C> This)
			{
				this.This = new SortedDictionary<K, C>(This);
			}
			
			#region GENERIC_IMPLEMENTATIONS
			
			public IEnumerator<KeyValuePair<K, C>> GetEnumerator()
			{
				return This.GetEnumerator();
			}
			public KeyValuePair<K, C> First
			{
				get{ return This.First(); }
			}
			public KeyValuePair<K, C> Last
			{
				get{ return This.Last(); }
			}
			public bool ContainsKey(K key)
			{
				return This.ContainsKey(key);
			}
			public SortedDictionary<K, C>.KeyCollection Keys
			{
				get{ return This.Keys; }
			}
			public IEnumerable<KeyValuePair<K, C>> Skip(int skipFrom)
			{
				return This.Skip(skipFrom);
			}
      		public IEnumerable<KeyValuePair<K, C>> Take(int takeTo)
			{
				return This.Take(takeTo);
			}
			public bool Remove(K key)
			{
				return This.Remove(key);
			}
			public int Count
			{
				get{ return This.Count; }
			}
			public void Clear()
			{
				This.Clear();
			}
			
			#endregion
			#region MY_IMPLEMENTATIONS
			
			public C this[K index]
			{
				get{ return This[index]; }
		        set{ This[index] = value; }
			}
			// *- si el acceso al valor no existe no genera una excepcion, si no un NULL
			public C At(K index)
			{
				if( !This.ContainsKey(index) )
					return default(C);
				return This[index];
			}
			// *- no generamos una excepcion si ya existe el item
			public bool Add(K index, C obj)
			{
				if( This.ContainsKey(index) )
					return false;
				
				This.Add(index, obj);
				return true;
			}
			//protected KeyValuePair<int, C> ElementAtOrDefault(int index){ return This.ElementAtOrDefault(index); }
			public bool isNull
			{
				get { return This == null || This.Count == 0; }
			}
			
			#endregion
		}
		public class SEBars<T> : SECore<int , T>{}
		
		#endregion
		#region VOLUME_ANALYSIS
		public static class VolumeAnalysis{
			public enum PeriodMode
			{
//				Bars,
				Minutes,
				Hours,
				Days
			}
			public enum VolumeType
			{
				BidAsk,
				Total,
				Delta
			}
			
			public class MarketOrder
			{
				public MarketOrder()
				{
					this.Clear();
				}
				private long bid;
				private long ask;
				private long total;
				private long delta;
				//private int secs;
				
				public void Clear()
				{
					bid = ask = total = delta = 0;
				}
				public void CalculateSigmaVolume(WyckoffBars.Bar wyckoffBar)
				{
					ask+= wyckoffBar.Ask;
					bid+= wyckoffBar.Bid;
					total+= wyckoffBar.Total;
					delta+= wyckoffBar.Delta;
				}
				public void CalculateSigmaVolume(MarketDataEventArgs MarketArgs)
				{
					long v = MarketArgs.Volume;
					double price = MarketArgs.Price;
					
					total+= v;
					if(price >= MarketArgs.Ask){
						ask+= v;
						delta+= v;
					}
			    	else if(price <= MarketArgs.Bid){
						bid+= v;
						delta-= v;
					}
					//contracts++;
					//secs += MarketArgs.Time.Second;
				}
				public void CalculateSigmaVolume(MarketOrder volume)
				{
					ask+= volume.Ask;
					bid+= volume.Bid;
					delta+= volume.Delta;
					total+= volume.Total;
				}
				public void CalculateSigmaVolume(long Bid, long Ask, long Delta, long Total)
				{
					bid+= Bid;
					ask+= Ask;
					delta+= Delta;
					total+= Total;
				}
				
				public long Total
				{
					get{ return this.total; }
				}
				public long Delta
				{
					get{ return this.delta; }
				}
				public long Bid
				{
					get{ return this.bid; }
				}
				public long Ask
				{
					get{ return this.ask; }
				}
				//public long Contracts
				//{
					//get{ return this.contracts; }
				//}
			}
			public class PriceLadder
			{
				private ConcurrentDictionary<double, MarketOrder> ladder;
				private double minPrice;
				private double maxPrice;
				private double lowPrice;
				private double highPrice;
				
				public PriceLadder()
				{
					this.ladder = new ConcurrentDictionary<double, MarketOrder>();
					this.minPrice = this.lowPrice = double.MaxValue;
					this.maxPrice = this.highPrice = 0;
				}
				
				public double LowPrice
				{
					get{ return this.lowPrice; }
				}
				public double HighPrice
				{
					get{ return this.highPrice; }
				}
				public void CalculateMinAndMax(ref MarketOrder minVolume, ref MarketOrder maxVolume,
					out double MinPrice, out double MaxPrice)
				{
					minVolume.Clear();
					maxVolume.Clear();
					
					MarketOrder mo;
					MinPrice = -1;
					MaxPrice = -1;
					long m_ask = long.MaxValue;
					long m_bid = long.MaxValue;
					long m_delta = long.MaxValue;
					long m_total = long.MaxValue;
					long M_ask = 0;
					long M_bid = 0;
					long M_delta = 0;
					long M_total = 0;
					long ask;
					long bid;
					long delta;
					long total;
					double price;
					
					foreach(var pl in ladder)
					{
						mo		= pl.Value;
						ask		= mo.Ask;
						bid		= mo.Bid;
						delta	= mo.Delta;
						total	= mo.Total;
						
						// !- Al decir que siempre es mayor o igual O menor o igual
						// actualizamos siempre el ciclo al ultimo precio
						// ...
						// !- Valor minimo
						if( ask <= m_ask )
							m_ask = ask;
						if( bid <= m_bid )
							m_bid = bid;
						if( delta <= m_delta )
							m_delta = delta;
						if( total <= m_total ){
							m_total = total;
							MinPrice = pl.Key;
						}
						// !- Valor maximo
						if( ask >= M_ask )
							M_ask = ask;
						if( bid >= M_bid )
							M_bid = bid;
						if( Math.Abs(delta) >= Math.Abs(M_delta) )
							M_delta = delta;
						if( total >= M_total ){
							M_total = total;
							MaxPrice = pl.Key;
						}
//						price = pl.Key;
//						// !- precio mas alto y bajo de la escalera respectivamente
//						if( price > this.highPrice )
//							this.highPrice = price;
//						if( price < this.lowPrice )
//							this.lowPrice = price;
					}
					minVolume.CalculateSigmaVolume(m_bid, m_ask, m_delta, m_total);
					maxVolume.CalculateSigmaVolume(M_bid, M_ask, M_delta, M_total);
				}
				public void CalculateMinAndMax(ref MarketOrder minVolume, ref MarketOrder maxVolume)
				{
					// !- Inutil
					double mpv = -1;
					double Mpv = -1;
					this.CalculateMinAndMax(ref minVolume, ref maxVolume, out mpv, out Mpv);
				}
				public void CalculateTotalVolume(ref MarketOrder totalVol)
				{
					totalVol.Clear();
					foreach(var pl in ladder){
						totalVol.CalculateSigmaVolume(pl.Value);
					}
				}
				private void _setPriceLadder(double price)
				{
					if( price > this.highPrice )
						this.highPrice = price;
					if( price < this.lowPrice )
						this.lowPrice = price;
					// !- si no existe el nivel de precio en el ladder lo creamos
					if( !ladder.ContainsKey(price) ){
						ladder[price] = new MarketOrder();
					}
				}
				public void AddPrice(double price, MarketOrder volume)
				{
					this._setPriceLadder(price);
					ladder[price].CalculateSigmaVolume(volume);
				}
				public void AddPrice(MarketDataEventArgs MarketArgs)
				{
					double price = MarketArgs.Price;
					
					this._setPriceLadder(price);
					ladder[price].CalculateSigmaVolume(MarketArgs);
				}
				
				#region DEFAULT_IMPS
				
				public IEnumerator<KeyValuePair<double, MarketOrder>> GetEnumerator(){
					return ladder.GetEnumerator();
				}
				public MarketOrder this[double price]
				{
					get{ return ladder[price]; }
			        set{ ladder[price] = value; }
				}
				public bool TryRemove(double price, out MarketOrder marketOrder){
					return ladder.TryRemove(price, out marketOrder);
				}
				public bool PriceExists(double price){
					return ladder.ContainsKey(price);
				}
				public void Clear()
				{
					this.ladder.Clear();
				}
				public int Count
				{
					get{ return this.ladder.Count; }
				}
				
				#endregion
			}
			#region VOLUME_PROFILE
			
			public class Profile
			{
				private SEBars<Profile.Ladder> internalProfileBars;
				private Func<int, bool> calculateProfilePeriod;
				private Ladder realtimeProfileLadder;
				private bool _realtimeLockCalcs;
				private Bars NT8Bars;
				private WyckoffBars internalWBars;
				private int Period;
				private int startBarIndex;
				private int lastBarIndex;
				private double minPrice;
				private double maxPrice;
				
				public Profile(WyckoffBars wyckoffBars)
				{
					this.internalProfileBars = new SEBars<Profile.Ladder> ();
					this.internalWBars = wyckoffBars;
					this.NT8Bars = this.internalWBars.NT8Bars;
					// !- Por defecto 1 dia de perfil de volumen
					this.Period = 1;
					this.calculateProfilePeriod = this._calculateDaysPeriod;
					this.startBarIndex = 0;
					this.realtimeProfileLadder= null;
					//this.beginTime = internalWBars.NT8Bars.GetTime(0); //  tiempo en que empezamos el profile
				}
				// !- esta clase tiene como objetivo optimzar los calculos en tiempo real, evitando
				// sobrecargar recursos innecesariamente
				public class Ladder : PriceLadder
				{
					private MarketOrder _minVolume;
					private MarketOrder _maxVolume;
					private MarketOrder _profileVolume;					
					private double _minPrice;
					private double _maxPrice;
					private int startIndex;
					private int endIndex;
					
					public Ladder(int startBarIndex, int endBarIndex)
					{
						this.startIndex = startBarIndex;
						this.endIndex = endBarIndex;
						this._minVolume = new MarketOrder();
						this._maxVolume = new MarketOrder();
						this._profileVolume = new MarketOrder();
					}
					public void setStartBarIndex(int barIndex)
					{
						this.startIndex= barIndex;
					}
					public void setEndBarIndex(int barIndex)
					{
						this.endIndex= barIndex;
					}
					// !- Optmizacion del Profile, de este modo hacemos los calculos una ves y los guardamos
					public void CalculateMinAndMax()
					{
						base.CalculateMinAndMax(ref _minVolume, ref _maxVolume, out _minPrice, out _maxPrice);
					}
					public void CalculateTotalVolume()
					{
						base.CalculateTotalVolume(ref _profileVolume);
					}
					public MarketOrder MinVolume
					{
						get{ return this._minVolume; }
					}
					public MarketOrder MaxVolume
					{
						get{ return this._maxVolume; }
					}
					public double MinLadderPrice
					{
						get{ return this._minPrice; }
					}
					public double MaxLadderPrice
					{
						get{ return this._maxPrice; }
					}
					public MarketOrder ProfileVolume
					{
						get{ return this._profileVolume; }
					}
					public int StartBarIndex
					{
						get{ return this.startIndex; }
					}
					public int EndBarIndex
					{
						get{ return this.endIndex; }
					}
					public int TotalBars
					{
						get{ return this.endIndex - this.startIndex; }
					}
				}
				#region VOLUME_PROFILE_PERIOD
				
				// !- Calculos para el rango del volume profile
				public void setTimePeriod(int Period, PeriodMode periodMode)
				{
					switch(periodMode)
					{
//						case PeriodMode.Bars:
//						{
//							this.calculateProfilePeriod = this._calculateBarsPeriod;
//							break;
//						}
						case PeriodMode.Minutes:
						{
							this.calculateProfilePeriod = this._calculateMinutesPeriod;
							break;
						}
						case PeriodMode.Hours:
						{
							this.calculateProfilePeriod = this._calculateHoursPeriod;
							break;
						}
						case PeriodMode.Days:
						{
							this.calculateProfilePeriod = this._calculateDaysPeriod;
							break;
						}
					}
					this.Period = Period;
				}
//				private bool _calculateBarsPeriod(int barIndex)
//				{
//					int currBar = this.internalWBars.CurrentBarIndex;
//					if( currBar < Period )
//						return false;
//					return currBar%Period == 0;
//				}
				private bool _calculateMinutesPeriod(int barIndex)
				{
					return (this.NT8Bars.GetTime(barIndex) - this.NT8Bars.GetTime(this.startBarIndex)).TotalMinutes > this.Period;
				}
				private bool _calculateHoursPeriod(int barIndex)
				{
					return (this.NT8Bars.GetTime(barIndex) - this.NT8Bars.GetTime(this.startBarIndex)).TotalHours > this.Period;
				}
				private bool _calculateDaysPeriod(int barIndex)
				{
					DateTime currTime = this.NT8Bars.GetTime(barIndex);
					DateTime prevTime = this.NT8Bars.GetTime(this.startBarIndex);
					int currDay = currTime.Day;
					int prevDay = prevTime.Day;
					
					return ( currDay != prevDay && Math.Abs(currDay - prevDay) >= this.Period );//return (- ).TotalDays >= this.Period;
				}
				
				#endregion
				public void setRealtimeCalculations(bool realtimeCalculation)
				{
					if( realtimeCalculation ){
						if( this.realtimeProfileLadder != null ){
							this.realtimeProfileLadder.Clear();
							this.realtimeProfileLadder = null;
						}
						this.realtimeProfileLadder = new Ladder(-1, -1);
					}
					this._realtimeLockCalcs = false;
				}
				// !- Agregamos el perfil dinamicamente(mercado real)
				private void AddRealtimeProfile(int currentBar, MarketDataEventArgs MarketArgs)
				{
					if( !this._realtimeLockCalcs ){
						WyckoffBars.Bar bar;
						// !- empezamos desde el ultimo volume profile
						for(int i = this.startBarIndex;i <= currentBar; i++){
							bar = this.internalWBars[i];
							foreach(var b in bar){
								realtimeProfileLadder.AddPrice(b.Key, b.Value);
							}
						}
						// !- lo necesitamos para los calculos de graficos
						realtimeProfileLadder.setStartBarIndex(this.startBarIndex);
						this._realtimeLockCalcs = true;
					}
					// !- agregamos la nueva informacion que llegue
					realtimeProfileLadder.AddPrice(MarketArgs);
					realtimeProfileLadder.setEndBarIndex(currentBar);
					// !- costoso pero necesario...
					realtimeProfileLadder.CalculateMinAndMax();
					realtimeProfileLadder.CalculateTotalVolume();
				}
				private bool _addProfile(int beginIndex, int endIndex)
				{
					Ladder pl = new Ladder(beginIndex, endIndex);
					int totalBars = pl.TotalBars;
					int startIndex = endIndex - totalBars;
					
					WyckoffBars.Bar bar;
					// !- Hasta la barra actual
					for(;startIndex <= endIndex; startIndex++)
					{
						bar = this.internalWBars[startIndex];
						// *- iteramos cada nivel de precio de la barra actual
						foreach(var b in bar){
							pl.AddPrice(b.Key, b.Value);
						}
					}
					// !- Hacemos los calculos una vez, optimizando asi la informacion de volumen
					pl.CalculateMinAndMax();
					pl.CalculateTotalVolume();
					return this.internalProfileBars.Add(endIndex, pl);
				}
				// !- agregamos el perfil en el rango de barras seleccionado por el usuario
				public bool AddRangeProfile(int beginIndex, int endIndex)
				{
					return _addProfile(beginIndex, endIndex);
				}
				// !- Agregamos el perfil estaticamente
				public void AddMarketProfile(int barIndex, MarketDataEventArgs MarketArgs)
				{
					if( this.internalWBars == null ){
						return;
					}
					if( this.internalWBars.IsNewBar && calculateProfilePeriod(barIndex)  ){
						// *- al crear un nuevo perfil podemos borrar el buffer de tiempo real y limpiar los calculos
						this.realtimeProfileLadder.Clear();
						this._realtimeLockCalcs = false;
						
						bool added = this._addProfile(this.startBarIndex, barIndex);
						this.startBarIndex = barIndex;// + 1;	
					}
					if( this.internalWBars.IsMarketRealtime && this.realtimeProfileLadder != null ){
						this.AddRealtimeProfile(this.internalWBars.CurrentBarIndex, MarketArgs);
					}
				}
				
				public  IEnumerator<KeyValuePair<int, Ladder>> GetEnumerator()
				{
					return this.internalProfileBars.GetEnumerator();
				}
				public  Ladder GetProfile(int barIndex)
				{
					return this.internalProfileBars[barIndex];
				}
				public void RemoveProfile(int barIndex)
				{
					this.internalProfileBars.Remove(barIndex);
				}
				public Ladder GetRealtimeProfile
				{
					get{ return this.realtimeProfileLadder; }
				}
				public int LastProfileIndex
				{
					get{ return this.startBarIndex; }
				}
				// !- con esta funcion podemos obtener un determinado perfil de volumen
				// en el rango que haya sido creado si el indice de la barra pasada como
				// argumento se encuentra dentro del rango de este
				public int GetProfileInRange(int barIndex)
				{
					int idx;
					foreach( var p in this.internalProfileBars )
					{
						idx = p.Key;
						if( Math2.isBounded(barIndex, p.Value.StartBarIndex, idx) ){
							return idx;
						}
					}
					return -1;
				}
				public void Clear()
				{
					this.internalProfileBars.Clear();
				}
				public bool Exists(int barIndex)
				{
					return internalProfileBars.ContainsKey(barIndex);
				}
				public MarketOrder GetProfileVolume(int barIndex)
				{
					return internalProfileBars[barIndex].ProfileVolume;
				}
				public double GetLadderHighPrice(int barIndex)
				{
					return internalProfileBars[barIndex].HighPrice;
				}
				public double GetLadderLowPrice(int barIndex)
				{
					return internalProfileBars[barIndex].LowPrice;
				}
				public MarketOrder GetLadderMaxVolume(int barIndex)
				{
					return internalProfileBars[barIndex].MaxVolume;
				}
				public MarketOrder GetLadderMinVolume(int barIndex)
				{
					return internalProfileBars[barIndex].MinVolume;
				}
				public int TotalBars(int barIndex)
				{
					return this.internalProfileBars[barIndex].TotalBars; //barIndex - internalProfileBars[barIndex].StartBarIndex;
				}
				public int TotalProfiles
				{
					get { return this.internalProfileBars.Count; } 
				}
				public int StartBarIndex(int barIndex)
				{
					return internalProfileBars[barIndex].StartBarIndex;
				}
			}
			
			#endregion
			#region BOOKMAP_CORE
			
			public enum OrderType
			{
				Bid, Ask,
//				BidRemoved, AskRemoved,
				Unknown
			}
			public class OrderInfo
			{
				public long Volume;
				public OrderType Type;
				
				public OrderInfo(){}
				public OrderInfo(long Volume, OrderType orderType){
					this.Volume = Volume;
					this.Type = orderType;
				}
			}
			public class OrderBookLadder
			{
				private ConcurrentDictionary<double, OrderInfo> orderLadder;
				// !- Mayor cluster de ladder
				private long maxOrderVolume;
				private double maxOrderPrice;
				// !- Alto mas alto del ladder ASK
				private long highOrderVolume;
				private double highOrderPrice;
				// !- Alto mas alto del ladder BID
				private long lowOrderVolume;
				private double lowOrderPrice;
				// !- Precio
				private double marketPrice;
				private double tickSize;
				
				private int priceLadderRange;
				private bool isDefaultLadder;
				
				public OrderBookLadder(double tickSize)
				{
					this.orderLadder = new ConcurrentDictionary<double, OrderInfo>();
					this.maxOrderVolume = 0;
					this.highOrderVolume = 0;
					this.highOrderPrice = 0;
					this.lowOrderVolume = long.MaxValue;
					this.lowOrderPrice = double.MaxValue;
					this.marketPrice = 0;
					this.tickSize = tickSize;
					// !- por defecto 10 niveles de precio
					this.priceLadderRange = 10;
					this.isDefaultLadder= true;
				}
				private void _setLadderMinAndMax(MarketDepthEventArgs depthMarketArgs)
				{
					double price = depthMarketArgs.Price;
					long volume = depthMarketArgs.Volume;
					
					if( volume >= this.maxOrderVolume )
					{
						this.maxOrderPrice = price;
						this.maxOrderVolume = volume;
					}
					// !- alto mas alto del ladder ASK
					if( depthMarketArgs.MarketDataType == MarketDataType.Ask )
					{
						if( price >= this.highOrderPrice ){
							this.highOrderPrice = price;
							this.highOrderVolume = volume;
						}
						return;
					}
					// !- bajo mas bajo del ladder BID
					if( depthMarketArgs.MarketDataType == MarketDataType.Bid )
					{
						if( price <= this.lowOrderPrice ){
							this.lowOrderPrice = price;
							this.lowOrderVolume = volume;
						}
					}
				}
				private void _setLadderMinAndMax(double price, OrderInfo orderInfo)
				{
					long volume = orderInfo.Volume;
					if( volume >= this.maxOrderVolume )
					{
						this.maxOrderVolume = volume;
						this.maxOrderPrice = price;
					}
					// !- alto mas alto del ladder ASK
					if( orderInfo.Type == OrderType.Ask )
					{
						if( price >= this.highOrderPrice ){
							this.highOrderVolume = volume;
							this.highOrderPrice = price;
						}
						return;
					}
					// !- bajo mas bajo del ladder BID
					if( orderInfo.Type == OrderType.Bid )
					{
						if( price <= this.lowOrderPrice ){
							this.lowOrderVolume = volume;
							this.lowOrderPrice = price;
						}
					}
				}
				// !- True: excede el rango de la escalera de precios
				private bool exceedsPriceLadder(MarketDepthEventArgs depthMarketArgs)
				{
					// !- por defecto son 10 niveles
					if( this.isDefaultLadder ){
						return false;
					}
					double dist = Math.Abs(depthMarketArgs.Price - this.marketPrice) / this.tickSize;
					if( depthMarketArgs.MarketDataType == MarketDataType.Ask ){
						if( dist >= this.priceLadderRange)
							return true;
					}
					else if( depthMarketArgs.MarketDataType == MarketDataType.Bid ){
						if( dist >= (this.priceLadderRange + 1) )
							return true;
					}
					
					return false;
				}
				private bool exceedsPriceLadder(double price, OrderInfo orderInfo)
				{
					// !- por defecto son 10 niveles
					if( this.isDefaultLadder ){
						return false;
					}
					if( orderInfo.Volume == 0 ){
						return true;
					}
					double dist = Math.Abs(price - this.marketPrice) / this.tickSize;
					if( orderInfo.Type == OrderType.Ask ){
						if( dist >= this.priceLadderRange)
							return true;
					}
					else if( orderInfo.Type == OrderType.Bid ){
						if( dist >= (this.priceLadderRange + 1))
							return true;
					}
					return false;
				}
				public bool PriceExists(double price)
				{
					return this.orderLadder.ContainsKey(price);
				}
				// !- si el rango permitido de la escalera de precios supera los 10 niveles
				// guardamos los 10+X niveles en el diccionario de precios, estos quedaran
				// en esos niveles(por donde el precio pase/paso) hasta que el precio pase
				// nuevamente...
				public void SetLadderRange(int priceLadderRange)
				{
					// !- por defecto solo se muetran 10 niveles de precio del libro de ordenes
					if( priceLadderRange <= 10 ){						
						this.isDefaultLadder = true;
					}
					else{
						this.priceLadderRange = priceLadderRange;
						this.isDefaultLadder = false;
					}
				}
				// !- seteamos el precio actual de mercado, el cual representa
				// el "centro" de la escalera de precios
				public void SetMarketPrice(double marketPrice)
				{
					this.marketPrice = marketPrice;
				}
				// !- obtiene la informacion de mercado en la escalera de precios si esta existe
				// de otro modo la crea
				private OrderInfo getOrderInfo(MarketDepthEventArgs depthMarketArgs, bool newOrder)
				{
					OrderInfo orderInfo;
					// !- si no existe la orden la creamos
					if( newOrder == true ){
						orderInfo = new OrderInfo();
					}
					else{
						orderInfo = this.orderLadder[depthMarketArgs.Price];
					}
					orderInfo.Type = OrderType.Unknown;
					if( depthMarketArgs.MarketDataType == MarketDataType.Ask )
						orderInfo.Type = OrderType.Ask;
					else if( depthMarketArgs.MarketDataType == MarketDataType.Bid )
						orderInfo.Type = OrderType.Bid;
					
					// !- actualizamos las ordenes entrantes si por defecto solo permitimos
					// 10 niveles de precio
					if( this.isDefaultLadder ){
						orderInfo.Volume = depthMarketArgs.Volume;
						return orderInfo;
					}
					// !- no actualizamos el volumen de orden, asi aun si la orden fue removida
					// dejamos la huella de esta.
					if( depthMarketArgs.Operation != Operation.Remove ){
						orderInfo.Volume = depthMarketArgs.Volume;
					}
					return orderInfo;
				}
				public void AddOrder(double marketPrice, MarketDepthEventArgs depthMarketArgs)
				{
					double price = depthMarketArgs.Price;
					// !- precio del mercado
					this.marketPrice = marketPrice;
					// !- movemos el minimo y maximo de la escalera de precio
					this._setLadderMinAndMax(depthMarketArgs);
					// -- Si el nivel de precio existe actualizamos los valores
					if( orderLadder.ContainsKey(price)){
						// -- Si la orden fue actualiza en el nivel de precio entonces,
						// actualizamos ese nivel en el ladder
						orderLadder[price] = this.getOrderInfo(depthMarketArgs, false);
					}
					else{
						if( this.exceedsPriceLadder(depthMarketArgs) )
							return;
						// -- Una nueva orden a mercado ha sido lanzada, creamos el nivel de precio en el ladder
						orderLadder[price] = this.getOrderInfo(depthMarketArgs, true);
					}
				}
				public void AddOrder(MarketDepthEventArgs depthMarketArgs)
				{
					this.AddOrder(depthMarketArgs.Price, depthMarketArgs);
				}
				public void AddOrder(double price, OrderInfo orderInfo)
				{
					// !- si la orden fue removida o excede el nivel de precio configurado
					if( this.exceedsPriceLadder(price, orderInfo) || orderLadder.ContainsKey(price) )
						return;
					orderLadder[price] = new OrderInfo(orderInfo.Volume, orderInfo.Type);
					this._setLadderMinAndMax(price, orderInfo);
				}
				public void RemoveOrder(double price)
				{
					OrderInfo tmp;
					this.orderLadder.TryRemove(price, out tmp);
				}
				
				public long MaxOrderVolume
				{
					get{ return this.maxOrderVolume; }
				}
				public double MaxOrderPrice
				{
					get{ return this.maxOrderPrice; }
				}
				public long HighOrderVolume
				{
					get{ return this.highOrderVolume; }
				}
				public double HighOrderPrice
				{
					get{ return this.highOrderPrice; }
				}
				public long LowOrderVolume
				{
					get{ return this.lowOrderVolume; }
				}
				public double LowOrderPrice
				{
					get{ return this.lowOrderPrice; }
				}
				public double MarketPrice
				{
					get{ return this.marketPrice; }
				}
				public IEnumerator<KeyValuePair<double, OrderInfo>> GetEnumerator(){
					return orderLadder.GetEnumerator();
				}
				public OrderInfo this[double price]
				{
					get{ return this.orderLadder[price]; }
			        set{ this.orderLadder[price] = value; }
				}
				public int Count
				{
					get{ return this.orderLadder.Count; }
				}
				public void Clear()
				{
					this.orderLadder.Clear();
				}
			}
			public class BookMap
			{
				private int currBarIndex;
				private bool firstOrder;
				private double tickSize;
				private string sessionFile;
				private float filterSessionPercent;
				private Bars NT8_Bars;
				private Dictionary<DateTime, OrderBookLadder> bookMap;
				private SortedDictionary<double, OrderInfo> orderBookDB;
				private int ladderRange;
				private DateTime lastMarketTime;
				
				public BookMap(Bars bars)
				{
					this.NT8_Bars = bars;
					this.tickSize = bars.Instrument.MasterInstrument.TickSize;
					this.bookMap = new Dictionary<DateTime, OrderBookLadder>();
					this.orderBookDB = null;
					this.currBarIndex = 0;
					this.filterSessionPercent = 0;
					this.firstOrder = true;
					this.sessionFile= string.Empty;
					// !- 10 niveles en la escalera de precios por defecto
					this.ladderRange= 10;
				}
				public Bars NT8Bars
				{
					get{ return this.NT8_Bars; }
				}
				
				public OrderBookLadder getOrderBookLadder(int barIndex)
				{
					DateTime barTime = this.NT8_Bars.GetTime(barIndex);
					if( !bookMap.ContainsKey(barTime) )
						return null;
					return bookMap[barTime];
				}
				public void setLadderRange(int ladderRange)
				{
					this.ladderRange = ladderRange;
				}
				
				#region BOOKMAP_SESSION_LOADER
				
				public enum SessionError{
					SUCCESSFUL_LOADING,
					FILE_PATH_ERROR,
					TIMESTAMP_ERROR,
					SESSION_NOT_EXIST,
					LADDER_LOADING_ERROR,
					TIMEFRAME_ERROR,
					INSTRUMENT_ERROR
				}
				public void SaveSessionFile(string sessionFile)
				{
					this.sessionFile = sessionFile;
					// !- si no existe lo creamos
					if( orderBookDB == null ){
						this.orderBookDB = new SortedDictionary<double, OrderInfo>();
					}
					else{
						this.orderBookDB.Clear();
					}
				}
				public void setFilterSessionPercent(float filterPercent)
				{
					this.filterSessionPercent = filterPercent;
				}
				private SessionError getInstrumentInfo(string line)
				{
					string[] instrument_info = line.Split(new char[]{' '});
					// !- periodo
					if( !instrument_info[0].Equals( this.NT8_Bars.BarsType.BarsPeriod.Value.ToString()) ||
						// !- tiempo: minute, second, etc..
						!instrument_info[1].Equals( "Second" ) ){//this.NT8_Bars.BarsType.BarsPeriod ) ){
						return SessionError.TIMEFRAME_ERROR;
					}
					if( !instrument_info[2].Equals( this.NT8_Bars.Instrument.MasterInstrument.Name ) ){
						return SessionError.INSTRUMENT_ERROR;
					}
					return SessionError.SUCCESSFUL_LOADING;
				}
				private int loadNewBar(string line, ref DateTime orderBookLadderTimestamp)
				{
					string s_orderBookLadderTimestamp = string.Empty;
					// !- extraemos el timestamp de la barra(copiamos hasta el BID del ladder)
					int bar_time_pos = StrUtils.extractInfo(line, out s_orderBookLadderTimestamp, '(');
					// -- Convertimos la informacion extraida a DateTime
					if( !DateTime.TryParse(s_orderBookLadderTimestamp, out orderBookLadderTimestamp) ){
						return -1;
					}
					bookMap[orderBookLadderTimestamp] = new OrderBookLadder(this.tickSize);
					bookMap[orderBookLadderTimestamp].SetLadderRange(this.ladderRange);	
					
					return bar_time_pos;
				}
				private int loadLadderInfo(string line, int start_pos, ref OrderBookLadder orderBookLadder, char first_key, char end_key, OrderType orderType)
				{
					string info = string.Empty;
					double price;
					long volume;
					// !- extramos la informacion del ladder y la 'spliteamos' para obtener el precio y volumen
					int last_pos = StrUtils.extractInfo(line, out info, first_key, end_key, start_pos);
					if( last_pos == -1 )
						return -1;
					string[] order_info = info.Split(new char[]{';'});
					OrderInfo orderInfo = new OrderInfo();
					
					foreach(string s in order_info){
						int s_pos = s.IndexOf(' ');
						if( s_pos == -1 )
							continue;
						
						// !- copiamos el precio y lo convertimos a double
						if(!double.TryParse(s.Substring(0, s_pos), out price))
							return -1;
						if(double.IsNaN(price) || double.IsInfinity(price))
							return -1;
						// !- copiamos el volumen y lo convertimos a long
						if(!long.TryParse(s.Substring(s_pos + 1), out volume))
							return -1;
						
						orderInfo.Volume = volume;
						orderInfo.Type = orderType;
						orderBookLadder.AddOrder(price, orderInfo);
					}
					
					return last_pos;
				}
				private bool copyLastLadderInfo(int barIndex, ref OrderBookLadder orderBookLadder)
				{
					int prevBarIndex = barIndex - 1;
					if( prevBarIndex < 0 )
						return false;
					OrderBookLadder orderLadder = this.getOrderBookLadder(prevBarIndex);
					if( orderLadder == null )
						return false;
					// !- copiamos la escalera anterior de precios a la barra actual
					//bookMap[orderBookLadderTimestamp].SetMarketPrice(this.NT8_Bars.GetClose(prevBarIndex));
					foreach(var order in orderLadder){
						orderBookLadder.AddOrder(order.Key, order.Value);
					}
					return true;
				}
				private void filterLadderInfo(ref OrderBookLadder orderBookLadder)
				{
					float volPer = 0;
					long maxVolumeLadder = orderBookLadder.MaxOrderVolume;
					List<double> removals = new List<double>();
					foreach(var order in orderBookLadder){
						if( (float)Math2.Percent(maxVolumeLadder, order.Value.Volume) < this.filterSessionPercent ){
							removals.Add(order.Key);
						}
					}
					foreach(double price in removals){
						orderBookLadder.RemoveOrder(price);
					}
				}
				public SessionError LoadSessionFile(string sessionFile)
				{
					SessionError err = SessionError.SUCCESSFUL_LOADING;
					if( bookMap == null || sessionFile.IsNullOrEmpty() ){
						return SessionError.FILE_PATH_ERROR;
					}
					// !- si en el mapa ya existia informacion la descartamos...
					bookMap.Clear();
					using (StreamReader bookMapSessionFile = File.OpenText(sessionFile))
				    {
						string line;
						bool is_first_line = true;
						int barIndex = -1;
						int totalBars = this.NT8_Bars.Count;
						DateTime orderBookLadderTimestamp = new DateTime();
						
						while ((line = bookMapSessionFile.ReadLine()) != null)
						{
							if( line.IsNullOrEmpty() )
								continue;
							if( line[0] == '#' ){
								// !- omitimos el #
								err = getInstrumentInfo(line.Substring(1));
								if( err != SessionError.SUCCESSFUL_LOADING )
									break;
								continue;
							}
							
							int timestamp_pos = loadNewBar(line, ref orderBookLadderTimestamp);
							if( timestamp_pos < 0 ){
								err = SessionError.TIMESTAMP_ERROR;
								break;
							}
							
							OrderBookLadder orderBookLadder = new OrderBookLadder(this.tickSize);
							orderBookLadder.SetLadderRange(this.ladderRange);
							/// !- ya que .GetBar es expensiva buscamos(solo una vez) el tiempo correspondiente
							/// a la primera barra guardada en la sesion, luego solo incrementamos el puntero
							/// a medida que se crean las barras generadas por el bookmap
							if( !is_first_line ){
								// !- la fecha extraida de la sesion no existe(porque probablemente el grafico no haya llegado a tal punto)
								if( this.NT8_Bars.LastBarTime.CompareTo(orderBookLadderTimestamp) < 0 ){
									err = SessionError.SESSION_NOT_EXIST;
									break;
								}
								barIndex = this.NT8_Bars.GetBar(orderBookLadderTimestamp);
							}
							else{
								barIndex++;
							}
							// !- las barras aun no existen, no necesitamos cargar nada...
							if( barIndex > totalBars )
								break;
							// !- el precio actual de mercado en el momento en que se creo el ladder
							orderBookLadder.SetMarketPrice(this.NT8_Bars.GetClose(barIndex));
							
							// !- primero copiamos el ladder Bid(porque en este orden fue guardado)
							int bid_pos = loadLadderInfo(line, timestamp_pos, ref orderBookLadder, '(', ')', OrderType.Bid);
							if( bid_pos < 0 ){
								err = SessionError.LADDER_LOADING_ERROR;
								break;
							}
							// !- finalmente copiamos el ladder Ask
							int ask_pos = loadLadderInfo(line, bid_pos, ref orderBookLadder, '{', '}', OrderType.Ask);
							if( ask_pos < 0 ){
								err = SessionError.LADDER_LOADING_ERROR;
								break;
							}
							// !- copiamos los precios de la escalera de precios previa para ir formando
							// el mapa de liquidez
							if( !is_first_line )
							{
								if( !copyLastLadderInfo( barIndex, ref orderBookLadder ) ){
									err = SessionError.LADDER_LOADING_ERROR;
									break;
								}
							}
							// !- si el % del filtro de liquidez es mayor que 0 filtramos...
							if( this.filterSessionPercent > 0 ){
								filterLadderInfo(ref orderBookLadder);
							}
							// !- copiamos el ladder al bookmap
							bookMap[orderBookLadderTimestamp] = orderBookLadder;
							
							is_first_line = false;
						}
						// !- liberamos el archivo...
						bookMapSessionFile.Close();
						bookMapSessionFile.Dispose();
					}
					
					return err;
				}
				
				#endregion
				#region BOOKMAP_SESSION_REGISTER
				
				private void addSessionOrder(MarketDepthEventArgs depthMarketArgs)
				{
					if( orderBookDB == null )
						return;
					long volume = depthMarketArgs.Volume;
					if( volume == 0 )
						return;
					
					double price = depthMarketArgs.Price;
					OrderType orderType = OrderType.Unknown;
					if( depthMarketArgs.MarketDataType == MarketDataType.Ask )
						orderType = OrderType.Ask;
					if( depthMarketArgs.MarketDataType == MarketDataType.Bid )
						orderType = OrderType.Bid;
					
					// -- Si el nivel de precio existe actualizamos los valores
					if( orderBookDB.ContainsKey(price)){
						// -- Si la orden fue actualiza en el nivel de precio entonces,
						// actualizamos ese nivel en el ladder
						orderBookDB[price].Type = orderType;
						orderBookDB[price].Volume = volume;
					}
					else{
						// -- Una nueva orden a mercado ha sido lanzada, creamos el nivel de precio en el ladder
						orderBookDB[price] = new OrderInfo(volume, orderType);
					}
				}
				private void saveLastOrderBook()
				{
					if( orderBookDB == null )
						return;
					// !- lo ejecutamos solo la primera vez(es decir la primer orden)
					if( firstOrder ){
						string instrument_info = this.NT8_Bars.BarsType.BarsPeriod.ToString() + ' ';
						instrument_info += this.NT8_Bars.Instrument.MasterInstrument.Name.ToString();
						
						File.AppendAllText(this.sessionFile, '#' + instrument_info + '\n');
						firstOrder = false;
						return;
					}
					
					string askInfo = string.Empty;
					string bidInfo = string.Empty;
					double price;
					long volume;
					foreach(var order in orderBookDB){
						volume= order.Value.Volume;
						price = order.Key;
						if( order.Value.Type == OrderType.Ask ){
							askInfo += price;
							askInfo += ' ';
							askInfo += volume;
							askInfo += ';';
						}
						if( order.Value.Type == OrderType.Bid ){
							bidInfo += price;
							bidInfo += ' ';
							bidInfo += volume;
							bidInfo += ';';
						}
					}
					
					File.AppendAllText(this.sessionFile, lastMarketTime.ToString() +
						'(' + bidInfo + ')' +
						'{' + askInfo + '}' +
						'\n'
					);
					
					orderBookDB.Clear();
				}
				
				#endregion
				private void copyLastLadder(DateTime barTime)
				{
					int prevBarIndex = this.currBarIndex - 2;
					if( prevBarIndex < 0 )
						return;
					OrderBookLadder orderLadder = this.getOrderBookLadder(prevBarIndex);//bookMap[this.NT8_Bars.GetTime(prevBarIndex)];
					if( orderLadder == null )
						return;
					// !- copiamos la escalera anterior de precios a la barra actual
					bookMap[barTime].SetMarketPrice(this.NT8_Bars.LastPrice);//bookMap[toBarTime].SetMarketPrice(orderLadder.MarketPrice);
					foreach(var order in orderLadder){
						bookMap[barTime].AddOrder(order.Key, order.Value);
					}
					// !- limpiamos la informacion de maxima orden de la escalera de precios
					//bookMap[toBarTime].resetMaxOrderInfo();
				}
				// !- nivel II
				public void onMarketDepth(MarketDepthEventArgs depthMarketArgs)
				{
					double price = depthMarketArgs.Price;
					long volume = depthMarketArgs.Volume;
					this.currBarIndex = this.NT8_Bars.Count;
					DateTime currBarTime = this.NT8_Bars.GetTime(currBarIndex);
					
					// -- Si no existe la barra la creamos
					if( !bookMap.ContainsKey(currBarTime) ){
						bookMap[currBarTime] = new OrderBookLadder(this.tickSize);
						bookMap[currBarTime].SetLadderRange(this.ladderRange);
						this.copyLastLadder(currBarTime);
						this.saveLastOrderBook();
					}
					bookMap[currBarTime].AddOrder(this.NT8_Bars.LastPrice, depthMarketArgs);
					
					this.addSessionOrder(depthMarketArgs);
					// !- ultimo tiempo de barra creado, necesario para la DB
					this.lastMarketTime = currBarTime;
				}
			}
			
			#endregion
			#region WYCKOFF_BARS_CLASS
			
			public class WyckoffBars : SEBars<WyckoffBars.Bar>
			{
				private bool isNewBar;
				private long minClusterVolumeFilter;
				private VolumeType volumeType;
				private int currentBar;
				private int lastBarLoaded;
				private double tickSize;
				protected Bars NT8_Bars;
				
				public WyckoffBars(Bars bars)
				{
					this.isNewBar = false;
					this.minClusterVolumeFilter = -1;
					this.NT8_Bars = bars;
					this.lastBarLoaded = bars.Count - 1;
					this.tickSize = bars.Instrument.MasterInstrument.TickSize;
					this.currentBar = 0;
					this[currentBar] = new Bar();
					//DateTime dt = bars.GetTime(bars.Count - 1);
					//this.lastBarTime = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second);
					//this.lastBarTime = this.lastBarTime.AddSeconds(-5);
					// !- Inicializamos las barras cargadas hasta el momento
					//for(int i = 0; i <= barsCount; i++) this[i] = new Bar();
				}
				
				public class Bar : MarketOrder
				{
					private PriceLadder pvLadder;
					private MarketOrder barOrderFlow;
					private MarketOrder minCluster;
					private MarketOrder maxCluster;
					private double minClusterPrice;
					private double maxClusterPrice;
					//public DateTime Time;
					
					public Bar()
					{
						this.pvLadder = new PriceLadder();
						this.minCluster = new MarketOrder();
						this.maxCluster = new MarketOrder();
					}
					public IEnumerator<KeyValuePair<double, MarketOrder>> GetEnumerator(){
						return pvLadder.GetEnumerator();
					}
					// !- la funcion recibe ordenes a mercado, por lo tanto la estructura $MarketDataEventArgs
					// cambia de informacion continuamente; precio, bid, ask, volume....
					public void CalculateMarketInfo(MarketDataEventArgs MarketArgs)
					{
						pvLadder.AddPrice(MarketArgs);
						this.CalculateSigmaVolume(MarketArgs);
					}
					public void CalculateMinAndMaxCluster()
					{
						pvLadder.CalculateMinAndMax(ref this.minCluster, ref this.maxCluster,
							out this.minClusterPrice, out this.maxClusterPrice);
					}
					public void FilterClusterVolume(long minVolume, VolumeType volType)
					{
						//List<double> priceRemovals = new List<double>();
						double price;
						foreach(var pl in pvLadder)
						{
							price = pl.Key;
							switch( volType )
							{
								case VolumeType.BidAsk:
								{
									if( Math.Abs(pl.Value.Total) < minVolume ){
										//priceRemovals.Add(pl.Key);
										MarketOrder mo = pl.Value;
										this.pvLadder.TryRemove(price, out mo);
									}
									break;
								}
								case VolumeType.Delta:
								{
									if( Math.Abs(pl.Value.Delta) < minVolume ){
										//priceRemovals.Add(pl.Key);
										MarketOrder mo = pl.Value;
										this.pvLadder.TryRemove(price, out mo);
									}
									break;
								}
							}
							
						}
						/*foreach(var p in priceRemovals){
							this.pvLadder.Remove(p);
						}*/
					}
					// !- volumen en un nivel de precio concreto, o NULL si ahi no se opero.
					// Necesario para el calculo de imbalances (comparacion diagonal entre niveles).
					public MarketOrder AtPrice(double price)
					{
						if( !this.pvLadder.PriceExists(price) )
							return null;
						return this.pvLadder[price];
					}
					public bool PriceExists(double price)
					{
						return this.pvLadder.PriceExists(price);
					}
					public MarketOrder MaxClusterVolume
					{
						get{ return maxCluster; }
					}
					public MarketOrder MinClusterVolume
					{
						get{ return minCluster; }
					}
					public double MinClusterPrice
					{
						get{ return this.minClusterPrice; }
					}
					public double MaxClusterPrice
					{
						get{ return this.maxClusterPrice; }
					}
				}
				public void enableMinClusterVolumeFilter(long minClusterVolumeFilter, VolumeType volumeType)
				{
					this.minClusterVolumeFilter = minClusterVolumeFilter;
					this.volumeType = volumeType;
				}
				public void disableMinClusterVolumeFilter()
				{
					this.minClusterVolumeFilter = -1;
				}
				public bool onMarketData(MarketDataEventArgs MarketArgs)
				{
					if( MarketArgs.MarketDataType != MarketDataType.Last )
						return false;
					
					this.isNewBar = false;
					DateTime barTime = NT8_Bars.GetTime(currentBar);
					// *- El tiempo de la orden de mercado supera al finalizado del tiempo de la barra actual
					// => una nueva barra fue creada
					if( MarketArgs.Time.CompareTo(barTime) > 0 )
					{
						// !- Hacemos los calculos del cluster una vez, optmizando asi futuros calculos innecesarios...
						this[currentBar].CalculateMinAndMaxCluster();
						// !- Solo si activamos el filtro de volumen
						if( this.minClusterVolumeFilter != -1 )
							this[currentBar].FilterClusterVolume(this.minClusterVolumeFilter, this.volumeType);
						
						// !- Tiempo de finalizado de la barra en mercado
						//this[currentBar].Time = MarketArgs.Time;
						// !- pasamos a la siguiente barra
						this.currentBar++;
						// !- creamos la nueva barra
						this[currentBar] = new Bar();
						
						this.isNewBar = true;
						//return true;
					}
					// !- Calculos de volumen en cada barra creada
					this[currentBar].CalculateMarketInfo(MarketArgs);
					// !- Ultimo tiempo de mercado
					//this[CurrentBar].Time = MarketArgs.Time;
					
					return true;
				}
				// !- SOBRECARGA: usa el indice de barra REAL de NinjaTrader (CurrentBar) en lugar de
				// llevar un contador interno basado en el tiempo del tick. Esto mantiene los clusters
				// perfectamente alineados con las barras del grafico, tanto en Tick Replay(historico)
				// como en tiempo real, y evita que el indice interno se desincronice cuando faltan
				// ticks, hay huecos de datos o la barra no avanza por tiempo (Range/Tick/Volume bars).
				public bool onMarketData(MarketDataEventArgs MarketArgs, int ntBarIndex)
				{
					if( MarketArgs.MarketDataType != MarketDataType.Last )
						return false;
					if( ntBarIndex < 0 )
						return false;

					this.isNewBar = false;

					if( ntBarIndex != this.currentBar )
					{
						// !- cerramos la barra anterior: calculos de cluster una sola vez
						if( this.ContainsKey(this.currentBar) )
						{
							this[this.currentBar].CalculateMinAndMaxCluster();
							// !- Solo si activamos el filtro de volumen
							if( this.minClusterVolumeFilter != -1 )
								this[this.currentBar].FilterClusterVolume(this.minClusterVolumeFilter, this.volumeType);
						}
						this.currentBar = ntBarIndex;
						this.isNewBar = true;
					}
					// !- creamos la barra si aun no existe
					if( !this.ContainsKey(this.currentBar) )
						this[this.currentBar] = new Bar();

					// !- Calculos de volumen en cada barra creada
					this[this.currentBar].CalculateMarketInfo(MarketArgs);

					return true;
				}
				// !- Cuando la barra termina de crearse esta retornara True, luego al pasar
				// a la siguiente nueva barra retornara False, repitiendose asi este ciclo.
				public bool IsNewBar
				{
					get{ return this.isNewBar; }
				}
				public bool BarExists(int barIndex)
				{
					return this.ContainsKey(barIndex);
				}
				//public bool IsRealtime{ get{ return currentBar >= lastBarIndex; } }
				public int CurrentBarIndex
				{
					get{ return this.currentBar; }
				}
				public Bar CurrentBar
				{
					get{ return this[currentBar]; }
				}
				// !- variable constante, una vez corriendo el mercado este valor NO aumentara
				public int LastBarLoadedIndex
				{
					get{ return this.lastBarLoaded; }
				}
				public bool IsMarketRealtime
				{
					get{ return this.CurrentBarIndex >= this.lastBarLoaded; }
				}
				
				public Bar PreviousBar{ get{ return this[currentBar - 1]; } }
				public Bars NT8Bars
				{
					get{ return this.NT8_Bars; }
				}
				public double TickSize
				{
					get{ return this.tickSize; }
				}
			}
			
			#endregion
		}
		#endregion
		
		#endregion
	}
	
	#endregion
}

// --- SOURCE: OrderFlow.cs ---
public static class _OrderFlowEnums
{
	public enum Calculation
	{
		BidAsk,
		TotalDelta,
		Total,
		Delta
	}
	public enum Representation
	{
		Volume,
		Percent
	}
	public enum Style
	{
		Profile,
		HeatMap,
		// !- celda de ancho fijo partida en dos mitades (bid | ask), como los footprints
		// comerciales: fondo teñido segun volumen y el texto "B x A" centrado en la celda.
		Footprint
	}
	public enum Position
	{
		Left,
		//Center,
		Right
	}
}

namespace NinjaTrader.NinjaScript.Indicators
{
	public class OrderFlow : Indicator
	{
		#region MAIN
		
		private class WyckoffOrderFlow : WyckoffRenderControl
		{
			private SharpDX.RectangleF Rect, Rect2;
			private Action _renderOFText;
			private Action<float, float> renderClusterRect;
			private SharpDX.DirectWrite.TextFormat volumeTextFormat;
			private SharpDX.Vector2 beg_maxClusterVec;
			private SharpDX.Vector2 end_maxClusterVec;
			
			// !- setup brushes
			private SharpDX.Color colorBidClusterColor;
			private SharpDX.Color colorAskClusterColor;
			private SharpDX.Color colorTotalClusterColor;
			private SharpDX.Color colorBidFontColor;
			private SharpDX.Color colorAskFontColor;
			private SharpDX.Color colorTotalFontColor;
			private SharpDX.Color colorMaxVolumeClusterColor;
			private SharpDX.Color colorMinVolumeClusterColor;
			private SharpDX.Color colorPOCLines;
			private SharpDX.Color colorPOILines;
			
			// !- setup opacity
			private float brushBidFontOpacity;
			private float brushAskFontOpacity;
			private float totalFontOpacity;
			private float maxClusterOpacity;
			private float minClusterOpacity;
			private float clustersOpacity;
			private float POCLinesOpacity;
			private float POILinesOpacity;
			// !- setup calculation
			private float minFontWidth;
			private float minFontHeight;
			private _OrderFlowEnums.Position orderFlowPosition;
			private _OrderFlowEnums.Style orderFlowStyle;
			private _OrderFlowEnums.Calculation orderFlowCalculation;
			private _OrderFlowEnums.Representation orderFlowRepresentation;
			// !- setup renders
			private bool showClusterPOC;
			private bool showClusterPOI;
			private bool showPOCSLines;
			private bool showPOISLines;
			private bool showText;
			private bool showOrderFlow;
			// !- delta total por barra, dibujado debajo del minimo de la vela
			private bool showBarDelta;
			private SharpDX.Color colorBarDeltaPositive;
			private SharpDX.Color colorBarDeltaNegative;
			private float barDeltaOpacity;
			private float barDeltaMinBarWidth;
			private SharpDX.DirectWrite.TextFormat barDeltaTextFormat;
			// !- tabla resumen Ask/Bid/Delta/Volume al pie del panel
			private bool showSummary;
			private float summaryRowHeight;
			private float summaryMinBarWidth;
			private float summaryOpacity;
			private SharpDX.Color colorSummaryAsk;
			private SharpDX.Color colorSummaryBid;
			private SharpDX.Color colorSummaryVolume;
			private SharpDX.Color colorSummaryText;
			private SharpDX.DirectWrite.TextFormat summaryTextFormat;
			// !- imbalances (comparacion diagonal ask[P] vs bid[P+tick])
			private bool showImbalance;
			private float imbalanceRatio;
			private long imbalanceMinVolume;
			private float imbalanceOpacity;
			private SharpDX.Color colorImbalanceBuy;
			private SharpDX.Color colorImbalanceSell;
			// !- imbalances apilados: N niveles consecutivos en la misma direccion
			private bool showStackedImbalance;
			private int stackedImbalanceCount;
			private int stackedImbalanceExtendBars;
			private float stackedImbalance_strokeWidth;
			private SharpDX.Direct2D1.StrokeStyle stackedImbalance_strokeStyle;
			// !- buffers reutilizados por barra: evitan asignaciones en cada OnRender
			private readonly List<double> imbPrices = new List<double>();
			private readonly List<int> imbFlags = new List<int>();
			private readonly Dictionary<double, int> imbByPrice = new Dictionary<double, int>();
			// !- nivel que se esta dibujando ahora mismo (lo necesita el texto de la celda)
			private double currentPrice;
			private int currentImbFlag;
			// !- estilo Footprint
			private SharpDX.Color colorImbalanceTextColor;
			private bool pocFilled;
			private SharpDX.Color colorPOCFill;
			private float pocFillOpacity;
			private long highlightVolumeThreshold;
			private SharpDX.Color colorHighlightBox;
			private float highlightBoxWidth;
			// !- celda footprint: UN color plano por celda (verde/rosa segun el delta del nivel),
			// no dos mitades. El volumen lo cuentan los numeros, no la saturacion del fondo.
			private SharpDX.Color colorFootprintUpCell;
			private SharpDX.Color colorFootprintDownCell;
			private float footprintCellOpacity;
			private SharpDX.Color colorFootprintCellBorder;
			private float footprintCellBorderWidth;
			private SharpDX.Color colorFootprintText;
			// !- tres alineaciones para poder pintar "B x A" y colorear solo un lado
			private SharpDX.DirectWrite.TextFormat fpFormatRight;
			private SharpDX.DirectWrite.TextFormat fpFormatCenter;
			private SharpDX.DirectWrite.TextFormat fpFormatLeft;

			// !- setup lines style
			private float POCLines_strokeWidth;
			private SharpDX.Direct2D1.StrokeStyle POCLines_strokeStyle;
			private float POILines_strokeWidth;
			private SharpDX.Direct2D1.StrokeStyle POILines_strokeStyle;
			
			// !- Para copiado de datos internos;
			private float barX;
			private float barY;
			private VolumeAnalysis.WyckoffBars wyckoffBars;
			private VolumeAnalysis.WyckoffBars.Bar currentBar;
			private VolumeAnalysis.MarketOrder volumeInfo;
			private bool realTime;
			
			public WyckoffOrderFlow()
			{
				this.volumeInfo = new VolumeAnalysis.MarketOrder();
				this.Rect = new SharpDX.RectangleF();
				this.Rect2= new SharpDX.RectangleF();
				this.realTime = false;
				
				this.beg_maxClusterVec = new SharpDX.Vector2();
				this.end_maxClusterVec = new SharpDX.Vector2();
			}
			
			#region SET_ORDER_FLOW_STYLE
			
			public void setFontStyle(SimpleFont font)
			{
				base.setFontStyle(font, out volumeTextFormat);
			}
			public void setBidAskClusterColor(Brush brushBidClusterColor, Brush brushAskClusterColor)
			{
				this.colorBidClusterColor = WyckoffRenderControl.BrushToColor(brushBidClusterColor);
				this.colorAskClusterColor = WyckoffRenderControl.BrushToColor(brushAskClusterColor);
			}
			public void setMaxMinVolumeClusterColor(
				Brush brushMaxVolumeClusterColor, float maxClusterOpacity,
				Brush brushMinVolumeClusterColor, float minClusterOpacity
				)
			{
				this.colorMaxVolumeClusterColor = WyckoffRenderControl.BrushToColor(brushMaxVolumeClusterColor);
				this.maxClusterOpacity = maxClusterOpacity / 100f;
				this.colorMinVolumeClusterColor = WyckoffRenderControl.BrushToColor(brushMinVolumeClusterColor);
				this.minClusterOpacity = minClusterOpacity / 100f;
			}
			public void setPOCPOILines(
				Brush brushPOCLines, float POCLines_strokeWidth, DashStyleHelper POCstrokeStyle, float POCLinesOpacity,
				Brush brushPOILines, float POILines_strokeWidth, DashStyleHelper POIstrokeStyle, float POILinesOpacity
				)
			{
				this.colorPOCLines = WyckoffRenderControl.BrushToColor(brushPOCLines);
				this.POCLinesOpacity = POCLinesOpacity / 100;
				this.POCLines_strokeWidth = POCLines_strokeWidth;
				SharpDX.Direct2D1.StrokeStyleProperties POCLines_strokeStyleProperties = new SharpDX.Direct2D1.StrokeStyleProperties();
				POCLines_strokeStyleProperties.DashStyle = DashStyleHelperToDX(POCstrokeStyle);
				
				this.POCLines_strokeStyle = new SharpDX.Direct2D1.StrokeStyle(NinjaTrader.Core.Globals.D2DFactory, POCLines_strokeStyleProperties);
				this.colorPOILines = WyckoffRenderControl.BrushToColor(brushPOILines);
				this.POILinesOpacity = POILinesOpacity / 100;
				this.POILines_strokeWidth = POCLines_strokeWidth;
				SharpDX.Direct2D1.StrokeStyleProperties POILines_strokeStyleProperties = new SharpDX.Direct2D1.StrokeStyleProperties();
				POILines_strokeStyleProperties.DashStyle = DashStyleHelperToDX(POIstrokeStyle);
				this.POILines_strokeStyle = new SharpDX.Direct2D1.StrokeStyle(NinjaTrader.Core.Globals.D2DFactory, POILines_strokeStyleProperties);
			}
			public void setBidAskFontColor(
				Brush brushBidFontColor, float bidOpacity,
				Brush brushAskFontColor, float askOpacity)
			{
				this.colorBidFontColor = WyckoffRenderControl.BrushToColor(brushBidFontColor);
				this.brushBidFontOpacity = bidOpacity / 100f;
				this.colorAskFontColor = WyckoffRenderControl.BrushToColor(brushAskFontColor);
				this.brushAskFontOpacity = askOpacity / 100f;
			}
			public void setTotalFontColor(Brush brushTotalClusterColor, Brush brushTotalFontColor, float totalOpacity)
			{
				this.colorTotalClusterColor = WyckoffRenderControl.BrushToColor(brushTotalClusterColor);
				this.colorTotalFontColor = WyckoffRenderControl.BrushToColor(brushTotalFontColor);
				this.totalFontOpacity = totalOpacity / 100f;
			}
			public void setMinSizeFont(float minFontWidth, float minFontHeight)
			{
				this.minFontWidth = minFontWidth;
				this.minFontHeight = minFontHeight;
			}
			// !- Delta total de la barra, dibujado bajo el minimo de la vela
			public void setBarDelta(
				bool showBarDelta, Brush brushPositive, Brush brushNegative,
				float barDeltaOpacity, float barDeltaMinBarWidth, SimpleFont font)
			{
				this.showBarDelta = showBarDelta;
				this.colorBarDeltaPositive = WyckoffRenderControl.BrushToColor(brushPositive);
				this.colorBarDeltaNegative = WyckoffRenderControl.BrushToColor(brushNegative);
				this.barDeltaOpacity = barDeltaOpacity / 100f;
				this.barDeltaMinBarWidth = barDeltaMinBarWidth;
				base.setFontStyle(font, out barDeltaTextFormat);
			}
			// !- Tabla resumen Ask/Bid/Delta/Volume al pie del panel
			public void setSummary(
				bool showSummary, float rowHeight, float minBarWidth, float opacity,
				Brush brushAsk, Brush brushBid, Brush brushVolume, Brush brushText, SimpleFont font)
			{
				this.showSummary = showSummary;
				this.summaryRowHeight = rowHeight;
				this.summaryMinBarWidth = minBarWidth;
				this.summaryOpacity = opacity / 100f;
				this.colorSummaryAsk = WyckoffRenderControl.BrushToColor(brushAsk);
				this.colorSummaryBid = WyckoffRenderControl.BrushToColor(brushBid);
				this.colorSummaryVolume = WyckoffRenderControl.BrushToColor(brushVolume);
				this.colorSummaryText = WyckoffRenderControl.BrushToColor(brushText);
				base.setFontStyle(font, out summaryTextFormat);
			}
			// !- Opciones del estilo Footprint (celda de ancho fijo)
			public void setFootprintStyle(
				Brush brushImbalanceText,
				bool pocFilled, Brush brushPOCFill, float pocFillOpacity,
				long highlightVolumeThreshold, Brush brushHighlightBox, float highlightBoxWidth,
				Brush brushUpCell, Brush brushDownCell, float cellOpacity,
				Brush brushCellBorder, float cellBorderWidth, Brush brushCellText, SimpleFont cellFont)
			{
				this.colorImbalanceTextColor = WyckoffRenderControl.BrushToColor(brushImbalanceText);
				this.pocFilled = pocFilled;
				this.colorPOCFill = WyckoffRenderControl.BrushToColor(brushPOCFill);
				this.pocFillOpacity = pocFillOpacity / 100f;
				this.highlightVolumeThreshold = highlightVolumeThreshold;
				this.colorHighlightBox = WyckoffRenderControl.BrushToColor(brushHighlightBox);
				this.highlightBoxWidth = highlightBoxWidth;

				this.colorFootprintUpCell = WyckoffRenderControl.BrushToColor(brushUpCell);
				this.colorFootprintDownCell = WyckoffRenderControl.BrushToColor(brushDownCell);
				this.footprintCellOpacity = cellOpacity / 100f;
				this.colorFootprintCellBorder = WyckoffRenderControl.BrushToColor(brushCellBorder);
				this.footprintCellBorderWidth = cellBorderWidth;
				this.colorFootprintText = WyckoffRenderControl.BrushToColor(brushCellText);

				// !- el mismo font en tres alineaciones: "B" pegado a la derecha, la "x" al
				// centro y "A" pegado a la izquierda => queda "115 x 39" bien centrado y
				// permite pintar de azul SOLO el lado con imbalance.
				base.setFontStyle(cellFont, out fpFormatCenter);
				base.setFontStyle(cellFont, out fpFormatRight);
				fpFormatRight.TextAlignment = SharpDX.DirectWrite.TextAlignment.Trailing;
				base.setFontStyle(cellFont, out fpFormatLeft);
				fpFormatLeft.TextAlignment = SharpDX.DirectWrite.TextAlignment.Leading;
			}
			// !- Imbalances e imbalances apilados
			public void setImbalance(
				bool showImbalance, float ratio, long minVolume, float opacity,
				Brush brushBuy, Brush brushSell,
				bool showStacked, int stackedCount, int stackedExtendBars,
				float stackedStrokeWidth, DashStyleHelper stackedStrokeStyle)
			{
				this.showImbalance = showImbalance;
				this.imbalanceRatio = ratio;
				this.imbalanceMinVolume = minVolume;
				this.imbalanceOpacity = opacity / 100f;
				this.colorImbalanceBuy = WyckoffRenderControl.BrushToColor(brushBuy);
				this.colorImbalanceSell = WyckoffRenderControl.BrushToColor(brushSell);

				this.showStackedImbalance = showStacked;
				this.stackedImbalanceCount = stackedCount;
				this.stackedImbalanceExtendBars = stackedExtendBars;
				this.stackedImbalance_strokeWidth = stackedStrokeWidth;

				SharpDX.Direct2D1.StrokeStyleProperties p = new SharpDX.Direct2D1.StrokeStyleProperties();
				p.DashStyle = DashStyleHelperToDX(stackedStrokeStyle);
				this.stackedImbalance_strokeStyle = new SharpDX.Direct2D1.StrokeStyle(
					NinjaTrader.Core.Globals.D2DFactory, p);
			}
			public void setShows(
				bool showClusterPOC, bool showClusterPOI,
				bool showPOCSLines, bool showPOISLines,
				bool showText, bool showOrderFlow)
			{
				this.showClusterPOC = showClusterPOC;
				this.showClusterPOI = showClusterPOI;
				this.showPOCSLines = showPOCSLines;
				this.showPOISLines = showPOISLines;
				this.showText = showText;
				this.showOrderFlow = showOrderFlow;
			}
			public void setPosition(_OrderFlowEnums.Position orderFlowPosition)
			{
				this.orderFlowPosition = orderFlowPosition;
			}
			public void setStyle(_OrderFlowEnums.Style orderFlowStyle)
			{
				this.orderFlowStyle = orderFlowStyle;
			}
			public void setClustersOpacity(float clustersOpacity)
			{
				this.clustersOpacity = clustersOpacity / 100f;
			}
			
			#endregion
			#region RENDER_INFORMATION
			
			private void __renderBidAskText()
			{
				long B = 0;
				long A = 0;
				string C = "";
				switch( this.orderFlowRepresentation )
				{
					case _OrderFlowEnums.Representation.Volume:
					{
						B = this.volumeInfo.Bid;
						A = this.volumeInfo.Ask;
						break;
					}
					case _OrderFlowEnums.Representation.Percent:
					{
						long total = this.volumeInfo.Total;
						B = (long)Math2.Percent(total, this.volumeInfo.Bid);
						A = (long)Math2.Percent(total, this.volumeInfo.Ask);
						// !- agregamos el simbolo de %
						C = "%";
						break;
					}
				}
				long D = volumeInfo.Delta;
				if( D > 0 ){
					myDrawText(string.Format("{0}x{1}"+C, B, A), ref Rect, colorAskFontColor, -1,-1, volumeTextFormat, brushAskFontOpacity);
				}
				else{
					myDrawText(string.Format("{0}x{1}"+C, B, A), ref Rect, colorBidFontColor, -1,-1, volumeTextFormat, brushBidFontOpacity);
				}
			}
			private void __renderTotalDeltaText()
			{
				long T = 0;
				long D = 0;
				string C = "";
				switch( this.orderFlowRepresentation )
				{
					case _OrderFlowEnums.Representation.Volume:
					{
						T = this.volumeInfo.Total;
						D = this.volumeInfo.Delta;
						break;
					}
					case _OrderFlowEnums.Representation.Percent:
					{
						long total = this.volumeInfo.Total;
						T = total;
						D = (long)Math2.Percent(total, Math.Abs(this.volumeInfo.Delta));
						// !- agregamos el simbolo de %
						C = "%";
						break;
					}
				}
				if( this.volumeInfo.Delta >= 0 ){
					myDrawText(string.Format("{0}x{1}"+C, T, D), ref Rect, colorAskFontColor, -1,-1, volumeTextFormat, brushAskFontOpacity);
				}
				else{
					myDrawText(string.Format("{0}x{1}"+C, T, D), ref Rect, colorBidFontColor, -1,-1, volumeTextFormat, brushBidFontOpacity);
				}
			}
			private void __renderTotalText()
			{
				myDrawText(volumeInfo.Total.ToString(), ref Rect, colorTotalFontColor, -1,-1, volumeTextFormat, totalFontOpacity);
			}
			private void __renderDeltaText()
			{
				long D = this.volumeInfo.Delta;
				string s_D = string.Empty;
				switch( this.orderFlowRepresentation )
				{
					case _OrderFlowEnums.Representation.Volume:
					{
						s_D = D.ToString();
						break;
					}
					case _OrderFlowEnums.Representation.Percent:
					{
						long total = this.volumeInfo.Total;
						s_D = Math2.Percent(total, Math.Abs(D)).ToString();
						// !- agregamos el simbolo de %
						s_D+= "%";
						break;
					}
				}
				
				if( D >= 0 ){
					myDrawText(s_D.ToString(), ref Rect, colorAskFontColor, -1,-1, volumeTextFormat, brushAskFontOpacity);
				}
				else{
					myDrawText(s_D.ToString(), ref Rect, colorBidFontColor, -1,-1, volumeTextFormat, brushBidFontOpacity);
				}
			}
			public void setCalculation(_OrderFlowEnums.Calculation orderFlowCalculation)
			{
				switch( orderFlowCalculation )
				{
					//case _OrderFlowEnums.Calculation.TotalBidAsk:
					case _OrderFlowEnums.Calculation.BidAsk:
					{
						this._renderOFText = this.__renderBidAskText;
						this.renderClusterRect = this.__renderBidAsk;
						break;
					}
					case _OrderFlowEnums.Calculation.TotalDelta:
					{
						this._renderOFText = this.__renderTotalDeltaText;
						this.renderClusterRect = this.__renderDelta;
						break;
					}
					case _OrderFlowEnums.Calculation.Total:
					{
						this._renderOFText = this.__renderTotalText;
						this.renderClusterRect = this.__renderTotal;
						break;
					}
					case _OrderFlowEnums.Calculation.Delta:
					{
						this._renderOFText = this.__renderDeltaText;
						this.renderClusterRect = this.__renderDelta;
						break;
					}
				}
				this.orderFlowCalculation = orderFlowCalculation;
			}
			public void setRepresentation(_OrderFlowEnums.Representation orderFlowRepresentation)
			{
				this.orderFlowRepresentation = orderFlowRepresentation;
			}
			
			#endregion
			
			public void setWyckoffBars(VolumeAnalysis.WyckoffBars wyckoffBars)
			{
				this.wyckoffBars = wyckoffBars;
			}
			public void setRealtime(bool isRealtime)
			{
				this.realTime = isRealtime;
			}
			public bool IsRealtime
			{
				get{ return this.realTime; }
			}
			// *- para la posicion del order flow, clusters y texto, si el resultado es negativo
			// se invierte la posicion
			private bool calculateXPosition(out float barXpos)
			{
				barXpos = 0;
				switch(orderFlowPosition)
				{
					case _OrderFlowEnums.Position.Right:
					{
						barXpos = (W / 4f);// - (BarW*4f);
						break;
					}
					case _OrderFlowEnums.Position.Left:
					{
						barXpos = -(W / 4f);
						return true;
					}
//					case _OrderFlowEnums.Position.Center:
//					{
//						barXpos = -(W / 4f);
//						break;
//					}
				}
				return false;
			}
			private float calculateXPositionFont()
			{
				if( orderFlowPosition == _OrderFlowEnums.Position.Left )//switch(orderFlowPosition)
				{
					return -W;
					// !- No necesitamos calcular la fuente
					//case _OrderFlowEnums.Position.Right:{ return 0; }
					//case _OrderFlowEnums.Position.Left:{ return -W; }
					//case _OrderFlowEnums.Position.Center:{ return -(W / 2f); }
				}
				return 0;
			}
			private float calculateClusterPOCPercent()
			{
				switch( this.orderFlowCalculation )
				{
//					case _OrderFlowEnums.Calculation.TotalBidAsk:
//					{
//						long vol;
//						if( this.volumeInfo.Delta >= 0 )
//							vol = this.volumeInfo.Ask;
//						else
//							vol = this.volumeInfo.Bid;
//						return (float)Math2.Percent(this.currentBar.MaxClusterVolume.Total, vol);
//					}
					case _OrderFlowEnums.Calculation.BidAsk:
					case _OrderFlowEnums.Calculation.Total:
					{
						// !- Obtenemos el porcentaje de volumen a partir del cluster maximo, en este punto
						// @maxClusterVolume representa el 100% y @vs.Total es el volumen en cada nivel
						// de precio, entonces si: maxClusterVolume == vs.Total el porcentaje sera 100%
						return (float)Math2.Percent(this.currentBar.MaxClusterVolume.Total, this.volumeInfo.Total);
					}
					case _OrderFlowEnums.Calculation.TotalDelta:
					{
						return (float)Math2.Percent(this.currentBar.MaxClusterVolume.Total, Math.Abs(this.volumeInfo.Delta));
					}
					case _OrderFlowEnums.Calculation.Delta:
					{
						return (float)Math2.Percent(Math.Abs(this.currentBar.MaxClusterVolume.Delta), Math.Abs(this.volumeInfo.Delta));
					}
				}
				return 0;
			}
			
			// !- Celda de footprint: un solo color plano (verde si el nivel tiene delta positivo,
			// rosa si negativo) con opacidad FIJA. En los footprints comerciales el fondo indica
			// direccion, no cantidad: la cantidad ya la dicen los numeros.
			private void __renderFootprintCell()
			{
				Rect.X = this.barX - (this.W / 2f);
				Rect.Width = this.W;   // Rect.Y y Rect.Height vienen de renderCluster()

				myFillRectangle(ref Rect,
					this.volumeInfo.Delta >= 0 ? colorFootprintUpCell : colorFootprintDownCell,
					this.footprintCellOpacity);

				if( this.footprintCellBorderWidth > 0f )
					myDrawRectangle(ref Rect, colorFootprintCellBorder, 1.0f, this.footprintCellBorderWidth);
			}
			private void __renderBidAsk(float clusterPOC_per, float opacity)
			{
				// !- el estilo Footprint pinta la celda entera de un color, no dos mitades
				if( this.orderFlowStyle == _OrderFlowEnums.Style.Footprint ){
					__renderFootprintCell();
					return;
				}
				bool invertSign = false;
				long vol = this.currentBar.MaxClusterVolume.Total;
				float bidPer = (float)Math2.Percent(vol, this.volumeInfo.Bid) / 100f;
				float askPer = (float)Math2.Percent(vol, this.volumeInfo.Ask) / 100f;
				this.Rect2.Y = this.Rect.Y;
				this.Rect2.Height = this.Rect.Height;
				
				// !- calculamos el estilo(profile, heatmap)
				switch( orderFlowStyle )
				{
					case _OrderFlowEnums.Style.Profile:
					{
						float bar_x;
						invertSign = this.calculateXPosition(out bar_x);
						
						Rect.X = barX + bar_x;//barX + (W / 4f);
						Rect2.X= Rect.X;
						
						// !- Anchura maxima: W / 2
						// el cual representa la anchura total de cada cluster, a partir de esto el
						// calculo (% * W) nos dara que cantidad de pixeles corresponde a cada cluster
						float width = W / 2f;
						float width_per, width_per2;
						
						width_per = (float)Math.Round(bidPer * width);
						width_per2= (float)Math.Round(askPer * width);
						if( invertSign ){
							width_per = -width_per;
							width_per2= -width_per2;
						}
						Rect.Width = width_per;
						Rect2.Width= width_per2;
						break;
					}
					case _OrderFlowEnums.Style.HeatMap:
					{
						Rect.X = barX - (W / 2f);
						Rect2.X= Rect.X;
						//float width = W; // 2f;
						//if( invertSign )
							//width = -width;
						Rect.Width = this.W;
						Rect2.Width= this.W;
						break;
					}
					case _OrderFlowEnums.Style.Footprint:
					{
						// !- celda de ancho FIJO: mitad izquierda = bid, mitad derecha = ask.
						// El ancho no varia con el volumen (eso ya lo dice el color), asi las
						// columnas quedan alineadas y el texto "B x A" siempre cabe.
						float half = this.W / 2f;
						Rect.X  = barX - half;
						Rect.Width = half;
						Rect2.X = barX;
						Rect2.Width= half;
						break;
					}
				}

				myFillRectangle(ref Rect, colorBidClusterColor, bidPer);
				myFillRectangle(ref Rect2,colorAskClusterColor, askPer);
			}
			private void __renderDelta(float clusterPOC_per, float opacity)
			{
				long D = this.volumeInfo.Delta;
				if( D == 0 )
					return;
				switch( this.orderFlowStyle )
				{
					case _OrderFlowEnums.Style.Profile:
					{
						float bar_x;
						bool invertSign = this.calculateXPosition(out bar_x);
						
						Rect.X = barX + bar_x;//barX + (W / 4f);
						// !- Anchura maxima: W / 2
						// el cual representa la anchura total de cada cluster, a partir de esto el
						// calculo (% * W) nos dara que cantidad de pixeles corresponde a cada cluster
						float width = W / 2f;
						float width_per = (float)Math.Round((clusterPOC_per * width) / 100);
						if( invertSign )
							width_per= -width_per;
						Rect.Width = width_per;
						break;
					}
					case _OrderFlowEnums.Style.Footprint:
					case _OrderFlowEnums.Style.HeatMap:
					{
						Rect.X = barX - (W / 2f);
						Rect.Width = W;
						break;
					}
				}
				if( D >= 0 )
					myFillRectangle(ref Rect, colorAskClusterColor, clustersOpacity);
				else
					myFillRectangle(ref Rect, colorBidClusterColor, clustersOpacity);
			}
			private void __renderTotal(float clusterPOC_per, float opacity)
			{
				switch( this.orderFlowStyle )
				{
					case _OrderFlowEnums.Style.Profile:
					{
						float bar_x;
						bool invertSign = this.calculateXPosition(out bar_x);
						
						Rect.X = barX + bar_x;//barX + (W / 4f);
						// !- Anchura maxima: W / 2
						// el cual representa la anchura total de cada cluster, a partir de esto el
						// calculo (% * W) nos dara que cantidad de pixeles corresponde a cada cluster
						float width = W / 2f;
						float width_per = (float)Math.Round((clusterPOC_per * width) / 100);
						if( invertSign )
							width_per= -width_per;
						
						Rect.Width = width_per;
						break;
					}
					case _OrderFlowEnums.Style.Footprint:
					case _OrderFlowEnums.Style.HeatMap:
					{
						Rect.X = barX - (W / 2f);
						Rect.Width = W;
						break;
					}
				}
				myFillRectangle(ref Rect, colorTotalClusterColor, opacity);
			}
			private void renderCluster()
			{
				if( !this.showOrderFlow ){
					return;
				}
				this.Rect.Y = barY - (this.H / 2f);
				this.Rect.Height = this.H;
				
				float clusterPOC_per = this.calculateClusterPOCPercent();
				float opacity = this.clustersOpacity == 0 ? 1.0f : (clusterPOC_per / 100f) * this.clustersOpacity;
				this.renderClusterRect(clusterPOC_per, opacity);
			}
			private void renderMinMaxCluster(double price)
			{
				Rect.Y = this.barY - (H / 2f);
				Rect.Height = H;
				
				switch( orderFlowStyle )
				{
					case _OrderFlowEnums.Style.Profile:{
						float bar_x;
						bool invertSign = this.calculateXPosition(out bar_x);
						Rect.X = barX + bar_x;
						float minmax_clus_width = W / 2f;
						if( invertSign )
							minmax_clus_width = -minmax_clus_width;
						Rect.Width = minmax_clus_width;
						
						break;
					}
					case _OrderFlowEnums.Style.Footprint:
					case _OrderFlowEnums.Style.HeatMap:{
						Rect.X = barX - (W / 2f);
						Rect.Width = W;
						
						break;
					}
				}
				
				// !- cluster maximo, es necesario que el precio actual coincida con el precio del cluster maximo
				// de otro modo si el volumen total es identico(se repite en la vela mas de una vez) el renderizado
				// mostratra mas de un cluster maximo confundiendo cual fue el ultimo maximo...
				if( this.showClusterPOC && Math2.Percent(currentBar.MaxClusterVolume.Total, volumeInfo.Total) == 100 && currentBar.MaxClusterPrice == price ){
					// !- relleno (amarillo, estilo footprint comercial) o solo el borde
					if( this.pocFilled )
						myFillRectangle(ref Rect, colorPOCFill, pocFillOpacity);
					else
						myDrawRectangle(ref Rect, colorMaxVolumeClusterColor, maxClusterOpacity, 1.5f);
				}
				// !- caja sobre niveles de volumen significativo (prints grandes)
				if( this.highlightVolumeThreshold > 0 && volumeInfo.Total >= this.highlightVolumeThreshold ){
					myDrawRectangle(ref Rect, colorHighlightBox, 1.0f, this.highlightBoxWidth);
				}
				// !- cluster minimo, misma logica
				if( this.showClusterPOI && Math2.Percent(currentBar.MinClusterVolume.Total, volumeInfo.Total) == 100 && currentBar.MinClusterPrice == price ){
					myFillRectangle(ref Rect, colorMinVolumeClusterColor, minClusterOpacity);
				}
			}
			private void renderText()
			{
				// !- renderizamos el texto
				if( this.showText && W >= minFontWidth && H >= minFontHeight )
				{
					switch( orderFlowStyle )
					{
						case _OrderFlowEnums.Style.Profile:
						{
							Rect.X = this.barX + this.calculateXPositionFont();
							break;
						}
						case _OrderFlowEnums.Style.Footprint:
						case _OrderFlowEnums.Style.HeatMap:
						{
							Rect.X = this.barX - (W / 2f);
							break;
						}
					}
					Rect.Y = this.barY - (H / 2f);
					Rect.Width = W;
					Rect.Height = H;
					
					// !- el estilo Footprint dibuja los dos numeros por separado (uno por mitad)
					// para poder colorear solo el lado con imbalance
					if( this.orderFlowStyle == _OrderFlowEnums.Style.Footprint
						&& this.orderFlowCalculation == _OrderFlowEnums.Calculation.BidAsk ){
						this.__renderFootprintCellText();
					}
					else{
						this._renderOFText();
					}
				}
			}
			// !- Renderizado para onRender
			// !- Texto de celda en estilo Footprint: bid en la mitad izquierda, ask en la derecha,
			// cada uno con su color. Si el nivel tiene imbalance se resalta SOLO el lado agresor,
			// que es como lo muestran los footprints comerciales.
			private void __renderFootprintCellText()
			{
				long B = this.volumeInfo.Bid;
				long A = this.volumeInfo.Ask;
				string C = "";
				if( this.orderFlowRepresentation == _OrderFlowEnums.Representation.Percent ){
					long total = this.volumeInfo.Total;
					B = (long)Math2.Percent(total, this.volumeInfo.Bid);
					A = (long)Math2.Percent(total, this.volumeInfo.Ask);
					C = "%";
				}
				// !- "115 x 39": bid pegado a la derecha de su mitad, la "x" al centro y el ask
				// pegado a la izquierda de la suya. Asi el texto queda centrado en la celda
				// y cada numero conserva su propio color.
				float half = this.W / 2f;
				const float GAP = 5f;   // media anchura de la zona de la "x"

				SharpDX.RectangleF lr = new SharpDX.RectangleF(
					this.barX - half, Rect.Y, half - GAP, Rect.Height);
				SharpDX.RectangleF cr = new SharpDX.RectangleF(
					this.barX - GAP, Rect.Y, GAP * 2f, Rect.Height);
				SharpDX.RectangleF rr = new SharpDX.RectangleF(
					this.barX + GAP, Rect.Y, half - GAP, Rect.Height);

				SharpDX.Color bidColor = (this.currentImbFlag < 0) ? colorImbalanceTextColor : colorFootprintText;
				SharpDX.Color askColor = (this.currentImbFlag > 0) ? colorImbalanceTextColor : colorFootprintText;

				myDrawText(B.ToString() + C, ref lr, bidColor, -1, -1, fpFormatRight, 1.0f);
				myDrawText("x", ref cr, colorFootprintText, -1, -1, fpFormatCenter, 0.55f);
				myDrawText(A.ToString() + C, ref rr, askColor, -1, -1, fpFormatLeft, 1.0f);
			}
			private void renderBarCluster(double price)
			{
				// !- el nivel actual, para que el texto sepa si hay imbalance y de que lado
				this.currentPrice = price;
				if( !this.imbByPrice.TryGetValue(price, out this.currentImbFlag) )
					this.currentImbFlag = 0;

				renderCluster();
				renderMinMaxCluster(price);
				renderImbalance(price);
				renderText();
			}
			private void renderLines(int barIndex)
			{
				if( !this.showPOCSLines && !this.showPOISLines )
					return;
				if( barIndex > 0 ){
					int prevBarIndex = barIndex - 1;
					// !- la barra anterior puede no existir (hueco de datos) => no dibujamos la linea
					VolumeAnalysis.WyckoffBars.Bar prevBar = this.wyckoffBars.At(prevBarIndex);
					if( prevBar == null || prevBar.MaxClusterVolume.Total == 0 )
						return;
					float prevBarX = CHART_CONTROL.GetXByBarIndex(CHART_BARS, prevBarIndex);
					
					beg_maxClusterVec.X = this.barX;
					end_maxClusterVec.X = prevBarX;
					
					if( this.orderFlowStyle == _OrderFlowEnums.Style.Profile )
					{
						switch( this.orderFlowPosition )
						{
							case _OrderFlowEnums.Position.Right:
							{
								float barW = W / 2f;
								beg_maxClusterVec.X += barW;//(W / 4f);
								end_maxClusterVec.X += barW;
								
								break;
							}
							case _OrderFlowEnums.Position.Left:
							{
								float barW = W / 2f;
								beg_maxClusterVec.X -= barW;
								end_maxClusterVec.X -= barW;
								break;
							}
							/* // podemos omitirlo es redundante
							case _OrderFlowEnums.Position.Center:
							{
								beg_maxClusterVec.X = this.barX;
								end_maxClusterVec.X = prevBarX;
								
								break;
							}*/
						}
						/* // podemos omitirlo es redundante
						case _OrderFlowEnums.Style.HeatMap:
						{
							break;
						}*/
					}
				
					// !- POC Lines
					if(this.showPOCSLines){
						beg_maxClusterVec.Y = CHART_SCALE.GetYByValue(currentBar.MaxClusterPrice);
						end_maxClusterVec.Y = CHART_SCALE.GetYByValue(prevBar.MaxClusterPrice);
						myDrawLine(ref beg_maxClusterVec, ref end_maxClusterVec,
							colorPOCLines, POCLinesOpacity,
							POCLines_strokeWidth, POCLines_strokeStyle);
					}
					// !- POI lines
					if(this.showPOISLines){
						beg_maxClusterVec.Y = CHART_SCALE.GetYByValue(currentBar.MinClusterPrice);
						end_maxClusterVec.Y = CHART_SCALE.GetYByValue(prevBar.MinClusterPrice);
						
						myDrawLine(ref beg_maxClusterVec, ref end_maxClusterVec,
							colorPOILines, POILinesOpacity,
							POILines_strokeWidth, POILines_strokeStyle);
					}
				}
			}
			// !- Escribe el delta TOTAL de la barra debajo del minimo de la vela.
			// No pasa por $myDrawText porque ese metodo exige H >= minFontHeight (altura de UN tick)
			// y aqui el texto es uno por barra, no uno por nivel de precio.
			private void renderBarDelta(int barIndex)
			{
				if( !this.showBarDelta || this.currentBar == null )
					return;
				// !- con barras muy estrechas los numeros se solapan: no dibujamos
				if( this.W < this.barDeltaMinBarWidth || this.barDeltaTextFormat == null )
					return;

				long D = this.currentBar.Delta;
				float lowY = CHART_SCALE.GetYByValue(CHART_BARS.Bars.GetLow(barIndex));

				SharpDX.RectangleF r = new SharpDX.RectangleF(
					this.barX - (this.W / 2f), lowY + 4f, this.W, 16f);

				SharpDX.Direct2D1.Brush dxBrush = new SharpDX.Direct2D1.SolidColorBrush(
					RENDER_TARGET, D >= 0 ? colorBarDeltaPositive : colorBarDeltaNegative);
				if( !dxBrush.IsDisposed ){
					dxBrush.Opacity = this.barDeltaOpacity;
					RENDER_TARGET.DrawText(D.ToString(), barDeltaTextFormat, r, dxBrush);
					dxBrush.Dispose();
				}
			}
			// !- redondeo al tick del instrumento: sumar/restar TickSize a un double NO da
			// exactamente la clave guardada en la escalera (7715.25 + 0.25 => 7715.500000000001),
			// asi que el precio vecino se normaliza antes de buscarlo.
			private double neighbourPrice(double price, int ticks)
			{
				return CHART_BARS.Bars.Instrument.MasterInstrument.RoundToTickSize(
					price + (ticks * CHART_BARS.Bars.Instrument.MasterInstrument.TickSize));
			}
			// !- Imbalance DIAGONAL, la definicion estandar de footprint:
			//    compra  => ask[P]  >= ratio * bid[P + 1 tick]
			//    venta   => bid[P]  >= ratio * ask[P - 1 tick]
			// Devuelve +1 compra, -1 venta, 0 ninguno.
			private int calculateImbalance(double price, VolumeAnalysis.MarketOrder vol)
			{
				if( !this.showImbalance || this.currentBar == null )
					return 0;

				// !- compra: agresion al ask contra el bid del nivel de ARRIBA
				if( vol.Ask >= this.imbalanceMinVolume ){
					VolumeAnalysis.MarketOrder up = this.currentBar.AtPrice(neighbourPrice(price, 1));
					long upBid = (up == null) ? 0 : up.Bid;
					if( (double)vol.Ask >= this.imbalanceRatio * upBid )
						return 1;
				}
				// !- venta: agresion al bid contra el ask del nivel de ABAJO
				if( vol.Bid >= this.imbalanceMinVolume ){
					VolumeAnalysis.MarketOrder dn = this.currentBar.AtPrice(neighbourPrice(price, -1));
					long dnAsk = (dn == null) ? 0 : dn.Ask;
					if( (double)vol.Bid >= this.imbalanceRatio * dnAsk )
						return -1;
				}
				return 0;
			}
			// !- pinta el borde de la celda cuando hay imbalance en ese nivel
			private void renderImbalance(double price)
			{
				if( !this.showImbalance )
					return;
				int flag;
				if( !this.imbByPrice.TryGetValue(price, out flag) || flag == 0 )
					return;

				Rect.Y = this.barY - (this.H / 2f);
				Rect.Height = this.H;
				switch( orderFlowStyle )
				{
					case _OrderFlowEnums.Style.Profile:{
						float bar_x;
						bool invertSign = this.calculateXPosition(out bar_x);
						Rect.X = this.barX + bar_x;
						float w = W / 2f;
						Rect.Width = invertSign ? -w : w;
						break;
					}
					case _OrderFlowEnums.Style.Footprint:
					case _OrderFlowEnums.Style.HeatMap:{
						Rect.X = this.barX - (W / 2f);
						Rect.Width = W;
						break;
					}
				}
				myDrawRectangle(ref Rect,
					flag > 0 ? colorImbalanceBuy : colorImbalanceSell,
					this.imbalanceOpacity, 2.0f);
			}
			// !- Precalculo por barra: marca cada nivel y dibuja las lineas de imbalance APILADO
			// (N niveles consecutivos - adyacentes en ticks - con imbalance en la misma direccion).
			private void prepareImbalances(int barIndex)
			{
				this.imbByPrice.Clear();
				this.imbPrices.Clear();
				this.imbFlags.Clear();
				if( !this.showImbalance || this.currentBar == null )
					return;

				// !- la escalera es un SortedDictionary: recorremos de menor a mayor precio
				foreach(var wb in this.currentBar){
					int f = calculateImbalance(wb.Key, wb.Value);
					this.imbByPrice[wb.Key] = f;
					this.imbPrices.Add(wb.Key);
					this.imbFlags.Add(f);
				}
				if( !this.showStackedImbalance || this.stackedImbalanceCount < 2 )
					return;

				double tick = CHART_BARS.Bars.Instrument.MasterInstrument.TickSize;
				double tol = tick / 2.0;
				int runStart = 0;
				for(int i = 1; i <= this.imbPrices.Count; i++)
				{
					bool sameRun = false;
					if( i < this.imbPrices.Count ){
						// !- mismo signo y niveles realmente contiguos (sin huecos de precio)
						sameRun = this.imbFlags[i] != 0
							&& this.imbFlags[i] == this.imbFlags[runStart]
							&& Math.Abs((this.imbPrices[i] - this.imbPrices[i - 1]) - tick) < tol;
					}
					if( !sameRun ){
						int runLen = i - runStart;
						if( this.imbFlags[runStart] != 0 && runLen >= this.stackedImbalanceCount ){
							renderStackedLine(barIndex, this.imbPrices[runStart],
								this.imbPrices[i - 1], this.imbFlags[runStart]);
						}
						runStart = i;
					}
				}
			}
			// !- linea horizontal que proyecta la zona de imbalance apilado hacia la derecha
			private void renderStackedLine(int barIndex, double priceFrom, double priceTo, int flag)
			{
				float x1 = this.barX;
				float x2 = CHART_CONTROL.GetXByBarIndex(CHART_BARS,
					Math.Min(barIndex + this.stackedImbalanceExtendBars, CHART_BARS.ToIndex));
				if( x2 <= x1 )
					x2 = x1 + (this.W * this.stackedImbalanceExtendBars);

				SharpDX.Color c = flag > 0 ? colorImbalanceBuy : colorImbalanceSell;

				SharpDX.Vector2 a = new SharpDX.Vector2();
				SharpDX.Vector2 b = new SharpDX.Vector2();
				a.X = x1; b.X = x2;
				// !- una linea en cada extremo de la zona apilada
				a.Y = b.Y = CHART_SCALE.GetYByValue(priceFrom);
				myDrawLine(ref a, ref b, c, this.imbalanceOpacity,
					this.stackedImbalance_strokeWidth, this.stackedImbalance_strokeStyle);
				a.Y = b.Y = CHART_SCALE.GetYByValue(priceTo);
				myDrawLine(ref a, ref b, c, this.imbalanceOpacity,
					this.stackedImbalance_strokeWidth, this.stackedImbalance_strokeStyle);
			}
			// !- una celda de la tabla resumen: fondo con intensidad proporcional al valor + numero
			private void renderSummaryCell(SharpDX.RectangleF rect, string text, SharpDX.Color bg, float bgOpacity)
			{
				if( bgOpacity < 0f ) bgOpacity = 0f;
				if( bgOpacity > 1f ) bgOpacity = 1f;
				myFillRectangle(ref rect, bg, bgOpacity);

				SharpDX.Direct2D1.Brush dxBrush = new SharpDX.Direct2D1.SolidColorBrush(RENDER_TARGET, colorSummaryText);
				if( !dxBrush.IsDisposed ){
					RENDER_TARGET.DrawText(text, summaryTextFormat, rect, dxBrush);
					dxBrush.Dispose();
				}
			}
			// !- Tabla resumen al pie: filas Ask / Bid / Delta / Volume, una columna por barra.
			// La intensidad del fondo es proporcional al maximo VISIBLE, no a un valor absoluto,
			// asi la lectura relativa se mantiene util en cualquier instrumento y timeframe.
			public void renderSummary(int fromIndex, int toIndex, float panelX, float panelY, float panelH)
			{
				if( !this.showSummary || this.wyckoffBars == null || this.summaryTextFormat == null )
					return;
				if( this.W < this.summaryMinBarWidth )
					return;

				float rowH = this.summaryRowHeight;
				float topY = (panelY + panelH) - (rowH * 4f);

				// 1- maximos del rango visible para escalar la intensidad del color
				long maxAsk = 1, maxBid = 1, maxDelta = 1, maxVol = 1;
				for(int i = fromIndex; i <= toIndex; i++){
					VolumeAnalysis.WyckoffBars.Bar b = this.wyckoffBars.At(i);
					if( b == null ) continue;
					if( b.Ask > maxAsk ) maxAsk = b.Ask;
					if( b.Bid > maxBid ) maxBid = b.Bid;
					long absD = Math.Abs(b.Delta);
					if( absD > maxDelta ) maxDelta = absD;
					if( b.Total > maxVol ) maxVol = b.Total;
				}

				// 2- una columna por barra
				for(int i = fromIndex; i <= toIndex; i++){
					VolumeAnalysis.WyckoffBars.Bar b = this.wyckoffBars.At(i);
					if( b == null ) continue;

					float x = CHART_CONTROL.GetXByBarIndex(CHART_BARS, i) - (this.W / 2f);
					SharpDX.RectangleF r = new SharpDX.RectangleF(x, topY, this.W, rowH);

					renderSummaryCell(r, b.Ask.ToString(), colorSummaryAsk,
						((float)b.Ask / maxAsk) * summaryOpacity);
					r.Y += rowH;
					renderSummaryCell(r, b.Bid.ToString(), colorSummaryBid,
						((float)b.Bid / maxBid) * summaryOpacity);
					r.Y += rowH;
					long D = b.Delta;
					renderSummaryCell(r, D.ToString(), D >= 0 ? colorSummaryAsk : colorSummaryBid,
						((float)Math.Abs(D) / maxDelta) * summaryOpacity);
					r.Y += rowH;
					renderSummaryCell(r, b.Total.ToString(), colorSummaryVolume,
						((float)b.Total / maxVol) * summaryOpacity);
				}

				// 3- etiquetas de fila, ancladas al borde izquierdo del panel
				string[] labels = new string[]{ "Ask", "Bid", "Delta", "Volume" };
				SharpDX.Color[] labelColors = new SharpDX.Color[]{
					colorSummaryAsk, colorSummaryBid, colorSummaryVolume, colorSummaryVolume };
				SharpDX.RectangleF lr = new SharpDX.RectangleF(panelX + 1f, topY, 54f, rowH);
				for(int k = 0; k < labels.Length; k++){
					renderSummaryCell(lr, labels[k], labelColors[k], 0.85f);
					lr.Y += rowH;
				}
			}
			public void renderBarClusters(int barIndex, bool realtimeCalculation)
			{
				// !- $At() devuelve NULL en lugar de lanzar KeyNotFoundException cuando la barra
				// no tiene ningun tick registrado (huecos de datos, arranque sin Tick Replay, etc.)
				this.currentBar = this.wyckoffBars.At(barIndex);
				if( this.currentBar == null )
					return;
				// !- Optimizamos para calculos en tiempo-real. El POC/POI tambien se recalcula si la
				// barra nunca fue cerrada (MaxClusterVolume == 0), de otro modo $Math2.Percent divide
				// entre cero y la opacidad de los clusters sale Infinity => nada visible.
				if( realtimeCalculation || this.currentBar.MaxClusterVolume.Total == 0 ){
					this.currentBar.CalculateMinAndMaxCluster();
				}
				if( this.currentBar.MaxClusterVolume.Total == 0 )
					return;
				this.barX = CHART_CONTROL.GetXByBarIndex(CHART_BARS, barIndex);
				// -- renderizamos las lineas de POCs y POIs si estas fueron calculadas
				renderLines(barIndex);
				// -- delta total de la barra, debajo del minimo
				renderBarDelta(barIndex);
				// -- marcamos los niveles con imbalance y dibujamos los apilados
				prepareImbalances(barIndex);
				
				double price;
				foreach(var wb in currentBar){
					price = wb.Key;
					this.barY = CHART_SCALE.GetYByValue(price);
					// !- informacion de volumen
					this.volumeInfo = wb.Value;
					// !- renderizamos el cluster precio a precio
					renderBarCluster(price);
				}
			}
		}
		
		#endregion
		#region GLOBAL_VARIABLES
		
		private VolumeAnalysis.WyckoffBars wyckoffBars;
		private WyckoffOrderFlow wyckoffOF;
		// !- true cuando la serie de datos tiene "Tick Replay" activado
		private bool isTickReplay;

		#endregion
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				wyckoffOF = new WyckoffOrderFlow();
				
				Description									= @"";
				Name										= "Order Flow";
				Calculate									= Calculate.OnEachTick;
				IsOverlay									= true;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= false;
				// !- necesitamos dibujar desde la primera barra cargada
				BarsRequiredToPlot							= 0;
				isTickReplay								= false;

				// !- Setup de estilo
				_TextFont = new SimpleFont();
				_TextFont.Family = new FontFamily("Arial");
				_TextFont.Size = 10f;
				_TextFont.Bold = false;
				_TextFont.Italic= false;

				// !- $_MinFontHeight es la ALTURA EN PIXELES DE UN TICK necesaria para dibujar los
				// numeros. Con el valor antiguo(10) un grafico de 5 min en 6E (1 tick ~ 2-3 px)
				// nunca llegaba al umbral y el footprint salia sin numeros. 1 = dibujar siempre;
				// subelo si quieres que los numeros se oculten al alejar el zoom.
				_MinFontWidth = 1f; _MinFontHeight= 1f;
				_BidTextVolumeColor = Brushes.LightCoral;
				_BidTextOpacity = 100f;
				_AskTextVolumeColor = Brushes.DarkSeaGreen;
				_AskTextOpacity = 100f;
				_TotalTextVolumeColor = Brushes.Beige;
				_TotalTextOpacity = 80f;
				
				_AskClusterVolumeColor = Brushes.SeaGreen;
				_BidClusterVolumeColor = Brushes.Red;
				_TotalClusterVolumeColor = Brushes.LightSkyBlue;
				_MaxClusterVolumeColor = Brushes.PowderBlue;
				_MaxClusterOpacity = 60f;
				_MinClusterVolumeColor = Brushes.Violet;
				_MinClusterOpacity = 20f;
				// !- Por defecto tiene un 70% de opacidad cada nivel de cluster
				_ClustersOpacity = 70f;
				
				// !- Estilo de lineas POCs y POIs
				_POCLinesColor = Brushes.WhiteSmoke;
				_POCLinesOpacity = 70f;
				_POCLines_strokeWidth = 1.0f;
				_POCLinesStrokeStyle = DashStyleHelper.Solid;//SharpDX.Direct2D1.DashStyle.Solid;
				_POILinesColor = Brushes.Violet;
				_POILinesOpacity = 40f;
				_POILines_strokeWidth = 1.0f;
				_POILinesStrokeStyle = DashStyleHelper.Dash;//SharpDX.Direct2D1.DashStyle.Dash;
				
				// !- Calculos del order flow por defecto
				_OrderFlowCalculation = _OrderFlowEnums.Calculation.BidAsk;
				_OrderFlowRepresentation = _OrderFlowEnums.Representation.Volume;
				_OrderFlowPosition = _OrderFlowEnums.Position.Right;
				// !- El original no asignaba $_OrderFlowStyle, asi que caia en el valor 0 del
				// enum (Profile). Por defecto usamos Footprint: celdas de ancho fijo "B x A",
				// que es el aspecto de footprint que se espera hoy en dia.
				_OrderFlowStyle = _OrderFlowEnums.Style.Footprint;
				
				_ShowClusterPOC = true;
				_ShowPOCSLines = false;
				_ShowClusterPOI = false;
				_ShowPOISLines = false;
				_ShowText = true;
				_ShowOrderFlow = true;

				// !- Delta total por barra (el numero bajo cada vela)
				_ShowBarDelta = true;
				_BarDeltaPositiveColor = Brushes.SeaGreen;
				_BarDeltaNegativeColor = Brushes.Firebrick;
				_BarDeltaOpacity = 100f;
				_BarDeltaMinBarWidth = 14f;
				_BarDeltaFont = new SimpleFont();
				_BarDeltaFont.Family = new FontFamily("Arial");
				_BarDeltaFont.Size = 11f;
				_BarDeltaFont.Bold = true;
				_BarDeltaFont.Italic = false;

				// !- Tabla resumen al pie (Ask / Bid / Delta / Volume)
				_ShowSummary = true;
				_SummaryRowHeight = 13f;
				_SummaryMinBarWidth = 14f;
				_SummaryOpacity = 85f;
				_SummaryAskColor = Brushes.SeaGreen;
				_SummaryBidColor = Brushes.Firebrick;
				_SummaryVolumeColor = Brushes.SteelBlue;
				_SummaryTextColor = Brushes.WhiteSmoke;
				_SummaryFont = new SimpleFont();
				_SummaryFont.Family = new FontFamily("Arial");
				_SummaryFont.Size = 10f;
				_SummaryFont.Bold = false;
				_SummaryFont.Italic = false;

				// !- Imbalances (diagonal) e imbalances apilados
				_ShowImbalance = true;
				_ImbalanceRatio = 3.0f;
				_ImbalanceMinVolume = 10;
				_ImbalanceOpacity = 90f;
				_ImbalanceBuyColor = Brushes.Lime;
				_ImbalanceSellColor = Brushes.OrangeRed;
				_ShowStackedImbalance = true;
				_StackedImbalanceCount = 3;
				_StackedImbalanceExtendBars = 12;
				_StackedImbalance_strokeWidth = 1.5f;
				_StackedImbalanceStrokeStyle = DashStyleHelper.Dot;

				// !- Estilo Footprint (celda de ancho fijo). Los valores por defecto estan
				// pensados para un grafico de FONDO BLANCO, como los footprints comerciales.
				_ImbalanceTextColor = Brushes.Blue;
				_ClusterPOCFilled = true;
				_POCFillColor = Brushes.Gold;
				_POCFillOpacity = 90f;
				_HighlightVolumeThreshold = 0;      // 0 = desactivado
				_HighlightBoxColor = Brushes.Black;
				_HighlightBoxWidth = 1.5f;

				// !- celda footprint, valores pensados para FONDO BLANCO
				_FootprintUpCellColor = Brushes.LightGreen;
				_FootprintDownCellColor = Brushes.LightPink;
				_FootprintCellOpacity = 55f;
				_FootprintCellBorderColor = Brushes.Gainsboro;
				_FootprintCellBorderWidth = 0f;     // 0 = sin borde
				_FootprintTextColor = Brushes.Black;
				_FootprintFont = new SimpleFont();
				_FootprintFont.Family = new FontFamily("Arial");
				_FootprintFont.Size = 10f;
				_FootprintFont.Bold = false;
				_FootprintFont.Italic = false;
				// !- calculamos la ultima barra creada en tiempo real/mercado (?)
				_RealtimeHeuristic = true;
			}
			else if (State == State.Configure)
			{
				wyckoffOF.setFontStyle(_TextFont);
				wyckoffOF.setCalculation(_OrderFlowCalculation);
				wyckoffOF.setRepresentation(_OrderFlowRepresentation);
				wyckoffOF.setPosition(_OrderFlowPosition);
				wyckoffOF.setStyle(_OrderFlowStyle);
				wyckoffOF.setBidAskClusterColor(_BidClusterVolumeColor, _AskClusterVolumeColor);
				wyckoffOF.setBidAskFontColor(_BidTextVolumeColor, _BidTextOpacity,
					_AskTextVolumeColor, _AskTextOpacity);
				wyckoffOF.setTotalFontColor(_TotalClusterVolumeColor, _TotalTextVolumeColor, _TotalTextOpacity);
				wyckoffOF.setMaxMinVolumeClusterColor(_MaxClusterVolumeColor, _MaxClusterOpacity,
					_MinClusterVolumeColor, _MinClusterOpacity);
				wyckoffOF.setPOCPOILines(
					_POCLinesColor, _POCLines_strokeWidth, _POCLinesStrokeStyle, _POCLinesOpacity,
					_POILinesColor, _POILines_strokeWidth, _POILinesStrokeStyle, _POILinesOpacity);
				wyckoffOF.setMinSizeFont(_MinFontWidth, _MinFontHeight);
				wyckoffOF.setClustersOpacity(_ClustersOpacity);
				wyckoffOF.setShows(_ShowClusterPOC, _ShowClusterPOI,
				_ShowPOCSLines, _ShowPOISLines,
				_ShowText, _ShowOrderFlow);
				// !- un chart template guardado ANTES de existir estas propiedades las deserializa
				// como null/0; rellenamos para no romper $setFontStyle ni $BrushToColor.
				if( _BarDeltaPositiveColor == null ) _BarDeltaPositiveColor = Brushes.SeaGreen;
				if( _BarDeltaNegativeColor == null ) _BarDeltaNegativeColor = Brushes.Firebrick;
				if( _BarDeltaOpacity <= 0f ) _BarDeltaOpacity = 100f;
				if( _BarDeltaMinBarWidth <= 0f ) _BarDeltaMinBarWidth = 14f;
				if( _BarDeltaFont == null ){
					_BarDeltaFont = new SimpleFont();
					_BarDeltaFont.Family = new FontFamily("Arial");
					_BarDeltaFont.Size = 11f;
					_BarDeltaFont.Bold = true;
					_BarDeltaFont.Italic = false;
				}
				wyckoffOF.setBarDelta(_ShowBarDelta, _BarDeltaPositiveColor, _BarDeltaNegativeColor,
					_BarDeltaOpacity, _BarDeltaMinBarWidth, _BarDeltaFont);

				// !- mismos guardas para la tabla resumen (templates guardados antes de existir)
				if( _SummaryAskColor == null )    _SummaryAskColor = Brushes.SeaGreen;
				if( _SummaryBidColor == null )    _SummaryBidColor = Brushes.Firebrick;
				if( _SummaryVolumeColor == null ) _SummaryVolumeColor = Brushes.SteelBlue;
				if( _SummaryTextColor == null )   _SummaryTextColor = Brushes.WhiteSmoke;
				if( _SummaryRowHeight <= 0f )     _SummaryRowHeight = 13f;
				if( _SummaryMinBarWidth <= 0f )   _SummaryMinBarWidth = 14f;
				if( _SummaryOpacity <= 0f )       _SummaryOpacity = 85f;
				if( _SummaryFont == null ){
					_SummaryFont = new SimpleFont();
					_SummaryFont.Family = new FontFamily("Arial");
					_SummaryFont.Size = 10f;
					_SummaryFont.Bold = false;
					_SummaryFont.Italic = false;
				}
				wyckoffOF.setSummary(_ShowSummary, _SummaryRowHeight, _SummaryMinBarWidth, _SummaryOpacity,
					_SummaryAskColor, _SummaryBidColor, _SummaryVolumeColor, _SummaryTextColor, _SummaryFont);

				// !- guardas para imbalances
				if( _ImbalanceBuyColor == null )  _ImbalanceBuyColor = Brushes.Lime;
				if( _ImbalanceSellColor == null ) _ImbalanceSellColor = Brushes.OrangeRed;
				if( _ImbalanceRatio < 1f )        _ImbalanceRatio = 3.0f;
				if( _ImbalanceOpacity <= 0f )     _ImbalanceOpacity = 90f;
				if( _StackedImbalanceCount < 2 )  _StackedImbalanceCount = 3;
				if( _StackedImbalanceExtendBars < 1 ) _StackedImbalanceExtendBars = 12;
				if( _StackedImbalance_strokeWidth <= 0f ) _StackedImbalance_strokeWidth = 1.5f;
				// !- guardas + wiring del estilo Footprint
				if( _ImbalanceTextColor == null ) _ImbalanceTextColor = Brushes.Blue;
				if( _POCFillColor == null )       _POCFillColor = Brushes.Gold;
				if( _HighlightBoxColor == null )  _HighlightBoxColor = Brushes.Black;
				if( _POCFillOpacity <= 0f )       _POCFillOpacity = 90f;
				if( _HighlightBoxWidth <= 0f )    _HighlightBoxWidth = 1.5f;
				if( _FootprintUpCellColor == null )     _FootprintUpCellColor = Brushes.LightGreen;
				if( _FootprintDownCellColor == null )   _FootprintDownCellColor = Brushes.LightPink;
				if( _FootprintCellBorderColor == null ) _FootprintCellBorderColor = Brushes.Gainsboro;
				if( _FootprintTextColor == null )       _FootprintTextColor = Brushes.Black;
				if( _FootprintCellOpacity <= 0f )       _FootprintCellOpacity = 55f;
				if( _FootprintFont == null ){
					_FootprintFont = new SimpleFont();
					_FootprintFont.Family = new FontFamily("Arial");
					_FootprintFont.Size = 10f;
					_FootprintFont.Bold = false;
					_FootprintFont.Italic = false;
				}
				wyckoffOF.setFootprintStyle(_ImbalanceTextColor,
					_ClusterPOCFilled, _POCFillColor, _POCFillOpacity,
					_HighlightVolumeThreshold, _HighlightBoxColor, _HighlightBoxWidth,
					_FootprintUpCellColor, _FootprintDownCellColor, _FootprintCellOpacity,
					_FootprintCellBorderColor, _FootprintCellBorderWidth,
					_FootprintTextColor, _FootprintFont);

				wyckoffOF.setImbalance(_ShowImbalance, _ImbalanceRatio, _ImbalanceMinVolume, _ImbalanceOpacity,
					_ImbalanceBuyColor, _ImbalanceSellColor,
					_ShowStackedImbalance, _StackedImbalanceCount, _StackedImbalanceExtendBars,
					_StackedImbalance_strokeWidth, _StackedImbalanceStrokeStyle);
				
				Calculate= Calculate.OnEachTick;
			}
			else if(State == State.DataLoaded)
			{
				wyckoffBars = new VolumeAnalysis.WyckoffBars(Bars);
				wyckoffOF.setWyckoffBars(wyckoffBars);
				// !- Sin Tick Replay NO existen eventos $OnMarketData para las barras historicas,
				// por lo tanto el order flow quedaria completamente vacio. Lo detectamos aqui para
				// poder avisar en pantalla en lugar de dibujar un grafico en blanco sin explicacion.
				// !- $IsTickReplays es un bool?[] (nullable), por eso comparamos con "== true"
				isTickReplay = IsTickReplays != null && IsTickReplays.Length > 0 && IsTickReplays[0] == true;
			}
			else if(State == State.Realtime)
			{
				wyckoffOF.setRealtime(true);
			}
			else if(State == State.Terminated)
			{
				if( ChartControl != null )
					ChartControl.Properties.BarMarginRight = 0;
			}
		}
		// !- Aviso en pantalla cuando el Tick Replay esta desactivado
		private void renderTickReplayWarning(ChartControl chartControl)
		{
			string msg = "Order Flow: Tick Replay is OFF - no historical order flow data."
				+ Environment.NewLine
				+ "Right-click chart > Data Series > set 'Tick Replay' = True"
				+ Environment.NewLine
				+ "(needs Tools > Options > Market Data > 'Show Tick Replay' and downloaded tick data)";

			using(SharpDX.DirectWrite.TextFormat tf = new SharpDX.DirectWrite.TextFormat(
					NinjaTrader.Core.Globals.DirectWriteFactory, "Arial", 13f))
			using(SharpDX.Direct2D1.SolidColorBrush brush =
					new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, SharpDX.Color.Orange))
			{
				SharpDX.RectangleF r = new SharpDX.RectangleF(
					ChartPanel.X + 12, ChartPanel.Y + 12, ChartPanel.W - 24, 70);
				RenderTarget.DrawText(msg, tf, r, brush);
			}
		}
		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			base.OnRender(chartControl, chartScale);
			// !- $IsInHitTest es un bool: la comparacion original "== null" era siempre false y
			// nunca filtraba nada. Ademas quitamos el gate "!IsRealtime": con Tick Replay las
			// barras historicas ya tienen clusters y deben dibujarse aunque no haya feed en vivo.
			if( IsInHitTest || chartControl == null || chartScale == null ||
				ChartBars == null || ChartBars.Bars == null ||
				wyckoffBars == null || RenderTarget == null ){
				return;
			}
			if( !isTickReplay ){
				renderTickReplayWarning(chartControl);
			}

			#region RENDER_ORDER_FLOW

			float W = chartControl.Properties.BarDistance;

			// !- Escribir en $BarMarginRight en cada render dispara un PropertyChanged y provoca
			// un bucle de repintado; solo lo tocamos cuando el valor realmente cambia.
			int wantedMargin = ChartControl.Properties.BarMarginRight;
			if( this._OrderFlowStyle == _OrderFlowEnums.Style.HeatMap ){
				wantedMargin = (int)(W / 4);
			}
			if( this._OrderFlowPosition == _OrderFlowEnums.Position.Right ){
				wantedMargin = (int)(W / 1.5);
			}
			if( ChartControl.Properties.BarMarginRight != wantedMargin ){
				ChartControl.Properties.BarMarginRight = wantedMargin;
			}

			//renderOF.BarW = (float)chartControl.BarWidth;
			// 1- Altura minima de un tick
			// 2- Ancho de barra en barra
			wyckoffOF.setHW(chartScale.GetPixelsForDistance(TickSize), W);
			// !- Apuntamos al target de renderizado
			wyckoffOF.setRenderTarget(chartControl, chartScale, ChartBars, RenderTarget);

			int fromIndex = ChartBars.FromIndex;
			int toIndex = ChartBars.ToIndex;
			// !- rango visible completo (incluida la barra en construccion) para la tabla resumen
			int summaryToIndex = Math.Min(toIndex, wyckoffBars.CurrentBarIndex);
			// ?- es la ultima barra generada (en construccion): se recalcula en cada render
			if( toIndex >= wyckoffBars.CurrentBarIndex )
			{
				toIndex = wyckoffBars.CurrentBarIndex;
				if( _RealtimeHeuristic ){
					wyckoffOF.renderBarClusters(toIndex, true);
				}
				// -- no cargamos la ultima barra en creacion
				toIndex--;
			}

			for(int barIndex = fromIndex; barIndex <= toIndex; barIndex++){
				wyckoffOF.renderBarClusters(barIndex, false);
			}
			// !- tabla resumen Ask/Bid/Delta/Volume al pie del panel
			wyckoffOF.renderSummary(fromIndex, summaryToIndex, ChartPanel.X, ChartPanel.Y, ChartPanel.H);

			#endregion
		}
		protected override void OnMarketData(MarketDataEventArgs MarketArgs){
			if( wyckoffBars == null || CurrentBar < 0 )
				return;
			// !- Indexamos por el CurrentBar REAL de NinjaTrader para que los clusters queden
			// alineados con las barras del grafico en historico(Tick Replay) y en tiempo real.
			wyckoffBars.onMarketData(MarketArgs, CurrentBar);
		}
		
		#region Properties
		
		[NinjaScriptProperty]
		[Display(Name="Formula", Order=0, GroupName="Order flow calculation")]
		public _OrderFlowEnums.Calculation _OrderFlowCalculation
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Representation", Order=1, GroupName="Order flow calculation")]
		public _OrderFlowEnums.Representation _OrderFlowRepresentation
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Position(Profile)", Order=2, GroupName="Order flow calculation")]
		public _OrderFlowEnums.Position _OrderFlowPosition
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Style", Order=3, GroupName="Order flow calculation")]
		public _OrderFlowEnums.Style _OrderFlowStyle
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Realtime heuristic", Order=4, GroupName="Order flow calculation")]
		public bool _RealtimeHeuristic
		{ get; set; }
		
		[XmlIgnore]
		[Display(Name="Ask clusters", Order=0, GroupName="Order flow style")]
		public Brush _AskClusterVolumeColor
		{ get; set; }
		[Browsable(false)]
		public string _AskClusterVolumeColorSerializable
		{
			get { return Serialize.BrushToString(_AskClusterVolumeColor); }
			set { _AskClusterVolumeColor = Serialize.StringToBrush(value); }
		}
		[XmlIgnore]
		[Display(Name="Bid clusters", Order=1, GroupName="Order flow style")]
		public Brush _BidClusterVolumeColor
		{ get; set; }
		[Browsable(false)]
		public string _BidClusterVolumeColorSerializable
		{
			get { return Serialize.BrushToString(_BidClusterVolumeColor); }
			set { _BidClusterVolumeColor = Serialize.StringToBrush(value); }
		}
		[XmlIgnore]
		[Display(Name="Total clusters", Order=2, GroupName="Order flow style")]
		public Brush _TotalClusterVolumeColor
		{ get; set; }
		[Browsable(false)]
		public string _TotalClusterVolumeColorSerializable
		{
			get { return Serialize.BrushToString(_TotalClusterVolumeColor); }
			set { _TotalClusterVolumeColor = Serialize.StringToBrush(value); }
		}
		
		[XmlIgnore]
		[Display(Name="Max cluster", Order=3, GroupName="Order flow style")]
		public Brush _MaxClusterVolumeColor
		{ get; set; }
		[Browsable(false)]
		public string _MaxClusterVolumeColorSerializable
		{
			get { return Serialize.BrushToString(_MaxClusterVolumeColor); }
			set { _MaxClusterVolumeColor = Serialize.StringToBrush(value); }
		}
		
		[XmlIgnore]
		[Display(Name="Min cluster", Order=4, GroupName="Order flow style")]
		public Brush _MinClusterVolumeColor
		{ get; set; }
		[Browsable(false)]
		public string _MinClusterVolumeColorSerializable
		{
			get { return Serialize.BrushToString(_MinClusterVolumeColor); }
			set { _MinClusterVolumeColor = Serialize.StringToBrush(value); }
		}
		
		[NinjaScriptProperty]
		[Range(0.0f, 100f)]
		[Display(Name="Max cluster opacity", Order=5, GroupName="Order flow style")]
		public float _MaxClusterOpacity
		{ get; set; }
		[NinjaScriptProperty]
		[Range(0.0f, 100f)]
		[Display(Name="Min cluster opacity", Order=6, GroupName="Order flow style")]
		public float _MinClusterOpacity
		{ get; set; }
		[NinjaScriptProperty]
		[Range(0.0f, 100f)]
		[Display(Name="Clusters opacity", Order=7, GroupName="Order flow style")]
		public float _ClustersOpacity
		{ get; set; }
		
		[XmlIgnore]
		[Display(Name="POC lines", Order=8, GroupName="Order flow style")]
		public Brush _POCLinesColor
		{ get; set; }
		[Browsable(false)]
		public string _POCLinesColorSerializable
		{
			get { return Serialize.BrushToString(_POCLinesColor); }
			set { _POCLinesColor = Serialize.StringToBrush(value); }
		}
		[XmlIgnore]
		[Display(Name="POI lines", Order=9, GroupName="Order flow style")]
		public Brush _POILinesColor
		{ get; set; }
		[Browsable(false)]
		public string _POILinesColorSerializable
		{
			get { return Serialize.BrushToString(_POILinesColor); }
			set { _POILinesColor = Serialize.StringToBrush(value); }
		}
		[NinjaScriptProperty]
		[Range(0.5f, 10f)]
		[Display(Name="POC lines width", Order=10, GroupName="Order flow style")]
		public float _POCLines_strokeWidth
		{ get; set; }
		[NinjaScriptProperty]
		[Display(Name="POC lines style", Order=11, GroupName="Order flow style")]
		public DashStyleHelper _POCLinesStrokeStyle
		{ get; set; }
		[NinjaScriptProperty]
		[Range(0.0f, 100f)]
		[Display(Name="POC lines opacity", Order=12, GroupName="Order flow style")]
		public float _POCLinesOpacity
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0.5f, 10f)]
		[Display(Name="POI lines width", Order=13, GroupName="Order flow style")]
		public float _POILines_strokeWidth
		{ get; set; }
		[NinjaScriptProperty]
		[Display(Name="POI lines style", Order=14, GroupName="Order flow style")]
		public DashStyleHelper _POILinesStrokeStyle
		{ get; set; }
		[NinjaScriptProperty]
		[Range(0.0f, 100f)]
		[Display(Name="POI lines opacity", Order=15, GroupName="Order flow style")]
		public float _POILinesOpacity
		{ get; set; }
		
		[XmlIgnore]
		[Display(Name="Ask text", Order=16, GroupName="Order flow style")]
		public Brush _AskTextVolumeColor
		{ get; set; }
		[Browsable(false)]
		public string _AskTextVolumeColorSerializable
		{
			get { return Serialize.BrushToString(_AskTextVolumeColor); }
			set { _AskTextVolumeColor = Serialize.StringToBrush(value); }
		}
		[NinjaScriptProperty]
		[Range(0.0f, 100f)]
		[Display(Name="Ask text opacity", Order=17, GroupName="Order flow style")]
		public float _AskTextOpacity
		{ get; set; }
		
		[XmlIgnore]
		[Display(Name="Bid text", Order=18, GroupName="Order flow style")]
		public Brush _BidTextVolumeColor
		{ get; set; }
		[Browsable(false)]
		public string _BidTextVolumeColorSerializable
		{
			get { return Serialize.BrushToString(_BidTextVolumeColor); }
			set { _BidTextVolumeColor = Serialize.StringToBrush(value); }
		}
		[NinjaScriptProperty]
		[Range(0.0f, 100f)]
		[Display(Name="Bid text opacity", Order=19, GroupName="Order flow style")]
		public float _BidTextOpacity
		{ get; set; }
		
		[XmlIgnore]
		[Display(Name="Total text", Order=20, GroupName="Order flow style")]
		public Brush _TotalTextVolumeColor
		{ get; set; }
		[Browsable(false)]
		public string _TotalTextVolumeColorColorSerializable
		{
			get { return Serialize.BrushToString(_TotalTextVolumeColor); }
			set { _TotalTextVolumeColor = Serialize.StringToBrush(value); }
		}
		[NinjaScriptProperty]
		[Range(0.0f, 100f)]
		[Display(Name="Total text opacity", Order=21, GroupName="Order flow style")]
		public float _TotalTextOpacity
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Font", Order=22, GroupName="Order flow style")]
		public SimpleFont _TextFont
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1.0f, float.MaxValue)]
		[Display(Name="Min font width", Order=23, GroupName="Order flow style")]
		public float _MinFontWidth
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1.0f, float.MaxValue)]
		[Display(Name="Min font height", Order=24, GroupName="Order flow style")]
		public float _MinFontHeight
		{ get; set; }
		
		
		[NinjaScriptProperty]
		[Display(Name="Cluster POC", Order=0, GroupName="Order flow show")]
		public bool _ShowClusterPOC
		{ get; set; }
		[NinjaScriptProperty]
		[Display(Name="POC lines", Order=1, GroupName="Order flow show")]
		public bool _ShowPOCSLines
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Cluster POI", Order=2, GroupName="Order flow show")]
		public bool _ShowClusterPOI
		{ get; set; }
		[NinjaScriptProperty]
		[Display(Name="POI lines", Order=3, GroupName="Order flow show")]
		public bool _ShowPOISLines
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Text", Order=4, GroupName="Order flow show")]
		public bool _ShowText
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Order flow", Order=5, GroupName="Order flow show")]
		public bool _ShowOrderFlow
		{ get; set; }

		// !- Delta total por barra. Sin [NinjaScriptProperty] a proposito: asi NO entra en la
		// firma del constructor autogenerado y no rompe nada que ya referencie el indicador.
		// Igualmente aparece en el dialogo de propiedades y se serializa en el chart template.
		[Display(Name="Bar delta (number under bar)", Order=6, GroupName="Order flow show")]
		public bool _ShowBarDelta
		{ get; set; }

		[XmlIgnore]
		[Display(Name="Bar delta positive", Order=10, GroupName="Order flow style")]
		public Brush _BarDeltaPositiveColor
		{ get; set; }
		[Browsable(false)]
		public string _BarDeltaPositiveColorSerializable
		{
			get { return Serialize.BrushToString(_BarDeltaPositiveColor); }
			set { _BarDeltaPositiveColor = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name="Bar delta negative", Order=11, GroupName="Order flow style")]
		public Brush _BarDeltaNegativeColor
		{ get; set; }
		[Browsable(false)]
		public string _BarDeltaNegativeColorSerializable
		{
			get { return Serialize.BrushToString(_BarDeltaNegativeColor); }
			set { _BarDeltaNegativeColor = Serialize.StringToBrush(value); }
		}

		[Range(1, 100)]
		[Display(Name="Bar delta opacity", Order=12, GroupName="Order flow style")]
		public float _BarDeltaOpacity
		{ get; set; }

		// !- ancho minimo de barra (px) para dibujar el delta; evita solapamiento al alejar zoom
		[Range(1, 200)]
		[Display(Name="Bar delta min bar width", Order=13, GroupName="Order flow style")]
		public float _BarDeltaMinBarWidth
		{ get; set; }

		[Display(Name="Bar delta font", Order=14, GroupName="Order flow style")]
		public SimpleFont _BarDeltaFont
		{ get; set; }

		// !- Tabla resumen Ask/Bid/Delta/Volume al pie del panel
		[Display(Name="Summary rows (Ask/Bid/Delta/Volume)", Order=7, GroupName="Order flow show")]
		public bool _ShowSummary
		{ get; set; }

		[Range(6, 40)]
		[Display(Name="Summary row height", Order=20, GroupName="Order flow summary")]
		public float _SummaryRowHeight
		{ get; set; }

		[Range(1, 200)]
		[Display(Name="Summary min bar width", Order=21, GroupName="Order flow summary")]
		public float _SummaryMinBarWidth
		{ get; set; }

		[Range(1, 100)]
		[Display(Name="Summary opacity", Order=22, GroupName="Order flow summary")]
		public float _SummaryOpacity
		{ get; set; }

		[XmlIgnore]
		[Display(Name="Summary ask", Order=23, GroupName="Order flow summary")]
		public Brush _SummaryAskColor
		{ get; set; }
		[Browsable(false)]
		public string _SummaryAskColorSerializable
		{
			get { return Serialize.BrushToString(_SummaryAskColor); }
			set { _SummaryAskColor = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name="Summary bid", Order=24, GroupName="Order flow summary")]
		public Brush _SummaryBidColor
		{ get; set; }
		[Browsable(false)]
		public string _SummaryBidColorSerializable
		{
			get { return Serialize.BrushToString(_SummaryBidColor); }
			set { _SummaryBidColor = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name="Summary volume", Order=25, GroupName="Order flow summary")]
		public Brush _SummaryVolumeColor
		{ get; set; }
		[Browsable(false)]
		public string _SummaryVolumeColorSerializable
		{
			get { return Serialize.BrushToString(_SummaryVolumeColor); }
			set { _SummaryVolumeColor = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name="Summary text", Order=26, GroupName="Order flow summary")]
		public Brush _SummaryTextColor
		{ get; set; }
		[Browsable(false)]
		public string _SummaryTextColorSerializable
		{
			get { return Serialize.BrushToString(_SummaryTextColor); }
			set { _SummaryTextColor = Serialize.StringToBrush(value); }
		}

		[Display(Name="Summary font", Order=27, GroupName="Order flow summary")]
		public SimpleFont _SummaryFont
		{ get; set; }

		// !- Imbalances
		[Display(Name="Imbalances", Order=8, GroupName="Order flow show")]
		public bool _ShowImbalance
		{ get; set; }

		[Range(1.1, 20.0)]
		[Display(Name="Imbalance ratio (x:1)", Order=30, GroupName="Order flow imbalance")]
		public float _ImbalanceRatio
		{ get; set; }

		[Range(0, 100000)]
		[Display(Name="Imbalance min volume", Order=31, GroupName="Order flow imbalance")]
		public long _ImbalanceMinVolume
		{ get; set; }

		[Range(1, 100)]
		[Display(Name="Imbalance opacity", Order=32, GroupName="Order flow imbalance")]
		public float _ImbalanceOpacity
		{ get; set; }

		[XmlIgnore]
		[Display(Name="Imbalance buy", Order=33, GroupName="Order flow imbalance")]
		public Brush _ImbalanceBuyColor
		{ get; set; }
		[Browsable(false)]
		public string _ImbalanceBuyColorSerializable
		{
			get { return Serialize.BrushToString(_ImbalanceBuyColor); }
			set { _ImbalanceBuyColor = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name="Imbalance sell", Order=34, GroupName="Order flow imbalance")]
		public Brush _ImbalanceSellColor
		{ get; set; }
		[Browsable(false)]
		public string _ImbalanceSellColorSerializable
		{
			get { return Serialize.BrushToString(_ImbalanceSellColor); }
			set { _ImbalanceSellColor = Serialize.StringToBrush(value); }
		}

		[Display(Name="Stacked imbalance lines", Order=35, GroupName="Order flow imbalance")]
		public bool _ShowStackedImbalance
		{ get; set; }

		[Range(2, 20)]
		[Display(Name="Stacked count (levels)", Order=36, GroupName="Order flow imbalance")]
		public int _StackedImbalanceCount
		{ get; set; }

		[Range(1, 500)]
		[Display(Name="Stacked extend (bars)", Order=37, GroupName="Order flow imbalance")]
		public int _StackedImbalanceExtendBars
		{ get; set; }

		[Range(0.5, 10.0)]
		[Display(Name="Stacked line width", Order=38, GroupName="Order flow imbalance")]
		public float _StackedImbalance_strokeWidth
		{ get; set; }

		[Display(Name="Stacked line style", Order=39, GroupName="Order flow imbalance")]
		public DashStyleHelper _StackedImbalanceStrokeStyle
		{ get; set; }

		// !- Estilo Footprint (celda de ancho fijo)
		[XmlIgnore]
		[Display(Name="Imbalance text color", Order=40, GroupName="Order flow footprint")]
		public Brush _ImbalanceTextColor
		{ get; set; }
		[Browsable(false)]
		public string _ImbalanceTextColorSerializable
		{
			get { return Serialize.BrushToString(_ImbalanceTextColor); }
			set { _ImbalanceTextColor = Serialize.StringToBrush(value); }
		}

		[Display(Name="POC filled (not outline)", Order=41, GroupName="Order flow footprint")]
		public bool _ClusterPOCFilled
		{ get; set; }

		[XmlIgnore]
		[Display(Name="POC fill color", Order=42, GroupName="Order flow footprint")]
		public Brush _POCFillColor
		{ get; set; }
		[Browsable(false)]
		public string _POCFillColorSerializable
		{
			get { return Serialize.BrushToString(_POCFillColor); }
			set { _POCFillColor = Serialize.StringToBrush(value); }
		}

		[Range(1, 100)]
		[Display(Name="POC fill opacity", Order=43, GroupName="Order flow footprint")]
		public float _POCFillOpacity
		{ get; set; }

		// !- 0 = desactivado. Dibuja una caja sobre cada nivel con volumen >= este valor.
		[Range(0, 1000000)]
		[Display(Name="Highlight volume >= (0 = off)", Order=44, GroupName="Order flow footprint")]
		public long _HighlightVolumeThreshold
		{ get; set; }

		[XmlIgnore]
		[Display(Name="Highlight box color", Order=45, GroupName="Order flow footprint")]
		public Brush _HighlightBoxColor
		{ get; set; }
		[Browsable(false)]
		public string _HighlightBoxColorSerializable
		{
			get { return Serialize.BrushToString(_HighlightBoxColor); }
			set { _HighlightBoxColor = Serialize.StringToBrush(value); }
		}

		[Range(0.5, 6.0)]
		[Display(Name="Highlight box width", Order=46, GroupName="Order flow footprint")]
		public float _HighlightBoxWidth
		{ get; set; }

		[XmlIgnore]
		[Display(Name="Cell up color (delta +)", Order=47, GroupName="Order flow footprint")]
		public Brush _FootprintUpCellColor
		{ get; set; }
		[Browsable(false)]
		public string _FootprintUpCellColorSerializable
		{
			get { return Serialize.BrushToString(_FootprintUpCellColor); }
			set { _FootprintUpCellColor = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name="Cell down color (delta -)", Order=48, GroupName="Order flow footprint")]
		public Brush _FootprintDownCellColor
		{ get; set; }
		[Browsable(false)]
		public string _FootprintDownCellColorSerializable
		{
			get { return Serialize.BrushToString(_FootprintDownCellColor); }
			set { _FootprintDownCellColor = Serialize.StringToBrush(value); }
		}

		[Range(1, 100)]
		[Display(Name="Cell opacity", Order=49, GroupName="Order flow footprint")]
		public float _FootprintCellOpacity
		{ get; set; }

		[XmlIgnore]
		[Display(Name="Cell border color", Order=50, GroupName="Order flow footprint")]
		public Brush _FootprintCellBorderColor
		{ get; set; }
		[Browsable(false)]
		public string _FootprintCellBorderColorSerializable
		{
			get { return Serialize.BrushToString(_FootprintCellBorderColor); }
			set { _FootprintCellBorderColor = Serialize.StringToBrush(value); }
		}

		[Range(0.0, 4.0)]
		[Display(Name="Cell border width (0 = off)", Order=51, GroupName="Order flow footprint")]
		public float _FootprintCellBorderWidth
		{ get; set; }

		[XmlIgnore]
		[Display(Name="Cell text color", Order=52, GroupName="Order flow footprint")]
		public Brush _FootprintTextColor
		{ get; set; }
		[Browsable(false)]
		public string _FootprintTextColorSerializable
		{
			get { return Serialize.BrushToString(_FootprintTextColor); }
			set { _FootprintTextColor = Serialize.StringToBrush(value); }
		}

		[Display(Name="Cell font", Order=53, GroupName="Order flow footprint")]
		public SimpleFont _FootprintFont
		{ get; set; }

		#endregion
	}
}


// --- SOURCE: VolumeAnalysisProfile.cs ---
public static class _VolumeAnalysisProfileEnums
{
	public enum Formula
	{
		Total,
		Delta,
		BidAsk,
		TotalAndBidAsk,
		TotalAndDelta,
		TotalAndDeltaAndBidAsk
	}
	public enum RenderInfo
	{
		BidAsk,
		TotalAndDelta,
		Total,
		Delta		
	}
}

namespace NinjaTrader.NinjaScript.Indicators
{
	public class VolumeAnalysisProfile : Indicator
	{
		#region MAIN
		
		public class WyckoffVolumeProfile : WyckoffRenderControl
		{
			private VolumeAnalysis.WyckoffBars wyckoffBars;
			private VolumeAnalysis.Profile marketVolumeProfile;
			private VolumeAnalysis.Profile rangeVolumeProfile;
			
			private float POCOpacity;
			private float POIOpacity;
			private float selectionOpacity;
			private float eraseOpacity;
			private int timeFrame;
			private bool isRealtime;
			private DateTime beginTime;
			private Func<int, int> calculateBars;
			private Action<int, int, int, double, VolumeAnalysis.MarketOrder> renderVolumeFormula;
			private Action<int, VolumeAnalysis.MarketOrder, VolumeAnalysis.Profile.Ladder> renderVolumeInfo;
			private SharpDX.DirectWrite.TextFormat volumeTextFormat;
			private SharpDX.RectangleF Rect;
			private float minFontWidth;
			private float minFontHeight;
			private bool showTotalVolumeInfo;
			private bool showDeltaInfo;
			private bool showFont;
			private bool showPOC;
			private bool showPOI;
			
			private SharpDX.Direct2D1.Brush gColor;
			private SharpDX.Direct2D1.Brush gColorFont;
			
			private SharpDX.Color colorBid;
			private SharpDX.Color colorAsk;
			private SharpDX.Color colorTotal;
			private SharpDX.Color colorPOC;
			private SharpDX.Color colorPOI;
			private SharpDX.Color colorFont;
			private SharpDX.Color colorSelection;
			private SharpDX.Color colorErase;
			
			private bool key_AddProfile;
			private bool click_AddProfile;
			private bool key_DeleteProfile;
			private SharpDX.Vector2 currentMouseCoords;
			private SharpDX.Vector2 firstMouseCoords;
			private SharpDX.RectangleF rectCoords;
			
			public WyckoffVolumeProfile()
			{
				this.Rect = new SharpDX.RectangleF();
				this.isRealtime= false;
				this.key_AddProfile = this.click_AddProfile = false;
				this.key_DeleteProfile = false;
				this.currentMouseCoords = new SharpDX.Vector2();
				this.firstMouseCoords = new SharpDX.Vector2();
				this.rectCoords = new SharpDX.Rectangle();
			}
			
			#region SETS
			
			public void setFontStyle(SimpleFont font)
			{
				base.setFontStyle(font, out volumeTextFormat);
			}
			public void setShowFont(bool showFont, float minFontWidth, float minFontHeight)
			{
				this.showFont= showFont;
				this.minFontWidth = minFontWidth;
				this.minFontHeight = minFontHeight;
			}
			
			public void setRealtime(bool isRealtime){ this.isRealtime = isRealtime; }
			public bool IsRealtime{ get{ return this.isRealtime; } }
			// !- obtenemos las barras segun la temporalidad elegida
			public int getCalculatedBars(int period)
			{
				return this.calculateBars(period);
			}
			public void setWyckoffBars(VolumeAnalysis.WyckoffBars wyckoffBars)
			{
				this.wyckoffBars = wyckoffBars;
				// !- Valor del timeframe en el que estamos
				this.timeFrame = wyckoffBars.NT8Bars.BarsType.BarsPeriod.Value;
				//this.lastBar = wyckoffBars.NT8Bars.Count - 1;
			}
			public void setVolumeProfile(VolumeAnalysis.Profile marketVolumeProfile, VolumeAnalysis.Profile rangeVolumeProfile)
			{
				this.marketVolumeProfile = marketVolumeProfile;
				this.rangeVolumeProfile = rangeVolumeProfile;
			}
			
			public void setProfileBidAskColor(Brush brushBidColor, Brush brushAskColor, Brush brushTotalColor)
			{
				this.colorBid = WyckoffRenderControl.BrushToColor(brushBidColor);
				this.colorAsk = WyckoffRenderControl.BrushToColor(brushAskColor);
				this.colorTotal = WyckoffRenderControl.BrushToColor(brushTotalColor);
			}
			public void setProfileCalculationColor(Brush brushPOCColor, Brush brushPOIColor)
			{
				this.colorPOC = WyckoffRenderControl.BrushToColor(brushPOCColor);
				this.colorPOI = WyckoffRenderControl.BrushToColor(brushPOIColor);
			}
			public void setCursorStyle(Brush brushSelectionColor, float selectionOpacity, Brush brushEraseColor, float eraseOpacity)
			{
				this.colorSelection = WyckoffRenderControl.BrushToColor(brushSelectionColor);
				this.selectionOpacity = selectionOpacity / 100f;
				this.colorErase = WyckoffRenderControl.BrushToColor(brushEraseColor);
				this.eraseOpacity = eraseOpacity / 100f;
			}
			public void setFontColor(Brush brushFontColor)
			{
				this.colorFont = WyckoffRenderControl.BrushToColor(brushFontColor);
			}
			public void setCalculationsOpacity(float POCOpacity, float POIOpacity)
			{
				this.POCOpacity = POCOpacity / 100.0f;
				this.POIOpacity = POIOpacity / 100.0f;
			}
			public void setShowInfo(bool showTotalVolumeInfo, bool showDeltaInfo)
			{
				this.showTotalVolumeInfo = showTotalVolumeInfo;
				this.showDeltaInfo = showDeltaInfo;
			}
			public void setShowCalculations(bool showPOC, bool showPOI)
			{
				this.showPOC = showPOC;
				this.showPOI = showPOI;
			}
			
			#endregion
			// !- debe ser cargada desde DataLoaded
			#region BARS_CALCULATION
			
			// !- Calculos solo usados para el render de rango de barras en la pantalla
//			private int _defaultTotalBars(int bars){ return bars; }
			private int _TotalBarsByMinutes(int Minutes){ return Minutes/this.timeFrame; }
			private int _TotalBarsByHours(int Hours){ return (Hours*60)/this.timeFrame; }
			private int _TotalBarsByDays(int Days){ return (Days*24*60)/this.timeFrame; }
			public void setBarsPeriodFormula(VolumeAnalysis.PeriodMode periodMode)
			{
				switch( periodMode )
				{
//					case VolumeAnalysis.PeriodMode.Bars:
//					{
//						this.calculateBars = this._defaultTotalBars;
//						break;
//					}
					case VolumeAnalysis.PeriodMode.Minutes:
					{
						this.calculateBars = this._TotalBarsByMinutes;
						break;
					}
					case VolumeAnalysis.PeriodMode.Hours:
					{
						this.calculateBars = this._TotalBarsByHours;
						break;
					}
					case VolumeAnalysis.PeriodMode.Days:
					{
						this.calculateBars = this._TotalBarsByDays;
						break;
					}
				}
			}
			
			#endregion
			#region RANGE_PROFILE_KEY_ACTIVATION
			
			// !- este evento se dispara continuamente mientras este pulsada la tecla en cuestion
			private void resetMouseCoords()
			{
				this.firstMouseCoords.X = currentMouseCoords.X = 0;
				//this.firstMouseCoords.Y = currentMouseCoords.Y = 0;
			}
			public bool onKeyDown(System.Windows.Input.KeyEventArgs e)
		    {
		        if(e.Key == Key.LeftCtrl && !this.click_AddProfile){// || e.Key == Key.Space || e.Key == Key.LeftAlt || e.Key == Key.LeftShift)
		            this.key_AddProfile = true;
					return true;
		        }
				if(e.Key == Key.LeftShift){
					this.key_DeleteProfile = true;
					return true;
				}
				return false;
		    }
		    public bool onKeyUp(System.Windows.Input.KeyEventArgs e){
				if(e.Key == Key.LeftCtrl){
					this.key_AddProfile = this.click_AddProfile = false;
					this.resetMouseCoords();
					return true;
		        }
				if(e.Key == Key.LeftShift){
					this.key_DeleteProfile = false;
					this.resetMouseCoords();
					return true;
				}
				return false;
		    }
			public bool mouseMoveEvent(MouseEventArgs mouseEvent, ChartPanel chartPanel)
			{
				if( this.key_AddProfile )//bool isDown = Keyboard.IsKeyDown( this.keyCode );
				{
					Point mouseCoords = mouseEvent.GetPosition(chartPanel);
					int mX = ChartingExtensions.ConvertToHorizontalPixels(mouseCoords.X, CHART_CONTROL.PresentationSource); //int mX = this.chartControl.MouseDownPoint.X.ConvertToHorizontalPixels(chartControl.PresentationSource);
					//int mY = ChartingExtensions.ConvertToVerticalPixels(mouseCoords.Y, this.chartControl.PresentationSource);
					//int barX = this.chartControl.GetXByBarIndex(this.chartBars, this.chartBars.GetBarIdxByX(this.chartControl, mX));
					//int barY = this.chartScale.GetYByValue(this.chartScale.GetValueByY(mY));
					if( this.firstMouseCoords.X == 0 ){//&& this.firstMouseCoords.Y == 0 ){
						this.firstMouseCoords.X = mX;
						//this.firstMouseCoords.Y = mY;
					}
					else{
						this.currentMouseCoords.X = mX;
						//this.currentMouseCoords.Y = mY;
					}
					return true;
				}
				if( this.key_DeleteProfile ){
					Point mouseCoords = mouseEvent.GetPosition(chartPanel);
					int mX = ChartingExtensions.ConvertToHorizontalPixels(mouseCoords.X, CHART_CONTROL.PresentationSource); //int mX = this.chartControl.MouseDownPoint.X.ConvertToHorizontalPixels(chartControl.PresentationSource);
//					int mY = ChartingExtensions.ConvertToVerticalPixels(mouseCoords.Y, this.chartControl.PresentationSource);
					int barIndex = this.rangeVolumeProfile.GetProfileInRange(CHART_BARS.GetBarIdxByX(CHART_CONTROL, mX));
					// !- si no esta dentro del rango reseteamos las coordenadas
					if( barIndex == -1 ){
						this.resetMouseCoords();
						return false;
					}
					if( this.firstMouseCoords.X == 0 ){
						this.firstMouseCoords.X = CHART_CONTROL.GetXByBarIndex(CHART_BARS, this.rangeVolumeProfile.StartBarIndex(barIndex));
					}
					else{
						this.currentMouseCoords.X = CHART_CONTROL.GetXByBarIndex(CHART_BARS, barIndex);
					}
				}
//				if( Keyboard.IsKeyUp( this.keyCode ) ){
//					this.keyPressed = 0;
//					isDown = false;
//				}
				return false;
			}
			public bool mouseClicked()//MouseButtonEventArgs mouseEvent)
			{
				// convert e.GetPosition for different dpi settings
				//clickPoint.X = ChartingExtensions.ConvertToHorizontalPixels(mouseEvent.GetPosition(this.chartControl as IInputElement).X, ChartControl.PresentationSource);
				//clickPoint.Y = ChartingExtensions.ConvertToVerticalPixels(mouseEvent.GetPosition(this.chartControl as IInputElement).Y, ChartControl.PresentationSource);
				//convertedPrice = Instrument.MasterInstrument.RoundToTickSize(chartScale.GetValueByY((float)clickPoint.Y));
				//DateTime convertedTime = ChartControl.GetTimeBySlotIndex((int)ChartControl.GetSlotIndexByX((int)clickPoint.X));
				//int mousePointX = chartControl.MouseDownPoint.X.ConvertToHorizontalPixels(chartControl.PresentationSource);
				if( this.key_AddProfile ){
					int firstBarX = CHART_BARS.GetBarIdxByX(CHART_CONTROL, (int)this.firstMouseCoords.X);
					int lastBarX = CHART_BARS.GetBarIdxByX(CHART_CONTROL, (int)this.currentMouseCoords.X);
					// !- minimos de barras: 2
					if( Math.Abs(firstBarX - lastBarX) <= 1 ){
						return false;
					}
					this.rangeVolumeProfile.AddRangeProfile(Math.Min(firstBarX, lastBarX), Math.Max(firstBarX, lastBarX));
					this.click_AddProfile= true;
					this.key_AddProfile = false;
					
					return true;
				}
				if( this.key_DeleteProfile ){
					int barIndex = CHART_BARS.GetBarIdxByX(CHART_CONTROL, (int)this.currentMouseCoords.X);
					barIndex = this.rangeVolumeProfile.GetProfileInRange(barIndex);
					if( this.rangeVolumeProfile.Exists(barIndex) ){
						this.rangeVolumeProfile.RemoveProfile( barIndex );
						// !- reseteamos para no ver las coordenadas del perfil borrado
						this.resetMouseCoords();
						return true;
					}
				}
				
				return false;
			}
			
			#endregion
			#region RENDER_VOLUME_INFO
			
			private bool _setVolumeInfo(int barY, VolumeAnalysis.Profile.Ladder profileLadder)
			{
				if( this.showFont && this.W >= this.minFontWidth && this.H >= this.minFontHeight ){
					int totalBars	= profileLadder.TotalBars;
					int startBar	= profileLadder.StartBarIndex;
					
					this.Rect.X = CHART_CONTROL.GetXByBarIndex(CHART_BARS, startBar) - (this.W / 2);
					this.Rect.Y = barY - (this.H / 2f);
					this.Rect.Width = totalBars + this.W;
					this.Rect.Height = this.H;
					return true;
				}
				return false;
			}
			private void _renderBidAskVolumeInfo(int barY, VolumeAnalysis.MarketOrder marketOrder, VolumeAnalysis.Profile.Ladder profileLadder)
			{
				if( this._setVolumeInfo(barY, profileLadder) ){
					myDrawText(string.Format("{0} x {1}", marketOrder.Bid, marketOrder.Ask), ref Rect, colorFont, -1, -1, volumeTextFormat, 1.0f);
				}
			}
			private void _renderTotalDeltaVolumeInfo(int barY, VolumeAnalysis.MarketOrder marketOrder, VolumeAnalysis.Profile.Ladder profileLadder)
			{
				if( this._setVolumeInfo(barY, profileLadder) ){
					myDrawText(string.Format("{0} x {1}", marketOrder.Total, marketOrder.Delta), ref Rect, colorFont, -1, -1, volumeTextFormat, 1.0f);
				}
			}
			private void _renderDeltaVolumeInfo(int barY, VolumeAnalysis.MarketOrder marketOrder, VolumeAnalysis.Profile.Ladder profileLadder)
			{
				if( this._setVolumeInfo(barY, profileLadder) ){
					myDrawText(marketOrder.Delta.ToString(), ref Rect, colorFont, -1, -1, volumeTextFormat, 1.0f);
				}
			}
			private void _renderTotalVolumeInfo(int barY, VolumeAnalysis.MarketOrder marketOrder, VolumeAnalysis.Profile.Ladder profileLadder)
			{
				if( this._setVolumeInfo(barY, profileLadder) ){
					myDrawText(marketOrder.Total.ToString(), ref Rect, colorFont, -1, -1, volumeTextFormat, 1.0f);
				}
			}
			public void setVolumeRenderInfo(_VolumeAnalysisProfileEnums.RenderInfo renderInfo)
			{
				switch( renderInfo )
				{
					case _VolumeAnalysisProfileEnums.RenderInfo.BidAsk:
					{
						this.renderVolumeInfo = this._renderBidAskVolumeInfo;
						break;
					}
					case _VolumeAnalysisProfileEnums.RenderInfo.Total:
					{
						this.renderVolumeInfo = this._renderTotalVolumeInfo;
						break;
					}
					case _VolumeAnalysisProfileEnums.RenderInfo.Delta:
					{
						this.renderVolumeInfo = this._renderDeltaVolumeInfo;
						break;
					}
					case _VolumeAnalysisProfileEnums.RenderInfo.TotalAndDelta:
					{
						this.renderVolumeInfo = this._renderTotalDeltaVolumeInfo;
						break;
					}
				}
			}
			
			#endregion
			#region RENDER_VOLUME_FORMULA
			
			private void _renderTotalVolume(
				int currentBar, int totalBars, int barY,
				double maxVolume, VolumeAnalysis.MarketOrder marketOrder)
			{
				double volPercent = Math2.Percent(maxVolume, marketOrder.Total);
				this.calculatePriceLadder(currentBar, totalBars, barY, volPercent);
				myFillRectangle(ref Rect, colorTotal, (float)volPercent / 100f);
			}
			private void _renderDeltaVolume(
				int currentBar, int totalBars, int barY,
				double maxVolume, VolumeAnalysis.MarketOrder marketOrder)
			{
				double delta = marketOrder.Delta;
				double volPercent = Math2.Percent(maxVolume, Math.Abs(delta));
				
				calculatePriceLadder(currentBar, totalBars, barY, volPercent);
				if( delta < 0 ){
					myFillRectangle(ref Rect, colorBid, 1.0f);
				}
				else{
					myFillRectangle(ref Rect, colorAsk, 1.0f);
				}
			}
			private void _renderBidAskVolume(
				int currentBar, int totalBars, int barY,
				double maxVolume, VolumeAnalysis.MarketOrder marketOrder)
			{
				double volPercent;
				
				volPercent = Math2.Percent(maxVolume, marketOrder.Bid);
				this.calculatePriceLadder(currentBar, totalBars, barY, volPercent);
				myFillRectangle(ref Rect, colorBid, (float)volPercent / 100f);
				
				volPercent = Math2.Percent(maxVolume, marketOrder.Ask);
				this.calculatePriceLadder(currentBar, totalBars, barY, volPercent);
				myFillRectangle(ref Rect, colorAsk, (float)volPercent / 100f);
			}
			private void _renderTotalAndBidAsk(int currentBar, int totalBars, int barY,
				double maxVolume, VolumeAnalysis.MarketOrder marketOrder)
			{
				_renderTotalVolume (currentBar, totalBars, barY, maxVolume, marketOrder);
				_renderBidAskVolume(currentBar, totalBars, barY, maxVolume, marketOrder);
			}
			private void _renderTotalAndDelta(int currentBar, int totalBars, int barY,
				double maxVolume, VolumeAnalysis.MarketOrder marketOrder)
			{
				_renderTotalVolume(currentBar, totalBars, barY, maxVolume, marketOrder);
				_renderDeltaVolume(currentBar, totalBars, barY, maxVolume, marketOrder);
			}
			private void _renderTotalAndDeltaAndBidAsk(int currentBar, int totalBars, int barY,
				double maxVolume, VolumeAnalysis.MarketOrder marketOrder)
			{
				_renderTotalVolume(currentBar, totalBars, barY, maxVolume, marketOrder);
				_renderDeltaVolume(currentBar, totalBars, barY, maxVolume, marketOrder);
				_renderBidAskVolume(currentBar, totalBars, barY, maxVolume, marketOrder);
			}
			
			public void setVolumeFormula(_VolumeAnalysisProfileEnums.Formula volumeFormula)
			{
				switch( volumeFormula )
				{
					case _VolumeAnalysisProfileEnums.Formula.Total:
					{
						this.renderVolumeFormula = this._renderTotalVolume;
						break;
					}
					case _VolumeAnalysisProfileEnums.Formula.Delta:
					{
						this.renderVolumeFormula = this._renderDeltaVolume;
						break;
					}
					case _VolumeAnalysisProfileEnums.Formula.BidAsk:
					{
						this.renderVolumeFormula = this._renderBidAskVolume;
						break;
					}
					case _VolumeAnalysisProfileEnums.Formula.TotalAndBidAsk:
					{
						this.renderVolumeFormula = this._renderTotalAndBidAsk;
						break;
					}
					case _VolumeAnalysisProfileEnums.Formula.TotalAndDelta:
					{
						this.renderVolumeFormula = this._renderTotalAndDelta;
						break;
					}
					case _VolumeAnalysisProfileEnums.Formula.TotalAndDeltaAndBidAsk:
					{
						this.renderVolumeFormula = this._renderTotalAndDeltaAndBidAsk;
						break;
					}
				}
			}
			
			#endregion
			#region RENDER_PROFILE
			
			public void renderMessageInfo(string textInfo, int X, int Y, SharpDX.Color4 textColor, float fontSize) //SharpDX.Color4 textLayoutColor, int fontSize)//SharpDX.Color.Beige, SharpDX.Color.White
			{
				SharpDX.Vector2 startPoint = new SharpDX.Vector2(X, Y);
				SharpDX.DirectWrite.TextFormat textFormat= new SharpDX.DirectWrite.TextFormat(Core.Globals.DirectWriteFactory, "Arial", fontSize);
				SharpDX.DirectWrite.TextLayout textLayout = new SharpDX.DirectWrite.TextLayout(Core.Globals.DirectWriteFactory,
					textInfo, textFormat, this.PanelW, this.PanelH);
				SharpDX.RectangleF msgRect = new SharpDX.RectangleF(startPoint.X, startPoint.Y,
					textLayout.Metrics.Width, textLayout.Metrics.Height);
				SharpDX.Direct2D1.SolidColorBrush textDXBrush = new SharpDX.Direct2D1.SolidColorBrush(RENDER_TARGET, textColor);
	 			
				// execute the render target draw rectangle with desired values
//				if(textLayoutColor != null){
//					SharpDX.Direct2D1.SolidColorBrush layoutDXBrush = new SharpDX.Direct2D1.SolidColorBrush(Target, textLayoutColor);
//					Target.DrawRectangle(msgRect, layoutDXBrush);
//					layoutDXBrush.Dispose();
//				}
				// execute the render target text layout command with desired values
				RENDER_TARGET.DrawTextLayout(startPoint, textLayout, textDXBrush);
				
				textLayout.Dispose();
				textFormat.Dispose();
				textDXBrush.Dispose();
			}
			
			private void calculatePriceLadder(
				int currentBar, int totalBars, int barY,
				double volumePercent)
			{
				// !- Calculamos las barras que hay en el periodo de tiempo elejido
				int barXfrom = CHART_CONTROL.GetXByBarIndex(CHART_BARS, currentBar - totalBars);
				int _barXto = currentBar - totalBars - (int)Math.Round((volumePercent * totalBars) / 100);
				//R.Width = W;
				Rect.X = barXfrom - (this.W / 2f);
				Rect.Y = barY - (this.H / 2f);
				Rect.Width = barXfrom - CHART_CONTROL.GetXByBarIndex(CHART_BARS, _barXto) + this.W;
				Rect.Height= this.H;
			}
			// !- para POCs y POIs
			private void renderPO(
				int currentBar, int totalBars, int barY,
				double maxLadderVolume, double currentVolume, SharpDX.Color color, float opacity)
			{
				double volPercent = Math2.Percent(maxLadderVolume, currentVolume);
				// !- El valor maximo de volumen representa el 100%, el cual es el POC
				if( volPercent == 100 )
				{
					this.calculatePriceLadder(currentBar, totalBars, barY, volPercent);
					myFillRectangle(ref Rect, color, opacity);
				}
			}
			// !- informacion delta y total del profile
			private void renderProfileInfo(int currentBar, VolumeAnalysis.Profile.Ladder profileLadder)
			{
				int totalBars = profileLadder.TotalBars;
				//if( (this.W - 1) > totalBars ) return;
				int barXfrom = CHART_CONTROL.GetXByBarIndex(CHART_BARS, currentBar - totalBars);
				int barXto = CHART_CONTROL.GetXByBarIndex(CHART_BARS, currentBar);
				
				Rect.Width = barXto - barXfrom;
				Rect.Height = this.H;
				Rect.X = barXfrom;
				// !- Obtenemos el precio mas alto del perfil
				VolumeAnalysis.MarketOrder marketOrder = profileLadder.ProfileVolume;
				// !- Renderizamos la funcion correcta de texto(elejida por el usuario)
				if( this.showTotalVolumeInfo ){
					//Rect.Y = chartScale.GetYByValue(vp.GetLadderHighPrice(currentBar)) - (this.H * 8);
					Rect.Y = CHART_SCALE.GetYByValue(profileLadder.HighPrice) - (this.H * 4f);
					//Target.DrawText("V:", this.volumeTextFormat, Rect, brushFontColor.ToDxBrush(Target));
					myDrawText(string.Format("V:{0}", marketOrder.Total), ref Rect, colorTotal, -1, -1, volumeTextFormat, 1.0f);
				}
				if( this.showDeltaInfo ){
					Rect.Y = CHART_SCALE.GetYByValue(profileLadder.LowPrice) + (this.H * 4f);
					//Target.DrawText("D:", this.volumeTextFormat, Rect, brushFontColor.ToDxBrush(Target));
					long delta = marketOrder.Delta;
					if( delta >= 0 ){
						myDrawText(string.Format("D:{0}", delta), ref Rect, colorAsk, -1, -1, volumeTextFormat, 1.0f);
					}
					else{
						myDrawText(string.Format("D:{0}", delta), ref Rect, colorBid, -1, -1, volumeTextFormat, 1.0f);
					}
				}
			}
			private void _renderProfile(int barIndex, VolumeAnalysis.Profile.Ladder profileLadder, int totalBars)
			{
				VolumeAnalysis.MarketOrder mo;
				long totalVol;
				int barY;
				double price;
				double maxLadderVol = profileLadder.MaxVolume.Total;
				double minLadderVol = profileLadder.MinVolume.Total;
				double maxLadderPrice = profileLadder.MaxLadderPrice;
				double minLadderPrice = profileLadder.MinLadderPrice;
				
				//Stopwatch sw = new Stopwatch(); sw.Start();
				foreach(var p in profileLadder)
				{
					// !- Informacion de volumen del precio en el ladder
					price = p.Key;
					mo = p.Value;
					
					barY = CHART_SCALE.GetYByValue(p.Key);
					totalVol = mo.Total;
					// !- Renderizamos el volumen segun la formula(Total, Bid, Ask, Delta, etc..)
					renderVolumeFormula(barIndex, totalBars, barY, maxLadderVol, mo);
					// !- Renderizamos el POC(Point of control)
					/// REVISAR ESTO EN UN FUTURO...
					if( this.showPOC && price == maxLadderPrice  ){
						renderPO(barIndex, totalBars, barY, maxLadderVol, totalVol, this.colorPOC, POCOpacity);
					}
					// !- Renderizamos el POI(Point of imbalance)
					if( this.showPOI && price == minLadderPrice ){
						renderPO(barIndex, totalBars, barY, minLadderVol, totalVol, this.colorPOI, POIOpacity);
					}
					// !- Renderizamos en texto la informacion de volumen total
					renderVolumeInfo(barY, mo, profileLadder);
				}
//				int barX = chartControl.GetXByBarIndex(chartBars, barIndex);
//				Target.DrawLine(new SharpDX.Vector2(barX, chartScale.GetYByValue(pl.HighPrice)), new SharpDX.Vector2(barX, chartScale.GetYByValue(pl.LowPrice)), Brushes.Salmon.ToDxBrush(Target, 0.3f), this.W / 2f);
				//sw.Stop(); Print(string.Format("ms:{0}", sw.ElapsedMilliseconds));
				renderProfileInfo(barIndex, profileLadder);
			}
			private void _renderCursor(bool keyIsPressed, SharpDX.Color color, float opacity)
			{
				if( !keyIsPressed || this.firstMouseCoords.X == 0 || this.currentMouseCoords.X == 0 ){
					return;
				}
//				Target.DrawLine(this.currentMouseCoords, this.firstMouseCoords, Brushes.Goldenrod.ToDxBrush(Target, 0.7f), 1.0f);
				this.rectCoords.X = currentMouseCoords.X;
				this.rectCoords.Y = 0;//currentMouseCoords.Y - this.PanelH;
				this.rectCoords.Width = firstMouseCoords.X - currentMouseCoords.X;
				this.rectCoords.Height = this.PanelH;//*2f;//firstMouseCoords.Y - currentMouseCoords.Y
				myFillRectangle(ref rectCoords, color, opacity);
				//Target.DrawLine(this.currentMouseCoords, this.firstMouseCoords, Brushes.Crimson.ToDxBrush(Target, 1.0f), 1.0f);
			}
			public void renderCursor()
			{
				_renderCursor(this.key_AddProfile, this.colorSelection, selectionOpacity);
				_renderCursor(this.key_DeleteProfile, this.colorErase, eraseOpacity);
			}
			public void renderProfile(int barIndex)
			{
				if( this.marketVolumeProfile == null ){
					return;
				}
				if( marketVolumeProfile.Exists(barIndex) ){
					/// !- quitamos una barra de adelante y una de atras, no las necesitamos(de otro modo habria desbordamiento de grafico)
					// esto sucede en la funcion .calculatePriceLadder(...) ya que los calculos de perfil de volumen en tiempo de mercado exigen
					// que la barra hasta donde se calcula el N-volume profile siempre sea + 1(es decir termina donde empieza al siguiente)
					_renderProfile(barIndex - 1, marketVolumeProfile.GetProfile(barIndex), marketVolumeProfile.TotalBars(barIndex) - 1);
				}
			}
			public void renderRealtimeProfile()
			{
				VolumeAnalysis.Profile.Ladder tmp = this.marketVolumeProfile.GetRealtimeProfile;
				if( tmp == null ){
					return;
				}
				_renderProfile(this.wyckoffBars.CurrentBarIndex, tmp, tmp.TotalBars);
			}
			public void renderRangeProfile()
			{
				if( this.rangeVolumeProfile == null ){
					return;
				}
				foreach(var vp in this.rangeVolumeProfile){
					_renderProfile(vp.Key, vp.Value, vp.Value.TotalBars);
				}
			}
			
			#endregion
		} // WyckoffVolumeProfile
		
		#endregion
		#region GLOBAL_VARIABLES
		
		private VolumeAnalysis.WyckoffBars wyckoffBars;
		private VolumeAnalysis.Profile volumeProfile;
		private VolumeAnalysis.Profile rangeVolumeProfile;
		private WyckoffVolumeProfile wyckoffVP;
		
		#endregion
		#region INDICATOR_SETUP
		
		private void setStyle()
		{
			_TotalVolColor = Brushes.CornflowerBlue;
			_AskVolColor = Brushes.Green;
			_BidVolColor = Brushes.Red;
			_POCColor = Brushes.Tan;
			_POIColor = Brushes.Indigo;
			_SelectionColor = Brushes.Navy;
			_SelectionOpacity = 10f;
			_EraseColor = Brushes.Crimson;
			_EraseOpacity = 10f;
			
			_FontColor = Brushes.LightYellow;			
			// *- 90%
			_POCOpacity = 90f;
			// *- 80%
			_POIOpacity = 80f;
			_TextOpacity = 100f;
		}
		private void setCalculations()
		{
			_PeriodMode = VolumeAnalysis.PeriodMode.Days;
			_VolumeFormula = _VolumeAnalysisProfileEnums.Formula.TotalAndBidAsk;
			_VolumeRenderInfo = _VolumeAnalysisProfileEnums.RenderInfo.Total;
			// !- 1 dia de perfil de volumen por defecto
			_Period = 1;
			_showTotalVolumeInfo = true;
			_showDeltaInfo = true;
			_ShowPOC = true;
			_ShowPOI = true;
			_RealtimeHeuristic = true;
			// !- si este valor es false entonces el perfil de volumen NO es calculado sobre el grafico
			_EnableMarketProfile= true;
		}
		private void setFontStyle()
		{
			_TextFont = new SimpleFont();
			_TextFont.Family = new FontFamily("Arial");
			_TextFont.Size = 10f;
			_TextFont.Bold = false;
			_TextFont.Italic= false;
			_ShowText = true;
			_MinFontWidth = 1f;
			_MinFontHeight = 8f;
		}
		
		#endregion
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"";
				Name										= "Volume Analysis Profile";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= true;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= false;
				
				setStyle();
				setCalculations();
				setFontStyle();
				
				wyckoffVP = new WyckoffVolumeProfile();
			}
			else if (State == State.Configure)
			{
				wyckoffVP.setProfileBidAskColor(_BidVolColor, _AskVolColor, _TotalVolColor);
				wyckoffVP.setProfileCalculationColor(_POCColor, _POIColor);
				wyckoffVP.setCursorStyle(_SelectionColor, _SelectionOpacity,_EraseColor, _EraseOpacity);
				wyckoffVP.setFontStyle(_TextFont);
				wyckoffVP.setFontColor(_FontColor);
				wyckoffVP.setShowFont(_ShowText, _MinFontWidth, _MinFontHeight);
				
				wyckoffVP.setShowCalculations(_ShowPOC, _ShowPOI);
				wyckoffVP.setVolumeRenderInfo(_VolumeRenderInfo);
				
				wyckoffVP.setCalculationsOpacity(_POCOpacity, _POIOpacity);
				wyckoffVP.setVolumeFormula(_VolumeFormula);
				wyckoffVP.setShowInfo(_showTotalVolumeInfo, _showDeltaInfo);
				
				// !- Seteamos la formula correcta para calcular las barras totales en cada perfil de volumen
				wyckoffVP.setBarsPeriodFormula(_PeriodMode);
				
				Calculate = Calculate.OnEachTick;
			}
			else if(State == State.DataLoaded)
			{
				wyckoffBars = new VolumeAnalysis.WyckoffBars(Bars);
				wyckoffVP.setWyckoffBars(wyckoffBars);
				
				/// !- menor a 6 barras el volume profile da error
				if( wyckoffVP.getCalculatedBars(_Period)  < 6){
					wyckoffBars = null;
					return;
				}
				
				if( this._EnableMarketProfile ){
					volumeProfile = new VolumeAnalysis.Profile(wyckoffBars);
					volumeProfile.setTimePeriod(_Period, _PeriodMode);
					volumeProfile.setRealtimeCalculations(_RealtimeHeuristic);
				}
				rangeVolumeProfile = new VolumeAnalysis.Profile(wyckoffBars);
				// !- no necesitamos tiempo real
				rangeVolumeProfile.setRealtimeCalculations(false);
				// !- no necesitamos determinar el periodo temporal ya que es escogido por el usuario
				//rangeVolumeProfile.setTimePeriod(_Period, _PeriodMode);
				
				wyckoffVP.setFontStyle(_TextFont);
				wyckoffVP.setVolumeProfile(volumeProfile, rangeVolumeProfile);
				
				if( ChartPanel != null && ChartControl != null )
				{
//					wyckoffVP.setRangeKey(_RangeKey);
					// !- activamos las funciones de teclado
					ChartPanel.KeyDown+= new System.Windows.Input.KeyEventHandler(OnKeyDown);
            		ChartPanel.KeyUp += new System.Windows.Input.KeyEventHandler(OnKeyUp);
					ChartControl.MouseLeftButtonDown += MouseClicked;
					ChartControl.MouseMove += MouseMoveEvent;
				}
			}
			else if(State == State.Realtime)
			{
				if( wyckoffBars != null )
					wyckoffVP.setRealtime(true);
			}
			else if(State == State.Terminated)
        	{
	            if( wyckoffBars != null && ChartPanel != null && ChartControl != null)
	            {
	                ChartPanel.KeyDown -= OnKeyDown;
	                ChartPanel.KeyUp -= OnKeyUp;
					ChartControl.MouseLeftButtonDown -= MouseClicked;
					ChartControl.MouseMove -= MouseMoveEvent;
	            }
	        }
		}
		#region MOUSE_AND_KEY_EVENTS
		
		private void MouseMoveEvent(object sender, MouseEventArgs e){
			if( wyckoffVP.mouseMoveEvent(e, ChartPanel) ){
				ForceRefresh();
			}
		}
		private void OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e){
			if( wyckoffVP.onKeyDown(e) ){
				ForceRefresh();
			}
		}
	    public void OnKeyUp(object sender, System.Windows.Input.KeyEventArgs e){
			if( wyckoffVP.onKeyUp(e) ){
				ForceRefresh();
			}
		}
		private void MouseClicked(object sender, MouseButtonEventArgs e)
		{
			if( wyckoffVP.mouseClicked() ){
				ForceRefresh();
			}
		}
		
		#endregion
		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			base.OnRender(chartControl, chartScale);
			// !- $IsInHitTest es bool: "== null" era siempre false y no filtraba nada.
			// Quitamos tambien el gate "!IsRealtime": con Tick Replay las barras historicas
			// ya tienen volumen y el perfil debe dibujarse aunque no haya feed en vivo.
			if( IsInHitTest || chartControl == null || chartScale == null ||
				ChartBars == null || ChartBars.Bars == null || RenderTarget == null )
				return;
			if( wyckoffBars == null ){
				wyckoffVP.setChartPanelHW(ChartPanel.H, ChartPanel.W);
				wyckoffVP.setRenderTarget(chartControl, chartScale, ChartBars, RenderTarget);
				wyckoffVP.renderMessageInfo(string.Format("Bars number error:{0} minimum required for volume profile:6",  wyckoffVP.getCalculatedBars(_Period)), ChartPanel.W / 3, ChartPanel.H / 2, SharpDX.Color.Beige, 14);
				return;
			}
			// 1- Altura minima de un tick
			// 2- Ancho de barra en barra
			wyckoffVP.setHW(chartScale.GetPixelsForDistance(TickSize), chartControl.Properties.BarDistance);
			wyckoffVP.setChartPanelHW(ChartPanel.H, ChartPanel.W);
			// !- Apuntamos al target de renderizado
			wyckoffVP.setRenderTarget(chartControl, chartScale, ChartBars, RenderTarget);
			
			if( this._EnableMarketProfile ){
				int fromIndex = ChartBars.FromIndex;
				int toIndex = ( ChartBars.ToIndex + wyckoffVP.getCalculatedBars(_Period) ) - 1;
				try{
					for(int barIndex = fromIndex; barIndex <= toIndex; barIndex++){
						wyckoffVP.renderProfile(barIndex);
					}
					if( this._RealtimeHeuristic ){
						wyckoffVP.renderRealtimeProfile();
					}
				} catch{ }
			}			
			wyckoffVP.renderRangeProfile();
			wyckoffVP.renderCursor();
		}
		protected override void OnMarketData(MarketDataEventArgs MarketArgs)
		{
			if( wyckoffBars == null || CurrentBar < 0 ){
				return;
			}
			// !- Indexado por el CurrentBar REAL de NinjaTrader (ver SE.cs), asi el perfil
			// queda alineado con las barras del grafico en historico y en tiempo real.
			if( !wyckoffBars.onMarketData(MarketArgs, CurrentBar) ){
				return;
			}
			if( this._EnableMarketProfile ){
				volumeProfile.AddMarketProfile(CurrentBar, MarketArgs);
			}
		}
		
		#region Properties
		
		// !- Setup
		[NinjaScriptProperty]
		[Display(Name="Formula", Order=0, GroupName="Volume Profile Calculations")]
		public _VolumeAnalysisProfileEnums.Formula _VolumeFormula
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Ladder information", Order=1, GroupName="Volume Profile Calculations")]
		public _VolumeAnalysisProfileEnums.RenderInfo _VolumeRenderInfo
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Time", Order=2, GroupName="Volume Profile Calculations")]
		public VolumeAnalysis.PeriodMode _PeriodMode
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Period", Order=3, GroupName="Volume Profile Calculations")]
		public int _Period
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Show POC", Order=4, GroupName="Volume Profile Calculations")]
		public bool _ShowPOC
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Show POI", Order=5, GroupName="Volume Profile Calculations")]
		public bool _ShowPOI
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Realtime heuristic", Order=6, GroupName="Volume Profile Calculations")]
		public bool _RealtimeHeuristic
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Enable market profile", Order=7, GroupName="Volume Profile Calculations")]
		public bool _EnableMarketProfile
		{ get; set; }
		
		// !- Style
		[XmlIgnore]
		[Display(Name="Total volume color", Order=1, GroupName="Volume Profile Style")]
		public Brush _TotalVolColor
		{ get; set; }
		[Browsable(false)]
		public string _TotalVolColorSerializable
		{
			get { return Serialize.BrushToString(_TotalVolColor); }
			set { _TotalVolColor = Serialize.StringToBrush(value); }
		}
		[XmlIgnore]
		[Display(Name="Bid volume color", Order=2, GroupName="Volume Profile Style")]
		public Brush _BidVolColor
		{ get; set; }
		[Browsable(false)]
		public string _BidVolColorSerializable
		{
			get { return Serialize.BrushToString(_BidVolColor); }
			set { _BidVolColor = Serialize.StringToBrush(value); }
		}
		[XmlIgnore]
		[Display(Name="Ask volume color", Order=3, GroupName="Volume Profile Style")]
		public Brush _AskVolColor
		{ get; set; }
		[Browsable(false)]
		public string _AskVolColorSerializable
		{
			get { return Serialize.BrushToString(_AskVolColor); }
			set { _AskVolColor = Serialize.StringToBrush(value); }
		}
		
		// *- POC color style
		[XmlIgnore]
		[Display(Name="POC color", Order=4, GroupName="Volume Profile Style")]
		public Brush _POCColor
		{ get; set; }
		[Browsable(false)]
		public string _POCColorSerializable
		{
			get { return Serialize.BrushToString(_POCColor); }
			set { _POCColor = Serialize.StringToBrush(value); }
		}
		[NinjaScriptProperty]
		[Range(1.0f, 100.0f)]
		[Display(Name="POC opacity %", Order=5, GroupName="Volume Profile Style")]
		public float _POCOpacity
		{ get; set; }
		
		// *- POI color style
		[XmlIgnore]
		[Display(Name="POI color", Order=6, GroupName="Volume Profile Style")]
		public Brush _POIColor
		{ get; set; }
		[Browsable(false)]
		public string _POIColorSerializable
		{
			get { return Serialize.BrushToString(_POIColor); }
			set { _POIColor = Serialize.StringToBrush(value); }
		}
		[NinjaScriptProperty]
		[Range(1.0f, 100.0f)]
		[Display(Name="POI opacity %", Order=7, GroupName="Volume Profile Style")]
		public float _POIOpacity
		{ get; set; }
		
		[XmlIgnore]
		[Display(Name="Selection color", Order=8, GroupName="Volume Profile Style")]
		public Brush _SelectionColor
		{ get; set; }
		[Browsable(false)]
		public string _SelectionColorSerializable
		{
			get { return Serialize.BrushToString(_SelectionColor); }
			set { _SelectionColor = Serialize.StringToBrush(value); }
		}
		[NinjaScriptProperty]
		[Range(1.0f, 100.0f)]
		[Display(Name="Selection opacity %", Order=9, GroupName="Volume Profile Style")]
		public float _SelectionOpacity
		{ get; set; }
		
		[XmlIgnore]
		[Display(Name="Erase color", Order=10, GroupName="Volume Profile Style")]
		public Brush _EraseColor
		{ get; set; }
		[Browsable(false)]
		public string _EraseColorSerializable
		{
			get { return Serialize.BrushToString(_EraseColor); }
			set { _EraseColor = Serialize.StringToBrush(value); }
		}
		[NinjaScriptProperty]
		[Range(1.0f, 100.0f)]
		[Display(Name="Erase opacity %", Order=11, GroupName="Volume Profile Style")]
		public float _EraseOpacity
		{ get; set; }
		
		[XmlIgnore]
		[Display(Name="Font Color", Order=12, GroupName="Volume Profile Style")]
		public Brush _FontColor
		{ get; set; }
		[Browsable(false)]
		public string _FontColorSerializable
		{
			get { return Serialize.BrushToString(_FontColor); }
			set { _FontColor = Serialize.StringToBrush(value); }
		}
		[NinjaScriptProperty]
		[Range(1.0f, 100.0f)]
		[Display(Name="Text opacity %", Order=13, GroupName="Volume Profile Style")]
		public float _TextOpacity
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Font", Order=14, GroupName="Volume Profile Style")]
		public SimpleFont _TextFont
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Show text", Order=15, GroupName="Volume Profile Style")]
		public bool _ShowText
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1.0f, float.MaxValue)]
		[Display(Name="Min font width", Order=16, GroupName="Volume Profile Style")]
		public float _MinFontWidth
		{ get; set; }
		[NinjaScriptProperty]
		[Range(1.0f, float.MaxValue)]
		[Display(Name="Min font height", Order=17, GroupName="Volume Profile Style")]
		public float _MinFontHeight
		{ get; set; }
		
		// !- Info
		[NinjaScriptProperty]
		[Display(Name="Total volume", Order=0, GroupName="Volume Profile Information")]
		public bool _showTotalVolumeInfo
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Delta", Order=1, GroupName="Volume Profile Information")]
		public bool _showDeltaInfo
		{ get; set; }
		
		#endregion
	}
}


// --- SOURCE: CumulativeDelta.cs ---
// !- El enum va en el namespace GLOBAL, igual que $_OrderFlowEnums y
// $_VolumeAnalysisProfileEnums. Motivo: NinjaTrader genera wrappers de este indicador
// tambien dentro de los namespaces MarketAnalyzerColumns y Strategies, y ahi escribe el
// tipo del parametro SIN cualificar. Si el enum vive dentro de .Indicators, esas dos
// copias no lo encuentran => CS0246 en cada compilacion.
public enum CumulativeDeltaReset
{
	Never,
	Session
}

namespace NinjaTrader.NinjaScript.Indicators
{
	// !- Delta acumulado calculado tick a tick.
	//
	// A diferencia de $FofAggressionDelta (que hace "if(IsTickReplays[0]) return;" y se apaga
	// justo en el grafico donde hace falta), este indicador esta pensado PARA Tick Replay:
	// clasifica cada trade contra el bid/ask del momento y acumula el resultado.
	//
	// Requiere Tick Replay activado en la Data Series para tener historico; sin el solo
	// se llenan las barras formadas despues de cargar el grafico. Se avisa en el panel.
	public class CumulativeDelta : Indicator
	{
		// !- delta de CADA barra, indexado por barra. Se rellena en OnMarketData y se
		// acumula en OnBarUpdate, asi el recalculo en cada tick sigue siendo correcto.
		private Series<double> barDelta;
		private bool isTickReplay;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= @"Cumulative delta (tick by tick). Works with Tick Replay.";
				Name						= "Cumulative Delta (Wyckoff)";
				Calculate					= Calculate.OnEachTick;
				IsOverlay					= false;
				DisplayInDataBox			= true;
				DrawOnPricePanel			= false;
				DrawHorizontalGridLines		= true;
				DrawVerticalGridLines		= true;
				PaintPriceMarkers			= true;
				ScaleJustification			= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				IsSuspendedWhileInactive	= false;
				BarsRequiredToPlot			= 0;

				_Reset						= CumulativeDeltaReset.Session;
				_ShowZeroLine				= true;
				_ZeroLineColor				= Brushes.DimGray;

				AddPlot(new Stroke(Brushes.DodgerBlue, DashStyleHelper.Solid, 2), PlotStyle.Line, "CumulativeDelta");
			}
			else if (State == State.Configure)
			{
				Calculate = Calculate.OnEachTick;
			}
			else if (State == State.DataLoaded)
			{
				barDelta = new Series<double>(this, MaximumBarsLookBack.Infinite);
				isTickReplay = IsTickReplays != null && IsTickReplays.Length > 0 && IsTickReplays[0] == true;
			}
		}

		protected override void OnMarketData(MarketDataEventArgs e)
		{
			if (e.MarketDataType != MarketDataType.Last)
				return;
			if (barDelta == null || CurrentBar < 0)
				return;

			// !- clasificacion estandar: trade al ask = compra agresiva, al bid = venta agresiva.
			// Los trades entre el spread no se cuentan a ningun lado.
			if (e.Price >= e.Ask)
				barDelta[0] = barDelta[0] + e.Volume;
			else if (e.Price <= e.Bid)
				barDelta[0] = barDelta[0] - e.Volume;
		}

		protected override void OnBarUpdate()
		{
			if (barDelta == null)
				return;
			if (!barDelta.IsValidDataPointAt(CurrentBar))
				barDelta[0] = 0;

			// !- reinicio por sesion: la barra de apertura arranca de cero
			bool resetHere = (CurrentBar == 0)
				|| (_Reset == CumulativeDeltaReset.Session && Bars.IsFirstBarOfSession);

			if (resetHere)
				Value[0] = barDelta[0];
			else
				Value[0] = Value[1] + barDelta[0];
		}

		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			base.OnRender(chartControl, chartScale);
			if (IsInHitTest || RenderTarget == null || chartScale == null)
				return;

			// !- linea de cero: separa presion compradora de vendedora de un vistazo
			if (_ShowZeroLine)
			{
				float y = chartScale.GetYByValue(0);
				using (SharpDX.Direct2D1.SolidColorBrush b = new SharpDX.Direct2D1.SolidColorBrush(
						RenderTarget, NinjaTrader.NinjaScript.AddOns.WyckoffRenderUtils
							.WyckoffRenderControl.BrushToColor(_ZeroLineColor)))
				{
					RenderTarget.DrawLine(
						new SharpDX.Vector2(ChartPanel.X, y),
						new SharpDX.Vector2(ChartPanel.X + ChartPanel.W, y), b, 1f);
				}
			}

			if (!isTickReplay)
			{
				using (SharpDX.DirectWrite.TextFormat tf = new SharpDX.DirectWrite.TextFormat(
						NinjaTrader.Core.Globals.DirectWriteFactory, "Arial", 12f))
				using (SharpDX.Direct2D1.SolidColorBrush b = new SharpDX.Direct2D1.SolidColorBrush(
						RenderTarget, SharpDX.Color.Orange))
				{
					RenderTarget.DrawText(
						"Cumulative Delta: Tick Replay is OFF - historical delta is empty.",
						tf,
						new SharpDX.RectangleF(ChartPanel.X + 8, ChartPanel.Y + 6, ChartPanel.W - 16, 24),
						b);
				}
			}
		}

		#region Properties

		[NinjaScriptProperty]
		[Display(Name = "Reset", Order = 0, GroupName = "Cumulative delta")]
		public CumulativeDeltaReset _Reset
		{ get; set; }

		[Display(Name = "Show zero line", Order = 1, GroupName = "Cumulative delta")]
		public bool _ShowZeroLine
		{ get; set; }

		[XmlIgnore]
		[Display(Name = "Zero line color", Order = 2, GroupName = "Cumulative delta")]
		public Brush _ZeroLineColor
		{ get; set; }
		[Browsable(false)]
		public string _ZeroLineColorSerializable
		{
			get { return Serialize.BrushToString(_ZeroLineColor); }
			set { _ZeroLineColor = Serialize.StringToBrush(value); }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> CumulativeDeltaValues
		{
			get { return Values[0]; }
		}

		#endregion
	}
}

