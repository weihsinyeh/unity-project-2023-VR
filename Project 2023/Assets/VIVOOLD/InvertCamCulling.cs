using UnityEngine;
using UnityEngine.Rendering;

public class InvertCamCulling : MonoBehaviour {
	Camera myCam;
	void Awake() {
		myCam = this.GetComponent<Camera>();
		RenderPipelineManager.beginCameraRendering += BeginCameraRendering;
		RenderPipelineManager.endCameraRendering += EndCameraRendering;
	}

	void OnDestroy() {
		GL.invertCulling = false;
	}

	void BeginCameraRendering(ScriptableRenderContext context, Camera camera) {
		if (camera != myCam) return;
		GL.invertCulling = true;

	}

	void EndCameraRendering(ScriptableRenderContext context, Camera camera) {
		if (camera != myCam) return;
		GL.invertCulling = false;
	}
}