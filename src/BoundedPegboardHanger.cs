using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Receiver2;

namespace Receiver2ModdingKit {

	[RequireComponent(typeof(BoxCollider))]
	class BoundedPegboardHanger : PegboardHanger {
		private Bounds m_bounds {
			get { return GetComponent<BoxCollider>().bounds; }
		}

		new public Vector3 BoundsCenter {
			get { return m_bounds.center; }
		}

		new public Vector3 GetBoundsCenterRelativeToTransform(Transform relative_to) {
			Vector3 center = m_bounds.center;
			Vector3 vector = transform.InverseTransformPoint(relative_to.position);
			center.z = vector.z;
			return transform.TransformPoint(center);
		}

		new public Rect GetRect() {
			return new Rect {
				center = m_bounds.center,
				size = m_bounds.size
			};
		}
	}
}
