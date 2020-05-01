namespace UnityEngine.UI.Mask
{
    [AddComponentMenu("UI/InverseMask/InverseMaskRaycastFilter")]
    public class InverseMaskRaycastFilter : MonoBehaviour, ICanvasRaycastFilter
    {
        [Tooltip("Inverse 마스크된 영역 뒤에 있는 오브젝트가 Ray가 접근되도록 합니다.")]
        [SerializeField]
        private InverseMask mTargetInverseMask;

        /// <summary>
        /// Inverse 마스크된 영역 뒤에 있는 오브젝트가 Ray가 접근되도록 합니다.
        /// </summary>
        public InverseMask TargetInverseMask
        {
            get => mTargetInverseMask;
            set => mTargetInverseMask = value;
        }

        /// <summary>
        /// 특정 영역에 해당 포인트가 있는지 반환합니다.
        /// </summary>
        /// <returns>Valid.</returns>
        /// <param name="sp">Screen position.</param>
        /// <param name="eventCamera">Raycast camera.</param>
        public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
        {
            // 비활성화 된 경우 건너 뜁니다.
            if (!isActiveAndEnabled || !mTargetInverseMask || !mTargetInverseMask.isActiveAndEnabled)
                return true;

            //eventCamera가 null이 할당되어있다면,
            if (eventCamera)
            {
                //eventCamera를 기반하여 transform이 sp영역에 있으면 True를 반환합니다.
                return !RectTransformUtility.RectangleContainsScreenPoint((RectTransform) mTargetInverseMask.transform,
                    sp, eventCamera);
            }
            else
                //transform이 sp영역에 있으면 True를 반환합니다.
                return !RectTransformUtility.RectangleContainsScreenPoint((RectTransform) mTargetInverseMask.transform,
                    sp);
        }
    }
}
