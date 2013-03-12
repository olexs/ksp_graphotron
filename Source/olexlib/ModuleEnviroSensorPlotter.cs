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
		private float plotDelay = 0.5f;

		[KSPField (isPersistant = false)]
		public float powerConsumption = 0.02f;

		[KSPField]
		private int dataPoints = 256;
		[KSPField]
		private int chartHeight = 128;
		[KSPField]
		private bool largeFont = false;

		private float lastPlotted = -1f;
		private string strPlotDelay;
		private string strChartHeight;
		private string strDataPoints;
		
		private Dictionary<int, LinkedList<float>> plotData = new Dictionary<int, LinkedList<float>>();

		private SortedDictionary<int, Part> availableSensors = new SortedDictionary<int, Part>();
		private Dictionary<int, bool> activeSensors = new Dictionary<int, bool>();
		private Dictionary<int, Color> sensorColors = new Dictionary<int, Color>();
		private Dictionary<int, string> sensorNames = new Dictionary<int, string>();

		private Dictionary<char, bool[]> fontSmall = new Dictionary<char, bool[]>();
		private Dictionary<char, bool[]> fontLarge = new Dictionary<char, bool[]>();

		private const int DATA_VELOCITY_SURFACE = 1001;
		private const int DATA_VELOCITY_ORBIT = 1002;
		private const int DATA_ALTITUDE = 1003;
		private const int DATA_ALTITUDE_ASL = 1004;
		private const int DATA_DYNAMIC_PRESSURE = 1005;

		private Rect windowPositionMain;
		private Rect windowPositionSources;
		private Rect windowPositionOptions;

		Texture2D plot;

		private static UnityEngine.Color[] graphColors = {
			XKCDColors.Yellow,
			XKCDColors.White,
			XKCDColors.Pink,
			XKCDColors.Beige,
			XKCDColors.Purple,
			XKCDColors.Yellow,
			XKCDColors.White,
			XKCDColors.Pink,
			XKCDColors.Beige,
			XKCDColors.Purple,
			XKCDColors.Yellow,
			XKCDColors.White,
			XKCDColors.Pink,
			XKCDColors.Beige,
			XKCDColors.Purple
		};

		private int getFontHeight() {
			return largeFont ? 7 : 5;
		}

		private int getFontWidth() {
			return largeFont ? 5 : 3;
		}

		private int getChartWidth () {
			return dataPoints + 10 * (getFontWidth()+1);
		}

		public ModuleEnviroSensorPlotter ()
		{
			plot = new Texture2D(getChartWidth(), chartHeight, TextureFormat.ARGB32, false);

			windowPositionMain = new Rect(Screen.width * 0.65f, 30, 10, 10);
			windowPositionSources = new Rect(Screen.width * 0.65f, 320, 10, 10);
			windowPositionOptions = new Rect(Screen.width * 0.65f - 220, 320, 10, 10);

			sensorColors[DATA_VELOCITY_SURFACE] = XKCDColors.Red;
			sensorColors[DATA_VELOCITY_ORBIT] = XKCDColors.Orange;
			sensorColors[DATA_ALTITUDE] = XKCDColors.Lime;
			sensorColors[DATA_ALTITUDE_ASL] = XKCDColors.Green;
			sensorColors[DATA_DYNAMIC_PRESSURE] = XKCDColors.SkyBlue;

			sensorNames[DATA_VELOCITY_SURFACE] = "Velocity (surface)";
			sensorNames[DATA_VELOCITY_ORBIT] = "Velocity (orbit)";
			sensorNames[DATA_ALTITUDE] = "Altitude (surface)";
			sensorNames[DATA_ALTITUDE_ASL] = "Altitude (above sea level)";
			sensorNames[DATA_DYNAMIC_PRESSURE] = "Dynamic pressure (q)";

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

			fontLarge['0'] = new bool[] { 
				false, 	true, 	true, 	true, 	false,
				true,	false,	false,	false,	true,
				true,	false,	false,	true,	true,
				true,	false,	true,	false,	true,
				true,	true,	false,	false,	true,
				true,	false,	false,	false,	true,
				false, 	true, 	true, 	true, 	false
			};
			fontLarge['1'] = new bool[] { 
				false, 	false, 	true, 	false, 	false,
				false,	true,	true,	false,	false,
				false,	false,	true,	false,	false,
				false,	false,	true,	false,	false,
				false,	false,	true,	false,	false,
				false,	false,	true,	false,	false,
				false, 	true, 	true, 	true, 	false
			};
			fontLarge['2'] = new bool[] { 
				false, 	true, 	true, 	true, 	false,
				true,	false,	false,	false,	true,
				false,	false,	false,	false,	true,
				false,	false,	false,	true,	false,
				false,	false,	true,	false,	false,
				false,	true,	false,	false,	false,
				true, 	true, 	true, 	true, 	true
			};
			fontLarge['3'] = new bool[] { 
				true, 	true, 	true, 	true, 	true,
				false,	false,	false,	true,	false,
				false,	false,	true,	false,	false,
				false,	false,	false,	true,	false,
				false,	false,	false,	false,	true,
				true,	false,	false,	false,	true,
				false, 	true, 	true, 	true, 	false
			};
			fontLarge['4'] = new bool[] { 
				false, 	false, 	false, 	true, 	false,
				false,	false,	true,	true,	false,
				false,	true,	false,	true,	false,
				true,	false,	false,	true,	false,
				true,	true,	true,	true,	true,
				false,	false,	false,	true,	false,
				false, 	false, 	false, 	true, 	false
			};
			fontLarge['5'] = new bool[] { 
				true, 	true, 	true, 	true, 	true,
				true,	false,	false,	false,	false,
				true,	true,	true,	true,	false,
				false,	false,	false,	false,	true,
				false,	false,	false,	false,	true,
				true,	false,	false,	false,	true,
				false, 	true, 	true, 	true, 	false
			};
			fontLarge['6'] = new bool[] { 
				false, 	false, 	true, 	true, 	false,
				false,	true,	false,	false,	false,
				true,	false,	false,	false,	false,
				true,	true,	true,	true,	false,
				true,	false,	false,	false,	true,
				true,	false,	false,	false,	true,
				false, 	true, 	true, 	true, 	false
			};
			fontLarge['7'] = new bool[] { 
				true, 	true, 	true, 	true, 	true,
				false,	false,	false,	false,	true,
				false,	false,	false,	true,	false,
				false,	false,	true,	false,	false,
				false,	true,	false,	false,	false,
				false,	true,	false,	false,	false,
				false, 	true, 	false, 	false, 	false
			};
			fontLarge['8'] = new bool[] { 
				false, 	true, 	true, 	true, 	false,
				true,	false,	false,	false,	true,
				true,	false,	false,	false,	true,
				false,	true,	true,	true,	false,
				true,	false,	false,	false,	true,
				true,	false,	false,	false,	true,
				false, 	true, 	true, 	true, 	false
			};
			fontLarge['9'] = new bool[] { 
				false, 	true, 	true, 	true, 	false,
				true,	false,	false,	false,	true,
				true,	false,	false,	false,	true,
				false,	true,	true,	true,	true,
				false,	false,	false,	false,	true,
				false,	false,	false,	true,	false,
				false, 	true, 	true, 	false, 	false
			};
			fontLarge['.'] = new bool[] { 
				false, 	false, 	false, 	false, 	false,
				false,	false,	false,	false,	false,
				false,	false,	false,	false,	false,
				false,	false,	false,	false,	false,
				false,	false,	false,	false,	false,
				false,	true,	true,	false,	false,
				false, 	true, 	true, 	false, 	false
			};
			fontLarge['-'] = new bool[] { 
				false, 	false, 	false, 	false, 	false,
				false,	false,	false,	false,	false,
				false,	false,	false,	false,	false,
				true,	true,	true,	true,	true,
				false,	false,	false,	false,	false,
				false,	false,	false,	false,	false,
				false, 	false, 	false, 	false, 	false
			};
			fontLarge[' '] = new bool[] { 
				false, 	false, 	false, 	false, 	false,
				false,	false,	false,	false,	false,
				false,	false,	false,	false,	false,
				false,	false,	false,	false,	false,
				false,	false,	false,	false,	false,
				false,	false,	false,	false,	false,
				false, 	false, 	false, 	false, 	false
			};
		}

		[KSPEvent(guiActive = true, guiName = "Show/Hide Graphotron UI")]
		public void TogglePlotterUI ()
		{
			isWindowShownMain = !isWindowShownMain;
		}

		public override void OnStart (StartState state)
		{
			base.OnStart (state);
			RenderingManager.AddToPostDrawQueue (3, DrawGUI);

			// find available sensors
			availableSensors.Clear();
			activeSensors.Clear();
			int nextColor = 0;
			foreach (Part part in this.part.vessel.parts.Where (p => p.Modules.OfType<ModuleEnviroSensor> ().Any ())) {
				availableSensors[part.GetInstanceID()] = part;
				activeSensors[part.GetInstanceID()] = false;
				sensorColors[part.GetInstanceID()] = graphColors[nextColor++];
				sensorNames[part.GetInstanceID()] = part.partInfo.title;
			}

			activeSensors[DATA_ALTITUDE] = false;
			activeSensors[DATA_ALTITUDE_ASL] = true;
			activeSensors[DATA_VELOCITY_ORBIT] = false;
			activeSensors[DATA_VELOCITY_SURFACE] = true;
			activeSensors[DATA_DYNAMIC_PRESSURE] = true;

			this.part.force_activate();
		}

		public override void OnUpdate ()
		{
			base.OnUpdate ();

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

				foreach (int sensorID in activeSensors.Keys) {
					// acquire data
					LinkedList<float> sensorData = plotData.ContainsKey(sensorID) ? plotData[sensorID] : new LinkedList<float>();
					float minValue = (sensorData.Count > 0) ? sensorData.Min() : 0f;
					float maxValue = (sensorData.Count > 0) ? sensorData.Max() : 1f;
					float currentReading = 0f;

					if (availableSensors.ContainsKey(sensorID)) {
						// get data from a sensor
						ModuleEnviroSensor sensor = availableSensors[sensorID].Modules.OfType<ModuleEnviroSensor>().First();
						string cleanedValue = Regex.Replace(sensor.readoutInfo, @"([0-9]+\.?[0-9]*).*", "$1");
						float.TryParse(cleanedValue, out currentReading);
					} else {
						// get basic data values
						switch (sensorID) {
						case DATA_ALTITUDE:
							currentReading = vessel.heightFromTerrain;
							break;
						case DATA_ALTITUDE_ASL:
							currentReading = (float)vessel.altitude;
							break;
						case DATA_VELOCITY_ORBIT:
							currentReading = (float)vessel.obt_velocity.magnitude;
							break;
						case DATA_VELOCITY_SURFACE:
							currentReading = (float)vessel.srf_velocity.magnitude;
							break;
						case DATA_DYNAMIC_PRESSURE:
							currentReading = (float)(vessel.srf_velocity.magnitude * vessel.srf_velocity.magnitude * vessel.atmDensity * 0.5);
							break;
						}
					}

					if (currentReading < minValue) 
						minValue = currentReading;
					if (currentReading > maxValue) 
						maxValue = currentReading;
					sensorData.AddLast(currentReading);

					// remove oldest entries until matching plot width
					while (sensorData.Count > dataPoints)
						sensorData.RemoveFirst();

					// plot data to texture
					plotData[sensorID] = sensorData;
					if (activeSensors[sensorID]) {
						sensorNo++;

						// graph
						int x = getChartWidth() - sensorData.Count; // offset starting point
						foreach (float dataPoint in sensorData) {
							if (dataPoint != float.NaN) {
								int y = 4 + (int)Mathf.Round(((dataPoint - minValue) / (maxValue - minValue)) * (chartHeight - 8));
								plot.SetPixel(x, y, sensorColors[sensorID]);
							}
							x++;
						}

						// min/max labels, formatting and positioning
						string strMin = string.Format("{0,10:######0.00}", minValue);
						int startY = 1 + sensorNo * (getFontHeight() + 1);
						renderText(strMin, 0, startY, sensorColors[sensorID]);

						string strMax = string.Format("{0,10:######0.00}", maxValue);
						startY = chartHeight - getFontHeight() - 2 - sensorNo * (getFontHeight() + 1);
						renderText(strMax, 0, startY, sensorColors[sensorID]);
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
			foreach (int sensorID in activeSensors.Keys) {
				if (!activeSensors[sensorID])
					continue;
				header += "\t" + sensorNames[sensorID];
				arrayData[sensorID] = plotData[sensorID].ToArray();
				if (arrayData[sensorID].Length < exportDataPoints)
					exportDataPoints = arrayData[sensorID].Length;
			}
			header += "\n";
			csvfile.Write(header);
			
			// output data
			for (int i = 0; i < exportDataPoints; i++) {
				string line = i.ToString();
				foreach (int sensorID in activeSensors.Keys) {
					if (!activeSensors[sensorID])
						continue;
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
					                                          GUILayout.MinWidth (300),
					                                          GUILayout.MinHeight (20));
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
			return string.Format("{0} sources selected", activeSensors.Sum(kv => kv.Value ? 1 : 0));
		}

		private void DrawWindowMain (int windowID)
		{
			int halfWidth = getChartWidth () / 2 - 2;

			GUILayout.BeginVertical (GUILayout.Width (getChartWidth ()));

			GUILayout.Box (plot, GUILayout.Width (getChartWidth ()));

			GUILayout.BeginHorizontal ();
			isPlotting = GUILayout.Toggle (isPlotting, "Draw plot", new GUIStyle (GUI.skin.button), GUILayout.Width (halfWidth));
			if (GUILayout.Button ("Reset plot", GUILayout.Width (halfWidth))) {
				plotData = new Dictionary<int, LinkedList<float>>();
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

			GUI.DragWindow(new Rect(0, 0, 300, 60));
		}

		private void DrawWindowSources (int windowID)
		{
			GUILayout.BeginVertical();
			
			Color oldColor = GUI.color;
			GUI.color = sensorColors[DATA_VELOCITY_SURFACE];
			activeSensors[DATA_VELOCITY_SURFACE] = GUILayout.Toggle(activeSensors[DATA_VELOCITY_SURFACE], "Velocity (surface)");
			GUI.color = sensorColors[DATA_VELOCITY_ORBIT];
			activeSensors[DATA_VELOCITY_ORBIT] = GUILayout.Toggle(activeSensors[DATA_VELOCITY_ORBIT], "Velocity (orbit)");
			GUI.color = sensorColors[DATA_ALTITUDE];
			activeSensors[DATA_ALTITUDE] = GUILayout.Toggle(activeSensors[DATA_ALTITUDE], "Altitude (surface)");
			GUI.color = sensorColors[DATA_ALTITUDE_ASL];
			activeSensors[DATA_ALTITUDE_ASL] = GUILayout.Toggle(activeSensors[DATA_ALTITUDE_ASL], "Altitude (above sea level)");
			GUI.color = sensorColors[DATA_DYNAMIC_PRESSURE];
			activeSensors[DATA_DYNAMIC_PRESSURE] = GUILayout.Toggle(activeSensors[DATA_DYNAMIC_PRESSURE], "Dynamic pressure (q)");
			
			foreach (Part sensor in availableSensors.Values) {
				GUI.color = sensorColors[sensor.GetInstanceID()];
				activeSensors[sensor.GetInstanceID()] = GUILayout.Toggle(activeSensors[sensor.GetInstanceID()], sensor.partInfo.title);
			}
			GUI.color = oldColor;
			
			GUILayout.EndVertical();

			GUI.DragWindow(new Rect(0, 0, 300, 60));
		}

		private void DrawWindowOptions (int WindowID)
		{
			GUILayout.BeginVertical();

			largeFont = GUILayout.Toggle (largeFont, "Use large font");

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

			GUILayout.EndVertical();

			GUI.DragWindow(new Rect(0, 0, 200, 60));
		}

	}
}

