#include "Shared.h"
#include <sal.h>

thread_local bool threadMonitoringActive = false;
thread_local std::unique_ptr<ProfilerThreadState> threadState = std::make_unique<ProfilerThreadState>();
std::atomic_ulong gcCount;

enum class AllocationMonitoringResult : int {
	OK = 0,
    GC = 1
};

extern "C" AllocationMonitoringResult StartMonitoringCurrentThreadAllocations()
{
    threadMonitoringActive = true;
    threadState->totalAllocationCount = 0;
    threadState->totalAllocationBytes = 0;
    threadState->nextAllocationIndex = 0;
    threadState->gcCountWhenStartedMonitoring = gcCount;
	return AllocationMonitoringResult::OK;
}

extern "C" AllocationMonitoringResult StopMonitoringCurrentThreadAllocations(
	_Out_ int32_t* allocationCount, // this will always be at most MAX_MONITORED_ALLOCATIONS-1
	_Out_ void** allocations,
    _Out_ int32_t* totalAllocationCount,
    _Out_ int32_t* totalAllocationBytes
)
{
    threadMonitoringActive = false;
    auto allocationCountValue = threadState->nextAllocationIndex;

	*allocationCount = allocationCountValue;
    *totalAllocationCount = threadState->totalAllocationCount;
    *totalAllocationBytes = static_cast<int32_t>(threadState->totalAllocationBytes);

    if (threadState->gcCountWhenStartedMonitoring != gcCount)
        return AllocationMonitoringResult::GC;
    for (auto i = 0u; i < allocationCountValue; i++) {
        auto allocation = threadState->allocations[i];
        auto objectStart = allocation.objectId - OBJECT_HEADER_SIZE;
        auto copyPtr = std::unique_ptr<uint8_t[]>{ new uint8_t[allocation.objectSize] };

        std::copy((uint8_t*)objectStart, (uint8_t*)(objectStart + allocation.objectSize), copyPtr.get());

        threadState->gcSafeCopiesOfAllocations[i] = std::move(copyPtr);
    }
    if (threadState->gcCountWhenStartedMonitoring != gcCount)
        return AllocationMonitoringResult::GC;

	*allocations = threadState->gcSafeCopiesOfAllocations.data();
	return AllocationMonitoringResult::OK;
}