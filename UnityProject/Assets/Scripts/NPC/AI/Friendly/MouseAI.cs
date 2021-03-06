using UnityEngine;

namespace Systems.MobAIs
{
	/// <summary>
	/// AI brain for mice
	/// used to get hunted by Runtime and squeak also annoy engis by chewing cables
	/// </summary>

	public class MouseAI : GenericFriendlyAI
	{
		[SerializeField, Tooltip("If this mouse get to this mood level, it will start chewing cables")]
		private int angryMouseLevel = -30;

		private MobMood mood;

		protected override void Awake()
		{
			base.Awake();
			mood = GetComponent<MobMood>();
		}

		protected override void MonitorExtras()
		{
			base.MonitorExtras();
			// If mouse not happy, mouse chew cable. Feed mouse. Or kill mouse, that would work too.
			CheckMoodLevel();
		}

		private void CheckMoodLevel()
		{
			if (mood.Level <= angryMouseLevel)
			{
				DoRandomWireChew();
			}
		}

		public override void OnPetted(GameObject performer)
		{
			Squeak();
			StartFleeing(performer, 3f);
		}

		protected override void OnFleeingStopped()
		{
			BeginExploring();
		}

		private void Squeak()
		{
			SoundManager.PlayNetworkedAtPos(
				"MouseSqueek",
				gameObject.transform.position,
				Random.Range(.6f, 1.2f));

			Chat.AddActionMsgToChat(
				gameObject,
				$"{mobNameCap} squeaks!",
				$"{mobNameCap} squeaks!");
		}

		private void DoRandomWireChew()
		{
			var metaTileMap = registerObject.TileChangeManager.MetaTileMap;
			var matrix = metaTileMap.Layers[LayerType.Underfloor].matrix;

			// Check if the floor plating is exposed.
			if (metaTileMap.HasTile(registerObject.LocalPosition, LayerType.Floors)) return;

			// Check if there's cables at this position
			var cables = matrix.GetElectricalConnections(registerObject.LocalPosition);
			if (cables == null || cables.Count < 1) return;

			// Pick a random cable from the mouse's current tile position to chew from
			var cable = cables[Random.Range(0, cables.Count - 1)];
			WireChew(cable);
		}

		private void WireChew(IntrinsicElectronicData cable)
		{
			ElectricityFunctions.WorkOutActualNumbers(cable);
			float voltage = cable.Data.ActualVoltage;

			// Remove the cable and spawn the item.
			cable.DestroyThisPlease();
			var electricalTile = registerObject.TileChangeManager
				.GetLayerTile(registerObject.WorldPosition, LayerType.Underfloor) as ElectricalCableTile;
			// Electrical tile is not null iff this is the first mousechew. Why?
			if (electricalTile != null)
			{
				Spawn.ServerPrefab(electricalTile.SpawnOnDeconstruct, registerObject.WorldPosition,
					count: electricalTile.SpawnAmountOnDeconstruct);
			}

			Electrocute(voltage);
		}

		private void Electrocute(float voltage)
		{
			var electrocution = new Electrocution(voltage, registerObject.WorldPosition);
			var performerLHB = GetComponent<LivingHealthBehaviour>();
			performerLHB.Electrocute(electrocution);

			//doing a shit ton of damage because all mobs have too much hardcoded HP right now
			//TODO get rid of this part once health rework is done!
			performerLHB.ApplyDamage(gameObject, 200, AttackType.Internal, DamageType.Tox);
		}

		protected override void DoRandomAction()
		{
			Squeak();
		}

		protected override void OnSpawnMob()
		{
			base.OnSpawnMob();
			BeginExploring();
		}

	}
}