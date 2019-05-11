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
	_Out_ int32_t* allocationCount, // this will always be at most MAX_MONITORED_ALLOCATIONS-1
	_Out_ void** allocations,
    _Out_ uint8_t* allocationLimitReached
)
{
    threadMonitoringActive = false;
    auto allocationCountValue = threadState->nextAllocationIndex;
    if (allocationCountValue == MAX_MONITORED_ALLOCATIONS) {
        allocationCountValue -= 1;
        *allocationLimitReached = 1;
    }

	*allocationCount = allocationCountValue;

    if (threadState->gcCountWhenStartedMonitoring != gcCount)
        return AllocationMonitoringResult::GC;
    for (auto i = 0; i < allocationCountValue; i++) {
        auto allocation = threadState->allocations[i];
        auto headerSize = sizeof(void*);
        auto copyPtr = std::unique_ptr<uint8_t[]>{ new uint8_t[headerSize + allocation.objectSize] };

        std::copy((uint8_t*)(allocation.objectId - headerSize), (uint8_t*)(allocation.objectId + allocation.objectSize), copyPtr.get());

        threadState->gcSafeCopiesOfAllocations[i] = std::move(copyPtr);
    }
    if (threadState->gcCountWhenStartedMonitoring != gcCount)
        return AllocationMonitoringResult::GC;

	*allocations = threadState->gcSafeCopiesOfAllocations.data();
	return AllocationMonitoringResult::OK;
}