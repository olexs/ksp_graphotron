using System;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace olexlib
{
	public class ModuleEnviroSensorPlotter : PartModule
	{
		[KSPField]
		private bool isWindowShownMain = false;

		[KSPField]
		private bool isWindowShownSources = false;

		[KSPField]
		private bool isWindowShownOptions = false;

		[KSPField]
		private bool isPlotting = false;
		[KSPField]
		private bool printMinMax = true;
		[KSPField]
		private bool printLegend = false;

		[KSPField]
		private float plotDelay = 0.5f;

		[KSPField (isPersistant = false)]
		public float powerConsumption = 0.02f;

		[KSPField]
		private bool activateOnStaging = false;

		[KSPField]
		private int dataPoints = 256;
		[KSPField]
		private int chartHeight = 128;
		[KSPField]
		private bool largeFont = true;

		private float lastPlotted = -1f;
		private int lastObservedStage = -1;

		private string strPlotDelay;
		private string strChartHeight;
		private string strDataPoints;
		
		private List<DataSource> sources = new List<DataSource>();

		private Dictionary<char, bool[]> fontSmall = new Dictionary<char, bool[]>();
		private Dictionary<char, bool[]> fontLarge = new Dictionary<char, bool[]>();

		private Rect windowPositionMain;
		private Rect windowPositionSources;
		private Rect windowPositionOptions;
		private Dictionary<string, bool> collapsedCategories = new Dictionary<string, bool>();

		Texture2D plot;


		private int getFontHeight() {
			return largeFont ? 7 : 5;
		}

		private int getFontWidth() {
			return largeFont ? 5 : 3;
		}

		private int getChartWidth () {
			return dataPoints 
				+ (printMinMax ? 10 * (getFontWidth()+1) : 0)
				+ getLegendWidth();
		}

		private int getLongestLabelLength () {
			int max = 0;
			foreach (DataSource src in sources)
				if (src.isActive && src.name.Length > max)
					max = src.name.Length;
			return max;
		}

		private int getLegendWidth ()
		{
			return printLegend && largeFont ? getLongestLabelLength() * (getFontWidth()+1) + 2 : 0;
		}


		public ModuleEnviroSensorPlotter ()
		{
			plot = new Texture2D (getChartWidth (), chartHeight, TextureFormat.ARGB32, false);

			windowPositionMain = new Rect (Screen.width * 0.65f, 30, 10, 10);
			windowPositionSources = new Rect (Screen.width * 0.65f, 320, 10, 10);
			windowPositionOptions = new Rect (Screen.width * 0.65f - 220, 320, 10, 10);

			// clear texture
			for (int x = 0; x < getChartWidth(); x++)
				for (int y = 0; y < chartHeight; y++)
					plot.SetPixel(x,y,XKCDColors.Black);
			plot.Apply();

			strPlotDelay = plotDelay.ToString();
			strChartHeight = chartHeight.ToString();
			strDataPoints = dataPoints.ToString();

			// initialize the "fonts"
			fontSmall['0'] = new bool[] { 
				true, 	true, 	true,
				true,	false,	true,
				true,	false,	true,
				true,	false,	true,
				true, 	true, 	true
			};
			fontSmall['1'] = new bool[] { 
				false,	true,	false,
				false,	true,	false,
				false,	true,	false,
				false,	true,	false,
				false,	true,	false
			};
			fontSmall['2'] = new bool[] { 
				true, 	true, 	true,
				false,	false,	true,
				true,	true,	true,
				true,	false,	false,
				true, 	true, 	true
			};
			fontSmall['3'] = new bool[] { 
				true, 	true, 	true,
				false,	false,	true,
				false,	true,	true,
				false,	false,	true,
				true, 	true, 	true
			};
			fontSmall['4'] = new bool[] { 
				true, 	false, 	true,
				true,	false,	true,
				true,	true,	true,
				false,	false,	true,
				false, 	false, 	true
			};
			fontSmall['5'] = new bool[] { 
				true, 	true, 	true,
				true,	false,	false,
				true,	true,	true,
				false,	false,	true,
				true, 	true, 	true
			};
			fontSmall['6'] = new bool[] { 
				true, 	true, 	true,
				true,	false,	false,
				true,	true,	true,
				true,	false,	true,
				true, 	true, 	true
			};
			fontSmall['7'] = new bool[] { 
				true, 	true, 	true,
				false,	false,	true,
				false,	false,	true,
				false,	false,	true,
				false, 	false, 	true
			};
			fontSmall['8'] = new bool[] { 
				true, 	true, 	true,
				true,	false,	true,
				true,	true,	true,
				true,	false,	true,
				true, 	true, 	true
			};
			fontSmall['9'] = new bool[] { 
				true, 	true, 	true,
				true,	false,	true,
				true,	true,	true,
				false,	false,	true,
				true, 	true, 	true
			};
			fontSmall['.'] = new bool[] { 
				false, 	false, 	false,
				false,	false,	false,
				false,	false,	false,
				false,	false,	false,
				false, 	true, 	false
			};
			fontSmall['-'] = new bool[] { 
				false, 	false, 	false,
				false,	false,	false,
				false,	true,	true,
				false,	false,	false,
				false, 	false, 	false
			};
			fontSmall[' '] = new bool[] { 
				false, 	false, 	false,
				false,	false,	false,
				false,	false,	false,
				false,	false,	false,
				false, 	false, 	false
			};

			fontLarge = FontManager.LargeFont.CharDict;
		}

		[KSPEvent(guiActive = true, guiName = "Show/Hide Graphotron UI")]
		public void TogglePlotterUI ()
		{
			isWindowShownMain = !isWindowShownMain;
		}

		[KSPAction("Toggle Graphotron UI")]
		public void ActionToggleUI (KSPActionParam param) {
			TogglePlotterUI();
		}

		[KSPAction("Toggle plotting")]
		public void ActionTogglePlotting (KSPActionParam param) {
			isPlotting = !isPlotting;
		}

		public override void OnStart (StartState state)
		{
			base.OnStart (state);

			if (state == StartState.Editor)
				return;

			RenderingManager.AddToPostDrawQueue (3, DrawGUI);

			lastObservedStage = vessel.currentStage;

			// add basic sensors to list
			sources.Add (new VesselInfoSource ("Flight data", "Velocity (surface)", 		() => (float)vessel.srf_velocity.magnitude));
			sources.Add (new VesselInfoSource ("Flight data", "Velocity (orbit)", 			() => (float)vessel.obt_velocity.magnitude));
			sources.Add (new VesselInfoSource ("Flight data", "Altitude (surface)", 		() => vessel.heightFromTerrain));
			sources.Add (new VesselInfoSource ("Flight data", "Altitude (above sea level)",	() => (float)vessel.altitude));
			sources.Add (new VesselInfoSource ("Flight data", "Dynamic pressure (q)", 		
			                                   () => (float)(vessel.srf_velocity.magnitude * vessel.srf_velocity.magnitude * vessel.atmDensity * 0.5)));

			sources.Add (new VesselInfoSource ("Flight data", "Total vehicle mass",			() => (float)vessel.GetTotalMass()));
			sources.Add (new VesselInfoSource ("Flight data", "Angle of attack",			() => Vector3.Angle(vessel.transform.up, vessel.srf_velocity)));

			// find available sensors
			foreach (Part part in this.part.vessel.parts.Where (p => p.Modules.OfType<ModuleEnviroSensor> ().Any ())) {
				sources.Add (new SensorSource (part));
			}

			// add resource data sources
			List<string> resourceNames = new List<string> ();
			foreach (Part p in vessel.parts) {
				foreach (PartResource pr in p.Resources) {
					if (!resourceNames.Contains (pr.resourceName))
						resourceNames.Add (pr.resourceName);
				}
			}
			resourceNames.Sort ();
			foreach (string resource in resourceNames)
				sources.Add (new ResourceSource (resource, vessel));

			// optional plugin data integration
			/*Type mechjebCoreType = Type.GetType ("MuMechLib.MuMech.MechJebCore");
			if (mechjebCoreType != null)
				MonoBehaviour.print("Graphotron: Mechjeb found!");
			else
				MonoBehaviour.print("Graphotron: no Mechjeb found");*/

			this.part.force_activate();
		}



		public override void OnUpdate ()
		{
			base.OnUpdate ();

			if (vessel.currentStage != lastObservedStage && activateOnStaging) {
				isPlotting = true;
				activateOnStaging = false;
				lastObservedStage = vessel.currentStage;
			}
			
			// power consumption
			if (isPlotting) {
				float powerDemand = powerConsumption * TimeWarp.deltaTime;
				float powerAmount = part.RequestResource ("ElectricCharge", powerDemand);
				if (powerAmount < powerDemand * 0.9) {
					MonoBehaviour.print("Graphotron: plotting stopped, need more power! (demand: "+powerDemand+", amount: "+powerAmount+")");
					isPlotting = false;
				}
			}

			if (isPlotting && lastPlotted <= Time.timeSinceLevelLoad - plotDelay) {
				// check texture size
				if (plot.width != getChartWidth() || plot.height != chartHeight)
					plot = new Texture2D(getChartWidth(), chartHeight, TextureFormat.ARGB32, false);

				// clear texture
				for (int x = 0; x < getChartWidth(); x++)
					for (int y = 0; y < chartHeight; y++)
						plot.SetPixel(x,y,XKCDColors.Black);

				int sensorNo = -1;

				foreach (DataSource source in sources) {
					// acquire current data
					source.fetchData();
					source.trimData(dataPoints);

					// plot data to texture
					if (source.isActive) {
						sensorNo++;

						float minValue = source.getMinValue();
						float maxValue = source.getMaxValue();

						// graph
						int x = getChartWidth() - getLegendWidth() - source.data.Count; // offset starting point
						foreach (float dataPoint in source.data) {
							if (dataPoint != float.NaN) {
								int y = 4 + (int)Mathf.Round(((dataPoint - minValue) / (maxValue - minValue)) * (chartHeight - 8));
								plot.SetPixel(x, y, source.color);
							}
							x++;
						}

						// legend
						if (printLegend && largeFont) {
							renderText(source.name, 
							           getChartWidth() - getLegendWidth() + 2, 
							           chartHeight - getFontHeight() - 2 - sensorNo * (getFontHeight() + 1), 
							           source.color);
						}

						// min/max values, formatting and positioning
						if (printMinMax) {
							string strMin = string.Format("{0,10:######0.00}", minValue);
							int startX = 0;
							int startY = 1 + sensorNo * (getFontHeight() + 1);
							renderText(strMin, startX, startY, source.color);

							string strMax = string.Format("{0,10:######0.00}", maxValue);
							startY = chartHeight - getFontHeight() - 2 - sensorNo * (getFontHeight() + 1);
							renderText(strMax, startX, startY, source.color);
						}
					}

				}

				plot.Apply();

				lastPlotted = Time.timeSinceLevelLoad;
			}

		}

		private void renderText (string text, int x, int y, Color color)
		{
			// rendertron 2000
			for (int c = 0; c < text.Length; c++) {
				int cx = x + c * (getFontWidth()+1);
				for (int p = 0; p < getFontWidth() * getFontHeight(); p++) {
					if ((!largeFont && fontSmall[text[c]][p]) || (largeFont && fontLarge[text[c]][p])) {
						int px = cx + p % getFontWidth();
						int py = y + getFontHeight() - p / getFontWidth();
						plot.SetPixel(px, py, color);
					}
				}
			}
		}

		void CSVExport ()
		{
			var csvfile = KSP.IO.File.CreateText<ModuleEnviroSensorPlotter>(DateTime.Now.ToString ("yyyy-MM-dd hhmmss") + ".csv", null);
			
			// output csv header and prefetch data into arrays
			string header = "Data point";
			int exportDataPoints = dataPoints;
			Dictionary<int, float[]> arrayData = new Dictionary<int, float[]>();
			foreach (DataSource source in sources) {
				if (!source.isActive)
					continue;
				int sensorID = source.GetHashCode();
				header += "\t" + source.name;
				arrayData[sensorID] = source.data.ToArray();
				if (arrayData[sensorID].Length < exportDataPoints)
					exportDataPoints = arrayData[sensorID].Length;
			}
			header += "\n";
			csvfile.Write(header);
			
			// output data
			for (int i = 0; i < exportDataPoints; i++) {
				string line = i.ToString();
				foreach (DataSource source in sources) {
					if (!source.isActive)
						continue;
					int sensorID = source.GetHashCode();
					line += "\t" + arrayData[sensorID][i].ToString();
				}
				line += "\n";
				csvfile.Write(line);
			}

			csvfile.Close();
		}
		
		private void DrawGUI ()
		{
			if (isWindowShownMain && this.part.State == PartStates.ACTIVE) {
				GUI.skin = HighLogic.Skin;
				windowPositionMain = GUILayout.Window (423595, 
			                                  windowPositionMain, 
			                                  DrawWindowMain, 
			                                  "Graphotron 2000", 
			                                  GUILayout.Width (getChartWidth() + 4),
				                              GUILayout.Height(chartHeight + 100));
				if (isWindowShownSources) {
					windowPositionSources = GUILayout.Window (423596, 
															  windowPositionSources, 
					                                          DrawWindowSources,
					                                          "Graphotron Sources", 
					                                          GUILayout.Width (280),
					                                          GUILayout.Height (20));
				}
				if (isWindowShownOptions) {
					windowPositionOptions = GUILayout.Window (423594, 
					                                          windowPositionOptions, 
					                                          DrawWindowOptions,
					                                          "Graphotron Options", 
					                                          GUILayout.MinWidth (200),
					                                          GUILayout.MinHeight (20));
				}
			}
		}

		private string GetSourcesButtonText ()
		{
			return string.Format("{0} sources selected", sources.Sum(src => src.isActive ? 1 : 0));
		}

		private void DrawWindowMain (int windowID)
		{
			int halfWidth = getChartWidth () / 2 - 2;

			GUILayout.BeginVertical (GUILayout.Width (getChartWidth ()));

			GUILayout.Box (plot, GUILayout.Width (getChartWidth ()));

			GUILayout.BeginHorizontal ();
			isPlotting = GUILayout.Toggle (isPlotting, "Draw plot", new GUIStyle (GUI.skin.button), GUILayout.Width (halfWidth));
			if (GUILayout.Button ("Reset plot", GUILayout.Width (halfWidth))) {
				//plotData = new Dictionary<int, LinkedList<float>>();
				foreach(DataSource src in sources)
					src.clearData();
			}
			GUILayout.EndHorizontal ();

			GUILayout.BeginHorizontal ();
			isWindowShownSources = GUILayout.Toggle (isWindowShownSources, this.GetSourcesButtonText (), new GUIStyle (GUI.skin.button), GUILayout.Width (halfWidth));
			isWindowShownOptions = GUILayout.Toggle (isWindowShownOptions, "Options", new GUIStyle (GUI.skin.button), GUILayout.Width (halfWidth));
			GUILayout.EndHorizontal ();

			GUILayout.BeginHorizontal ();
			if (GUILayout.Button ("Save to PNG", GUILayout.Width (halfWidth))) {
				var pbytes = plot.EncodeToPNG ();
				KSP.IO.File.WriteAllBytes<ModuleEnviroSensorPlotter> (pbytes, DateTime.Now.ToString ("yyyy-MM-dd hhmmss") + ".png", null);
			}

			if (GUILayout.Button ("Save to CSV", GUILayout.Width (halfWidth))) {
				this.CSVExport();
			}
			GUILayout.EndHorizontal ();

			GUILayout.EndVertical();

			GUI.DragWindow(new Rect(0, 0, getChartWidth() + 4, 60));
		}

		private void DrawWindowSources (int windowID)
		{
			GUILayout.BeginHorizontal ();
			GUILayout.BeginVertical();
			
			Color baseColor = GUI.color;
			string lastGroup = "";
			int count = 0;
			foreach (DataSource source in sources) {
				if (source.group != lastGroup) {
					GUI.color = baseColor;

					if (!collapsedCategories.ContainsKey(source.group))
						collapsedCategories[source.group] = false;

					GUILayout.BeginHorizontal (GUILayout.Width(285));
					GUILayout.Label(source.group, GUILayout.Width(255));
					collapsedCategories[source.group] = !GUILayout.Toggle (!collapsedCategories[source.group], 
					                                                      collapsedCategories[source.group] ? "+" : "-", 
					                                                      new GUIStyle (GUI.skin.button), 
					                                                      GUILayout.Width (20),
					                                                      GUILayout.Height(20));
					GUILayout.EndHorizontal ();

					lastGroup = source.group;
					count++;
				}

				if (!collapsedCategories[source.group]) {
					GUILayout.BeginHorizontal (GUILayout.Width(285));
					GUI.color = source.isActive ? source.color : baseColor;
					source.setActive(GUILayout.Toggle(source.isActive, source.name, GUILayout.Width(230)));
					if (source.isActive) {
						if (GUILayout.Button ("Color", GUILayout.Width(45), GUILayout.Height(20)))
							source.setNextColor();
					}
					GUILayout.EndHorizontal ();
				}

				count++;
				if (count >= 100) {
					GUILayout.EndVertical();
					GUILayout.BeginVertical();
					count = 0;
					lastGroup = "";
				}
			}
			GUI.color = baseColor;
			
			GUILayout.EndVertical();
			GUILayout.EndHorizontal ();

			GUI.DragWindow(new Rect(0, 0, 300, 60));
		}

		private void DrawWindowOptions (int WindowID)
		{
			GUILayout.BeginVertical();

			largeFont = GUILayout.Toggle (largeFont, "Use large font");
			printMinMax = GUILayout.Toggle (printMinMax, "Print min/max values");
			if (!largeFont)
				GUI.enabled = false;
			printLegend = GUILayout.Toggle (printLegend && largeFont, "Print legend");
			GUI.enabled = true;

			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Resolution: ", GUILayout.Width (80f));
			strPlotDelay = GUILayout.TextField (strPlotDelay, GUILayout.Width (60f));
			float.TryParse (Regex.Replace (strPlotDelay, @"([0-9]+\.?[0-9]*).*", "$1"), out plotDelay);
			GUILayout.Label ("s");
			GUILayout.EndHorizontal ();
			
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Data points: ", GUILayout.Width (80f));
			strDataPoints = GUILayout.TextField (strDataPoints, GUILayout.Width (80f));
			int.TryParse (Regex.Replace (strDataPoints, @"([0-9]+).*", "$1"), out dataPoints);
			if (dataPoints < 256) {
				dataPoints = 256;
				GUILayout.Label ("min.256!");
			}
			GUILayout.EndHorizontal ();

			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Plot height: ", GUILayout.Width (80f));
			strChartHeight = GUILayout.TextField (strChartHeight, GUILayout.Width (80f));
			int.TryParse (Regex.Replace (strChartHeight, @"([0-9]+).*", "$1"), out chartHeight);
			if (chartHeight < 128) {
				chartHeight = 128;
				GUILayout.Label ("min.128!");
			}
			GUILayout.EndHorizontal ();

			activateOnStaging = GUILayout.Toggle (activateOnStaging, "Activate on next staging");

			GUILayout.EndVertical();

			GUI.DragWindow(new Rect(0, 0, 280, 60));
		}

	}
}

