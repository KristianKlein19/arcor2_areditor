using SystemAction = System.Action;
using UnityEngine;

namespace Base
{
    public class Icon3DClickHandler : MonoBehaviour
    {
        public SystemAction OnClick;
        public Camera arCamera;

        private void Start()
        {
            arCamera = Camera.main;
        }

        private void Update()
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);

                if (touch.phase == TouchPhase.Began)
                {
                    Ray ray = arCamera.ScreenPointToRay(touch.position);
                    RaycastHit hit;

                    if (Physics.Raycast(ray, out hit))
                    {
                        if (hit.collider.gameObject == gameObject)
                        {
                            OnClick.Invoke();
                        }
                    }
                }
            }
        }

    }
}
