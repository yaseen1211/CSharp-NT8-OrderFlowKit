#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
#endregion

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

namespace NinjaTrader.NinjaScript.Indicators.WyckoffZen
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
