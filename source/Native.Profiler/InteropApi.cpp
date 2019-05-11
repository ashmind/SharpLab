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
    threadState->nextAllocationIndex = 0;
    threadState->gcCountWhenStartedMonitoring = gcCount;
	return AllocationMonitoringResult::OK;
}

extern "C" AllocationMonitoringResult StopMonitoringCurrentThreadAllocations(
	_Out_ int* allocationCount,
	_Out_ void** allocations
)
{
    threadMonitoringActive = false;
    auto allocationCountValue = threadState->nextAllocationIndex;
	*allocationCount = allocationCountValue;

    if (threadState->gcCountWhenStartedMonitoring != gcCount)
        return AllocationMonitoringResult::GC;
    for (auto i = 0; i < allocationCountValue; i++) {
        auto allocation = threadState->allocations[i];
        auto headerSize = sizeof(void*);
        auto copyPtr = std::unique_ptr<BYTE[]>{ new BYTE[headerSize + allocation.objectSize] };

        std::copy((BYTE*)(allocation.objectId - headerSize), (BYTE*)(allocation.objectId + allocation.objectSize), copyPtr.get());

        threadState->gcSafeCopiesOfAllocations[i] = std::move(copyPtr);
    }
    if (threadState->gcCountWhenStartedMonitoring != gcCount)
        return AllocationMonitoringResult::GC;

	*allocations = threadState->gcSafeCopiesOfAllocations.data();
	return AllocationMonitoringResult::OK;
}