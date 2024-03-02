using Receiver2;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Receiver2ModdingKit.Editor
{
	[RequireComponent(typeof(Collider))]
	public class AssignPhysicsMaterial : MonoBehaviour
	{
		public Collider colliderToModify;

		public Receiver2PhysicsMaterials physicsMaterial;

		private void Awake()
		{
			if (colliderToModify == null) colliderToModify = GetComponent<Collider>(); //in case there's only one collider and you can't be bothered

			colliderToModify.sharedMaterial = GetPhysicMaterialForEnum(physicsMaterial);

			Destroy(this);
		}

		private static PhysicMaterial GetPhysicMaterialForEnum(Receiver2PhysicsMaterials receiver2PhysicsMaterials)
		{
			PhysicMaterial physicMaterial = null;

			var physicsMaterialManager = Object.FindObjectOfType<PhysicsMaterialManager>();

			switch (receiver2PhysicsMaterials)
			{
				case Receiver2PhysicsMaterials.Brass_Physics:
					physicMaterial = physicsMaterialManager.physics_materials.First(material => { return material is PhysicMaterial elFisicoMaterial && elFisicoMaterial.name == "brass_physics"; });
					break;

				case Receiver2PhysicsMaterials.Brick:
					physicMaterial = physicsMaterialManager.physics_materials.First(material => { return material is PhysicMaterial elFisicoMaterial && elFisicoMaterial.name == "brick"; });
					break;

				case Receiver2PhysicsMaterials.Cardboard_Box:
					physicMaterial = physicsMaterialManager.physics_materials.First(material => { return material is PhysicMaterial elFisicoMaterial && elFisicoMaterial.name == "cardboard_box"; });
					break;

				case Receiver2PhysicsMaterials.Ceramic:
					physicMaterial = physicsMaterialManager.physics_materials.First(material => { return material is PhysicMaterial elFisicoMaterial && elFisicoMaterial.name == "ceramic"; });
					break;

				case Receiver2PhysicsMaterials.Cloth:
					physicMaterial = physicsMaterialManager.physics_materials.First(material => { return material is PhysicMaterial elFisicoMaterial && elFisicoMaterial.name == "cloth"; });
					break;

				case Receiver2PhysicsMaterials.Concrete:
					physicMaterial = physicsMaterialManager.physics_materials.First(material => { return material is PhysicMaterial elFisicoMaterial && elFisicoMaterial.name == "concrete"; });
					break;

				case Receiver2PhysicsMaterials.Dirt:
					physicMaterial = physicsMaterialManager.physics_materials.First(material => { return material is PhysicMaterial elFisicoMaterial && elFisicoMaterial.name == "dirt"; });
					break;

				case Receiver2PhysicsMaterials.Drywall:
					physicMaterial = physicsMaterialManager.physics_materials.First(material => { return material is PhysicMaterial elFisicoMaterial && elFisicoMaterial.name == "drywall"; });
					break;

				case Receiver2PhysicsMaterials.Flesh:
					physicMaterial = physicsMaterialManager.physics_materials.First(material => { return material is PhysicMaterial elFisicoMaterial && elFisicoMaterial.name == "flesh"; });
					break;

				case Receiver2PhysicsMaterials.Glass:
					physicMaterial = physicsMaterialManager.physics_materials.First(material => { return material is PhysicMaterial elFisicoMaterial && elFisicoMaterial.name == "glass"; });
					break;

				case Receiver2PhysicsMaterials.Glass_Bulletproof:
					physicMaterial = physicsMaterialManager.physics_materials.First(material => { return material is PhysicMaterial elFisicoMaterial && elFisicoMaterial.name == "glass_bulletproof"; });
					break;

				case Receiver2PhysicsMaterials.Hard_Metal:
					physicMaterial = physicsMaterialManager.physics_materials.First(material => { return material is PhysicMaterial elFisicoMaterial && elFisicoMaterial.name == "hard_metal"; });
					break;

				case Receiver2PhysicsMaterials.Metal:
					physicMaterial = physicsMaterialManager.physics_materials.First(material => { return material is PhysicMaterial elFisicoMaterial && elFisicoMaterial.name == "metal"; });
					break;

				case Receiver2PhysicsMaterials.Metal_Fence:
					physicMaterial = physicsMaterialManager.physics_materials.First(material => { return material is PhysicMaterial elFisicoMaterial && elFisicoMaterial.name == "metal_fence"; });
					break;

				case Receiver2PhysicsMaterials.Metal_Target_Large:
					physicMaterial = physicsMaterialManager.physics_materials.First(material => { return material is PhysicMaterial elFisicoMaterial && elFisicoMaterial.name == "metal_target_large"; });
					break;

				case Receiver2PhysicsMaterials.Metal_Target_Small:
					physicMaterial = physicsMaterialManager.physics_materials.First(material => { return material is PhysicMaterial elFisicoMaterial && elFisicoMaterial.name == "metal_target_small"; });
					break;

				case Receiver2PhysicsMaterials.Metal_Target_Triple:
					physicMaterial = physicsMaterialManager.physics_materials.First(material => { return material is PhysicMaterial elFisicoMaterial && elFisicoMaterial.name == "metal_target_triple"; });
					break;

				case Receiver2PhysicsMaterials.No_Friction:
					physicMaterial = physicsMaterialManager.physics_materials.First(material => { return material is PhysicMaterial elFisicoMaterial && elFisicoMaterial.name == "no friction"; });
					break;

				case Receiver2PhysicsMaterials.Paper:
					physicMaterial = physicsMaterialManager.physics_materials.First(material => { return material is PhysicMaterial elFisicoMaterial && elFisicoMaterial.name == "paper"; });
					break;

				case Receiver2PhysicsMaterials.Pillow:
					physicMaterial = physicsMaterialManager.physics_materials.First(material => { return material is PhysicMaterial elFisicoMaterial && elFisicoMaterial.name == "pillow"; });
					break;

				case Receiver2PhysicsMaterials.Plant:
					physicMaterial = physicsMaterialManager.physics_materials.First(material => { return material is PhysicMaterial elFisicoMaterial && elFisicoMaterial.name == "plant"; });
					break;

				case Receiver2PhysicsMaterials.Plastic:
					physicMaterial = physicsMaterialManager.physics_materials.First(material => { return material is PhysicMaterial elFisicoMaterial && elFisicoMaterial.name == "plastic"; });
					break;

				case Receiver2PhysicsMaterials.Pool_Ball:
					physicMaterial = physicsMaterialManager.physics_materials.First(material => { return material is PhysicMaterial elFisicoMaterial && elFisicoMaterial.name == "pool_ball"; });
					break;

				case Receiver2PhysicsMaterials.Rubber:
					physicMaterial = physicsMaterialManager.physics_materials.First(material => { return material is PhysicMaterial elFisicoMaterial && elFisicoMaterial.name == "rubber"; });
					break;

				case Receiver2PhysicsMaterials.Tile:
					physicMaterial = physicsMaterialManager.physics_materials.First(material => { return material is PhysicMaterial elFisicoMaterial && elFisicoMaterial.name == "tile"; });
					break;

				case Receiver2PhysicsMaterials.Wood:
					physicMaterial = physicsMaterialManager.physics_materials.First(material => { return material is PhysicMaterial elFisicoMaterial && elFisicoMaterial.name == "wood"; });
					break;
			}

			return physicMaterial;
		}

		public enum Receiver2PhysicsMaterials
		{
			Brass_Physics,
			Brick,
			Cardboard_Box,
			Ceramic,
			Cloth,
			Concrete,
			Dirt,
			Drywall,
			Flesh,
			Glass,
			Glass_Bulletproof,
			Hard_Metal,
			Metal,
			Metal_Fence,
			Metal_Target_Large,
			Metal_Target_Small,
			Metal_Target_Triple,
			No_Friction,
			Paper,
			Pillow,
			Plant,
			Plastic,
			Pool_Ball,
			Rubber,
			Tile,
			Wood
		}
	}
}
