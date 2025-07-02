using UnityEngine;
using UnityEditor;

public class JointFollowerTool : EditorWindow
{
	[MenuItem("Kayac/JointFollower")]
	public static void ShowWindow()
	{
		GetWindow<JointFollowerTool>(false, "JointFollower");
	}

	GameObject followedRoot;
	GameObject followingRoot;
	float driveSpring = 10000f;
	float driveDamping = 1000f;
	float defaultBodyMass = 1f;
	bool generateRigidbody = true;
	bool lockPosition = true;
	bool useGravity = true;
	int maxDepth = 8;
	float maxLinearVelocity = float.MaxValue;
	float maxAngularVelocity = float.MaxValue;
	bool generateColliders = false;
	float colliderRadius = 0.01f;

	void OnGUI()
	{
		followedRoot = (GameObject)EditorGUILayout.ObjectField("コピーされる方", followedRoot, typeof(GameObject), true);
		followingRoot = (GameObject)EditorGUILayout.ObjectField("コピーする方", followingRoot, typeof(GameObject), true);

		driveSpring = EditorGUILayout.FloatField("Spring", driveSpring);
		driveDamping = EditorGUILayout.FloatField("Damping", driveDamping);

		generateRigidbody = EditorGUILayout.Toggle("GenerateRigidbody", generateRigidbody);
		lockPosition = EditorGUILayout.Toggle("LockPosition", lockPosition);
		defaultBodyMass = EditorGUILayout.FloatField("DefaultMass", defaultBodyMass);
		useGravity = EditorGUILayout.Toggle("UseGravity", useGravity);
		maxLinearVelocity = EditorGUILayout.FloatField("MaxLinearVelocity", maxLinearVelocity);
		maxAngularVelocity = EditorGUILayout.FloatField("MaxAngularVelocity", maxAngularVelocity);
		maxDepth = EditorGUILayout.IntField("MaxDepth", maxDepth);
		generateColliders = EditorGUILayout.Toggle("GenerateColliders", generateColliders); // なんかまともに動かないので封じる
		colliderRadius = EditorGUILayout.FloatField("ColliderRadius", colliderRadius);

		if (GUILayout.Button("やれ"))
		{
			Setup(followedRoot.transform, followingRoot.transform, null, null, 0);
		}
	}
	
	void Setup(Transform followed, Transform following, Transform parent, Rigidbody parentBody, int depth)
	{
		Debug.Log("PoseFollowerTool.Setup : " + followed.gameObject.name);
		var body = following.gameObject.GetComponent<Rigidbody>();
		if (generateRigidbody && (body == null))
		{
			body = following.gameObject.AddComponent<Rigidbody>();
			Debug.Log("\t Rigidbody generated.");
			body.mass = defaultBodyMass;
		}

		if (parentBody != null)
		{
			var joint = following.gameObject.GetComponent<ConfigurableJoint>();
			if (joint == null)
			{
				joint = following.gameObject.AddComponent<ConfigurableJoint>();
				Debug.Log("\t ConfigurableJoint generated.");
			}

			var controller = following.gameObject.GetComponent<JointFollower>();
			if (controller == null)
			{
				controller = following.gameObject.AddComponent<JointFollower>();
				Debug.Log("\t JointFollower generated.");
			}

			controller.Init(parent, followed, joint, body, maxLinearVelocity, maxAngularVelocity);
			joint.connectedBody = parentBody;
			joint.slerpDrive = new JointDrive
			{
				positionSpring = driveSpring,
				positionDamper = driveDamping,
				maximumForce = Mathf.Infinity,
				useAcceleration = true
			};
			joint.xDrive = joint.yDrive = joint.zDrive = joint.slerpDrive;
			if (lockPosition)
			{
				joint.xMotion = ConfigurableJointMotion.Locked;
				joint.yMotion = ConfigurableJointMotion.Locked;
				joint.zMotion = ConfigurableJointMotion.Locked;
			}
			joint.rotationDriveMode = RotationDriveMode.Slerp;
		}

		if (generateColliders)
		{
			var collider = following.gameObject.GetComponent<Collider>();
			if (collider == null)
			{
				var sphereCollider = following.gameObject.AddComponent<SphereCollider>();
				sphereCollider.radius = colliderRadius;
				Debug.Log("\t Collider generated.");
			}
		}

		if (body != null) // この先のparentはこれになる
		{
			body.useGravity = useGravity;
			parentBody = body;
			parent = followed;
		}

		// 名前が一致したら再帰
		if (depth < maxDepth)
		{
			for (var followedIndex = 0; followedIndex < followed.childCount; followedIndex++)
			{
				var followedChild = followed.GetChild(followedIndex);
				for (var followingIndex = 0; followingIndex < following.childCount; followingIndex++)
				{
					var followingChild = following.GetChild(followingIndex);
					if (followedChild.name == followingChild.name)
					{
						Setup(followedChild, followingChild, parent, parentBody, depth + 1);
						break;
					}
				}
			}
		}
	}
}
