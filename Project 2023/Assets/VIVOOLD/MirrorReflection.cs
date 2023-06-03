using UnityEngine;
using Valve.VR;

public class MirrorReflection : MonoBehaviour {
	Camera mainCam;
	Camera mirrorCam;

	[SerializeField] ReflectionType reflectionType = ReflectionType.nonVR;
	[SerializeField] int textureSize = 256;
	[SerializeField] float clipPlaneOffset = 0.07f;

	RenderTexture reflectionTexture;

	Camera.StereoscopicEye eye = Camera.StereoscopicEye.Left;

	enum ReflectionType {
		nonVR,
		leftEye,
		rightEye
	}

	void Awake() {
		if (IsVRMode() == (reflectionType == ReflectionType.nonVR)) {
			this.enabled = false;
			return;
		}
		if (this.reflectionType == ReflectionType.rightEye) {
			eye = Camera.StereoscopicEye.Right;
		}
		SetupRenderTexture();
		SetupMaterial();
		SetupCams();
	}

	void LateUpdate() {
		PositionCamera();
	}

	void SetupRenderTexture() {
		if (!reflectionTexture) {
			reflectionTexture = new RenderTexture(textureSize, textureSize, 16);
		}
		reflectionTexture.name = "__MirrorReflection" + GetInstanceID();
		reflectionTexture.isPowerOfTwo = true;
		reflectionTexture.hideFlags = HideFlags.DontSave;
	}

	void SetupMaterial() {
		MeshRenderer mr = this.GetComponent<MeshRenderer>();
		if (reflectionType != ReflectionType.rightEye) {
			mr.material.SetTexture("_ReflectionTexLeft", reflectionTexture);
		} else {
			mr.material.SetTexture("_ReflectionTexRight", reflectionTexture);

		}
	}

	void SetupCams() {
		mainCam = Camera.main;

		mirrorCam = new GameObject($"Mirror Camera {GetInstanceID()}").AddComponent<Camera>();
		mirrorCam.gameObject.AddComponent<InvertCamCulling>();

		mirrorCam.targetTexture = reflectionTexture;
		mirrorCam.transform.parent = this.transform;
		mirrorCam.transform.position = Vector3.zero;
		mirrorCam.transform.rotation = Quaternion.identity;

		mirrorCam.clearFlags = mainCam.clearFlags;
		mirrorCam.backgroundColor = mainCam.backgroundColor;
		// if (mainCam.clearFlags == CameraClearFlags.Skybox) {
		// 	Skybox sky = mainCam.GetComponent<Skybox>();
		// 	Skybox mysky = mirrorCam.GetComponent<Skybox>();
		// 	if (!sky || !sky.material) {
		// 		mysky.enabled = false;
		// 	} else {
		// 		mysky.enabled = true;
		// 		mysky.material = sky.material;
		// 	}
		// }
		mirrorCam.farClipPlane = mainCam.farClipPlane;
		mirrorCam.nearClipPlane = mainCam.nearClipPlane;
		mirrorCam.orthographic = mainCam.orthographic;
		mirrorCam.fieldOfView = mainCam.fieldOfView;
		mirrorCam.aspect = mainCam.aspect;
		mirrorCam.orthographicSize = mainCam.orthographicSize;
	}

	void PositionCamera() {
		// The reflection plane position and normal in world space
		Vector3 pos = transform.position;
		Vector3 normal = transform.up;

		// Reflect camera around reflection plane
		float d = -Vector3.Dot(normal, pos) - clipPlaneOffset;
		Vector4 reflectionPlane = new Vector4(normal.x, normal.y, normal.z, d);

		Matrix4x4 reflection = Matrix4x4.zero;
		CalculateReflectionMatrix(ref reflection, reflectionPlane);
		Vector3 oldpos = mainCam.transform.position;
		Vector3 newpos = reflection.MultiplyPoint(oldpos);
		mirrorCam.transform.position = newpos;
		mirrorCam.transform.rotation = mainCam.transform.rotation;

		Matrix4x4 worldToCameraMatrix;
		if (IsVRMode()) {
			worldToCameraMatrix = mainCam.GetStereoViewMatrix(eye) * reflection;
			// I have no idea why, but for whatever reason, this must be set to right
			// Or at the very least, setting it to left causes the matrix to be nulled
			mirrorCam.SetStereoViewMatrix(Camera.StereoscopicEye.Right, worldToCameraMatrix);
		} else {
			worldToCameraMatrix = mainCam.worldToCameraMatrix * reflection;
			mirrorCam.worldToCameraMatrix = worldToCameraMatrix;
		}

		// Setup oblique projection matrix so that near plane is our reflection
		// plane. This way we clip everything below/above it for free.
		Vector4 clipPlane = CameraSpacePlane(worldToCameraMatrix, pos, normal, 1.0f);

		Matrix4x4 projectionMatrix;
		if (IsVRMode()) {
			projectionMatrix = mirrorCam.GetStereoProjectionMatrix(eye);
			// projectionMatrix = HMDMatrix4x4ToMatrix4x4(
			// 	OpenVR.System.GetProjectionMatrix(Valve.VR.EVREye.Eye_Left, mirrorCam.nearClipPlane, mirrorCam.farClipPlane)
			// );
			// projectionMatrix = GL.GetGPUProjectionMatrix(mirrorCam.GetStereoProjectionMatrix(eye), false);
		} else {
			projectionMatrix = mainCam.projectionMatrix;
		}
		MakeProjectionMatrixOblique(ref projectionMatrix, clipPlane);
		if (IsVRMode()) {
			// Same here, I have no idea why this must be set to right but hey here we are
			mirrorCam.SetStereoProjectionMatrix(Camera.StereoscopicEye.Right, projectionMatrix);
		} else {
			mirrorCam.projectionMatrix = projectionMatrix;
		}
	}

	private static void CalculateReflectionMatrix(ref Matrix4x4 reflectionMat, Vector4 plane) {
		reflectionMat.m00 = (1F - 2F * plane[0] * plane[0]);
		reflectionMat.m01 = (-2F * plane[0] * plane[1]);
		reflectionMat.m02 = (-2F * plane[0] * plane[2]);
		reflectionMat.m03 = (-2F * plane[3] * plane[0]);

		reflectionMat.m10 = (-2F * plane[1] * plane[0]);
		reflectionMat.m11 = (1F - 2F * plane[1] * plane[1]);
		reflectionMat.m12 = (-2F * plane[1] * plane[2]);
		reflectionMat.m13 = (-2F * plane[3] * plane[1]);

		reflectionMat.m20 = (-2F * plane[2] * plane[0]);
		reflectionMat.m21 = (-2F * plane[2] * plane[1]);
		reflectionMat.m22 = (1F - 2F * plane[2] * plane[2]);
		reflectionMat.m23 = (-2F * plane[3] * plane[2]);

		reflectionMat.m30 = 0F;
		reflectionMat.m31 = 0F;
		reflectionMat.m32 = 0F;
		reflectionMat.m33 = 1F;
	}

	// Given position/normal of the plane, calculates plane in camera space.
	private Vector4 CameraSpacePlane(Matrix4x4 worldToCameraMatrix, Vector3 pos, Vector3 normal, float sideSign) {
		Vector3 offsetPos = pos + normal * clipPlaneOffset;
		Vector3 cpos = worldToCameraMatrix.MultiplyPoint(offsetPos);
		Vector3 cnormal = worldToCameraMatrix.MultiplyVector(normal).normalized * sideSign;
		return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
	}

	// Extended sign: returns -1, 0 or 1 based on sign of a
	private static float sgn(float a) {
		if (a > 0.0f) return 1.0f;
		if (a < 0.0f) return -1.0f;
		return 0.0f;
	}

	// taken from http://www.terathon.com/code/oblique.html
	private static void MakeProjectionMatrixOblique(ref Matrix4x4 matrix, Vector4 clipPlane) {
		Vector4 q;

		// Calculate the clip-space corner point opposite the clipping plane
		// as (sgn(clipPlane.x), sgn(clipPlane.y), 1, 1) and
		// transform it into camera space by multiplying it
		// by the inverse of the projection matrix

		q.x = (sgn(clipPlane.x) + matrix[8]) / matrix[0];
		q.y = (sgn(clipPlane.y) + matrix[9]) / matrix[5];
		q.z = -1.0F;
		q.w = (1.0F + matrix[10]) / matrix[14];

		// Calculate the scaled plane vector
		Vector4 c = clipPlane * (2.0F / Vector3.Dot(clipPlane, q));

		// Replace the third row of the projection matrix
		matrix[2] = c.x;
		matrix[6] = c.y;
		matrix[10] = c.z + 1.0F;
		matrix[14] = c.w;
	}

	protected Matrix4x4 HMDMatrix4x4ToMatrix4x4(Valve.VR.HmdMatrix44_t input) {
		var m = Matrix4x4.identity;

		m[0, 0] = input.m0;
		m[0, 1] = input.m1;
		m[0, 2] = input.m2;
		m[0, 3] = input.m3;

		m[1, 0] = input.m4;
		m[1, 1] = input.m5;
		m[1, 2] = input.m6;
		m[1, 3] = input.m7;

		m[2, 0] = input.m8;
		m[2, 1] = input.m9;
		m[2, 2] = input.m10;
		m[2, 3] = input.m11;

		m[3, 0] = input.m12;
		m[3, 1] = input.m13;
		m[3, 2] = input.m14;
		m[3, 3] = input.m15;

		return m;
	}

	bool IsVRMode() {
		// return VRManager.IsVR;
		return true;
	}
}