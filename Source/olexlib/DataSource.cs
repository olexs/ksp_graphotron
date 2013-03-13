using System;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;


namespace olexlib
{
	public abstract class DataSource
	{
		protected static UnityEngine.Color[] graphColors = {
			XKCDColors.Red,
			XKCDColors.Orange,
			XKCDColors.Lime,
			XKCDColors.Green,
			XKCDColors.SkyBlue,
			XKCDColors.Yellow,
			XKCDColors.White,
			XKCDColors.Pink,
			XKCDColors.Beige,
			XKCDColors.Purple
		};
		protected static int nextColor = 0;

		public string group = "";
		public string name = "";
		public Color color = XKCDColors.Black;
		public LinkedList<float> data = new LinkedList<float>();
		public bool isActive = false;

		public float getMinValue ()	{
			return data.Count > 0 ? data.Min () : 0f;
		}

		public float getMaxValue ()	{
			return data.Count > 0 ? data.Max () : 1f;
		}

		public void trimData (int dataPoints) {
			while (data.Count > dataPoints) {
				data.RemoveFirst();
			}
		}

		public void clearData (){
			data.Clear();
		}

		public void setActive(bool active) {
			if (active && !isActive && color == XKCDColors.Black)
				setNextColor();
			isActive = active;
		}

		public void setNextColor() {
			color = graphColors[nextColor++ % graphColors.Count()];
		}

		public abstract void fetchData();
	}

	public class SensorSource : DataSource 
	{
		private Part part;

		public SensorSource (Part part) {
			this.part = part;
			this.name = part.partInfo.title;
			this.group = "Sensors";
		}

		public override void fetchData ()
		{
			float currentReading = 0;
			ModuleEnviroSensor sensor = part.Modules.OfType<ModuleEnviroSensor>().First();
			string cleanedValue = Regex.Replace(sensor.readoutInfo, @"([0-9]+\.?[0-9]*).*", "$1");
			float.TryParse(cleanedValue, out currentReading);
			data.AddLast(currentReading);
		}
	}

	public class VesselInfoSource : DataSource 
	{
		Func<float> valueFunction;

		public VesselInfoSource (string group, string name, Func<float> valueFunction) {
			this.valueFunction = valueFunction;
			this.name = name;
			this.group = group;
		}

		public override void fetchData ()
		{
			data.AddLast(valueFunction.Invoke());
		}
	}

	public class ResourceSource : DataSource
	{
		Vessel vessel;

		public ResourceSource (string resourceName, Vessel vessel)
		{
			this.name = resourceName;
			this.vessel = vessel;
			this.group = "Resources";
		}

		public override void fetchData ()
		{
			float total = 0f;
			foreach (Part p in vessel.parts) {
				foreach (PartResource pr in p.Resources) {
					if (pr.resourceName == name)
						total += (float)pr.amount;
				}
			}
			data.AddLast(total);
		}
		
	}
}

