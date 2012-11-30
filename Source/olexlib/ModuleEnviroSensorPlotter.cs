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
		private bool isPlotting = false;

		[KSPField]
		private float plotDelay = 0.5f;

		[KSPField (isPersistant = false)]
		public float powerConsumption = 0.02f;

		private float lastPlotted = -1f;
		private string strPlotDelay;
		
		private Dictionary<int, LinkedList<float>> plotData = new Dictionary<int, LinkedList<float>>();

		private SortedDictionary<int, Part> availableSensors = new SortedDictionary<int, Part>();
		private Dictionary<int, bool> activeSensors = new Dictionary<int, bool>();
		private Dictionary<int, Color> sensorColors = new Dictionary<int, Color>();

		private Dictionary<char, bool[]> font = new Dictionary<char, bool[]>();

		private const int DATA_VELOCITY_SURFACE = 1001;
		private const int DATA_VELOCITY_ORBIT = 1002;
		private const int DATA_ALTITUDE = 1003;
		private const int DATA_ALTITUDE_ASL = 1004;
		private const int DATA_DYNAMIC_PRESSURE = 1005;

		private Rect windowPositionMain;
		private Rect windowPositionSources;

		Texture2D plot = new Texture2D(296, 128, TextureFormat.ARGB32, false);

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

		public ModuleEnviroSensorPlotter ()
		{
			windowPositionMain = new Rect(Screen.width * 0.65f, 30, 10, 10);
			windowPositionSources = new Rect(Screen.width * 0.65f, 320, 10, 10);

			sensorColors[DATA_VELOCITY_SURFACE] = XKCDColors.Red;
			sensorColors[DATA_VELOCITY_ORBIT] = XKCDColors.Orange;
			sensorColors[DATA_ALTITUDE] = XKCDColors.Lime;
			sensorColors[DATA_ALTITUDE_ASL] = XKCDColors.Green;
			sensorColors[DATA_DYNAMIC_PRESSURE] = XKCDColors.SkyBlue;

			// clear texture
			for (int x = 0; x < 296; x++)
				for (int y = 0; y < 128; y++)
					plot.SetPixel(x,y,XKCDColors.Black);
			plot.Apply();

			strPlotDelay = plotDelay.ToString();

			// initialize the "font"
			font['0'] = new bool[] { 
				true, 	true, 	true,
				true,	false,	true,
				true,	false,	true,
				true,	false,	true,
				true, 	true, 	true
			};
			font['1'] = new bool[] { 
				false,	true,	false,
				false,	true,	false,
				false,	true,	false,
				false,	true,	false,
				false,	true,	false
			};
			font['2'] = new bool[] { 
				true, 	true, 	true,
				false,	false,	true,
				true,	true,	true,
				true,	false,	false,
				true, 	true, 	true
			};
			font['3'] = new bool[] { 
				true, 	true, 	true,
				false,	false,	true,
				false,	true,	true,
				false,	false,	true,
				true, 	true, 	true
			};
			font['4'] = new bool[] { 
				true, 	false, 	true,
				true,	false,	true,
				true,	true,	true,
				false,	false,	true,
				false, 	false, 	true
			};
			font['5'] = new bool[] { 
				true, 	true, 	true,
				true,	false,	false,
				true,	true,	true,
				false,	false,	true,
				true, 	true, 	true
			};
			font['6'] = new bool[] { 
				true, 	true, 	true,
				true,	false,	false,
				true,	true,	true,
				true,	false,	true,
				true, 	true, 	true
			};
			font['7'] = new bool[] { 
				true, 	true, 	true,
				false,	false,	true,
				false,	false,	true,
				false,	false,	true,
				false, 	false, 	true
			};
			font['8'] = new bool[] { 
				true, 	true, 	true,
				true,	false,	true,
				true,	true,	true,
				true,	false,	true,
				true, 	true, 	true
			};
			font['9'] = new bool[] { 
				true, 	true, 	true,
				true,	false,	true,
				true,	true,	true,
				false,	false,	true,
				true, 	true, 	true
			};
			font['.'] = new bool[] { 
				false, 	false, 	false,
				false,	false,	false,
				false,	false,	false,
				false,	false,	false,
				false, 	true, 	false
			};
			font['-'] = new bool[] { 
				false, 	false, 	false,
				false,	false,	false,
				false,	true,	true,
				false,	false,	false,
				false, 	false, 	false
			};
			font[' '] = new bool[] { 
				false, 	false, 	false,
				false,	false,	false,
				false,	false,	false,
				false,	false,	false,
				false, 	false, 	false
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
				// clear texture
				for (int x = 0; x < 296; x++)
					for (int y = 0; y < 128; y++)
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
						/*if (float.TryParse(cleanedValue, out currentReading)) {
							MonoBehaviour.print("Read " + availableSensors[sensorID].partInfo.title + ": " + sensor.readoutInfo + " -> " + currentReading);
						} else {
							MonoBehaviour.print("Failed reading " + availableSensors[sensorID].partInfo.title + ": " + sensor.readoutInfo);
						}*/
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

					// remove oldest entry
					if (sensorData.Count > 256)
						sensorData.RemoveFirst();

					// plot data to texture
					plotData[sensorID] = sensorData;
					if (activeSensors[sensorID]) {
						sensorNo++;

						// graph
						int x = 296 - sensorData.Count; // offset starting point
						foreach (float dataPoint in sensorData) {
							if (dataPoint != float.NaN) {
								int y = 4 + (int)Mathf.Round(((dataPoint - minValue) / (maxValue - minValue)) * 120);
								plot.SetPixel(x, y, sensorColors[sensorID]);
							}
							x++;
						}

						// min/max labels, formatting and positioning
						string strMin = string.Format("{0,10:######0.00}", minValue);
						for (int c = 0; c < strMin.Length; c++) {
							int startY = 1 + sensorNo * 6;
							int startX = c * 4;
							for (int p = 0; p < 15; p++) {
								// font rendertron 2000
								if (font[strMin[c]][p]) {
									int px = startX + p % 3;
									int py = startY + 5 - p / 3;
									plot.SetPixel(px, py, sensorColors[sensorID]);
								}
							}
						}

						string strMax = string.Format("{0,10:######0.00}", maxValue);
						for (int c = 0; c < strMax.Length; c++) {
							int startY = 128 - 7 - sensorNo * 6;
							int startX = c * 4;
							for (int p = 0; p < 15; p++) {
								if (font[strMax[c]][p]) {
									int px = startX + p % 3;
									int py = startY + 5 - p / 3;
									plot.SetPixel(px, py, sensorColors[sensorID]);
								}
							}
						}
					}

				}

				plot.Apply();

				lastPlotted = Time.timeSinceLevelLoad;
			}

		}
		
		private void DrawGUI ()
		{
			if (isWindowShownMain && this.part.State == PartStates.ACTIVE) {
				GUI.skin = HighLogic.Skin;
				windowPositionMain = GUILayout.Window (423595, 
			                                  windowPositionMain, 
			                                  DrawWindowMain, 
			                                  "Graphotron 2000", 
			                                  GUILayout.MinWidth (300), 
			                                  GUILayout.MinHeight (20));
				if (isWindowShownSources) {
					windowPositionSources = GUILayout.Window (423596, 
															  windowPositionSources, 
					                                          DrawWindowSources,
					                                          "Graphotron Sources", 
					                                          GUILayout.MinWidth (300),
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

			GUILayout.BeginVertical (GUILayout.Width(296f));

			GUILayout.Box (plot, GUILayout.Width(296f));

			GUILayout.BeginHorizontal();
			isPlotting = GUILayout.Toggle (isPlotting, "Draw plot", new GUIStyle(GUI.skin.button), GUILayout.Width(146));
			isWindowShownSources = GUILayout.Toggle (isWindowShownSources, this.GetSourcesButtonText(), new GUIStyle(GUI.skin.button), GUILayout.Width(146));
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Resolution: ");
			strPlotDelay = GUILayout.TextField(strPlotDelay, GUILayout.Width(80f));
			float.TryParse(Regex.Replace(strPlotDelay, @"([0-9]+\.?[0-9]*).*", "$1"), out plotDelay);
			GUILayout.Label("s");
			GUILayout.EndHorizontal();

			if (GUILayout.Button ("Save to PNG", GUILayout.Width(296f))) {
				var pbytes = plot.EncodeToPNG();
				KSP.IO.File.WriteAllBytes<ModuleEnviroSensorPlotter>(pbytes, DateTime.Now.ToString("yyyy-MM-dd hhmmss") + ".png", null);
			}

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

	}
}

