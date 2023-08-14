using UnityEngine;
using UnityEngine.Animations;

namespace SimKit
{
    public class LookAtCanvasManager : MonoBehaviour
    {
        private Camera _cam;
        private float _canvasScaleFixedSize = 0.0008f;

        private void Awake()
        {
            _cam = Camera.main;
            ConstraintSource source = new()
            {
                sourceTransform = _cam.transform,
                weight = 0.85f
            };
            GetComponent<LookAtConstraint>().AddSource(source);
            GetComponent<Canvas>().worldCamera = _cam;
        }

        private void Update()
        {
            AutoSize();
        }

        private void AutoSize()
        {
            var distance = (_cam.transform.position - transform.position).magnitude;
            var size = distance * _canvasScaleFixedSize * _cam.fieldOfView;
            transform.localScale = Vector3.one * size;
            transform.forward = transform.position - _cam.transform.position;
        }
    }
}