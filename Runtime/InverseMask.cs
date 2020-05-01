using UnityEngine.Rendering;

namespace UnityEngine.UI.Mask
{
    //ExecuteInEditMode : 런타임 중이 아닌, 에디터 모드에서도 해당 스크립트가 실행됩니다.
    [ExecuteInEditMode]
    [AddComponentMenu("UI/InverseMask/InverseMask")]
    public class InverseMask : MonoBehaviour, IMaterialModifier
    {
        #region Show inspector

        [Tooltip("트랜스폼 변화를 타겟과 동일한 형태로 맞춥니다.")]
        [SerializeField]
        private RectTransform m_FitTarget;

        [Tooltip("매 프레임마다 LateUpdate에서 타겟과 동일하게 트랜스폼을 변화시킵니다.")]
        [SerializeField]
        private bool m_FitOnLateUpdate;

        [Tooltip("Inverse마스크 처리를 자식으로 들어온 오브젝트에게만 영향을 줍니다.")]
        [SerializeField]
        private bool m_OnlyForChildren;

        [Header("Debug")]
        [Tooltip("해당 오브젝트가 Inverse되서 안보이는 것을 보이게합니다.")]
        [SerializeField]
        private bool m_ShowInverseMaskGraphic;

        #endregion

        #region Hide Inspector

        //피봇 중심점
        private static readonly Vector2 pivotCenter = new Vector2(0.5f, 0.5f);

        //Image가 화면에 그래픽 처리가 되는 것을 처리하는 변수입니다. 
        private Graphic _graphic;
        private Graphic graphic => _graphic ? _graphic : _graphic = GetComponent<Graphic>();

        //Private Members.
        private Material _MaskMaterial;
        private Material _InverseMaskMaterial;
        
        #endregion

        #region Public Valiable

        /// <summary>
        /// 핏 타겟을 대상을 변화시킵니다.
        /// </summary>
        public RectTransform fitTarget
        {
            get => m_FitTarget;
            set
            {
                m_FitTarget = value;
                FitTo(m_FitTarget);
            }
        }

        /// <summary>
        /// 타겟과 동일하게 트랜스폼을 변화시킵니다.
        /// </summary>
        /// <param name="target">타겟 트랜스폼</param>
        public void FitTo(RectTransform target)
        {
            //트랜스폼을 렉트 트랜스폼으로 캐스팅
            var rt = (RectTransform) transform;

            if (rt != null)
            {
                //트랜스폼을 동기화함 
                rt.position = target.position;
                rt.rotation = target.rotation;
                rt.localScale = target.localScale;

                //피봇, 사이즈, 앵커 포지션을 동기화함
                rt.pivot = target.pivot;
                rt.sizeDelta = target.rect.size;
                rt.anchorMax = rt.anchorMin = pivotCenter;
            }
        }
        
        /// <summary>
        /// 매 프레임마다 LateUpdate에서 타겟과 동일하게 트랜스폼을 변화시킵니다.
        /// </summary>
        public bool fitOnLateUpdate
        {
            get => m_FitOnLateUpdate;
            set => m_FitOnLateUpdate = value;
        }

        /// <summary>
        /// 해당 오브젝트가 Inverse되서 안보이는 것을 보이게합니다.
        /// </summary>
        public bool showInverseMaskGraphic
        {
            get => m_ShowInverseMaskGraphic;
            set
            {
                m_ShowInverseMaskGraphic = value;
                SetDirty();
            }
        }

        /// <summary>
        /// Inverse마스크 처리를 자식으로 들어온 오브젝트에게만 영향이 가도록 합니다.
        /// </summary>
        public bool onlyForChildren
        {
            get => m_OnlyForChildren;
            set
            {
                m_OnlyForChildren = value;
                SetDirty();
            }
        }

        //머티리얼 변화에 대한 인터페이스 구현
        public Material GetModifiedMaterial(Material baseMaterial)
        {
            //오브젝트가 비활성화시 기본 머티리얼로 반환합니다.
            if (!isActiveAndEnabled)
                return baseMaterial;

            //해당 UI오브젝트를 자식으로하고 있는 캔바스 오브젝트를 수록합니다.
            var stopAfter = MaskUtilities.FindRootSortOverrideCanvas(transform);
            
            //스텐실의 깊이 값을 가져옵니다.
            var stencilDepth = MaskUtilities.GetStencilDepth(transform, stopAfter);
            
            //스텐실 머티리얼에서 이전에 정의된 Mask 머티리얼을 지웁니다.
            StencilMaterial.Remove(_MaskMaterial);

            //Mask머티리얼에 스텐실 ID (1 << stencilDepth) - 1 까지 버퍼에 0을 작성하고, 스텐실 테스트를 항상 통과하게 하며,
            //ShowUnmaskGraphic가 true 라면 컬러 읽기를 허락하고 false라면 읽을 수 없도록 0을 대입합니다.
            //ReadMask는 0을 대입하고, WriteMask는 (1 << stencilDepth) - 1를 대입합니다.
            _MaskMaterial = StencilMaterial.Add(baseMaterial, (1 << stencilDepth) - 1, StencilOp.Zero,
                CompareFunction.Always, m_ShowInverseMaskGraphic ? ColorWriteMask.All : 0, 0, (1 << stencilDepth) - 1);
            
            //캔버스 렌더러를 가져옵니다.
            var canvasRenderer = graphic.canvasRenderer;

            //InverseMask를 자식 오브젝트에게만 적용 시킵니다.
            if (m_OnlyForChildren)
            {
                //스텐실 머티리얼에서 이전에 정의된 InverseMask머티리얼을 지웁니다.
                StencilMaterial.Remove(_InverseMaskMaterial);
                
                //Mask머티리얼에 스텐실 ID (1 << stencilDepth) - 1 까지 작성을 할것이며, 레퍼런스 값이 버퍼 안의 값과 다른 픽셀만 렌더링합니다,
                //컬러 읽기는 허락할 수 없도록 처리합니다.
                _InverseMaskMaterial = StencilMaterial.Add(baseMaterial, (1 << stencilDepth) - 1, StencilOp.Replace,
                    CompareFunction.NotEqual, 0);
                
                //이 부분은 설명하기에 너무 복잡해지므로.. 스킵합니다.
                canvasRenderer.hasPopInstruction = true;
                canvasRenderer.popMaterialCount = 1;
                canvasRenderer.SetPopMaterial(_InverseMaskMaterial, 0);
            }
            else
            {
                canvasRenderer.hasPopInstruction = false;
                canvasRenderer.popMaterialCount = 0;
            }

            return _MaskMaterial;
        }
        
        #endregion
        
        //오브젝트가 활성화시 발동됩니다.
        private void OnEnable()
        {
            //활성화시 핏 대상이 있을 경우, 동일하게 맞춥니다.
            if (m_FitTarget)
                FitTo(m_FitTarget);
            
            //화면에 이미지를 렌더링을 할지 말지 처리합니다.
            SetDirty();
        }

        //오브젝트가 비활성화시 발동됩니다.
        private void OnDisable()
        {
            //스텐실 머티리얼에서 기존에 만들어진 MaskMaterial, InverseMaskMaterial을 삭제합니다.
            StencilMaterial.Remove(_MaskMaterial);
            StencilMaterial.Remove(_InverseMaskMaterial);
            
            //초기화
            _MaskMaterial = null;
            _InverseMaskMaterial = null;
            
            if (graphic)
            {
                var canvasRenderer = graphic.canvasRenderer;
                canvasRenderer.hasPopInstruction = false;
                canvasRenderer.popMaterialCount = 0;
                graphic.SetMaterialDirty();
            }

            //화면에 이미지를 렌더링을 할지 말지 처리합니다.
            SetDirty();
        }

        //모든 Update 함수가 호출된 후에 호출됩니다
        private void LateUpdate()
        {
#if UNITY_EDITOR
            //타겟이 설정되어있고, LateUpdate가 활성화 되어있으며, 유니티 에디터 실행모드가 아닌 경우
            if (m_FitTarget && (m_FitOnLateUpdate || !Application.isPlaying))
#else
            //타겟이 설정되어있고, LateUpdate가 활성화 되어있는 경우
			if (m_FitTarget && m_FitOnLateUpdate)
#endif
            {
                FitTo(m_FitTarget);
            }
        }
        
        /// <summary>
        /// 이 함수는 스크립트가 인스펙터에 붙여지거나, 인스펙터에 있는 변수의 값이 변경 될 때 호출됩니다.
        /// </summary>
        private void OnValidate() =>
            SetDirty();

        /// <summary>
        /// 해당 UI를 보이게 합니다.
        /// </summary>
        private void SetDirty()
        {
            if (graphic) graphic.SetMaterialDirty();
        }
    }
}
