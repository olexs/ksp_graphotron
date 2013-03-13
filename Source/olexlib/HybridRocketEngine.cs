using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace olexlib
{
	public class HybridRocketModule : PartModule
	{
		private const string DISABLED = "Disabled";
		private const string JET = "Jet";
		private const string ROCKET = "Rocket";

		[KSPField(guiActive = true, guiName = "Mode")]
		public string status = DISABLED;

		//public List<HybridRocketModule.Engine> engines = new List<HybridRocketModule.Engine>();

		private ModuleEngines jet;
		private ModuleEngines rocket;

		private float throttleToSet = -1f; 
		private float throttleToSet2 = -1f; 
						
		public override void OnStart (PartModule.StartState state)
		{
			base.OnStart (state);

			/*MonoBehaviour.print("Configured with " + engines.Count + " switchable engines:");
			foreach (Engine engine in engines)
				MonoBehaviour.print(engine.ToString());*/

			//FlightInputHandler.OnFlyByWire += new FlightInputHandler.FlightInputCallback(autopilotDelegate);
			
			// let's find us the other two part modules, throw errors if we can't
			try {
				jet = this.part.Modules.OfType<ModuleEngines> ().Where (m => m.propellants.Any (p => p.name == "IntakeAir")).First ();
				rocket = this.part.Modules.OfType<ModuleEngines> ().Where (m => m.propellants.Any (p => p.name == "Oxidizer")).First ();
			} catch (InvalidOperationException e) {
				MonoBehaviour.print ("HybridRocketModule error: jet or rocket engine partmodules not found, check part configuration");
			}

			MonoBehaviour.print("HybridRocketModule initialized");

			// hide all GUI events for this, except our own Toggle event
			Deactivate();

			switch (this.status) {
			case JET:
				ActivateJet();
				break;
			case ROCKET:
				ActivateRocket();
				break;
			}
		}

		private void HideOtherEvents ()
		{
			jet.Events.Any(e => e.guiActive = false);
			rocket.Events.Any(e => e.guiActive = false);
			HideFields(jet);
			HideFields(rocket);
		}

		private void HideFields (ModuleEngines engine)
		{
			foreach (BaseField field in engine.Fields) {
				field.guiActive = false;
			}
		}

		private void ShowFields (ModuleEngines engine)
		{
			foreach (BaseField field in engine.Fields) {
				if (field.guiName != "")
					field.guiActive = true;
			}
		}

		[KSPEvent(guiActive = true, guiName = "Toggle engine")]
		public void ToggleSabre() 
		{
			switch (this.status) {
			case DISABLED:
				ActivateJet();
				break;
			case JET:
				ActivateRocket();
				break;
			case ROCKET:
				Deactivate();
				break;
			}
		}

		[KSPAction ("Toggle SABRE mode (off-jet-rocket")]
		private void ToggleAction (KSPActionParam param)
		{
			ToggleSabre();
		}

		private void Deactivate ()
		{
			MonoBehaviour.print ("HybridRocketModule shutting down");

			if (FlightInputHandler.state != null) { // if inflight
				this.throttleToSet2 = FlightInputHandler.state.mainThrottle;
				this.throttleToSet = 0f;
			
				jet.Events["Shutdown"].Invoke();
				rocket.Events["Shutdown"].Invoke();
			}

			HideOtherEvents();

			this.status = DISABLED;
			this.Events["ToggleSabre"].guiName = "Activate Jet mode";
		}

		private void ActivateJet ()
		{
			MonoBehaviour.print ("HybridRocketModule switching to jet mode");

			this.throttleToSet2 = FlightInputHandler.state.mainThrottle;
			this.throttleToSet = 0f;

			rocket.Events["Shutdown"].Invoke();
			jet.Events["Activate"].Invoke();
			HideOtherEvents();
			ShowFields(jet);

			this.status = JET;
			this.Events["ToggleSabre"].guiName = "Activate Rocket mode";
		}

		private void ActivateRocket ()
		{
			MonoBehaviour.print ("HybridRocketModule switching to rocket mode");

			this.throttleToSet2 = FlightInputHandler.state.mainThrottle;
			this.throttleToSet = 0f;

			jet.Events["Shutdown"].Invoke();
			rocket.Events["Activate"].Invoke();
			HideOtherEvents();
			ShowFields(rocket);

			this.status = ROCKET;
			this.Events["ToggleSabre"].guiName = "Shutdown";
		}

		public override void OnActive ()
		{
			base.OnActive ();
			ToggleSabre();
			MonoBehaviour.print ("HybridRocketModule.OnActive triggered");
		}

		private void autopilotDelegate (FlightCtrlState s)
		{
			if (this.throttleToSet > -1f) {
				s.mainThrottle = this.throttleToSet;
				this.throttleToSet = -1f;
			} else if (this.throttleToSet2 > -1f) {
				s.mainThrottle = this.throttleToSet2;
				this.throttleToSet2 = -1f;
			}
		}

		/*[Serializable]
		public class Engine : IConfigNode 
		{
			[SerializeField]
			public string name;

			[SerializeField]
			public string propellant;
			
			public void Load (ConfigNode node)
			{
				name = node.GetValue ("name");
				propellant = node.GetValue("uniquePropellant");
			}
			
			public void Save (ConfigNode node)
			{
				node.AddValue("name", name);
				node.AddValue("uniquePropellant", propellant);
			}

			public override string ToString ()
			{
				return "Engine: "+name+", unique propellant: "+propellant;
			}
		}*/

	}


}



