using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using static Unity.Mathematics.math;
using float3 = Unity.Mathematics.float3;
using quaternion = Unity.Mathematics.quaternion;
using float4x4 = Unity.Mathematics.float4x4;

namespace Splines
{
    public partial class SplineMesh
    {
        struct SplineMeshPoint
        {
            public float3 position;
            public float3 tangent;
            public float roll;
            public float2 offset;
            public float3 scale;
        }

        [BurstCompile]
        struct NativeMesh : IDisposable
        {
            public NativeArray<Vector3> vertices;

            public NativeArray<Vector3> normals;
            public readonly bool hasNormals;
            
            public NativeArray<Vector4> tangents;
            public readonly bool hasTangents;
            
            public NativeArray<Color> colors;
            public readonly bool hasColors;

            [NativeDisableParallelForRestriction]
            public NativeArray<Vector2> texCoords;
            public readonly bool hasTexCoords;
            public readonly int activeUVChannelCount;
            public readonly int activeUVChannelMask;

            public NativeArray<int> indices;
            public NativeArray<SubMeshDescriptor> subMeshes;

            public bool isCreated => vertices.IsCreated;

            public NativeMesh(NativeMesh nativeMesh, int instanceCount, Allocator allocator)
            {
                int vertexCount = nativeMesh.vertices.Length;
                int indexCount = nativeMesh.indices.Length;
                
                vertices = new NativeArray<Vector3>(vertexCount * instanceCount, allocator);

                normals = new NativeArray<Vector3>(vertexCount * instanceCount, allocator);
                hasNormals = nativeMesh.hasNormals;
                
                tangents = new NativeArray<Vector4>(vertexCount * instanceCount, allocator);
                hasTangents = nativeMesh.hasTangents;
                
                colors = new NativeArray<Color>(vertexCount * instanceCount, allocator);
                hasColors = nativeMesh.hasColors;
                
                texCoords = new NativeArray<Vector2>(vertexCount * instanceCount * nativeMesh.activeUVChannelCount, allocator);
                hasTexCoords = nativeMesh.hasTexCoords;
                
                indices = new NativeArray<int>(indexCount * instanceCount, allocator);
                subMeshes = new NativeArray<SubMeshDescriptor>(nativeMesh.subMeshes.Length, allocator);
                activeUVChannelCount = nativeMesh.activeUVChannelCount;
                activeUVChannelMask = nativeMesh.activeUVChannelMask;
            }

            public NativeMesh(Mesh mesh, Allocator allocator)
            {
                using (var meshDataArray = Mesh.AcquireReadOnlyMeshData(mesh))
                {
                    var meshData = meshDataArray[0];
                    int vertexCount = mesh.vertexCount;

                    vertices = new NativeArray<Vector3>(vertexCount, allocator);
                    meshData.GetVertices(vertices);

                    normals = new NativeArray<Vector3>(vertexCount, allocator);
                    hasNormals = meshData.HasVertexAttribute(VertexAttribute.Normal);
                    if (hasNormals)
                    {
                        meshData.GetNormals(normals);
                    }

                    tangents = new NativeArray<Vector4>(vertexCount, allocator);
                    hasTangents = meshData.HasVertexAttribute(VertexAttribute.Tangent);
                    if (hasTangents)
                    {
                        meshData.GetTangents(tangents);
                    }

                    colors = new NativeArray<Color>(vertexCount, allocator);
                    hasColors = meshData.HasVertexAttribute(VertexAttribute.Color);
                    if (hasColors)
                    {
                        meshData.GetColors(colors);
                    }

                    activeUVChannelCount = 0;
                    activeUVChannelMask = 0;

                    for (int i = 0; i < 8; ++i)
                    {
                        if (meshData.HasVertexAttribute(VertexAttribute.TexCoord0 + i))
                        {
                            activeUVChannelMask |= 1 << i;
                            activeUVChannelCount++;
                        }
                    }

                    texCoords = new NativeArray<Vector2>(vertexCount * activeUVChannelCount, allocator);
                    hasTexCoords = activeUVChannelCount > 0;
                    if (hasTexCoords)
                    {
                        using (var tmpUV = new NativeArray<Vector2>(vertexCount, Allocator.Temp))
                        {
                            int activeUVChannelIndex = 0;
                            for (int channnel = 0; channnel < 8; ++channnel)
                            {
                                if (meshData.HasVertexAttribute(VertexAttribute.TexCoord0 + channnel))
                                {
                                    meshData.GetUVs(channnel, tmpUV);
                                    texCoords.Slice(activeUVChannelIndex * vertexCount, vertexCount).CopyFrom(tmpUV);
                                    activeUVChannelIndex++;
                                }
                            }
                        }
                    }
                    
                    subMeshes = new NativeArray<SubMeshDescriptor>(meshData.subMeshCount, allocator);

                    int indexCount = 0;
                    for (int i = 0; i < subMeshes.Length; ++i)
                    {
                        var subMesh = meshData.GetSubMesh(i);
                        subMeshes[i] = subMesh;
                        indexCount += subMesh.indexCount;
                    }

                    indices = new NativeArray<int>(indexCount, allocator);

                    for (int i = 0; i < subMeshes.Length; ++i)
                    {
                        using (var tmpIndices = new NativeArray<int>(subMeshes[i].indexCount, Allocator.Temp))
                        {
                            meshData.GetIndices(tmpIndices, i);
                            indices.Slice(subMeshes[i].indexStart, subMeshes[i].indexCount).CopyFrom(tmpIndices);
                        }
                    }
                }
            }

            public void Dispose()
            {
                vertices.Dispose();
                normals.Dispose();
                tangents.Dispose();
                colors.Dispose();
                texCoords.Dispose();
                indices.Dispose();
                subMeshes.Dispose();
            }

            public JobHandle Dispose(JobHandle deps)
            {
                deps = vertices.Dispose(deps);
                deps = normals.Dispose(deps);
                deps = tangents.Dispose(deps);
                deps = colors.Dispose(deps);
                deps = texCoords.Dispose(deps); 
                deps = indices.Dispose(deps);
                deps = subMeshes.Dispose(deps);
                return deps;
            }
        }

        [BurstCompile]
        struct SplineMeshJob : IJobParallelFor
        {
            public float3 defaultUpDirection;
            public ForwardAxisMode forwardAxis;
            public float2 uvScale;
            public float2 uvOffset;
            public float uvRotation;
            public Bounds meshBounds;
            public bool smoothScaleRoll;

            [ReadOnly]
            public NativeArray<SplineMeshPoint> splineMeshPoints;
            [ReadOnly]
            public NativeMesh input;
            [WriteOnly]
            public NativeMesh output;

            public void Execute(int index)
            {
                int numInputVertices = input.vertices.Length;
                int numOutputVertices = output.vertices.Length;

                int segmentIndex = index / numInputVertices;
                SplineMeshPoint prev = splineMeshPoints[segmentIndex];
                SplineMeshPoint next = splineMeshPoints[segmentIndex + 1];

                int inputVertexIndex = index % numInputVertices;
                float3 vertex = input.vertices[inputVertexIndex];
                float3 normal = input.normals[inputVertexIndex];
                float4 tangent = input.tangents[inputVertexIndex];

                float vertexAxisValue = vertex[(int)forwardAxis];
                float alpha = saturate((vertexAxisValue - meshBounds.min[(int)forwardAxis]) / meshBounds.size[(int)forwardAxis]);

                // Apply hermite interp to Alpha if desired
                float smoothAlpha = smoothScaleRoll ? smoothstep(0.0f, 1.0f, alpha) : alpha;

                // Then find the point and direction of the spline at this point along
                float3 splinePos = CurveMath.InterpolatePosition(prev.position, prev.tangent, next.tangent, next.position, alpha);
                float3 splineDir = normalizesafe(CurveMath.InterpolateTangent(prev.position, prev.tangent, next.tangent, next.position, alpha));
                
                // Find base frenet frame
                float3 baseX = normalizesafe(cross(defaultUpDirection, splineDir));
                float3 baseY = normalizesafe(cross(splineDir, baseX));

                // Offset the spline by the desired amount
                float2 offset = lerp(prev.offset, next.offset, smoothAlpha);
                splinePos += offset.x * baseX;
                splinePos += offset.y * baseY;

                // Apply roll to frame around spline
                float roll = lerp(prev.roll, next.roll, smoothAlpha);
                float cosAng = cos(roll);
                float sinAng = sin(roll);
                float3 cx = (cosAng * baseX) - (sinAng * baseY);
                float3 cy = (cosAng * baseY) + (sinAng * baseX);
                float3 cz = splineDir;

                // Find scale at this point along spline
                float2 scale = lerp(prev.scale.xy, next.scale.xy, smoothAlpha);

                // Build overall transform
                float3 vertexMask;
                float4x4 sliceTransform;
                float4x4 scaleTransform;
                switch (forwardAxis)
                {
                    case ForwardAxisMode.X:
                        vertexMask = float3(0, 1, 1);
                        sliceTransform = float4x4(float4(cz,        0), 
                                                  float4(cx,        0), 
                                                  float4(cy,        0), 
                                                  float4(splinePos, 1));
                        
                        scaleTransform = float4x4.Scale(1, scale.x, scale.y);
                        break;
                    case ForwardAxisMode.Y:
                        vertexMask = float3(1, 0, 1);
                        sliceTransform = float4x4(float4(cy,        0), 
                                                  float4(cz,        0), 
                                                  float4(cx,        0), 
                                                  float4(splinePos, 1));
                        
                        scaleTransform = float4x4.Scale(scale.y, 1, scale.x);
                        break;
                    default:
                        vertexMask = float3(1, 1, 0);
                        sliceTransform = float4x4(float4(cx,        0), 
                                                  float4(cy,        0), 
                                                  float4(cz,        0), 
                                                  float4(splinePos, 1));
                        
                        scaleTransform = float4x4.Scale(scale.x, scale.y, 1);
                        break;
                }

                output.vertices[index] = transform(mul(sliceTransform, scaleTransform), vertex * vertexMask);
                
                if (output.hasNormals)
                    output.normals[index] = normalizesafe(rotate(sliceTransform, normal));
                
                if (output.hasTangents)
                    output.tangents[index] = mul(sliceTransform, tangent);
                
                if (output.hasColors)
                    output.colors[index] = input.colors[inputVertexIndex];

                if (output.hasTexCoords)
                {
                    quaternion uvTransform = quaternion.RotateZ(uvRotation);
                    
                    for (int channel = 0; channel < input.activeUVChannelCount; ++channel)
                    {
                        float2 uv = input.texCoords[channel * numInputVertices + inputVertexIndex];
                        uv *= uvScale;
                        uv += uvRotation;
                        uv = rotate(uvTransform, float3(uv, 0)).xy;
                        output.texCoords[channel * numOutputVertices + index] = uv;
                    }
                }
            }
        }

        [BurstCompile]
        struct CopyIndicesJob : IJob
        {
            public int instanceCount;
            
            [ReadOnly]
            public NativeMesh input;
            [WriteOnly]
            public NativeMesh output;

            public void Execute()
            {
                int outputStart = 0;

                for (int subMeshIndex = 0; subMeshIndex < input.subMeshes.Length; ++subMeshIndex)
                {
                    int baseVertex = 0;
                    
                    SubMeshDescriptor subMesh    = input.subMeshes[subMeshIndex];
                    SubMeshDescriptor outSubMesh = new SubMeshDescriptor(outputStart, 0) {baseVertex = baseVertex};
                    NativeSlice<int>  indices    = input.indices.Slice(subMesh.indexStart, subMesh.indexCount);

                    for (int instanceIndex = 0; instanceIndex < instanceCount; ++instanceIndex)
                    {
                        int startIndex = outSubMesh.indexStart + instanceIndex * subMesh.indexCount;
                        NativeSlice<int> outIndices = output.indices.Slice(startIndex, subMesh.indexCount);

                        for (int i = 0; i < subMesh.indexCount; ++i)
                        {
                            outIndices[i] = baseVertex + indices[i];
                        }

                        baseVertex += input.vertices.Length;
                        outSubMesh.indexCount += subMesh.indexCount;
                    }

                    output.subMeshes[subMeshIndex] = outSubMesh;
                    outputStart += outSubMesh.indexCount;
                }
            }
        }

        JobHandle ScheduleRebuild(out NativeMesh outputSplineMesh)
        {
            NativeMesh inputMesh = new NativeMesh(m_MeshInstance, Allocator.TempJob);
            
            float inputSize = m_MeshInstance.bounds.size[(int)m_ForwardAxis] * m_AutoSegmentLengthScale;
            float originalSplineLength = spline.splineLength;
            float clippedSplineLength  = originalSplineLength * (clipRange.y - clipRange.x);
            float splineDistanceStart  = originalSplineLength * clipRange.x;
            
            if (inputSize == 0f || originalSplineLength == 0f || clippedSplineLength == 0f)
            {
                outputSplineMesh = default;
                inputMesh.Dispose();
                return default;
            }

            float meshLength;
            int segmentCount;
            
            if (m_AutoSegments)
            {
                float meshDivisions = clippedSplineLength / inputSize;
                float remainingLength = frac(meshDivisions) * inputSize;
                segmentCount = (int) max(1, trunc(meshDivisions));
                meshLength = inputSize + (remainingLength / segmentCount);
            }
            else
            {
                meshLength = clippedSplineLength / m_SegmentCount;
                segmentCount = m_SegmentCount;
            }

            int pointCount = segmentCount + 1;
            NativeArray<SplineMeshPoint> splineMeshPoints = new NativeArray<SplineMeshPoint>(pointCount, Allocator.TempJob);

            for (int index = 0; index < splineMeshPoints.Length; ++index)
            {
                float distance = splineDistanceStart + clamp(index * meshLength, 0f, clippedSplineLength);
                splineMeshPoints[index] = new SplineMeshPoint
                {
                    position = spline.GetPositionAtDistance(distance, Space.Self),
                    tangent = spline.GetForwardAtDistance(distance, Space.Self) * meshLength,
                    offset = m_Offset,
                    scale = float3(m_Scale, 1f) * spline.GetScaleAtDistance(distance),
                    roll = radians(spline.GetRollAtDistance(distance, Space.Self))
                };
            }
            
            outputSplineMesh = new NativeMesh(inputMesh, segmentCount, Allocator.TempJob);

            if (m_IndexFormat == IndexFormat.UInt16 && outputSplineMesh.indices.Length >= ushort.MaxValue)
            {
                Debug.LogError("SplineMesh: Index overflow. More indices than IndexFormat.UInt16 can contain, please use IndexFormat.UInt32.");
                inputMesh.Dispose();
                splineMeshPoints.Dispose();
                outputSplineMesh.Dispose();
                return default;
            }

            JobHandle jobs = default;
            
            // Create spline mesh
            jobs = new SplineMeshJob
            {
                defaultUpDirection = spline.defaultUpDirection,
                forwardAxis = m_ForwardAxis,
                meshBounds = m_MeshInstance.bounds,
                smoothScaleRoll = m_SmoothScaleAndRoll,
                uvScale = m_UVScale,
                uvOffset = m_UVOffset,
                uvRotation = m_UVRotation,
                splineMeshPoints = splineMeshPoints,
                input = inputMesh,
                output = outputSplineMesh
            }.Schedule(outputSplineMesh.vertices.Length, 3, jobs);

            // Apply indices
            jobs = new CopyIndicesJob
            {
                instanceCount = segmentCount,
                input = inputMesh,
                output = outputSplineMesh
            }.Schedule(jobs);

            // Dispose 
            jobs = splineMeshPoints.Dispose(jobs);
            jobs = inputMesh.Dispose(jobs);

            return jobs;
        }
    }
}