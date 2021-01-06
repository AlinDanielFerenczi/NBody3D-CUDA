
#include "cuda_runtime.h"
#include "device_launch_parameters.h"
#include "device_atomic_functions.h"
#define DllExport __declspec(dllexport)
#include <stdio.h>
#include <string>

class Vector3D
{
public:
    float X;
    float Y;
    float Z;
};

extern "C"
{
    DllExport cudaError_t calculatePosition(Vector3D* positions, Vector3D* velocities, Vector3D* accelerations, float* masses, float timeStep, int size);
    cudaError_t calculatePosition(Vector3D* positions, Vector3D* velocities, Vector3D* accelerations, float* masses, float timeStep, int size);
}

__global__ void calculateVelocityKernel(Vector3D* velocities, Vector3D* accelerations, float timeStep)
{
    int i = blockIdx.x * blockDim.x + threadIdx.x;

    float halfStep = timeStep / 2;
    float multipliedX = accelerations[i].X * halfStep + velocities[i].X;
    float multipliedY = accelerations[i].Y * halfStep + velocities[i].Y;
    float multipliedZ = accelerations[i].Z * halfStep + velocities[i].Z;

    Vector3D result;
    result.X = multipliedX;
    result.Y = multipliedY;
    result.Z = multipliedZ;

    velocities[i] = result;
}

//__device__ unsigned long long totThr = 0;

__global__ void calculatePositionKernel(Vector3D *positions, Vector3D *velocities, float timeStep)
{
    int i = blockIdx.x * blockDim.x + threadIdx.x;

    float multipliedX = velocities[i].X * timeStep + positions[i].X;
    float multipliedY = velocities[i].Y * timeStep + positions[i].Y;
    float multipliedZ = velocities[i].Z * timeStep + positions[i].Z;

    Vector3D result;
    result.X = multipliedX;
    result.Y = multipliedY;
    result.Z = multipliedZ;

    positions[i] = result;
}

__global__ void calculateAccelerationKernel(Vector3D* positions, float* masses, Vector3D* accelerations) 
{
    int i = blockIdx.x * blockDim.x + threadIdx.x;
    int j = blockIdx.y * blockDim.y + threadIdx.y;

    float diffX = positions[j].X - positions[i].X;
    float diffY = positions[j].Y - positions[i].Y;
    float diffZ = positions[j].Z - positions[i].Z;

    float dist = sqrtf(diffX * diffX + diffY * diffY + diffZ * diffZ);
    float F = (1 * masses[i] * masses[j]) / (dist * dist + 0.1 * 0.1);

    //atomicAdd(&totThr, 1);

    atomicAdd(&accelerations[i].X, diffX * F * dist);
    atomicAdd(&accelerations[i].Y, diffY * F * dist);
    atomicAdd(&accelerations[i].Z, diffZ * F * dist);
}

__global__ void initAccelerationsKernel(Vector3D* accelerations)
{
    int i = blockIdx.x * blockDim.x + threadIdx.x;
    Vector3D newVector;
    newVector.X = 0;
    newVector.Y = 0;
    newVector.Z = 0;
    accelerations[i] = newVector;
}

cudaError_t calculatePosition(Vector3D* positions, Vector3D* velocities, Vector3D* accelerations, float* masses, float timeStep, int size)
{
    FILE* fptr;
    fptr = fopen("fileopen.txt", "w");
    Vector3D* dev_positions = 0;
    Vector3D* dev_velocities = 0;
    Vector3D* dev_accelerations = 0;
    float* dev_masses = 0;
    cudaError_t cudaStatus;

    /*for (int i = 0; i < size; i++)
    {
        fprintf(fptr,
            "X for object %f with position: %f velocity: %f mass: %f\n",
            i, positions[i].X, velocities[i].X, masses[i]
        );
        fprintf(fptr,
            "Y for object %f with position: %f velocity: %f mass: %f\n",
            i, positions[i].Y, velocities[i].Z, masses[i]
        );
        fprintf(fptr,
            "Z for object %f with position: %f velocity: %f mass: %f\n\n",
            i, positions[i].Z, velocities[i].Y, masses[i]
        );
    }*/

    // Choose which GPU to run on, change this on a multi-GPU system.
    cudaStatus = cudaSetDevice(0);
    if (cudaStatus != cudaSuccess) {
        fprintf(stderr, "cudaSetDevice failed!  Do you have a CUDA-capable GPU installed?");
        goto Error;
    }

    // Allocate GPU buffers for three vectors (two input, one output)    .

    cudaStatus = cudaMalloc((void**)&dev_positions, size * sizeof(Vector3D));
    if (cudaStatus != cudaSuccess) {
        fprintf(stderr, "cudaMalloc failed!");
        goto Error;
    }

    cudaStatus = cudaMalloc((void**)&dev_velocities, size * sizeof(Vector3D));
    if (cudaStatus != cudaSuccess) {
        fprintf(stderr, "cudaMalloc failed!");
        goto Error;
    }

    cudaStatus = cudaMalloc((void**)&dev_masses, size * sizeof(float));
    if (cudaStatus != cudaSuccess) {
        fprintf(stderr, "cudaMalloc failed!");
        goto Error;
    }

    cudaStatus = cudaMalloc((void**)&dev_accelerations, size * sizeof(Vector3D));
    if (cudaStatus != cudaSuccess) {
        fprintf(stderr, "cudaMalloc failed!");
        goto Error;
    }

    // Copy input vectors from host memory to GPU buffers.
    cudaStatus = cudaMemcpy(dev_positions, positions, size * sizeof(Vector3D), cudaMemcpyHostToDevice);
    if (cudaStatus != cudaSuccess) {
        fprintf(stderr, "cudaMemcpy failed!");
        goto Error;
    }

    cudaStatus = cudaMemcpy(dev_velocities, velocities, size * sizeof(Vector3D), cudaMemcpyHostToDevice);
    if (cudaStatus != cudaSuccess) {
        fprintf(stderr, "cudaMemcpy failed!");
        goto Error;
    }

    cudaStatus = cudaMemcpy(dev_accelerations, accelerations, size * sizeof(Vector3D), cudaMemcpyHostToDevice);
    if (cudaStatus != cudaSuccess) {
        fprintf(stderr, "cudaMemcpy failed!");
        goto Error;
    }

    cudaStatus = cudaMemcpy(dev_masses, masses, size * sizeof(float), cudaMemcpyHostToDevice);
    if (cudaStatus != cudaSuccess) {
        fprintf(stderr, "cudaMemcpy failed!");
        goto Error;
    }
    dim3 block(32, 32);
    dim3 grid;
    grid.x = (size + block.x - 1) / block.x;
    grid.y = (size + block.y - 1) / block.y;
    dim3 simpleGrid;
    simpleGrid = (size + block.x - 1) / block.x;
    calculateVelocityKernel <<<simpleGrid, block>>> (dev_velocities, dev_accelerations, timeStep);
    calculatePositionKernel <<<simpleGrid, block>>> (dev_positions, dev_velocities, timeStep);
    initAccelerationsKernel <<<simpleGrid, block>>> (dev_accelerations);
    calculateAccelerationKernel <<<grid, block>>> (dev_positions, dev_masses, dev_accelerations);
    calculateVelocityKernel <<<simpleGrid, block>>> (dev_velocities, dev_accelerations, timeStep);

    // Check for any errors launching the kernel
    cudaStatus = cudaGetLastError();
    if (cudaStatus != cudaSuccess) {
        fprintf(stderr, "addKernel launch failed: %s\n", cudaGetErrorString(cudaStatus));
        goto Error;
    }

    // cudaDeviceSynchronize waits for the kernel to finish, and returns
    // any errors encountered during the launch.
    cudaStatus = cudaDeviceSynchronize();
    if (cudaStatus != cudaSuccess) {
        fprintf(stderr, "cudaDeviceSynchronize returned error code %d after launching addKernel!\n", cudaStatus);
        goto Error;
    }

    // Copy output vector from GPU buffer to host memory.
    cudaStatus = cudaMemcpy(positions, dev_positions, size * sizeof(Vector3D), cudaMemcpyDeviceToHost);
    if (cudaStatus != cudaSuccess) {
        fprintf(stderr, "cudaMemcpy failed!");
        goto Error;
    }

    cudaStatus = cudaMemcpy(velocities, dev_velocities, size * sizeof(Vector3D), cudaMemcpyDeviceToHost);
    if (cudaStatus != cudaSuccess) {
        fprintf(stderr, "cudaMemcpy failed!");
        goto Error;
    }

    //unsigned long long total;
    //cudaMemcpyFromSymbol(&total, totThr, sizeof(unsigned long long));
    //fprintf(fptr, "Total threads counted: %lu\n", total);

Error:
    cudaFree(dev_positions);
    cudaFree(dev_velocities);
    cudaFree(dev_accelerations);
    cudaFree(dev_masses);
    //fclose(fptr);

    return cudaStatus;
}