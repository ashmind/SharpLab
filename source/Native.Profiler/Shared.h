#pragma once

#include <array>
#include <atomic>
#include "cor.h"
#include "corprof.h"

static const size_t MAX_STORED_ALLOCATIONS = 10;
static const size_t OBJECT_HEADER_SIZE = sizeof(void*);

struct Allocation {
    ObjectID objectId;
    SIZE_T objectSize;
};

struct ProfilerThreadState {
    unsigned long gcCountWhenStartedMonitoring;
	unsigned int nextAllocationIndex;
    SIZE_T totalAllocationBytes;
    unsigned int totalAllocationCount;
    std::array<Allocation, MAX_STORED_ALLOCATIONS> allocations;
    std::array<std::unique_ptr<BYTE[]>, MAX_STORED_ALLOCATIONS> gcSafeCopiesOfAllocations;
};

extern thread_local bool threadMonitoringActive;
extern thread_local std::unique_ptr<ProfilerThreadState> threadState;

extern std::atomic_ulong gcCount;