using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace Splines
{
    [AddComponentMenu("Splines/Spline Mesh")]
    public partial class SplineMesh : SplineEvaluator
    {
        public enum ForwardAxisMode
        {
            X = 0,
            Y = 1,
            Z = 2
        }

        [SerializeField]
        Mesh m_Mesh;
        public Mesh mesh
        {
            get => m_Mesh;
            set
            {
                if (value != m_Mesh)
                {
                    m_Mesh = value;
                    m_MeshInstance = CloneMesh(value);
                    SetNeedsRebuild();
                }
            }
        }

        [SerializeField] 
        ForwardAxisMode m_ForwardAxis = ForwardAxisMode.Z;
        public ForwardAxisMode forwardAxis
        {
            get => m_ForwardAxis;
            set
            {
                if (value != m_ForwardAxis)
                {
                    m_ForwardAxis = value;
                    SetNeedsRebuild();
                }
            }
        }
        
        [SerializeField]
        Vector2 m_Offset = Vector2.zero;
        public Vector2 offset
        {
            get => m_Offset;
            set
            {
                if (value != m_Offset)
                {
                    m_Offset = value;
                    SetNeedsRebuild();
                }
            }
        }
        
        [SerializeField]
        Vector2 m_Scale = Vector2.one;
        public Vector2 scale
        {
            get => m_Scale;
            set
            {
                if (value != m_Scale)
                {
                    m_Scale = value;
                    SetNeedsRebuild();
                }
            }
        }
        
        [SerializeField] 
        Vector2 m_UVScale = new Vector2(1, 1);
        public Vector2 uvScale
        {
            get => m_UVScale;
            set
            {
                if (value != m_UVScale)
                {
                    m_UVScale = value;
                    SetNeedsRebuild();
                }
            }
        }
        
        [SerializeField] 
        Vector2 m_UVOffset;
        public Vector2 uvOffset
        {
            get => m_UVOffset;
            set
            {
                if (value != m_UVOffset)
                {
                    m_UVOffset = value;
                    SetNeedsRebuild();
                }
            }
        }
        
        [SerializeField] 
        float m_UVRotation;
        public float uvRotation
        {
            get => m_UVRotation;
            set
            {
                if (value != m_UVRotation)
                {
                    m_UVRotation = value;
                    SetNeedsRebuild();
                }
            }
        }
        
        [SerializeField]
        bool m_SmoothScaleAndRoll = true;
        public bool smoothScaleAndRoll
        {
            get => m_SmoothScaleAndRoll;
            set
            {
                if (value != m_SmoothScaleAndRoll)
                {
                    m_SmoothScaleAndRoll = value;
                    SetNeedsRebuild();
                }
            }
        }

        [SerializeField]
        bool m_AutoSegments;
        public bool autoSegments
        {
            get => m_AutoSegments;
            set
            {
                if (value != m_AutoSegments)
                {
                    m_AutoSegments = value;
                    SetNeedsRebuild();
                }
            }
        }

        [SerializeField, Min(0)]
        float m_AutoSegmentLengthScale = 1.0f;
        public float autoSegmentLengthScale
        {
            get => m_AutoSegmentLengthScale;
            set
            {
                if (value != m_AutoSegmentLengthScale)
                {
                    m_AutoSegmentLengthScale = value;
                    SetNeedsRebuild();
                }
            }
        }

        [SerializeField, Min(1)]
        int m_SegmentCount = 1;
        public int segmentCount
        {
            get => m_SegmentCount;
            set
            {
                if (value != m_SegmentCount)
                {
                    m_SegmentCount = value;
                    SetNeedsRebuild();
                }
            }
        }

        [SerializeField]
        Material[] m_SharedMaterials;
        public Material[] sharedMaterials
        {
            get => m_SharedMaterials;
            set
            {
                if (value != m_SharedMaterials)
                {
                    m_SharedMaterials = value;
                    SetNeedsRebuild();
                }
            }
        }

        [SerializeField]
        IndexFormat m_IndexFormat = IndexFormat.UInt16;
        public IndexFormat indexFormat
        {
            get => m_IndexFormat;
            set
            {
                if (value != m_IndexFormat)
                {
                    m_IndexFormat = value;
                    SetNeedsRebuild();
                }
            }
        }

        [SerializeField]
        Mesh m_MeshInstance;
        [SerializeField]
        Mesh m_FinalMesh;
        [SerializeField]
        GameObject m_MeshGameObject;

        MeshFilter m_MeshFilter;
        MeshRenderer m_MeshRenderer;

        protected override void Build()
        {
            if (!m_MeshInstance)
                return;
            
            if (!m_MeshGameObject)
            {
                m_MeshGameObject = new GameObject
                {
                    name = "Mesh", 
                    // hideFlags = HideFlags.NotEditable | HideFlags.HideInHierarchy | HideFlags.HideInInspector
                };
                
                m_MeshGameObject.transform.parent = transform;
                m_MeshGameObject.transform.localPosition = Vector3.zero;
                m_MeshGameObject.transform.localRotation = Quaternion.identity;
                m_MeshGameObject.transform.localScale = Vector3.one;
                m_MeshFilter = m_MeshGameObject.AddComponent<MeshFilter>();
                m_MeshRenderer = m_MeshGameObject.AddComponent<MeshRenderer>();
            }

            var jobs = new JobHandle();
            jobs = ScheduleRebuild(out var splineMesh);

            if (splineMesh.isCreated)
            {
                jobs.Complete();
            
                FinalizeMesh(splineMesh);
                splineMesh.Dispose();

                if (!m_MeshFilter)
                    m_MeshFilter = m_MeshGameObject.GetComponent<MeshFilter>();
                    
                if (!m_MeshRenderer)
                    m_MeshRenderer = m_MeshGameObject.GetComponent<MeshRenderer>();
            
                m_MeshFilter.sharedMesh = m_FinalMesh;
                m_MeshRenderer.sharedMaterials = m_SharedMaterials;
            }
        }

        void FinalizeMesh(NativeMesh splineMesh)
        {
            if (!m_FinalMesh)
            {
                m_FinalMesh = new Mesh {name = $"{name} - Mesh"};
                m_FinalMesh.MarkDynamic();
            }
            
            m_FinalMesh.indexFormat = m_IndexFormat;
            m_FinalMesh.Clear();
            m_FinalMesh.SetVertices(splineMesh.vertices);
            
            if (splineMesh.hasNormals)
                m_FinalMesh.SetNormals(splineMesh.normals);
            
            if (splineMesh.hasTangents)
                m_FinalMesh.SetTangents(splineMesh.tangents);
            
            if (splineMesh.hasColors)
                m_FinalMesh.SetColors(splineMesh.colors);

            if (splineMesh.hasTexCoords)
            {
                var vertexCount = splineMesh.vertices.Length;
                var activeChannelIndex = 0;
                for (int i = 0; i < 8; ++i)
                {
                    if ((splineMesh.activeUVChannelMask & 1 << i) != 0)
                    {
                        m_FinalMesh.SetUVs(i, splineMesh.texCoords, activeChannelIndex * vertexCount, vertexCount);
                        activeChannelIndex++;
                    }
                }
            }

            m_FinalMesh.subMeshCount = splineMesh.subMeshes.Length;
            for (var subMesh = 0; subMesh < m_FinalMesh.subMeshCount; ++subMesh)
            {
                var subMeshDescriptor = splineMesh.subMeshes[subMesh];
                m_FinalMesh.SetIndices(splineMesh.indices, subMeshDescriptor.indexStart, subMeshDescriptor.indexCount, subMeshDescriptor.topology, subMesh, false);
            }

            if (!splineMesh.hasNormals)
                m_FinalMesh.RecalculateNormals();
            
            if (!splineMesh.hasTangents)
                m_FinalMesh.RecalculateTangents();
            
            m_FinalMesh.RecalculateBounds();
            m_FinalMesh.Optimize();
            m_FinalMesh.MarkModified();
        }

        static readonly List<Vector3> s_TmpVertices = new List<Vector3>();
        static readonly List<Vector3> s_TmpNormals = new List<Vector3>();
        static readonly List<Vector4> s_TmpTangents = new List<Vector4>();
        static readonly List<Vector4> s_TmpTexCoords = new List<Vector4>();
        static readonly List<int> s_TmpIndices = new List<int>();

        internal static Mesh CloneMesh(Mesh mesh)
        {
            if (!mesh)
                return null;

            var cloneMesh = new Mesh {indexFormat = mesh.indexFormat};

            mesh.GetVertices(s_TmpVertices);

            bool hasNormals = mesh.HasVertexAttribute(VertexAttribute.Normal);
            if (hasNormals)
            {
                mesh.GetNormals(s_TmpNormals);
            }

            bool hasTangents = mesh.HasVertexAttribute(VertexAttribute.Tangent);
            if (hasTangents)
            {
                mesh.GetTangents(s_TmpTangents);
            }

            cloneMesh.SetVertices(s_TmpVertices);
            
            if (hasNormals)
                cloneMesh.SetNormals(s_TmpNormals);
            
            if (hasTangents)
                cloneMesh.SetTangents(s_TmpTangents);

            for (int channel = 0; channel < 8; ++channel)
            {
                if (mesh.HasVertexAttribute(VertexAttribute.TexCoord0 + channel))
                {
                    mesh.GetUVs(channel, s_TmpTexCoords);
                    cloneMesh.SetUVs(channel, s_TmpTexCoords);
                }
            }

            cloneMesh.subMeshCount = mesh.subMeshCount;

            for (int subMesh = 0; subMesh < mesh.subMeshCount; ++subMesh)
            {
                var subMeshDescriptor = mesh.GetSubMesh(subMesh);
                mesh.GetIndices(s_TmpIndices, subMesh);
                cloneMesh.SetIndices(s_TmpIndices, subMeshDescriptor.topology, subMesh);
            }

            cloneMesh.RecalculateBounds();
            
            return cloneMesh;
        }
    }
}